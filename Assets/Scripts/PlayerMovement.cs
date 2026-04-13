using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    enum PlayerState
    {
        None,
        Idle,
        Jumping,
        ShortHop,
        Falling,
        Running,
        Dashing
    }

    PlayerState currPlayerState = PlayerState.Idle;

    public float maxRunSpeed = 100.0f;
    public float runForce = 50.0f;
    public float runDragStrength = 0.5f;
    public float jumpForce = 800.0f;
    public float onGroundHeight = 0.2f;
    public float dashForce = 200.0f;

    float distanceToGround = 0.0f;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    public SpriteRenderer sprite;
    bool flipVisual = false;

    float secondsSinceOnGround = 0.0f;
    float timeVar = 0.0f;
    bool wasPressed = false;

    Vector3 startingPosition;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startingPosition = transform.position;
    }

    void FixedUpdate()
    {
        //calculate distance to ground
        {
            RaycastHit hit;
            Physics.Raycast(groundCheck.position, -Vector3.up, out hit);
            if (hit.collider.gameObject.layer == 6) //6 is ground layer
            {
                distanceToGround = hit.distance;
            }
        }

        float horizontalAxis = Input.GetAxis("Horizontal");
        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);

        //character movement formula
        if(horizontalSpeed < maxRunSpeed)
        {
            rb.AddForce(Vector3.right * horizontalAxis * runForce);
        }

        float clampedVelocityX = Mathf.Clamp
            (
                rb.linearVelocity.x * (1.0f - runDragStrength), 
                -maxRunSpeed, 
                maxRunSpeed
            );

        //add velocity to rigidbody
        rb.linearVelocity = new Vector3
        (
            clampedVelocityX,
            rb.linearVelocity.y,
            rb.linearVelocity.z
        );

        if (clampedVelocityX < -0.1f && !sprite.flipX)
        {
            flipVisual = true;
        }
        else if (clampedVelocityX > 0.1f && sprite.flipX)
        {
            flipVisual = true;
        }
        else
        {
            flipVisual = false;
        }
    }

    void PlayerStateMachine()
    {
        switch(currPlayerState)
        {
            case PlayerState.Idle:
                if (Input.GetButtonDown("Jump"))
                    SetPlayerState(PlayerState.Jumping);

                if (Input.GetKeyDown(KeyCode.LeftShift))
                    SetPlayerState(PlayerState.Dashing);

                if (IsFalling() && IsOnGround() == false)
                    SetPlayerState(PlayerState.Falling);

                break;
            case PlayerState.Jumping:
                if (Input.GetButtonUp("Jump"))
                    SetPlayerState(PlayerState.ShortHop);

                if (IsFalling())
                    SetPlayerState(PlayerState.Falling);

                if (Input.GetKeyDown(KeyCode.LeftShift))
                    SetPlayerState(PlayerState.Dashing);

                break;
            case PlayerState.ShortHop:
                if (IsFalling())
                    SetPlayerState(PlayerState.Falling);

                if (Input.GetKeyDown(KeyCode.LeftShift))
                    SetPlayerState(PlayerState.Dashing);

                if (IsOnGround())
                    SetPlayerState(PlayerState.Idle);

                break;
            case PlayerState.Falling:
                if (IsOnGround())
                    SetPlayerState(PlayerState.Idle);

                if (Input.GetKeyDown(KeyCode.LeftShift))
                    SetPlayerState(PlayerState.Dashing);

                break;
            case PlayerState.Running:
                if (Input.GetButtonDown("Jump"))
                    SetPlayerState(PlayerState.Jumping);

                if (Input.GetKeyDown(KeyCode.LeftShift))
                    SetPlayerState(PlayerState.Dashing);

                if (IsOnGround())
                    SetPlayerState(PlayerState.Idle);

                break;
            case PlayerState.Dashing:
                if (Input.GetButtonDown("Jump"))
                    SetPlayerState(PlayerState.Jumping);

                if (Input.GetKeyUp(KeyCode.LeftShift))
                    SetPlayerState(PlayerState.Running);

                if (IsFalling())
                    SetPlayerState(PlayerState.Falling);

                if (IsOnGround())
                    SetPlayerState(PlayerState.Idle);

                break;
        }
    }
    void Update()
    {
        PlayerStateMachine();

        if (wasPressed)
        {
            timeVar += Time.deltaTime;
        }

        if (flipVisual)
        {
            sprite.flipX = !sprite.flipX;
            flipVisual = false;
            dashForce *= -1;
        }
    }


    void SetPlayerState(PlayerState ps)
    {
        if (ps != currPlayerState)
            currPlayerState = ps;
            switch (ps)
            {
                case PlayerState.Idle:
                    break;
                case PlayerState.Jumping:
                    rb.linearVelocity = new Vector3
                        (
                            rb.linearVelocity.x,
                            0.0f,
                            rb.linearVelocity.z
                        );
                    rb.AddForce(Vector3.up * jumpForce);
                    break;
                case PlayerState.ShortHop:
                rb.linearVelocity = new Vector3
                    (
                        rb.linearVelocity.x,
                        rb.linearVelocity.y * 0.5f,
                        rb.linearVelocity.z
                    );
                break;
                case PlayerState.Falling:
                    break;
                case PlayerState.Running:
                    break;
                case PlayerState.Dashing:
                    rb.linearVelocity = new Vector3
                        (
                            dashForce,
                            rb.linearVelocity.y,
                            rb.linearVelocity.z
                        );
                    rb.AddForce(Vector3.right * dashForce);
                    break;
            }
    }

    bool IsOnGround()
    {
       return distanceToGround <= onGroundHeight;
    }
    bool IsFalling()
    {
        return rb.linearVelocity.y < 0.0f;
    }

    float TimeSinceLastButtonPress()
    {
        timeVar += Time.fixedDeltaTime;
        print(timeVar);
        return timeVar;
    }

}
