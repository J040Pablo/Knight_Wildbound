using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roguelite.Core;
using Roguelite.Progression;
using Roguelite.UI.Theme;

namespace Roguelite.UI.Widgets
{
    public class HUDPlayerInfoWidget : MonoBehaviour
    {
        private Image classIconImage;
        private TextMeshProUGUI playerHeaderText;
        private TextMeshProUGUI masteryTrailsText;

        private TextMeshProUGUI timerText;
        private TextMeshProUGUI regionText;

        private RunManager runManager;

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            runManager = FindFirstObjectByType<RunManager>();
        }

        private void BuildUI()
        {
            // --- TOP LEFT: CLASS & MASTERY INFO ---
            RectTransform topLeftContainer = HUDTheme.CreatePanel(transform, "TopLeft_Info_Container",
                HUDTheme.RoundedRect(10, HUDTheme.PanelFill, 2, HUDTheme.PanelBorder), HUDTheme.PanelFill);
            topLeftContainer.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(360f, 65f));

            // Class Icon Circle Backdrop
            Image iconBg = HUDTheme.CreateImage(topLeftContainer, "Class_Icon_BG", HUDTheme.Circle(HUDTheme.WoodDark, 2, HUDTheme.GoldAccent), Color.white);
            iconBg.rectTransform.SetRect(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(46f, 46f));

            // Class Icon Image (SDF Shield / Wand / Leaf)
            classIconImage = HUDTheme.CreateImage(iconBg.transform, "Class_Icon_Img", HUDTheme.IconShield(HUDTheme.GoldAccent), HUDTheme.GoldAccent);
            classIconImage.rectTransform.SetRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));

            // Player Header Text (Level & Class Name)
            playerHeaderText = HUDTheme.CreateText(topLeftContainer, "Player_Header_Text", "LV 1 • CAVALEIRO", 14f, HUDTheme.GoldAccent, TextAlignmentOptions.Left, FontStyles.Bold);
            playerHeaderText.rectTransform.SetRect(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(64f, -10f), new Vector2(-74f, 22f));

            // Mastery Trails Text
            masteryTrailsText = HUDTheme.CreateText(topLeftContainer, "Mastery_Trails_Text", "TRILHAS: --", 11f, HUDTheme.TextCream, TextAlignmentOptions.Left, FontStyles.Normal);
            masteryTrailsText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(64f, 10f), new Vector2(-74f, 24f));

            // --- TOP RIGHT: TIMER & LOCATION ---
            RectTransform topRightContainer = HUDTheme.CreatePanel(transform, "TopRight_Info_Container",
                HUDTheme.RoundedRect(10, HUDTheme.PanelFill, 2, HUDTheme.PanelBorder), HUDTheme.PanelFill);
            topRightContainer.SetRect(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(220f, 65f));

            timerText = HUDTheme.CreateText(topRightContainer, "Timer_Text", "00:00", 14f, HUDTheme.TextCream, TextAlignmentOptions.Right, FontStyles.Bold);
            timerText.rectTransform.SetRect(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-15f, -10f), new Vector2(-30f, 22f));

            regionText = HUDTheme.CreateText(topRightContainer, "Region_Text", "Rota da Floresta", 12f, HUDTheme.GoldDim, TextAlignmentOptions.Right, FontStyles.Normal);
            regionText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-15f, 10f), new Vector2(-30f, 24f));
        }

        private void Update()
        {
            if (runManager == null)
            {
                runManager = FindFirstObjectByType<RunManager>();
            }

            // Update Class Icon & Player Info
            ClassType currentClass = ClassType.Knight;
            int level = 1;

            if (ProgressionManager.Instance != null)
            {
                currentClass = ProgressionManager.Instance.CurrentClass;
                level = ProgressionManager.Instance.CurrentLevel;
            }

            if (classIconImage != null)
            {
                switch (currentClass)
                {
                    case ClassType.Mage:
                        classIconImage.sprite = HUDTheme.IconWand(HUDTheme.RareBlue);
                        classIconImage.color = HUDTheme.RareBlue;
                        break;
                    case ClassType.Druid:
                        classIconImage.sprite = HUDTheme.IconLeaf(HUDTheme.StaminaGreen);
                        classIconImage.color = HUDTheme.StaminaGreen;
                        break;
                    case ClassType.Knight:
                    default:
                        classIconImage.sprite = HUDTheme.IconShield(HUDTheme.GoldAccent);
                        classIconImage.color = HUDTheme.GoldAccent;
                        break;
                }
            }

            string classNameStr = currentClass.ToString().ToUpper();
            if (playerHeaderText != null)
            {
                playerHeaderText.text = $"NÍVEL {level}  •  {classNameStr}";
            }

            // Update Mastery Status
            if (masteryTrailsText != null)
            {
                if (ProgressionManager.Instance != null && currentClass != ClassType.None)
                {
                    ClassDefinition def = ProgressionManager.Instance.GetActiveClassDefinition();
                    string p1 = def != null ? def.GetPathAbbrev(MasteryPath.Path1) : "P1";
                    string p2 = def != null ? def.GetPathAbbrev(MasteryPath.Path2) : "P2";
                    string p3 = def != null ? def.GetPathAbbrev(MasteryPath.Path3) : "P3";

                    string t1 = GetRomanTier(ProgressionManager.Instance.GetTier(MasteryPath.Path1));
                    string t2 = GetRomanTier(ProgressionManager.Instance.GetTier(MasteryPath.Path2));
                    string t3 = GetRomanTier(ProgressionManager.Instance.GetTier(MasteryPath.Path3));

                    masteryTrailsText.text = $"{p1} {t1}  |  {p2} {t2}  |  {p3} {t3}";
                }
                else
                {
                    masteryTrailsText.text = "ESCOLHA UMA ARMA";
                }
            }

            // Update Timer & Region
            float runSeconds = runManager != null ? runManager.RunTimeSeconds : Time.timeSinceLevelLoad;
            int mins = (int)(runSeconds / 60f);
            int secs = (int)(runSeconds % 60f);
            if (timerText != null)
            {
                timerText.text = $"{mins:D2}:{secs:D2}";
            }

            string region = Environment.BiomeRegionTrigger.CurrentRegionName;
            if (string.IsNullOrEmpty(region)) region = "Rota da Floresta";
            if (regionText != null)
            {
                regionText.text = $"{region}";
            }
        }

        private string GetRomanTier(MasteryTier tier)
        {
            switch (tier)
            {
                case MasteryTier.N1: return "I";
                case MasteryTier.N2: return "II";
                case MasteryTier.N3: return "III";
                default: return "0";
            }
        }
    }
}
