using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Roguelite.Items;
using Roguelite.Inventory;
using Roguelite.Player;
using Roguelite.Progression;
using Roguelite.UI.Theme;
using Roguelite.UI.Widgets;
using Roguelite.Core.StateMachine;

namespace Roguelite.UI
{
    /// <summary>
    /// Unified Dual-Page Ancient RPG Book UI Controller ("Grimório de Inventário & Maestria").
    /// Starts hidden and completely deactivated (alpha = 0, SetActive(false)).
    /// Opened with [E], closed with [E] or [ESC].
    /// Layout:
    ///   - Vertical bookmark page markers on the RIGHT side: [Inventário], [Maestria].
    ///   - Small gear button at the TOP-RIGHT corner: [Configurações ⚙].
    /// </summary>
    public class InventoryBookUI : MonoBehaviour
    {
        public static InventoryBookUI Instance { get; private set; }

        [Header("UI Canvas & Hierarchy")]
        private Canvas rootCanvas;
        private CanvasGroup canvasGroup;
        private GameObject backdropObj;
        private GameObject mainBookContainer;
        private RectTransform leftPageRect;
        private RectTransform rightPageRect;

        // Navigation Tabs & Buttons
        private GameObject rightTabInventario;
        private GameObject rightTabMaestria;
        private GameObject topRightSettingsBtn;

        // Tab Content Panels (Dual Page)
        private GameObject inventoryLeftPanel;
        private GameObject inventoryRightPanel;
        private GameObject masteryLeftPanel;
        private GameObject masteryRightPanel;
        private GameObject settingsLeftPanel;
        private GameObject settingsRightPanel;

        // Inventory Elements
        private readonly Dictionary<EquipmentSlot, BookSlotView> equipSlotViews = new Dictionary<EquipmentSlot, BookSlotView>();
        private readonly List<BookSlotView> gridSlotViews = new List<BookSlotView>();

        // Character Stats Elements
        private TextMeshProUGUI statLevelText;
        private TextMeshProUGUI statHpText;
        private TextMeshProUGUI statDamageText;
        private TextMeshProUGUI statStaminaText;
        private TextMeshProUGUI statSpeedText;
        private TextMeshProUGUI goldText;

        // Mastery Elements
        private TextMeshProUGUI masteryClassInfoText;
        private Image masteryXpBarFill;
        private TextMeshProUGUI masteryXpText;
        private TextMeshProUGUI masteryPointsText;
        private TextMeshProUGUI masteryUnlocksSummaryText;
        private readonly List<GameObject> masteryNodeCards = new List<GameObject>();
        private Transform masteryNodeContainer;

        // Tooltip Elements
        private GameObject tooltipContainer;
        private RectTransform tooltipRect;
        private TextMeshProUGUI tooltipTitleText;
        private TextMeshProUGUI tooltipMetaText;
        private TextMeshProUGUI tooltipDescText;
        private TextMeshProUGUI tooltipBonusText;

        // Drag Ghost
        private GameObject dragGhostObj;
        private RectTransform dragGhostRect;
        private Image dragGhostIcon;
        private TextMeshProUGUI dragGhostGlyph;
        private BookSlotView draggedSlot;

        // Icons Cache
        private Sprite iconWeapon, iconAmulet, iconBelt, iconRing, iconBackpack;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildProceduralSprites();
            ConstructBookHierarchy();

            // CRITICAL FIX: Ensure Inventory Book STARTS CLOSED & DEACTIVATED on startup
            SetVisible(false);
        }

