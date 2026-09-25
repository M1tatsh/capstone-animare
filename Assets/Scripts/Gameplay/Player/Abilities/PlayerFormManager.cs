using UnityEngine;

namespace Gameplay.Player
{
    public enum PlayerForm
    {
        Reaper,
        Fox,
        Rabbit
    }

    [RequireComponent(typeof(CollisionCheck))]
    public class PlayerFormManager : MonoBehaviour
    {
        [SerializeField] private int foxSlot = 1;
        [SerializeField] private int rabbitSlot = 2;

        [SerializeField] private GameObject reaperModel;
        [SerializeField] private GameObject foxModel;
        [SerializeField] private GameObject rabbitModel;

        private DashAbility dashAbility;
        private DoubleJumpAbility doubleJumpAbility;
        private CollisionCheck collisionCheck;
        private RadialSelection radialSelection;
        private PlayerForm currentForm = PlayerForm.Reaper;

        void Awake()
        {
            dashAbility = GetComponent<DashAbility>();
            doubleJumpAbility = GetComponent<DoubleJumpAbility>();
            collisionCheck = GetComponent<CollisionCheck>();
        }

        void Start()
        {
            radialSelection = FindAnyObjectByType<RadialSelection>();

            if (radialSelection != null)
            {
                radialSelection.OnPartSelected.AddListener(OnPartSelected);
            }
            else
            {
                Debug.LogWarning("PlayerFormManager: RadialSelection is missing in the scene!");
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
        }

        public void SetForm(PlayerForm form)
        {
            currentForm = form;

            if (dashAbility != null)
            {
                dashAbility.enabled = form == PlayerForm.Fox;
            }

            if (doubleJumpAbility != null)
            {
                doubleJumpAbility.enabled = form == PlayerForm.Rabbit;
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

            Debug.Log($"PlayerFormManager | Form: {form}");
        }

        public PlayerForm CurrentForm
        {
            get { return currentForm; }
        }
    }
}