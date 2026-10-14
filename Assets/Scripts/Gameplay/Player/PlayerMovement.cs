using Data.Audio;
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
        //up/down on the move stick or W/S. Only used for dropping through one-way blocks.
        private float verticalMoveInput;
        [Tooltip("How far down the player has to hold for Down + Jump to drop through a one-way block.")]
        [SerializeField] private float dropInputThreshold = 0.5f;
        //while dropping, the one-way block the player was standing on (and any at that height) is ignored.
        private bool isDroppingThrough;
        private float dropThroughHeight;
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
        private bool isBehindGeometry;
        private bool isWaitingToSnapDepth;

        //sound
        [Header("Sounds")]
        [SerializeField] private SoundEffect jumpSound;
        [SerializeField] private SoundEffect landSound;
        [Tooltip("The landing sound only plays when falling at least this fast, so stepping off a tiny ledge stays quiet.")]
        [SerializeField] private float landSoundMinimumFallSpeed = 5f;
        [SerializeField] private SoundEffect footstepSound;
        [Tooltip("Seconds between footstep sounds while walking.")]
        [SerializeField] private float footstepInterval = 0.3f;
        [SerializeField] private SoundEffect dropThroughSound;
        [Tooltip("The player's sounds are muffled while they are behind a building (in background mode).")]
        [SerializeField] private bool muffleSoundsBehindBuildings = true;
        [SerializeField] private float behindBuildingMuffleCutoff = 1200f;
        private float footstepTimer;
        private bool wasGroundedLastFrame;

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
            //sounds in the world (Sound Effect Emitters) are heard from the player's position.
            AudioManager.Listener = transform;

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
            wasGroundedLastFrame = isGrounded;

            ApplyTransform();
        }

        void OnDestroy()
        {
            //don't leave the AudioManager listening from a player that no longer exists.
            if (AudioManager.Listener == transform)
                AudioManager.Listener = null;
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

            //Down + Jump on a one-way block drops through it instead of jumping.
            if (isGrounded && jumpBufferTimer > 0f && !isDashing
                && verticalMoveInput < -dropInputThreshold && IsStandingOnlyOnOneWay())
            {
                StartDropThrough();
            }

            if (isGrounded && jumpBufferTimer > 0f && !isDashing)
            {
                Jump(jumpForce);
                isJumpPressed = false;
            }

            float horizontalBeforeMoving = horizontal;
            float verticalBeforeMoving = vertical;
            //saved before moving, because landing sets the falling speed back to 0.
            float fallSpeedBeforeMoving = -velocity.y;

            Walk(velocity.x * deltaTime);
            MoveVertical(velocity.y * deltaTime);

            UpdateGrounded();

            //landed this frame after falling fast enough.
            if (isGrounded && !wasGroundedLastFrame && fallSpeedBeforeMoving >= landSoundMinimumFallSpeed)
            {
                PlayPlayerSound(landSound);
            }
            wasGroundedLastFrame = isGrounded;

            bool isWalking = isGrounded && !isDashing && Mathf.Abs(horizontal - horizontalBeforeMoving) > 0.001f;
            UpdateFootsteps(deltaTime, isWalking);

            bool movedThisFrame = horizontal != horizontalBeforeMoving || vertical != verticalBeforeMoving;

            if (isBehindGeometry)
            {
                //depth stays fixed while behind, until nothing is in front of the player any more.
                TryLeaveBackgroundMode();
            }
            else
            {
                if (isWaitingToSnapDepth)
                {
                    //the first step (or landing) after a rotation pulls the player to the depth nearest the camera.
                    if (movedThisFrame && isGrounded)
                    {
                        isWaitingToSnapDepth = false;
                        UpdateDepthFromGround(nearestToCamera: true);
                    }
                }
                else if (isGrounded)
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

        /// <summary>
        /// Up (positive) or down (negative) on the move input. Holding down while pressing Jump drops through one-way blocks.
        /// </summary>
        public void SetVerticalMoveInput(float input)
        {
            verticalMoveInput = Mathf.Clamp(input, -1f, 1f);
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
            float feetBeforeMoving = vertical - halfSize.y;

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

            //one-way blocks: only stop the player when falling onto their top from above.
            if (distance < 0 && LandsOnOneWay(horizontal - halfSize.x, horizontal + halfSize.x,
                feetBeforeMoving, newVertical - halfSize.y, out float oneWayTop))
            {
                float landedVertical = oneWayTop + halfSize.y;

                //keep whichever landing is higher, in case a solid block was also reached this frame.
                if (landedVertical > newVertical)
                    newVertical = landedVertical;

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

                PlayPlayerSound(jumpSound);
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
            isWaitingToSnapDepth = false;
            isDroppingThrough = false;

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
            //a teleport is not a landing, so no landing sound.
            wasGroundedLastFrame = isGrounded;

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
            isDroppingThrough = false;

            int depthLayer = FindFreeDepthLayer(FindStandingDepthLayer());

            //boxed in on every nearby layer (rare, e.g. rotating while falling past a floor):
            //use the old sideways nudge in the flattened view instead.
            if (IsInsideBlockAtDepth(depthLayer) && FindPlacement(view, world, out float nudgedHorizontal, out float nudgedDepth))
            {
                horizontal = nudgedHorizontal;
                depth = nudgedDepth;
                isBehindGeometry = false;
                isWaitingToSnapDepth = true;
            }
            else if (ShouldBeBehindGeometry(depthLayer))
            {
                //keep the player where they really are until nothing is in front of them.
                isBehindGeometry = true;
                isWaitingToSnapDepth = false;
                depth = depthLayer;
            }
            else
            {
                //the player stays exactly where they are; Update pulls them to the nearest depth once they move.
                isBehindGeometry = false;
                isWaitingToSnapDepth = true;
            }

            UpdateGrounded();

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
                probeHeight, probeHeight)
                || LandsOnOneWay(horizontal - halfSize.x, horizontal + halfSize.x, feetHeight, probeHeight, out _);

            //moving upward never counts as grounded, otherwise a jump would be cancelled on its first frame.
            isGrounded = velocity.y <= 0 && groundIsBelow;

            //the drop is over once the player lands on something or has fallen a full block below where it started.
            if (isDroppingThrough && (isGrounded || feetHeight < dropThroughHeight - 1f))
                isDroppingThrough = false;
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
        /// One-way blocks: did the feet cross the top of one while moving down from previousFeet to newFeet?
        /// Uses the flattened view normally, and only the player's own depth layer in background mode.
        /// </summary>
        private bool LandsOnOneWay(float minHorizontal, float maxHorizontal, float previousFeet, float newFeet, out float landingHeight)
        {
            //while dropping through, pretend the feet started half a block below the block that was dropped through,
            //so its top (and any other one-way top at that height) can't catch the player again.
            if (isDroppingThrough)
                previousFeet = Mathf.Min(previousFeet, dropThroughHeight - 0.5f);

            if (isBehindGeometry)
                return grid.LandsOnOneWayAtDepth(view, minHorizontal, maxHorizontal, previousFeet, newFeet, DepthLayer, out landingHeight);

            return grid.LandsOnOneWay(view, minHorizontal, maxHorizontal, previousFeet, newFeet, out landingHeight);
        }

        /// <summary>
        /// True when the only thing under the player's feet is the top of a one-way block (no solid block).
        /// </summary>
        private bool IsStandingOnlyOnOneWay()
        {
            float leftEdge = horizontal - halfSize.x;
            float rightEdge = horizontal + halfSize.x;
            float feetHeight = vertical - halfSize.y;
            float probeHeight = feetHeight - groundProbeDistance;

            if (IsBlocked(leftEdge, rightEdge, probeHeight, probeHeight))
                return false;

            return LandsOnOneWay(leftEdge, rightEdge, feetHeight, probeHeight, out _);
        }

        /// <summary>
        /// Starts falling through the one-way block under the player. Gravity takes it from here.
        /// </summary>
        private void StartDropThrough()
        {
            isDroppingThrough = true;
            dropThroughHeight = vertical - halfSize.y;

            //use up the jump press so it doesn't also jump.
            jumpBufferTimer = 0f;
            isJumpPressed = false;

            isGrounded = false;
            collisionCheck.IsGrounded = false;

            PlayPlayerSound(dropThroughSound);
        }

        /// <summary>
        /// Plays one of the player's own sounds. These are always at full volume (the player is the listener),
        /// but they are muffled while the player is behind a building.
        /// The ability scripts use this too, so all player sounds get the same muffling.
        /// </summary>
        public void PlayPlayerSound(SoundEffect sound)
        {
            if (sound == null)
                return;

            if (muffleSoundsBehindBuildings && isBehindGeometry)
                AudioManager.PlaySoundEffect(sound, SoundPlayOptions.Muffled(behindBuildingMuffleCutoff));
            else
                AudioManager.PlaySoundEffect(sound);
        }

        /// <summary>
        /// Plays a footstep every footstepInterval seconds while walking. The first step plays right away.
        /// </summary>
        private void UpdateFootsteps(float deltaTime, bool isWalking)
        {
            if (!isWalking)
            {
                footstepTimer = 0f;
                return;
            }

            footstepTimer -= deltaTime;

            if (footstepTimer <= 0f)
            {
                PlayPlayerSound(footstepSound);
                footstepTimer = footstepInterval;
            }
        }

        /// <summary>
        /// Is there ground (a solid block, or the top of a one-way block) just under the feet on this depth layer?
        /// </summary>
        private bool HasGroundAtDepth(float leftEdge, float rightEdge, float feetHeight, int depthLayer)
        {
            float probeHeight = feetHeight - groundProbeDistance;

            return grid.BlockedAtDepth(view, leftEdge, rightEdge, probeHeight, probeHeight, depthLayer)
                || grid.LandsOnOneWayAtDepth(view, leftEdge, rightEdge, feetHeight, probeHeight, depthLayer, out _);
        }

        /// <summary>
        /// Is there ground the camera can see just under the feet, in the flattened view?
        /// </summary>
        private bool HasVisibleGround(float leftEdge, float rightEdge, float feetHeight)
        {
            float probeHeight = feetHeight - groundProbeDistance;

            return grid.Blocked(view, leftEdge, rightEdge, probeHeight, probeHeight)
                || grid.LandsOnOneWay(view, leftEdge, rightEdge, feetHeight, probeHeight, out _);
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

            //a building is between part of the player and the camera.
            if (grid.BehindBuilding(view, leftEdge, rightEdge, feetHeight, headHeight, depthLayer))
                return true;

            //the player is inside a solid cell of the flattened view, so they can't come out here yet.
            if (grid.Blocked(view, leftEdge, rightEdge, feetHeight, headHeight))
                return true;

            //the player is standing on a platform on their own layer that the camera can't see.
            bool standingOnOwnLayer = HasGroundAtDepth(leftEdge, rightEdge, feetHeight, depthLayer);
            bool visibleGroundBelow = HasVisibleGround(leftEdge, rightEdge, feetHeight);

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
            isWaitingToSnapDepth = false;
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
            float feetHeight = vertical - halfSize.y;

            int middleLayer = BlockGrid.CellIndex(depth);
            int nearLayer = BlockGrid.CellIndex(depth - halfSize.x + BlockGrid.EdgeTolerance);
            int farLayer = BlockGrid.CellIndex(depth + halfSize.x - BlockGrid.EdgeTolerance);

            if (HasGroundAtDepth(leftEdge, rightEdge, feetHeight, middleLayer))
                return middleLayer;

            if (HasGroundAtDepth(leftEdge, rightEdge, feetHeight, nearLayer))
                return nearLayer;

            if (HasGroundAtDepth(leftEdge, rightEdge, feetHeight, farLayer))
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
            //some scenes turn depth pulling off on their BlockGrid.
            if (!grid.AllowDepthPulling)
                return;

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
        /// Which of the four views the camera is looking from (0, 1, 2 or 3). Used with ViewAxes.
        /// </summary>
        public int CurrentView
        {
            get { return view; }
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
