using System.Collections;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Progression;

namespace Roguelite.UI
{
    /// <summary>
    /// Displays a 4-second center-screen floating reward card when a Campaign Relic is discovered.
    /// Does NOT pause the game or force open menus.
    /// </summary>
    public class RelicDiscoveryUI : MonoBehaviour
    {
        private static RelicDiscoveryUI instance;
        public static RelicDiscoveryUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<RelicDiscoveryUI>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("RelicDiscoveryUI");
                        instance = go.AddComponent<RelicDiscoveryUI>();
                    }
                }
                return instance;
            }
        }

        private ItemData activeRelic;
        private float displayTimer = 0f;
        private const float TOTAL_DISPLAY_TIME = 4.0f;
        private const float FADE_TIME = 0.6f;

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

        private void OnEnable()
        {
            if (RelicManager.Instance != null)
            {
                RelicManager.Instance.OnRelicCollected += HandleRelicCollected;
            }
        }

        private void OnDisable()
        {
            if (RelicManager.Instance != null)
            {
                RelicManager.Instance.OnRelicCollected -= HandleRelicCollected;
            }
        }

        private void Start()
        {
            if (RelicManager.Instance != null)
            {
                RelicManager.Instance.OnRelicCollected -= HandleRelicCollected;
                RelicManager.Instance.OnRelicCollected += HandleRelicCollected;
            }
        }

        public void HandleRelicCollected(ItemData relicItem)
        {
            if (relicItem == null) return;
            activeRelic = relicItem;
            displayTimer = TOTAL_DISPLAY_TIME;
            Debug.Log($"[RelicDiscoveryUI] Showing 4s relic reward card for '{relicItem.itemName}'");
        }

        public void TriggerDiscovery(string title, string bonus)
        {
            ItemData dummy = ScriptableObject.CreateInstance<ItemData>();
            dummy.itemName = title;
            dummy.description = bonus;
            HandleRelicCollected(dummy);
        }

        private void Update()
        {
            if (displayTimer > 0f)
            {
                displayTimer -= Time.deltaTime;
            }
        }

        private void OnGUI()
        {
            if (displayTimer <= 0f || activeRelic == null) return;

            GUI.depth = -30;

            float alpha = 1.0f;
            if (displayTimer < FADE_TIME)
            {
                alpha = Mathf.Clamp01(displayTimer / FADE_TIME);
            }
            else if (TOTAL_DISPLAY_TIME - displayTimer < FADE_TIME)
            {
                alpha = Mathf.Clamp01((TOTAL_DISPLAY_TIME - displayTimer) / FADE_TIME);
            }

            float cardW = 380f;
            float cardH = 140f;
            float cardX = (Screen.width - cardW) * 0.5f;
            float cardY = (Screen.height - cardH) * 0.35f;

            Rect cardRect = new Rect(cardX, cardY, cardW, cardH);

            // Dark Gold / Emerald translucent card background
            GUI.color = new Color(0.04f, 0.08f, 0.06f, 0.92f * alpha);
            GUI.DrawTexture(cardRect, Texture2D.whiteTexture);

            // Glowing Emerald border
            GUI.color = new Color(0.2f, 0.9f, 0.4f, 0.95f * alpha);
            GUI.Box(cardRect, "");

            // Top Header: 🏆 RELIC DISCOVERED
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = new Color(1.0f, 0.85f, 0.3f, alpha);
            GUI.Label(new Rect(cardX, cardY + 12, cardW, 22), "🏆 RELIC DISCOVERED", headerStyle);

            // Relic Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.3f, 1.0f, 0.5f, alpha);
            GUI.Label(new Rect(cardX, cardY + 38, cardW, 28), activeRelic.itemName, titleStyle);

            // Permanent Bonus Subtitle
            GUIStyle bonusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bonusStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, alpha);

            string bonusText = GetRelicBonusText(activeRelic);
            GUI.Label(new Rect(cardX, cardY + 75, cardW, 22), "Permanent Bonus:", bonusStyle);

            GUIStyle detailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            detailStyle.normal.textColor = new Color(0.2f, 0.9f, 0.4f, alpha);
            GUI.Label(new Rect(cardX, cardY + 98, cardW, 24), bonusText, detailStyle);
        }

        private string GetRelicBonusText(ItemData relic)
        {
            if (relic == null) return "+25 Max HP";
            if (relic.itemId == "relic_tree_seed" || relic.itemName.Contains("Ancient Tree")) return "+25 Max HP";
            if (!string.IsNullOrEmpty(relic.description)) return relic.description;
            return "+25 Max HP";
        }
    }
}
