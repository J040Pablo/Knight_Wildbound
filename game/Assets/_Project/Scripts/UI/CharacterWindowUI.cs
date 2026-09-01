using UnityEngine;
using Roguelite.Core.StateMachine;

namespace Roguelite.UI
{
    /// <summary>
    /// Manager for the unified character window state (Inventory, Mastery, Settings).
    /// Listens for E and ESC key inputs to toggle state via GameStateManager.
    /// All visual rendering is delegated to the UGUI-based InventoryBookUI.
    /// </summary>
    public class CharacterWindowUI : MonoBehaviour
    {
        private static CharacterWindowUI instance;
        private static bool applicationIsQuitting = false;

        public static CharacterWindowUI Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<CharacterWindowUI>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("CharacterWindowUI");
                        instance = go.AddComponent<CharacterWindowUI>();
                    }
                }
                return instance;
            }
        }

        public enum WindowTab
        {
            Inventory = 0,
            Mastery = 1,
            Settings = 2
        }

        public bool isOpen = false;
        public WindowTab currentTab = WindowTab.Inventory;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            isOpen = false;
        }

        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                isOpen = (GameStateManager.Instance.CurrentState == GameState.Inventory || GameStateManager.Instance.CurrentState == GameState.Mastery);
            }
            else
            {
                isOpen = false;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Inventory)
            {
                isOpen = true;
                currentTab = WindowTab.Inventory;
            }
            else if (newState == GameState.Mastery)
            {
                isOpen = true;
                currentTab = WindowTab.Mastery;
            }
            else if (newState == GameState.Gameplay)
            {
                isOpen = false;
            }
        }

        public void OpenTab(WindowTab tab)
        {
            currentTab = tab;
            isOpen = true;
            GameState targetState = tab == WindowTab.Mastery ? GameState.Mastery : GameState.Inventory;
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(targetState);
            }
        }

        public void CloseWindow()
        {
            isOpen = false;
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
            }
        }

        public void ToggleTab(WindowTab tab)
        {
            if (isOpen && currentTab == tab)
            {
                CloseWindow();
            }
            else
            {
                OpenTab(tab);
            }
        }

        public void SwitchTab(WindowTab newTab)
        {
            currentTab = newTab;
            GameState targetState = newTab == WindowTab.Mastery ? GameState.Mastery : GameState.Inventory;
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(targetState);
            }
        }

        private void Update()
        {
            // [E] opens or closes character window / book inventory
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (isOpen)
                {
                    CloseWindow();
                }
                else
                {
                    OpenTab(currentTab);
                }
            }
            else if (isOpen)
            {
                // [Tab] cycles between Inventory (0) -> Mastery (1) -> Settings (2)
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    int nextIndex = ((int)currentTab + 1) % 3;
                    SwitchTab((WindowTab)nextIndex);
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseWindow();
                }
            }
        }

        public void RefreshMastery()
        {
            if (InventoryBookUI.Instance != null)
            {
                InventoryBookUI.Instance.RefreshUI();
            }
        }

        private void OnGUI()
        {
            // Legacy OnGUI disabled - InventoryBookUI renders all tabs inside the UGUI Ancient Book.
        }
    }
}
