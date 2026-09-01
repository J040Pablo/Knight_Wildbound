using UnityEngine;
using Roguelite.Core;
using Roguelite.Player;
using Roguelite.Items;
using Roguelite.UI;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    public class RoyalBridgeGate : MonoBehaviour, IInteractable
    {
        public bool IsUnlocked { get; private set; } = false;

        public string InteractionPrompt => IsUnlocked 
            ? "Ponte Real Aberta" 
            : "F — Inserir Chave Cristalina";

        public bool CanInteract(GameObject player)
        {
            return !IsUnlocked;
        }

        public void Interact(GameObject player)
        {
            if (IsUnlocked) return;

            // Check if player has Chave Cristalina in inventory or allow interaction
            PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
            
            // Unlock the gate
            IsUnlocked = true;
            // Debug.Log("🔑 [PONTE REAL UNLOCKED] — Royal Bridge opened using Chave Cristalina!");

            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner("✨ PONTE REAL DESBLOQUEADA — Acesso à Corte Real Concedido!");
            }

            // Lower / open gate obstacle
            Transform visual = transform.Find("GateVisual");
            if (visual != null)
            {
                visual.gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject, 0.5f);
            }
        }
    }
}
