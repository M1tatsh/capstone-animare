using System;

namespace Data.Scene
{
    public static class GameFlow
    {
        public static event Action Started;
        public static event Action<float> StartPressed;
        public static bool HasStarted { get; private set; }

        public static void PressStart(float titleFadeDuration)
        {
            StartPressed?.Invoke(titleFadeDuration);
        }

        public static void Begin()
        {
            HasStarted = true;
            Started?.Invoke();
        }

        /// <summary>
        /// Called to ensure the level behind the title screen gets reset.
        /// </summary>
        public static void ReturnToTitle()
        {
            HasStarted = false;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() 
        { 
            HasStarted = false; 
            Started = null;     
            StartPressed = null;
        }
    }
}
    
