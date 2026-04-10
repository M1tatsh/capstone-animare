using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Walkable : MonoBehaviour
{
    public List<WalkPath> paths = new List<WalkPath>();

    public Transform previousBlock;

    public bool isStair = false;

    public float walkPointOffset = .5f;
    public float stairOffset = .4f;

    public Vector3 GetWalk()
    {
        float stair = isStair ? stairOffset : 0;
        return transform.position + transform.up * walkPointOffset - transform.up * stair;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        float stair = isStair ? stairOffset : 0;
        Gizmos.DrawSphere(GetWalk(), .09f);
    }

    [System.Serializable]
    public class WalkPath
    {
        public Transform target;
        public bool active = true;
    }

    
}
