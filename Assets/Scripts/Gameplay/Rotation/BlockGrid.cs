using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Rotation
{
    /// <summary>
    /// Stores every solid block in the level as a cell on a 3D grid of whole numbers.
    /// Each child of this object counts as one platform block, and its position is rounded to the nearest cell.
    /// A screen cell is solid if at least one platform block lines up behind it, no matter how deep it is.
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
        public const float EdgeTolerance = 0.001f;

        private const int ViewCount = 4;

        //don't exceed this number
        private const int MaximumCellsPerBuilding = 50000;

        [Tooltip("Optional. Parent object of the background buildings. The player walks in front of these, " +
            "and they hide any platform block that is behind them.")]
        [SerializeField] private Transform buildingsParent;

        //every platform block in the level, as a 3D cell.
        private readonly HashSet<Vector3Int> cells = new HashSet<Vector3Int>();

        //every cell covered by a background building.
        private readonly HashSet<Vector3Int> buildingCells = new HashSet<Vector3Int>();

        //one dictionary per view. The key is a screen cell (horizontal, vertical).
        //the value is the depths of the platform blocks that can be seen behind that screen cell, nearest first.
        private readonly Dictionary<Vector2Int, List<int>>[] depthsByScreenCell = new Dictionary<Vector2Int, List<int>>[ViewCount];

        //one dictionary per view. The key is a screen cell, the value is the depth of the building nearest to the camera there.
        private readonly Dictionary<Vector2Int, int>[] nearestBuildingDepthByScreenCell = new Dictionary<Vector2Int, int>[ViewCount];

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
            buildingCells.Clear();

            foreach (Transform block in transform)
            {
                if (!block.gameObject.activeInHierarchy)
                    continue;

                //the buildings parent is allowed to sit under this object without counting as a platform.
                if (block == buildingsParent)
                    continue;

                cells.Add(Vector3Int.RoundToInt(block.position));
            }

            if (buildingsParent != null)
            {
                //buildings can be grouped under more parents, so every mesh below the buildings parent is read.
                foreach (MeshRenderer buildingMesh in buildingsParent.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!buildingMesh.enabled)
                        continue;

                    AddCellsInsideBounds(buildingMesh, buildingCells);
                }
            }

            for (int view = 0; view < ViewCount; view++)
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

                foreach (Vector3Int cell in cells)
                {
                    Vector2Int screenCell = new Vector2Int(Dot(cell, right), cell.y);
                    int platformDepth = Dot(cell, forward);

                    //a platform with a building between it and the camera can't be seen from this view,
                    //so it is left out and does not block or hold up the player from this view.
                    if (buildingDepthsThisView.TryGetValue(screenCell, out int nearestBuildingDepth) && nearestBuildingDepth < platformDepth)
                        continue;

                    if (!screenCells.TryGetValue(screenCell, out List<int> depths))
                    {
                        depths = new List<int>();
                        screenCells[screenCell] = depths;
                    }

                    depths.Add(platformDepth);   //smaller number means closer to the camera
                }

                foreach (List<int> depths in screenCells.Values)
                    depths.Sort();

                depthsByScreenCell[view] = screenCells;
            }
        }

        static int Dot(Vector3Int a, Vector3Int b) => a.x * b.x + a.y * b.y + a.z * b.z;

        /// <summary>
        /// Adds every cell whose center is inside the mesh's bounding box.
        /// A unit cube on a whole-number position gives one cell. A larger mesh gives every cell it covers.
        /// </summary>
        static void AddCellsInsideBounds(MeshRenderer mesh, HashSet<Vector3Int> cellSet)
        {
            Bounds bounds = mesh.bounds;

            //cell centers are whole numbers, so round the low corner up and the high corner down.
            int firstX = Mathf.CeilToInt(bounds.min.x - EdgeTolerance);
            int firstY = Mathf.CeilToInt(bounds.min.y - EdgeTolerance);
            int firstZ = Mathf.CeilToInt(bounds.min.z - EdgeTolerance);
            int lastX = Mathf.FloorToInt(bounds.max.x + EdgeTolerance);
            int lastY = Mathf.FloorToInt(bounds.max.y + EdgeTolerance);
            int lastZ = Mathf.FloorToInt(bounds.max.z + EdgeTolerance);

            //a mesh that is too small or too far off the grid to cover a cell center adds nothing.
            if (lastX < firstX || lastY < firstY || lastZ < firstZ)
                return;

            long cellCount = (long)(lastX - firstX + 1) * (lastY - firstY + 1) * (lastZ - firstZ + 1);

            if (cellCount > MaximumCellsPerBuilding)
            {
                Debug.LogWarning($"BlockGrid: The building mesh '{mesh.name}' covers {cellCount} cells, which is over the limit of {MaximumCellsPerBuilding}. It was skipped.", mesh);
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
            return depthsByScreenCell[view].ContainsKey(new Vector2Int(column, row));
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
            int firstColumn = CellIndex(minHorizontal + EdgeTolerance);
            int lastColumn = CellIndex(maxHorizontal - EdgeTolerance);
            int firstRow = CellIndex(minVertical + EdgeTolerance);
            int lastRow = CellIndex(maxVertical - EdgeTolerance);

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
            int firstColumn = CellIndex(minHorizontal + EdgeTolerance);
            int lastColumn = CellIndex(maxHorizontal - EdgeTolerance);
            int firstRow = CellIndex(minVertical + EdgeTolerance);
            int lastRow = CellIndex(maxVertical - EdgeTolerance);

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
            int firstColumn = CellIndex(minHorizontal + EdgeTolerance);
            int lastColumn = CellIndex(maxHorizontal - EdgeTolerance);
            int firstRow = CellIndex(minVertical + EdgeTolerance);
            int lastRow = CellIndex(maxVertical - EdgeTolerance);

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
            int firstColumn = CellIndex(minHorizontal + EdgeTolerance);
            int lastColumn = CellIndex(maxHorizontal - EdgeTolerance);
            int firstRow = CellIndex(minVertical + EdgeTolerance);
            int lastRow = CellIndex(maxVertical - EdgeTolerance);

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
