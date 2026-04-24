using UnityEngine;

public abstract class AnimalBase : MonoBehaviour
{
    [Header("Animal Stats")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public Sprite animalSprite;

    [Header("Abilities")]
    public bool canDash = false;
    public bool canWallJump = false;
    public bool canCarry = false;
    public bool canShrink = false;
    public bool canDoubleJump = false;
    public bool canGlide = false;

    public virtual void OnActivate(PlayerMovement player)
    {
        player.moveSpeed = moveSpeed;
        player.normalJumpForce = jumpForce;

        SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && animalSprite != null)
            sr.sprite = animalSprite;

        AbilityDash dash = player.GetComponent<AbilityDash>();
        if (dash != null)
        {
            dash.StopAllCoroutines();
            dash.isDashing = false;
            player.hasWallJumped = false;
            player.disableStateMachine = false;
            dash.enabled = canDash;
        }

        AbilityWallJump wallJump = player.GetComponent<AbilityWallJump>();
        if (wallJump != null)
        {
            wallJump.StopAllCoroutines();
            player.hasWallJumped = false;
            player.disableStateMachine = false;
            wallJump.enabled = canWallJump;
        }

        AbilityCarry carry = player.GetComponent<AbilityCarry>();
        if (carry != null)
        {
            carry.enabled = canCarry;
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

    public virtual void OnDeactivate(PlayerMovement player) { }
}