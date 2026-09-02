using System.Collections;
using UnityEngine;
using Roguelite.Core;
using Roguelite.Loot;
using Roguelite.Player;

namespace Roguelite.Environment
{
    /// <summary>
    /// Interactive world treasure chest. Players approach and press [E] to trigger the opening sequence.
    /// Pops out equipment, potions, and gold in a physical arc around the chest.
    /// </summary>
    public class TreasureChest : MonoBehaviour, IInteractable
    {
        [Header("Chest Properties")]
        public ChestRarity chestRarity = ChestRarity.Common;
        public bool isOpened = false;

        private GameObject lidMesh;
        private bool isAnimating = false;

        private void Start()
        {
            BuildChestVisuals();
        }

        private void BuildChestVisuals()
        {
            // Base Box
            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "ChestBase";
            baseBox.transform.SetParent(transform, false);
            baseBox.transform.localPosition = new Vector3(0, 0.4f, 0);
            baseBox.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);

            Renderer bRen = baseBox.GetComponent<Renderer>();
            if (bRen != null)
            {
                bRen.material.color = GetChestColor();
            }

            // Hinged Lid
            lidMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lidMesh.name = "ChestLid";
            lidMesh.transform.SetParent(transform, false);
            lidMesh.transform.localPosition = new Vector3(0, 0.95f, 0);
            lidMesh.transform.localScale = new Vector3(1.25f, 0.3f, 0.85f);

            Renderer lRen = lidMesh.GetComponent<Renderer>();
            if (lRen != null)
            {
                lRen.material.color = GetChestColor() * 1.15f;
            }
        }

        private Color GetChestColor()
        {
            switch (chestRarity)
            {
                case ChestRarity.Legendary: return new Color(0.95f, 0.56f, 0.10f); // Gold / Orange
                case ChestRarity.Epic:      return new Color(0.64f, 0.27f, 0.90f); // Purple
                case ChestRarity.Rare:      return new Color(0.25f, 0.55f, 0.95f); // Blue
                case ChestRarity.Common:
                default:                   return new Color(0.45f, 0.30f, 0.15f); // Wooden Brown
            }
        }

        public string InteractionPrompt => isOpened ? "" : $"F — Abrir Baú ({chestRarity})";

        public bool CanInteract(GameObject player)
        {
            return !isOpened && !isAnimating;
        }

        public void Interact(GameObject player)
        {
            if (!CanInteract(player)) return;

            // 1. Retrieve current active mount (or mount from player hierarchy)
            MountSystem mount = MountSystem.ActiveMount;
            if (mount == null && player != null)
            {
                mount = player.GetComponentInParent<MountSystem>();
            }
            if (mount == null)
            {
                mount = FindFirstObjectByType<MountSystem>();
            }

            // 2. Force dismount if player is mounted
            if (mount != null && mount.IsPlayerMounted)
            {
                mount.ForceDismount();
            }

            // 3. Guarantee camera focus and ownership are restored to player
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.ForceRestorePlayerCamera("TreasureChest.Interact");
            }

            StartCoroutine(OpenSequence());
        }

        private IEnumerator OpenSequence()
        {
            isAnimating = true;

            // Lid rotation animation
            if (lidMesh != null)
            {
                float t = 0f;
                Vector3 startPos = lidMesh.transform.localPosition;
                Vector3 targetPos = startPos + new Vector3(0, 0.3f, -0.4f);

                while (t < 0.4f)
                {
                    t += Time.deltaTime;
                    float lerp = t / 0.4f;
                    lidMesh.transform.localPosition = Vector3.Lerp(startPos, targetPos, lerp);
                    lidMesh.transform.localRotation = Quaternion.Euler(-45f * lerp, 0, 0);
                    yield return null;
                }
            }

            isOpened = true;
            isAnimating = false;

            // Generate & Spawn Loot
            LootResult rewards = ChestLootTable.GenerateRewards(chestRarity);
            LootDrop.SpawnFromResult(rewards, transform.position + Vector3.up * 0.8f);

            // Debug.Log($"[TreasureChest] Opened {chestRarity} chest! Spawned {rewards.droppedItems.Count} items & {rewards.goldAmount} gold.");
        }
    }
}
