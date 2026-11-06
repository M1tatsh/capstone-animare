using System;
using System.Collections;
using System.Collections.Generic;
using Data.Save;
using Data.Scene;
using Gameplay.Player;
using Gameplay.Rotation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Save
{
    /// <summary>
    /// Saves and loads the game (the level the player is in, where they are standing)<br/>
    /// the items they have collected in every level, and the animal forms they have unlocked.<br/>
    /// Nothing needs to be placed in a scene: call the static methods from anywhere.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        //screen fade
        private const float LoadFadeDuration = 0.5f;

        private static SaveManager instance;

        private static readonly HashSet<string> collectedItemIds = new HashSet<string>();

        //the collected list as it was when the current level was loaded, for "Restart Level".
        private static readonly HashSet<string> collectedAtLevelStart = new HashSet<string>();

        //the save being loaded, waiting for its level to finish loading.
        private SaveData pendingLoad;

        /// <summary>
        /// Runs after a save has been loaded and the player put back in place.
        /// </summary>
        public static event Action GameLoaded;

        /// <summary>
        /// Runs after the game has been saved.
        /// </summary>
        public static event Action GameSaved;

        public static bool HasSave => SaveFile.Exists;

        //static fields keep their values between play sessions in the editor (with fast enter play mode), so clear them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            collectedItemIds.Clear();
            collectedAtLevelStart.Clear();

            //remember what was collected each time a level loads.
            SceneManager.sceneLoaded -= TakeLevelStartSnapshot;
            SceneManager.sceneLoaded += TakeLevelStartSnapshot;

            instance = null;
            GameLoaded = null;
            GameSaved = null;
        }

        #region Collected items

        /// <summary>
        /// StageCollectible asks this when a level loads.
        /// </summary>
        public static bool IsCollected(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && collectedItemIds.Contains(itemId);
        }

        private static void TakeLevelStartSnapshot(Scene scene, LoadSceneMode mode)
        {
            collectedAtLevelStart.Clear();
            collectedAtLevelStart.UnionWith(collectedItemIds);
        }


        public static void RestoreLevelStartProgress()
        {
            collectedItemIds.Clear();
            collectedItemIds.UnionWith(collectedAtLevelStart);
        }


        public static void MarkCollected(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
                collectedItemIds.Add(itemId);
        }

        #endregion

        #region Saving

        /// <summary>
        /// Saves the game right now. Returns false (and logs why) if there is no player to save, or the file can't be written.
        /// </summary>
        public static bool SaveGame()
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

            if (player == null)
            {
                Debug.LogWarning("SaveManager: There is no player in the scene, so the game wasn't saved.");
                return false;
            }

            SaveData data = new SaveData();
            data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            //the level is the scene the player is in (not the title screen that may be loaded next to it).
            data.sceneName = player.gameObject.scene.name;
            data.playerPosition = player.transform.position;
            data.playerRotationY = player.transform.eulerAngles.y;
            data.cameraView = player.CurrentView;

            PlayerFormManager formManager = player.GetComponent<PlayerFormManager>();
            if (formManager != null)
            {
                data.currentForm = formManager.CurrentForm.ToString();

                foreach (PlayerForm form in Enum.GetValues(typeof(PlayerForm)))
                {
                    if (formManager.IsFormUnlocked(form))
                        data.unlockedForms.Add(form.ToString());
                }
            }

            data.collectedItemIds.AddRange(collectedItemIds);



            data.playStats = Data.Playtest.PlaytestLogger.CurrentStats ?? new Data.Playtest.PlayStats();

            if (!SaveFile.Write(data))
                return false;

            Debug.Log($"SaveManager: Saved the game in {data.sceneName} to {SaveFile.FilePath}");
            GameSaved?.Invoke();
            return true;
        }

        /// <summary>
        /// Deletes the save file
        /// </summary>
        public static void DeleteSave()
        {
            SaveFile.Delete();
        }

        /// <summary>
        /// For starting a new game without quitting.
        /// </summary>
        public static void ClearProgress()
        {
            collectedItemIds.Clear();
        }

        #endregion

        #region Loading

        /// <summary>
        /// Loads the saved level (with a screen fade) and puts the player back where they saved,
        /// with the same camera view, forms and collected items. Returns false if there is no save to load.
        /// </summary>
        public static bool LoadGame()
        {
            SaveData data = SaveFile.Read();

            if (data == null)
            {
                Debug.LogWarning("SaveManager: There is no save to load.");
                return false;
            }

            if (ScreenFader.IsTransitioning)
                return false;



            collectedItemIds.Clear();
            Data.Playtest.PlaytestLogger.ContinueFromSave(data.playStats);

            foreach (string itemId in data.collectedItemIds)
                collectedItemIds.Add(itemId);

            if (!GameFlow.HasStarted)
                GameFlow.Begin();

            GetOrCreateInstance().pendingLoad = data;
            ScreenFader.LoadSceneWithFade(data.sceneName, LoadSceneMode.Single, LoadFadeDuration, LoadFadeDuration, Color.black);
            return true;
        }

        private static SaveManager GetOrCreateInstance()
        {
            if (instance != null)
                return instance;

            GameObject managerObject = new GameObject("SaveManager");
            DontDestroyOnLoad(managerObject);
            instance = managerObject.AddComponent<SaveManager>();
            return instance;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (pendingLoad == null || scene.name != pendingLoad.sceneName)
                return;

            StartCoroutine(ApplyLoadedSave(pendingLoad));
            pendingLoad = null;
        }

        /// <summary>
        /// Applies proper position, camera view, forms and collected items to player after a save has been loaded.
        /// </summary>
        private IEnumerator ApplyLoadedSave(SaveData data)
        {
            
            yield return null;
            yield return null;

            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

            if (player == null)
            {
                Debug.LogWarning($"SaveManager: Loaded {data.sceneName}, but there is no player in it to move.");
                yield break;
            }


            ViewRotator viewRotator = FindAnyObjectByType<ViewRotator>();
            if (viewRotator != null)
                viewRotator.SnapToView(data.cameraView);

            player.TeleportTo(data.playerPosition);


            player.SetRespawnPoint(data.playerPosition, data.cameraView);

            PlayerFormManager formManager = player.GetComponent<PlayerFormManager>();
            if (formManager != null)
            {
                foreach (PlayerForm form in Enum.GetValues(typeof(PlayerForm)))
                {
                    bool unlocked = data.unlockedForms.Contains(form.ToString());
                    formManager.SetFormUnlocked(form, unlocked);
                }

                if (Enum.TryParse(data.currentForm, out PlayerForm savedForm))
                    formManager.SetForm(savedForm);
            }


            Debug.Log($"SaveManager: Loaded the save from {data.savedAt} in {data.sceneName}.");
            GameLoaded?.Invoke();
        }

        #endregion
    }
}
