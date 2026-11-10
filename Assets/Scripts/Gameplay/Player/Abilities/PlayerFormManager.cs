using Data.Audio;
using UnityEngine;

namespace Gameplay.Player
{
    public enum PlayerForm
    {
        Reaper,
        Fox,
        Rabbit,
        Crow,
        Ant
    }

    [RequireComponent(typeof(CollisionCheck))]
    public class PlayerFormManager : MonoBehaviour
    {
        [SerializeField] private int foxSlot = 1;
        [SerializeField] private int rabbitSlot = 2;
        [SerializeField] private int crowSlot = 3;
        [SerializeField] private int antSlot = 4;

        [SerializeField] private GameObject reaperModel;
        [SerializeField] private GameObject foxModel;
        [SerializeField] private GameObject rabbitModel;
        [SerializeField] private GameObject crowModel;
        [SerializeField] private GameObject antModel;

        //Josh: one sound for changing into each form. Leave any of them empty for no sound.
        [Header("Transformation Sounds")]
        [SerializeField] private SoundEffect reaperSound;
        [SerializeField] private SoundEffect foxSound;
        [SerializeField] private SoundEffect rabbitSound;
        [SerializeField] private SoundEffect crowSound;
        [SerializeField] private SoundEffect antSound;

        //Josh: which forms the player has. Locked forms can't be picked from the radial menu.
        //The SaveManager saves and loads these, and FormUnlock unlocks them during play.
        [Header("Unlocked Forms")]
        [SerializeField] private bool foxUnlocked = true;
        [SerializeField] private bool rabbitUnlocked = true;
        [SerializeField] private bool crowUnlocked = true;
        [SerializeField] private bool antUnlocked = true;

        /// <summary>
        /// Runs when a form is unlocked (for a sound, or to update the radial menu).
        /// </summary>
        public event System.Action<PlayerForm> FormUnlocked;

        private DashAbility dashAbility;
        private DoubleJumpAbility doubleJumpAbility;
        private GlideAbility glideAbility;
        private ShrinkAbility shrinkAbility;
        private CollisionCheck collisionCheck;
        private PlayerMovement playerMovement;
        private RadialSelection radialSelection;
        private PlayerForm currentForm = PlayerForm.Reaper;

        void Awake()
        {
            dashAbility = GetComponent<DashAbility>();
            doubleJumpAbility = GetComponent<DoubleJumpAbility>();
            glideAbility = GetComponent<GlideAbility>();
            shrinkAbility = GetComponent<ShrinkAbility>();
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
            else if (slot == antSlot)
            {
                SetForm(PlayerForm.Ant);
            }
        }

        public void SetForm(PlayerForm form)
        {
            if (currentForm == PlayerForm.Ant && form != PlayerForm.Ant && shrinkAbility != null && !shrinkAbility.TryGrow())
            {
                return;
            }

            //Josh: only a real change plays a sound, so picking the form you're already in (or the
            //starting Reaper form when the scene loads) stays quiet.
            if (!IsFormUnlocked(form))
            {
                return;
            }

            //Josh: bool check to stop edge cases where player is already in the form.
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

            if (shrinkAbility != null)
            {
                shrinkAbility.enabled = form == PlayerForm.Ant;
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

            if (antModel != null)
            {
                antModel.SetActive(form == PlayerForm.Ant);
            }

            if (formChanged)
            {
                PlayTransformationSound(form);
            }
        }

        //Josh: plays the sound for the form the player just changed into.
        /// <summary>
        /// Plays the sound effect for transforming into the given form. Does nothing if the sound is empty.
        /// </summary>
        /// <param name="form">PlayerForm associated with target sound.</param>
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
            else if (form == PlayerForm.Ant)
            {
                sound = antSound;
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

        

        //Josh: unlocking forms.

        /// <summary>
        /// Checks if the given form is unlocked to manage
        /// </summary>
        public bool IsFormUnlocked(PlayerForm form)
        {
            if (form == PlayerForm.Fox)
                return foxUnlocked;
            if (form == PlayerForm.Rabbit)
                return rabbitUnlocked;
            if (form == PlayerForm.Crow)
                return crowUnlocked;
            if (form == PlayerForm.Ant)
                return antUnlocked;

            return true;
        }

        /// <summary>
        /// Unlocks (or locks) a form.
        /// </summary>
        public void SetFormUnlocked(PlayerForm form, bool unlocked)
        {
            bool wasUnlocked = IsFormUnlocked(form);

            if (form == PlayerForm.Fox)
                foxUnlocked = unlocked;
            else if (form == PlayerForm.Rabbit)
                rabbitUnlocked = unlocked;
            else if (form == PlayerForm.Crow)
                crowUnlocked = unlocked;
            else if (form == PlayerForm.Ant)
                antUnlocked = unlocked;

            //default to reaper if current form gets locked somehow.
            if (!IsFormUnlocked(currentForm))
            {
                SetForm(PlayerForm.Reaper);
            }

            if (unlocked && !wasUnlocked)
            {
                FormUnlocked?.Invoke(form);
            }
        }
        
        public PlayerForm CurrentForm
        {
            get { return currentForm; }
        }
    }
}