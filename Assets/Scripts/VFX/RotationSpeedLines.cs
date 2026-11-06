using Gameplay.Rotation;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.VFX
{
    public class RotationSpeedLines : MonoBehaviour
    {
        [SerializeField] private int lineCount = 24;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Vector2 lengthRange = new Vector2(0.15f, 0.45f);
        [SerializeField] private Vector2 thicknessRange = new Vector2(1f, 3f);
        [SerializeField] private Vector2 speedRange = new Vector2(2.5f, 5f);
        [SerializeField] private Vector2 alphaRange = new Vector2(0.3f, 1f);
        [SerializeField] private int pixelSize = 2;
        [SerializeField] private float peakAngularSpeed = 250f;
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private bool invertDirection = false;
        [SerializeField] private int sortingOrder = 20;

        private Canvas canvas;
        private RectTransform[] lines;
        private Image[] lineImages;
        private float[] lineX;
        private float[] lineY;
        private float[] lineLengths;
        private float[] lineThicknesses;
        private float[] lineSpeeds;
        private float[] lineAlphas;
        private Camera viewCamera;
        private ViewRotator viewRotator;
        private float lastYaw;
        private float intensity;
        private float direction = -1f;
        private bool hasYaw;

        void Start()
        {
            viewRotator = FindAnyObjectByType<ViewRotator>();

            GameObject canvasObject = new GameObject("RotationSpeedLines", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = true;
            canvas.enabled = false;

            int count = Mathf.Max(0, lineCount);
            lines = new RectTransform[count];
            lineImages = new Image[count];
            lineX = new float[count];
            lineY = new float[count];
            lineLengths = new float[count];
            lineThicknesses = new float[count];
            lineSpeeds = new float[count];
            lineAlphas = new float[count];

            for (int i = 0; i < count; i++)
            {
                GameObject lineObject = new GameObject("Line " + i, typeof(RectTransform), typeof(Image));
                lineObject.transform.SetParent(canvasObject.transform, false);

                RectTransform line = lineObject.GetComponent<RectTransform>();
                line.anchorMin = Vector2.zero;
                line.anchorMax = Vector2.zero;
                line.pivot = new Vector2(0.5f, 0.5f);

                Image image = lineObject.GetComponent<Image>();
                image.raycastTarget = false;

                lines[i] = line;
                lineImages[i] = image;
            }
        }

        void LateUpdate()
        {
            if (viewCamera == null)
            {
                viewCamera = Camera.main;

                if (viewCamera == null)
                {
                    return;
                }

                hasYaw = false;
            }

            float yaw = viewCamera.transform.eulerAngles.y;
            float yawChange = hasYaw ? Mathf.DeltaAngle(lastYaw, yaw) : 0f;
            lastYaw = yaw;
            hasYaw = true;

            float targetIntensity = 0f;
            bool isRotating = viewRotator != null && viewRotator.IsRotating;

            if (isRotating && Time.deltaTime > 0f && Mathf.Abs(yawChange) > 0.001f)
            {
                targetIntensity = Mathf.Clamp01(Mathf.Abs(yawChange) / Time.deltaTime / peakAngularSpeed);
                direction = -Mathf.Sign(yawChange) * (invertDirection ? -1f : 1f);
            }

            bool wasHidden = intensity <= 0f;
            intensity = Mathf.MoveTowards(intensity, targetIntensity, fadeSpeed * Time.deltaTime);

            if (intensity <= 0f)
            {
                canvas.enabled = false;
                return;
            }

            if (wasHidden)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    ResetLine(i, Random.Range(0f, Screen.width));
                }
            }

            canvas.enabled = true;
            float screenWidth = Screen.width;

            for (int i = 0; i < lines.Length; i++)
            {
                lineX[i] += direction * lineSpeeds[i] * screenWidth * Time.deltaTime;

                float halfLength = lineLengths[i] * 0.5f;

                if (direction < 0f && lineX[i] + halfLength < 0f)
                {
                    ResetLine(i, screenWidth + halfLength);
                }
                else if (direction > 0f && lineX[i] - halfLength > screenWidth)
                {
                    ResetLine(i, -halfLength);
                }

                lines[i].anchoredPosition = new Vector2(Snap(lineX[i]), Snap(lineY[i]));
                lines[i].sizeDelta = new Vector2(Snap(lineLengths[i]), lineThicknesses[i]);

                Color color = lineColor;
                color.a = lineColor.a * lineAlphas[i] * intensity;
                lineImages[i].color = color;
            }
        }

        private void ResetLine(int i, float x)
        {
            float step = Mathf.Max(1, pixelSize);

            lineLengths[i] = Random.Range(lengthRange.x, lengthRange.y) * Screen.width;
            lineThicknesses[i] = Mathf.Max(1, Mathf.RoundToInt(Random.Range(thicknessRange.x, thicknessRange.y))) * step;
            lineSpeeds[i] = Random.Range(speedRange.x, speedRange.y);
            lineAlphas[i] = Random.Range(alphaRange.x, alphaRange.y);
            lineY[i] = Random.Range(0f, Screen.height);
            lineX[i] = x;
        }

        private float Snap(float value)
        {
            float step = Mathf.Max(1, pixelSize);
            return Mathf.Round(value / step) * step;
        }
    }
}
