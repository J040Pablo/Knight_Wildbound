using UnityEngine;

namespace Roguelite.Core
{
    public enum InputMode
    {
        Gameplay,
        UI
    }

    public class InputStateManager : MonoBehaviour
    {
        private static InputStateManager instance;
        public static InputStateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<InputStateManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("InputStateManager");
                        instance = go.AddComponent<InputStateManager>();
                    }
                }
                return instance;
            }
        }

        public InputMode CurrentMode { get; private set; } = InputMode.Gameplay;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetGameplayMode();
        }

        public void SetGameplayMode()
        {
            CurrentMode = InputMode.Gameplay;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[InputState] Gameplay Mode");
        }

        public void SetUIMode()
        {
            CurrentMode = InputMode.UI;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[InputState] UI Mode");
        }
    }
}
