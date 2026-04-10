using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform currentCube;
    void Start()
    {
        PlayerRayCast();
    }

    // Update is called once per frame
    public void PlayerRayCast()
    {
        Ray pRay = new Ray(transform.GetChild(0).position, -transform.up);
        RaycastHit pHit;

        if (Physics.Raycast(pRay,out pHit))
        {
            if (pHit.transform.GetComponent<Walkable>() != null)
            {
                currentCube = pHit.transform;
            }
        }
    }
}
