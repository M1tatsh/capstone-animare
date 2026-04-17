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

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float maxWalkSpeed = 10f;
    public float normalJumpForce = 5f;

    [Header("Toggles")]
    public bool hasWallJumped = false;
    public bool disableStateMachine = false;
    public bool SetPlayerXToZ = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        collision = GetComponent<PlayerCollision>();
        abilityDash = GetComponent<AbilityDash>();
        abilityWallJump = GetComponent<AbilityWallJump>();
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxis("Horizontal");
        Walk(moveX);
    }

    private void Update()
    {
        PlayerStateMachine();

        if (Input.GetButtonDown("Fire2"))
        {
            SetPlayerRotation(Input.GetAxis("Fire2"));
            SetPlayerXToZ = !SetPlayerXToZ;
        }

        ChangeConstraints(!SetPlayerXToZ);
    }

    private bool HasDash() => abilityDash != null && abilityDash.enabled;
    private bool HasWallJump() => abilityWallJump != null && abilityWallJump.enabled;

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
                    break;

                case PlayerState.Airborn:
                    if (OnGround())
                        SetPlayerState(PlayerState.Grounded);

                    if (HasWallJump() && OnWallGeneric(OnWallLeft(), OnWallRight()))
                        SetPlayerState(PlayerState.Wall);

                    if (HasDash() && Input.GetButtonDown("Fire3"))
                        SetPlayerState(PlayerState.Dashing);
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

    private void SetPlayerRotation(float f)
    {
        if (f > 0)
            rb.MoveRotation(Quaternion.Euler(0, rb.rotation.eulerAngles.y + 90, 0));
        else if (f < 0)
            rb.MoveRotation(Quaternion.Euler(0, rb.rotation.eulerAngles.y - 90, 0));
    }

    private void Walk(float x)
    {
        if (SetPlayerXToZ)
        {
            x = -x;
        }

        if (currPS != PlayerState.Wall)
        {
            if ((x > 0 && OnWallRight()) || (x < 0 && OnWallLeft()))
                x = 0;
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
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    public float GetDirectionalAxis()
    {
        return rb.rotation.y;
    }
}