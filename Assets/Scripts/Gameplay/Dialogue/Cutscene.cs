using System.Collections;
using System.Collections.Generic;
using Gameplay.Rotation;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Dialogue
{
    public class Cutscene : MonoBehaviour
    {
        [SerializeField] private List<CutscenePage> pages = new List<CutscenePage>();

        [Header("Playing")]
        [SerializeField] private bool playOnSceneStart = false;
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private bool playOnlyOnce = false;
        [SerializeField] private string cutsceneName = "";

        [Header("Events")]
        [SerializeField] private UnityEvent onFinished = new UnityEvent();

        public int PageCount => pages.Count;

        public CutscenePage GetPage(int pageIndex)
        {
            return pages[pageIndex];
        }

        private string SeenKey
        {
            get
            {
                string keyName = string.IsNullOrEmpty(cutsceneName) ? gameObject.scene.name + "_" + gameObject.name : cutsceneName;
                return "CutsceneSeen_" + keyName;
            }
        }

        public bool HasBeenSeen
        {
            get { return PlayerPrefs.GetInt(SeenKey, 0) == 1; }
        }

        IEnumerator Start()
        {
            if (!playOnSceneStart)
                yield break;

            yield return new WaitForSeconds(startDelay);

            while (LevelFlyover.IsPlaying)
                yield return null;

            Play();
        }

        public void Play()
        {
            if (pages.Count == 0)
                return;

            if (playOnlyOnce && HasBeenSeen)
                return;

            CutsceneDialogueManager cutsceneManager = FindAnyObjectByType<CutsceneDialogueManager>();

            if (cutsceneManager == null)
            {
                Debug.LogWarning("Cutscene: There is no CutsceneDialogueManager in this scene, so the cutscene can't be played.", this);
                return;
            }

            cutsceneManager.Play(this);
        }

        public void MarkAsFinished()
        {
            if (playOnlyOnce)
            {
                PlayerPrefs.SetInt(SeenKey, 1);
                PlayerPrefs.Save();
            }

            onFinished.Invoke();
        }

        [ContextMenu("Forget That This Cutscene Was Seen")]
        public void ForgetSeen()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
        }
    }
}
