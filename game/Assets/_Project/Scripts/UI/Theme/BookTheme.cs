using UnityEngine;
using Roguelite.Items;

namespace Roguelite.UI.Theme
{
    /// <summary>
    /// Procedural visual theme generator for the Ancient RPG Book UI.
    /// Generates high-quality fantasy RPG textures at runtime: aged parchment paper,
    /// dark mahogany book cover, gold/bronze metallic slot frames, right-side vertical bookmark tabs,
    /// top-right gear settings button, mastery node frames, and item category icons.
    /// </summary>
    public static class BookTheme
    {
        private static Texture2D cachedAgedPaper;
        private static Texture2D cachedWoodCover;
        private static Texture2D cachedActiveRightTab;
        private static Texture2D cachedInactiveRightTab;
        private static Texture2D cachedGearBtnActive;
        private static Texture2D cachedGearBtnInactive;

        // ── Required Theme Interface ──────────────────────────────

        public static Sprite CreateBookBackground(int width = 1100, int height = 680)
        {
            return WoodBackgroundTexture(width, height);
        }

        public static Sprite CreateParchmentPage(int width = 640, int height = 720, int seed = 7)
        {
            return AgedPaperTexture(width, height, seed);
        }

        public static Sprite CreateEquipmentFrame(Color rarityColor)
        {
            return SquareFantasyFrame(rarityColor);
        }

        public static Sprite CreateSlotBackground()
        {
            return SquareFantasyFrame(Color.clear);
        }

        // ── Right Side Vertical Bookmark Tabs ──────────────────────

        public static Sprite CreateRightSideTabSprite(bool isActive, int width = 46, int height = 120)
        {
            if (isActive && cachedActiveRightTab != null)
                return Sprite.Create(cachedActiveRightTab, new Rect(0, 0, width, height), new Vector2(0f, 0.5f));
            if (!isActive && cachedInactiveRightTab != null)
                return Sprite.Create(cachedInactiveRightTab, new Rect(0, 0, width, height), new Vector2(0f, 0.5f));

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color fillBase   = isActive ? new Color(0.88f, 0.79f, 0.63f, 1.0f) : new Color(0.24f, 0.16f, 0.10f, 0.95f);
            Color fillDark   = isActive ? new Color(0.78f, 0.66f, 0.48f, 1.0f) : new Color(0.14f, 0.09f, 0.05f, 0.95f);
            Color borderGold = isActive ? new Color(0.85f, 0.68f, 0.25f, 1.0f) : new Color(0.45f, 0.32f, 0.18f, 0.90f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isRightBorder = x >= width - 3;
                    bool isTopBottomBorder = y <= 2 || y >= height - 3;
                    bool isLeftEdge = x <= 2; // Attaches to book spine/cover

                    if (isRightBorder || isTopBottomBorder)
                    {
                        tex.SetPixel(x, y, borderGold);
                    }
                    else if (isLeftEdge)
                    {
                        tex.SetPixel(x, y, borderGold * 0.7f);
                    }
                    else
                    {
                        float grad = x / (float)width;
                        tex.SetPixel(x, y, Color.Lerp(fillBase, fillDark, grad * 0.4f));
                    }
                }
            }

