using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private enum PlayerState
    {
        Grounded,
        Jumping,
        Airborn,
        Wall,
        Dashing,
    }


    [SerializeField] PlayerState currPS = PlayerState.Airborn;
    private Rigidbody rb;
    private PlayerCollision collision;

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float normalJumpForce = 5f;
    public float wallJumpForce = 10f;
    public float slideForce = 2.5f;
    public float dashSpeed = 5.0f;
    public float dashTime = 0.2f;

    [Header("Toggles")]
    private bool canMove = true;
    private bool isDashing = false;
    public bool hasWallJumped = false;
    public bool disableStateMachine = false;
    public int wallSide = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        collision = GetComponent<PlayerCollision>();
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxis("Horizontal");
        float speedX = Mathf.Abs(rb.linearVelocity.x);

        Walk(moveX);
    }

    private void Update()
    {
        PlayerStateMachine();
    }

    private void PlayerStateMachine()
    {
        if (!disableStateMachine)
        {
            switch (currPS)
            {
                case PlayerState.Grounded:

                    if (Input.GetButtonDown("Jump"))
                        SetPlayerState(PlayerState.Jumping);

                    if (Input.GetButtonDown("Fire3"))
                        SetPlayerState(PlayerState.Dashing);

                    if (!OnGround() && !OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Airborn);

                    break;

                case PlayerState.Jumping:
                    if (OnGround())
                        SetPlayerState(PlayerState.Grounded);

                    if (!OnGround() && !OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Airborn);

                    if (Input.GetButtonDown("Fire3"))
                        SetPlayerState(PlayerState.Dashing);

                    break;

                case PlayerState.Airborn:
                    if (OnGround())
                        SetPlayerState(PlayerState.Grounded);

                    if (OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Wall);

                    if (Input.GetButtonDown("Fire3"))
                        SetPlayerState(PlayerState.Dashing);

                    break;

                case PlayerState.Wall:
                    WallSlide();

                    if (OnGround())
                        SetPlayerState(PlayerState.Grounded);

                    if (Input.GetButtonDown("Jump") && OnWallGeneric(OnWallLeft(), OnWallRight()))
                    {
                        SetPlayerState(PlayerState.Jumping);
                    }

                    if (!OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Airborn);

                    break;

                case PlayerState.Dashing:

                    if (!isDashing)
                    {
                        if (OnWallGeneric(OnWallLeft(), OnWallRight()))
                            SetPlayerState(PlayerState.Wall);
                        else if (OnGround())
                            SetPlayerState(PlayerState.Grounded);
                        else
                            SetPlayerState(PlayerState.Airborn);
                    }

                    break;
            }
        }
    }

    private void SetPlayerState(PlayerState ps)
    {
        if (ps != currPS)
        {
            currPS = ps;

            switch (currPS)
            {
                case PlayerState.Grounded:
                    break;
                case PlayerState.Jumping:
                    if (OnWallGeneric(OnWallLeft(), OnWallRight()))
                    {
                        WallJump();
                    }
                    else
                    {
                        Jump(Vector3.up, normalJumpForce);
                    }
                    break;
                case PlayerState.Airborn:
                    break;
                case PlayerState.Dashing:
                    float x = Input.GetAxisRaw("Horizontal");
                    float y = Input.GetAxisRaw("Vertical");

                    if (x != 0 || y != 0)
                    {
                        Dash(x, y);
                    }
                    break;
            }
        }
    }

    private void Walk(float x)
    {
        if (!hasWallJumped)
        {
            rb.linearVelocity = (new Vector3(x * moveSpeed, rb.linearVelocity.y, 0));
            print(rb.linearVelocity);
        }
        else
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, (new Vector3(x * moveSpeed, rb.linearVelocity.y, 0)), .5f * Time.deltaTime);
        }

    }

    private void Jump(Vector3 dir, float jumpForce)
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, 0);
        rb.linearVelocity += dir * jumpForce;
    }

    private void Dash(float x, float y)
    {
        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(dashTime));

        rb.linearVelocity = Vector3.zero;
        Vector3 dir = new Vector3(x, y, 0);

        rb.linearVelocity += dir.normalized * dashSpeed;
    }

    IEnumerator DisableMovement(float time)
    {
        hasWallJumped = true;
        disableStateMachine = true;
        isDashing = true;
        yield return new WaitForSeconds(time);
        hasWallJumped = false;
        disableStateMachine = false;
        isDashing = false;
    }

    private bool PlayerIsIdle()
    {
        if (rb.linearVelocity == Vector3.zero)
            return true;

        return false;
    }

    private bool OnGround()
    {
        return collision.onGround;
    }
    private bool OnWallRight()
    {
        return collision.onWallRight;
    }
    private bool OnWallLeft()
    {
        return collision.onWallLeft;
    }

    private bool OnWallGeneric(bool f, bool b)
    {
        if (f || b)
            return true;

        return false;
    }

    private void WallSlide()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, -slideForce, rb.linearVelocity.z);
    }

    private void WallJump()
    {
        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(0.1f));

        Vector3 wallDir = collision.onWallRight ? Vector3.left : Vector3.right;

        Jump((Vector3.up / 1.5f + wallDir / 1.5f), wallJumpForce);
    }
}
