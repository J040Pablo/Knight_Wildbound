using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Inventory;
using Roguelite.Player;
using Roguelite.Progression;
using Roguelite.Core;
using Roguelite.Core.StateMachine;

namespace Roguelite.UI
{
    public class InventoryUI : MonoBehaviour
    {
        private static InventoryUI instance;
        private static bool applicationIsQuitting = false;

        public static InventoryUI Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<InventoryUI>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("InventoryUI");
                        instance = go.AddComponent<InventoryUI>();
                    }
                }
                return instance;
            }
        }

        public bool isOpen = false;

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

        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
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
            isOpen = (newState == GameState.Inventory);
        }

        public void ToggleInventory()
        {
            if (CharacterWindowUI.Instance != null)
            {
                CharacterWindowUI.Instance.ToggleTab(CharacterWindowUI.WindowTab.Inventory);
            }
            else if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ToggleState(GameState.Inventory);
            }
        }

        private void OnGUI()
        {
            // CharacterWindowUI handles unified tabbed OnGUI rendering.
        }
    }
}
