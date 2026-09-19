using UnityEngine;
using Gameplay.Player;

namespace Gameplay.Mechanics
{
    /// <summary>
    /// Turns off collision of attached object while the player's y-value is less than attached object's. 
    /// Requires two colliders to work; a larger triggerbox to check for collision and a smaller non-trigger collider for actual physics collision.
    /// </summary>

    [RequireComponent(typeof(Collider))] 
    public class OneWayPlatform : MonoBehaviour
    {
        public Collider platformCollider;
        private float bufferArea = 0.8f;

        void Start()
        {
            if (platformCollider == null)
                Debug.LogError($"OneWayPlatform.Start | Could not find Collider component!");
        }

        void OnTriggerStay(Collider other)
        {
            //Debug.Log($"OneWayPlatform.OnTriggerStay | Colliding with: {other.name}");
            if (other.CompareTag("Player"))
            {
                PlayerMovement pm = other.GetComponent<PlayerMovement>();
                if (pm.transform.position.y <= transform.position.y + bufferArea)
                {
                    Physics.IgnoreCollision(other, platformCollider, true);
                }
                else
                    Physics.IgnoreCollision(other, platformCollider, false);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Physics.IgnoreCollision(other, platformCollider, false);
            }
        }
    }
}

    
