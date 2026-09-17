using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class AbilityWallJump : MonoBehaviour
{
    public float wallJumpForce = 10f;
    public float slideForce = 2.5f;
    public float disableTime = 0.1f;

    private Rigidbody rb;
    private PlayerMovement player;
    private PlayerCollision collision;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = GetComponent<PlayerMovement>();
        collision = GetComponent<PlayerCollision>();
    }

    public void WallSlide()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, -slideForce, rb.linearVelocity.z);
    }

    public void Execute()
    {
        StopCoroutine(DisableMovement());
        StartCoroutine(DisableMovement());

        Vector3 wallDir;

        if (player.movingOnZ)
            wallDir = collision.onWallRight ? Vector3.right : Vector3.left;
        else
            wallDir = collision.onWallRight ? Vector3.left : Vector3.right;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, 0);
        rb.linearVelocity += transform.InverseTransformDirection((Vector3.up / 1.5f + wallDir / 1.5f) * wallJumpForce);
    }

    private IEnumerator DisableMovement()
    {
        player.hasWallJumped = true;
        player.disableStateMachine = true;
        yield return new WaitForSeconds(disableTime);
        player.hasWallJumped = false;
        player.disableStateMachine = false;
    }
}