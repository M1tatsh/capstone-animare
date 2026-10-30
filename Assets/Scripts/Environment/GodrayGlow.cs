using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.Environment
{
    public class GodrayGlow : MonoBehaviour
    {
        [Header("Rays")]
        [SerializeField] private Material rayMaterial;
        [SerializeField] private int rayCount = 8;
        [SerializeField] private float rayLength = 3f;
        [SerializeField] private float rayWidth = 0.5f;
        [SerializeField] private Vector2 lengthRange = new Vector2(0.6f, 1.2f);
        [SerializeField] private Vector2 widthRange = new Vector2(0.8f, 1.2f);
        [SerializeField] private Vector2 spinSpeedRange = new Vector2(10f, 30f);
        [SerializeField] private Vector2 alphaRange = new Vector2(0.1f, 1f);
        [SerializeField] private Vector2 pulseSpeedRange = new Vector2(0.3f, 1f);
        [SerializeField][Range(0.05f, 1f)] private float textureWidthUsed = 0.34f;

        [Header("Custom Ray Mesh")]
        [SerializeField] private Mesh rayMesh;
        [SerializeField] private Vector3 meshRotation = new Vector3(90f, 0f, 0f);

        [Header("Flare")]
        [SerializeField] private Material flareMaterial;
        [SerializeField] private float flareSize = 1.25f;
        [SerializeField] private Vector2 flareAlphaRange = new Vector2(0.7f, 1f);
        [SerializeField] private float flarePulseSpeed = 1.5f;
        [SerializeField] private float flareScalePulse = 0.1f;

        [Header("Placement")]
        [SerializeField] private float depthOffset = 0.45f;
        [SerializeField] private float flareDepthOffset = -0.4f;

        private Transform rayHolder;
        private Transform[] rays = new Transform[0];
        private Renderer[] rayRenderers = new Renderer[0];
        private float[] angles;
        private float[] spinSpeeds;
        private float[] pulseSpeeds;
        private float[] pulseOffsets;
        private Mesh stripMesh;
        private Transform flare;
        private Renderer flareRenderer;
        private MaterialPropertyBlock propertyBlock;
        private int rayColorId;
        private Color rayBaseColor = Color.white;
        private int flareColorId;
        private Color flareBaseColor = Color.white;
        private Camera viewCamera;

        void Start()
        {
            if (rayMaterial == null && flareMaterial == null)
            {
                enabled = false;
                return;
            }

            propertyBlock = new MaterialPropertyBlock();

            rayHolder = new GameObject("Godrays").transform;
            rayHolder.SetParent(transform, false);
            rayHolder.localScale = Vector3.one / Mathf.Max(transform.lossyScale.x, 0.0001f);

            CreateRays();
            CreateFlare();
        }

        void OnDestroy()
        {
            if (stripMesh != null)
            {
                Destroy(stripMesh);
            }
        }

        void LateUpdate()
        {
            if (rayHolder == null)
            {
                return;
            }

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            if (viewCamera != null)
            {
                rayHolder.rotation = viewCamera.transform.rotation;
                rayHolder.position = transform.position + viewCamera.transform.forward * depthOffset;
            }

            Quaternion meshOffset = Quaternion.Euler(meshRotation);

            for (int i = 0; i < rays.Length; i++)
            {
                angles[i] += spinSpeeds[i] * Time.deltaTime;
                Quaternion spin = Quaternion.AngleAxis(angles[i], Vector3.forward);
                rays[i].localRotation = rayMesh != null ? spin * meshOffset : spin;

                float pulse = Pulse(pulseSpeeds[i], pulseOffsets[i]);
                SetColor(rayRenderers[i], rayColorId, rayBaseColor * Mathf.Lerp(alphaRange.x, alphaRange.y, pulse));
            }

            if (flare != null)
            {
                float flarePulse = Pulse(flarePulseSpeed, 0f);
                flare.localPosition = new Vector3(0f, 0f, flareDepthOffset - depthOffset);
                flare.localScale = Vector3.one * flareSize * (1f + flareScalePulse * flarePulse);
                SetColor(flareRenderer, flareColorId, flareBaseColor * Mathf.Lerp(flareAlphaRange.x, flareAlphaRange.y, flarePulse));
            }
        }

        private void CreateRays()
        {
            if (rayMaterial == null || rayCount <= 0)
            {
                return;
            }

            rayColorId = ColorIdOf(rayMaterial);
            rayBaseColor = ColorOf(rayMaterial, rayColorId);

            Mesh mesh = rayMesh;

            if (mesh == null)
            {
                stripMesh = BuildStripMesh();
                mesh = stripMesh;
            }

            rays = new Transform[rayCount];
            rayRenderers = new Renderer[rayCount];
            angles = new float[rayCount];
            spinSpeeds = new float[rayCount];
            pulseSpeeds = new float[rayCount];
            pulseOffsets = new float[rayCount];

            for (int i = 0; i < rayCount; i++)
            {
                rayRenderers[i] = CreatePart("Godray " + i, mesh, rayMaterial, 0, out rays[i]);

                float length = rayLength * Random.Range(lengthRange.x, lengthRange.y);
                float width = rayWidth * Random.Range(widthRange.x, widthRange.y);
                rays[i].localScale = rayMesh != null ? Vector3.one * length : new Vector3(width, length, 1f);

                angles[i] = Random.Range(0f, 360f);
                spinSpeeds[i] = Random.Range(spinSpeedRange.x, spinSpeedRange.y) * (Random.value < 0.5f ? -1f : 1f);
                pulseSpeeds[i] = Random.Range(pulseSpeedRange.x, pulseSpeedRange.y);
                pulseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private Mesh BuildStripMesh()
        {
            float uMin = 0.5f - textureWidthUsed * 0.5f;
            float uMax = 0.5f + textureWidthUsed * 0.5f;

            Mesh mesh = new Mesh();
            mesh.name = "GodrayStrip";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(-0.5f, 1f, 0f),
                new Vector3(0.5f, 1f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(uMin, 0f),
                new Vector2(uMax, 0f),
                new Vector2(uMin, 1f),
                new Vector2(uMax, 1f)
            };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateFlare()
        {
            if (flareMaterial == null)
            {
                return;
            }

            flareColorId = ColorIdOf(flareMaterial);
            flareBaseColor = ColorOf(flareMaterial, flareColorId);

            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            flareRenderer = CreatePart("Flare", quad, flareMaterial, 1, out flare);
            flare.localPosition = new Vector3(0f, 0f, -0.01f);
            flare.localScale = Vector3.one * flareSize;
        }

        private Renderer CreatePart(string partName, Mesh mesh, Material material, int sortingOrder, out Transform part)
        {
            GameObject partObject = new GameObject(partName, typeof(MeshFilter), typeof(MeshRenderer));
            partObject.layer = gameObject.layer;
            partObject.transform.SetParent(rayHolder, false);
            partObject.GetComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer meshRenderer = partObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = sortingOrder;

            part = partObject.transform;
            return meshRenderer;
        }

        private void SetColor(Renderer target, int colorId, Color color)
        {
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorId, color);
            target.SetPropertyBlock(propertyBlock);
        }

        private static float Pulse(float speed, float offset)
        {
            return 0.5f + 0.5f * Mathf.Sin(Time.time * speed * Mathf.PI * 2f + offset);
        }

        private static int ColorIdOf(Material material)
        {
            return Shader.PropertyToID(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color");
        }

        private static Color ColorOf(Material material, int colorId)
        {
            return material.HasProperty(colorId) ? material.GetColor(colorId) : Color.white;
        }
    }
}
