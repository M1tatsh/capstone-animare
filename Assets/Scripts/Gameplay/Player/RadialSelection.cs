using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Gameplay.Player
{
    public class RadialSelection : MonoBehaviour
    {
        [Range(1, 6)]
        public int numberOfRadialPart = 6;
        public GameObject radialPartPrefab;
        public GameObject centerPartPrefab;

        public Transform canvasTransform;

        public float slowTimeScale = 0.1f;
        public float resumeDelay = 0.2f;
        public UnityEvent<int> OnPartSelected;

        static float ANGLE_BETWEEN_PART = 10f;

        private List<GameObject> spawnedParts = new List<GameObject>();
        private GameObject spawnedCenterPart;
        private int currentSelectedRadialPart = -1;
        private float originalTimeScale = 1f; 
        private float originalFixedDeltaTime;

        private bool isMenuOpen = false;

        void Start()
        {
            if (canvasTransform != null)
            {
                canvasTransform.gameObject.SetActive(false);
            }

            
            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;
        }

        void Update()
        {
            if (isMenuOpen)
            {
                SetSelectedRadialPart();
            }
        }

        public void SetMenuState(bool isPressed)
        {
            if (isPressed)
            {
                OpenMenu();
            }
            else
            {
                if (isMenuOpen) HideAndTriggerSelected();
            }
        }

        private void OpenMenu()
        {
            isMenuOpen = true;
            StopAllCoroutines();
            SpawnRadialPart();

            

            Time.timeScale = slowTimeScale;

      
            Time.fixedDeltaTime = originalFixedDeltaTime * slowTimeScale;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void HideAndTriggerSelected()
        {
            isMenuOpen = false;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            OnPartSelected.Invoke(currentSelectedRadialPart);

            if (canvasTransform != null)
            {
                canvasTransform.gameObject.SetActive(false);
            }

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
            if (canvasTransform == null) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Vector3 centerToCursor3D = (Vector3)mousePosition - canvasTransform.position;
            Vector3 centerToCursorProjected = Vector3.ProjectOnPlane(centerToCursor3D, canvasTransform.forward);
            float distance = centerToCursorProjected.magnitude;

            if (distance < 60f)
            {
                currentSelectedRadialPart = 0;
            }
            else
            {
                float angle = Vector3.SignedAngle(canvasTransform.up, centerToCursorProjected, -canvasTransform.forward);
                if (angle < 0)
                {
                    angle += 360;
                }
                currentSelectedRadialPart = (int)(angle * numberOfRadialPart / 360) + 1;

                if (currentSelectedRadialPart > numberOfRadialPart)
                {
                    currentSelectedRadialPart = 1;
                }
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (spawnedCenterPart != null)
            {
                if (currentSelectedRadialPart == 0)
                {
                    spawnedCenterPart.GetComponent<Image>().color = Color.gray;
                    spawnedCenterPart.transform.localScale = 1.1f * Vector3.one;
                }
                else
                {
                    spawnedCenterPart.GetComponent<Image>().color = Color.white;
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
                    spawnedParts[i].GetComponent<Image>().color = Color.white;
                    spawnedParts[i].transform.localScale = Vector3.one;
                }
            }
        }

        public void SpawnRadialPart()
        {
            if (canvasTransform == null) return;
            canvasTransform.gameObject.SetActive(true);

            foreach (var item in spawnedParts)
            {
                Destroy(item);
            }
            spawnedParts.Clear();

            if (spawnedCenterPart != null)
            {
                Destroy(spawnedCenterPart);
            }

            spawnedCenterPart = Instantiate(centerPartPrefab, canvasTransform);
            spawnedCenterPart.transform.position = canvasTransform.position;
            spawnedCenterPart.transform.localScale = Vector3.one;

            for (int i = 0; i < numberOfRadialPart; i++)
            {
                float angle = -i * 360 / numberOfRadialPart - ANGLE_BETWEEN_PART / 2;
                Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

                GameObject spawnedRadialPart = Instantiate(radialPartPrefab, canvasTransform);
                spawnedRadialPart.transform.position = canvasTransform.position;
                spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;

                spawnedRadialPart.GetComponent<Image>().fillAmount =
                    (1 / (float)numberOfRadialPart) - (ANGLE_BETWEEN_PART / 360);

                spawnedParts.Add(spawnedRadialPart);
            }
        }
    }
}