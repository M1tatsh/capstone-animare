using System.Collections.Generic;
using Gameplay.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.VFX
{
    [RequireComponent(typeof(PlayerFormManager))]
    public class TransformationBurst : MonoBehaviour
    {
        [Header("Flash")]
        [SerializeField] private Material flashMaterial;
        [SerializeField] private float flashSize = 1.25f;
        [SerializeField] private float flashDuration = 0.3f;
        [SerializeField] private float flashHeight = 0.35f;

        [Header("Smoke")]
        [SerializeField] private Material smokeMaterial;
        [SerializeField] private int smokeCount = 10;
        [SerializeField] private Vector2 smokeSizeRange = new Vector2(0.4f, 0.8f);
        [SerializeField] private float smokeGrowth = 1.8f;
        [SerializeField] private Vector2 smokeSpeedRange = new Vector2(1.5f, 4f);
        [SerializeField] private Vector2 smokeLifetimeRange = new Vector2(0.5f, 0.9f);
        [SerializeField] private float smokeSpreadAngle = 110f;
        [SerializeField] private float smokeDrag = 3f;
        [SerializeField] private Vector2 smokeSpinRange = new Vector2(-90f, 90f);

        [Header("Placement")]
        [SerializeField] private float depthOffset = -0.5f;

        private class Part
        {
            public Transform transform;
            public Renderer renderer;
            public Vector3 origin;
            public Vector2 offset;
            public Vector2 velocity;
            public float age;
            public float lifetime;
            public float startSize;
            public float endSize;
            public float angle;
            public float spin;
            public bool isFlash;
            public int colorId;
            public Color baseColor;
        }

        private readonly List<Part> parts = new List<Part>();
        private PlayerFormManager formManager;
        private MaterialPropertyBlock propertyBlock;
        private Mesh quad;
        private Camera viewCamera;
        private PlayerForm lastForm;
        private bool hasForm;

        void Awake()
        {
            formManager = GetComponent<PlayerFormManager>();
            propertyBlock = new MaterialPropertyBlock();
            quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        }

        void OnDestroy()
        {
            foreach (Part part in parts)
            {
                if (part.transform != null)
                {
                    Destroy(part.transform.gameObject);
                }
            }

            parts.Clear();
        }

        void LateUpdate()
        {
            if (!hasForm)
            {
                lastForm = formManager.CurrentForm;
                hasForm = true;
            }
            else if (formManager.CurrentForm != lastForm)
            {
                lastForm = formManager.CurrentForm;
                Play();
            }

            UpdateParts(Time.unscaledDeltaTime);
        }

        public void Play()
        {
            Vector3 origin = transform.position;

            if (flashMaterial != null)
            {
                Part flash = CreatePart("TransformFlash", flashMaterial, origin, 1);
                flash.isFlash = true;
                flash.offset = new Vector2(0f, flashHeight);
                flash.lifetime = Mathf.Max(0.01f, flashDuration);
                flash.startSize = flashSize * 0.4f;
                flash.endSize = flashSize;
            }

            if (smokeMaterial == null)
            {
                return;
            }

            for (int i = 0; i < smokeCount; i++)
            {
                Part smoke = CreatePart("TransformSmoke", smokeMaterial, origin, 0);

                float angle = Random.Range(-smokeSpreadAngle, smokeSpreadAngle) * Mathf.Deg2Rad;
                Vector2 moveDirection = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

                smoke.velocity = moveDirection * Random.Range(smokeSpeedRange.x, smokeSpeedRange.y);
                smoke.offset = moveDirection * 0.1f;
                smoke.lifetime = Random.Range(smokeLifetimeRange.x, smokeLifetimeRange.y);
                smoke.startSize = Random.Range(smokeSizeRange.x, smokeSizeRange.y);
                smoke.endSize = smoke.startSize * smokeGrowth;
                smoke.angle = Random.Range(0f, 360f);
                smoke.spin = Random.Range(smokeSpinRange.x, smokeSpinRange.y);
            }
        }

        private Part CreatePart(string partName, Material material, Vector3 origin, int sortingOrder)
        {
            GameObject partObject = new GameObject(partName, typeof(MeshFilter), typeof(MeshRenderer));
            partObject.layer = gameObject.layer;
            partObject.GetComponent<MeshFilter>().sharedMesh = quad;

            MeshRenderer meshRenderer = partObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = sortingOrder;

            int colorId = Shader.PropertyToID(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color");

            Part part = new Part
            {
                transform = partObject.transform,
                renderer = meshRenderer,
                origin = origin,
                colorId = colorId,
                baseColor = material.HasProperty(colorId) ? material.GetColor(colorId) : Color.white
            };

            parts.Add(part);
            return part;
        }

        private void UpdateParts(float deltaTime)
        {
            if (parts.Count == 0)
            {
                return;
            }

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            Transform cameraTransform = viewCamera != null ? viewCamera.transform : null;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            Vector3 up = cameraTransform != null ? cameraTransform.up : Vector3.up;
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Quaternion facing = cameraTransform != null ? cameraTransform.rotation : Quaternion.identity;

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                Part part = parts[i];
                part.age += deltaTime;

                float t = part.age / part.lifetime;

                if (t >= 1f || part.transform == null)
                {
                    if (part.transform != null)
                    {
                        Destroy(part.transform.gameObject);
                    }

                    parts.RemoveAt(i);
                    continue;
                }

                float fade;
                float size;

                if (part.isFlash)
                {
                    size = Mathf.Lerp(part.startSize, part.endSize, 1f - (1f - t) * (1f - t));
                    fade = 1f - t * t;
                }
                else
                {
                    part.velocity *= Mathf.Exp(-smokeDrag * deltaTime);
                    part.offset += part.velocity * deltaTime;
                    part.angle += part.spin * deltaTime;
                    size = Mathf.Lerp(part.startSize, part.endSize, 1f - (1f - t) * (1f - t));
                    fade = (1f - t) * (1f - t);
                }

                part.transform.position = part.origin + right * part.offset.x + up * part.offset.y + forward * depthOffset;
                part.transform.rotation = facing * Quaternion.AngleAxis(part.angle, Vector3.forward);
                part.transform.localScale = Vector3.one * size;

                part.renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(part.colorId, part.baseColor * fade);
                part.renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
