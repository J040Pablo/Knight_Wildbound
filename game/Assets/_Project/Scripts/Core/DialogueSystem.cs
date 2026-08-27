using System;
using UnityEngine;

namespace Roguelite.Core
{
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        public bool IsDialogueActive { get; private set; } = false;
        public string CurrentSpeaker { get; private set; } = "";
        public string CurrentText { get; private set; } = "";

        private Action onDialogueCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PlayDialogue(string speaker, string text, Action onComplete = null)
        {
            CurrentSpeaker = speaker;
            CurrentText = text;
            IsDialogueActive = true;
            onDialogueCompleted = onComplete;
        }

        public void AdvanceOrCloseDialogue()
        {
            if (!IsDialogueActive) return;

            IsDialogueActive = false;
            CurrentSpeaker = "";
            CurrentText = "";

            Action callback = onDialogueCompleted;
            onDialogueCompleted = null;
            callback?.Invoke();
        }

        private void Update()
        {
            if (IsDialogueActive && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                AdvanceOrCloseDialogue();
            }
        }
    }
}
