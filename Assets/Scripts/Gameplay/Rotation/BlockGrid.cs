using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Rotation
{
    /// <summary>
    /// Stores every solid block in the level as a cell on a 3D grid of whole numbers.
    /// Each block under this object counts as one platform cell, and its position is rounded to the nearest cell.
    /// 
    /// Meshes under the buildings parent are background. The player is never blocked by them,
    /// and a platform block that sits behind a building is hidden, so it is not solid from that view.
    /// </summary>
    public class BlockGrid : MonoBehaviour
    {
        /// <summary>
        /// A box has to reach at least this far into a cell before it counts as overlapping it.
        /// Without this, a player standing exactly against a wall would count as being inside the wall.
        /// </summary>
        public const float EDGE_TOLERANCE = 0.001f;

        private const int VIEW_COUNT = 4;

        //don't exceed this number
        private const int MAX_CELLS_PER_BUILDING = 50000;

        [Tooltip("Optional. Parent object of the background buildings. The player walks in front of these, " +
            "and they hide any platform block that is behind them.")]
        [SerializeField] private Transform buildingsParent;

        [Tooltip("On: the player's depth is pulled to the block they stand on (normal flatten behavior). " +
            "Off: the player keeps their depth in this scene; flattened collision and rotation still work the same.")]
        [SerializeField] private bool allowDepthPulling = true;

        public bool AllowDepthPulling => allowDepthPulling;

        private readonly HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> oneWayCells = new HashSet<Vector3Int>();
        //every climbable block (ladders, vines), as a 3D cell. These are never solid.
        private readonly HashSet<Vector3Int> climbableCells = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> buildingCells = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector2Int, List<int>>[] depthsByScreenCell = new Dictionary<Vector2Int, List<int>>[VIEW_COUNT];
        private readonly HashSet<Vector2Int>[] solidScreenCells = new HashSet<Vector2Int>[VIEW_COUNT];

        private readonly HashSet<Vector2Int>[] oneWayScreenCells = new HashSet<Vector2Int>[VIEW_COUNT];

        //per view: the screen cells where a climbable block can be seen (and isn't hidden by a wall in front of it).
        private readonly HashSet<Vector2Int>[] climbableScreenCells = new HashSet<Vector2Int>[VIEW_COUNT];
        //per view: the depths of the visible climbable blocks behind each screen cell, nearest first.
        private readonly Dictionary<Vector2Int, List<int>>[] climbableDepthsByScreenCell = new Dictionary<Vector2Int, List<int>>[VIEW_COUNT];

        private readonly Dictionary<Vector2Int, int>[] nearestBuildingDepthByScreenCell = new Dictionary<Vector2Int, int>[VIEW_COUNT];

        void Awake()
        {
            Rebuild();
        }

        /// <summary>
        /// Reads the child blocks again and rebuilds the flattened picture for all four views.
        /// Call this after adding, removing, moving, or toggling blocks while the game is running.
        /// </summary>
        public void Rebuild()
        {
            cells.Clear();
            oneWayCells.Clear();
            climbableCells.Clear();
            buildingCells.Clear();
            foreach (Transform child in transform)
            {
                AddBlocksFrom(child, false, false);
            }

            if (buildingsParent != null)
            {
                foreach (MeshRenderer buildingMesh in buildingsParent.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!buildingMesh.enabled)
                        continue;

                    AddCellsInsideBounds(buildingMesh, buildingCells);
                }
            }

            for (int view = 0; view < VIEW_COUNT; view++)
            {
                Vector3Int right = ViewAxes.Right(view);
                Vector3Int forward = ViewAxes.Forward(view);

                //for each screen cell, the depth of the building that is closest to the camera.
                Dictionary<Vector2Int, int> buildingDepthsThisView = new Dictionary<Vector2Int, int>();

                foreach (Vector3Int buildingCell in buildingCells)
                {
                    Vector2Int buildingScreenCell = new Vector2Int(Dot(buildingCell, right), buildingCell.y);
                    int buildingDepth = Dot(buildingCell, forward);

                    if (!buildingDepthsThisView.TryGetValue(buildingScreenCell, out int nearestSoFar) || buildingDepth < nearestSoFar)
                        buildingDepthsThisView[buildingScreenCell] = buildingDepth;
                }

                nearestBuildingDepthByScreenCell[view] = buildingDepthsThisView;

                Dictionary<Vector2Int, List<int>> screenCells = new Dictionary<Vector2Int, List<int>>();
                HashSet<Vector2Int> solidCellsThisView = new HashSet<Vector2Int>();
                HashSet<Vector2Int> oneWayCellsThisView = new HashSet<Vector2Int>();

                AddVisiblePlatforms(cells, right, forward, buildingDepthsThisView, screenCells, solidCellsThisView);
                AddVisiblePlatforms(oneWayCells, right, forward, buildingDepthsThisView, screenCells, oneWayCellsThisView);

                foreach (List<int> depths in screenCells.Values)
                    depths.Sort();

                //climbable blocks are kept apart from the platforms: they are never stood inside or blocked by.
                Dictionary<Vector2Int, List<int>> climbableDepths = new Dictionary<Vector2Int, List<int>>();
                HashSet<Vector2Int> climbableCellsThisView = new HashSet<Vector2Int>();
                AddVisiblePlatforms(climbableCells, right, forward, buildingDepthsThisView, climbableDepths, climbableCellsThisView);

                foreach (List<int> depths in climbableDepths.Values)
                    depths.Sort();

                ResolveClimbableCovering(right, forward, buildingDepthsThisView, climbableDepths, solidCellsThisView, climbableCellsThisView);

                depthsByScreenCell[view] = screenCells;
                solidScreenCells[view] = solidCellsThisView;
                oneWayScreenCells[view] = oneWayCellsThisView;
                climbableScreenCells[view] = climbableCellsThisView;
                climbableDepthsByScreenCell[view] = climbableDepths;
            }
        }

        /// <summary>
        /// Adds one object under the grid. A group is opened and everything inside it is added the same way
        /// (this method calls itself for each child). Anything that isn't a group is one block.
        /// </summary>
        /// <param name="insideOneWayGroup">True when a group above this object has OneWayBlock on it.</param>
        /// <param name="insideClimbableGroup">True when a group above this object has ClimbableBlock on it.</param>
        private void AddBlocksFrom(Transform current, bool insideOneWayGroup, bool insideClimbableGroup)
        {
            if (!current.gameObject.activeInHierarchy)
                return;

            if (current == buildingsParent)
                return;

            bool isOneWay = insideOneWayGroup || current.GetComponent<OneWayBlock>() != null;
            bool isClimbable = insideClimbableGroup || current.GetComponent<ClimbableBlock>() != null;

            if (IsGroup(current))
            {
                foreach (Transform child in current)
                {
                    AddBlocksFrom(child, isOneWay, isClimbable);
                }

                return;
            }

            Vector3Int blockCell = Vector3Int.RoundToInt(current.position);

            //climbable wins: a ladder is never solid, even inside a one-way group.
            if (isClimbable)
                climbableCells.Add(blockCell);
            else if (isOneWay)
                oneWayCells.Add(blockCell);
            else
                cells.Add(blockCell);
        }

        /// <summary>
        /// A group is an empty object used to organize blocks: it has children, and nothing that is visible or solid.
        /// An empty object with no children still counts as an invisible block, the same as before groups existed.
        /// </summary>
        private static bool IsGroup(Transform current)
        {
            if (current.childCount == 0)
                return false;

            bool hasRenderer = current.GetComponent<Renderer>() != null;
            bool hasCollider = current.GetComponent<Collider>() != null;

            return !hasRenderer && !hasCollider;
        }

        /// <summary>
        /// Where a climbable block and a solid block share a screen cell, the one nearer the camera wins.
        /// A climbable block in front covers the wall behind it, so that cell is climbable and not solid
        /// (the player climbs up the face of the wall). A wall in front hides the climbable block.
        /// </summary>
        private void ResolveClimbableCovering(
            Vector3Int right,
            Vector3Int forward,
            Dictionary<Vector2Int, int> buildingDepthsThisView,
            Dictionary<Vector2Int, List<int>> climbableDepths,
            HashSet<Vector2Int> solidCellsThisView,
            HashSet<Vector2Int> climbableCellsThisView)
        {
            //the depth of the nearest visible solid block in each screen cell that also has a climbable block.
            Dictionary<Vector2Int, int> nearestSolidDepths = new Dictionary<Vector2Int, int>();

            foreach (Vector3Int cell in cells)
            {
                Vector2Int screenCell = new Vector2Int(Dot(cell, right), cell.y);

                if (!climbableCellsThisView.Contains(screenCell))
                    continue;

                int solidDepth = Dot(cell, forward);

                //hidden behind a building: it doesn't count.
                if (buildingDepthsThisView.TryGetValue(screenCell, out int buildingDepth) && buildingDepth < solidDepth)
                    continue;

                if (!nearestSolidDepths.TryGetValue(screenCell, out int nearestSoFar) || solidDepth < nearestSoFar)
                    nearestSolidDepths[screenCell] = solidDepth;
            }

            foreach (KeyValuePair<Vector2Int, int> pair in nearestSolidDepths)
            {
                Vector2Int screenCell = pair.Key;
                int nearestClimbableDepth = climbableDepths[screenCell][0];

                if (nearestClimbableDepth < pair.Value)
                {
                    //the climbable block is in front: the wall behind it is covered.
                    solidCellsThisView.Remove(screenCell);
                }
                else
                {
                    //the wall is in front: the climbable block can't be reached from this view.
                    climbableCellsThisView.Remove(screenCell);
                    climbableDepths.Remove(screenCell);
                }
            }
        }

        static int Dot(Vector3Int a, Vector3Int b) => a.x * b.x + a.y * b.y + a.z * b.z;

        /// <summary>
        /// Flattens one set of platform blocks for one view. A platform with a building between it and the camera
        /// can't be seen from this view, so it is left out and does not block or hold up the player from this view.
        /// </summary>
        static void AddVisiblePlatforms(
            HashSet<Vector3Int> platformCells,
            Vector3Int right,
            Vector3Int forward,
            Dictionary<Vector2Int, int> buildingDepthsThisView,
            Dictionary<Vector2Int, List<int>> screenCells,
            HashSet<Vector2Int> visibleScreenCells)
        {
            foreach (Vector3Int cell in platformCells)
            {
                Vector2Int screenCell = new Vector2Int(Dot(cell, right), cell.y);
                int platformDepth = Dot(cell, forward);

                if (buildingDepthsThisView.TryGetValue(screenCell, out int nearestBuildingDepth) && nearestBuildingDepth < platformDepth)
                    continue;

                if (!screenCells.TryGetValue(screenCell, out List<int> depths))
                {
                    depths = new List<int>();
                    screenCells[screenCell] = depths;
                }

                depths.Add(platformDepth);   //smaller number means closer to the camera
                visibleScreenCells.Add(screenCell);
            }
        }

        /// <summary>
        /// Adds every cell whose center is inside the mesh's bounding box.
        /// A unit cube on a whole-number position gives one cell. A larger mesh gives every cell it covers.
        /// </summary>
        static void AddCellsInsideBounds(MeshRenderer mesh, HashSet<Vector3Int> cellSet)
        {
            Bounds bounds = mesh.bounds;

            int firstX = Mathf.CeilToInt(bounds.min.x - EDGE_TOLERANCE);
            int firstY = Mathf.CeilToInt(bounds.min.y - EDGE_TOLERANCE);
            int firstZ = Mathf.CeilToInt(bounds.min.z - EDGE_TOLERANCE);
            int lastX = Mathf.FloorToInt(bounds.max.x + EDGE_TOLERANCE);
            int lastY = Mathf.FloorToInt(bounds.max.y + EDGE_TOLERANCE);
            int lastZ = Mathf.FloorToInt(bounds.max.z + EDGE_TOLERANCE);
            if (lastX < firstX || lastY < firstY || lastZ < firstZ)
                return;

            long cellCount = (long)(lastX - firstX + 1) * (lastY - firstY + 1) * (lastZ - firstZ + 1);
            if (cellCount > MAX_CELLS_PER_BUILDING)
            {
                Debug.LogWarning($"BlockGrid: The building mesh '{mesh.name}' covers {cellCount} cells, which is over the limit of {MAX_CELLS_PER_BUILDING}. It was skipped.", mesh);
                return;
            }

            for (int x = firstX; x <= lastX; x++)
            {
                for (int y = firstY; y <= lastY; y++)
                {
                    for (int z = firstZ; z <= lastZ; z++)
                    {
                        cellSet.Add(new Vector3Int(x, y, z));
                    }
                }
            }

        }

        /// <summary>
        /// Converts a position along one axis into the index of the cell that contains it.
        /// Cells are one unit wide and centered on whole numbers, so cell 0 covers -0.5 to 0.5.
        /// </summary>
        public static int CellIndex(float position)
        {
            return Mathf.FloorToInt(position + 0.5f);
        }

        /// <summary>
        /// Is there a platform block that can be seen behind this screen cell in the given view?
        /// </summary>
        public bool IsSolid(int view, int column, int row)
        {
            //one-way blocks are not solid: the player only collides with their top (see LandsOnOneWay).
            return solidScreenCells[view].Contains(new Vector2Int(column, row));
        }

        /// <summary>
        /// One-way blocks: did the player's feet cross the top of a visible one-way block while moving down
        /// from previousFeet to newFeet? Only tops the feet were at or above before the move count, which is
        /// what lets the player jump up through these blocks.
        /// </summary>
        /// <param name="landingHeight">The highest top that was crossed (where the feet should stop).</param>
        public bool LandsOnOneWay(int view, float minHorizontal, float maxHorizontal, float previousFeet, float newFeet, out float landingHeight)
        {
            return FindOneWayLanding(view, minHorizontal, maxHorizontal, previousFeet, newFeet, false, 0, out landingHeight);
        }

        /// <summary>
        /// Background mode version of LandsOnOneWay: only one-way blocks on exactly this depth layer count.
        /// </summary>
        public bool LandsOnOneWayAtDepth(int view, float minHorizontal, float maxHorizontal, float previousFeet, float newFeet, int depth, out float landingHeight)
        {
            return FindOneWayLanding(view, minHorizontal, maxHorizontal, previousFeet, newFeet, true, depth, out landingHeight);
        }

        private bool FindOneWayLanding(int view, float minHorizontal, float maxHorizontal, float previousFeet, float newFeet,
            bool onlyThisDepth, int depth, out float landingHeight)
        {
            landingHeight = 0f;

            //the top of row r is at r + 0.5. A top counts if newFeet < top <= previousFeet.
            int highestRow = Mathf.FloorToInt(previousFeet + EDGE_TOLERANCE - 0.5f);
            int lowestRow = Mathf.FloorToInt(newFeet - 0.5f) + 1;

            int firstColumn = CellIndex(minHorizontal + EDGE_TOLERANCE);
            int lastColumn = CellIndex(maxHorizontal - EDGE_TOLERANCE);

            //check from the highest row down, so the first top found is the one the feet reach first.
            for (int row = highestRow; row >= lowestRow; row--)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    bool hasOneWayBlock;

                    if (onlyThisDepth)
                        hasOneWayBlock = oneWayCells.Contains(CellAt(view, column, row, depth));
                    else
                        hasOneWayBlock = oneWayScreenCells[view].Contains(new Vector2Int(column, row));

                    if (hasOneWayBlock)
                    {
                        landingHeight = row + 0.5f;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Does a 2D box on the screen overlap any solid screen cell in the given view?
        /// Edges that only touch a cell do not count as overlapping (see EdgeTolerance).
        /// </summary>
        public bool Blocked(
            int view,
            float minHorizontal,
            float maxHorizontal,
            float minVertical,
            float maxVertical)
        {
            int firstColumn = CellIndex(minHorizontal + EDGE_TOLERANCE);
            int lastColumn = CellIndex(maxHorizontal - EDGE_TOLERANCE);
            int firstRow = CellIndex(minVertical + EDGE_TOLERANCE);
            int lastRow = CellIndex(maxVertical - EDGE_TOLERANCE);
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                for (int row = firstRow; row <= lastRow; row++)
                {
                    if (IsSolid(view, column, row))
                        return true;
                }
            }

            return false;

        }

        /// <summary>
        /// Background mode: does a 2D box overlap a platform block at exactly this depth?
        /// Only the one layer of the level at that depth counts, whether the camera can see it or not.
        /// </summary>
        public bool BlockedAtDepth(
            int view,
            float minHorizontal,
            float maxHorizontal,
            float minVertical,
            float maxVertical,
            int depth)
        {
            int firstColumn = CellIndex(minHorizontal + EDGE_TOLERANCE);
            int lastColumn = CellIndex(maxHorizontal - EDGE_TOLERANCE);
            int firstRow = CellIndex(minVertical + EDGE_TOLERANCE);
            int lastRow = CellIndex(maxVertical - EDGE_TOLERANCE);

            for (int column = firstColumn; column <= lastColumn; column++)
            {
                for (int row = firstRow; row <= lastRow; row++)
                {
                    if (cells.Contains(CellAt(view, column, row, depth)))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Is any part of a 2D box behind a building, when the box is at the given depth?
        /// </summary>
        public bool BehindBuilding(
            int view,
            float minHorizontal,
            float maxHorizontal,
            float minVertical,
            float maxVertical,
            int depth)
        {
            int firstColumn = CellIndex(minHorizontal + EDGE_TOLERANCE);
            int lastColumn = CellIndex(maxHorizontal - EDGE_TOLERANCE);
            int firstRow = CellIndex(minVertical + EDGE_TOLERANCE);
            int lastRow = CellIndex(maxVertical - EDGE_TOLERANCE);

            for (int column = firstColumn; column <= lastColumn; column++)
            {
                for (int row = firstRow; row <= lastRow; row++)
                {
                    Vector2Int screenCell = new Vector2Int(column, row);

                    if (nearestBuildingDepthByScreenCell[view].TryGetValue(screenCell, out int buildingDepth) && buildingDepth < depth)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Can the camera NOT see all of a 2D box at the given depth?
        /// True when a building or a visible platform is closer to the camera in any of the box's screen cells.
        /// </summary>
        public bool HiddenFromCamera(
            int view,
            float minHorizontal,
            float maxHorizontal,
            float minVertical,
            float maxVertical,
            int depth)
        {
            int firstColumn = CellIndex(minHorizontal + EDGE_TOLERANCE);
            int lastColumn = CellIndex(maxHorizontal - EDGE_TOLERANCE);
            int firstRow = CellIndex(minVertical + EDGE_TOLERANCE);
            int lastRow = CellIndex(maxVertical - EDGE_TOLERANCE);

            for (int column = firstColumn; column <= lastColumn; column++)
            {
                for (int row = firstRow; row <= lastRow; row++)
                {
                    Vector2Int screenCell = new Vector2Int(column, row);

                    if (nearestBuildingDepthByScreenCell[view].TryGetValue(screenCell, out int buildingDepth) && buildingDepth < depth)
                        return true;

                    //the platform list is sorted, so index 0 is the visible platform nearest to the camera.
                    if (depthsByScreenCell[view].TryGetValue(screenCell, out List<int> platformDepths) && platformDepths[0] < depth)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Is there a climbable block (ladder, vines) that can be seen in this screen cell?
        /// </summary>
        public bool IsClimbable(int view, int column, int row)
        {
            return climbableScreenCells[view].Contains(new Vector2Int(column, row));
        }

        /// <summary>
        /// Background mode version of IsClimbable: only a climbable block on exactly this depth layer counts.
        /// </summary>
        public bool IsClimbableAtDepth(int view, int column, int row, int depth)
        {
            return climbableCells.Contains(CellAt(view, column, row, depth));
        }

        /// <summary>
        /// The depth of the climbable block in this screen cell that is closest to currentDepth.
        /// </summary>
        public bool TryGetClimbableDepth(int view, int column, int row, int currentDepth, out int depth)
        {
            depth = 0;

            if (!climbableDepthsByScreenCell[view].TryGetValue(new Vector2Int(column, row), out List<int> depths))
                return false;

            depth = depths[0];

            foreach (int candidateDepth in depths)
            {
                if (Mathf.Abs(candidateDepth - currentDepth) < Mathf.Abs(depth - currentDepth))
                    depth = candidateDepth;
            }

            return true;
        }

        /// <summary>
        /// The top of a ladder can be stood on like a one-way platform. Did the feet cross the top of a climbable
        /// column (a climbable cell with no climbable cell above it) while moving down from previousFeet to newFeet?
        /// </summary>
        /// <param name="onlyThisDepth">Background mode: only climbable blocks on this depth layer count.</param>
        public bool LandsOnClimbableTop(int view, float minHorizontal, float maxHorizontal, float previousFeet, float newFeet,
            bool onlyThisDepth, int depth, out float landingHeight)
        {
            landingHeight = 0f;

            int highestRow = Mathf.FloorToInt(previousFeet + EDGE_TOLERANCE - 0.5f);
            int lowestRow = Mathf.FloorToInt(newFeet - 0.5f) + 1;

            int firstColumn = CellIndex(minHorizontal + EDGE_TOLERANCE);
            int lastColumn = CellIndex(maxHorizontal - EDGE_TOLERANCE);

            for (int row = highestRow; row >= lowestRow; row--)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    bool climbableHere;
                    bool climbableAbove;

                    if (onlyThisDepth)
                    {
                        climbableHere = IsClimbableAtDepth(view, column, row, depth);
                        climbableAbove = IsClimbableAtDepth(view, column, row + 1, depth);
                    }
                    else
                    {
                        climbableHere = IsClimbable(view, column, row);
                        climbableAbove = IsClimbable(view, column, row + 1);
                    }

                    if (climbableHere && !climbableAbove)
                    {
                        landingHeight = row + 0.5f;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Turns a screen cell plus a depth back into the 3D cell it stands for.
        /// </summary>
        static Vector3Int CellAt(int view, int column, int row, int depth)
        {
            return ViewAxes.Right(view) * column + Vector3Int.up * row + ViewAxes.Forward(view) * depth;
        }

        /// <summary>
        /// Finds the depth of a platform block that can be seen behind the given screen cell.
        /// </summary>
        /// <param name="nearestToCamera">True picks the block closest to the camera. False picks the block closest to currentDepth.</param>
        /// <param name="currentDepth">The depth we are at right now. Only used when nearestToCamera is false.</param>
        /// <param name="depth">The depth that was found.</param>
        /// <returns>False if there is no block behind that screen cell.</returns>
        public bool TryGetDepth(int view, int column, int row, bool nearestToCamera, int currentDepth, out int depth)
        {
            depth = 0;

            if (!depthsByScreenCell[view].TryGetValue(new Vector2Int(column, row), out List<int> depths))
                return false;

            //index 0 -> nearest to the camera.
            depth = depths[0];

            if (!nearestToCamera)
            {
                //uses depth closest to where we already are first.
                foreach (int candidateDepth in depths)
                {
                    if (Mathf.Abs(candidateDepth - currentDepth) < Mathf.Abs(depth - currentDepth))
                        depth = candidateDepth;
                }
            }

            return true;
        }
    }
}
