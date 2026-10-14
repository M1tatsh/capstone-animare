using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gameplay.Progression
{
    /// <summary>
    /// Shows "collected / required" in a corner of the screen.
    /// Put this on a GameObject with a UIDocument that uses StageProgressHUD.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StageProgressHUD : MonoBehaviour
    {
        [Tooltip("Shown when the exit is unlocked, after the numbers.")]
        [SerializeField] private string unlockedText = "  Door open!";

        private StageProgress stageProgress;
        private Label countLabel;
        private VisualElement panel;
        //the panel's current size, so the tween always knows where it is starting from.
        private float panelScale = 1f;

        //Start instead of Awake: the UIDocument builds its UI when it is enabled, which can be after this Awake.
        //It may also be switched off at first (for example by a SceneLoadGate), so wait until it is on.
        IEnumerator Start()
        {
            UIDocument document = GetComponent<UIDocument>();
            yield return new WaitUntil(() => document.isActiveAndEnabled && document.rootVisualElement != null);

            VisualElement root = document.rootVisualElement;
            panel = root.Q<VisualElement>("ProgressPanel");
            countLabel = root.Q<Label>("ProgressCount");

            stageProgress = FindAnyObjectByType<StageProgress>();

            if (panel == null || countLabel == null)
            {
                Debug.LogError("StageProgressHUD: The UIDocument needs StageProgressHUD.uxml as its Source Asset.", this);
                enabled = false;
                yield break;
            }

            if (stageProgress == null)
            {
                //nothing to count in this scene.
                panel.style.display = DisplayStyle.None;
                enabled = false;
                yield break;
            }

            stageProgress.CountChanged += OnCountChanged;

            //StageProgress may already have sent its starting numbers before this Start, so show them now.
            OnCountChanged(stageProgress.CollectedCount, stageProgress.RequiredCount);
        }

        void OnDestroy()
        {
            if (stageProgress != null)
                stageProgress.CountChanged -= OnCountChanged;

            DOTween.Kill(this);
        }

        private void SetPanelScale(float scale)
        {
            panelScale = scale;
            panel.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        private void OnCountChanged(int collected, int required)
        {
            countLabel.text = $"{collected} / {required}";

            if (collected >= required)
                countLabel.text += unlockedText;

            //a small pop each time the number goes up.
            if (collected > 0)
            {
                DOTween.Kill(this);
                SetPanelScale(1.2f);
                DOTween.To(() => panelScale, SetPanelScale, 1f, 0.2f)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(this);
            }
        }
    }
}
