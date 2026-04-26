using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Space]
    public bool onGround = false;
    public bool onWallRight = false;
    public bool onWallLeft = false;
    public int wallSide;

    [Header("Collision")]
    public float collisionRadius = 0.25f;
    public Vector3 bottomOffset, rightOffset, leftOffset;
    private Vector3 reverseRightOffset, reverseLeftOffset;
    private Color debugCollisionColor = Color.red;
    public bool drawDebug = true;

    void Start()
    {

    }

    void Update()
    {
        ChangeAxis();

        onGround = Physics.CheckSphere(transform.position + bottomOffset, collisionRadius, groundLayer | wallLayer);
        onWallRight = Physics.CheckSphere(transform.position + transform.InverseTransformDirection(rightOffset), collisionRadius, wallLayer);
        onWallLeft = Physics.CheckSphere(transform.position + transform.InverseTransformDirection(leftOffset), collisionRadius, wallLayer);

        if (onWallRight) wallSide = 1;
        else if (onWallLeft) wallSide = -1;
        else wallSide = 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = debugCollisionColor;

        if (drawDebug)
        {
            Gizmos.DrawWireSphere(transform.position + bottomOffset, collisionRadius);
            Gizmos.DrawWireSphere(transform.position + transform.InverseTransformDirection(rightOffset), collisionRadius);
            Gizmos.DrawWireSphere(transform.position + transform.InverseTransformDirection(leftOffset), collisionRadius);
        }
    }

    private void ChangeAxis()
    {
        if(GetComponent<PlayerMovement>().movingOnZ)
        {
            if (rightOffset.x >= 0 && leftOffset.x <= 0)
            {
            rightOffset = -rightOffset;
            leftOffset = -leftOffset;
            }
        }
        else
        {
            if (rightOffset.x <= 0 && leftOffset.x >= 0)
            {
                rightOffset = -rightOffset;
                leftOffset = -leftOffset;
            }
        }
    }
}