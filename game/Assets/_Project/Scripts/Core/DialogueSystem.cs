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

            Debug.Log($"[DialogueSystem] Playing dialogue for: {speaker}");
        }

        public void AdvanceOrCloseDialogue()
        {
            if (!IsDialogueActive) return;

            Debug.Log($"[DialogueSystem] Closed dialogue for: {CurrentSpeaker}");
            IsDialogueActive = false;
            CurrentSpeaker = "";
            CurrentText = "";

            Action callback = onDialogueCompleted;
            onDialogueCompleted = null;
            callback?.Invoke();
        }

        private void Update()
        {
            if (IsDialogueActive && (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                AdvanceOrCloseDialogue();
            }
        }

        private void OnGUI()
        {
            if (!IsDialogueActive) return;

            // Stylish Action-RPG Dialogue Box at bottom-center
            float boxWidth = Mathf.Min(680f, Screen.width - 40f);
            float boxHeight = 135f;
            float posX = (Screen.width - boxWidth) * 0.5f;
            float posY = Screen.height - boxHeight - 40f;

            Rect mainRect = new Rect(posX, posY, boxWidth, boxHeight);

            // Dark semi-transparent background
            GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.94f);
            GUI.DrawTexture(mainRect, Texture2D.whiteTexture);

            // Gold accent border
            GUI.color = new Color(0.85f, 0.70f, 0.25f, 0.85f);
            GUI.Box(mainRect, "");

            // Speaker Tag Box
            Rect speakerRect = new Rect(posX + 20f, posY - 18f, 160f, 28f);
            GUI.color = new Color(0.18f, 0.14f, 0.08f, 0.95f);
            GUI.DrawTexture(speakerRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.80f, 0.30f, 1.0f);
            GUI.Box(speakerRect, "");

            GUI.skin.label.fontSize = 12;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(1.0f, 0.88f, 0.40f);
            GUI.Label(speakerRect, $"👑 {CurrentSpeaker.ToUpper()}");

            // Dialogue Body Text
            Rect textRect = new Rect(posX + 25f, posY + 22f, boxWidth - 50f, boxHeight - 55f);
            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            GUI.Label(textRect, CurrentText);

            // Prompt Hint at Bottom-Right
            Rect hintRect = new Rect(posX + boxWidth - 260f, posY + boxHeight - 24f, 240f, 20f);
            GUI.skin.label.fontSize = 10;
            GUI.skin.label.fontStyle = FontStyle.BoldAndItalic;
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUI.color = new Color(0.95f, 0.85f, 0.45f, 0.9f);
            GUI.Label(hintRect, "[Pressione F, ESPAÇO ou CLIQUE para Continuar →]");

            // Reset label skin alignment
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }
    }
}
