using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(ParticleSystem))]
    public class FootstepVFX : MonoBehaviour
    {
        [SerializeField] private LayerMask surfaceLayers;
        [SerializeField] private float minMoveSpeed = 0.5f;
        [SerializeField] private float maxVerticalSpeed = 0.5f;
        [SerializeField] private float surfaceProbeDepth = 0.1f;
        [SerializeField] private bool emitWhileDashing = true;
        [SerializeField] private Color colorTint = Color.white;

        private const float ProbeDistance = 50f;
        private const int SampleSize = 16;

        private ParticleSystem footstepParticle;
        private PlayerMovement playerMovement;
        private CollisionCheck collisionCheck;
        private Rigidbody rb;
        private Collider playerCollider;
        private Color currentColor;


        /// <summary>
        /// Gets  Color of the surface the player is currently standing on.
        /// </summary>
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Dictionary<(Material, Mesh), Color> colorCache = new Dictionary<(Material, Mesh), Color>();

        void Reset()
        {
            surfaceLayers = LayerMask.GetMask("Ground");
        }

        void Awake()
        {
            footstepParticle = GetComponent<ParticleSystem>();
            playerMovement = GetComponentInParent<PlayerMovement>();
            collisionCheck = GetComponentInParent<CollisionCheck>();
            rb = GetComponentInParent<Rigidbody>();
            playerCollider = GetComponentInParent<Collider>();

            if (playerMovement == null || collisionCheck == null || rb == null || playerCollider == null)
            {
                Debug.LogError("FootstepVFX: Place this under the Player object!");
                enabled = false;
                return;
            }

            currentColor = footstepParticle.main.startColor.color;
            SetEmission(false);
        }

        void Update()
        {
            Vector3 moveAxis = playerMovement.GetMoveAxis();
            Vector3 velocity = rb.linearVelocity;
            float moveSpeed = Vector3.Dot(velocity, moveAxis);

            bool isWalking = collisionCheck.IsGrounded
                && Mathf.Abs(moveSpeed) > minMoveSpeed
                && Mathf.Abs(velocity.y) < maxVerticalSpeed
                && (emitWhileDashing || !playerMovement.IsDashing);

            if (isWalking)
            {
                transform.localRotation = Quaternion.Euler(0f, moveSpeed > 0f ? -90f : 90f, 0f);
                UpdateSurfaceColor(moveAxis);
            }

            SetEmission(isWalking);
        }

        private void SetEmission(bool isOn)
        {
            ParticleSystem.EmissionModule emission = footstepParticle.emission;

            if (emission.enabled != isOn)
            {
                emission.enabled = isOn;
            }
        }

        private void UpdateSurfaceColor(Vector3 moveAxis)
        {
            Vector3 viewAxis = Vector3.Cross(moveAxis, Vector3.up);
            Bounds bounds = playerCollider.bounds;
            Vector3 probePoint = new Vector3(bounds.center.x, bounds.min.y - surfaceProbeDepth, bounds.center.z);
            int hitCount = Physics.RaycastNonAlloc(probePoint - viewAxis * ProbeDistance, viewAxis, hits, ProbeDistance * 2f, surfaceLayers, QueryTriggerInteraction.Ignore);

            Renderer surface = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Renderer candidate = hits[i].collider.GetComponentInParent<Renderer>();

                if (candidate != null && candidate.enabled && candidate.sharedMaterial != null && hits[i].distance < closestDistance)
                {
                    surface = candidate;
                    closestDistance = hits[i].distance;
                }
            }

            if (surface == null)
            {
                return;
            }

            Color surfaceColor = GetSurfaceColor(surface) * colorTint;

            if (surfaceColor != currentColor)
            {
                currentColor = surfaceColor;
                ParticleSystem.MainModule main = footstepParticle.main;
                main.startColor = currentColor;
            }
        }

        private Color GetSurfaceColor(Renderer surface)
        {
            Material material = surface.sharedMaterial;
            MeshFilter meshFilter = surface.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;

            if (colorCache.TryGetValue((material, mesh), out Color cachedColor))
            {
                return cachedColor;
            }

            Color color = Color.white;

            if (material.HasProperty("_BaseColor"))
            {
                color = material.GetColor("_BaseColor");
            }
            else if (material.HasProperty("_Color"))
            {
                color = material.GetColor("_Color");
            }

            string textureProperty = material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            Texture texture = material.HasProperty(textureProperty) ? material.GetTexture(textureProperty) : null;

            if (texture != null)
            {
                Rect uvRect = GetTopFaceUVRect(mesh, surface.transform);
                Vector2 tiling = material.GetTextureScale(textureProperty);
                Vector2 offset = material.GetTextureOffset(textureProperty);
                Vector2 sampleScale = new Vector2(uvRect.width * tiling.x, uvRect.height * tiling.y);
                Vector2 sampleOffset = new Vector2(uvRect.x * tiling.x + offset.x, uvRect.y * tiling.y + offset.y);
                color *= GetAverageColor(texture, sampleScale, sampleOffset);
            }

            color.a = 1f;
            colorCache[(material, mesh)] = color;
            return color;
        }

        private Rect GetTopFaceUVRect(Mesh mesh, Transform surface)
        {
            Rect fullRect = new Rect(0f, 0f, 1f, 1f);

            if (mesh == null || !mesh.isReadable)
            {
                return fullRect;
            }

            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;

            if (normals.Length == 0 || normals.Length != uvs.Length)
            {
                return fullRect;
            }

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool foundTopFace = false;

            for (int i = 0; i < normals.Length; i++)
            {
                if (surface.TransformDirection(normals[i]).y > 0.9f)
                {
                    min = Vector2.Min(min, uvs[i]);
                    max = Vector2.Max(max, uvs[i]);
                    foundTopFace = true;
                }
            }

            return foundTopFace ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : fullRect;
        }

        private Color GetAverageColor(Texture texture, Vector2 scale, Vector2 offset)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture sample = RenderTexture.GetTemporary(SampleSize, SampleSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            Graphics.Blit(texture, sample, scale, offset);
            RenderTexture.active = sample;

            Texture2D readback = new Texture2D(SampleSize, SampleSize, TextureFormat.RGBA32, false);
            readback.ReadPixels(new Rect(0, 0, SampleSize, SampleSize), 0, 0);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(sample);

            Color[] pixels = readback.GetPixels();
            Destroy(readback);

            Color sum = Color.clear;
            float totalWeight = 0f;

            foreach (Color pixel in pixels)
            {
                sum += pixel * pixel.a;
                totalWeight += pixel.a;
            }

            if (totalWeight <= 0f)
            {
                return Color.white;
            }

            Color average = sum / totalWeight;
            average.a = 1f;
            return average;
        }
    }
}