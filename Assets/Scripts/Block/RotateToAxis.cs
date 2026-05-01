using System;
using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class RotateToAxis : MonoBehaviour
{
    public float verticalOffset = 1f;
    public bool triggerIsActive = true;
    public float deactiveTime = 10f;

    public bool flipperBool = false;

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
        if (other.TryGetComponent(out RotationHandler rt) && triggerIsActive)
        {
            triggerIsActive = false;

            if (!flipperBool)
            {
                rt.RotatePlayer(GetPosition(), -90f);

                
            }
            else if(flipperBool)
            {
                rt.RotatePlayer(GetPosition(), 90f);
            }

            flipperBool = !flipperBool;
            StopCoroutine(DisableTrigger(0));
            StartCoroutine(DisableTrigger(deactiveTime));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<RotationHandler>() != null)
        {
            triggerIsActive = true;
        }
    }

    private IEnumerator DisableTrigger(float time)
    {
        yield return new WaitWhile(() => !triggerIsActive);
        //yield return new WaitForSeconds(time);

    }

}
