using UnityEngine;
using UnityEngine.UI;
using Roguelite.UI.Theme;
using Roguelite.Core.StateMachine;

namespace Roguelite.UI.Widgets
{
    /// <summary>
    /// Center-screen crosshair reticle for 3rd person combat aiming.
    /// Dual-layer UGUI implementation guaranteeing a solid 4px white center inside an 8px solid black border.
    /// Zero white outer border, zero antialias halo, zero glow, zero transparency artifacts.
    /// </summary>
    public class HUDCrosshairWidget : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        [SerializeField] private float outerBorderDiameter = 8f; // 8px solid black outline ring
        [SerializeField] private float innerWhiteDiameter = 4f;  // 4px solid white center dot

        private GameObject reticleRoot;

        private void Awake()
        {
            BuildCrosshairUI();
        }

        private void BuildCrosshairUI()
        {
            // Center container
            reticleRoot = new GameObject("HUD_Crosshair_Root", typeof(RectTransform));
            reticleRoot.transform.SetParent(transform, false);

            RectTransform rootRt = reticleRoot.GetComponent<RectTransform>();
            rootRt.SetRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(outerBorderDiameter, outerBorderDiameter));

            // Layer 1: Outer Solid Black Circle (8px diameter)
            Sprite circleSprite = HUDTheme.Circle(Color.white, 0);
            Image borderImage = HUDTheme.CreateImage(reticleRoot.transform, "Reticle_BlackBorder", circleSprite, Color.black);
            RectTransform borderRt = borderImage.rectTransform;
            borderRt.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Layer 2: Inner Solid White Center Dot (4px diameter)
            GameObject whiteDotObj = new GameObject("Reticle_WhiteDot", typeof(RectTransform), typeof(Image));
            whiteDotObj.transform.SetParent(borderImage.transform, false);

            RectTransform whiteRt = whiteDotObj.GetComponent<RectTransform>();
            whiteRt.SetRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(innerWhiteDiameter, innerWhiteDiameter));

            Image whiteImage = whiteDotObj.GetComponent<Image>();
            whiteImage.sprite = circleSprite;
            whiteImage.color = Color.white;
            whiteImage.raycastTarget = false;
        }

        private void Update()
        {
            if (reticleRoot == null) return;

            // Hide crosshair when menu or game over is active
            bool isMenuOpen = CharacterWindowUI.Instance != null && CharacterWindowUI.Instance.isOpen;
            if (!isMenuOpen && GameStateManager.Instance != null)
            {
                isMenuOpen = !GameStateManager.Instance.IsGameplayActive();
            }

            if (reticleRoot.activeSelf == isMenuOpen)
            {
                reticleRoot.SetActive(!isMenuOpen);
            }
        }
    }
}
