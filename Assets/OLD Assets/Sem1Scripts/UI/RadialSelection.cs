using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;

public class RadialSelection : MonoBehaviour
{
    public Input menuButton;
    [Range(1, 7)]
    public int numberOfRadialPart = 6;
    public GameObject radialPartPrefab;
    public GameObject centerPartPrefab;
    public Transform cursorTransform;
    public float slowTimeScale = 0.1f;
    public float resumeDelay = 0.2f;
    public UnityEvent<int> OnPartSelected;

    static float ANGLE_BETWEEN_PART = 10f;

    private List<GameObject> spawnedParts = new List<GameObject>();
    private GameObject spawnedCenterPart;
    private int currentSelectedRadialPart = -1;
    private float originalTimeScale;
    private float originalFixedDeltaTime;

    Transform canvas;

    void Start()
    {
        canvas = GetComponentInChildren<Canvas>().transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            StopAllCoroutines();
            SpawnRadialPart();
            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = slowTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime * slowTimeScale;
        }
        if (Input.GetMouseButton(1))
        {
            SetSelectedRadialPart();
        }
        if (Input.GetMouseButtonUp(1))
        {
            HideAndTriggerSelected();
        }
    }

    public void HideAndTriggerSelected()
    {
        SoundManager.PlaySound(SoundType.TRANSFORM);
        OnPartSelected.Invoke(currentSelectedRadialPart);
        canvas.gameObject.SetActive(false);
        StartCoroutine(ResumeTimeAfterDelay());
    }

    private IEnumerator ResumeTimeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(resumeDelay);
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }

    public void SetSelectedRadialPart()
    {
        Vector3 centerToCursor = Input.mousePosition - canvas.position;
        Vector3 centerToCursorProjected = Vector3.ProjectOnPlane(centerToCursor, canvas.forward);
        float distance = centerToCursorProjected.magnitude;

        if (distance < 60f)
        {
            currentSelectedRadialPart = 0;
        }
        else
        {
            float angle = Vector3.SignedAngle(canvas.up, centerToCursorProjected, -canvas.forward);
            if (angle < 0)
            {
                angle += 360;
            }
            currentSelectedRadialPart = (int)(angle * numberOfRadialPart / 360) + 1;
        }

        if (spawnedCenterPart != null)
        {
            if (currentSelectedRadialPart == 0)
            {
                spawnedCenterPart.GetComponent<Image>().color = Color.gray;
                spawnedCenterPart.transform.localScale = 1.1f * Vector3.one;
            }
            else
            {
                spawnedCenterPart.GetComponent<Image>().color = Color.black;
                spawnedCenterPart.transform.localScale = Vector3.one;
            }
        }

        for (int i = 0; i < spawnedParts.Count; i++)
        {
            if (i == currentSelectedRadialPart - 1)
            {
                spawnedParts[i].GetComponent<Image>().color = Color.gray;
                spawnedParts[i].transform.localScale = 1.1f * Vector3.one;
            }
            else
            {
                spawnedParts[i].GetComponent<Image>().color = Color.black;
                spawnedParts[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void SpawnRadialPart()
    {
        canvas.gameObject.SetActive(true);

        foreach (var item in spawnedParts)
        {
            Destroy(item);
        }
        spawnedParts.Clear();

        if (spawnedCenterPart != null)
        {
            Destroy(spawnedCenterPart);
        }

        spawnedCenterPart = Instantiate(centerPartPrefab, canvas);
        spawnedCenterPart.transform.position = canvas.position;
        spawnedCenterPart.transform.localScale = Vector3.one;

        for (int i = 0; i < numberOfRadialPart; i++)
        {
            float angle = -i * 360 / numberOfRadialPart - ANGLE_BETWEEN_PART / 2;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

            GameObject spawnedRadialPart = Instantiate(radialPartPrefab, canvas);
            spawnedRadialPart.transform.position = canvas.position;
            spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;
            spawnedRadialPart.GetComponent<Image>().fillAmount =
                (1 / (float)numberOfRadialPart) - (ANGLE_BETWEEN_PART / 360);

            spawnedParts.Add(spawnedRadialPart);
        }
    }
}