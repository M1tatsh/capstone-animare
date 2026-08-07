using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonBehavior : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool mouse_over = false;
    [SerializeField] private AudioClip sound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponentInParent<AudioSource>();
    }

    void Update()
    {
        if (mouse_over)
        {

        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouse_over = true;
        GetComponentInParent<MainMenu>().PlaySound(sound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouse_over = false;
    }
}
