using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public LayerMask groundLayer;

    [Space]

    public bool onGround = false;
    public bool onWallRight = false;
    public bool onWallLeft = false;
    public int wallSide;

    [Header("Collision")]
    public float collisionRadius = 0.25f;
    public Vector3 bottomOffset, rightOffset, leftOffset;
    private Color debugCollisionColor = Color.red; //draw collision circles
    public bool drawDebug = true;

    void Update()
    {
        onGround = Physics.CheckSphere((Vector3)transform.position + bottomOffset, collisionRadius, groundLayer);
        onWallRight = Physics.CheckSphere((Vector3)transform.position + rightOffset, collisionRadius, groundLayer);
        onWallLeft = Physics.CheckSphere((Vector3)transform.position + leftOffset, collisionRadius, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = debugCollisionColor;

        if (drawDebug)
        {
            var positions = new Vector3[] { bottomOffset, rightOffset, leftOffset };

            Gizmos.DrawWireSphere((Vector3)transform.position + bottomOffset, collisionRadius);
            Gizmos.DrawWireSphere((Vector3)transform.position + rightOffset, collisionRadius);
            Gizmos.DrawWireSphere((Vector3)transform.position + leftOffset, collisionRadius);

        }
    }
}
