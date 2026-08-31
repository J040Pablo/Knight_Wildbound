using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Roguelite.Core;
using Roguelite.UI.Widgets;

namespace Roguelite.UI
{
    /// <summary>
    /// Root builder/orchestrator for the in-run HUD.
    ///
    /// This replaces the previous OnGUI()-based HUD with a fully procedural UGUI canvas
    /// (Canvas + CanvasScaler + EventSystem + child widgets), built entirely at runtime —
    /// consistent with how the rest of this project already spins up its scene hierarchy
    /// from code (see GameBootstrapper.SetupGameRunHierarchy). No prefabs, no manual scene
    /// wiring, nothing that can go missing/broken when the project is opened.
    ///
    /// Integration: GameBootstrapper already does `uiCanvasObj.AddComponent<HUDController>();`
    /// — the class name/namespace are unchanged on purpose so nothing else needs editing.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        public RectTransform CanvasRoot { get; private set; }
        private Canvas rootCanvas;
        private RunManager runManager;

        private void Awake()
        {
            Instance = this;
            BuildEventSystem();
            BuildCanvas();
            BuildWidgets();
        }

        private void Start()
        {
            runManager = FindFirstObjectByType<RunManager>();
        }

        private void Update()
        {
            if (runManager == null)
            {
                runManager = FindFirstObjectByType<RunManager>();
            }

            // Hide the whole HUD on the Game Over screen (matches previous OnGUI behaviour).
            if (rootCanvas != null)
            {
                bool shouldShow = runManager == null || runManager.State != RunState.GameOver;
                if (rootCanvas.enabled != shouldShow)
                {
                    rootCanvas.enabled = shouldShow;
                }
            }
        }

        private void BuildEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvasObj.transform.SetParent(transform, false);

            rootCanvas = canvasObj.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;

            CanvasRoot = canvasObj.GetComponent<RectTransform>();
        }

        private void BuildWidgets()
        {
            // Each widget is self-contained: builds its own visuals on Awake and drives
            // itself off the existing gameplay systems' public events/properties.
            CanvasRoot.gameObject.AddComponent<HUDVitalsWidget>();
            CanvasRoot.gameObject.AddComponent<HUDPlayerInfoWidget>();
            CanvasRoot.gameObject.AddComponent<HUDAbilityBarWidget>();
            CanvasRoot.gameObject.AddComponent<HUDBossBarWidget>();
            CanvasRoot.gameObject.AddComponent<HUDInteractionPromptWidget>();
            CanvasRoot.gameObject.AddComponent<HUDFloatingCombatText>();
            CanvasRoot.gameObject.AddComponent<HUDCrosshairWidget>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
