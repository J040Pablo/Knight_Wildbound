using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roguelite.Player;
using Roguelite.Inventory;
using Roguelite.Progression;
using Roguelite.UI.Theme;

namespace Roguelite.UI.Widgets
{
    public class HUDAbilityBarWidget : MonoBehaviour
    {
        private class AbilitySlotUI
        {
            public RectTransform Container;
            public Image IconImage;
            public Image CooldownRadial;
            public TextMeshProUGUI CooldownText;
            public TextMeshProUGUI KeyText;
            public TextMeshProUGUI NameText;
            public GameObject LockOverlay;
            public TextMeshProUGUI LockText;
        }

        private AbilitySlotUI[] slots = new AbilitySlotUI[4];
        private SpecialAbilitySystem specialAbilitySystem;
        private EquipmentManager equipmentManager;

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            FindSystems();
        }

        private void FindSystems()
        {
            if (specialAbilitySystem == null)
            {
                specialAbilitySystem = FindFirstObjectByType<SpecialAbilitySystem>();
            }
            if (equipmentManager == null)
            {
                equipmentManager = EquipmentManager.Instance;
            }
        }

        private void BuildUI()
        {
            // Main Bottom Center Container
            RectTransform barContainer = HUDTheme.CreatePanel(transform, "AbilityBar_Container",
                HUDTheme.RoundedRect(12, HUDTheme.PanelFill, 2, HUDTheme.PanelBorder), HUDTheme.PanelFill);
            barContainer.SetRect(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(340f, 75f));

            float slotWidth = 65f;
            float slotHeight = 65f;
            float spacing = 12f;
            float startX = -((4 * slotWidth + 3 * spacing) * 0.5f) + slotWidth * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                AbilitySlotUI slot = new AbilitySlotUI();

                // Slot Frame Panel
                slot.Container = HUDTheme.CreatePanel(barContainer, $"Slot_{i}",
                    HUDTheme.RoundedRect(8, HUDTheme.WoodDark, 2, HUDTheme.PanelBorder), HUDTheme.WoodDark);
                slot.Container.SetRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + i * (slotWidth + spacing), 0f), new Vector2(slotWidth, slotHeight));

                // Slot Icon
                slot.IconImage = HUDTheme.CreateImage(slot.Container, "Slot_Icon", HUDTheme.IconSword(HUDTheme.GoldAccent), HUDTheme.GoldAccent);
                slot.IconImage.rectTransform.SetRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));

                // Cooldown Radial Fill Overlay
                slot.CooldownRadial = HUDTheme.CreateImage(slot.Container, "Cooldown_Radial",
                    HUDTheme.Circle(Color.white), new Color(0f, 0f, 0f, 0.75f), Image.Type.Filled);
                slot.CooldownRadial.fillMethod = Image.FillMethod.Radial360;
                slot.CooldownRadial.fillOrigin = (int)Image.Origin360.Top;
                slot.CooldownRadial.fillClockwise = true;
                slot.CooldownRadial.fillAmount = 0f;
                slot.CooldownRadial.rectTransform.SetRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));

                // Cooldown Text
                slot.CooldownText = HUDTheme.CreateText(slot.Container, "Cooldown_Text", "", 14f, HUDTheme.TextCream, TextAlignmentOptions.Center, FontStyles.Bold);
                slot.CooldownText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                // Key Binding Badge Box
                RectTransform keyBadge = HUDTheme.CreatePanel(slot.Container, "Key_Badge",
                    HUDTheme.RoundedRect(4, HUDTheme.WoodMid, 1, HUDTheme.GoldAccent), HUDTheme.WoodMid);
                keyBadge.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-4f, 4f), new Vector2(24f, 18f));

                slot.KeyText = HUDTheme.CreateText(keyBadge, "Key_Text", "LMB", 9f, HUDTheme.GoldAccent, TextAlignmentOptions.Center, FontStyles.Bold);
                slot.KeyText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                // Ability Name Label
                slot.NameText = HUDTheme.CreateText(slot.Container, "Name_Text", "", 9f, HUDTheme.TextDim, TextAlignmentOptions.Center, FontStyles.Normal);
                slot.NameText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -14f), new Vector2(80f, 14f));

                // Lock Overlay (for Q when unlearned or "EM BREVE", or R when item unequipped)
                RectTransform lockRt = HUDTheme.CreatePanel(slot.Container, "Lock_Overlay",
                    HUDTheme.RoundedRect(8, new Color(0.05f, 0.05f, 0.05f, 0.82f), 1, HUDTheme.PanelBorder), Color.white);
                lockRt.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                slot.LockOverlay = lockRt.gameObject;

                slot.LockText = HUDTheme.CreateText(lockRt, "Lock_Text", "BLOQUEADO", 9f, HUDTheme.CommonGray, TextAlignmentOptions.Center, FontStyles.Bold);
                slot.LockText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                slot.LockOverlay.SetActive(false);

                slots[i] = slot;
            }

            // Configure Static Slot Metadata
            // Slot 0: Basic Attack
            slots[0].IconImage.sprite = HUDTheme.IconSword(HUDTheme.GoldAccent);
            slots[0].KeyText.text = "LMB";
            slots[0].NameText.text = "Básico";

            // Slot 1: Charged Attack
            slots[1].IconImage.sprite = HUDTheme.IconBurst(HUDTheme.LegendaryOrange);
            slots[1].IconImage.color = HUDTheme.LegendaryOrange;
            slots[1].KeyText.text = "RMB";
            slots[1].NameText.text = "Carregado";

            // Slot 2: Special Ability (Q)
            slots[2].IconImage.sprite = HUDTheme.IconPinwheel(HUDTheme.RareBlue);
            slots[2].IconImage.color = HUDTheme.RareBlue;
            slots[2].KeyText.text = "Q";
            slots[2].NameText.text = "Especial";

            // Slot 3: Ring of Shadows (R)
            slots[3].IconImage.sprite = HUDTheme.IconCrescent(HUDTheme.EpicPurple);
            slots[3].IconImage.color = HUDTheme.EpicPurple;
            slots[3].KeyText.text = "R";
            slots[3].NameText.text = "Sombras";
        }

        private void Update()
        {
            FindSystems();

            // Update Slot 2 (Special Q)
            UpdateSpecialAbilitySlot(slots[2]);

            // Update Slot 3 (Ring of Shadows R)
            UpdateRingOfShadowsSlot(slots[3]);
        }

        private void UpdateSpecialAbilitySlot(AbilitySlotUI slot)
        {
            ClassType classType = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentClass : ClassType.Knight;

            // Check if special ability is available for this class
            if (classType == ClassType.Mage || classType == ClassType.Druid)
            {
                // Check if ultimate/special ability unlocked in tree but system is Knight-only gap
                bool hasMageDruidAbilityUnlocked = ProgressionManager.Instance != null && (
                    ProgressionManager.Instance.HasAbility(AbilityId.MageMeteor) ||
                    ProgressionManager.Instance.HasAbility(AbilityId.MageArcaneStorm) ||
                    ProgressionManager.Instance.HasAbility(AbilityId.DruidNatureWrath));

                if (hasMageDruidAbilityUnlocked)
                {
                    slot.LockOverlay.SetActive(true);
                    slot.LockText.text = "EM BREVE";
                    slot.CooldownRadial.fillAmount = 0f;
                    slot.CooldownText.text = "";
                }
                else
                {
                    slot.LockOverlay.SetActive(true);
                    slot.LockText.text = "BLOQUEADO";
                    slot.CooldownRadial.fillAmount = 0f;
                    slot.CooldownText.text = "";
                }
                return;
            }

            // Knight Special Abilities
            if (specialAbilitySystem == null || !specialAbilitySystem.CanUseAbility() && specialAbilitySystem.CooldownSecondsRemaining <= 0f)
            {
                slot.LockOverlay.SetActive(true);
                slot.LockText.text = "BLOQUEADO";
                slot.CooldownRadial.fillAmount = 0f;
                slot.CooldownText.text = "";
                return;
            }

            slot.LockOverlay.SetActive(false);

            float cdRemaining = specialAbilitySystem != null ? specialAbilitySystem.CooldownSecondsRemaining : 0f;
            float ratio = specialAbilitySystem != null ? specialAbilitySystem.GetCooldownRatio() : 0f;

            if (cdRemaining > 0f)
            {
                slot.CooldownRadial.fillAmount = ratio;
                slot.CooldownText.text = $"{cdRemaining:F1}s";
            }
            else
            {
                slot.CooldownRadial.fillAmount = 0f;
                slot.CooldownText.text = "";
            }
        }

        private void UpdateRingOfShadowsSlot(AbilitySlotUI slot)
        {
            bool isEquipped = equipmentManager != null && equipmentManager.IsRingOfShadowsEquipped();

            if (!isEquipped)
            {
                slot.LockOverlay.SetActive(true);
                slot.LockText.text = "SEM ANEL";
                slot.CooldownRadial.fillAmount = 0f;
                slot.CooldownText.text = "";
                return;
            }

            slot.LockOverlay.SetActive(false);

            float remaining = equipmentManager.ShadowCooldownRemaining;
            float max = equipmentManager.ShadowCooldownMax;

            if (remaining > 0f && max > 0f)
            {
                slot.CooldownRadial.fillAmount = Mathf.Clamp01(remaining / max);
                slot.CooldownText.text = $"{remaining:F0}s";
            }
            else
            {
                slot.CooldownRadial.fillAmount = 0f;
                slot.CooldownText.text = "";
            }
        }
    }
}