            tex.Apply();
            if (isActive) cachedActiveRightTab = tex;
            else cachedInactiveRightTab = tex;

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0f, 0.5f));
        }

        // ── Top-Right Gear Settings Button Sprite ─────────────────

        public static Sprite CreateGearButtonSprite(bool isActive, int width = 150, int height = 34)
        {
            if (isActive && cachedGearBtnActive != null)
                return Sprite.Create(cachedGearBtnActive, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            if (!isActive && cachedGearBtnInactive != null)
                return Sprite.Create(cachedGearBtnInactive, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color fillBase   = isActive ? new Color(0.45f, 0.35f, 0.22f, 0.95f) : new Color(0.20f, 0.14f, 0.09f, 0.90f);
            Color borderGold = isActive ? new Color(0.92f, 0.75f, 0.28f, 1.0f) : new Color(0.50f, 0.38f, 0.22f, 0.85f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x <= 2 || x >= width - 3 || y <= 2 || y >= height - 3;
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, borderGold);
                    }
                    else
                    {
                        tex.SetPixel(x, y, fillBase);
                    }
                }
            }

            tex.Apply();
            if (isActive) cachedGearBtnActive = tex;
            else cachedGearBtnInactive = tex;

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        // ── Mastery Node Frames ───────────────────────────────────

        public static Sprite CreateNodeFrame(bool unlocked, bool available, int width = 230, int height = 90)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color bgFill = unlocked ? new Color(0.22f, 0.32f, 0.18f, 0.92f) :
                          (available ? new Color(0.35f, 0.28f, 0.12f, 0.92f) : new Color(0.14f, 0.10f, 0.08f, 0.88f));

            Color borderColor = unlocked ? new Color(0.40f, 0.85f, 0.35f, 1.0f) :
                               (available ? new Color(0.95f, 0.78f, 0.25f, 1.0f) : new Color(0.40f, 0.30f, 0.20f, 0.70f));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x <= 3 || x >= width - 4 || y <= 3 || y >= height - 4;
                    bool isCorner = (x <= 10 || x >= width - 11) && (y <= 10 || y >= height - 11);

                    if (isCorner)
                    {
                        tex.SetPixel(x, y, new Color(0.78f, 0.60f, 0.28f, 1.0f));
                    }
                    else if (isBorder)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, bgFill);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        // ── Textures ──────────────────────────────────────────────

        public static Sprite AgedPaperTexture(int width = 640, int height = 720, int seed = 7)
        {
            if (cachedAgedPaper != null && cachedAgedPaper.width == width && cachedAgedPaper.height == height)
            {
                return Sprite.Create(cachedAgedPaper, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            }

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color paperBase  = new Color(0.91f, 0.83f, 0.69f, 1.0f); // Warm golden parchment
            Color paperDark  = new Color(0.81f, 0.70f, 0.53f, 1.0f); // Stain tint
            Color edgeShadow = new Color(0.35f, 0.24f, 0.14f, 0.95f); // Darkened paper edge

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise((x + seed * 80) * 0.012f, (y + seed * 80) * 0.012f);
                    Color c = Color.Lerp(paperBase, paperDark, noise * 0.5f);

                    float edgeX = Mathf.Min(x, width - 1 - x) / (float)width;
                    float edgeY = Mathf.Min(y, height - 1 - y) / (float)height;
                    float minEdge = Mathf.Min(edgeX, edgeY);

                    if (minEdge < 0.06f)
                    {
                        float f = 1f - (minEdge / 0.06f);
                        c = Color.Lerp(c, edgeShadow, f * 0.65f);
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            cachedAgedPaper = tex;
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        public static Sprite WoodBackgroundTexture(int width = 1100, int height = 680)
        {
            if (cachedWoodCover != null && cachedWoodCover.width == width && cachedWoodCover.height == height)
            {
                return Sprite.Create(cachedWoodCover, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            }

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color deskWood = new Color(0.18f, 0.12f, 0.07f, 1.0f); // Dark mahogany wood
            Color leatherCover = new Color(0.28f, 0.18f, 0.11f, 1.0f); // Leather book binding
            Color goldRim = new Color(0.72f, 0.55f, 0.25f, 1.0f); // Gold embossed rim

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.004f, y * 0.05f) * 0.12f;
                    Color c = deskWood;
                    c.r += grain;
                    c.g += grain * 0.6f;

                    bool isCover = x >= 14 && x <= width - 15 && y >= 14 && y <= height - 15;
                    bool isGoldBorder = (x >= 10 && x <= 14) || (x >= width - 15 && x <= width - 11) ||
                                        (y >= 10 && y <= 14) || (y >= height - 15 && y <= height - 11);

                    if (isCover)
                    {
                        c = leatherCover;
                    }
                    else if (isGoldBorder)
                    {
                        c = goldRim;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            cachedWoodCover = tex;
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        // ── Equipment & Inventory Slot Frames ─────────────────────

        public static Sprite SquareFantasyFrame(Color rarityColor, int size = 68)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color darkInlay = new Color(0.12f, 0.08f, 0.05f, 0.90f);
            Color borderBase = (rarityColor == Color.clear || rarityColor == Color.gray)
                ? new Color(0.48f, 0.38f, 0.25f, 0.95f)
                : rarityColor;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x <= 3 || x >= size - 4 || y <= 3 || y >= size - 4;
                    bool isCorner = (x <= 10 || x >= size - 11) && (y <= 10 || y >= size - 11);

                    if (isCorner)
                    {
                        tex.SetPixel(x, y, new Color(0.78f, 0.60f, 0.28f, 1.0f));
                    }
                    else if (isBorder)
                    {
                        tex.SetPixel(x, y, borderBase);
                    }
                    else
                    {
                        tex.SetPixel(x, y, darkInlay);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        // ── Procedural Category Icons ─────────────────────────────

        public static Sprite IconWeapon(int size = 48)
        {
            return DrawGlyphTexture(size, new Color(0.85f, 0.88f, 0.95f), (x, y, c) =>
            {
                float diag = Mathf.Abs(x - y);
                return (diag < 2.5f && (x + y) > size * 0.4f && (x + y) < size * 1.6f);
            });
        }

        public static Sprite IconAmulet(int size = 48)
        {
            return DrawGlyphTexture(size, new Color(0.95f, 0.75f, 0.25f), (x, y, c) =>
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c - 4));
                return (dist >= 6f && dist <= 12f);
            });
        }

        public static Sprite IconBelt(int size = 48)
        {
            return DrawGlyphTexture(size, new Color(0.75f, 0.55f, 0.35f), (x, y, c) =>
            {
                bool strap = Mathf.Abs(y - c) < 4f && Mathf.Abs(x - c) < size * 0.4f;
                bool buckle = Mathf.Abs(x - c) < 7f && Mathf.Abs(y - c) < 7f && (Mathf.Abs(x - c) > 4f || Mathf.Abs(y - c) > 4f);
                return strap || buckle;
            });
        }

        public static Sprite IconRing(int size = 48)
        {
            return DrawGlyphTexture(size, new Color(0.95f, 0.85f, 0.30f), (x, y, c) =>
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                return (dist >= 7f && dist <= 12f);
            });
        }

        public static Sprite IconBackpack(int size = 48)
        {
            return DrawGlyphTexture(size, new Color(0.65f, 0.45f, 0.25f), (x, y, c) =>
            {
                bool body = Mathf.Abs(x - c) < 14f && Mathf.Abs(y - (c - 2)) < 12f;
                bool flap = Mathf.Abs(x - c) < 12f && Mathf.Abs(y - (c + 6)) < 4f;
                return body || flap;
            });
        }

        public static Sprite CharacterSilhouette(int width = 180, int height = 280)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color silhColor = new Color(0.38f, 0.28f, 0.18f, 0.55f); // Golden-brown mannequin
            float cx = width * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isHead = Vector2.Distance(new Vector2(x, y), new Vector2(cx, height * 0.82f)) < 22f;
                    bool isChest = Mathf.Abs(x - cx) < 32f && y > height * 0.52f && y < height * 0.74f;
                    bool isLegs = (Mathf.Abs(x - (cx - 16f)) < 12f || Mathf.Abs(x - (cx + 16f)) < 12f) && y > height * 0.15f && y <= height * 0.52f;

                    if (isHead || isChest || isLegs)
                    {
                        tex.SetPixel(x, y, silhColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private static Sprite DrawGlyphTexture(int size, Color color, System.Func<float, float, float, bool> drawFunc)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (drawFunc(x, y, c))
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
