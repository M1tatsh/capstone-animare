using System;
using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class RotateToAxis : MonoBehaviour
{
    public float verticalOffset = 1f;
    public bool triggerIsActive = false;

    void Start()
    {

    }

    void Update()
    {

    }

    public Vector3 GetPosition()
    {
        return transform.position + transform.up * verticalOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out RotationHandler rt))
        {
            triggerIsActive = true;
            rt.RotatePlayer(GetPosition());
            StartCoroutine(DisableTrigger());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<RotationHandler>() != null)
        {
            triggerIsActive = false;
        }
    }

    private IEnumerator DisableTrigger()
    {
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitWhile(() => triggerIsActive);
        print("Now Active!");
        GetComponent<SphereCollider>().enabled = true;

    }

}
