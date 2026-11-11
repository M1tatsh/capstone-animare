using System.Collections.Generic;
using Gameplay.Player;
using UnityEngine;

namespace Gameplay
{
    public static class GameplayPause
    {
        private static readonly HashSet<Object> pausedBy = new HashSet<Object>();
        private static PlayerMovement frozenPlayer;

        public static bool IsPaused
        {
            get
            {
                pausedBy.RemoveWhere(
                    requester => requester == null
                    );
                return pausedBy.Count > 0;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            pausedBy.Clear();
            frozenPlayer = null;
        }

        public static void Pause(Object requester)
        {
            if (requester == null)
                return;

            bool wasPaused = IsPaused;

            if (!pausedBy.Add(requester) || wasPaused)
                return;

            frozenPlayer = Object.FindAnyObjectByType<PlayerMovement>();

            if (frozenPlayer != null)
                frozenPlayer.SetFrozen(true);
        }

        public static void Resume(Object requester)
        {
            if (!pausedBy.Remove(requester))
                return;

            if (IsPaused)
                return;

            if (frozenPlayer != null)
                frozenPlayer.SetFrozen(false);

            frozenPlayer = null;
        }
    }
}
