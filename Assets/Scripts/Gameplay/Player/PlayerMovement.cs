using Gameplay.Rotation;
using UnityEngine;


namespace Gameplay.Player
{
    /// <summary>
    /// Handles player movement data and provides public methods for actions like walking, dashing, and jumping.
    ///
    /// The player is moved on the flattened 2D picture that the camera sees, not by Unity physics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CollisionCheck))]
    public class PlayerMovement : MonoBehaviour
    {

        #region variables
        //Stats
        [Header("Stats")]
        [SerializeField] private float moveSpeed;
        [SerializeField] private float maxMoveSpeed;
        [SerializeField] private float jumpForce;
        [SerializeField] private float maxJumpVelocity;
        [SerializeField] private float gravityForce = -20f;
        [SerializeField] private float cutJumpSpeed = -1f;
        [SerializeField] private float maxFallSpeed = 25f;

        //Rotation determinants
        [Header("Grid Size Adjustments")]
        [Tooltip("Half of the player's width and half of the player's height, in world units.")]
        [SerializeField] Vector2 halfSize = new Vector2(0.25f, 0.5f);
        [SerializeField] private float groundProbeDistance = 0.05f;
        [Tooltip("If the player falls below this height they are put back where they started.")]
        [SerializeField] private float respawnBelowHeight = -30f;
        private float horizontal, vertical, depth;
        private int view = 0;
        static readonly int[] ColumnOffsets = { 0, 1, -1, 2, -2 };
        //depth layers tried, in order, when the player's own layer has a block where they are standing.
        static readonly int[] LayerOffsets = { 0, -1, 1, -2, 2 };

        //movement validation to ensure the player can't be moved into a block.
        private const float MaximumStepDistance = 0.9f;

        private const float MaximumDeltaTime = 0.033f;

        //Movement logic variables
        private float moveInput;
        private Vector2 velocity;
        private float jumpBufferTimer;
        [SerializeField] private float jumpBufferTime = 0.4f;
        private float lastMoveSign = 1f;
        private Vector3 spawnWorldPosition;

        //bools
        private bool isJumpPressed = false;
        private bool isJumpReleased = false;
        private bool isDashing = false;
        private bool isGliding = false;
        private bool isFrozen;
        private bool isGrounded;
        //for background mode shifts.
        private bool isBehindGeometry;

        //Local instance objects and other monobehaviors
        private CollisionCheck collisionCheck;
        private Rigidbody rb;
        [SerializeField] BlockGrid grid;


        #endregion

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            collisionCheck = GetComponent<CollisionCheck>();

            if (grid == null)
                grid = FindAnyObjectByType<BlockGrid>();
        }

        void Start()
        {
            if (grid == null)
            {
                Debug.LogError("PlayerMovement: There is no BlockGrid in this scene, so the player cannot move. " +
                    "Add a BlockGrid object and make every solid block a child of it.");
                enabled = false;
                return;
            }

            //necessary so unity physics can't mess with the rb.
            rb.isKinematic = true;

            //blockgrid decides when the player is on the ground
            collisionCheck.UsePhysicsGroundCheck = false;

            Vector3 origin = transform.position;
            spawnWorldPosition = origin;

            //player must start at view = 0
            horizontal = origin.x;
            vertical = origin.y;
            depth = origin.z;

            UpdateGrounded();
            if (isGrounded)
            {
                UpdateDepthFromGround(nearestToCamera: false);
            }

            ApplyTransform();
        }

        //using update because movement doesn't rely on physics anymore hahahahaha teeheepero
        void Update()
        {
            if (isFrozen)
                return;

            float deltaTime = Mathf.Min(Time.deltaTime, MaximumDeltaTime);

            if (!isDashing)
            {
                velocity.x = Mathf.Clamp(moveInput * moveSpeed, -maxMoveSpeed, maxMoveSpeed);
            }

            ApplyGravity(deltaTime);

            jumpBufferTimer -= deltaTime;

            if (isGrounded && jumpBufferTimer > 0f && !isDashing)
            {
                Jump(jumpForce);
                isJumpPressed = false;
            }

            Walk(velocity.x * deltaTime);
            MoveVertical(velocity.y * deltaTime);

            UpdateGrounded();

            if (isBehindGeometry)
            {
                //depth stays fixed while behind, until nothing is in front of the player any more.
                TryLeaveBackgroundMode();
            }
            else
            {
                if (isGrounded)
                {
                    UpdateDepthFromGround(nearestToCamera: false);
                }

                //walking behind a building also puts the player in background mode, not only a rotation.
                TryEnterBackgroundMode();
            }

            if (vertical < respawnBelowHeight)
            {
                TeleportTo(spawnWorldPosition);
                return;
            }

            ApplyTransform();
        }

