using Data.Audio;
using UnityEngine;

namespace Gameplay.Dialogue
{
    [System.Serializable]
    public class CutscenePage
    {
        public string speakerName;
        [TextArea(2, 5)] public string text;
        public Sprite portrait;
        public Sprite stillFrame;
        public SoundEffect voiceSound;
    }
}
