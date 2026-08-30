using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Inventory;
using Roguelite.Progression;

namespace Roguelite.UI
{
    public class NotificationToast
    {
        public string text;
        public Color color;
        public float timeRemaining;

        public NotificationToast(string text, Color color, float duration = 3.2f)
        {
            this.text = text;
            this.color = color;
            this.timeRemaining = duration;
        }
    }

    /// <summary>
    /// HUD overlay providing real-time feedback for Gold count, item pickup toasts,
    /// Ring of Shadows cooldown indicator, and celebratory Boss Relic banners.
    /// </summary>
    public class LootNotificationUI : MonoBehaviour
    {
        private static LootNotificationUI instance;
        public static LootNotificationUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<LootNotificationUI>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("LootNotificationUI");
                        instance = go.AddComponent<LootNotificationUI>();
                    }
                }
                return instance;
            }
        }

        private readonly List<NotificationToast> activeToasts = new List<NotificationToast>();
        private NotificationToast relicBanner = null;

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
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemPickedUp += HandleItemPickedUp;
                InventoryManager.Instance.OnGoldChanged += HandleGoldChanged;
            }

            if (RelicManager.Instance != null)
            {
                RelicManager.Instance.OnRelicCollected += HandleRelicCollected;
            }
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemPickedUp -= HandleItemPickedUp;
                InventoryManager.Instance.OnGoldChanged -= HandleGoldChanged;
            }

            if (RelicManager.Instance != null)
            {
                RelicManager.Instance.OnRelicCollected -= HandleRelicCollected;
            }
        }

        private void Update()
        {
            // Update regular toasts
            for (int i = activeToasts.Count - 1; i >= 0; i--)
            {
                activeToasts[i].timeRemaining -= Time.deltaTime;
                if (activeToasts[i].timeRemaining <= 0f)
                {
                    activeToasts.RemoveAt(i);
                }
            }

            // Update relic banner
            if (relicBanner != null)
            {
                relicBanner.timeRemaining -= Time.deltaTime;
                if (relicBanner.timeRemaining <= 0f)
                {
                    relicBanner = null;
                }
            }
        }

        private void HandleItemPickedUp(ItemData item, int quantity, int totalGold)
        {
            if (item == null) return;
            string text = quantity > 1 ? $"+{quantity} {item.itemName}" : $"+1 {item.itemName}";
            activeToasts.Add(new NotificationToast(text, item.RarityColor));
        }

        private void HandleGoldChanged(int totalGold)
        {
            // Gold change notification handled when picking up physical gold
        }

        public void AddGoldToast(int amount)
        {
            activeToasts.Add(new NotificationToast($"+{amount} Gold", new Color(0.95f, 0.85f, 0.15f)));
        }

        private void HandleRelicCollected(ItemData relic)
        {
            string bannerText = $"🏆 CAMPAIGN RELIC OBTAINED!\n{relic?.itemName ?? "Seed of the Ancient Tree"} (+25 Max HP)";
            relicBanner = new NotificationToast(bannerText, new Color(0.20f, 0.85f, 0.40f), 5.5f);
        }

        public void ShowBannerNotification(string titleText, Color color, float duration = 5.0f)
        {
            relicBanner = new NotificationToast(titleText, color, duration);
        }

        private void OnGUI()
        {
            GUI.depth = -10;

            // 1. Top-Right Gold Counter & Controls Hint
            int currentGold = InventoryManager.Instance != null ? InventoryManager.Instance.Gold : 0;
            GUIStyle goldStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            goldStyle.normal.textColor = new Color(0.95f, 0.85f, 0.20f);

            Rect goldRect = new Rect(Screen.width - 220, 20, 200, 36);
            GUI.Box(goldRect, $"💰 {currentGold} Gold   [I] Inventory", goldStyle);

            // 2. Ring of Shadows [R] Ability Cooldown Tracker
            if (EquipmentManager.Instance != null && EquipmentManager.Instance.IsRingOfShadowsEquipped())
            {
                float cd = EquipmentManager.Instance.ShadowCooldownRemaining;
                bool isStealth = EquipmentManager.Instance.IsStealthActive;

                GUIStyle ringStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                Rect ringRect = new Rect(Screen.width - 220, 62, 200, 30);
                if (isStealth)
                {
                    ringStyle.normal.textColor = new Color(0.25f, 0.90f, 0.45f);
                    GUI.Box(ringRect, $"🌑 STEALTH ACTIVE ({StealthState.InvisibilityDurationRemaining:F1}s)", ringStyle);
                }
                else if (cd > 0f)
                {
                    ringStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                    GUI.Box(ringRect, $"🌑 Ring of Shadows [R] ({cd:F0}s)", ringStyle);
                }
                else
                {
                    ringStyle.normal.textColor = new Color(0.95f, 0.60f, 0.15f);
                    GUI.Box(ringRect, $"🌑 Ring of Shadows [R] READY", ringStyle);
                }
            }

            // 3. Pickup Toasts Queue (Bottom Left)
            float startY = Screen.height - 180;
            GUIStyle toastStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            for (int i = 0; i < activeToasts.Count; i++)
            {
                NotificationToast toast = activeToasts[i];
                float alpha = Mathf.Clamp01(toast.timeRemaining);
                Color col = toast.color;
                col.a = alpha;
                toastStyle.normal.textColor = col;

                Rect toastRect = new Rect(25, startY - (i * 38), 260, 32);
                GUI.Box(toastRect, $"  {toast.text}", toastStyle);
            }

            // 4. Celebratory Relic Banner (Center Screen Top)
            if (relicBanner != null)
            {
                GUIStyle bannerStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                bannerStyle.normal.textColor = relicBanner.color;

                Rect bannerRect = new Rect((Screen.width - 480) * 0.5f, 90, 480, 60);
                GUI.Box(bannerRect, relicBanner.text, bannerStyle);
            }
        }
    }
}