        public void SetFrozen(bool frozen)
        {
            isFrozen = frozen;
            if (frozen)
            {
                jumpBufferTimer = 0f;
            }
        }

        public void RequestJump()
        {
            if (!isFrozen)
                jumpBufferTimer = jumpBufferTime;
        }

        public void SetMoveInput(float input)
        {
            moveInput = Mathf.Clamp(input, -1f, 1f);

            if (Mathf.Abs(moveInput) > 0.1f)
            {
                lastMoveSign = Mathf.Sign(moveInput);
            }
        }

        /// <summary>
        /// Moves the player left or right on the screen by the given distance.
        /// If a solid screen cell is in the way, the player stops flush against it.
        /// </summary>
        /// <param name="distance">How far to move this frame. Positive is toward the right side of the screen.</param>
        public void Walk(float distance)
        {
            distance = Mathf.Clamp(distance, -MaximumStepDistance, MaximumStepDistance);

            if (distance == 0f)
            {
                return;
            }

            float newHorizontal = horizontal + distance;

            if (IsBlocked(newHorizontal - halfSize.x, newHorizontal + halfSize.x,
                vertical - halfSize.y, vertical + halfSize.y))
            {
                if (distance > 0)
                {
                    //right edge went into a solid column correction.
                    int blockedColumn = BlockGrid.CellIndex(newHorizontal + halfSize.x - BlockGrid.EdgeTolerance);
                    newHorizontal = blockedColumn - 0.5f - halfSize.x;
                }
                else
                {
                    //ditto for left edge.
                    int blockedColumn = BlockGrid.CellIndex(newHorizontal - halfSize.x + BlockGrid.EdgeTolerance);
                    newHorizontal = blockedColumn + 0.5f + halfSize.x;
                }

                velocity.x = 0;
            }

            horizontal = newHorizontal;
        }

        /// <summary>
        /// Moves the player up or down by the given distance.
        /// Landing on a block or hitting a ceiling stops the player flush against it.
        /// </summary>
        /// <param name="distance">How far to move this frame. Positive is up.</param>
        private void MoveVertical(float distance)
        {
            distance = Mathf.Clamp(distance, -MaximumStepDistance, MaximumStepDistance);

            if (distance == 0f)
            {
                return;
            }

            float newVertical = vertical + distance;

            if (IsBlocked(horizontal - halfSize.x, horizontal + halfSize.x,
                newVertical - halfSize.y, newVertical + halfSize.y))
            {   
                //head went into a solid row
                if (distance > 0)
                {
                    
                    int blockedRow = BlockGrid.CellIndex(newVertical + halfSize.y - BlockGrid.EdgeTolerance);
                    newVertical = blockedRow - 0.5f - halfSize.y;
                }
                //feet went into a solid row
                else
                {
                    
                    int blockedRow = BlockGrid.CellIndex(newVertical - halfSize.y + BlockGrid.EdgeTolerance);
                    newVertical = blockedRow + 0.5f + halfSize.y;
                }

                velocity.y = 0;
            }

            vertical = newVertical;
        }

        /// <summary>
        /// Simulates jump action by giving the player an instant upward speed.
        /// Only works while the player is standing on the ground.
        /// </summary>
        /// <param name="jumpVelocity">Upward speed to start the jump with, in units per second.</param>
        public void Jump(float jumpVelocity)
        {
            //StartCoroutine(JumpTime(jumpTime));

            if (isGrounded)
            {
                velocity.y = jumpVelocity;
                jumpBufferTimer = 0f;

                isGrounded = false;
                collisionCheck.IsGrounded = false;
            }

        }

        /// <summary>
        /// This method is called anytime the player releases the jump button (inputvalue = false).
        /// Currently used to force a "drop-down" effect when the player releases the jump button mid-jump.
        /// </summary>
        public void OnJumpReleased()
        {
            //isJumpPressed = false;

            if (!isGrounded && velocity.y > cutJumpSpeed)
            {
                velocity.y = cutJumpSpeed;

                //Debug.Log("PlayerMovement: Speed cut.");
            }

        }

