using Data.Audio;
using UnityEngine;

namespace Gameplay.Player
{
    public enum PlayerForm
    {
        Reaper,
        Fox,
        Rabbit,
        Crow
    }

    [RequireComponent(typeof(CollisionCheck))]
    public class PlayerFormManager : MonoBehaviour
    {
        [SerializeField] private int foxSlot = 1;
        [SerializeField] private int rabbitSlot = 2;
        [SerializeField] private int crowSlot = 3;

        [SerializeField] private GameObject reaperModel;
        [SerializeField] private GameObject foxModel;
        [SerializeField] private GameObject rabbitModel;
        [SerializeField] private GameObject crowModel;

        //Josh: one sound for changing into each form. Leave any of them empty for no sound.
        [Header("Transformation Sounds")]
        [SerializeField] private SoundEffect reaperSound;
        [SerializeField] private SoundEffect foxSound;
        [SerializeField] private SoundEffect rabbitSound;
        [SerializeField] private SoundEffect crowSound;

        private DashAbility dashAbility;
        private DoubleJumpAbility doubleJumpAbility;
        private GlideAbility glideAbility;
        private CollisionCheck collisionCheck;
        private PlayerMovement playerMovement;
        private RadialSelection radialSelection;
        private PlayerForm currentForm = PlayerForm.Reaper;

        void Awake()
        {
            dashAbility = GetComponent<DashAbility>();
            doubleJumpAbility = GetComponent<DoubleJumpAbility>();
            glideAbility = GetComponent<GlideAbility>();
            collisionCheck = GetComponent<CollisionCheck>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        void Start()
        {
            radialSelection = FindAnyObjectByType<RadialSelection>();

            if (radialSelection != null)
            {
                radialSelection.OnPartSelected.AddListener(OnPartSelected);
            }
            

            SetForm(PlayerForm.Reaper);
        }

        void OnDestroy()
        {
            if (radialSelection != null)
            {
                radialSelection.OnPartSelected.RemoveListener(OnPartSelected);
            }
        }

        void FixedUpdate()
        {
            if (!collisionCheck.IsGrounded)
            {
                return;
            }

            if (dashAbility != null)
            {
                dashAbility.ResetAirDashes();
            }

            if (doubleJumpAbility != null)
            {
                doubleJumpAbility.ResetAirJumps();
            }
        }

        private void OnPartSelected(int slot)
        {
            if (slot == 0)
            {
                SetForm(PlayerForm.Reaper);
            }
            else if (slot == foxSlot)
            {
                SetForm(PlayerForm.Fox);
            }
            else if (slot == rabbitSlot)
            {
                SetForm(PlayerForm.Rabbit);
            }
            else if (slot == crowSlot)
            {
                SetForm(PlayerForm.Crow);
            }
        }

        public void SetForm(PlayerForm form)
        {
            //Josh: only a real change plays a sound, so picking the form you're already in (or the
            //starting Reaper form when the scene loads) stays quiet.
            bool formChanged = form != currentForm;

            currentForm = form;

            if (dashAbility != null)
            {
                dashAbility.enabled = form == PlayerForm.Fox;
            }

            if (doubleJumpAbility != null)
            {
                doubleJumpAbility.enabled = form == PlayerForm.Rabbit;
            }

            if (glideAbility != null)
            {
                glideAbility.enabled = form == PlayerForm.Crow;
            }

            if (reaperModel != null)
            {
                reaperModel.SetActive(form == PlayerForm.Reaper);
            }

            if (foxModel != null)
            {
                foxModel.SetActive(form == PlayerForm.Fox);
            }

            if (rabbitModel != null)
            {
                rabbitModel.SetActive(form == PlayerForm.Rabbit);
            }

            if (crowModel != null)
            {
                crowModel.SetActive(form == PlayerForm.Crow);
            }

            if (formChanged)
            {
                PlayTransformationSound(form);
            }
        }

        //Josh: plays the sound for the form the player just changed into.
        private void PlayTransformationSound(PlayerForm form)
        {
            SoundEffect sound = null;

            if (form == PlayerForm.Reaper)
            {
                sound = reaperSound;
            }
            else if (form == PlayerForm.Fox)
            {
                sound = foxSound;
            }
            else if (form == PlayerForm.Rabbit)
            {
                sound = rabbitSound;
            }
            else if (form == PlayerForm.Crow)
            {
                sound = crowSound;
            }

            //through playermovement so it gets muffled behind buildings like the other player sounds.
            if (playerMovement != null)
            {
                playerMovement.PlayPlayerSound(sound);
            }
            else
            {
                AudioManager.PlaySoundEffect(sound);
            }
        }

        public PlayerForm CurrentForm
        {
            get { return currentForm; }
        }
    }
}