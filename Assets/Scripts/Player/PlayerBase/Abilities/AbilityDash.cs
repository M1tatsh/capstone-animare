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

    public void Execute(float x)
    {
        if (player.setPlayerXToZ)
        {
            x = -x;
        }

        StopCoroutine(DashRoutine());
        StartCoroutine(DashRoutine());

        rb.linearVelocity = Vector3.zero;
        Vector3 dir = new Vector3(x, 0, 0);
        rb.linearVelocity += transform.InverseTransformDirection(dir.normalized * dashSpeed);
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        player.disableStateMachine = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        player.disableStateMachine = false;
    }
}