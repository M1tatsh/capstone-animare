using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;

public class RadialSelection : MonoBehaviour
{
    public Input menuButton;

    [Range(1,7)]
    public int numberOfRadialPart;
    public GameObject radialPartPrefab;
    public Transform cursorTransform;
    public float setGameSpeed = 0.1f;

    public UnityEvent<int> OnPartSelected;

    static float ANGLE_BETWEEN_PART = 10f;

    private List<GameObject> spawnedParts = new List<GameObject>();
    private int currentSelectedRadialPart = -1;
    private Vector3 mousePos;
    
    Transform canvas;
    void Start()
    {
        canvas = GetComponentInChildren<Canvas>().transform;
    }

    void Update()
    {
        SetSelectedRadialPart();

        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            SpawnRadialPart();
            //GameController.SetGameSpeed(setGameSpeed);
        }

        if (Input.GetKey(KeyCode.LeftAlt))
        {
            SetSelectedRadialPart();
        }

        if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            HideAndTriggerSelected();
            //GameController.ResetGameSpeed();
        }
    }

    public void HideAndTriggerSelected()
    {
        SoundManager.PlaySound(SoundType.TRANSFORM);

        OnPartSelected.Invoke(currentSelectedRadialPart);
        canvas.gameObject.SetActive(false);
    }

    public void SetSelectedRadialPart()
    {
        Vector3 centerToCursor = Input.mousePosition - canvas.position;
        Vector3 centerToCursorProjected = Vector3.ProjectOnPlane(centerToCursor, canvas.forward);

        float angle = Vector3.SignedAngle(canvas.up, centerToCursorProjected, -canvas.forward);

        if (angle < 0)
        {
            angle += 360;
        }

        currentSelectedRadialPart = (int)angle * numberOfRadialPart / 360;

        for (int i = 0; i < spawnedParts.Count; i++)
        {
            if (i == currentSelectedRadialPart)
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

        for (int i = 0; i < numberOfRadialPart; i++)
        {
            float angle = -i * 360 / numberOfRadialPart - ANGLE_BETWEEN_PART / 2;
            Vector3 radialPartEulerAngle = new Vector3
            (
                    0,
                    0,
                    angle
            );

            GameObject spawnedRadialPart = Instantiate(radialPartPrefab, canvas);
            spawnedRadialPart.transform.position = canvas.position;
            spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;

            spawnedRadialPart.GetComponent<Image>().fillAmount = (1 / (float)numberOfRadialPart) - (ANGLE_BETWEEN_PART / 360);

            spawnedParts.Add(spawnedRadialPart);
        }
    }
}
