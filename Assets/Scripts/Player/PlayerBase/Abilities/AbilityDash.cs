using System.Collections;
using UnityEngine;

public class AbilityDash : MonoBehaviour
{
    public float dashSpeed = 5f;
    public float dashTime = 0.2f;

    private Rigidbody rb;
    private PlayerMovement player;
    [HideInInspector] public bool isDashing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = GetComponent<PlayerMovement>();
    }

    public void Execute(float x, float y)
    {
        StopCoroutine(DashRoutine());
        StartCoroutine(DashRoutine());

        rb.linearVelocity = Vector3.zero;
        Vector3 dir = new Vector3(x, y, 0);
        rb.linearVelocity += dir.normalized * dashSpeed;
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        player.hasWallJumped = true;
        player.disableStateMachine = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        player.hasWallJumped = false;
        player.disableStateMachine = false;
    }
}