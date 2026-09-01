using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Roguelite.UI.Theme
{
    /// <summary>
    /// Central place for the "cartoon low-poly fantasy" look of the whole UI/UX system.
    ///
    /// The project ships with zero imported art (no sprites, no custom fonts) — see the
    /// analysis notes in HUDController. Rather than depending on external assets that would
    /// need to be manually imported/wired in the Editor (fragile, and against the brief's
    /// "no broken references" requirement), every panel, frame and icon here is generated
    /// procedurally at runtime as a small Texture2D → Sprite, using rounded-rect / circle /
    /// simple-polygon signed-distance fields for clean anti-aliased edges. Swap any of these
    /// for hand-painted art later by simply replacing the Sprite an Image uses — nothing else
    /// needs to change.
    /// </summary>
    public static class HUDTheme
    {
        // ------------------------------------------------------------------
        // Palette — warm, saturated "storybook fantasy" colors (not realistic, not sci-fi).
        // ------------------------------------------------------------------
        public static readonly Color WoodDark = new Color32(0x3B, 0x28, 0x1E, 0xF0);
        public static readonly Color WoodMid = new Color32(0x5C, 0x3D, 0x27, 0xFF);
        public static readonly Color Parchment = new Color32(0xF2, 0xE1, 0xB8, 0xFF);
        public static readonly Color GoldAccent = new Color32(0xF2, 0xC1, 0x4E, 0xFF);
        public static readonly Color GoldDim = new Color32(0xB8, 0x8A, 0x2E, 0xFF);

        public static readonly Color HPRed = new Color32(0xE3, 0x3A, 0x3A, 0xFF);
        public static readonly Color HPRedDark = new Color32(0x7A, 0x14, 0x14, 0xFF);
        public static readonly Color HPChip = new Color32(0xFF, 0xB0, 0x7A, 0xFF);

        public static readonly Color StaminaGreen = new Color32(0x6F, 0xC6, 0x3B, 0xFF);
        public static readonly Color StaminaYellow = new Color32(0xD8, 0xE0, 0x3E, 0xFF);

        public static readonly Color XPBlue = new Color32(0x3D, 0xA9, 0xF5, 0xFF);
        public static readonly Color XPBlueDark = new Color32(0x18, 0x4E, 0x7A, 0xFF);

        public static readonly Color PanelFill = new Color(0.07f, 0.06f, 0.09f, 0.80f);
        public static readonly Color PanelBorder = new Color32(0x2E, 0x22, 0x16, 0xFF);

        public static readonly Color TextCream = new Color32(0xFB, 0xF3, 0xDE, 0xFF);
        public static readonly Color TextDim = new Color32(0xC9, 0xBE, 0xA6, 0xFF);

        public static readonly Color CommonGray = new Color32(0x9A, 0x9A, 0x9A, 0xFF);
        public static readonly Color RareBlue = new Color32(0x40, 0x8C, 0xF2, 0xFF);
        public static readonly Color EpicPurple = new Color32(0xA3, 0x45, 0xE6, 0xFF);
        public static readonly Color LegendaryOrange = new Color32(0xF2, 0x8F, 0x1A, 0xFF);

        private static TMP_FontAsset _font;
        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_font == null) _font = TMP_Settings.defaultFontAsset;
                return _font;
            }
        }

        // ------------------------------------------------------------------
        // Signed-distance-field primitives (all: negative = inside, 0 = edge, positive = outside)
        // ------------------------------------------------------------------
        private static float SdCircle(Vector2 p, Vector2 c, float r) => Vector2.Distance(p, c) - r;

        private static float SdBox(Vector2 p, Vector2 c, Vector2 half)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half;
            float outside = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(d.x, d.y), 0f);
            return outside + inside;
        }

        private static float SdRoundBox(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            return SdBox(p, c, half - new Vector2(r, r)) - r;
        }

        private static float SdSegment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude - r;
        }

        /// <summary>Max-of-halfplanes field for a convex polygon. List points in clockwise order (y-up space).</summary>
        private static float SdConvexPoly(Vector2 p, params Vector2[] pts)
        {
            float d = float.NegativeInfinity;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[(i + 1) % pts.Length];
                Vector2 e = b - a;
                Vector2 n = new Vector2(-e.y, e.x).normalized;
                d = Mathf.Max(d, Vector2.Dot(p - a, n));
            }
            return d;
        }

        private static float SdUnion(float a, float b) => Mathf.Min(a, b);
        private static float SdSubtract(float a, float b) => Mathf.Max(a, -b);
        private static float SdIntersect(float a, float b) => Mathf.Max(a, b);

        // ------------------------------------------------------------------
        // Texture / sprite generation
        // ------------------------------------------------------------------

        /// <summary>Rounded, optionally bordered panel sprite — 9-sliced so it stretches cleanly to any size.</summary>
        public static Sprite RoundedRect(int radius, Color fill, int border, Color borderColor)
        {
            int size = Mathf.Max(32, (radius + border) * 2 + 8);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            Vector2 half = new Vector2(size / 2f - 1f, size / 2f - 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SdRoundBox(p, center, half, radius);

                    Color baseColor = fill;
                    if (border > 0)
                    {
                        float borderT = Mathf.Clamp01(0.5f - (d + border));
                        baseColor = Color.Lerp(fill, borderColor, borderT);
                    }
                    float a = Mathf.Clamp01(0.5f - d);
                    tex.SetPixel(x, y, new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * a));
                }
            }
            tex.Apply();

            int b = radius + border + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        /// <summary>Simple filled/bordered circle sprite (used for icon backdrops and rounded bar caps).</summary>
        public static Sprite Circle(Color fill, int border = 0, Color borderColor = default)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float r = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SdCircle(p, center, r);
                    Color baseColor = fill;
                    if (border > 0)
                    {
                        float borderT = Mathf.Clamp01((d + border) / border);
                        baseColor = Color.Lerp(fill, borderColor, borderT);
                    }
                    float a = Mathf.Clamp01(0.5f - d);
                    tex.SetPixel(x, y, new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite IconFromSDF(System.Func<Vector2, float> sdf, Color color)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Normalized coords, -1..1, y-up, origin center.
                    Vector2 p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    float d = sdf(p) * (size * 0.5f); // scale back to pixel-ish units for a ~1px AA band
                    float a = Mathf.Clamp01(0.5f - d);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Sword silhouette — Basic Attack (LMB).</summary>
        public static Sprite IconSword(Color color) => IconFromSDF(p =>
        {
            float blade = SdConvexPoly(p, new Vector2(0f, 0.92f), new Vector2(0.14f, -0.05f), new Vector2(-0.14f, -0.05f));
            float guard = SdBox(p, new Vector2(0f, -0.08f), new Vector2(0.30f, 0.045f));
            float handle = SdSegment(p, new Vector2(0f, -0.12f), new Vector2(0f, -0.55f), 0.07f);
            float pommel = SdCircle(p, new Vector2(0f, -0.62f), 0.10f);
            return SdUnion(SdUnion(blade, guard), SdUnion(handle, pommel));
        }, color);

        /// <summary>Angular starburst — Charged Attack (RMB).</summary>
        public static Sprite IconBurst(Color color) => IconFromSDF(p =>
        {
            float angle = Mathf.Atan2(p.y, p.x);
            float radius = p.magnitude;
            float maxR = 0.34f + 0.32f * Mathf.Pow(Mathf.Abs(Mathf.Cos(4f * angle)), 0.6f);
            return radius - maxR;
        }, color);

        /// <summary>Three-bladed trinity/vortex glyph — Special Ability (Q).</summary>
        public static Sprite IconPinwheel(Color color) => IconFromSDF(p =>
        {
            float d = float.PositiveInfinity;
            for (int i = 0; i < 3; i++)
            {
                float ang = i * 120f * Mathf.Deg2Rad;
                Vector2 c = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.30f;
                d = SdUnion(d, SdCircle(p, c, 0.34f));
            }
            d = SdSubtract(d, SdCircle(p, Vector2.zero, 0.16f));
            return d;
        }, color);

        /// <summary>Crescent moon — Ring of Shadows (R).</summary>
        public static Sprite IconCrescent(Color color) => IconFromSDF(p =>
        {
            float main = SdCircle(p, Vector2.zero, 0.62f);
            float bite = SdCircle(p, new Vector2(0.32f, 0.08f), 0.56f);
            return SdSubtract(main, bite);
        }, color);

        /// <summary>Rounded shield — Knight class glyph.</summary>
        public static Sprite IconShield(Color color) => IconFromSDF(p =>
        {
            float top = SdRoundBox(p, new Vector2(0f, 0.18f), new Vector2(0.42f, 0.42f), 0.14f);
            float bottom = SdConvexPoly(p, new Vector2(-0.42f, -0.05f), new Vector2(0.42f, -0.05f), new Vector2(0f, -0.85f));
            return SdUnion(top, bottom);
        }, color);

        /// <summary>Wand with orb — Mage class glyph.</summary>
        public static Sprite IconWand(Color color) => IconFromSDF(p =>
        {
            float stick = SdSegment(p, new Vector2(-0.45f, -0.7f), new Vector2(0.45f, 0.55f), 0.07f);
            float orb = SdCircle(p, new Vector2(0.55f, 0.68f), 0.22f);
            return SdUnion(stick, orb);
        }, color);

        /// <summary>Leaf (vesica) — Druid class glyph.</summary>
        public static Sprite IconLeaf(Color color) => IconFromSDF(p =>
        {
            float a = SdCircle(p, new Vector2(-0.42f, -0.42f), 0.78f);
            float b = SdCircle(p, new Vector2(0.42f, 0.42f), 0.78f);
            return SdIntersect(a, b);
        }, color);

        // ------------------------------------------------------------------
        // Small UGUI builder helpers shared by every HUD widget
        // ------------------------------------------------------------------

        public static RectTransform SetRect(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Sprite sprite, Color color, Image.Type type = Image.Type.Sliced)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            img.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, Image.Type type = Image.Type.Simple)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize,
            Color color, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = DefaultFont;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }
    }
}
