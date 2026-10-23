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

        //ledge grabbing: hanging from the top edge of a platform while in the air, then climbing up onto it.
        [Header("Ledge Grab")]
        [Tooltip("Lets the player grab the top edge of a platform while falling past it, then climb up onto it.")]
        [SerializeField] private bool allowLedgeGrab = true;
        [Tooltip("On: the player has to hold toward the wall to grab its ledge. Off: any ledge the player's hands pass is grabbed.")]
        [SerializeField] private bool holdTowardWallToGrab = true;
        [Tooltip("How far below a ledge's top the top of the player can already be and still grab it. Bigger is more forgiving.")]
        [SerializeField] private float ledgeGrabTolerance = 0.25f;
        [Tooltip("How far away from the wall the player can be and still grab its ledge.")]
        [SerializeField] private float ledgeGrabReach = 0.15f;
        [Tooltip("How far below the ledge's top the top of the player sits while hanging.")]
        [SerializeField] private float hangHeightBelowLedge = 0.1f;
        [Tooltip("Seconds the climb up onto the ledge takes. 0 is instant.")]
        [SerializeField] private float climbDuration = 0.3f;
        [Tooltip("Seconds after letting go before the player can grab a ledge again, so they don't grab the same one straight away.")]
        [SerializeField] private float regrabDelay = 0.25f;
        [Tooltip("On: pressing Up also climbs. Off: only Jump climbs.")]
        [SerializeField] private bool climbWithUp = true;
        [SerializeField] private SoundEffect ledgeGrabSound;
        [SerializeField] private SoundEffect ledgeClimbSound;

        //climbable blocks (ladders and vines). A separate system from ledge grabbing.
        [Header("Climbing (Ladders and Vines)")]
        [Tooltip("Lets the player grab blocks marked with ClimbableBlock by pressing Up, and climb them.")]
        [SerializeField] private bool allowClimbing = true;
        [Tooltip("Speed climbing up and down, in units per second.")]
        [SerializeField] private float climbingSpeed = 3f;
        [Tooltip("Speed moving left and right while climbing, in units per second.")]
        [SerializeField] private float climbingSidewaysSpeed = 2.5f;
        [Tooltip("Jumping off a climbable block uses the normal jump times this.")]
        [SerializeField] private float climbingJumpMultiplier = 0.8f;
        [Tooltip("Moves the player to the middle of the ladder's column when they grab it, so they line up with the rungs.")]
        [SerializeField] private bool snapToClimbableCenter = true;
        [Tooltip("Seconds after jumping off before the player can grab a climbable block again.")]
        [SerializeField] private float climbingRegrabDelay = 0.2f;
        [SerializeField] private SoundEffect climbingGrabSound;
        [Tooltip("Played every Climbing Step Interval seconds while the player moves on a climbable block.")]
        [SerializeField] private SoundEffect climbingStepSound;
        [SerializeField] private float climbingStepInterval = 0.25f;

        private bool isOnClimbable;
        //true from jumping off a climbable block until the jump starts falling, so holding Up during
        //the jump doesn't grab the same ladder again on the way up.
        private bool jumpedOffClimbable;
        private float climbingRegrabTimer;
        private float climbingStepTimer;

        private enum LedgeState
        {
            None,
            Hanging,
            Climbing
        }

        private LedgeState ledgeState = LedgeState.None;
        //which side the wall is on: 1 = right side of the screen, -1 = left side.
        private int ledgeSide;
        //the screen cell of the block being held.
        private int ledgeColumn;
        private int ledgeRow;
        //true when hanging from underneath a one-way platform instead of beside a block's edge.
        private bool isHangingUnderLedge;
        private float regrabTimer;
        private float climbTimer;
        private Vector2 climbStart;   //x = horizontal, y = vertical
        private Vector2 climbEnd;
        //inputs held on the last hanging frame, so holding a direction from before the grab doesn't count as a new press.
        private bool upWasHeld;
        private bool downWasHeld;
        private bool awayWasHeld;
        //how far to the side the player ends up past the ledge's edge after climbing.
        private const float ClimbInset = 0.05f;
        //how far a direction has to be held (0 to 1) to count as pressed while on a ledge.
        private const float LedgeInputThreshold = 0.5f;

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

            //hanging from or climbing up a ledge replaces normal movement until it is over.
            if (ledgeState != LedgeState.None)
            {
                UpdateLedge(deltaTime);
                return;
            }

            //on a ladder or vines: climbing replaces normal movement until the player gets off.
            if (isOnClimbable)
            {
                UpdateClimbing(deltaTime);
                return;
            }

            regrabTimer -= deltaTime;
            climbingRegrabTimer -= deltaTime;

            //Up next to a climbable block (or Down while standing on top of a ladder) grabs it.
            if (TryStartClimbing())
            {
                ApplyTransform();
                return;
            }

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

            //falling past a ledge while next to it grabs it.
            if (TryGrabLedge(verticalBeforeMoving + halfSize.y, vertical + halfSize.y))
            {
                ApplyTransform();
                return;
            }

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
                    int blockedColumn = BlockGrid.CellIndex(newHorizontal + halfSize.x - BlockGrid.EDGE_TOLERANCE);
                    newHorizontal = blockedColumn - 0.5f - halfSize.x;
                }
                else
                {
                    //ditto for left edge.
                    int blockedColumn = BlockGrid.CellIndex(newHorizontal - halfSize.x + BlockGrid.EDGE_TOLERANCE);
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
        /// <param name="canLandOnOneWay">False while climbing, so the player can climb down through the top of a ladder.</param>
        private void MoveVertical(float distance, bool canLandOnOneWay = true)
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
                    
                    int blockedRow = BlockGrid.CellIndex(newVertical + halfSize.y - BlockGrid.EDGE_TOLERANCE);
                    newVertical = blockedRow - 0.5f - halfSize.y;
                }
                //feet went into a solid row
                else
                {
                    
                    int blockedRow = BlockGrid.CellIndex(newVertical - halfSize.y + BlockGrid.EDGE_TOLERANCE);
                    newVertical = blockedRow + 0.5f + halfSize.y;
                }

                velocity.y = 0;
            }

            //one-way blocks: only stop the player when falling onto their top from above.
            if (canLandOnOneWay && distance < 0 && LandsOnOneWay(horizontal - halfSize.x, horizontal + halfSize.x,
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

            //letting go of Jump while on a ledge shouldn't push the player down.
            if (ledgeState != LedgeState.None || isOnClimbable)
                return;

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
            ledgeState = LedgeState.None;
            isOnClimbable = false;

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
            if (ledgeState == LedgeState.Climbing)
            {
                horizontal = climbEnd.x;
                vertical = climbEnd.y;
                ledgeState = LedgeState.None;
            }

            bool wasHanging = ledgeState == LedgeState.Hanging;
            ledgeState = LedgeState.None;

            bool wasOnClimbable = isOnClimbable;
            isOnClimbable = false;
            Vector3 world = WorldPosition();

            view = newView;
            horizontal = Vector3.Dot(world, (Vector3)ViewAxes.Right(view));
            depth = Vector3.Dot(world, (Vector3)ViewAxes.Forward(view));
            velocity = Vector2.zero;
            isDroppingThrough = false;

            int depthLayer = FindFreeDepthLayer(FindStandingDepthLayer());
            if (IsInsideBlockAtDepth(depthLayer) && FindPlacement(view, world, out float nudgedHorizontal, out float nudgedDepth))
            {
                horizontal = nudgedHorizontal;
                depth = nudgedDepth;
                isBehindGeometry = false;
                isWaitingToSnapDepth = true;
            }
            else if (ShouldBeBehindGeometry(depthLayer))
            {
                isBehindGeometry = true;
                isWaitingToSnapDepth = false;
                depth = depthLayer;
            }
            else
            {
                isBehindGeometry = false;
                isWaitingToSnapDepth = true;
            }

            UpdateGrounded();


            //on a ladder or vines: keep climbing if there is still something to climb in the new view.
            if (wasOnClimbable)
            {
                if (FindTouchedClimbable(out int climbableColumn, out int climbableRow))
                    StartClimbing(climbableColumn, climbableRow, false);
                else
                    StopClimbing();
            }

            if (wasHanging && !TryRegrabAfterRotation())
            {
                ReleaseLedge();
            }

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

        #region Climbing (ladders and vines)

        /// <summary>
        /// Is this screen cell climbable? Uses the flattened view, or only the player's own layer in background mode.
        /// </summary>
        private bool IsClimbableCell(int column, int row)
        {
            if (isBehindGeometry)
                return grid.IsClimbableAtDepth(view, column, row, DepthLayer);

            return grid.IsClimbable(view, column, row);
        }

        /// <summary>
        /// Is the player touching a climbable block? Checks the column under the player's middle,
        /// at their middle first and then at their feet.
        /// </summary>
        private bool FindTouchedClimbable(out int column, out int row)
        {
            column = BlockGrid.CellIndex(horizontal);

            row = BlockGrid.CellIndex(vertical);
            if (IsClimbableCell(column, row))
                return true;

            row = BlockGrid.CellIndex(vertical - halfSize.y + 0.01f);
            return IsClimbableCell(column, row);
        }

        /// <summary>
        /// Grabs a climbable block when the player holds Up while touching one,
        /// or holds Down while standing on top of a ladder.
        /// </summary>
        private bool TryStartClimbing()
        {
            if (velocity.y <= 0f || isGrounded)
                jumpedOffClimbable = false;

            if (!allowClimbing || climbingRegrabTimer > 0f || isDashing || jumpedOffClimbable)
                return false;

            bool upHeld = verticalMoveInput > LedgeInputThreshold;
            bool downHeld = verticalMoveInput < -LedgeInputThreshold;

            if (upHeld && FindTouchedClimbable(out int column, out int row))
            {
                StartClimbing(column, row, true);
                return true;
            }

            if (downHeld && isGrounded)
            {
                //the cell just under the feet: standing on the top of a ladder.
                int belowColumn = BlockGrid.CellIndex(horizontal);
                int belowRow = BlockGrid.CellIndex(vertical - halfSize.y - 0.01f);

                if (IsClimbableCell(belowColumn, belowRow))
                {
                    StartClimbing(belowColumn, belowRow, true);
                    return true;
                }
            }

            return false;
        }

        private void StartClimbing(int column, int row, bool playSound)
        {
            isOnClimbable = true;
            isDroppingThrough = false;
            velocity = Vector2.zero;
            isGrounded = false;
            //the ability scripts treat climbing like standing: no air jump or glide, and air uses are refilled.
            collisionCheck.IsGrounded = true;
            jumpBufferTimer = 0f;
            isJumpPressed = false;
            climbingStepTimer = 0f;

            //line up with the middle of the ladder, if the player fits there.
            if (snapToClimbableCenter && !IsBlocked(column - halfSize.x, column + halfSize.x,
                vertical - halfSize.y, vertical + halfSize.y))
            {
                horizontal = column;
            }

            //move to the depth of the climbable block, so the player is drawn on it (in front of the wall it covers).
            if (!isBehindGeometry && grid.AllowDepthPulling
                && grid.TryGetClimbableDepth(view, column, row, Mathf.RoundToInt(depth), out int climbableDepth))
            {
                depth = climbableDepth;
            }
            isWaitingToSnapDepth = false;

            if (playSound)
                PlayPlayerSound(climbingGrabSound);
        }

        /// <summary>
        /// Lets go of the climbable block. Gravity takes over (or the player is standing, if there is ground).
        /// </summary>
        private void StopClimbing()
        {
            isOnClimbable = false;
            velocity = Vector2.zero;
            UpdateGrounded();
            //getting off a ladder is not a landing, so no landing sound.
            wasGroundedLastFrame = isGrounded;
        }

        /// <summary>
        /// Runs every frame instead of normal movement while on a ladder or vines.
        /// </summary>
        private void UpdateClimbing(float deltaTime)
        {
            velocity = Vector2.zero;
            collisionCheck.IsGrounded = true;

            //a dash lets go, and the dash carries on next frame.
            if (isDashing)
            {
                StopClimbing();
                ApplyTransform();
                return;
            }

            //jumping off.
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer = 0f;
                isOnClimbable = false;
                jumpedOffClimbable = true;
                climbingRegrabTimer = climbingRegrabDelay;
                isGrounded = false;
                collisionCheck.IsGrounded = false;
                velocity.y = jumpForce * climbingJumpMultiplier;
                PlayPlayerSound(jumpSound);
                ApplyTransform();
                return;
            }

            float horizontalBeforeMoving = horizontal;
            float verticalBeforeMoving = vertical;

            //small stick movements are ignored so the player doesn't drift.
            float sidewaysInput = Mathf.Abs(moveInput) > 0.2f ? moveInput : 0f;
            float upDownInput = Mathf.Abs(verticalMoveInput) > 0.2f ? verticalMoveInput : 0f;

            Walk(sidewaysInput * climbingSidewaysSpeed * deltaTime);
            MoveVertical(upDownInput * climbingSpeed * deltaTime, false);

            if (!FindTouchedClimbable(out int column, out int row))
            {
                if (upDownInput > 0f && TryStandOnClimbableTop())
                {
                    //climbed off the top of a ladder: standing on it now.
                }
                else
                {
                    //climbed off the side or the bottom: let go.
                    StopClimbing();
                }

                ApplyTransform();
                return;
            }

            //climbed down onto the floor (or a platform): get off.
            if (upDownInput < 0f && IsOnGroundWhileClimbing())
            {
                StopClimbing();
                ApplyTransform();
                return;
            }

            //keep the player drawn on the climbable block they are on (vines can be at different depths).
            if (!isBehindGeometry && grid.AllowDepthPulling
                && grid.TryGetClimbableDepth(view, column, row, Mathf.RoundToInt(depth), out int climbableDepth))
            {
                depth = climbableDepth;
            }

            bool movedThisFrame = horizontal != horizontalBeforeMoving || vertical != verticalBeforeMoving;
            UpdateClimbingSteps(deltaTime, movedThisFrame);

            //facing follows sideways movement while climbing.
            if (sidewaysInput != 0f)
                lastMoveSign = Mathf.Sign(sidewaysInput);

            ApplyTransform();
        }

        /// <summary>
        /// The player's feet just went above the top of the ladder. Puts them standing on its top.
        /// </summary>
        private bool TryStandOnClimbableTop()
        {
            int column = BlockGrid.CellIndex(horizontal);
            float feetHeight = vertical - halfSize.y;

            //the top climbable cell is the one just under the feet (or one more below after a long frame).
            int topRow = BlockGrid.CellIndex(feetHeight - 0.01f);

            for (int row = topRow; row >= topRow - 1; row--)
            {
                if (!IsClimbableCell(column, row) || IsClimbableCell(column, row + 1))
                    continue;

                float standingVertical = row + 0.5f + halfSize.y;

                if (IsBlocked(horizontal - halfSize.x, horizontal + halfSize.x,
                    standingVertical - halfSize.y, standingVertical + halfSize.y))
                    return false;

                vertical = standingVertical;
                StopClimbing();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Is there ground right under the player's feet (a solid block, a one-way block, or another ladder's top)?
        /// </summary>
        private bool IsOnGroundWhileClimbing()
        {
            float feetHeight = vertical - halfSize.y;
            float probeHeight = feetHeight - groundProbeDistance;

            return IsBlocked(horizontal - halfSize.x, horizontal + halfSize.x, probeHeight, probeHeight)
                || LandsOnOneWay(horizontal - halfSize.x, horizontal + halfSize.x, feetHeight, probeHeight, out _);
        }

        private void UpdateClimbingSteps(float deltaTime, bool isMoving)
        {
            if (!isMoving)
            {
                climbingStepTimer = 0f;
                return;
            }

            climbingStepTimer -= deltaTime;

            if (climbingStepTimer <= 0f)
            {
                PlayPlayerSound(climbingStepSound);
                climbingStepTimer = climbingStepInterval;
            }
        }

        #endregion

        #region Ledge grab

        /// <summary>
        /// Looks for a ledge to grab after this frame's fall. The top of the player is their "hands".
        /// </summary>
        /// <param name="handsBefore">Height of the top of the player before this frame's move.</param>
        /// <param name="handsAfter">Height of the top of the player after this frame's move.</param>
        private bool TryGrabLedge(float handsBefore, float handsAfter)
        {
            if (!allowLedgeGrab || regrabTimer > 0f || isGrounded || isDashing || velocity.y > 0f)
                return false;

            float lowestTop = handsAfter;
            float highestTop = handsBefore + ledgeGrabTolerance;

            if (holdTowardWallToGrab)
            {
                if (Mathf.Abs(moveInput) < LedgeInputThreshold)
                    return false;

                int heldSide = moveInput > 0f ? 1 : -1;
                return TryGrabLedgeOnSide(heldSide, lowestTop, highestTop, ledgeGrabReach);
            }

            int facingSide = lastMoveSign > 0f ? 1 : -1;
            return TryGrabLedgeOnSide(facingSide, lowestTop, highestTop, ledgeGrabReach)
                || TryGrabLedgeOnSide(-facingSide, lowestTop, highestTop, ledgeGrabReach);
        }

        /// <summary>
        /// After a rotation, looks for a ledge right where the player's hands already are.
        /// </summary>
        private bool TryRegrabAfterRotation()
        {
            float ledgeTop = vertical + halfSize.y + hangHeightBelowLedge;
            int facingSide = lastMoveSign > 0f ? 1 : -1;
            float reach = 0.5f;

            return TryGrabLedgeOnSide(facingSide, ledgeTop - 0.05f, ledgeTop + 0.05f, reach)
                || TryGrabLedgeOnSide(-facingSide, ledgeTop - 0.05f, ledgeTop + 0.05f, reach);
        }

        private bool TryGrabLedgeOnSide(int side, float lowestTop, float highestTop, float reach)
        {
            //the edge of a block beside the player (solid, or the end of a one-way platform).
            if (FindLedge(side, lowestTop, highestTop, reach, out int column, out int row))
            {
                StartHanging(side, column, row, false);
                return true;
            }

            //one-way platforms don't stop the player sideways, so a jump toward one often carries the player
            //underneath it. If the hands reach its top there, they hang right where they are.
            if (FindOneWayOverhead(lowestTop, highestTop, out column, out row))
            {
                StartHanging(side, column, row, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Looks for a one-way block right above the player (overlapping them left to right) whose top is between
        /// lowestTop and highestTop, with nothing on top of it.
        /// </summary>
        private bool FindOneWayOverhead(float lowestTop, float highestTop, out int column, out int row)
        {
            row = 0;
            column = 0;

            //the column under the player's middle first, then the columns under their two edges.
            int middleColumn = BlockGrid.CellIndex(horizontal);
            int leftColumn = BlockGrid.CellIndex(horizontal - halfSize.x + BlockGrid.EDGE_TOLERANCE);
            int rightColumn = BlockGrid.CellIndex(horizontal + halfSize.x - BlockGrid.EDGE_TOLERANCE);
            int[] columnsToCheck = { middleColumn, leftColumn, rightColumn };

            for (int candidateRow = Mathf.FloorToInt(highestTop - 0.5f); candidateRow + 0.5f >= lowestTop; candidateRow--)
            {
                //the player hangs where they are, just below the top, and has to fit there.
                float hangVertical = candidateRow + 0.5f - hangHeightBelowLedge - halfSize.y;

                if (IsBlocked(horizontal - halfSize.x, horizontal + halfSize.x,
                    hangVertical - halfSize.y, hangVertical + halfSize.y))
                    continue;

                foreach (int candidateColumn in columnsToCheck)
                {
                    if (IsOneWayTop(candidateColumn, candidateRow) && IsCellOpen(candidateColumn, candidateRow + 1))
                    {
                        column = candidateColumn;
                        row = candidateRow;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Finds a ledge next to the player on one side, whose top is between lowestTop and highestTop.
        /// Picks the highest one. Solid blocks and one-way blocks both have ledges.
        /// </summary>
        private bool FindLedge(int side, float lowestTop, float highestTop, float reach, out int column, out int row)
        {
            float nearEdge = horizontal + side * halfSize.x;

            //the column the player can reach into on that side.
            column = BlockGrid.CellIndex(nearEdge + side * reach);
            if (FindLedgeInColumn(side, column, lowestTop, highestTop, reach, out row))
                return true;

            //one-way blocks don't stop the player sideways, so the player can already be partway inside one.
            //this checks the column their edge is in as well.
            int overlappedColumn = BlockGrid.CellIndex(nearEdge - side * 0.01f);
            if (overlappedColumn != column && FindLedgeInColumn(side, overlappedColumn, lowestTop, highestTop, reach, out row))
            {
                column = overlappedColumn;
                return true;
            }

            return false;
        }

        private bool FindLedgeInColumn(int side, int column, float lowestTop, float highestTop, float reach, out int row)
        {
            row = 0;

            float nearEdge = horizontal + side * halfSize.x;

            //how far the block's side is from the player's edge. Below 0 means the player is partway inside the column.
            float wallFace = column - side * 0.5f;
            float gap = (wallFace - nearEdge) * side;

            if (gap > reach)
                return false;

            //the player can never be inside a solid block, but can be up to their whole width inside a one-way block.
            bool canReachSolidLedge = gap >= -BlockGrid.EDGE_TOLERANCE * 10f;
            bool canReachOneWayLedge = gap >= -halfSize.x * 2f;

            if (!canReachOneWayLedge)
                return false;

            //tops of cells are at row + 0.5. Go from the highest possible row down.
            for (int candidateRow = Mathf.FloorToInt(highestTop - 0.5f); candidateRow + 0.5f >= lowestTop; candidateRow--)
            {
                bool isSolidLedge = IsCellBlocked(column, candidateRow);
                bool isOneWayLedge = !isSolidLedge && IsOneWayTop(column, candidateRow);

                if (!isSolidLedge && !isOneWayLedge)
                    continue;

                if (isSolidLedge && !canReachSolidLedge)
                    continue;

                //something on top means it isn't a ledge.
                if (!IsCellOpen(column, candidateRow + 1))
                    continue;

                //the cell beside it, where the player hangs, has to be empty too. Otherwise this is the middle of
                //a longer one-way platform (the player can be under one, since they pass through its sides), not its end.
                if (!IsCellOpen(column - side, candidateRow))
                    continue;

                //the player hangs beside the block, and has to fit there.
                float hangHorizontal = wallFace - side * halfSize.x;
                float hangVertical = candidateRow + 0.5f - hangHeightBelowLedge - halfSize.y;

                if (IsBlocked(hangHorizontal - halfSize.x, hangHorizontal + halfSize.x,
                    hangVertical - halfSize.y, hangVertical + halfSize.y))
                    continue;

                row = candidateRow;
                return true;
            }

            return false;
        }

        /// <summary>
        /// A ledge is a solid or one-way screen cell with nothing on top of it.
        /// </summary>
        private bool IsLedge(int column, int row)
        {
            bool hasBlock = IsCellBlocked(column, row) || IsOneWayTop(column, row);
            return hasBlock && IsCellOpen(column, row + 1);
        }

        /// <summary>
        /// Nothing in this screen cell: no solid block and no one-way block.
        /// </summary>
        private bool IsCellOpen(int column, int row)
        {
            return !IsCellBlocked(column, row) && !IsOneWayTop(column, row);
        }

        /// <summary>
        /// Is this one screen cell blocked? Uses the same rules as walking (flattened view, or own layer in background mode).
        /// </summary>
        private bool IsCellBlocked(int column, int row)
        {
            //a tiny bit inside the cell, so the cells next to it don't count.
            const float inset = 0.01f;
            return IsBlocked(column - 0.5f + inset, column + 0.5f - inset, row - 0.5f + inset, row + 0.5f - inset);
        }

        /// <summary>
        /// Does this screen cell have a one-way block in it? Uses the same rules as landing on one
        /// (flattened view, or own layer in background mode). The block the player just dropped through
        /// with Down + Jump doesn't count, so they can't grab it on the way down.
        /// </summary>
        private bool IsOneWayTop(int column, int row)
        {
            const float inset = 0.01f;
            float top = row + 0.5f;

            //asks "would feet moving from just above this cell's top to just below it land on a one-way block?"
            return LandsOnOneWay(column - 0.5f + inset, column + 0.5f - inset, top + inset, top - inset, out _);
        }

        /// <param name="hangUnder">True: hang where the player is, under a one-way platform. False: hang beside the block's edge.</param>
        private void StartHanging(int side, int column, int row, bool hangUnder)
        {
            ledgeState = LedgeState.Hanging;
            isHangingUnderLedge = hangUnder;
            //grabbing a ledge ends a Down + Jump drop, so the next one-way top below works normally again.
            isDroppingThrough = false;
            ledgeSide = side;
            ledgeColumn = column;
            ledgeRow = row;

            if (!hangUnder)
            {
                float wallFace = column - side * 0.5f;
                horizontal = wallFace - side * halfSize.x;
            }
            vertical = row + 0.5f - hangHeightBelowLedge - halfSize.y;

            velocity = Vector2.zero;
            isGrounded = false;
            collisionCheck.IsGrounded = true;

            jumpBufferTimer = 0f;
            isJumpPressed = false;

            upWasHeld = verticalMoveInput > LedgeInputThreshold;
            downWasHeld = verticalMoveInput < -LedgeInputThreshold;
            awayWasHeld = moveInput * side < -LedgeInputThreshold;

            lastMoveSign = side;

            if (!isBehindGeometry && grid.AllowDepthPulling
                && grid.TryGetDepth(view, column, row, false, Mathf.RoundToInt(depth), out int ledgeDepth))
            {
                depth = ledgeDepth;
            }
            isWaitingToSnapDepth = false;

            PlayPlayerSound(ledgeGrabSound);
        }

        /// <summary>
        /// Runs every frame instead of normal movement while hanging or climbing.
        /// </summary>
        private void UpdateLedge(float deltaTime)
        {
            if (ledgeState == LedgeState.Climbing)
            {
                UpdateClimb(deltaTime);
                ApplyTransform();
                return;
            }

            velocity = Vector2.zero;
            collisionCheck.IsGrounded = true;

            if (isDashing)
            {
                ReleaseLedge();
                ApplyTransform();
                return;
            }

            if (!IsLedge(ledgeColumn, ledgeRow))
            {
                ReleaseLedge();
                ApplyTransform();
                return;
            }

            bool upHeld = verticalMoveInput > LedgeInputThreshold;
            bool downHeld = verticalMoveInput < -LedgeInputThreshold;
            bool awayHeld = moveInput * ledgeSide < -LedgeInputThreshold;

            bool upPressed = upHeld && !upWasHeld;
            bool downPressed = downHeld && !downWasHeld;
            bool awayPressed = awayHeld && !awayWasHeld;

            upWasHeld = upHeld;
            downWasHeld = downHeld;
            awayWasHeld = awayHeld;

            bool jumpPressed = jumpBufferTimer > 0f;
            jumpBufferTimer = 0f;

            if (jumpPressed || (climbWithUp && upPressed))
            {
                TryStartClimb();
            }
            else if (downPressed || awayPressed)
            {
                ReleaseLedge();
            }

            ApplyTransform();
        }

        /// <summary>
        /// Starts climbing up if there is room above the ledge for the player. Otherwise they keep hanging.
        /// </summary>
        private void TryStartClimb()
        {
            float ledgeTop = ledgeRow + 0.5f;
            float wallFace = ledgeColumn - ledgeSide * 0.5f;

            float endHorizontal = wallFace + ledgeSide * (halfSize.x + ClimbInset);
            float endVertical = ledgeTop + halfSize.y;

            //hanging under a one-way platform: climb straight up through it.
            if (isHangingUnderLedge)
                endHorizontal = horizontal;

            if (IsBlocked(horizontal - halfSize.x, horizontal + halfSize.x,
                vertical - halfSize.y, endVertical + halfSize.y))
                return;

            float leftmost = Mathf.Min(horizontal, endHorizontal) - halfSize.x;
            float rightmost = Mathf.Max(horizontal, endHorizontal) + halfSize.x;

            if (IsBlocked(leftmost, rightmost, endVertical - halfSize.y + BlockGrid.EDGE_TOLERANCE, endVertical + halfSize.y))
                return;

            ledgeState = LedgeState.Climbing;
            climbStart = new Vector2(horizontal, vertical);
            climbEnd = new Vector2(endHorizontal, endVertical);
            climbTimer = 0f;

            PlayPlayerSound(ledgeClimbSound);

            if (climbDuration <= 0f)
                FinishClimb();
        }

        /// <summary>
        /// Moves the player up first, then over onto the ledge.
        /// </summary>
        private void UpdateClimb(float deltaTime)
        {
            climbTimer += deltaTime;
            float progress = Mathf.Clamp01(climbTimer / climbDuration);

            const float pullUpPart = 0.6f;

            if (progress < pullUpPart)
            {
                float pullUpProgress = Mathf.SmoothStep(0f, 1f, progress / pullUpPart);
                horizontal = climbStart.x;
                vertical = Mathf.Lerp(climbStart.y, climbEnd.y, pullUpProgress);
            }
            else
            {
                float stepOverProgress = Mathf.SmoothStep(0f, 1f, (progress - pullUpPart) / (1f - pullUpPart));
                horizontal = Mathf.Lerp(climbStart.x, climbEnd.x, stepOverProgress);
                vertical = climbEnd.y;
            }

            if (progress >= 1f)
                FinishClimb();
        }

        private void FinishClimb()
        {
            horizontal = climbEnd.x;
            vertical = climbEnd.y;
            ledgeState = LedgeState.None;
            velocity = Vector2.zero;

            UpdateGrounded();
            wasGroundedLastFrame = isGrounded;
        }

        /// <summary>
        /// Lets go of the ledge and starts falling.
        /// </summary>
        private void ReleaseLedge()
        {
            ledgeState = LedgeState.None;
            regrabTimer = regrabDelay;
            velocity.y = 0f;
            isGrounded = false;
            collisionCheck.IsGrounded = false;
        }

        #endregion

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

            bool landsOnOneWay;
            if (isBehindGeometry)
                landsOnOneWay = grid.LandsOnOneWayAtDepth(view, minHorizontal, maxHorizontal, previousFeet, newFeet, DepthLayer, out landingHeight);
            else
                landsOnOneWay = grid.LandsOnOneWay(view, minHorizontal, maxHorizontal, previousFeet, newFeet, out landingHeight);

            //the top of a ladder (or vines) holds the player up the same way a one-way block does.
            bool landsOnClimbableTop = grid.LandsOnClimbableTop(view, minHorizontal, maxHorizontal, previousFeet, newFeet,
                isBehindGeometry, DepthLayer, out float climbableTop);

            if (landsOnClimbableTop && (!landsOnOneWay || climbableTop > landingHeight))
            {
                landingHeight = climbableTop;
                return true;
            }

            return landsOnOneWay;
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
            int nearLayer = BlockGrid.CellIndex(depth - halfSize.x + BlockGrid.EDGE_TOLERANCE);
            int farLayer = BlockGrid.CellIndex(depth + halfSize.x - BlockGrid.EDGE_TOLERANCE);

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
            int leftColumn = BlockGrid.CellIndex(horizontal - halfSize.x + BlockGrid.EDGE_TOLERANCE);
            int rightColumn = BlockGrid.CellIndex(horizontal + halfSize.x - BlockGrid.EDGE_TOLERANCE);

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
        /// True while the player is hanging from a ledge (not while climbing up it).
        /// </summary>
        public bool IsHanging
        {
            get { return ledgeState == LedgeState.Hanging; }
        }

        /// <summary>
        /// True while the player is climbing up onto a ledge.
        /// </summary>
        /// <summary>
        /// True while the player is on a ladder or vines (a ClimbableBlock).
        /// </summary>
        public bool IsOnClimbable
        {
            get { return isOnClimbable; }
        }

        public bool IsClimbingLedge
        {
            get { return ledgeState == LedgeState.Climbing; }
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
