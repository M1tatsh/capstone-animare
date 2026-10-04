using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Effects
{
    public class FloatingElement : MonoBehaviour
    {
        [SerializeField] private string floatClass = "TitleMenu";
        [SerializeField] private Vector2 amplitudeRange = new Vector2(4f, 12f);   // pixels
        [SerializeField] private Vector2 durationRange = new Vector2(1.5f, 3.5f); // seconds per leg

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            root.Query<VisualElement>(className: floatClass)
                .ForEach(element => Drift(
                    element, 
                    Vector2.zero, 
                    Random.Range(amplitudeRange.x, amplitudeRange.y)
                    )
                );
        }

        void OnDisable() => DOTween.Kill(this);

        void Drift(VisualElement element, Vector2 current, float amplitude)
        {
            Vector2 target = new Vector2(
                Random.Range(-amplitude, amplitude) * 0.5f,
                Random.Range(-amplitude, amplitude)
            );

            DOTween.To(
                () => current,
                vector => { current = vector; element.style.translate = new Translate(vector.x, vector.y); },
                target,
                Random.Range(durationRange.x, durationRange.y)
            )
            .SetEase(Ease.InOutSine)
            .SetTarget(this)
            .SetUpdate(true)
            .OnComplete(() => Drift(element, target, amplitude));
        }
    }
}
    
