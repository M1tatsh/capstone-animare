using UnityEngine;

public class BlockCarryable : BlockBase
{
    private Vector3 offset;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    void FixedUpdate()
    {
        if (transform.parent != null)
        {
            rb.useGravity = false;

            transform.SetPositionAndRotation(transform.parent.position + offset, transform.parent.rotation);
        }
    }

    public void SetOffset(Vector3 desiredOffset)
    {
        offset = desiredOffset;
    }

    public void ThrowBlock(bool onZAxis, bool facingRight, float throwSpeed)
    {
        transform.SetParent(null);
        rb.useGravity = true;

        if (onZAxis)
        {
            facingRight = !facingRight;
        }

        SetConstraints(!onZAxis);

        rb.linearVelocity = Vector3.zero;
        Vector3 dir = facingRight ? Vector3.right : Vector3.left;
        rb.linearVelocity += transform.InverseTransformDirection(dir.normalized * throwSpeed);

    }
}
