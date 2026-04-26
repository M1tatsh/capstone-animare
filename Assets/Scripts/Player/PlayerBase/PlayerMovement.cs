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
    private AbilityDash abilityDash;
    private AbilityWallJump abilityWallJump;
    private AbilityDoubleJump abilityDoubleJump;

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float maxWalkSpeed = 10f;
    public float normalJumpForce = 5f;

    [Header("Toggles")]
    public bool hasWallJumped = false;
    public bool disableStateMachine = false;
    public bool movingOnZ;
    public bool isFacingLeft = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        collision = GetComponent<PlayerCollision>();
        abilityDash = GetComponent<AbilityDash>();
        abilityWallJump = GetComponent<AbilityWallJump>();
        abilityDoubleJump = GetComponent<AbilityDoubleJump>();
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxis("Horizontal");
        Walk(moveX);
        FaceDirection(moveX);
    }

    private void Update()
    {
        SetMovingOnZ();

        ChangeConstraints(!movingOnZ);

        PlayerStateMachine();

        if (isFacingLeft)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().flipX = true;
        }
        else
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().flipX = false;
        }
    }

    private bool HasDash() => abilityDash != null && abilityDash.enabled;
    private bool HasWallJump() => abilityWallJump != null && abilityWallJump.enabled;
    private bool HasDoubleJump() => abilityDoubleJump != null && abilityDoubleJump.enabled;

    private void PlayerStateMachine()
    {
        if (!disableStateMachine)
        {
            switch (currPS)
            {
                case PlayerState.Grounded:
                    if (Input.GetButtonDown("Jump"))
                        SetPlayerState(PlayerState.Jumping);

                    if (HasDash() && Input.GetButtonDown("Fire3"))
                        SetPlayerState(PlayerState.Dashing);

                    if (!OnGround() && !OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Airborn);
                    break;

                case PlayerState.Jumping:
                    if (OnGround())
                        SetPlayerState(PlayerState.Grounded);

                    if (!OnGround() && !OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Airborn);

                    if (HasDash() && Input.GetButtonDown("Fire3"))
                        SetPlayerState(PlayerState.Dashing);

                    if (Input.GetButtonDown("Jump") && HasDoubleJump())
                        abilityDoubleJump.TryDoubleJump();

                    break;

                case PlayerState.Airborn:
                    if (OnGround())
                        SetPlayerState(PlayerState.Grounded);

                    if (HasWallJump() && OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Wall);

                    if (HasDash() && Input.GetButtonDown("Fire3"))
                        SetPlayerState(PlayerState.Dashing);

                    if (Input.GetButtonDown("Jump") && HasDoubleJump())
                        abilityDoubleJump.TryDoubleJump();

                    break;

                case PlayerState.Wall:
                    if (HasWallJump())
                        abilityWallJump.WallSlide();

                    if (OnGround())
                        SetPlayerState(PlayerState.Grounded);

                    if (Input.GetButtonDown("Jump") && OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Jumping);

                    if (!OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Airborn);
                    break;

                case PlayerState.Dashing:
                    if (HasDash() && !abilityDash.isDashing)
                    {
                        if (HasWallJump() && OnWallGeneric(OnWallLeft(), OnWallRight()))
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
                    if (HasWallJump() && OnWallGeneric(OnWallLeft(), OnWallRight()))
                        abilityWallJump.Execute();
                    else
                        Jump(Vector3.up, normalJumpForce);
                    break;
                case PlayerState.Airborn:
                    break;
                case PlayerState.Dashing:
                    if (HasDash())
                    {
                        float x = Input.GetAxisRaw("Horizontal");
                        if (x != 0)
                        {
                            abilityDash.Execute(x);
                        }
                    }
                    break;
            }
        }
    }

    private void Walk(float x)
    {
        if (movingOnZ)
        {
            x = -x;
        }

        if (!hasWallJumped && !abilityDash.isDashing)
        {
            rb.linearVelocity = transform.InverseTransformDirection(new Vector3(x * moveSpeed, rb.linearVelocity.y, 0));
        }
        else
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(x * moveSpeed, rb.linearVelocity.y, 0), .5f * Time.deltaTime);
    }

    private void Jump(Vector3 dir, float jumpForce)
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, 0);
        rb.linearVelocity += dir * jumpForce;
    }

    private bool OnGround() => collision.onGround;
    private bool OnWallRight() => collision.onWallRight;
    private bool OnWallLeft() => collision.onWallLeft;
    private bool OnWallGeneric(bool f, bool b) => f || b;

    private void ChangeConstraints(bool def)
    {
        if (def)
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        else
        {
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    public float GetDirectionalAxis()
    {
        return rb.rotation.y;
    }

    public void SetPositionToWholeNumber(bool zAxis)
    {
        Vector3 currPos = transform.position;

        if (zAxis)
            transform.position = new Vector3(Mathf.Round(currPos.x), currPos.y, currPos.z);
        else
            transform.position = new Vector3(currPos.x, currPos.y, Mathf.Round(currPos.z));
    }

    private void FaceDirection(float direction)
    {
        if (direction > 0)
        {
            isFacingLeft = false;
        }
        if (direction < 0)
        {
            isFacingLeft = true;
        }
    }

    private void SetMovingOnZ()
    {
        movingOnZ = (transform.rotation.eulerAngles.y == Mathf.Abs(90f) || transform.rotation.eulerAngles.y == Mathf.Abs(270f)) ? true : false;
    }
}