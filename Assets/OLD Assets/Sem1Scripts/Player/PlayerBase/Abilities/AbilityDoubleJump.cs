using UnityEngine;

public class AbilityDoubleJump : MonoBehaviour
{
    private Rigidbody rb;
    private PlayerCollision collision;
    private bool canDoubleJump = false;
    private bool wasGrounded = false;

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        collision = GetComponent<PlayerCollision>();
        canDoubleJump = false;
        wasGrounded = false;
    }

    private void OnDisable()
    {
        canDoubleJump = false;
    }

    private void Update()
    {
        if (collision.onGround)
        {
            wasGrounded = true;
            canDoubleJump = false;
        }
        else if (wasGrounded)
        {
            canDoubleJump = true;
            wasGrounded = false;
        }
    }

    public bool TryDoubleJump()
    {
        if (canDoubleJump)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, 0);
            rb.linearVelocity += Vector3.up * GetComponent<PlayerMovement>().normalJumpForce;
            canDoubleJump = false;
            return true;
        }
        return false;
    }
}