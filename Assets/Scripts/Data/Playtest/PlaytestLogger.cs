using System;
using Data.Scene;
using Gameplay.Player;
using Gameplay.Rotation;
using UnityEngine;

namespace Data.Playtest
{
    /// <summary>
    /// Records some playtest data and logs it to CSV files, stored in the PlaytestLogs folder (persistent data path).
    /// 
    /// level_visits.csv, falls.csv, sessions.csv
    /// </summary>
    public class PlaytestLogger : MonoBehaviour
    {
        #region variables
        public static bool LoggingEnabled = true;


        //reduces number of times the logger looks for performance reasons iykyk.
        private const float LookupInterval = 0.25f;

        private const string VisitsFileName = "level_visits.csv";
        private const string FallsFileName = "falls.csv";
        private const string SessionsFileName = "sessions.csv";

        private static PlaytestLogger instance;


        private PlayStats stats;

        //this time the game was opened.
        private string sessionId;
        private DateTime sessionStartTime;
        private float sessionSeconds;


        private string currentLevel;
        private float visitSeconds;
        private int visitRotations;
        private int visitFalls;

        private PlayerMovement player;
        private bool isTitleShowing;
        private float lookupTimer;
        #endregion

        /// <summary>
        /// The current playthrough's totals. The SaveManager stores these in the save file.
        /// </summary>
        public static PlayStats CurrentStats => instance != null ? instance.stats : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateLogger()
        {
            if (!LoggingEnabled || instance != null)
                return;

            GameObject loggerObject = new GameObject("PlaytestLogger");
            DontDestroyOnLoad(loggerObject);
            instance = loggerObject.AddComponent<PlaytestLogger>();
        }

        void Awake()
        {
            sessionId = NewShortId();
            sessionStartTime = DateTime.Now;
            stats = NewPlayStats();

            ViewRotator.ViewRotated += OnViewRotated;
            PlayerMovement.FellOutOfLevel += OnPlayerFell;

            Debug.Log($"PlaytestLogger: Writing playtest logs to {CSVDataLogger.FolderPath}");
        }

        void OnDestroy()
        {
            ViewRotator.ViewRotated -= OnViewRotated;
            PlayerMovement.FellOutOfLevel -= OnPlayerFell;

            if (instance == this)
                instance = null;
        }

        void Update()
        {
            lookupTimer -= Time.unscaledDeltaTime;

            if (lookupTimer <= 0f)
            {
                lookupTimer = LookupInterval;
                LookForTitleAndPlayer();
            }


            if (currentLevel == null || UI.Behaviors.PauseMenu.IsPaused || ScreenFader.IsTransitioning)
                return;


            float deltaTime = Time.unscaledDeltaTime;

            visitSeconds += deltaTime;
            sessionSeconds += deltaTime;
            stats.totalSeconds += deltaTime;
            stats.GetLevel(currentLevel).secondsPlayed += deltaTime;
        }

        /// <summary>
        /// Works out whether the title is showing and which level the player is in, and starts or ends visits.
        /// </summary>
        private void LookForTitleAndPlayer()
        {
            bool titleNowShowing = FindAnyObjectByType<TitleMenuBehavior>() != null;


            if (titleNowShowing && !isTitleShowing)
            {
                EndVisit("ReturnedToTitle");
                stats = NewPlayStats();
            }

            isTitleShowing = titleNowShowing;


            if (isTitleShowing)
            {
                EndVisit("ReturnedToTitle");
                return;
            }

            if (player == null)
                player = FindAnyObjectByType<PlayerMovement>();

            string levelNow = player != null ? player.gameObject.scene.name : null;

            if (levelNow != currentLevel)
            {
                EndVisit("LeftLevel");
                StartVisit(levelNow);
            }
        }

        private void StartVisit(string levelName)
        {
            currentLevel = levelName;
            visitSeconds = 0f;
            visitRotations = 0;
            visitFalls = 0;
        }

        /// <summary>
        /// Writes one row for the level visit that just ended.
        /// </summary>
        private void EndVisit(string reason)
        {
            if (currentLevel == null)
                return;

            LevelPlayStats level = stats.GetLevel(currentLevel);

            CSVDataLogger.AppendRow(VisitsFileName,
                new string[]
                {
                    "timestamp", "run_id", "session_id", "level", "end_reason",
                    "visit_seconds", "visit_rotations", "visit_falls",
                    "level_total_seconds", "level_total_rotations", "level_total_falls",
                    "run_total_seconds", "run_total_rotations", "run_total_falls"
                },
                new object[]
                {
                    Timestamp(), stats.runId, sessionId, currentLevel, reason,
                    visitSeconds, visitRotations, visitFalls,
                    level.secondsPlayed, level.rotations, level.falls,
                    stats.totalSeconds, stats.totalRotations, stats.totalFalls
                });

            currentLevel = null;
        }

        private void OnViewRotated(int newView)
        {
            if (currentLevel == null)
                return;

            visitRotations++;
            stats.totalRotations++;
            stats.GetLevel(currentLevel).rotations++;
        }

        private void OnPlayerFell(PlayerMovement fallenPlayer, Vector3 fallPosition, Vector3 lastGroundPosition)
        {
            string levelName = currentLevel ?? fallenPlayer.gameObject.scene.name;
            LevelPlayStats level = stats.GetLevel(levelName);

            visitFalls++;
            stats.totalFalls++;
            level.falls++;

            CSVDataLogger.AppendRow(FallsFileName,
                new string[]
                {
                    "timestamp", "run_id", "session_id", "level", "fall_number_in_level", "seconds_into_visit",
                    "camera_view", "fall_x", "fall_y", "fall_z", "last_ground_x", "last_ground_y", "last_ground_z"
                },
                new object[]
                {
                    Timestamp(), stats.runId, sessionId, levelName, level.falls, visitSeconds,
                    fallenPlayer.CurrentView, fallPosition.x, fallPosition.y, fallPosition.z,
                    lastGroundPosition.x, lastGroundPosition.y, lastGroundPosition.z
                });
        }

        void OnApplicationQuit()
        {
            EndVisit("Quit");

            CSVDataLogger.AppendRow(SessionsFileName,
                new string[]
                {
                    "timestamp", "run_id", "session_id", "session_started", "session_seconds",
                    "run_total_seconds", "run_total_rotations", "run_total_falls", "levels_played"
                },
                new object[]
                {
                    Timestamp(), stats.runId, sessionId, sessionStartTime.ToString("yyyy-MM-dd HH:mm:ss"), sessionSeconds,
                    stats.totalSeconds, stats.totalRotations, stats.totalFalls, stats.levels.Count
                });
        }


        public static void ContinueFromSave(PlayStats savedStats)
        {
            if (instance == null)
                return;

            instance.EndVisit("LoadedSave");

            if (savedStats == null || string.IsNullOrEmpty(savedStats.runId))
                instance.stats = NewPlayStats();
            else
                instance.stats = savedStats;
        }

        private static PlayStats NewPlayStats()
        {
            PlayStats newStats = new PlayStats();
            newStats.runId = NewShortId();
            return newStats;
        }


        private static string NewShortId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static string Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
