using System.Collections.Generic;
using System.Text.RegularExpressions;
using Data.Audio;
using UnityEngine.UIElements;

namespace Gameplay.Dialogue
{
    /// <summary>
    /// Types text into a label one letter at a time, with optional typing sounds.
    /// </summary>
    public class TypewriterText
    {
        private const string HiddenTextTag = "<alpha=#00>";
        private static readonly Regex ColorTagPattern = new Regex(@"</?(color|alpha)[^>]*>", RegexOptions.IgnoreCase);

        private readonly Label label;
        private readonly int lettersPerSound;
        private readonly float punctuationPause;

        private string fullText = "";
        private readonly List<int> letterEndIndexes = new List<int>();
        private readonly List<char> letters = new List<char>();
        private int lettersShown;
        private float typingProgress;
        private float typingSpeed;
        private SoundEffect typingSound;
        private int lettersSinceSound;

        public bool IsTyping { get; private set; }

        public TypewriterText(Label label, int lettersPerSound, float punctuationPause)
        {
            this.label = label;
            this.lettersPerSound = lettersPerSound < 1 ? 1 : lettersPerSound;
            this.punctuationPause = punctuationPause < 0f ? 0f : punctuationPause;
            label.enableRichText = true;
        }

        public void StartTyping(string text, SoundEffect sound, float lettersPerSecond)
        {
            fullText = text ?? "";
            FindLetters(fullText);

            lettersShown = 0;
            typingProgress = 0f;
            typingSpeed = lettersPerSecond < 1f ? 1f : lettersPerSecond;
            typingSound = sound;
            lettersSinceSound = lettersPerSound;
            IsTyping = letters.Count > 0;

            ShowTypedLetters();
        }

        public void UpdateTyping(float deltaTime)
        {
            if (!IsTyping)
                return;

            typingProgress += deltaTime * typingSpeed;
            bool lettersAdded = false;
            bool soundPlayedThisFrame = false;

            while (typingProgress >= 1f && lettersShown < letters.Count)
            {
                typingProgress -= 1f;
                char letter = letters[lettersShown];
                lettersShown++;
                lettersAdded = true;

                if (letter == '.' || letter == '!' || letter == '?')
                    typingProgress -= punctuationPause * typingSpeed;
                else if (letter == ',' || letter == ';' || letter == ':')
                    typingProgress -= punctuationPause * 0.5f * typingSpeed;

                if (char.IsWhiteSpace(letter))
                    continue;

                lettersSinceSound++;

                if (lettersSinceSound >= lettersPerSound && !soundPlayedThisFrame)
                {
                    AudioManager.PlaySoundEffect(typingSound);
                    lettersSinceSound = 0;
                    soundPlayedThisFrame = true;
                }
            }

            if (lettersAdded)
                ShowTypedLetters();

            if (lettersShown >= letters.Count)
                FinishTyping();
        }

        public void FinishTyping()
        {
            if (!IsTyping)
                return;

            IsTyping = false;
            lettersShown = letters.Count;
            label.text = fullText;
        }

        public void StopTyping()
        {
            IsTyping = false;
        }

        private void ShowTypedLetters()
        {
            if (lettersShown >= letters.Count)
            {
                label.text = fullText;
                return;
            }

            int splitIndex = lettersShown == 0 ? 0 : letterEndIndexes[lettersShown - 1];

            string shownPart = fullText.Substring(0, splitIndex);
            string hiddenPart = ColorTagPattern.Replace(fullText.Substring(splitIndex), "");

            label.text = shownPart + HiddenTextTag + hiddenPart;
        }

        private void FindLetters(string text)
        {
            letters.Clear();
            letterEndIndexes.Clear();

            int index = 0;

            while (index < text.Length)
            {
                if (text[index] == '<')
                {
                    int tagEnd = text.IndexOf('>', index);

                    if (tagEnd > index)
                    {
                        index = tagEnd + 1;
                        continue;
                    }
                }

                letters.Add(text[index]);
                letterEndIndexes.Add(index + 1);
                index++;
            }
        }
    }
}
