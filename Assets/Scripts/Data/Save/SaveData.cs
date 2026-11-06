using System;
using System.Collections.Generic;
using Data.Playtest;
using UnityEngine;

namespace Data.Save
{
    /// <summary>
    /// Everything that goes into the save file using Unity's JsonUtility.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        //bump this when the save layout changes, so old saves can be recognized.
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        [Tooltip("When the game was saved, for showing on a load screen.")]
        public string savedAt = "";

        [Header("Where the player is")]
        public string sceneName = "";
        public Vector3 playerPosition;
        public float playerRotationY;
        [Tooltip("Which of the four views the camera was in (0 to 3).")]
        public int cameraView;

        [Header("Abilities")]
        public string currentForm = "";
        public List<string> unlockedForms = new List<string>();

        [Header("Collected items, in every level")]
        public List<string> collectedItemIds = new List<string>();

        [Header("Playtest statistics (time, rotations, falls)")]
        public PlayStats playStats = new PlayStats();
    }
}
