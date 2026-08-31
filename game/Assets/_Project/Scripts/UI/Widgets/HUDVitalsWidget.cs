using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roguelite.Player;
using Roguelite.Progression;
using Roguelite.Core.Events;
using Roguelite.UI.Theme;

namespace Roguelite.UI.Widgets
{
    public class HUDVitalsWidget : MonoBehaviour
    {
        private PlayerStats playerStats;

        private Image hpFillImage;
        private Image hpChipImage;
        private Image hpBorderImage;
        private TextMeshProUGUI hpText;

        private Image staminaFillImage;
        private TextMeshProUGUI staminaText;

        private Image xpFillImage;
        private TextMeshProUGUI xpText;

        private GameObject levelUpBanner;
        private TextMeshProUGUI levelUpText;

        private float targetHpRatio = 1f;
        private float chipHpRatio = 1f;
        private float targetStamRatio = 1f;
        private float targetXpRatio = 0f;

        private float levelUpFlashTimer = 0f;

        private void Awake()
        {
            BuildUI();
        }

        private void OnEnable()
        {
            GameEvents.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelUp -= HandleLevelUp;
        }

        private void Start()
        {
            FindPlayerStats();
        }

        private void FindPlayerStats()
        {
            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>();
            }
        }

        private void BuildUI()
        {
            // Main Root Panel - Bottom Left
            RectTransform container = HUDTheme.CreatePanel(transform, "Vitals_Container",
                HUDTheme.RoundedRect(12, HUDTheme.PanelFill, 2, HUDTheme.PanelBorder), HUDTheme.PanelFill);
            container.SetRect(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(380f, 110f));

            // --- HP BAR ---
            // HP Bar Background
            RectTransform hpBg = HUDTheme.CreatePanel(container, "HP_Bar_BG",
                HUDTheme.RoundedRect(6, HUDTheme.HPRedDark * 0.4f, 1, HUDTheme.PanelBorder), HUDTheme.HPRedDark * 0.4f);
            hpBg.SetRect(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(-32f, 26f));

            // HP Chip Damage Fill (Trailing)
            hpChipImage = HUDTheme.CreateImage(hpBg, "HP_Bar_Chip", HUDTheme.RoundedRect(4, HUDTheme.HPChip, 0, Color.clear), HUDTheme.HPChip);
            RectTransform hpChipRt = hpChipImage.rectTransform;
            hpChipRt.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            // HP Main Fill
            hpFillImage = HUDTheme.CreateImage(hpBg, "HP_Bar_Fill", HUDTheme.RoundedRect(4, HUDTheme.HPRed, 0, Color.clear), HUDTheme.HPRed);
            RectTransform hpFillRt = hpFillImage.rectTransform;
            hpFillRt.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            // HP Border Overlay / Glow
            hpBorderImage = HUDTheme.CreateImage(hpBg, "HP_Bar_Border", HUDTheme.RoundedRect(6, Color.clear, 1, HUDTheme.GoldAccent), Color.white);
            hpBorderImage.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hpBorderImage.color = Color.clear;

            // HP Text
            hpText = HUDTheme.CreateText(hpBg, "HP_Text", "100 / 100", 13f, HUDTheme.TextCream, TextAlignmentOptions.Center, FontStyles.Bold);
            hpText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // --- STAMINA BAR ---
            RectTransform stamBg = HUDTheme.CreatePanel(container, "Stam_Bar_BG",
                HUDTheme.RoundedRect(5, new Color(0.08f, 0.15f, 0.08f, 0.8f), 1, HUDTheme.PanelBorder), Color.white);
            stamBg.SetRect(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(16f, -46f), new Vector2(-32f, 18f));

            staminaFillImage = HUDTheme.CreateImage(stamBg, "Stam_Bar_Fill", HUDTheme.RoundedRect(4, HUDTheme.StaminaGreen, 0, Color.clear), HUDTheme.StaminaGreen);
            staminaFillImage.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            staminaText = HUDTheme.CreateText(stamBg, "Stam_Text", "STAMINA", 10f, HUDTheme.TextCream, TextAlignmentOptions.Center, FontStyles.Bold);
            staminaText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // --- XP BAR ---
            RectTransform xpBg = HUDTheme.CreatePanel(container, "XP_Bar_BG",
                HUDTheme.RoundedRect(4, new Color(0.05f, 0.1f, 0.18f, 0.8f), 1, HUDTheme.PanelBorder), Color.white);
            xpBg.SetRect(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(16f, 12f), new Vector2(-32f, 14f));

            xpFillImage = HUDTheme.CreateImage(xpBg, "XP_Bar_Fill", HUDTheme.RoundedRect(3, HUDTheme.XPBlue, 0, Color.clear), HUDTheme.XPBlue);
            xpFillImage.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            xpText = HUDTheme.CreateText(xpBg, "XP_Text", "XP", 9f, HUDTheme.TextDim, TextAlignmentOptions.Center, FontStyles.Bold);
            xpText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // --- LEVEL UP / MASTERY BANNER ---
            RectTransform levelBannerRt = HUDTheme.CreatePanel(transform, "LevelUp_Banner",
                HUDTheme.RoundedRect(8, new Color(0.1f, 0.25f, 0.45f, 0.95f), 2, HUDTheme.GoldAccent), Color.white);
            levelBannerRt.SetRect(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 150f), new Vector2(380f, 40f));
            levelUpBanner = levelBannerRt.gameObject;

