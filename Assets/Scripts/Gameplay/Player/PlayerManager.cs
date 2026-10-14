using Gameplay.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

                //Instantiate puts the player in the active scene. When this scene is loaded behind the title screen,
                //the active scene is still the title, which gets unloaded, so move the player into this scene.
                SceneManager.MoveGameObjectToScene(player, gameObject.scene);

                //FindAnyObjectByType<Camera>().transform.SetParent(player.transform);

                HoldPlayerControlUntilStart(player);
            }

            /// <summary>
            /// In a scene with a SceneLoadGate (one that waits behind the title screen), the player can't be
            /// controlled until Start is pressed. The player still stands in the scene and falls with gravity.
            /// Scenes without a gate (played directly) are not affected.
            /// </summary>
            private void HoldPlayerControlUntilStart(GameObject player)
            {
                //only look for a gate in this scene, not in the title scene loaded next to it.
                SceneLoadGate gate = null;

                foreach (GameObject rootObject in gameObject.scene.GetRootGameObjects())
                {
                    gate = rootObject.GetComponentInChildren<SceneLoadGate>(true);

                    if (gate != null)
                        break;
                }

                if (gate == null)
                    return;

                //the order matters: these are switched back on in this order when Start is pressed.
                gate.DisableUntilStart(player.GetComponent<PlayerInput>());
                gate.DisableUntilStart(player.GetComponent<InputInitializer>());
                gate.DisableUntilStart(player.GetComponent<PlayerController>());
            }
        }
}
