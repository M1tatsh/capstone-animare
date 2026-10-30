using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Rotation
{
    /// <summary>
    /// Put this on a block under BlockGrid to let the player pick it up (with Interact) and carry it, like the
    /// crates in Fez. It is a normal solid block while it sits in the level: it can be stood on and it follows
    /// the flattening rules. While it is being carried it is taken out of the grid and floats above the player's head.
    ///
    /// The player can only carry one block at a time. When put down it goes one block in front of the player
    /// and falls until it lands on something. A block dropped into a pit goes back to where it started.
    /// </summary>
    public class HoldableBlock : MonoBehaviour
    {
        [Header("Events")]
        [Tooltip("Runs when the player picks this block up. For sounds or effects.")]
        [SerializeField] private UnityEvent onPickedUp = new UnityEvent();
        [Tooltip("Runs when the player puts this block down.")]
        [SerializeField] private UnityEvent onPlaced = new UnityEvent();

        /// <summary>
        /// True while the player is carrying this block. BlockGrid leaves held blocks out.
        /// </summary>
        public bool IsHeld { get; private set; }

        /// <summary>
        /// Where the block was when the scene started. It goes back here if it falls out of the level.
        /// </summary>
        public Vector3 SpawnPosition { get; private set; }

        public UnityEvent OnPickedUp => onPickedUp;
        public UnityEvent OnPlaced => onPlaced;

        void Awake()
        {
            SpawnPosition = transform.position;
        }

        void OnDestroy()
        {
            DOTween.Kill(transform);
        }

        /// <summary>
        /// Called by PlayerMovement when the player lifts this block.
        /// </summary>
        public void MarkPickedUp()
        {
            //stops a fall that was still playing, if it was picked up again straight away.
            DOTween.Kill(transform);
            IsHeld = true;
            onPickedUp.Invoke();
        }

        /// <summary>
        /// Called by PlayerMovement when the player puts this block down.
        /// </summary>
        public void MarkPlaced()
        {
            IsHeld = false;
            onPlaced.Invoke();
        }
    }
}