        private void Start()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += RefreshUI;
                InventoryManager.Instance.OnGoldChanged += HandleGoldChanged;
            }

            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
            }

            // Enforce closed state on Start
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
                InventoryManager.Instance.OnGoldChanged -= HandleGoldChanged;
            }

            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
            }
        }

        private CharacterWindowUI.WindowTab activeTabState = (CharacterWindowUI.WindowTab)(-1);

        private void Update()
        {
            // Synchronize visibility with CharacterWindowUI state
            bool shouldBeOpen = false;
            CharacterWindowUI.WindowTab currentTab = CharacterWindowUI.WindowTab.Inventory;

            if (CharacterWindowUI.Instance != null)
            {
                shouldBeOpen = CharacterWindowUI.Instance.isOpen;
                currentTab = CharacterWindowUI.Instance.currentTab;
            }

            if (canvasGroup != null)
            {
                bool isVisibleNow = canvasGroup.alpha > 0.5f && (mainBookContainer != null && mainBookContainer.activeSelf);
                if (shouldBeOpen != isVisibleNow)
                {
                    SetVisible(shouldBeOpen);
                    if (shouldBeOpen)
                    {
                        activeTabState = currentTab;
                        UpdateActiveTabPanels(currentTab);
                        RefreshUI();
                    }
                    else
                    {
                        activeTabState = (CharacterWindowUI.WindowTab)(-1);
                    }
                }
            }

            if (canvasGroup != null && canvasGroup.alpha > 0.5f && mainBookContainer != null && mainBookContainer.activeSelf)
            {
                if (activeTabState != currentTab)
                {
                    activeTabState = currentTab;
                    UpdateActiveTabPanels(currentTab);
                    RefreshUI();
                }
                UpdateRealtimeStats();
            }
        }

        public void SetVisible(bool visible)
        {
            if (backdropObj != null) backdropObj.SetActive(visible);
            if (mainBookContainer != null) mainBookContainer.SetActive(visible);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (!visible)
            {
                HideTooltip();
                OnSlotEndDrag();
            }
        }

        private void BuildProceduralSprites()
        {
            iconWeapon = BookTheme.IconWeapon();
            iconAmulet = BookTheme.IconAmulet();
            iconBelt   = BookTheme.IconBelt();
            iconRing   = BookTheme.IconRing();
            iconBackpack = BookTheme.IconBackpack();
        }

        // =========================================================================
        // UGUI HIERARCHY GENERATION
        // =========================================================================
        private void ConstructBookHierarchy()
        {
            rootCanvas = gameObject.GetComponent<Canvas>();
            if (rootCanvas == null) rootCanvas = gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 100;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 1. Backdrop Overlay
            backdropObj = CreateRect("Backdrop", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = backdropObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.03f, 0.03f, 0.85f);

            // 2. Main Book Container (Centered 1100x680)
            mainBookContainer = CreateRect("BookContainer", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 680));
            Image bookCoverImg = mainBookContainer.AddComponent<Image>();
            bookCoverImg.sprite = BookTheme.CreateBookBackground(1100, 680);
            bookCoverImg.type = Image.Type.Simple;

            Outline bookFrameOutline = mainBookContainer.AddComponent<Outline>();
            bookFrameOutline.effectColor = new Color(0.12f, 0.08f, 0.04f, 0.9f);
            bookFrameOutline.effectDistance = new Vector2(4, -4);

            // 3. Right-Side Bookmark Tabs (Inventário & Maestria)
            BuildRightSideBookmarkTabs(mainBookContainer.transform);

            // 4. Top-Right Gear Button (Configurações ⚙)
            BuildTopRightSettingsButton(mainBookContainer.transform);

            // 5. Close Button [X] (Top-Right)
            GameObject closeObj = CreateRect("CloseBtn", mainBookContainer.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(530, 320), new Vector2(36, 36));
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.65f, 0.15f, 0.12f);
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => CharacterWindowUI.Instance?.CloseWindow());

            GameObject xTextObj = CreateRect("XLabel", closeObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI xTxt = xTextObj.AddComponent<TextMeshProUGUI>();
            xTxt.text = "X";
            xTxt.fontSize = 18;
            xTxt.fontStyle = FontStyles.Bold;
            xTxt.color = Color.white;
            xTxt.alignment = TextAlignmentOptions.Center;

            // 6. Left & Right Parchment Pages
            leftPageRect = CreateRect("LeftPage", mainBookContainer.transform, new Vector2(0.02f, 0.03f), new Vector2(0.485f, 0.95f), Vector2.zero, Vector2.zero).GetComponent<RectTransform>();
            Image leftPaper = leftPageRect.gameObject.AddComponent<Image>();
            leftPaper.sprite = BookTheme.CreateParchmentPage(640, 720, 7);

            rightPageRect = CreateRect("RightPage", mainBookContainer.transform, new Vector2(0.515f, 0.03f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero).GetComponent<RectTransform>();
            Image rightPaper = rightPageRect.gameObject.AddComponent<Image>();
            rightPaper.sprite = BookTheme.CreateParchmentPage(640, 720, 19);

            // Build Tab Content Panels
            BuildInventoryPanels();
            BuildMasteryPanels();
            BuildSettingsPanels();

            // Tooltip & Drag Ghost
            BuildTooltip(transform);
            BuildDragGhost(transform);
        }

        // =========================================================================
        // RIGHT-SIDE VERTICAL BOOKMARK TABS
        // =========================================================================
        private void BuildRightSideBookmarkTabs(Transform parent)
        {
            // Inventário Tab (Upper-Right side tab)
            rightTabInventario = CreateRect("SideTab_Inventario", parent, new Vector2(1.0f, 0.5f), new Vector2(1.0f, 0.5f), new Vector2(22, 120), new Vector2(46, 120));
            Image invImg = rightTabInventario.AddComponent<Image>();
            invImg.sprite = BookTheme.CreateRightSideTabSprite(true, 46, 120);

            Button invBtn = rightTabInventario.AddComponent<Button>();
            invBtn.onClick.AddListener(() => CharacterWindowUI.Instance?.SwitchTab(CharacterWindowUI.WindowTab.Inventory));

            GameObject invTxtObj = CreateRect("Text", rightTabInventario.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI invTxt = invTxtObj.AddComponent<TextMeshProUGUI>();
            invTxt.text = "I\nN\nV\nE\nN\nT\nÁ\nR\nI\nO";
            invTxt.fontSize = 11;
            invTxt.fontStyle = FontStyles.Bold;
            invTxt.color = new Color(0.28f, 0.18f, 0.10f);
            invTxt.alignment = TextAlignmentOptions.Center;
            invTxt.lineSpacing = -20f;

            // Maestria Tab (Lower-Right side tab)
            rightTabMaestria = CreateRect("SideTab_Maestria", parent, new Vector2(1.0f, 0.5f), new Vector2(1.0f, 0.5f), new Vector2(22, 0), new Vector2(46, 120));
            Image maeImg = rightTabMaestria.AddComponent<Image>();
            maeImg.sprite = BookTheme.CreateRightSideTabSprite(false, 46, 120);

            Button maeBtn = rightTabMaestria.AddComponent<Button>();
            maeBtn.onClick.AddListener(() => CharacterWindowUI.Instance?.SwitchTab(CharacterWindowUI.WindowTab.Mastery));

            GameObject maeTxtObj = CreateRect("Text", rightTabMaestria.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI maeTxt = maeTxtObj.AddComponent<TextMeshProUGUI>();
            maeTxt.text = "M\nA\nE\nS\nT\nR\nI\nA";
            maeTxt.fontSize = 11;
            maeTxt.fontStyle = FontStyles.Bold;
            maeTxt.color = new Color(0.85f, 0.75f, 0.55f);
            maeTxt.alignment = TextAlignmentOptions.Center;
            maeTxt.lineSpacing = -20f;
        }

        // =========================================================================
        // TOP-RIGHT GEAR SETTINGS BUTTON (Configurações)
        // =========================================================================
        private void BuildTopRightSettingsButton(Transform parent)
        {
            topRightSettingsBtn = CreateRect("TopRightSettingsBtn", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420, 320), new Vector2(150, 34));

            Image btnImg = topRightSettingsBtn.AddComponent<Image>();
            btnImg.sprite = BookTheme.CreateGearButtonSprite(false, 150, 34);

            Button btn = topRightSettingsBtn.AddComponent<Button>();
            btn.onClick.AddListener(() => CharacterWindowUI.Instance?.SwitchTab(CharacterWindowUI.WindowTab.Settings));

            GameObject txtObj = CreateRect("Text", topRightSettingsBtn.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = "CONFIGURAÇÕES";
            txt.fontSize = 13;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.92f, 0.82f, 0.65f);
            txt.alignment = TextAlignmentOptions.Center;
        }

        private void UpdateActiveTabPanels(CharacterWindowUI.WindowTab currentTab)
        {
            if (inventoryLeftPanel != null) inventoryLeftPanel.SetActive(currentTab == CharacterWindowUI.WindowTab.Inventory);
            if (inventoryRightPanel != null) inventoryRightPanel.SetActive(currentTab == CharacterWindowUI.WindowTab.Inventory);

            if (masteryLeftPanel != null) masteryLeftPanel.SetActive(currentTab == CharacterWindowUI.WindowTab.Mastery);
            if (masteryRightPanel != null) masteryRightPanel.SetActive(currentTab == CharacterWindowUI.WindowTab.Mastery);

            if (settingsLeftPanel != null) settingsLeftPanel.SetActive(currentTab == CharacterWindowUI.WindowTab.Settings);
            if (settingsRightPanel != null) settingsRightPanel.SetActive(currentTab == CharacterWindowUI.WindowTab.Settings);

            // Update Right-Side Bookmark Tabs
            bool isInv = (currentTab == CharacterWindowUI.WindowTab.Inventory);
            bool isMae = (currentTab == CharacterWindowUI.WindowTab.Mastery);

            if (rightTabInventario != null)
            {
                rightTabInventario.GetComponent<Image>().sprite = BookTheme.CreateRightSideTabSprite(isInv, 46, 120);
                rightTabInventario.transform.SetAsLastSibling(); // Move active to top if active
                TextMeshProUGUI txt = rightTabInventario.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.color = isInv ? new Color(0.28f, 0.18f, 0.10f) : new Color(0.85f, 0.75f, 0.55f);
            }

            if (rightTabMaestria != null)
            {
                rightTabMaestria.GetComponent<Image>().sprite = BookTheme.CreateRightSideTabSprite(isMae, 46, 120);
                if (isMae) rightTabMaestria.transform.SetAsLastSibling();
                TextMeshProUGUI txt = rightTabMaestria.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.color = isMae ? new Color(0.28f, 0.18f, 0.10f) : new Color(0.85f, 0.75f, 0.55f);
            }

            // Update Top-Right Gear Button
            bool isSet = (currentTab == CharacterWindowUI.WindowTab.Settings);
            if (topRightSettingsBtn != null)
            {
                topRightSettingsBtn.GetComponent<Image>().sprite = BookTheme.CreateGearButtonSprite(isSet, 150, 34);
                TextMeshProUGUI txt = topRightSettingsBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.color = isSet ? new Color(0.98f, 0.90f, 0.35f) : new Color(0.92f, 0.82f, 0.65f);
            }
        }

        // =========================================================================
        // INVENTORY PANELS (Dual Page)
        // =========================================================================
        private void BuildInventoryPanels()
        {
            inventoryLeftPanel = CreateRect("InvLeftPanel", leftPageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            inventoryRightPanel = CreateRect("InvRightPanel", rightPageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Left Page: Equipment & Stats
            GameObject titleObj = CreateRect("LeftTitle", inventoryLeftPanel.transform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "EQUIPAMENTO DA JORNADA";
            titleText.fontSize = 19;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.28f, 0.18f, 0.10f);
            titleText.alignment = TextAlignmentOptions.Center;

            GameObject silhObj = CreateRect("Silhouette", inventoryLeftPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 75), new Vector2(180, 240));
            Image silhImg = silhObj.AddComponent<Image>();
            silhImg.sprite = BookTheme.CharacterSilhouette(180, 240);

            equipSlotViews[EquipmentSlot.Weapon] = CreateEquipSlot(inventoryLeftPanel.transform, EquipmentSlot.Weapon, "ARMA PRINCIPAL", new Vector2(0, 210), iconWeapon);
            equipSlotViews[EquipmentSlot.Amulet] = CreateEquipSlot(inventoryLeftPanel.transform, EquipmentSlot.Amulet, "AMULETO", new Vector2(-150, 90), iconAmulet);
            equipSlotViews[EquipmentSlot.Ring1]  = CreateEquipSlot(inventoryLeftPanel.transform, EquipmentSlot.Ring1,  "ANEL 1", new Vector2(150, 90), iconRing);
            equipSlotViews[EquipmentSlot.Belt]   = CreateEquipSlot(inventoryLeftPanel.transform, EquipmentSlot.Belt,   "CINTURÃO", new Vector2(0, -60), iconBelt);
            equipSlotViews[EquipmentSlot.Ring2]  = CreateEquipSlot(inventoryLeftPanel.transform, EquipmentSlot.Ring2,  "ANEL 2", new Vector2(0, -160), iconRing);

            GameObject statsBox = CreateRect("StatsBox", inventoryLeftPanel.transform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.20f), Vector2.zero, Vector2.zero);
            Image statsBg = statsBox.AddComponent<Image>();
            statsBg.color = new Color(0.18f, 0.14f, 0.10f, 0.20f);
            statsBox.AddComponent<Outline>().effectColor = new Color(0.40f, 0.30f, 0.20f, 0.5f);

            statLevelText  = CreateStatLabel(statsBox.transform, "Nível / Classe:", "Nív 1 • Heroi", 85);
            statHpText     = CreateStatLabel(statsBox.transform, "HP Máximo:", "100 / 100", 62);
            statDamageText = CreateStatLabel(statsBox.transform, "Dano de Ataque:", "25.0", 41);
            statStaminaText= CreateStatLabel(statsBox.transform, "Stamina:", "100 / 100", 20);
            statSpeedText  = CreateStatLabel(statsBox.transform, "Velocidade:", "100%", 0);

            // Right Page: Inventory 25-Slot Grid
            GameObject rTitleObj = CreateRect("RightTitle", inventoryRightPanel.transform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI rTitleText = rTitleObj.AddComponent<TextMeshProUGUI>();
            rTitleText.text = "BOLSA DE ITENS (25 SLOTS)";
            rTitleText.fontSize = 19;
            rTitleText.fontStyle = FontStyles.Bold;
            rTitleText.color = new Color(0.28f, 0.18f, 0.10f);
            rTitleText.alignment = TextAlignmentOptions.Center;

            GameObject gridObj = CreateRect("GridContainer", inventoryRightPanel.transform, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(72, 72);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.MiddleCenter;

            gridSlotViews.Clear();
            for (int i = 0; i < 25; i++)
            {
                gridSlotViews.Add(CreateGridSlotCell(gridObj.transform, i));
            }

            GameObject goldBox = CreateRect("GoldBox", inventoryRightPanel.transform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.12f), Vector2.zero, Vector2.zero);
            goldBox.AddComponent<Image>().color = new Color(0.22f, 0.18f, 0.10f, 0.18f);

            GameObject goldTextObj = CreateRect("GoldText", goldBox.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            goldText = goldTextObj.AddComponent<TextMeshProUGUI>();
            goldText.text = "OURO ACUMULADO: 0";
            goldText.fontSize = 15;
            goldText.fontStyle = FontStyles.Bold;
            goldText.color = new Color(0.70f, 0.52f, 0.08f);
            goldText.alignment = TextAlignmentOptions.Center;
        }

        // =========================================================================
        // MASTERY PANELS (In-Book Skill Tree & Progression)
        // =========================================================================
        private void BuildMasteryPanels()
        {
            masteryLeftPanel = CreateRect("MasteryLeftPanel", leftPageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            masteryRightPanel = CreateRect("MasteryRightPanel", rightPageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Left Page: Mastery & Progression Overview
            GameObject titleObj = CreateRect("Title", masteryLeftPanel.transform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "MAESTRIA & PROGRESSÃO";
            titleTxt.fontSize = 19;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color(0.28f, 0.18f, 0.10f);
            titleTxt.alignment = TextAlignmentOptions.Center;

            // Class Info Label
            GameObject classInfoObj = CreateRect("ClassInfo", masteryLeftPanel.transform, new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.89f), Vector2.zero, Vector2.zero);
            masteryClassInfoText = classInfoObj.AddComponent<TextMeshProUGUI>();
            masteryClassInfoText.fontSize = 14;
            masteryClassInfoText.fontStyle = FontStyles.Bold;
            masteryClassInfoText.color = new Color(0.35f, 0.25f, 0.15f);
            masteryClassInfoText.alignment = TextAlignmentOptions.Center;

            // XP Progress Bar Box
            GameObject xpBox = CreateRect("XPBox", masteryLeftPanel.transform, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.79f), Vector2.zero, Vector2.zero);
            xpBox.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.10f, 0.35f);
            xpBox.AddComponent<Outline>().effectColor = new Color(0.55f, 0.42f, 0.25f);

            GameObject xpFillObj = CreateRect("Fill", xpBox.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            masteryXpBarFill = xpFillObj.AddComponent<Image>();
            masteryXpBarFill.color = new Color(0.25f, 0.65f, 0.35f, 0.85f);
            masteryXpBarFill.type = Image.Type.Filled;
            masteryXpBarFill.fillMethod = Image.FillMethod.Horizontal;

            GameObject xpTextObj = CreateRect("XPText", xpBox.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            masteryXpText = xpTextObj.AddComponent<TextMeshProUGUI>();
            masteryXpText.fontSize = 12;
            masteryXpText.fontStyle = FontStyles.Bold;
            masteryXpText.color = Color.white;
            masteryXpText.alignment = TextAlignmentOptions.Center;

            // Available Points Label
            GameObject ptsObj = CreateRect("PtsText", masteryLeftPanel.transform, new Vector2(0.05f, 0.61f), new Vector2(0.95f, 0.68f), Vector2.zero, Vector2.zero);
            masteryPointsText = ptsObj.AddComponent<TextMeshProUGUI>();
            masteryPointsText.fontSize = 12;
            masteryPointsText.fontStyle = FontStyles.Bold;
            masteryPointsText.color = new Color(0.85f, 0.55f, 0.10f);
            masteryPointsText.alignment = TextAlignmentOptions.Left;

            // Unlocked Passives Box
            GameObject summaryBox = CreateRect("SummaryBox", masteryLeftPanel.transform, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.58f), Vector2.zero, Vector2.zero);
            summaryBox.AddComponent<Image>().color = new Color(0.22f, 0.16f, 0.10f, 0.15f);
            summaryBox.AddComponent<Outline>().effectColor = new Color(0.40f, 0.30f, 0.20f, 0.4f);

            GameObject summaryTxtObj = CreateRect("Text", summaryBox.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            masteryUnlocksSummaryText = summaryTxtObj.AddComponent<TextMeshProUGUI>();
            masteryUnlocksSummaryText.fontSize = 11;
            masteryUnlocksSummaryText.color = new Color(0.28f, 0.20f, 0.12f);
            masteryUnlocksSummaryText.alignment = TextAlignmentOptions.TopLeft;

            // Right Page: Upgrade Tree Nodes
            GameObject rTitleObj = CreateRect("RightTitle", masteryRightPanel.transform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI rTitleText = rTitleObj.AddComponent<TextMeshProUGUI>();
            rTitleText.text = "ÁRVORE DE TALENTOS & HABILIDADES";
            rTitleText.fontSize = 19;
            rTitleText.fontStyle = FontStyles.Bold;
            rTitleText.color = new Color(0.28f, 0.18f, 0.10f);
            rTitleText.alignment = TextAlignmentOptions.Center;

            GameObject nodeContainerObj = CreateRect("NodeContainer", masteryRightPanel.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            masteryNodeContainer = nodeContainerObj.transform;

            VerticalLayoutGroup vert = nodeContainerObj.AddComponent<VerticalLayoutGroup>();
            vert.spacing = 10;
            vert.childControlWidth = true;
            vert.childControlHeight = false;
        }

        private void RefreshMasteryUI()
        {
            if (ProgressionManager.Instance == null) return;

            ProgressionManager pm = ProgressionManager.Instance;
            ClassDefinition classDef = pm.GetActiveClassDefinition();

            string className = "Nenhuma (Escolha no Saguão)";
            if (classDef != null)
            {
                if (pm.ChosenSpecializationPath == MasteryPath.None)
                {
                    className = $"{classDef.baseClassName} (Sem Especialização)";
                }
                else
                {
                    string specName = classDef.GetPathName(pm.ChosenSpecializationPath);
                    MasteryTier tier = pm.GetTier(pm.ChosenSpecializationPath);
                    className = $"{classDef.baseClassName}  ▸  {specName} (N{(int)tier})";
                }
            }

            if (masteryClassInfoText != null) masteryClassInfoText.text = $"Classe: {className}  •  Nível {pm.CurrentLevel}";

            int reqXp = pm.GetXPRequired(pm.CurrentLevel);
            float xpRatio = Mathf.Clamp01((float)pm.CurrentLevelXP / Mathf.Max(1, reqXp));
            if (masteryXpBarFill != null) masteryXpBarFill.fillAmount = xpRatio;
            if (masteryXpText != null) masteryXpText.text = $"XP: {pm.CurrentLevelXP} / {reqXp}";

            if (masteryPointsText != null)
            {
                masteryPointsText.text = pm.PendingLevelUpCount > 0
                    ? $"PONTOS DISPONÍVEIS: {pm.PendingLevelUpCount}"
                    : "NENHUM PONTO PENDENTE";
            }

            // Build Mastery Node Cards on Right Page
            foreach (GameObject card in masteryNodeCards) Destroy(card);
            masteryNodeCards.Clear();

            if (classDef == null || classDef.upgrades == null || masteryNodeContainer == null) return;

            // 1. Root Base Class Card
            CreateBaseClassRootCard(masteryNodeContainer, classDef, pm);

            // 2. Specialization Node Cards
            List<ClassUpgradeDefinition> choices = pm.GetUpgradeChoices();

            foreach (var pathVal in System.Enum.GetValues(typeof(MasteryPath)))
            {
                MasteryPath p = (MasteryPath)pathVal;
                if (p == MasteryPath.None) continue;
                MasteryTier currentTier = pm.GetTier(p);

                // If specialization is not chosen yet: show Tier N1 of all 3 paths
                if (pm.ChosenSpecializationPath == MasteryPath.None)
                {
                    ClassUpgradeDefinition n1Upgrade = classDef.upgrades.Find(u => u.path == p && u.tier == MasteryTier.N1);
                    if (n1Upgrade != null)
                    {
                        bool isAvailable = choices.Contains(n1Upgrade) && pm.PendingLevelUpCount > 0;
                        CreateMasteryNodeCard(masteryNodeContainer, n1Upgrade, currentTier, isAvailable, isSpecializationChoice: true, isLockedByOtherSpec: false);
                    }
                }
                else if (p == pm.ChosenSpecializationPath)
                {
                    // Active chosen specialization path
                    MasteryTier nextTier = (MasteryTier)((int)currentTier + 1);
                    ClassUpgradeDefinition nextUpgrade = classDef.upgrades.Find(u => u.path == p && u.tier == nextTier);

                    if (nextUpgrade != null)
                    {
                        bool isAvailable = choices.Contains(nextUpgrade) && pm.PendingLevelUpCount > 0;
                        CreateMasteryNodeCard(masteryNodeContainer, nextUpgrade, currentTier, isAvailable, isSpecializationChoice: false, isLockedByOtherSpec: false);
                    }
                    else if (currentTier == MasteryTier.N3)
                    {
                        // Maxed path card
                        ClassUpgradeDefinition maxUpgrade = classDef.upgrades.Find(u => u.path == p && u.tier == MasteryTier.N3);
                        if (maxUpgrade != null)
                        {
                            CreateMasteryNodeCard(masteryNodeContainer, maxUpgrade, currentTier, canUnlock: false, isSpecializationChoice: false, isLockedByOtherSpec: false, isMaxed: true);
                        }
                    }
                }
                else
                {
                    // Locked alternative specialization path
                    ClassUpgradeDefinition n1Upgrade = classDef.upgrades.Find(u => u.path == p && u.tier == MasteryTier.N1);
                    if (n1Upgrade != null)
                    {
                        CreateMasteryNodeCard(masteryNodeContainer, n1Upgrade, currentTier, canUnlock: false, isSpecializationChoice: false, isLockedByOtherSpec: true);
                    }
                }
            }

            // Update Unlocks Summary
            if (masteryUnlocksSummaryText != null)
            {
                string specText = pm.ChosenSpecializationPath == MasteryPath.None
                    ? "Nenhuma (Escolha 1 na Árvore)"
                    : $"{classDef.GetPathName(pm.ChosenSpecializationPath)} (N{(int)pm.GetTier(pm.ChosenSpecializationPath)})";

                masteryUnlocksSummaryText.text = $"<b>Classe Base:</b> {classDef.baseClassName}\n" +
                                                 $"<b>Especialização:</b> {specText}\n\n" +
                                                 $"<b>Ataque Básico:</b> {pm.CurrentBasicAttack?.attackName ?? "Padrão"}\n" +
                                                 $"<b>Ataque Carregado:</b> {pm.CurrentChargedAttack?.attackName ?? "Padrão"}\n\n" +
                                                 $"<b>Status dos Caminhos:</b>\n" +
                                                 $"• {classDef.GetPathName(MasteryPath.Path1)}: {(pm.ChosenSpecializationPath == MasteryPath.Path1 ? $"N{(int)pm.GetTier(MasteryPath.Path1)}" : (pm.ChosenSpecializationPath == MasteryPath.None ? "Disponível" : "Bloqueado"))}\n" +
                                                 $"• {classDef.GetPathName(MasteryPath.Path2)}: {(pm.ChosenSpecializationPath == MasteryPath.Path2 ? $"N{(int)pm.GetTier(MasteryPath.Path2)}" : (pm.ChosenSpecializationPath == MasteryPath.None ? "Disponível" : "Bloqueado"))}\n" +
                                                 $"• {classDef.GetPathName(MasteryPath.Path3)}: {(pm.ChosenSpecializationPath == MasteryPath.Path3 ? $"N{(int)pm.GetTier(MasteryPath.Path3)}" : (pm.ChosenSpecializationPath == MasteryPath.None ? "Disponível" : "Bloqueado"))}";
            }
        }

        private void CreateBaseClassRootCard(Transform parent, ClassDefinition classDef, ProgressionManager pm)
        {
            GameObject cardObj = CreateRect("Root_BaseClass", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 55));
            masteryNodeCards.Add(cardObj);

            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 50f;
            le.preferredHeight = 55f;
            le.flexibleWidth = 1f;

            Image bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.28f, 0.20f, 0.12f, 0.25f);
            cardObj.AddComponent<Outline>().effectColor = new Color(0.65f, 0.50f, 0.25f, 0.6f);

            GameObject titleObj = CreateRect("Title", cardObj.transform, new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = $"<b>RAÍZ: {classDef.baseClassName.ToUpper()}</b>";
            titleTxt.fontSize = 12;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color(0.90f, 0.75f, 0.30f);

            GameObject descObj = CreateRect("Desc", cardObj.transform, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.48f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = pm.ChosenSpecializationPath == MasteryPath.None
                ? "Ativo. Escolha 1 especialização permanente abaixo para liberar habilidades."
                : $"Ativo. Especialização selecionada: <b>{classDef.GetPathName(pm.ChosenSpecializationPath)}</b>";
            descTxt.fontSize = 9;
            descTxt.color = new Color(0.35f, 0.28f, 0.20f);
        }

        private void CreateMasteryNodeCard(
            Transform parent,
            ClassUpgradeDefinition upgrade,
            MasteryTier currentTier,
            bool canUnlock,
            bool isSpecializationChoice = false,
            bool isLockedByOtherSpec = false,
            bool isMaxed = false)
        {
            GameObject cardObj = CreateRect($"Node_{upgrade.path}_{upgrade.tier}", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 95));
            masteryNodeCards.Add(cardObj);

            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 90f;
            le.preferredHeight = 95f;
            le.flexibleWidth = 1f;

            Image bg = cardObj.AddComponent<Image>();
            bg.sprite = BookTheme.CreateNodeFrame(false, canUnlock);
            bg.raycastTarget = false;

            if (isLockedByOtherSpec)
            {
                bg.color = new Color(0.12f, 0.10f, 0.08f, 0.4f);
            }

            // Title & Path Label
            GameObject titleObj = CreateRect("Title", cardObj.transform, new Vector2(0.04f, 0.65f), new Vector2(0.65f, 0.92f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.raycastTarget = false;
            titleTxt.text = upgrade.upgradeTitle;
            titleTxt.fontSize = 11;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = canUnlock
                ? new Color(0.95f, 0.85f, 0.25f)
                : (isLockedByOtherSpec ? new Color(0.35f, 0.30f, 0.25f, 0.6f) : new Color(0.40f, 0.30f, 0.20f));

            // Description
            GameObject descObj = CreateRect("Desc", cardObj.transform, new Vector2(0.04f, 0.08f), new Vector2(0.65f, 0.62f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.raycastTarget = false;
            descTxt.text = isLockedByOtherSpec ? "<i>Bloqueado (Outra Especialização Ativa)</i>" : upgrade.description;
            descTxt.fontSize = 9;
            descTxt.color = isLockedByOtherSpec ? new Color(0.40f, 0.35f, 0.30f, 0.5f) : new Color(0.30f, 0.22f, 0.15f);

            // Unlock Button / Status Badge
            GameObject btnObj = CreateRect("ActionBtn", cardObj.transform, new Vector2(0.67f, 0.20f), new Vector2(0.97f, 0.80f), Vector2.zero, Vector2.zero);
            Image btnBg = btnObj.AddComponent<Image>();

            if (isMaxed) btnBg.color = new Color(0.70f, 0.55f, 0.15f);
            else if (canUnlock && isSpecializationChoice) btnBg.color = new Color(0.15f, 0.55f, 0.75f);
            else if (canUnlock) btnBg.color = new Color(0.20f, 0.60f, 0.25f);
            else btnBg.color = new Color(0.22f, 0.18f, 0.14f, 0.6f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.interactable = canUnlock;
            btn.onClick.AddListener(() =>
            {
                // Debug.Log("[SPECIALIZATION] Button clicked!");
                // Debug.Log($"[SPECIALIZATION] Selected Node: {upgrade.upgradeTitle} (Path: {upgrade.path}, Tier: {upgrade.tier})");
                // Debug.Log($"[SPECIALIZATION] Current Class: {ProgressionManager.Instance?.CurrentClass}");
                // Debug.Log($"[SPECIALIZATION] Before ChosenSpecializationPath: {ProgressionManager.Instance?.ChosenSpecializationPath}");
                // Debug.Log($"[SPECIALIZATION] PendingLevelUpCount: {ProgressionManager.Instance?.PendingLevelUpCount}");

                if (ProgressionManager.Instance != null)
                {
                    bool remainingPoints = ProgressionManager.Instance.SelectUpgrade(upgrade);
                    // Debug.Log($"[SPECIALIZATION] After ChosenSpecializationPath: {ProgressionManager.Instance.ChosenSpecializationPath}");
                    // Debug.Log($"[SPECIALIZATION] Remaining Points: {ProgressionManager.Instance.PendingLevelUpCount}");
                    RefreshMasteryUI();
                    RefreshUI();
                }
            });

            GameObject lblObj = CreateRect("Label", btnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI lblTxt = lblObj.AddComponent<TextMeshProUGUI>();
            lblTxt.raycastTarget = false;

            if (isMaxed) lblTxt.text = "MAXIMIZADO";
            else if (canUnlock && isSpecializationChoice) lblTxt.text = "ESPECIALIZAR";
            else if (canUnlock) lblTxt.text = "APRENDER";
            else if (isLockedByOtherSpec) lblTxt.text = "BLOQUEADO";
            else lblTxt.text = "REQUER PONTO";

            lblTxt.fontSize = 10;
            lblTxt.fontStyle = FontStyles.Bold;
            lblTxt.color = Color.white;
            lblTxt.alignment = TextAlignmentOptions.Center;
        }

        private TextMeshProUGUI cameraBobBtnText;

        private void BuildSettingsPanels()
        {
            settingsLeftPanel = CreateRect("SettingsLeftPanel", leftPageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            settingsRightPanel = CreateRect("SettingsRightPanel", rightPageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Left Page: Camera & Gameplay Settings
            GameObject titleObj = CreateRect("Title", settingsLeftPanel.transform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "CONFIGURAÇÕES DO JOGO";
            titleText.fontSize = 19;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.28f, 0.18f, 0.10f);
            titleText.alignment = TextAlignmentOptions.Center;

            // Camera Bobbing Box Container
            GameObject bobBox = CreateRect("BobbingBox", settingsLeftPanel.transform, new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);
            bobBox.AddComponent<Image>().color = new Color(0.22f, 0.16f, 0.10f, 0.15f);
            bobBox.AddComponent<Outline>().effectColor = new Color(0.40f, 0.30f, 0.20f, 0.4f);

            GameObject bobTitleObj = CreateRect("BobTitle", bobBox.transform, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI bobTitleTxt = bobTitleObj.AddComponent<TextMeshProUGUI>();
            bobTitleTxt.text = "MOVIMENTO DA CÂMERA";
            bobTitleTxt.fontSize = 13;
            bobTitleTxt.fontStyle = FontStyles.Bold;
            bobTitleTxt.color = new Color(0.30f, 0.20f, 0.12f);

            GameObject bobDescObj = CreateRect("BobDesc", bobBox.transform, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.62f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI bobDescTxt = bobDescObj.AddComponent<TextMeshProUGUI>();
            bobDescTxt.text = "Balanço natural da cabeça ao andar, correr e cavalgar.";
            bobDescTxt.fontSize = 10;
            bobDescTxt.color = new Color(0.40f, 0.30f, 0.20f);

            // Cycle Button
            GameObject btnObj = CreateRect("CycleBtn", bobBox.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.33f), Vector2.zero, Vector2.zero);
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.35f, 0.25f, 0.15f);

            Button cycleBtn = btnObj.AddComponent<Button>();
            cycleBtn.targetGraphic = btnBg;
            cycleBtn.onClick.AddListener(CycleCameraBobbingSetting);

            GameObject btnLblObj = CreateRect("Label", btnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cameraBobBtnText = btnLblObj.AddComponent<TextMeshProUGUI>();
            cameraBobBtnText.fontSize = 11;
            cameraBobBtnText.fontStyle = FontStyles.Bold;
            cameraBobBtnText.color = Color.white;
            cameraBobBtnText.alignment = TextAlignmentOptions.Center;

            RefreshCameraBobbingButtonUI();

            // Right Page: Keybinds Reference Guide
            GameObject rTitleObj = CreateRect("RightTitle", settingsRightPanel.transform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI rTitleText = rTitleObj.AddComponent<TextMeshProUGUI>();
            rTitleText.text = "GUIA DE CONTROLES";
            rTitleText.fontSize = 19;
            rTitleText.fontStyle = FontStyles.Bold;
            rTitleText.color = new Color(0.28f, 0.18f, 0.10f);
            rTitleText.alignment = TextAlignmentOptions.Center;

            GameObject keyBox = CreateRect("KeyBox", settingsRightPanel.transform, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
            keyBox.AddComponent<Image>().color = new Color(0.22f, 0.16f, 0.10f, 0.15f);
            keyBox.AddComponent<Outline>().effectColor = new Color(0.40f, 0.30f, 0.20f, 0.4f);

            GameObject keyTextObj = CreateRect("Text", keyBox.transform, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI keyTxt = keyTextObj.AddComponent<TextMeshProUGUI>();
            keyTxt.fontSize = 12;
            keyTxt.color = new Color(0.28f, 0.20f, 0.12f);
            keyTxt.lineSpacing = 6f;
            keyTxt.text =
                "<b>[WASD]</b> — Movimentação\n" +
                "<b>[Mouse]</b> — Orbitar Câmera & Mirar\n" +
                "<b>[Shift]</b> — Correr (Consome Stamina)\n" +
                "<b>[Espaço]</b> — Esquivar / Pular\n" +
                "<b>[Botão Esq.]</b> — Ataque Principal\n" +
                "<b>[Botão Dir.]</b> — Habilidade Secundaria\n" +
                "<b>[E]</b> — Abrir / Fechar Grimório\n" +
                "<b>[Tab]</b> — Alternar Abas\n" +
                "<b>[F]</b> — Interagir / Montar Cavalo";
        }

        private void CycleCameraBobbingSetting()
        {
            CameraBobbing bobbing = FindFirstObjectByType<CameraBobbing>();
            float current = bobbing != null ? bobbing.IntensityMultiplier : 1.0f;

            float next;
            if (current < 0.2f) next = 0.5f;       // Off -> Weak
            else if (current < 0.7f) next = 1.0f;  // Weak -> Normal
            else if (current < 1.2f) next = 1.5f;  // Normal -> Strong
            else next = 0.0f;                      // Strong -> Off

            if (bobbing != null)
            {
                bobbing.IntensityMultiplier = next;
            }

            Core.Save.SaveManager.Instance?.SaveAll();
            RefreshCameraBobbingButtonUI();
        }

        private void RefreshCameraBobbingButtonUI()
        {
            if (cameraBobBtnText == null) return;

            CameraBobbing bobbing = FindFirstObjectByType<CameraBobbing>();
            float val = bobbing != null ? bobbing.IntensityMultiplier : 1.0f;

            if (val < 0.2f) cameraBobBtnText.text = "DESLIGADO (0%)";
            else if (val < 0.7f) cameraBobBtnText.text = "FRACO (50%)";
            else if (val < 1.2f) cameraBobBtnText.text = "NORMAL (100%)";
            else cameraBobBtnText.text = "FORTE (150%)";
        }

        // =========================================================================
        // UI HELPERS & REFRESH LOGIC
        // =========================================================================
        public void RefreshUI()
        {
            RefreshEquipSlots();
            RefreshInventoryGrid();
            RefreshGoldDisplay();
            UpdateRealtimeStats();
            RefreshMasteryUI();
            RefreshCameraBobbingButtonUI();
        }

        private void RefreshEquipSlots()
        {
            if (EquipmentManager.Instance == null) return;
            foreach (var kvp in equipSlotViews)
            {
                ApplyItemVisual(kvp.Value, EquipmentManager.Instance.GetEquipped(kvp.Key));
            }
        }

        private void RefreshInventoryGrid()
        {
            if (InventoryManager.Instance == null) return;
            IReadOnlyList<InventorySlot> items = InventoryManager.Instance.Items;

            for (int i = 0; i < gridSlotViews.Count; i++)
            {
                BookSlotView view = gridSlotViews[i];
                InventorySlot slot = (items != null && i < items.Count) ? items[i] : null;
                ApplyItemVisual(view, slot?.item, slot?.quantity ?? 0);
            }
        }

        private void RefreshGoldDisplay()
        {
            int gold = InventoryManager.Instance != null ? InventoryManager.Instance.Gold : 0;
            if (goldText != null) goldText.text = $"OURO ACUMULADO: {gold}";
        }

        private void ApplyItemVisual(BookSlotView view, ItemData item, int quantity = 1)
        {
            if (view == null) return;

            if (item != null)
            {
                if (item.customIcon != null)
                {
                    view.iconImage.sprite = item.customIcon;
                    view.iconImage.color = Color.white;
                    view.glyphText.text = "";
                }
                else
                {
                    Sprite categorySprite = GetProceduralIcon(item.category);
                    if (categorySprite != null)
                    {
                        view.iconImage.sprite = categorySprite;
                        view.iconImage.color = item.RarityColor;
                        view.glyphText.text = "";
                    }
                    else
                    {
                        view.iconImage.color = Color.clear;
                        view.glyphText.text = item.iconGlyph;
                    }
                }

                if (view.borderFrame != null) view.borderFrame.sprite = BookTheme.SquareFantasyFrame(item.RarityColor);
                if (view.quantityText != null) view.quantityText.text = quantity > 1 ? $"x{quantity}" : "";
                if (view.slotCategoryWatermark != null) view.slotCategoryWatermark.gameObject.SetActive(false);
            }
            else
            {
                view.iconImage.color = Color.clear;
                view.glyphText.text = "";
                if (view.quantityText != null) view.quantityText.text = "";
                if (view.borderFrame != null) view.borderFrame.sprite = BookTheme.SquareFantasyFrame(Color.clear);
                if (view.slotCategoryWatermark != null) view.slotCategoryWatermark.gameObject.SetActive(true);
            }
        }

        private Sprite GetProceduralIcon(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon: return iconWeapon;
                case ItemCategory.Amulet: return iconAmulet;
                case ItemCategory.Belt:   return iconBelt;
                case ItemCategory.Ring:   return iconRing;
                default:                  return null;
            }
        }

        private void UpdateRealtimeStats()
        {
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats == null) return;

            string className = (stats.CharacterData != null && !string.IsNullOrEmpty(stats.CharacterData.characterName))
                ? stats.CharacterData.characterName
                : "Guerreiro";

            if (statLevelText != null) statLevelText.text = $"Nív {stats.Level} • {className}";
            if (statHpText != null) statHpText.text = $"{stats.CurrentHP:F0} / {stats.MaxHP:F0}";
            if (statDamageText != null) statDamageText.text = $"{stats.FlatDamage:F1}";
            if (statStaminaText != null) statStaminaText.text = $"{stats.CurrentStamina:F0} / {stats.MaxStamina:F0}";
            if (statSpeedText != null) statSpeedText.text = $"{stats.MoveSpeedMultiplier * 100f:F0}%";
        }

        private BookSlotView CreateEquipSlot(Transform parent, EquipmentSlot slot, string labelText, Vector2 localPos, Sprite categoryIcon)
        {
            GameObject slotObj = CreateRect($"EquipSlot_{slot}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), localPos, new Vector2(68, 68));

            Image frameImg = slotObj.AddComponent<Image>();
            frameImg.sprite = BookTheme.CreateEquipmentFrame(Color.clear);

            GameObject wmObj = CreateRect("Watermark", slotObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image wmImg = wmObj.AddComponent<Image>();
            wmImg.sprite = categoryIcon;
            wmImg.color = new Color(0.40f, 0.30f, 0.20f, 0.40f);

            GameObject iconObj = CreateRect("Icon", slotObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.clear;

            GameObject glyphObj = CreateRect("Glyph", slotObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI glyphTxt = glyphObj.AddComponent<TextMeshProUGUI>();
            glyphTxt.fontSize = 24;
            glyphTxt.alignment = TextAlignmentOptions.Center;

            GameObject lblObj = CreateRect("Label", slotObj.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, -14), new Vector2(100, 18));
            TextMeshProUGUI labelTxt = lblObj.AddComponent<TextMeshProUGUI>();
            labelTxt.text = labelText;
            labelTxt.fontSize = 10;
            labelTxt.fontStyle = FontStyles.Bold;
            labelTxt.color = new Color(0.35f, 0.25f, 0.15f);
            labelTxt.alignment = TextAlignmentOptions.Center;

            BookSlotView view = slotObj.AddComponent<BookSlotView>();
            view.isEquipSlot = true;
            view.equipSlot = slot;
            view.gridIndex = -1;
            view.bgFrame = frameImg;
            view.borderFrame = frameImg;
            view.iconImage = iconImg;
            view.glyphText = glyphTxt;
            view.slotCategoryWatermark = wmImg;
            view.SetOwner(this);

            return view;
        }

        private BookSlotView CreateGridSlotCell(Transform parent, int index)
        {
            GameObject cellObj = CreateRect($"Slot_{index}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72));

            Image bgFrame = cellObj.AddComponent<Image>();
            bgFrame.sprite = BookTheme.CreateSlotBackground();

            GameObject borderObj = CreateRect("Border", cellObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = BookTheme.CreateEquipmentFrame(Color.clear);

            GameObject iconObj = CreateRect("Icon", cellObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.clear;

            GameObject glyphObj = CreateRect("Glyph", cellObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI glyphTxt = glyphObj.AddComponent<TextMeshProUGUI>();
            glyphTxt.fontSize = 26;
            glyphTxt.alignment = TextAlignmentOptions.Center;

            GameObject qtyObj = CreateRect("Qty", cellObj.transform, new Vector2(0.4f, 0.05f), new Vector2(0.95f, 0.40f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI qtyTxt = qtyObj.AddComponent<TextMeshProUGUI>();
            qtyTxt.fontSize = 12;
            qtyTxt.fontStyle = FontStyles.Bold;
            qtyTxt.color = new Color(0.95f, 0.85f, 0.2f);
            qtyTxt.alignment = TextAlignmentOptions.Right;

            BookSlotView view = cellObj.AddComponent<BookSlotView>();
            view.isEquipSlot = false;
            view.gridIndex = index;
            view.bgFrame = bgFrame;
            view.borderFrame = borderImg;
            view.iconImage = iconImg;
            view.glyphText = glyphTxt;
            view.quantityText = qtyTxt;
            view.SetOwner(this);

            return view;
        }

        private TextMeshProUGUI CreateStatLabel(Transform parent, string title, string defaultVal, float yPos)
        {
            GameObject container = CreateRect("StatRow", parent, new Vector2(0.05f, 0f), new Vector2(0.95f, 0f), new Vector2(0, yPos), new Vector2(0, yPos + 18));

            GameObject titleObj = CreateRect("Title", container.transform, Vector2.zero, new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = title;
            titleTxt.fontSize = 11;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color(0.30f, 0.22f, 0.14f);
            titleTxt.alignment = TextAlignmentOptions.Left;

            GameObject valObj = CreateRect("Val", container.transform, new Vector2(0.55f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI valTxt = valObj.AddComponent<TextMeshProUGUI>();
            valTxt.text = defaultVal;
            valTxt.fontSize = 11;
            valTxt.fontStyle = FontStyles.Bold;
            valTxt.color = new Color(0.12f, 0.45f, 0.18f);
            valTxt.alignment = TextAlignmentOptions.Right;

            return valTxt;
        }

        private void BuildTooltip(Transform parent)
        {
            tooltipContainer = CreateRect("BookTooltip", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(280, 140));
            tooltipRect = tooltipContainer.GetComponent<RectTransform>();

            tooltipContainer.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.96f);
            tooltipContainer.AddComponent<Outline>().effectColor = new Color(0.75f, 0.60f, 0.25f, 0.9f);

            GameObject titleObj = CreateRect("Title", tooltipContainer.transform, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            tooltipTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            tooltipTitleText.fontSize = 13;
            tooltipTitleText.fontStyle = FontStyles.Bold;
            tooltipTitleText.color = Color.white;

            GameObject metaObj = CreateRect("Meta", tooltipContainer.transform, new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero);
            tooltipMetaText = metaObj.AddComponent<TextMeshProUGUI>();
            tooltipMetaText.fontSize = 10;
            tooltipMetaText.fontStyle = FontStyles.Italic;
            tooltipMetaText.color = new Color(0.75f, 0.75f, 0.75f);

            GameObject descObj = CreateRect("Desc", tooltipContainer.transform, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.54f), Vector2.zero, Vector2.zero);
            tooltipDescText = descObj.AddComponent<TextMeshProUGUI>();
            tooltipDescText.fontSize = 10;
            tooltipDescText.color = new Color(0.9f, 0.9f, 0.9f);

            GameObject bonusObj = CreateRect("Bonus", tooltipContainer.transform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.22f), Vector2.zero, Vector2.zero);
            tooltipBonusText = bonusObj.AddComponent<TextMeshProUGUI>();
            tooltipBonusText.fontSize = 11;
            tooltipBonusText.fontStyle = FontStyles.Bold;
            tooltipBonusText.color = new Color(0.35f, 0.88f, 0.45f);

            tooltipContainer.SetActive(false);
        }

        private void BuildDragGhost(Transform parent)
        {
            dragGhostObj = CreateRect("DragGhost", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(64, 64));
            dragGhostRect = dragGhostObj.GetComponent<RectTransform>();

            dragGhostIcon = dragGhostObj.AddComponent<Image>();
            dragGhostIcon.raycastTarget = false;

            GameObject glyphObj = CreateRect("Glyph", dragGhostObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dragGhostGlyph = glyphObj.AddComponent<TextMeshProUGUI>();
            dragGhostGlyph.fontSize = 24;
            dragGhostGlyph.alignment = TextAlignmentOptions.Center;
            dragGhostGlyph.raycastTarget = false;

            dragGhostObj.SetActive(false);
        }

        // =========================================================================
        // INTERACTION HANDLERS & POINTER EVENTS
        // =========================================================================
        public void OnSlotPointerEnter(BookSlotView slot)
        {
            ItemData item = GetItemInSlot(slot);
            if (item != null) ShowTooltip(item, slot.transform.position);
        }

        public void OnSlotPointerExit(BookSlotView slot) => HideTooltip();

        public void OnSlotLeftClick(BookSlotView slot) { }

        public void OnSlotRightClick(BookSlotView slot)
        {
            if (slot == null) return;

            if (slot.isEquipSlot)
            {
                EquipmentManager.Instance?.Unequip(slot.equipSlot);
            }
            else
            {
                IReadOnlyList<InventorySlot> items = InventoryManager.Instance?.Items;
                if (items != null && slot.gridIndex >= 0 && slot.gridIndex < items.Count)
                {
                    InventorySlot invSlot = items[slot.gridIndex];
                    if (invSlot != null && invSlot.item != null)
                    {
                        ItemData item = invSlot.item;
                        if (item.category == ItemCategory.Weapon || item.category == ItemCategory.Amulet || item.category == ItemCategory.Belt || item.category == ItemCategory.Ring)
                        {
                            EquipmentSlot targetSlot = GetTargetEquipSlot(item);
                            EquipmentManager.Instance?.Equip(item, targetSlot);
                            InventoryManager.Instance?.RemoveItem(item, 1);
                        }
                        else if (item.category == ItemCategory.Consumable)
                        {
                            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
                            if (ConsumableItem.TryUse(item, stats)) InventoryManager.Instance?.RemoveItem(item, 1);
                        }
                    }
                }
            }

            HideTooltip();
            RefreshUI();
        }

        public void OnSlotBeginDrag(BookSlotView slot)
        {
            ItemData item = GetItemInSlot(slot);
            if (item == null) return;

            draggedSlot = slot;
            if (dragGhostObj != null)
            {
                dragGhostObj.SetActive(true);
                if (item.customIcon != null)
                {
                    dragGhostIcon.sprite = item.customIcon;
                    dragGhostIcon.color = Color.white;
                    dragGhostGlyph.text = "";
                }
                else
                {
                    dragGhostIcon.color = Color.clear;
                    dragGhostGlyph.text = item.iconGlyph;
                }
            }
            HideTooltip();
        }

        public void OnSlotDrag(PointerEventData eventData)
        {
            if (draggedSlot != null && dragGhostObj != null) dragGhostRect.position = eventData.position;
        }

        public void OnSlotEndDrag()
        {
            draggedSlot = null;
            if (dragGhostObj != null) dragGhostObj.SetActive(false);
        }

        public void OnSlotDrop(BookSlotView targetSlot)
        {
            if (draggedSlot == null || targetSlot == null || draggedSlot == targetSlot) return;

            if (!draggedSlot.isEquipSlot && !targetSlot.isEquipSlot)
            {
                InventoryManager.Instance?.SwapSlots(draggedSlot.gridIndex, targetSlot.gridIndex);
            }
            else if (!draggedSlot.isEquipSlot && targetSlot.isEquipSlot)
            {
                IReadOnlyList<InventorySlot> items = InventoryManager.Instance?.Items;
                if (items != null && draggedSlot.gridIndex >= 0 && draggedSlot.gridIndex < items.Count)
                {
                    ItemData itemToEquip = items[draggedSlot.gridIndex]?.item;
                    if (itemToEquip != null && EquipmentManager.Instance != null && EquipmentManager.Instance.Equip(itemToEquip, targetSlot.equipSlot))
                    {
                        InventoryManager.Instance.RemoveItem(itemToEquip, 1);
                    }
                }
            }
            else if (draggedSlot.isEquipSlot && !targetSlot.isEquipSlot)
            {
                EquipmentManager.Instance?.Unequip(draggedSlot.equipSlot);
            }
            else if (draggedSlot.isEquipSlot && targetSlot.isEquipSlot)
            {
                EquipmentManager.Instance?.MoveEquipped(draggedSlot.equipSlot, targetSlot.equipSlot);
            }

            OnSlotEndDrag();
            RefreshUI();
        }

        private ItemData GetItemInSlot(BookSlotView slot)
        {
            if (slot == null) return null;
            if (slot.isEquipSlot) return EquipmentManager.Instance?.GetEquipped(slot.equipSlot);

            IReadOnlyList<InventorySlot> items = InventoryManager.Instance?.Items;
            if (items != null && slot.gridIndex >= 0 && slot.gridIndex < items.Count) return items[slot.gridIndex]?.item;
            return null;
        }

        private EquipmentSlot GetTargetEquipSlot(ItemData item)
        {
            if (item.category == ItemCategory.Weapon) return EquipmentSlot.Weapon;
            if (item.category == ItemCategory.Amulet) return EquipmentSlot.Amulet;
            if (item.category == ItemCategory.Belt)   return EquipmentSlot.Belt;
            if (EquipmentManager.Instance != null && EquipmentManager.Instance.ringSlot1 == null) return EquipmentSlot.Ring1;
            return EquipmentSlot.Ring2;
        }

        private void ShowTooltip(ItemData item, Vector3 worldPosition)
        {
            if (item == null || tooltipContainer == null) return;

            tooltipTitleText.text = item.itemName;
            tooltipTitleText.color = item.RarityColor;
            tooltipMetaText.text = $"{item.rarity} • {item.category}";
            tooltipDescText.text = item.description;
            tooltipBonusText.text = item.GetBonusSummary();

            tooltipContainer.SetActive(true);

            Vector2 mousePos = Input.mousePosition;
            float x = Mathf.Clamp(mousePos.x + 15f, 10f, Screen.width - 290f);
            float y = Mathf.Clamp(mousePos.y - 100f, 10f, Screen.height - 150f);
            tooltipRect.position = new Vector3(x, y, 0f);
        }

        private void HideTooltip()
        {
            if (tooltipContainer != null) tooltipContainer.SetActive(false);
        }

        private void HandleGoldChanged(int newGold) => RefreshGoldDisplay();
        private void HandleEquipmentChanged(EquipmentSlot slot, ItemData item) => RefreshUI();

        private GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);

            if (anchorMin != anchorMax)
            {
                rect.offsetMin = new Vector2(anchoredPos.x, anchoredPos.y);
                rect.offsetMax = new Vector2(sizeDelta.x, sizeDelta.y);
            }
            else
            {
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = sizeDelta;
            }

            return go;
        }
    }
}
