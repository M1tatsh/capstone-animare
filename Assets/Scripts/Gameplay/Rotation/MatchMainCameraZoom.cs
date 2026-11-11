using UnityEngine;

namespace Gameplay.Rotation
{
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(Camera))]
    public class MatchMainCameraZoom : MonoBehaviour
    {
        [SerializeField] private Camera cameraToMatch;
        private Camera overlayCamera;

        void Awake()
        {
            overlayCamera = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            if (cameraToMatch == null)
                cameraToMatch = Camera.main;

            if (cameraToMatch == null || cameraToMatch == overlayCamera)
                return;

            overlayCamera.orthographic = cameraToMatch.orthographic;
            overlayCamera.orthographicSize = cameraToMatch.orthographicSize;
            overlayCamera.fieldOfView = cameraToMatch.fieldOfView;
            overlayCamera.nearClipPlane = cameraToMatch.nearClipPlane;
            overlayCamera.farClipPlane = cameraToMatch.farClipPlane;
        }
    }
}
