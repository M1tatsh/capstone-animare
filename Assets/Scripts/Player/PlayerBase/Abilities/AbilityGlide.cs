using UnityEngine;

public class AbilityGlide : MonoBehaviour
{
    public float glideDuration = 3f;
    public float glideGravity = 0.5f;
    private Rigidbody rb;
    private PlayerCollision collision;
    private bool isGliding = false;
    private float glideTimer = 0f;

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        collision = GetComponent<PlayerCollision>();
        isGliding = false;
        glideTimer = 0f;
    }

    private void OnDisable()
    {
        isGliding = false;
        glideTimer = 0f;
    }

    private void Update()
    {
        if (collision.onGround)
        {
            isGliding = false;
            glideTimer = 0f;
        }

        if (!collision.onGround && Input.GetButtonDown("Jump") && !isGliding && glideTimer < glideDuration)
        {
            isGliding = true;
        }

        if (Input.GetButtonUp("Jump"))
        {
            isGliding = false;
        }

        if (isGliding)
        {
            glideTimer += Time.deltaTime;
            if (glideTimer >= glideDuration)
            {
                isGliding = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isGliding)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -glideGravity, rb.linearVelocity.z);
        }
    }
}