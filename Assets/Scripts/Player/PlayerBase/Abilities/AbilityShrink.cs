using UnityEngine;

public class AbilityShrink : MonoBehaviour
{
    public float shrinkRatio = 0.5f;
    private Vector3 originalScale;
    private bool isShrunk = false;

    private void OnEnable()
    {
        originalScale = transform.localScale;
        isShrunk = false;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
        isShrunk = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (isShrunk)
            {
                transform.localScale = originalScale;
                isShrunk = false;
            }
            else
            {
                transform.localScale = originalScale * shrinkRatio;
                isShrunk = true;
            }
        }
    }
}