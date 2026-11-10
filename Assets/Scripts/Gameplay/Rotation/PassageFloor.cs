using UnityEngine;

namespace Gameplay.Rotation
{
    public class PassageFloor : MonoBehaviour
    {
        [SerializeField] private bool pullWhenCameraOnNegativeZSide;
        [SerializeField] private bool pullWhenCameraOnNegativeXSide;
        [SerializeField] private bool pullWhenCameraOnPositiveZSide;
        [SerializeField] private bool pullWhenCameraOnPositiveXSide;

        public bool PullsInView(int view)
        {
            Vector3Int cameraLooksToward = ViewAxes.Forward(view);

            if (cameraLooksToward.z > 0)
                return pullWhenCameraOnNegativeZSide;

            if (cameraLooksToward.x > 0)
                return pullWhenCameraOnNegativeXSide;

            if (cameraLooksToward.z < 0)
                return pullWhenCameraOnPositiveZSide;

            return pullWhenCameraOnPositiveXSide;
        }
    }
}
