using System;
using System.IO;
using UnityEngine;

namespace Data.Save
{
    /// <summary>
    /// Reads and writes the save file on disk.
    /// 
    /// File saves to Application.persistentDataPath (AppData/LocalLow/DeathArtStudios/capstone-animare).
    /// </summary>
    public static class SaveFile
    {
        private const string FileName = "animare_save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists => File.Exists(FilePath);

        /// <summary>
        /// Writes the save to disk. Returns false (and logs why) if it couldn't.
        /// </summary>
        public static bool Write(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);

                string temporaryPath = FilePath + ".tmp";
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(FilePath))
                    File.Delete(FilePath);

                File.Move(temporaryPath, FilePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"SaveFile: Could not save the game to {FilePath}.\n{exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads the save from disk
        /// </summary>
        public static SaveData Read()
        {
            if (!Exists)
                return null;

            try
            {
                string json = File.ReadAllText(FilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data == null || string.IsNullOrEmpty(data.sceneName))
                {
                    Debug.LogWarning($"SaveFile: The save file at {FilePath} is empty or damaged.");
                    return null;
                }

                return data;
            }
            catch (Exception exception)
            {
                Debug.LogError($"SaveFile: Could not read the save file at {FilePath}.\n{exception.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            if (Exists)
                File.Delete(FilePath);
        }
    }
}
