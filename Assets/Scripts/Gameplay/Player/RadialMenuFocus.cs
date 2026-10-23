using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Gameplay.Player
{
    public class RadialMenuFocus : MonoBehaviour
    {
        [SerializeField] private float transitionTime = 0.15f;

        [Header("Zoom")]
        [SerializeField] private float zoomScale = 0.85f;

        [Header("Vignette")]
        [SerializeField] private Color vignetteColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField][Range(0f, 1f)] private float vignetteStart = 0.35f;
        [SerializeField] private bool pixelatedVignette = true;

        private const int PixelatedWidth = 64;
        private const int PixelatedHeight = 36;
        private const int SmoothWidth = 256;
        private const int SmoothHeight = 144;

        private RadialSelection radialSelection;
        private RawImage vignetteImage;
        private Texture2D vignetteTexture;
        private float blend = 0f;

        private readonly List<Camera> zoomCameras = new List<Camera>();
        private readonly List<float> baseZooms = new List<float>();

        void Awake()
        {
            radialSelection = GetComponent<RadialSelection>();
            Canvas canvas = GetComponentInParent<Canvas>();

            if (radialSelection == null || canvas == null)
            {
                enabled = false;
                return;
            }

            CreateVignette(canvas.transform);
        }

        void Update()
        {
            float target = radialSelection.IsMenuOpen ? 1f : 0f;

            if (blend == target)
            {
                return;
            }

            if (blend <= 0f)
            {
                CaptureCameras();
            }

            blend = Mathf.MoveTowards(blend, target, Time.unscaledDeltaTime / Mathf.Max(transitionTime, 0.01f));
            ApplyBlend();

            if (blend <= 0f)
            {
                RestoreCameras();
            }
        }

        void OnDisable()
        {
            blend = 0f;
            RestoreCameras();

            if (vignetteImage != null)
            {
                vignetteImage.enabled = false;
            }
        }

        void OnDestroy()
        {
            if (vignetteTexture != null)
            {
                Destroy(vignetteTexture);
            }
        }

        private void ApplyBlend()
        {
            float eased = blend * blend * (3f - 2f * blend);
            float zoom = Mathf.Lerp(1f, zoomScale, eased);

            for (int i = 0; i < zoomCameras.Count; i++)
            {
                Camera zoomCamera = zoomCameras[i];

                if (zoomCamera == null)
                {
                    continue;
                }

                if (zoomCamera.orthographic)
                {
                    zoomCamera.orthographicSize = baseZooms[i] * zoom;
                }
                else
                {
                    zoomCamera.fieldOfView = baseZooms[i] * zoom;
                }
            }

            if (vignetteImage != null)
            {
                vignetteImage.enabled = eased > 0f;
                vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteColor.a * eased);
            }
        }

        private void CaptureCameras()
        {
            zoomCameras.Clear();
            baseZooms.Clear();

            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                return;
            }

            AddZoomCamera(mainCamera);

            UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();

            if (cameraData.renderType == CameraRenderType.Base && cameraData.cameraStack != null)
            {
                foreach (Camera stackedCamera in cameraData.cameraStack)
                {
                    AddZoomCamera(stackedCamera);
                }
            }
        }

        private void AddZoomCamera(Camera zoomCamera)
        {
            if (zoomCamera == null || zoomCameras.Contains(zoomCamera))
            {
                return;
            }

            zoomCameras.Add(zoomCamera);
            baseZooms.Add(zoomCamera.orthographic ? zoomCamera.orthographicSize : zoomCamera.fieldOfView);
        }

        private void RestoreCameras()
        {
            for (int i = 0; i < zoomCameras.Count; i++)
            {
                Camera zoomCamera = zoomCameras[i];

                if (zoomCamera == null)
                {
                    continue;
                }

                if (zoomCamera.orthographic)
                {
                    zoomCamera.orthographicSize = baseZooms[i];
                }
                else
                {
                    zoomCamera.fieldOfView = baseZooms[i];
                }
            }

            zoomCameras.Clear();
            baseZooms.Clear();
        }

        private void CreateVignette(Transform canvasTransform)
        {
            vignetteTexture = BuildVignetteTexture();

            GameObject vignetteObject = new GameObject("MenuVignette", typeof(RectTransform), typeof(RawImage));
            vignetteObject.layer = canvasTransform.gameObject.layer;
            vignetteObject.transform.SetParent(canvasTransform, false);
            vignetteObject.transform.SetAsFirstSibling();

            RectTransform rectTransform = vignetteObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            vignetteImage = vignetteObject.GetComponent<RawImage>();
            vignetteImage.texture = vignetteTexture;
            vignetteImage.raycastTarget = false;
            vignetteImage.enabled = false;
        }

        private Texture2D BuildVignetteTexture()
        {
            int width = pixelatedVignette ? PixelatedWidth : SmoothWidth;
            int height = pixelatedVignette ? PixelatedHeight : SmoothHeight;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = pixelatedVignette ? FilterMode.Point : FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 offset = new Vector2((x + 0.5f) / width * 2f - 1f, (y + 0.5f) / height * 2f - 1f);
                    float distance = offset.magnitude / Mathf.Sqrt(2f);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(vignetteStart, 1f, distance));
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}