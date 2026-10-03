using Gameplay.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Mechanics
{
    /// <summary>
    /// PlatformManager will create an array of invisible cubes for the player to move on, based on 
    /// some logic that accounts for object depth in 3D. Uses a number of private methods to calculate
    /// player depth to camera and determine when and where an invisible cube needs to be spawned in.
    /// </summary>
    public class PlatformManager : MonoBehaviour
    {
        #region variables

        //object storage
        private PlayerMovement playerMovement;
        private CollisionCheck collisionCheck;
        private GameObject Player;

        [Tooltip("Set to facing direction at the start of the scene.")]
        private FacingDirection facingDirection;
        
        private float degree = 0;
        [SerializeField]private float bufferArea = 0.5f;
        [SerializeField] private float leniencyArea = 0.1f;
        [Tooltip("Size of 1 block in world transform")]
        private static float STANDARD_UNIT = 1.0f;

        public Transform Platforms; //objects that can be stood on
        public Transform Buildings; //all other objects

        [Tooltip("Cube object with its mesh turned off. " +
            "Spawns in when the player is standing on an empty space, " +
            "when there should be a platform there (accounting for depth)"
            )]
        public GameObject invisibleCube;

        private List<Transform> cubes = new List<Transform>();
        private FacingDirection lastDirection;
        private float lastDepth = 0f;


        public bool rotateLeft = false;
        public bool rotateRight = false;
        public bool updateCubeArray = false;
        #endregion

        private void Start()
        {
            //Define facing direction and cache the playermovement scripts
            Player = GameObject.FindWithTag("Player");
            playerMovement = Player.GetComponent<PlayerMovement>();
            collisionCheck = Player.GetComponent<CollisionCheck>();
            UpdateLevel(true);

            if(Player == null)
            {
                Debug.LogError("Player not found in scene!");
            }

            if(playerMovement == null)
            {
                Debug.LogError("PlayerMovement not found in scene!");
            }

            if (collisionCheck == null)
            {
                Debug.LogError("CollisionCheck not found in scene!");
            }
        }

        private void Update()
        {

            //Debug.Log($"PlatformManager.Update | Current Facing Direction: {facingDirection}");
            //Player depth handler
            if (collisionCheck.IsGrounded)
            {
                bool updateLevel = false;
                
                if (OnInvisibleCube())
                {
                    if (MovePlayerDepthToClosestPlatform())
                        updateLevel = true;
                }

                if (updateLevel)
                    UpdateLevel(false);
            }

            //use bools to check if the player wants to rotate
            if (rotateRight)
            {
                //If we rotate while on an invisible platform we must move to a physical platform
                //If we don't, then we could be standing in mid air after the rotation
                if (OnInvisibleCube())
                {
                    MovePlayerDepthToClosestPlatform();
                }
                lastDirection = facingDirection;
                facingDirection = RotateDirectionRight();
                degree -= 90f;
                UpdateLevel(false);
                rotateRight = false;
                playerMovement.UpdateToFacingDirection(facingDirection, degree);
            }
            else if (rotateLeft)
            {
                if (OnInvisibleCube())
                {
                    MovePlayerDepthToClosestPlatform();
                }
                lastDirection = facingDirection;
                facingDirection = RotateDirectionLeft();
                degree += 90f;
                UpdateLevel(false);
                rotateLeft = false;
                playerMovement.UpdateToFacingDirection(facingDirection, degree);
            }
        }

        /// <summary>
        /// Returns new facing direction by incrementing. 
        /// </summary>
        /// <returns></returns>
        public FacingDirection RotateDirectionRight()
        {
            int direction = (int)(facingDirection);
            direction++;

            //loop state when it reaches the last value
            if(direction > 3)
                direction = 0;

            return((FacingDirection)(direction));
        }

        /// <summary>
        /// Returns new facing direction by decrementing.
        /// </summary>
        /// <returns></returns>
        public FacingDirection RotateDirectionLeft()
        {
            int direction = (int)(facingDirection);
            direction--;

            if (direction < 0)
                direction = 3;

            return ((FacingDirection)(direction));
        }
        /// <summary>
        /// Destroy all existing invisible cubes and creates new ones
        /// using player's facingDirection.
        /// </summary>
        /// <param name="forceRebuild" ></param>
        private void UpdateLevel(bool forceRebuild)
        {
            if (!forceRebuild)
                if (lastDirection == facingDirection && lastDepth == GetPlayerDepth())
                {
                    //Debug.Log($"PlatformManager.UpdateLevel | Returned early!");
                    return;
                }
                    

            foreach(Transform item in cubes)
            {
                //update current cube list

                item.position = Vector3.zero;
                Destroy(item.gameObject);
            }
            cubes.Clear();

            float newDepth = 0f;
            newDepth = GetPlayerDepth();
            CreateCubesAtNewDepth(newDepth);

            //Debug.Log($"PlatformManager.UpdateLevel | Level Updated!");
        }

        /// <summary>
        /// Is player standing on invisible cube?
        /// </summary>
        private bool OnInvisibleCube()
        {
            foreach(Transform item in cubes)
            {
                //check player's position against cube's position
                if (Mathf.Abs(item.position.x - playerMovement.transform.position.x) <= STANDARD_UNIT - bufferArea && Mathf.Abs(item.position.z - playerMovement.transform.position.z) <= STANDARD_UNIT - bufferArea)
                {
                    if (playerMovement.transform.position.y - item.position.y <= STANDARD_UNIT + leniencyArea && playerMovement.transform.position.y - item.position.y > STANDARD_UNIT - leniencyArea)
                    {
                        Debug.Log($"PlatformManager.OnInvisibleCube | Standing on invisible cube: {item.position} with player position at {playerMovement.transform.position}");
                        return true;
                    }
                }
            }
            return false;
        }

        //deprecated
        private bool MovePlayerToClosestPlatformFromCamera()
        {
            foreach (Transform item in Platforms)
            {
                //check if player is outside of block's y position
                if (playerMovement.transform.position.y - item.position.y >= STANDARD_UNIT + leniencyArea && playerMovement.transform.position.y - item.position.y <= STANDARD_UNIT - leniencyArea)    
                    continue;

                if (!collisionCheck.IsGrounded)
                    continue;

                if (facingDirection == FacingDirection.Front || facingDirection == FacingDirection.Back)
                {
                    //check if player is outside of block's x position
                    if (Mathf.Abs(playerMovement.transform.position.x - item.position.x) > STANDARD_UNIT - bufferArea)
                        continue;

                    if (facingDirection == FacingDirection.Front && playerMovement.transform.position.z - item.position.z > STANDARD_UNIT - bufferArea)
                    {
                        playerMovement.transform.position = new Vector3(playerMovement.transform.position.x, playerMovement.transform.position.y, item.position.z);
                        return true;
                    }

                    if (facingDirection == FacingDirection.Back && item.position.z - playerMovement.transform.position.z > STANDARD_UNIT - bufferArea)
                    {
                        playerMovement.transform.position = new Vector3(playerMovement.transform.position.x, playerMovement.transform.position.y, item.position.z);
                        return true;
                    }
                }
                else
                {
                    //check if player is outside of block's x position
                    if (Mathf.Abs(playerMovement.transform.position.z - item.position.z) > STANDARD_UNIT - bufferArea)
                        continue;

                    if (facingDirection == FacingDirection.Right && item.position.x - playerMovement.transform.position.x > STANDARD_UNIT - bufferArea)
                    {
                        playerMovement.transform.position = new Vector3(item.position.x,  playerMovement.transform.position.y, playerMovement.transform.position.z);
                        return true;
                    }

                    if (facingDirection == FacingDirection.Left && playerMovement.transform.position.x - item.position.x > STANDARD_UNIT - bufferArea)
                    {
                        playerMovement.transform.position = new Vector3(item.position.x, playerMovement.transform.position.y, playerMovement.transform.position.z);
                        return true;
                    }
                }
            }
            return false;
        }

        /*
        /// <summary>
        /// Moves the player to the closest walkable ground based on camera height.
        /// Also returns bool based on if we move the player or not.
        /// </summary>
        /// <returns></returns>
        private bool MoveToClosestPlatformToCamera()
        {
            bool moveCloser = false;
            foreach (Transform item in Platforms)
            {
                if (facingDirection == FacingDirection.Front || facingDirection == FacingDirection.Back)
                {

                    //When facing Front, find cubes that are close enough in the x position and just below our current y value
                    //This would have to be updated if using cubes bigger or smaller than (1,1,1)
                    if (Mathf.Abs(item.position.x - playerMovement.transform.position.x) < STANDARD_UNIT + leniency)
                    {

                        if (playerMovement.transform.position.y - item.position.y <= STANDARD_UNIT + leniency && playerMovement.transform.position.y - item.position.y > 0 && collisionCheck.IsGrounded)
                        {
                            if (facingDirection == FacingDirection.Front && item.position.z < playerMovement.transform.position.z)
                                moveCloser = true;

                            if (facingDirection == FacingDirection.Back && item.position.z > playerMovement.transform.position.z)
                                moveCloser = true;


                            if (moveCloser)
                            {
                                playerMovement.transform.position = new Vector3(playerMovement.transform.position.x, playerMovement.transform.position.y, item.position.z);
                                return true;
                            }
                        }

                    }

                }
                else
                {
                    if (Mathf.Abs(item.position.z - playerMovement.transform.position.z) < STANDARD_UNIT + leniency)
                    {
                        if (playerMovement.transform.position.y - item.position.y <= STANDARD_UNIT + leniency && playerMovement.transform.position.y - item.position.y > 0 && collisionCheck.IsGrounded)
                        {
                            if (facingDirection == FacingDirection.Right && item.position.x > playerMovement.transform.position.x)
                                moveCloser = true;

                            if (facingDirection == FacingDirection.Left && item.position.x < playerMovement.transform.position.x)
                                moveCloser = true;

                            if (moveCloser)
                            {
                                playerMovement.transform.position = new Vector3(item.position.x, playerMovement.transform.position.y, playerMovement.transform.position.z);
                                return true;
                            }

                        }

                    }
                }


            }

            return false;
        }
        */

        /// <summary>
        /// Will set player's position to closest platform available.
        /// </summary>
        /// <returns>Returns true if the player was moved, false otherwise.</returns>
        private bool MovePlayerDepthToClosestPlatform()
        {
            foreach(Transform item in Platforms)
            {
                if (playerMovement.transform.position.y - item.position.y >= STANDARD_UNIT + leniencyArea || playerMovement.transform.position.y - item.position.y <= STANDARD_UNIT - leniencyArea)
                {
                    Debug.Log("PlatformManager.MovePlayerDepthToClosestPlatform: y-check confirmed.");
                    continue;
                }

                if (facingDirection == FacingDirection.Front || facingDirection == FacingDirection.Back)
                {
                    if (Mathf.Abs(item.position.x - playerMovement.transform.position.x) > STANDARD_UNIT - bufferArea)
                    {
                        Debug.Log("PlatformManager.MovePlayerDepthToClosestPlatform: x-check confirmed.");
                        continue;
                    }

                    lastDepth = playerMovement.transform.position.z;
                    playerMovement.transform.position = new Vector3(playerMovement.transform.position.x, playerMovement.transform.position.y, item.position.z);
                    

                    Debug.Log($"PlatformManager.MovePlayerDepthToClosestPlatform | Moved player to: {item.position.z} using block: {item.name}");
                    return true;
                }
                else
                {
                    if (Mathf.Abs(item.position.z - playerMovement.transform.position.z) > STANDARD_UNIT - bufferArea)
                    {
                        Debug.Log("PlatformManager.MovePlayerDepthToClosestPlatform: z-check confirmed.");
                        continue;
                    }

                    lastDepth = playerMovement.transform.position.x;
                    playerMovement.transform.position = new Vector3(item.position.x, playerMovement.transform.position.y, playerMovement.transform.position.z);

                    Debug.Log($"PlatformManager.MovePlayerDepthToClosestPlatform | Moved player to: {item.position.x}  using block: {item.name}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Looks for an invisible cube at Vector3 position and returns a bool if it finds one.
        /// </summary>
        /// <param name="cube">position of reference cube.</param>
        /// <returns></returns>
        private bool FindTransformInCubes(Vector3 cube)
        {
            foreach(Transform item in cubes)
            {
                if(item.position == cube)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the player's x or z coordinate, depending on facing direction.
        /// </summary>
        /// <returns></returns>
        private float GetPlayerDepth()
        {
            float closestPoint = 0f;

            if(facingDirection == FacingDirection.Front || facingDirection == FacingDirection.Back)
            {
                closestPoint = playerMovement.transform.position.z;
            }
            else if(facingDirection == FacingDirection.Right || facingDirection == FacingDirection.Left)
            {
                closestPoint = playerMovement.transform.position.x;
            }

            //Debug.Log($"PlatformManager.GetPlayerDepth | closest point found: {Mathf.Round(closestPoint)}");
            return Mathf.Round(closestPoint);
        }

       

        /// <summary>
        /// Create cube at given position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Transform CreateCube(Vector3 position)
        {
            GameObject go = Instantiate(invisibleCube) as GameObject;

            go.transform.position = position;

            return go.transform;
        }

        /// <summary>
        /// Create cubes based on platform depth using CreateCube function and adds to cube list.
        /// </summary>
        /// <param name="newDepth"></param>
        private void CreateCubesAtNewDepth(float newDepth)
        {
            Vector3 tempCube = Vector3.zero;
            foreach(Transform item in Platforms)
            {
                if(facingDirection == FacingDirection.Front || facingDirection == FacingDirection.Back)
                {
                    tempCube = new Vector3(item.position.x, item.position.y, newDepth);
                    if(!FindTransformInCubes(tempCube) && !FindTransformPlatform(tempCube) && !FindTransformBuilding(item.position) && (Mathf.Abs(playerMovement.transform.position.x - item.position.x) > STANDARD_UNIT - bufferArea || Mathf.Abs(playerMovement.transform.position.y - item.position.y) > STANDARD_UNIT - bufferArea))
                    {
                        Transform go = CreateCube(tempCube);
                        cubes.Add(go);
                    }
                }
                if (facingDirection == FacingDirection.Right || facingDirection == FacingDirection.Left)
                {
                    tempCube = new Vector3(newDepth, item.position.y, item.position.z);
                    if (!FindTransformInCubes(tempCube) && !FindTransformPlatform(tempCube) && !FindTransformBuilding(item.position) && (Mathf.Abs(playerMovement.transform.position.z - item.position.z) > STANDARD_UNIT - bufferArea || Mathf.Abs(playerMovement.transform.position.y - item.position.y) > STANDARD_UNIT - bufferArea))
                    {
                        Transform go = CreateCube(tempCube);
                        cubes.Add(go);
                    }
                }
            }
        }

        
        /// <summary>
        /// Finds physical cube in the platform list at given cube position.
        /// </summary>
        /// <param name="cube">Vector3 to check</param>
        /// <returns></returns>
        private bool FindTransformPlatform(Vector3 cube)
        {
            foreach(Transform item in Platforms)
            {
                if(item.position == cube)
                    return true;
            }
            return false;
        }
        

        
        /// <summary>
        /// Determine whether there are any buildings (background) objects between the camera and the given cube.
        /// </summary>
        /// <param name="cube"></param>
        /// <returns></returns>
        private bool FindTransformBuilding(Vector3 cube)
        {
            foreach (Transform item in Buildings)
            {
                if (facingDirection == FacingDirection.Front)
                {
                    if (item.position.x == cube.x && item.position.y == cube.y && item.position.z < cube.z)
                        return true;
                }
                else if (facingDirection == FacingDirection.Back)
                {
                    if (item.position.x == cube.x && item.position.y == cube.y && item.position.z > cube.z)
                        return true;
                }
                else if (facingDirection == FacingDirection.Right)
                {
                    if (item.position.z == cube.z && item.position.y == cube.y && item.position.x > cube.x)
                        return true;

                }
                else
                {
                    if (item.position.z == cube.z && item.position.y == cube.y && item.position.x < cube.x)
                        return true;

                }
            }
            return false;
        }
        

        public enum FacingDirection
        {
            Front = 0,
            Right = 1,
            Back = 2,
            Left = 3
        }
    }
}