        /// <summary>
        /// Instantly places the player at a world position, for things like respawning or checkpoints.
        /// Setting transform.position directly does not work, because this script writes the position every frame.
        /// </summary>
        public void TeleportTo(Vector3 worldPosition)
        {
            //a teleport always lands the player back in the normal flattened view.
            isBehindGeometry = false;

            //from a rotated view, a block somewhere else in the level can line up with the target spot.
            //FindPlacement slides the player sideways to a free column, the same way a rotation does.
            if (FindPlacement(view, worldPosition, out float newHorizontal, out float newDepth))
            {
                horizontal = newHorizontal;
                depth = newDepth;
            }
            else
            {
                horizontal = Vector3.Dot(worldPosition, (Vector3)ViewAxes.Right(view));
                depth = Vector3.Dot(worldPosition, (Vector3)ViewAxes.Forward(view));
            }

            vertical = worldPosition.y;
            velocity = Vector2.zero;

            UpdateGrounded();

            if (isGrounded)
            {
                UpdateDepthFromGround(nearestToCamera: false);
            }

            ApplyTransform();
        }

        Vector3 WorldPosition() =>
            (Vector3)ViewAxes.Right(view)
            * horizontal + Vector3.up
            * vertical + (Vector3)ViewAxes.Forward(view)
            * depth;

        void ApplyTransform()
        {
            transform.position = WorldPosition();
            transform.rotation = Quaternion.Euler(0, 90f * view, 0);
        }

        /// <summary>
        /// Rotations are never refused. If something ends up in front of the player, they go into background mode instead.
        /// </summary>
        public bool CanRotateTo(int newView) => true;

        public void CommitRotation(int newView)
        {
            //the player keeps their 3D position; only how it is measured changes.
            Vector3 world = WorldPosition();

            view = newView;
            horizontal = Vector3.Dot(world, (Vector3)ViewAxes.Right(view));
            depth = Vector3.Dot(world, (Vector3)ViewAxes.Forward(view));
            velocity = Vector2.zero;

            int depthLayer = FindFreeDepthLayer(FindStandingDepthLayer());

            //boxed in on every nearby layer (rare, e.g. rotating while falling past a floor):
            //use the old sideways nudge in the flattened view instead.
            if (IsInsideBlockAtDepth(depthLayer) && FindPlacement(view, world, out float nudgedHorizontal, out float nudgedDepth))
            {
                horizontal = nudgedHorizontal;
                depth = nudgedDepth;
                isBehindGeometry = false;
            }
            else if (ShouldBeBehindGeometry(depthLayer))
            {
                //keep the player where they really are until nothing is in front of them.
                isBehindGeometry = true;
                depth = depthLayer;
            }
            else
            {
                isBehindGeometry = false;
            }

            UpdateGrounded();

            if (!isBehindGeometry && isGrounded)
                UpdateDepthFromGround(nearestToCamera: true);

            ApplyTransform();
        }


        /// <summary>
        /// The world direction that points toward the right side of the screen in the current view.
        /// </summary>
        public Vector3 GetMoveAxis()
        {
            return (Vector3)ViewAxes.Right(view);
        }


