using UnityEngine;

namespace Data.Audio
{
    /// <summary>
    /// Calculator for audio range using spherical radii.
    /// </summary>
    [System.Serializable]
    public class SoundHearingRange
    {
        //Unity's cutoff for a low-pass filter that changes nothing (the top of human hearing).
        public const float UnmuffledCutoffFrequency = 22000f;

        [Tooltip("Closer than this, the sound is at full volume.")]
        [Min(0f)]
        public float fullVolumeRadius = 3f;

        [Tooltip("Farther than this, the sound can't be heard at all.")]
        [Min(0f)]
        public float hearingRadius = 10f;

        [Tooltip("Off: the real 3D distance is used. On: depth is ignored, so the sound is heard whenever it " +
            "looks close on screen, even if it is far behind or in front of the player.")]
        public bool ignoreDepth = false;

        [Tooltip("The sound also gets more muffled the farther away it is, not only quieter.")]
        public bool muffleWithDistance = false;

        [Tooltip("How muffled the sound is at the very edge of the Hearing Radius. Lower is more muffled.")]
        [Range(100f, 22000f)]
        public float farthestMuffleCutoffFrequency = 1000f;

        /// <summary>
        /// The distance between the sound and the listener, ignoring depth if ignoreDepth is on.
        /// </summary>
        public float GetDistance(Vector3 soundPosition, Vector3 listenerPosition)
        {
            Vector3 listenerToSound = soundPosition - listenerPosition;

            if (ignoreDepth)
            {
                //remove the part of the distance that points into the screen (the same way ContextDialogue does it).
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                    listenerToSound = Vector3.ProjectOnPlane(listenerToSound, mainCamera.transform.forward);
            }

            return listenerToSound.magnitude;
        }

        /// <summary>
        /// 1 when the listener is inside the Full Volume Radius, 0 at (or past) the Hearing Radius,
        /// and in between when the listener is between the two.
        /// </summary>
        public float GetCloseness(float distance)
        {
            if (distance <= fullVolumeRadius)
                return 1f;

            if (distance >= hearingRadius)
                return 0f;

            return 1f - Mathf.InverseLerp(fullVolumeRadius, hearingRadius, distance);
        }

        /// <summary>
        /// How much to multiply the sound's volume by for this closeness.
        /// </summary>
        public float GetVolumeMultiplier(float closeness)
        {
            //squaring makes the fade sound more natural: our ears notice volume changes less when a sound is loud,
            //so a straight line would sound like it stays loud and then suddenly drops off at the edge.
            return closeness * closeness;
        }

        /// <summary>
        /// The low-pass cutoff for this closeness. Returns the unmuffled cutoff if muffleWithDistance is off.
        /// </summary>
        public float GetMuffleCutoffFrequency(float closeness)
        {
            if (!muffleWithDistance)
                return UnmuffledCutoffFrequency;

            //frequencies are heard on a curve too (each octave doubles the frequency), so the fade is done
            //on the logarithm of the frequency. Otherwise almost all of the muffling would happen right at the edge.
            float farthestLog = Mathf.Log(farthestMuffleCutoffFrequency);
            float unmuffledLog = Mathf.Log(UnmuffledCutoffFrequency);
            return Mathf.Exp(Mathf.Lerp(farthestLog, unmuffledLog, closeness));
        }

        /// <summary>
        /// Keeps the Hearing Radius from being smaller than the Full Volume Radius. Call it from OnValidate.
        /// </summary>
        public void KeepRadiiInOrder()
        {
            if (hearingRadius < fullVolumeRadius)
                hearingRadius = fullVolumeRadius;
        }
    }
}
