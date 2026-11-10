using UnityEngine;

namespace Gameplay.Rotation
{
    public class ViewDependentWall : MonoBehaviour
    {
        public enum FaceOnBehavior
        {
            ActLikeBuilding,
            Ignore
        }

        [SerializeField] private FaceOnBehavior whenSeenFaceOn = FaceOnBehavior.ActLikeBuilding;

        public bool IgnoredWhenFaceOn => whenSeenFaceOn == FaceOnBehavior.Ignore;
    }
}