        /// <summary>
        /// Pulls the player down while they are in the air.
        /// </summary>
        private void ApplyGravity(float deltaTime)
        {
            if (isGrounded || isDashing || isGliding)
            {
                return;
            }

            float totalGravity = Physics.gravity.y + gravityForce;

            velocity.y += totalGravity * deltaTime;

            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
            }
        }

        /// <summary>
        /// Checks the grid for a solid screen cell just below the player's feet.
        /// CollisionCheck is given the same answer so the ability scripts agree with the grid.
        /// </summary>
        private void UpdateGrounded()
        {
            float feetHeight = vertical - halfSize.y;
            float probeHeight = feetHeight - groundProbeDistance;

            bool groundIsBelow = IsBlocked(horizontal - halfSize.x, horizontal + halfSize.x,
                probeHeight, probeHeight);

            //moving upward never counts as grounded, otherwise a jump would be cancelled on its first frame.
            isGrounded = velocity.y <= 0 && groundIsBelow;
            collisionCheck.IsGrounded = isGrounded;
        }

        /// <summary>
        /// The depth layer the player is on. In background mode depth is always a whole number.
        /// </summary>
        private int DepthLayer => Mathf.RoundToInt(depth);

        /// <summary>
        /// Is this 2D box blocked? Normally that means any visible platform in the flattened view.
        /// In background mode only the platforms on the player's own depth layer count.
        /// </summary>
        private bool IsBlocked(float minHorizontal, float maxHorizontal, float minVertical, float maxVertical)
        {
            if (isBehindGeometry)
                return grid.BlockedAtDepth(view, minHorizontal, maxHorizontal, minVertical, maxVertical, DepthLayer);

            return grid.Blocked(view, minHorizontal, maxHorizontal, minVertical, maxVertical);
        }

        /// <summary>
        /// True while something is in front of the player at this depth layer, so they have to stay in background mode.
        /// </summary>
        private bool ShouldBeBehindGeometry(int depthLayer)
        {
            float leftEdge = horizontal - halfSize.x;
            float rightEdge = horizontal + halfSize.x;
            float feetHeight = vertical - halfSize.y;
            float headHeight = vertical + halfSize.y;
            float probeHeight = feetHeight - groundProbeDistance;

            //a building is between part of the player and the camera.
            if (grid.BehindBuilding(view, leftEdge, rightEdge, feetHeight, headHeight, depthLayer))
                return true;

            //the player is inside a solid cell of the flattened view, so they can't come out here yet.
            if (grid.Blocked(view, leftEdge, rightEdge, feetHeight, headHeight))
                return true;

            //the player is standing on a platform on their own layer that the camera can't see.
            bool standingOnOwnLayer = grid.BlockedAtDepth(view, leftEdge, rightEdge, probeHeight, probeHeight, depthLayer);
            bool visibleGroundBelow = grid.Blocked(view, leftEdge, rightEdge, probeHeight, probeHeight);

            return standingOnOwnLayer && !visibleGroundBelow;
        }

        /// <summary>
        /// Switches to background mode when a building is in front of any part of the player's body,
        /// for example when they walk along a platform that runs behind a building.
        /// </summary>
        private void TryEnterBackgroundMode()
        {
            int depthLayer = DepthLayer;

            if (!grid.BehindBuilding(view, horizontal - halfSize.x, horizontal + halfSize.x,
                vertical - halfSize.y, vertical + halfSize.y, depthLayer))
                return;

            //stay in normal mode if the player's body would be inside a block on their own layer.
            if (IsInsideBlockAtDepth(depthLayer))
                return;

            isBehindGeometry = true;
            depth = depthLayer;

            UpdateGrounded();
        }

        /// <summary>
        /// Goes back to normal flattened movement as soon as nothing is in front of the player.
        /// </summary>
        private void TryLeaveBackgroundMode()
        {
            if (ShouldBeBehindGeometry(DepthLayer))
                return;

            isBehindGeometry = false;

            UpdateGrounded();

            if (isGrounded)
                UpdateDepthFromGround(nearestToCamera: false);
        }

        /// <summary>
        /// After a rotation the player's depth is usually not a whole number.
        /// This picks the layer of the platform they are standing on: under their middle first, then their two sides.
        /// </summary>
        private int FindStandingDepthLayer()
        {
            float leftEdge = horizontal - halfSize.x;
            float rightEdge = horizontal + halfSize.x;
            float probeHeight = vertical - halfSize.y - groundProbeDistance;

            int middleLayer = BlockGrid.CellIndex(depth);
            int nearLayer = BlockGrid.CellIndex(depth - halfSize.x + BlockGrid.EdgeTolerance);
            int farLayer = BlockGrid.CellIndex(depth + halfSize.x - BlockGrid.EdgeTolerance);

            if (grid.BlockedAtDepth(view, leftEdge, rightEdge, probeHeight, probeHeight, middleLayer))
                return middleLayer;

            if (grid.BlockedAtDepth(view, leftEdge, rightEdge, probeHeight, probeHeight, nearLayer))
                return nearLayer;

            if (grid.BlockedAtDepth(view, leftEdge, rightEdge, probeHeight, probeHeight, farLayer))
                return farLayer;

            return middleLayer;
        }

        /// <summary>
        /// Picks the closest depth layer where the player's body is not inside a block.
        /// </summary>
        private int FindFreeDepthLayer(int preferredLayer)
        {
            foreach (int offset in LayerOffsets)
            {
                if (!IsInsideBlockAtDepth(preferredLayer + offset))
                    return preferredLayer + offset;
            }

            return preferredLayer;
        }

        private bool IsInsideBlockAtDepth(int depthLayer)
        {
            return grid.BlockedAtDepth(view, horizontal - halfSize.x, horizontal + halfSize.x,
                vertical - halfSize.y, vertical + halfSize.y, depthLayer);
        }

        /// <summary>
        /// Works out where a world position ends up on the screen when seen from the given view.
        /// If that spot is inside a solid screen cell, it tries one and then two columns to either side.
        /// </summary>
        /// <returns>False if every column that was tried is solid.</returns>
        bool FindPlacement(int newView, Vector3 world, out float newHorizontal, out float newDepth)
        {
            float projectedHorizontal = Vector3.Dot(world, (Vector3) ViewAxes.Right(newView));
            newDepth = Vector3.Dot(world, (Vector3) ViewAxes.Forward(newView));

            foreach (int offset in ColumnOffsets)
            {
                newHorizontal = offset == 0 ? projectedHorizontal : Mathf.Round(projectedHorizontal) + offset;
                if (!grid.Blocked(newView, newHorizontal - halfSize.x, newHorizontal + halfSize.x,
                    world.y - halfSize.y, world.y + halfSize.y))
                {
                    return true;
                }
            }
            newHorizontal = 0; newDepth = 0; return false;
        }

        /// <summary>
        /// Moves the player to the depth of the block they are standing on.
        /// This is what lets the player walk across blocks that only line up from the current view.
        /// </summary>
        void UpdateDepthFromGround(bool nearestToCamera)
        {
            int row = BlockGrid.CellIndex(vertical - halfSize.y - groundProbeDistance);
            int currentDepth = Mathf.RoundToInt(depth);

            //look under the middle of the player first, then under the left edge, then under the right edge.
            int middleColumn = BlockGrid.CellIndex(horizontal);
            int leftColumn = BlockGrid.CellIndex(horizontal - halfSize.x + BlockGrid.EdgeTolerance);
            int rightColumn = BlockGrid.CellIndex(horizontal + halfSize.x - BlockGrid.EdgeTolerance);

            if (grid.TryGetDepth(view, middleColumn, row, nearestToCamera, currentDepth, out int foundDepth)
                || grid.TryGetDepth(view, leftColumn, row, nearestToCamera, currentDepth, out foundDepth)
                || grid.TryGetDepth(view, rightColumn, row, nearestToCamera, currentDepth, out foundDepth))
            {
                depth = foundDepth;
            }
        }

        /*
        IEnumerator JumpTime(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if(!collisionCheck.IsGrounded)
            {
                OnJumpReleased();
            }
        }
        */

        #region Properties
        public float MoveSpeed
        {
            get { return moveSpeed; }
            set { moveSpeed = value; }
        }

        public float JumpForce
        {
            get { return jumpForce; }
            set { jumpForce = value; }
        }

        public bool IsJumpPressed
        {
            get { return isJumpPressed; }
            set { isJumpPressed = value; }
        }

        public bool IsJumpReleased
        {
            get { return isJumpReleased; }
            set { isJumpReleased = value; }
        }

        public bool IsDashing
        {
            get { return isDashing; }
            set { isDashing = value; }
        }

        public bool IsGliding
        {
            get { return isGliding; }
            set { isGliding = value; }
        }

        public bool IsGrounded
        {
            get { return isGrounded; }
        }

        /// <summary>
        /// True while the player is behind geometry after a rotation (background mode).
        /// </summary>
        public bool IsBehindGeometry
        {
            get { return isBehindGeometry; }
        }

        /// <summary>
        /// True while in background mode and some part of the player's body can't be seen by the camera.
        /// The player can still be in background mode with their whole body in view, for example while
        /// standing on a ledge that is hidden behind a building, so they don't fall through it.
        /// </summary>
        public bool IsBodyHidden
        {
            get
            {
                if (!isBehindGeometry)
                    return false;

                return grid.HiddenFromCamera(view, horizontal - halfSize.x, horizontal + halfSize.x,
                    vertical - halfSize.y, vertical + halfSize.y, DepthLayer);
            }
        }

        public float LastMoveSign
        {
            get { return lastMoveSign; }
        }

        /// <summary>
        /// Speed along the screen's left/right direction. Positive is toward the right side of the screen.
        /// While IsDashing is true the move input leaves this alone, so a dash can set it.
        /// </summary>
        public float HorizontalVelocity
        {
            get { return velocity.x; }
            set { velocity.x = value; }
        }

        /// <summary>
        /// Speed going up (positive) or down (negative).
        /// </summary>
        public float VerticalVelocity
        {
            get { return velocity.y; }
            set { velocity.y = value; }
        }

        /// <summary>
        /// The player's velocity as a world direction. Use this instead of the Rigidbody's velocity,
        /// which is always zero now that the Rigidbody is kinematic.
        /// </summary>
        public Vector3 WorldVelocity
        {
            get { return GetMoveAxis() * velocity.x + Vector3.up * velocity.y; }
        }
        #endregion

    }
}
