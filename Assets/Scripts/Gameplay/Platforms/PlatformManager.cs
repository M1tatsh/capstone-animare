using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * Attach to GameManager to allow for the player to land on empty space, accounting for depth in an otherwise 2D plane.
 * Creates invisible cubes for the player to land on, based on the closest block to the camera. 
 * Then, moves the player to the closest real block. This allows depth to be ignored when creating 3D levels.
 */

public class PlatformManager : MonoBehaviour
{
    private PlayerMovement playerMove;

    public FacingDirection facingDirection;

    public GameObject Player;

    private float degree = 0;

    //platforms in the level
    public Transform Platforms;

    //background objects in the level
    public Transform Buildings;

    [Tooltip("Cube object with its mesh turned off. " +
        "Spawns in when the player is standing on an empty space, " +
        "when there should be a platform there (accounting for depth)"
        )]
    public GameObject invisibleCube;

    private List<Transform> cubes = new List<Transform>();

    private FacingDirection lastDirection;

    private float lastDepth = 0f;

    [Tooltip("Size of 1 block in world transform")]
    public float GLOBAL_UNIT = 1.0f;

    public enum FacingDirection
    {
        Front = 0,
        Right = 1,
        Back = 2,
        Left = 3

    }

    private void Start()
    {
        //Define directional and cache the playermovement scripts
        Player = GameObject.FindWithTag("Player");
        facingDirection = FacingDirection.Front;
        playerMove = Player.GetComponent<PlayerMovement>();
        UpdateLevel(true);
    }

    private void Update()
    {
        //Player depth handler
        if(playerMove.currPS != PlayerMovement.PlayerState.Airborn)
        {
            bool updateLevel = false;
            if(OnInvisibleCube())
                if(MovePlayerDepthToClosestPlatform())
                    updateLevel = true;
            if (MoveToClosestPlatformToCamera())
                updateLevel = true;
            if (updateLevel)
                UpdateLevel(false);
        }

        if(Input.GetButton("Fire2"))
        {

        }
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
                return;

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
    }

    /// <summary>
    /// Is player standing on invisible cube?
    /// </summary>
    private bool OnInvisibleCube()
    {
        foreach(Transform item in cubes)
        {
            //check player's position against cube's position
            if (Mathf.Abs(item.position.x - playerMove.transform.position.x) < GLOBAL_UNIT && Mathf.Abs(item.position.z - playerMove.transform.position.z) < GLOBAL_UNIT)
                if (playerMove.transform.position.y - item.position.y <= GLOBAL_UNIT + 0.2f && playerMove.transform.position.y - item.position.y > 0)
                    return true;
        }

        return false;
    }

    /// <summary>
    /// Moves the player to the closest ground based on camera height.
    /// </summary>
    /// <returns></returns>
    private bool MoveToClosestPlatformToCamera()
    {
        bool movePlayer = false;
        foreach(Transform item in Platforms)
        {
            //if moving along x-axis
            if(facingDirection == FacingDirection.Front || facingDirection == FacingDirection.Back)
            {
                if(Mathf.Abs(item.position.x - playerMove.transform.position.x) < GLOBAL_UNIT + 0.1f)
                {
                    if(playerMove.transform.position.y - item.position.y <= GLOBAL_UNIT + 0.2f && playerMove.transform.position.y - item.position.y > 0 && playerMove.currPS != PlayerMovement.PlayerState.Airborn)
                    {
                        if(facingDirection == FacingDirection.Front && item.position.z < playerMove.transform.position.z)
                            movePlayer = true;
                        if (facingDirection == FacingDirection.Back && item.position.z > playerMove.transform.position.z)
                            movePlayer = true;

                        if(movePlayer)
                        {
                            playerMove.transform.position = new Vector3(playerMove.transform.position.x, playerMove.transform.position.y, item.position.z);
                            return true;
                        }
                    }
                }
            }
            //if moving along z-axis
            else
            {
                if (Mathf.Abs(item.position.z - playerMove.transform.position.z) < GLOBAL_UNIT + 0.1f)
                {
                    if (playerMove.transform.position.y - item.position.y <= GLOBAL_UNIT + 0.2f && playerMove.transform.position.y - item.position.y > 0 && playerMove.currPS != PlayerMovement.PlayerState.Airborn)
                    {
                        if (facingDirection == FacingDirection.Right && item.position.x > playerMove.transform.position.x)
                            movePlayer = true;
                        if (facingDirection == FacingDirection.Left && item.position.x < playerMove.transform.position.x)
                            movePlayer = true;

                        if (movePlayer)
                        {
                            playerMove.transform.position = new Vector3(item.position.x, playerMove.transform.position.y, playerMove.transform.position.z);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Finds a cube in the cube list at given position.
    /// </summary>
    /// <param name="cube"></param>
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
    /// Returns the player's distance from camera.
    /// </summary>
    /// <returns></returns>
    private float GetPlayerDepth()
    {
        float closestPoint = 0f;

        if(facingDirection == FacingDirection.Front || facingDirection == FacingDirection.Back)
        {
            closestPoint = playerMove.transform.position.z;
        }
        else if(facingDirection == FacingDirection.Right ||  facingDirection == FacingDirection.Left)
        {
            closestPoint = playerMove.transform.position.x;
        }

        return Mathf.Round(closestPoint);
    }

    /// <summary>
    /// Returns new facing direction by incrementing. 
    /// </summary>
    /// <returns></returns>
    private FacingDirection RotateDirectionRight()
    {
        int direction = (int)(facingDirection);
        direction++;

        if(direction > 3)
            direction = 0;

        return((FacingDirection)direction);
    }

    /// <summary>
    /// Returns new facing direction by decrementing.
    /// </summary>
    /// <returns></returns>
    private FacingDirection RotateDirectionLeft()
    {
        int direction = (int)(facingDirection);
        direction--;

        if (direction < 0)
            direction = 3;

        return ((FacingDirection)direction);
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
                if(!FindTransformInCubes(tempCube) && !FindTransformPlatform(tempCube) && !FindTransformBuilding(item.position))
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
    /// <param name="cube"></param>
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

    /// <summary>
    /// Will set player's position to closest platform available and return true.
    /// </summary>
    /// <returns></returns>
    private bool MovePlayerDepthToClosestPlatform()
    {
        foreach(Transform item in Platforms)
        {
            if (facingDirection == FacingDirection.Front || facingDirection == FacingDirection.Back)
            {
                if (Mathf.Abs(item.position.x - playerMove.transform.position.x) < GLOBAL_UNIT + 0.1f)
                    if (playerMove.transform.position.y - item.position.y <= GLOBAL_UNIT + 0.2f && playerMove.transform.position.y - item.position.y > 0)
                    {

                        playerMove.transform.position = new Vector3(playerMove.transform.position.x, playerMove.transform.position.y, item.position.z);
                        return true;

                    }
            }
            else
            {
                if (Mathf.Abs(item.position.z - playerMove.transform.position.z) < GLOBAL_UNIT + 0.1f)
                    if (playerMove.transform.position.y - item.position.y <= GLOBAL_UNIT + 0.2f && playerMove.transform.position.y - item.position.y > 0)
                    {

                        playerMove.transform.position = new Vector3(item.position.x, playerMove.transform.position.y, playerMove.transform.position.z);
                        return true;
                    }
            }
        }
        return false;
    }
}
