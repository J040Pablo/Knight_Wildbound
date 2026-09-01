using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Roguelite.Core.Events;
using Roguelite.UI.Theme;

namespace Roguelite.UI.Widgets
{
    public class HUDFloatingCombatText : MonoBehaviour
    {
        private class TextItem
        {
            public GameObject GameObject;
            public RectTransform RectTransform;
            public TextMeshProUGUI Text;
            public Vector3 WorldPos;
            public float Lifetime;
            public float MaxLifetime;
            public CombatTextType Type;
            public Vector2 Velocity;
        }

        private readonly List<TextItem> activeItems = new List<TextItem>();
        private readonly Queue<TextItem> pool = new Queue<TextItem>();

        private Camera mainCamera;
        private RectTransform canvasRect;

        private void Awake()
        {
            canvasRect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            GameEvents.OnCombatTextRequested += SpawnCombatText;
        }

        private void OnDisable()
        {
            GameEvents.OnCombatTextRequested -= SpawnCombatText;
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                TextItem item = activeItems[i];
                item.Lifetime += Time.deltaTime;

                if (item.Lifetime >= item.MaxLifetime)
                {
                    DespawnItem(item, i);
                    continue;
                }

                // Update World-to-Screen position
                Vector3 screenPos = mainCamera.WorldToScreenPoint(item.WorldPos);
                if (screenPos.z < 0)
                {
                    item.GameObject.SetActive(false);
                    continue;
                }

                if (!item.GameObject.activeSelf) item.GameObject.SetActive(true);

                // Convert Screen Position to Local Canvas Position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPoint);

                // Upward Drift Motion
                float progress = item.Lifetime / item.MaxLifetime;
                localPoint += new Vector2(item.Velocity.x * item.Lifetime, item.Velocity.y * item.Lifetime + progress * 40f);
                item.RectTransform.anchoredPosition = localPoint;

                // Scale Animation (Critical Pop)
                if (item.Type == CombatTextType.Critical)
                {
                    float scale = Mathf.Lerp(1.6f, 1.0f, Mathf.Clamp01(item.Lifetime * 6f));
                    item.RectTransform.localScale = Vector3.one * scale;
                }
                else
                {
                    item.RectTransform.localScale = Vector3.one;
                }

                // Fade Out Animation
                float alpha = 1.0f;
                if (progress > 0.6f)
                {
                    alpha = Mathf.Clamp01((1.0f - progress) / 0.4f);
                }

                Color c = item.Text.color;
                c.a = alpha;
                item.Text.color = c;
            }
        }

        private void SpawnCombatText(Vector3 worldPos, float amount, CombatTextType type)
        {
            TextItem item = GetPooledItem();
            item.WorldPos = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.4f), Random.Range(-0.3f, 0.3f));
            item.Lifetime = 0f;
            item.MaxLifetime = type == CombatTextType.Critical ? 1.0f : 0.8f;
            item.Type = type;
            item.Velocity = new Vector2(Random.Range(-15f, 15f), Random.Range(30f, 50f));

            switch (type)
            {
                case CombatTextType.Critical:
                    item.Text.text = $"{Mathf.RoundToInt(amount)}!";
                    item.Text.fontSize = 24f;
                    item.Text.color = HUDTheme.LegendaryOrange;
                    item.Text.fontStyle = FontStyles.Bold;
                    break;
                case CombatTextType.Heal:
                    item.Text.text = $"+{Mathf.RoundToInt(amount)}";
                    item.Text.fontSize = 18f;
                    item.Text.color = HUDTheme.StaminaGreen;
                    item.Text.fontStyle = FontStyles.Bold;
                    break;
                case CombatTextType.Normal:
                default:
                    item.Text.text = $"{Mathf.RoundToInt(amount)}";
                    item.Text.fontSize = 18f;
                    item.Text.color = HUDTheme.TextCream;
                    item.Text.fontStyle = FontStyles.Normal;
                    break;
            }

            item.GameObject.SetActive(true);
            activeItems.Add(item);
        }

        private TextItem GetPooledItem()
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            TextItem item = new TextItem();
            GameObject go = new GameObject("CombatText_Item", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            item.GameObject = go;
            item.RectTransform = go.GetComponent<RectTransform>();
            item.RectTransform.sizeDelta = new Vector2(150f, 40f);

            item.Text = go.AddComponent<TextMeshProUGUI>();
            item.Text.font = HUDTheme.DefaultFont;
            item.Text.alignment = TextAlignmentOptions.Center;
            item.Text.raycastTarget = false;
            item.Text.textWrappingMode = TextWrappingModes.NoWrap;
            item.Text.overflowMode = TextOverflowModes.Overflow;

            return item;
        }

        private void DespawnItem(TextItem item, int index)
        {
            item.GameObject.SetActive(false);
            activeItems.RemoveAt(index);
            pool.Enqueue(item);
        }
    }
}
