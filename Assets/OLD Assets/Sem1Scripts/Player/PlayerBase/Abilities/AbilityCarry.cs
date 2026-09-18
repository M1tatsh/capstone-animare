using UnityEngine;
using Gameplay.Player;

//BROKEN PLEASE FIX OR THROW AWAY
public class AbilityCarry : MonoBehaviour
{
    GameObject currentItem = null;
    public float throwStrength = 5.0f;
    private bool playerWithinRange;
    public bool drawDebug = true;
    public Vector3 offset;

    Color debugCollisionColor = Color.yellow;

    void Update()
    {
        if (Input.GetButtonDown("Fire3") && playerWithinRange && transform.childCount <= 1)
        {
            CarryItem();
        }
        else if (Input.GetButtonDown("Fire3") && currentItem != null)
        {
            Debug.Log(currentItem.name);
            currentItem.GetComponent<BlockCarryable>().ThrowBlock
            (
                GetPlayerIsOnZAXis(),
                PlayerIsFacingRight(),
                throwStrength
            );
        }
    }

    private void CarryItem()
    {
        currentItem.GetComponent<BlockCarryable>().SetOffset(offset);
        currentItem.transform.SetParent(transform, false);
    }

    private void OnDisable()
    {
        if (currentItem != null)
        {
            currentItem.transform.SetParent(null);
            currentItem = null;
            playerWithinRange = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Carryable") && currentItem == null)
        {
            playerWithinRange = true;
            currentItem = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Carryable") && currentItem != null)
        {
            playerWithinRange = false;
            currentItem = null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = debugCollisionColor;
        if (drawDebug)
        {
            Gizmos.DrawWireSphere(transform.position + offset, 0.05f);
        }
    }

    private bool GetPlayerIsOnZAXis()
    {
        //return GetComponent<PlayerMovement>().movingOnZ;
        return true;
    }

    private bool PlayerIsFacingRight()
    {
        //return !GetComponent<PlayerMovement>().isFacingLeft;
        return true;
    }
}