using UnityEngine;
using System.Collections;

public class MoveToBlock : MonoBehaviour
{
    public WalkPath path = new WalkPath();

    public float walkPointOffset = 0.5f;
    public float castDistance = 2.0f;
    public float moveTime = 1.0f;
    bool targetIsNearby = false;


    void Update()
    {
        targetIsNearby = Physics.Raycast(transform.position, Vector3.up, castDistance);

        if (targetIsNearby)
        {
            MoveTarget(GameObject.FindGameObjectWithTag("Player"), GetEndPoint());
        }
    }

    public void MoveTarget(GameObject player, Vector3 desiredPosition)
    {
        if (!player.GetComponent<PlayerMovement>().setPlayerXToZ)
        {
            player.transform.position = new Vector3
            (
                player.transform.position.x,
                player.transform.position.y,
                desiredPosition.z
            );
        }
        else
        {
            player.transform.position = new Vector3
            (
                desiredPosition.x,
                player.transform.position.y,
                player.transform.position.z
            );
        }
    }

    public Vector3 GetStartPoint()
    {
        return transform.position + transform.up * walkPointOffset;
    }
    public Vector3 GetEndPoint()
    {
        return path.target.position + transform.up * walkPointOffset;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(GetStartPoint(), .1f);
        Gizmos.DrawLine(GetStartPoint(), GetStartPoint() + Vector3.up * castDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(GetStartPoint(), path.target.position + transform.up * walkPointOffset);

    }

    private bool CheckTag(string tag)
    {


        return false;
    }
}

[System.Serializable]

public class WalkPath
{
    public Transform target;
    public bool active = true;
}
