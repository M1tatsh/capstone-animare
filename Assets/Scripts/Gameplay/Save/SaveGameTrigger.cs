using System.Collections;
using Data.Scene;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Save
{
    /// <summary>
    /// Lets scene objects save and load without code. Hook Save() or Load() up to any UnityEvent,
    /// for example, a save point object with Context Dialogue OnInteract(). <br/><br/>
    /// 
    /// Unity Editor Test Keys: F5 saves, F9 loads, for testing.
    /// </summary>
    public class SaveGameTrigger : MonoBehaviour
    {
        [Tooltip("Saves the game when this scene starts (after the title screen, if there is one).")]
        [SerializeField] private bool saveWhenSceneStarts = false;

        [Tooltip("In the Unity editor only: F5 saves and F9 loads, for testing.")]
        [SerializeField] private bool enableTestKeys = true;

        IEnumerator Start()
        {
            if (!saveWhenSceneStarts)
                yield break;

            while (!GameFlow.HasStarted)
                yield return null;

            //this is to wait a bit for the player to spawn.
            yield return null;
            yield return null;
            yield return null;

            SaveManager.SaveGame();
        }

        void Update()
        {
            #if UNITY_EDITOR
            if (!enableTestKeys || Keyboard.current == null)
                return;

            if (Keyboard.current.f5Key.wasPressedThisFrame)
                Save();

            if (Keyboard.current.f9Key.wasPressedThisFrame)
                Load();
            #endif
        }


        public void Save()
        {
            SaveManager.SaveGame();
        }

        public void Load()
        {
            SaveManager.LoadGame();
        }

        public void DeleteSave()
        {
            SaveManager.DeleteSave();
        }
    }
}
