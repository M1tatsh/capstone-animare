using System;
using System.Collections.Generic;

namespace Data.Playtest
{
    /// <summary>
    /// Play statistics for one level, added up over every visit in one save.
    /// </summary>
    [Serializable]
    public class LevelPlayStats
    {
        public string sceneName = "";
        public float secondsPlayed;
        public int rotations;
        public int falls;
    }

    /// <summary>
    /// Play statistics for one save. Kept in the save file, so the totals carry on after Continue.
    /// </summary>
    [Serializable]
    public class PlayStats
    {
        public string runId = "";
        public float totalSeconds;
        public int totalRotations;
        public int totalFalls;
        public List<LevelPlayStats> levels = new List<LevelPlayStats>();


        public LevelPlayStats GetLevel(string sceneName)
        {
            foreach (LevelPlayStats level in levels)
            {
                if (level.sceneName == sceneName)
                    return level;
            }

            LevelPlayStats newLevel = new LevelPlayStats();
            
            newLevel.sceneName = sceneName;
            levels.Add(newLevel);

            return newLevel;
        }
    }
}
