using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roguelite.Enemy;
using Roguelite.UI.Theme;

namespace Roguelite.UI.Widgets
{
    public class HUDBossBarWidget : MonoBehaviour
    {
        private GameObject barContainer;
        private Image hpFillImage;
        private Image hpChipImage;
        private TextMeshProUGUI bossTitleText;
        private TextMeshProUGUI bossHpText;

        private EnemyBase activeBoss;
        private float targetHpRatio = 1f;
        private float chipHpRatio = 1f;

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            // Top Center Container
            RectTransform container = HUDTheme.CreatePanel(transform, "BossBar_Container",
                HUDTheme.RoundedRect(10, HUDTheme.PanelFill, 2, HUDTheme.GoldAccent), HUDTheme.PanelFill);
            container.SetRect(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(580f, 54f));
            barContainer = container.gameObject;

            // Crown / Boss Badge Icon
            Image crownIcon = HUDTheme.CreateImage(container, "Boss_Crown_Icon", HUDTheme.Circle(HUDTheme.GoldAccent, 1, HUDTheme.WoodDark), HUDTheme.GoldAccent);
            crownIcon.rectTransform.SetRect(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(25f, 0f), new Vector2(32f, 32f));

            // Boss Title & Phase Label Text
            bossTitleText = HUDTheme.CreateText(container, "Boss_Title_Text", "GUARDIAO DA FLORESTA", 13f, HUDTheme.GoldAccent, TextAlignmentOptions.Left, FontStyles.Bold);
            bossTitleText.rectTransform.SetRect(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(50f, -6f), new Vector2(-60f, 20f));

            // HP Bar Background Frame
            RectTransform hpBg = HUDTheme.CreatePanel(container, "Boss_HP_BG",
                HUDTheme.RoundedRect(5, HUDTheme.HPRedDark * 0.5f, 1, HUDTheme.PanelBorder), Color.white);
            hpBg.SetRect(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(50f, 8f), new Vector2(-60f, 18f));

            // Chip Damage Fill (Trailing)
            hpChipImage = HUDTheme.CreateImage(hpBg, "Boss_HP_Chip", HUDTheme.RoundedRect(4, HUDTheme.HPChip, 0, Color.clear), HUDTheme.HPChip);
            hpChipImage.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            // HP Main Fill
            hpFillImage = HUDTheme.CreateImage(hpBg, "Boss_HP_Fill", HUDTheme.RoundedRect(4, HUDTheme.HPRed, 0, Color.clear), HUDTheme.HPRed);
            hpFillImage.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            // HP Text Numbers
            bossHpText = HUDTheme.CreateText(hpBg, "Boss_HP_Text", "500 / 500", 10f, HUDTheme.TextCream, TextAlignmentOptions.Center, FontStyles.Bold);
            bossHpText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            barContainer.SetActive(false);
        }

        private void Update()
        {
            FindActiveBoss();

            if (activeBoss == null || activeBoss.IsDead)
            {
                if (barContainer.activeSelf) barContainer.SetActive(false);
                return;
            }

            if (!barContainer.activeSelf) barContainer.SetActive(true);

            // Calculate HP Ratios
            float maxHp = Mathf.Max(1f, activeBoss.MaxHP);
            targetHpRatio = Mathf.Clamp01(activeBoss.CurrentHP / maxHp);

            // Smooth main fill Lerp
            if (hpFillImage != null)
            {
                hpFillImage.rectTransform.anchorMax = new Vector2(Mathf.Lerp(hpFillImage.rectTransform.anchorMax.x, targetHpRatio, Time.deltaTime * 10f), 1f);
            }

            // Smooth chip fill Lerp
            if (chipHpRatio > targetHpRatio)
            {
                chipHpRatio = Mathf.Lerp(chipHpRatio, targetHpRatio, Time.deltaTime * 3f);
            }
            else
            {
                chipHpRatio = targetHpRatio;
            }

            if (hpChipImage != null)
            {
                hpChipImage.rectTransform.anchorMax = new Vector2(chipHpRatio, 1f);
            }

            // Boss Title & Phase status
            string title = activeBoss.DisplayName.ToUpper();
            Color barColor = HUDTheme.HPRed;

            if (activeBoss is HollowTreeBossAI treeBoss)
            {
                if (treeBoss.IsPhase2)
                {
                    title += "  [FASE 2]";
                    barColor = new Color(0.95f, 0.15f, 0.25f);
                }
            }
            else if (activeBoss is BossAI bossAI)
            {
                if (bossAI.IsEnraged)
                {
                    title += "  [ENFURECIDO]";
                    barColor = new Color(0.95f, 0.25f, 0.1f);
                }
            }

            if (hpFillImage != null) hpFillImage.color = barColor;
            if (bossTitleText != null) bossTitleText.text = $"{title}";
            if (bossHpText != null) bossHpText.text = $"{Mathf.CeilToInt(activeBoss.CurrentHP)} / {Mathf.CeilToInt(maxHp)}";
        }

        private void FindActiveBoss()
        {
            if (activeBoss != null && !activeBoss.IsDead && IsBossValid(activeBoss)) return;

            activeBoss = null;
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead && enemy.IsBossEnemy && IsBossValid(enemy))
                {
                    activeBoss = enemy;
                    break;
                }
            }
        }

        private bool IsBossValid(EnemyBase enemy)
        {
            if (enemy is StoneGiantAI giant)
            {
                return giant.IsAwakened;
            }
            if (enemy is HollowTreeBossAI)
            {
                return Environment.BossActivationTrigger.IsBossActivated;
            }
            return true;
        }
    }
}
