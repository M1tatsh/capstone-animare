using UnityEngine;
using Gameplay.Player;

public abstract class AnimalBase : MonoBehaviour
{
    [Header("Animal Stats")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float height = 1f;
    public float colliderOffset = 0;
    public float sphereColliderSize = 0f;
    public Vector3 carryOffset = Vector3.zero;

    [Header("Abilities")]
    public bool canDash = false;
    public bool canWallJump = false;
    public bool canCarry = false;
    public bool canShrink = false;
    public bool canDoubleJump = false;
    public bool canGlide = false;

    [Header("Sprite Sheet")]
    public RuntimeAnimatorController animator;
    public Transform childTransform = null;
    public virtual void OnActivate(PlayerMovement player)
    {
        SetColliderHeight(height);
        SetSpriteOffset(colliderOffset);
        SetSphereColliderSize(sphereColliderSize);

        player.MoveSpeed = moveSpeed;
        player.JumpForce = jumpForce;

        if (GetComponentInChildren<Animator>() != null)
        {
           GetComponentInChildren<Animator>().runtimeAnimatorController = animator as RuntimeAnimatorController;
        }

        AbilityDash dash = player.GetComponent<AbilityDash>();
        if (dash != null)
        {
            dash.StopAllCoroutines();
            dash.isDashing = false;
            //player.hasWallJumped = false;
            //player.disableStateMachine = false;
            dash.enabled = canDash;
        }

        AbilityWallJump wallJump = player.GetComponent<AbilityWallJump>();
        if (wallJump != null)
        {
            wallJump.StopAllCoroutines();
            //player.hasWallJumped = false;
            //player.disableStateMachine = false;
            wallJump.enabled = canWallJump;
        }

        AbilityCarry carry = player.GetComponent<AbilityCarry>();
        if (carry != null)
        {
            carry.enabled = canCarry;
            carry.offset = carryOffset;
        }

        AbilityShrink shrink = player.GetComponent<AbilityShrink>();
        if (shrink != null)
        {
            shrink.enabled = canShrink;
        }

        AbilityDoubleJump doubleJump = player.GetComponent<AbilityDoubleJump>();
        if (doubleJump != null)
        {
            doubleJump.enabled = canDoubleJump;
        }

        AbilityGlide glide = player.GetComponent<AbilityGlide>();
        if (glide != null)
        {
            glide.enabled = canGlide;
        }
    }

    protected virtual void SetColliderHeight(float colliderHeight)
    {
        if (TryGetComponent<CapsuleCollider>(out CapsuleCollider coll))
        {
            coll.height = colliderHeight;
        }
    }

    protected virtual void SetSpriteOffset(float offset)
    {
        if (childTransform != null)
            childTransform.localPosition = new Vector3(0, offset, 0);
    }

    protected virtual void SetSphereColliderSize(float size)
    {
        if (TryGetComponent<SphereCollider>(out SphereCollider coll))
        {
            coll.radius = size;
        }
    }

    public virtual void OnDeactivate(PlayerMovement player) { }
}