            levelUpText = HUDTheme.CreateText(levelBannerRt, "Banner_Text", "PONTOS DE MAESTRIA DISPONÍVEIS [Q]", 12f, HUDTheme.GoldAccent, TextAlignmentOptions.Center, FontStyles.Bold);
            levelUpText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            levelUpBanner.SetActive(false);
        }

        private void HandleLevelUp(int level)
        {
            levelUpFlashTimer = 2.5f;
        }

        private void Update()
        {
            if (playerStats == null)
            {
                FindPlayerStats();
                if (playerStats == null) return;
            }

            // Update Target Values
            float maxHp = Mathf.Max(1f, playerStats.MaxHP);
            targetHpRatio = Mathf.Clamp01(playerStats.CurrentHP / maxHp);

            float maxStam = Mathf.Max(1f, playerStats.MaxStamina);
            targetStamRatio = Mathf.Clamp01(playerStats.CurrentStamina / maxStam);

            int currentXp = playerStats.CurrentXP;
            int xpToNext = playerStats.XPToNextLevel;
            int level = playerStats.Level;

            if (ProgressionManager.Instance != null)
            {
                currentXp = ProgressionManager.Instance.CurrentLevelXP;
                level = ProgressionManager.Instance.CurrentLevel;
                xpToNext = ProgressionManager.Instance.GetXPRequired(level);
            }
            targetXpRatio = xpToNext > 0 ? Mathf.Clamp01((float)currentXp / xpToNext) : 1f;

            // Smooth Fill Animation
            if (hpFillImage != null)
            {
                hpFillImage.rectTransform.anchorMax = new Vector2(targetHpRatio, 1f);
            }

            // Chip Damage Animation (lagging behind)
            if (chipHpRatio > targetHpRatio)
            {
                chipHpRatio = Mathf.Lerp(chipHpRatio, targetHpRatio, Time.deltaTime * 3.5f);
            }
            else
            {
                chipHpRatio = targetHpRatio;
            }

            if (hpChipImage != null)
            {
                hpChipImage.rectTransform.anchorMax = new Vector2(chipHpRatio, 1f);
            }

            if (staminaFillImage != null)
            {
                staminaFillImage.rectTransform.anchorMax = new Vector2(Mathf.Lerp(staminaFillImage.rectTransform.anchorMax.x, targetStamRatio, Time.deltaTime * 12f), 1f);
            }

            if (xpFillImage != null)
            {
                xpFillImage.rectTransform.anchorMax = new Vector2(Mathf.Lerp(xpFillImage.rectTransform.anchorMax.x, targetXpRatio, Time.deltaTime * 10f), 1f);
            }

            // Text Updates
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(playerStats.CurrentHP)} / {Mathf.CeilToInt(maxHp)}";
            }

            if (staminaText != null)
            {
                staminaText.text = $"STAMINA  {Mathf.CeilToInt(playerStats.CurrentStamina)}";
            }

            if (xpText != null)
            {
                xpText.text = $"XP  {currentXp} / {xpToNext}";
            }

            // Low HP Warning Flash (< 25%)
            if (targetHpRatio < 0.25f && !playerStats.IsDead)
            {
                float flash = Mathf.PingPong(Time.time * 4f, 0.7f) + 0.3f;
                hpBorderImage.color = new Color(HUDTheme.HPRed.r, HUDTheme.HPRed.g, HUDTheme.HPRed.b, flash);
            }
            else
            {
                hpBorderImage.color = Color.clear;
            }

            // Level Up / Pending Mastery Banner Check
            int pendingPoints = ProgressionManager.Instance != null ? ProgressionManager.Instance.PendingLevelUpCount : 0;
            if (pendingPoints > 0 || levelUpFlashTimer > 0f)
            {
                if (!levelUpBanner.activeSelf) levelUpBanner.SetActive(true);

                if (levelUpFlashTimer > 0f)
                {
                    levelUpFlashTimer -= Time.deltaTime;
                    float flashScale = 1f + Mathf.PingPong(Time.time * 6f, 0.12f);
                    levelUpBanner.transform.localScale = Vector3.one * flashScale;
                    levelUpText.text = $"NÍVEL ALCANÇADO! [{level}]  •  PONTOS: {pendingPoints}";
                }
                else
                {
                    levelUpBanner.transform.localScale = Vector3.one;
                    levelUpText.text = $"PONTOS DE MAESTRIA PENDENTES ({pendingPoints})  [Q]";
                }
            }
            else
            {
                if (levelUpBanner.activeSelf) levelUpBanner.SetActive(false);
            }
        }
    }
}
