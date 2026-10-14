using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Environment
{
    /// <summary>
    /// A background image that always fills the screen behind the level and scrolls (parallaxes) as the player moves:
    /// left and right with the player's movement on screen, and up and down with the player's height.
    ///
    /// One quad per layer for multiple layer parallaxing.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        //URP shaders call their main texture _BaseMap, older shaders call it _MainTex.
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");

        [Header("Follow")]
        [Tooltip("Optional. What the background scrolls with. Leave empty to use the player, found automatically.")]
        [SerializeField] private Transform target;

        [Header("Parallax")]
        [Tooltip("How much the background scrolls when the player moves left or right. " +
            "0 = it stays still on screen (very far away), 1 = it moves as fast as the level (as close as the player).")]
        [Range(0f, 1f)]
        [SerializeField] private float horizontalParallax = 0.2f;

        [Tooltip("The same as Horizontal Parallax, for moving up and down.")]
        [Range(0f, 1f)]
        [SerializeField] private float verticalParallax = 0.1f;

        [Tooltip("How far the background slides sideways for each 90 degree view rotation, in image widths. " +
            "0.25 makes four rotations go all the way around the image, like a panorama. Use a negative number to slide the other way.")]
        [SerializeField] private float scrollPerQuarterTurn = 0.25f;

        [Header("Image size")]
        [Tooltip("How tall the image is compared to the screen. Above 1 leaves room to scroll up and down without running off the image.")]
        [Min(1f)]
        [SerializeField] private float imageHeightInScreens = 1.5f;

        [Tooltip("Off: the image stops at its top and bottom edges. On: it repeats up and down forever (for a pattern or a starry sky).")]
        [SerializeField] private bool repeatVertically = false;

        [Tooltip("Where the image starts up and down. 0 = showing its bottom, 0.5 = its middle, 1 = its top.")]
        [Range(0f, 1f)]
        [SerializeField] private float startingHeight = 0.5f;

        [Header("Placement")]
        [Tooltip("How far in front of the camera's far clip plane the quad sits. Layers with a bigger number are drawn in front.")]
        [Min(0.01f)]
        [SerializeField] private float distanceFromBack = 0.5f;

        private Camera viewCamera;
        private Material backgroundMaterial;
        private int texturePropertyId;
        private float textureAspect = 1f;

        private float horizontalScroll;
        private float startingTargetHeight;
        private Vector3 targetPositionLastFrame;
        private float cameraYawLastFrame;
        private bool hasStarted;

        void Awake()
        {
            viewCamera = GetComponentInParent<Camera>();

            if (viewCamera == null)
                viewCamera = Camera.main;

            if (viewCamera == null)
            {
                Debug.LogWarning("ParallaxBackground: Put this quad under the Main Camera so it can fill the screen.", this);
                enabled = false;
                return;
            }

            //material makes a copy for this quad, so scrolling never changes the material asset itself.
            backgroundMaterial = GetComponent<MeshRenderer>().material;
            texturePropertyId = backgroundMaterial.HasProperty(BaseMapId) ? BaseMapId : MainTextureId;

            Texture backgroundTexture = backgroundMaterial.GetTexture(texturePropertyId);

            if (backgroundTexture == null)
            {
                Debug.LogWarning("ParallaxBackground: The quad's material has no image.", this);
            }
            else
            {
                textureAspect = (float)backgroundTexture.width / backgroundTexture.height;

                //left and right always repeat, so the player can walk forever. Up and down only if asked.
                backgroundTexture.wrapModeU = TextureWrapMode.Repeat;
                backgroundTexture.wrapModeV = repeatVertically ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            }

            //the background is only a picture, it shouldn't cast or receive shadows.
            MeshRenderer quadRenderer = GetComponent<MeshRenderer>();
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            //a Quad made from the menu comes with a collider, which the background doesn't need.
            Collider quadCollider = GetComponent<Collider>();
            if (quadCollider != null)
                Destroy(quadCollider);
        }

        //LateUpdate runs after the player has moved and the camera has followed them.
        void LateUpdate()
        {
            if (target == null && !TryFindTarget())
                return;

            if (!hasStarted)
            {
                //the first frame with a target: remember where everything starts from.
                hasStarted = true;
                startingTargetHeight = target.position.y;
                targetPositionLastFrame = target.position;
                cameraYawLastFrame = viewCamera.transform.eulerAngles.y;
            }

            FitToScreen();
            UpdateScroll();
        }

        private bool TryFindTarget()
        {
            //the player is spawned when the scene starts, so it may not exist on the first frames.
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

            if (player == null)
                return false;

            target = player.transform;
            return true;
        }

        /// <summary>
        /// Places the quad at the back of the camera's view and makes it exactly as big as the screen.
        /// Done every frame so it keeps up with window resizing and camera zoom.
        /// </summary>
        private void FitToScreen()
        {
            float distance = viewCamera.farClipPlane - distanceFromBack;

            //the quad follows the camera's position and rotation, so it is always straight in front of it.
            transform.position = viewCamera.transform.position + viewCamera.transform.forward * distance;
            transform.rotation = viewCamera.transform.rotation;

            float screenHeight;

            if (viewCamera.orthographic)
                screenHeight = viewCamera.orthographicSize * 2f;
            else
                screenHeight = 2f * distance * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            float screenWidth = screenHeight * viewCamera.aspect;

            //lossyScale is the size including the parents' scale, so divide it out to get the right world size.
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(screenWidth / parentScale.x, screenHeight / parentScale.y, 1f);
        }

        private void UpdateScroll()
        {
            Vector3 targetPosition = target.position;
            Vector3 movedThisFrame = targetPosition - targetPositionLastFrame;
            targetPositionLastFrame = targetPosition;

            float sidewaysDistance = Vector3.Dot(movedThisFrame, viewCamera.transform.right);
            horizontalScroll += sidewaysDistance * horizontalParallax;

            float screenHeight = transform.lossyScale.y;
            float screenWidth = transform.lossyScale.x;
            float imageHeight = screenHeight * imageHeightInScreens;
            float imageWidth = imageHeight * textureAspect;

            //rotation consideration
            float cameraYaw = viewCamera.transform.eulerAngles.y;
            float degreesTurned = Mathf.DeltaAngle(cameraYawLastFrame, cameraYaw);
            cameraYawLastFrame = cameraYaw;
            horizontalScroll += (degreesTurned / 90f) * scrollPerQuarterTurn * imageWidth;

            Vector2 tiling = new Vector2(screenWidth / imageWidth, screenHeight / imageHeight);

            float horizontalOffset = horizontalScroll / imageWidth;

            //up and down: measured from where the player started, so the image starts at Starting Height.
            float verticalScroll = (targetPosition.y - startingTargetHeight) * verticalParallax;
            float lowestOffset = 0f;
            float highestOffset = 1f - tiling.y;
            float verticalOffset = Mathf.Lerp(lowestOffset, highestOffset, startingHeight) + verticalScroll / imageHeight;

            if (!repeatVertically)
                verticalOffset = Mathf.Clamp(verticalOffset, lowestOffset, highestOffset);

            //keep the horizontal offset small; a huge number loses precision after a long time walking one way.
            horizontalOffset = Mathf.Repeat(horizontalOffset, 1f);

            backgroundMaterial.SetTextureScale(texturePropertyId, tiling);
            backgroundMaterial.SetTextureOffset(texturePropertyId, new Vector2(horizontalOffset, verticalOffset));
        }

        void OnDestroy()
        {
            //the copy made by .material isn't cleaned up on its own.
            if (backgroundMaterial != null)
                Destroy(backgroundMaterial);
        }
    }
}
