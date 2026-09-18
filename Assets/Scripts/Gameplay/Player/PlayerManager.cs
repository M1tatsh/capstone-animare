using UnityEngine;

/// <summary>
/// Spawns in a player-controlled pawn at the start of the scene.
/// </summary>
/// 
namespace Gameplay.Player
{
    public class PlayerManager : MonoBehaviour
        {
            [SerializeField][Tooltip("Attach the player prefab to spawn in the scene.")] private GameObject playerPrefab;

            void Awake()
            {
                GetComponent<MeshRenderer>().enabled = false;
                
                GameObject player = Instantiate(playerPrefab);
                player.transform.position = transform.position;

                FindAnyObjectByType<Camera>().transform.SetParent(player.transform);
            }
        }
}
    
