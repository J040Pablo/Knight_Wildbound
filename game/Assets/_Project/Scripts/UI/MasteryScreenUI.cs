using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Progression;
using Roguelite.Core;
using Roguelite.Core.StateMachine;

namespace Roguelite.UI
{
    /// <summary>
    /// Legacy wrapper for Mastery state toggling. Delegates window controls
    /// to CharacterWindowUI and InventoryBookUI.
    /// </summary>
    public class MasteryScreenUI : MonoBehaviour
    {
        public static bool IsAnyMenuOpen { get; set; } = false;

        private bool isOpen = false;
        public bool IsOpen => isOpen;

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            isOpen = (newState == GameState.Mastery);
            IsAnyMenuOpen = (newState == GameState.Mastery || newState == GameState.Inventory);
            if (isOpen)
            {
                Refresh();
            }
        }

        public void Open()
        {
            if (CharacterWindowUI.Instance != null)
            {
                CharacterWindowUI.Instance.OpenTab(CharacterWindowUI.WindowTab.Mastery);
            }
            else if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Mastery);
            }
            else
            {
                isOpen = true;
                IsAnyMenuOpen = true;
            }
        }

        public void Close()
        {
            if (CharacterWindowUI.Instance != null)
            {
                CharacterWindowUI.Instance.CloseWindow();
            }
            else if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
            }
            else
            {
                isOpen = false;
                IsAnyMenuOpen = false;
            }
        }

        public void Toggle()
        {
            if (CharacterWindowUI.Instance != null)
            {
                CharacterWindowUI.Instance.ToggleTab(CharacterWindowUI.WindowTab.Mastery);
            }
            else if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ToggleState(GameState.Mastery);
            }
            else
            {
                if (isOpen) Close();
                else Open();
            }
        }

        private void Update()
        {
            IsAnyMenuOpen = (CharacterWindowUI.Instance != null && CharacterWindowUI.Instance.isOpen);
        }

        public void Refresh()
        {
            if (InventoryBookUI.Instance != null)
            {
                InventoryBookUI.Instance.RefreshUI();
            }
        }

        private void OnGUI()
        {
            // Legacy OnGUI disabled - InventoryBookUI renders Mastery inside the UGUI book.
        }
    }
}
