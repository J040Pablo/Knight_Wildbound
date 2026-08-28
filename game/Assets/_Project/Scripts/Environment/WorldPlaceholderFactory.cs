using UnityEngine;

namespace Roguelite.Environment
{
    /// <summary>
    /// Builds placeholder GameObjects for every PlaceholderAssetKey using Unity primitives.
    ///
    /// HOW TO REPLACE AN ASSET:
    ///   1. Find the PropMarker / LandmarkMarker in scene hierarchy.
    ///   2. Set assetOverrideReady = true on the marker component.
    ///   3. Attach your real prefab as a child of the marker GameObject.
    ///   4. The factory will skip generating a primitive for that marker.
    ///   SceneEnvironmentBuilder.cs never needs to change.
    ///
    /// OPTIMIZATION NOTE:
    ///   Materials are cached per color across the entire build pass.
    ///   Uses the active URP default material template to prevent missing shader pink material issues.
    /// </summary>
    public static class WorldPlaceholderFactory
    {
        // ── Material cache (cleared at world build start) ─────────────
        private static readonly System.Collections.Generic.Dictionary<int, Material> _matCache
            = new System.Collections.Generic.Dictionary<int, Material>();

        private static Material _baseTemplateMaterial;

        public static void ClearCache()
        {
            _matCache.Clear();
            _baseTemplateMaterial = null;
        }

        // ─────────────────────────────────────────────────────────────────
        //  PRIMARY ENTRY POINT
        // ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// Build a placeholder for the given key under 'parent'.
        /// Returns the root GameObject of the placeholder.
        /// If colorOverride is provided it overrides the default palette.
        /// </summary>
        public static GameObject Build(PlaceholderAssetKey key, Transform parent,
                                       Color? colorOverride = null, float scale = 1f)
        {
            return key switch
            {
                // Terrain
                PlaceholderAssetKey.GroundTile        => MakeSlab("PH_Ground",       parent, new Vector3(1f,0.02f,1f), colorOverride ?? new Color(0.310f, 0.608f, 0.271f), scale),
                PlaceholderAssetKey.HillCap           => MakeHill("PH_Hill",         parent, colorOverride ?? new Color(0.212f, 0.459f, 0.227f), scale),
                PlaceholderAssetKey.CliffFace         => MakeSlab("PH_Cliff",        parent, new Vector3(0.15f,1f,1f), colorOverride ?? new Color(0.349f, 0.388f, 0.416f), scale),
                PlaceholderAssetKey.CliffPillar       => MakeCylinder("PH_CliffPillar", parent, new Vector3(0.8f,3f,0.8f), colorOverride ?? new Color(0.349f, 0.388f, 0.416f), scale),
                PlaceholderAssetKey.WaterPlane        => MakeSlab("PH_Water",        parent, new Vector3(1f,0.01f,1f), colorOverride ?? new Color(0.349f, 0.725f, 0.776f), scale),
                PlaceholderAssetKey.WaterWaterfall    => MakeWaterfall("PH_Waterfall", parent, colorOverride ?? new Color(0.349f, 0.725f, 0.776f), scale),
                PlaceholderAssetKey.RiverSlab         => MakeSlab("PH_River",        parent, new Vector3(1f,0.01f,1f), colorOverride ?? new Color(0.349f, 0.725f, 0.776f), scale),
                PlaceholderAssetKey.RiverBank         => MakeSlab("PH_RiverBank",    parent, new Vector3(1f,0.01f,1f), colorOverride ?? new Color(0.604f, 0.388f, 0.247f), scale),
                PlaceholderAssetKey.TerrainHillSlab   => MakeSlab("PH_HillSlab",     parent, new Vector3(1f,0.03f,1f), colorOverride ?? new Color(0.212f, 0.459f, 0.227f), scale),
                PlaceholderAssetKey.RiverSegment      => MakeRiverSegment("PH_RiverSeg", parent, scale),
                PlaceholderAssetKey.LakeWater         => MakeSphere("PH_LakeWater",  parent, new Vector3(1f,0.02f,1f), colorOverride ?? new Color(0.196f, 0.541f, 0.643f), scale),
                PlaceholderAssetKey.LakeIsland        => MakeHill("PH_LakeIsland",   parent, colorOverride ?? new Color(0.310f, 0.608f, 0.271f), scale),
                PlaceholderAssetKey.RockShelf         => MakeSlab("PH_RockShelf",    parent, new Vector3(1f,0.2f,1f), colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),

                // Vegetation
                PlaceholderAssetKey.TreeDeciduous     => MakeTree("PH_TreeDeciduous",parent, new Color(0.463f, 0.318f, 0.227f), colorOverride ?? new Color(0.306f, 0.580f, 0.275f), scale),
                PlaceholderAssetKey.TreePine          => MakeTree("PH_TreePine",     parent, new Color(0.463f, 0.318f, 0.227f), colorOverride ?? new Color(0.184f, 0.439f, 0.220f), scale, pine:true),
                PlaceholderAssetKey.TreeWillow        => MakeTree("PH_TreeWillow",   parent, new Color(0.463f, 0.318f, 0.227f), colorOverride ?? new Color(0.455f, 0.722f, 0.353f), scale),
                PlaceholderAssetKey.TreeAncient       => MakeAncientTree("PH_Ancient", parent, colorOverride ?? new Color(0.184f, 0.439f, 0.220f), scale),
                PlaceholderAssetKey.TreeDeadSmall     => MakeDeadTree("PH_DeadSm",   parent, scale * 0.7f),
                PlaceholderAssetKey.TreeDeadGiant     => MakeDeadTree("PH_DeadLg",   parent, scale * 1.5f),
                PlaceholderAssetKey.HeroTree          => MakeHeroTree("PH_HeroTree", parent, colorOverride ?? new Color(0.306f, 0.580f, 0.275f), scale),
                PlaceholderAssetKey.BushSmall         => MakeSphere("PH_BushSm",     parent, new Vector3(1.2f,0.7f,1.2f), colorOverride ?? new Color(0.306f, 0.580f, 0.275f), scale),
                PlaceholderAssetKey.BushLarge         => MakeSphere("PH_BushLg",     parent, new Vector3(1.6f,0.8f,1.6f), colorOverride ?? new Color(0.184f, 0.439f, 0.220f), scale),
                PlaceholderAssetKey.FlowerCluster     => MakeFlowers("PH_Flowers",   parent, colorOverride ?? new Color(0.92f, 0.48f, 0.65f), scale),
                PlaceholderAssetKey.GrassClump        => MakeSphere("PH_Grass",      parent, new Vector3(0.8f,0.3f,0.8f), colorOverride ?? new Color(0.471f, 0.725f, 0.341f), scale),
                PlaceholderAssetKey.MushroomSingle    => MakeMushroom("PH_Shroom",   parent, scale),
                PlaceholderAssetKey.MushroomGroup     => MakeMushroomGroup("PH_ShroomGrp", parent, scale),
                PlaceholderAssetKey.FallenLog         => MakeCylinder("PH_Log",      parent, new Vector3(0.4f, 2.0f, 0.4f), colorOverride ?? new Color(0.463f, 0.318f, 0.227f), scale),

                // Rocks
                PlaceholderAssetKey.RockPebble        => MakeSphere("PH_Pebble",     parent, new Vector3(0.3f,0.25f,0.35f), colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),
                PlaceholderAssetKey.RockMedium        => MakeSphere("PH_RockMed",    parent, new Vector3(0.7f,0.55f,0.8f), colorOverride ?? new Color(0.349f, 0.388f, 0.416f), scale),
                PlaceholderAssetKey.RockBoulder       => MakeSphere("PH_Boulder",    parent, new Vector3(1.2f,0.9f,1.3f), colorOverride ?? new Color(0.349f, 0.388f, 0.416f), scale),
                PlaceholderAssetKey.RockPillarGiant   => MakePillar("PH_Pillar",    parent, colorOverride ?? new Color(0.349f, 0.388f, 0.416f), scale),
                PlaceholderAssetKey.RockArch          => MakeArch("PH_RockArch",    parent, colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),
                PlaceholderAssetKey.RockCliffWall     => MakeSlab("PH_CliffWall",   parent, new Vector3(0.2f,1f,1f), colorOverride ?? new Color(0.349f, 0.388f, 0.416f), scale),
                PlaceholderAssetKey.RockCaveEntrance  => MakeCaveEntrance("PH_Cave", parent, colorOverride ?? new Color(0.24f, 0.26f, 0.28f), scale),
                PlaceholderAssetKey.RockClusterGroup  => MakeRockCluster("PH_RockCluster", parent, colorOverride ?? new Color(0.349f, 0.388f, 0.416f), scale),

                // Structures
                PlaceholderAssetKey.RuinWallSegment   => MakeSlab("PH_Wall",        parent, new Vector3(0.2f,0.8f,1f), colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),
                PlaceholderAssetKey.RuinPillar        => MakeCylinder("PH_RPillar", parent, new Vector3(0.5f,1.8f,0.5f), colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),
                PlaceholderAssetKey.RuinTowerLandmark => MakeRuinedTower("PH_Tower", parent, colorOverride ?? new Color(0.48f, 0.46f, 0.44f), scale),
                PlaceholderAssetKey.RuinGate          => MakeSlab("PH_Gate",        parent, new Vector3(1f,0.9f,0.1f), colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),
                PlaceholderAssetKey.RuinStatue        => MakeStatue("PH_Statue",    parent, colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),
                PlaceholderAssetKey.RuinAqueductArch  => MakeAqueductArch("PH_Aqueduct", parent, colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),
                PlaceholderAssetKey.RuinWatchtower    => MakeWatchtower("PH_Watchtower", parent, colorOverride ?? new Color(0.48f, 0.46f, 0.44f), scale),
                PlaceholderAssetKey.WoodenBridge      => MakeBridge("PH_WoodBridge",parent, colorOverride ?? new Color(0.463f, 0.318f, 0.227f), scale),
                PlaceholderAssetKey.StoneBridge       => MakeBridge("PH_StoneBridge",parent, colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),
                PlaceholderAssetKey.StoneSteps        => MakeSteps("PH_Steps",      parent, colorOverride ?? new Color(0.522f, 0.549f, 0.541f), scale),

                // Props
                PlaceholderAssetKey.Campfire          => MakeCampfire("PH_Campfire", parent),
                PlaceholderAssetKey.CampTent          => MakeTent("PH_Tent",        parent, colorOverride ?? new Color(0.55f,0.40f,0.25f), scale),
                PlaceholderAssetKey.LanternPost       => MakeLantern("PH_Lantern",  parent),
                PlaceholderAssetKey.Chest             => MakeSlab("PH_Chest",       parent, new Vector3(0.6f,0.45f,0.4f), colorOverride ?? new Color(0.55f,0.38f,0.12f), scale),
                PlaceholderAssetKey.WeaponPedestal    => MakeCylinder("PH_Pedestal",parent, new Vector3(0.6f,0.3f,0.6f), colorOverride ?? new Color(0.36f,0.33f,0.30f), scale),
                PlaceholderAssetKey.WeaponSword       => MakeSlab("PH_Sword",       parent, new Vector3(0.08f,0.7f,0.1f), colorOverride ?? new Color(0.82f,0.85f,0.92f), scale),
                PlaceholderAssetKey.WeaponStaff       => MakeCylinder("PH_Staff",   parent, new Vector3(0.06f,0.75f,0.06f), colorOverride ?? new Color(0.82f,0.70f,0.20f), scale),
                PlaceholderAssetKey.WeaponBranch      => MakeCylinder("PH_Branch",  parent, new Vector3(0.06f,0.72f,0.06f), colorOverride ?? new Color(0.45f,0.30f,0.15f), scale),
                PlaceholderAssetKey.ForgottenShrine   => MakeShrine("PH_Shrine",    parent, colorOverride ?? new Color(0.50f,0.48f,0.44f), scale),
                PlaceholderAssetKey.AncientAltar      => MakeAltar("PH_Altar",      parent, colorOverride ?? new Color(0.45f,0.42f,0.38f), scale),
                PlaceholderAssetKey.GlowingCrystal    => MakeCrystal("PH_Crystal",  parent, colorOverride ?? new Color(0.55f,0.15f,0.90f), scale),
                PlaceholderAssetKey.Pond              => MakeSphere("PH_Pond",      parent, new Vector3(1f,0.05f,1f), colorOverride ?? new Color(0.20f,0.45f,0.78f), scale),
                PlaceholderAssetKey.AbandonedCamp     => MakeAbandonedCamp("PH_AbandonedCamp", parent, scale),
                PlaceholderAssetKey.LoreSignPost      => MakeLoreSign("PH_LoreSign", parent, scale),
                PlaceholderAssetKey.DestroyedWagon    => MakeWagon("PH_Wagon",      parent, scale),

                // NPCs
                PlaceholderAssetKey.KingNPC           => MakeKingNPC("PH_King",     parent),
                PlaceholderAssetKey.FriendlyHorse     => MakeHorsePlaceholder("PH_Horse", parent),

                // Landmarks
                PlaceholderAssetKey.LandmarkWaterfall          => MakeWaterfall("LM_Waterfall",   parent, new Color(0.55f,0.72f,0.95f), scale * 2f),
                PlaceholderAssetKey.LandmarkGiantAncestralTree => MakeAncientTree("LM_AncTree",   parent, new Color(0.22f,0.50f,0.15f), scale * 2.5f),
                PlaceholderAssetKey.LandmarkCrystalFormation   => MakeCrystalFormation("LM_Crystals", parent, new Color(0.55f,0.15f,0.90f), scale),
                PlaceholderAssetKey.LandmarkBossHollowTree     => MakeBossTree("LM_BossTree",     parent, scale),
                PlaceholderAssetKey.LandmarkStoneArch          => MakeArch("LM_StoneArch",       parent, new Color(0.42f,0.40f,0.36f), scale * 1.5f),
                PlaceholderAssetKey.LandmarkVistaPoint         => MakePillar("LM_Vista",          parent, new Color(0.45f,0.42f,0.38f), scale),
                PlaceholderAssetKey.LandmarkRuinedTower        => MakeRuinedTower("LM_RuinTower", parent, new Color(0.44f,0.41f,0.36f), scale * 1.4f),
                PlaceholderAssetKey.LandmarkGiantObelisk       => MakeObelisk("LM_Obelisk",       parent, colorOverride ?? new Color(0.35f,0.35f,0.40f), scale),

                PlaceholderAssetKey.ExitGate => MakeExitGate("PH_ExitGate", parent, scale),

                _ => new GameObject($"PH_Unhandled_{key}")
            };
        }

        // ─────────────────────────────────────────────────────────────────
        //  PRIMITIVE CONSTRUCTORS
        // ─────────────────────────────────────────────────────────────────
        private static GameObject MakeSlab(string name, Transform parent, Vector3 localScale, Color color, float scale = 1f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localScale = localScale * scale;
            Apply(g, color);
            Strip(g);
            return g;
        }

        private static GameObject MakeSphere(string name, Transform parent, Vector3 localScale, Color color, float scale = 1f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localScale = localScale * scale;
            Apply(g, color);
            Strip(g);
            return g;
        }

        private static GameObject MakeCylinder(string name, Transform parent, Vector3 localScale, Color color, float scale = 1f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localScale = localScale * scale;
            Apply(g, color);
            Strip(g);
            return g;
        }

        private static GameObject MakeHill(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var s = MakeSphere("Cap", root.transform, new Vector3(1f, 0.45f, 1f), color, scale);
            s.transform.localPosition = new Vector3(0, scale * 0.45f * 0.5f, 0);
            return root;
        }

        private static GameObject MakeTree(string name, Transform parent,
            Color trunkColor, Color foliageColor, float scale, bool pine = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var trunk = MakeCylinder("Trunk", root.transform, new Vector3(0.3f, 1.5f, 0.3f), trunkColor, scale);
            trunk.transform.localPosition = new Vector3(0, scale * 1.5f, 0);
            if (pine)
            {
                for (int i = 0; i < 3; i++)
                {
                    float yOff = scale * (2.2f + i * 0.8f);
                    float coneScale = scale * (1.4f - i * 0.3f);
                    var cone = MakeSphere($"PineLayer{i}", root.transform,
                        new Vector3(coneScale, coneScale * 0.6f, coneScale), foliageColor);
                    cone.transform.localPosition = new Vector3(0, yOff, 0);
                }
            }
            else
            {
                var canopy = MakeSphere("Canopy", root.transform, new Vector3(1.8f, 1.0f, 1.8f), foliageColor, scale);
                canopy.transform.localPosition = new Vector3(0, scale * 3.4f, 0);
            }
            return root;
        }

        private static GameObject MakeHeroTree(string name, Transform parent, Color foliageColor, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            // 10m trunk, 25m canopy
            var trunk = MakeCylinder("HeroTrunk", root.transform, new Vector3(1.2f, 5.0f, 1.2f), new Color(0.28f, 0.18f, 0.10f), scale);
            trunk.transform.localPosition = new Vector3(0, scale * 5.0f, 0);
            var mainCanopy = MakeSphere("HeroCanopyMain", root.transform, new Vector3(4.5f, 2.2f, 4.5f), foliageColor, scale);
            mainCanopy.transform.localPosition = new Vector3(0, scale * 11.5f, 0);
            var subCanopy = MakeSphere("HeroCanopySub", root.transform, new Vector3(3.2f, 1.5f, 3.2f), new Color(foliageColor.r*0.85f, foliageColor.g*0.9f, foliageColor.b*0.8f), scale);
            subCanopy.transform.localPosition = new Vector3(1.2f * scale, scale * 9.5f, 1.0f * scale);
            return root;
        }

        private static GameObject MakeAncientTree(string name, Transform parent, Color foliageColor, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var trunk = MakeCylinder("Trunk", root.transform, new Vector3(0.7f, 3.5f, 0.7f), new Color(0.28f, 0.18f, 0.10f), scale);
            trunk.transform.localPosition = new Vector3(0, scale * 3.5f, 0);
            var canopy = MakeSphere("BigCanopy", root.transform, new Vector3(3.2f, 1.4f, 3.2f), foliageColor, scale);
            canopy.transform.localPosition = new Vector3(0, scale * 8.0f, 0);
            var canopy2 = MakeSphere("SubCanopy", root.transform, new Vector3(2.2f, 0.9f, 2.2f), new Color(foliageColor.r * 0.85f, foliageColor.g * 0.9f, foliageColor.b * 0.8f), scale);
            canopy2.transform.localPosition = new Vector3(0.5f * scale, scale * 6.5f, 0.5f * scale);
            return root;
        }

        private static GameObject MakeDeadTree(string name, Transform parent, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var trunk = MakeCylinder("Trunk", root.transform, new Vector3(0.4f, 2.0f, 0.4f), new Color(0.15f, 0.10f, 0.08f), scale);
            trunk.transform.localPosition = new Vector3(0, scale * 2f, 0);
            trunk.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
            var branch1 = MakeCylinder("Branch1", root.transform, new Vector3(0.15f, 0.8f, 0.15f), new Color(0.12f, 0.08f, 0.06f), scale);
            branch1.transform.localPosition = new Vector3(0.3f * scale, scale * 3.2f, 0);
            branch1.transform.localRotation = Quaternion.Euler(0, 45f, -50f);
            return root;
        }

        private static GameObject MakeMushroom(string name, Transform parent, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var stem = MakeCylinder("Stem", root.transform, new Vector3(0.1f, 0.25f, 0.1f), new Color(0.85f, 0.80f, 0.72f), scale);
            stem.transform.localPosition = new Vector3(0, scale * 0.25f, 0);
            var cap = MakeSphere("Cap", root.transform, new Vector3(0.35f, 0.18f, 0.35f), new Color(0.85f, 0.20f, 0.15f), scale);
            cap.transform.localPosition = new Vector3(0, scale * 0.45f, 0);
            return root;
        }

        private static GameObject MakeMushroomGroup(string name, Transform parent, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Vector3[] offsets = { Vector3.zero, new Vector3(0.4f,0,0.3f), new Vector3(-0.35f,0,0.2f), new Vector3(0.2f,0,-0.4f) };
            float[] scales = { 1f, 0.7f, 0.85f, 0.6f };
            for (int i = 0; i < offsets.Length; i++)
            {
                var m = MakeMushroom($"Shroom{i}", root.transform, scale * scales[i]);
                m.transform.localPosition = offsets[i];
            }
            return root;
        }

        private static GameObject MakeFlowers(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            for (int i = 0; i < 5; i++)
            {
                float ax = Random.Range(-0.6f, 0.6f), az = Random.Range(-0.6f, 0.6f);
                var f = MakeSphere($"Flower{i}", root.transform, new Vector3(0.12f, 0.12f, 0.12f), color);
                f.transform.localPosition = new Vector3(ax * scale, 0.12f * scale, az * scale);
            }
            return root;
        }

        private static GameObject MakePillar(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = MakeCylinder("Body", root.transform, new Vector3(0.9f, 3.5f, 0.9f), color, scale);
            body.transform.localPosition = new Vector3(0, scale * 3.5f, 0);
            var cap = MakeSlab("Cap", root.transform, new Vector3(1.1f, 0.15f, 1.1f), new Color(color.r * 0.9f, color.g * 0.9f, color.b * 0.9f), scale);
            cap.transform.localPosition = new Vector3(0, scale * 7.0f, 0);
            return root;
        }

        private static GameObject MakeArch(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var lp = MakeSlab("LeftPillar",  root.transform, new Vector3(0.7f, 3f, 0.7f), color, scale);
            lp.transform.localPosition = new Vector3(-scale * 2.5f, scale * 3f, 0);
            var rp = MakeSlab("RightPillar", root.transform, new Vector3(0.7f, 3f, 0.7f), color, scale);
            rp.transform.localPosition = new Vector3( scale * 2.5f, scale * 3f, 0);
            var sp = MakeSlab("Span",        root.transform, new Vector3(5.5f, 0.5f, 0.7f), color, scale);
            sp.transform.localPosition = new Vector3(0, scale * 6.2f, 0);
            return root;
        }

        private static GameObject MakeBridge(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var deck = MakeSlab("Deck", root.transform, new Vector3(3.5f, 0.2f, 0.8f), color, scale);
            deck.AddComponent<BoxCollider>();
            deck.transform.localPosition = new Vector3(0, 0.1f * scale, 0);
            var rail1 = MakeSlab("Rail_L", root.transform, new Vector3(3.5f, 0.3f, 0.06f), new Color(color.r*0.85f, color.g*0.85f, color.b*0.85f), scale);
            rail1.transform.localPosition = new Vector3(0, 0.35f * scale, -0.4f * scale);
            var rail2 = MakeSlab("Rail_R", root.transform, new Vector3(3.5f, 0.3f, 0.06f), new Color(color.r*0.85f, color.g*0.85f, color.b*0.85f), scale);
            rail2.transform.localPosition = new Vector3(0, 0.35f * scale, 0.4f * scale);
            return root;
        }

        private static GameObject MakeCaveEntrance(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var left  = MakeSlab("CaveL", root.transform, new Vector3(0.5f, 1.4f, 0.4f), color, scale);
            left.transform.localPosition = new Vector3(-scale * 1.2f, scale * 1.4f, 0);
            var right = MakeSlab("CaveR", root.transform, new Vector3(0.5f, 1.4f, 0.4f), color, scale);
            right.transform.localPosition = new Vector3( scale * 1.2f, scale * 1.4f, 0);
            var top   = MakeSlab("CaveTop", root.transform, new Vector3(2.8f, 0.5f, 0.4f), color, scale);
            top.transform.localPosition = new Vector3(0, scale * 2.9f, 0);
            var dark  = MakeSlab("CaveDark", root.transform, new Vector3(2.0f, 2.5f, 0.1f), new Color(0.05f, 0.04f, 0.04f), scale);
            dark.transform.localPosition = new Vector3(0, scale * 1.4f, 0.1f);
            var rubble = MakeSphere("Rubble", root.transform, new Vector3(1.5f, 0.3f, 1.8f), color);
            rubble.transform.localPosition = new Vector3(0, 0.15f, 0.3f * scale);
            return root;
        }

        private static GameObject MakeRockCluster(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Vector3[] offsets = { Vector3.zero, new Vector3(0.6f,0,0.4f), new Vector3(-0.5f,0,0.3f), new Vector3(0.2f,0,-0.5f), new Vector3(-0.3f,0,-0.4f) };
            Vector3[] scales  = { new Vector3(1.2f,0.8f,1.1f), new Vector3(0.6f,0.4f,0.7f), new Vector3(0.8f,0.5f,0.8f), new Vector3(0.4f,0.3f,0.4f), new Vector3(0.5f,0.35f,0.5f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                var r = MakeSphere($"Rock_{i}", root.transform, scales[i], color, scale);
                r.transform.localPosition = offsets[i] * scale;
                r.AddComponent<BoxCollider>();
            }
            return root;
        }

        private static GameObject MakeRuinedTower(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            float[] heights = { 0f, 2.2f, 4.2f, 6f };
            float[] widths  = { 2.4f, 2.2f, 2.0f, 1.8f };
            for (int i = 0; i < heights.Length; i++)
            {
                var s = MakeSlab($"Ring{i}", root.transform, new Vector3(widths[i], 2.0f, widths[i]), color, scale);
                s.transform.localPosition = new Vector3(0, (heights[i] + 1.0f) * scale, 0);
            }
            var broken = MakeSlab("Broken", root.transform, new Vector3(1.0f, 1.2f, 1.6f), new Color(color.r*0.8f, color.g*0.8f, color.b*0.8f), scale);
            broken.transform.localPosition = new Vector3(0.5f * scale, 8.5f * scale, 0);
            broken.transform.localRotation = Quaternion.Euler(0, 0, 15f);
            return root;
        }

        private static GameObject MakeStatue(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var base1 = MakeSlab("Base",  root.transform, new Vector3(0.8f, 0.25f, 0.5f), color, scale);
            base1.transform.localPosition = new Vector3(0, 0.125f * scale, 0);
            var body  = MakeCylinder("Body", root.transform, new Vector3(0.35f, 1.2f, 0.35f), color, scale);
            body.transform.localPosition = new Vector3(0, 1.5f * scale, 0);
            var head  = MakeSphere("Head", root.transform, new Vector3(0.4f, 0.4f, 0.4f), new Color(color.r * 0.95f, color.g * 0.95f, color.b * 0.95f), scale);
            head.transform.localPosition = new Vector3(0, 2.8f * scale, 0);
            var arm = MakeSlab("BrokenArm", root.transform, new Vector3(0.1f, 0.5f, 0.1f), color, scale);
            arm.transform.localPosition = new Vector3(0.35f * scale, 2.0f * scale, 0);
            arm.transform.localRotation = Quaternion.Euler(0, 0, -40f);
            return root;
        }

        private static GameObject MakeSteps(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            for (int i = 0; i < 3; i++)
            {
                var s = MakeSlab($"Step{i}", root.transform, new Vector3(1.4f - i*0.2f, 0.15f, 0.6f), color, scale);
                s.transform.localPosition = new Vector3(0, (0.15f + i * 0.15f) * scale, -i * 0.4f * scale);
            }
            return root;
        }

        private static GameObject MakeCampfire(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var pit  = MakeCylinder("Pit",  root.transform, new Vector3(0.55f, 0.08f, 0.55f), new Color(0.22f,0.17f,0.10f));
            pit.transform.localPosition = new Vector3(0, 0.04f, 0);
            var logs = MakeSlab("Logs",     root.transform, new Vector3(0.7f, 0.12f, 0.18f), new Color(0.30f,0.18f,0.08f));
            logs.transform.localPosition = new Vector3(0, 0.12f, 0);
            var fire = MakeSphere("Flame",  root.transform, new Vector3(0.3f, 0.4f, 0.3f), new Color(1.0f, 0.55f, 0.05f));
            fire.transform.localPosition = new Vector3(0, 0.3f, 0);
            var glow = MakeSphere("Glow",   root.transform, new Vector3(0.5f, 0.3f, 0.5f), new Color(1.0f, 0.85f, 0.3f));
            glow.transform.localPosition = new Vector3(0, 0.22f, 0);
            return root;
        }

        private static GameObject MakeTent(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = MakeSlab("TentBody", root.transform, new Vector3(1.4f, 0.9f, 1.0f), color, scale);
            body.transform.localPosition = new Vector3(0, 0.45f * scale, 0);
            body.transform.localRotation = Quaternion.Euler(0, 0, -20f);
            var entryFlap = MakeSlab("Flap", root.transform, new Vector3(0.5f, 0.7f, 0.05f), new Color(color.r*0.8f, color.g*0.8f, color.b*0.8f), scale);
            entryFlap.transform.localPosition = new Vector3(0, 0.35f * scale, -0.52f * scale);
            return root;
        }

        private static GameObject MakeLantern(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var post = MakeCylinder("Post", root.transform, new Vector3(0.08f, 1.2f, 0.08f), new Color(0.20f, 0.18f, 0.15f));
            post.transform.localPosition = new Vector3(0, 1.2f, 0);
            var lamp = MakeSlab("Lamp",     root.transform, new Vector3(0.22f, 0.22f, 0.22f), new Color(1.0f, 0.88f, 0.22f));
            lamp.transform.localPosition = new Vector3(0, 2.3f, 0);
            var glow = MakeSphere("GlowSphere", root.transform, new Vector3(0.32f, 0.32f, 0.32f), new Color(1.0f, 0.95f, 0.5f));
            glow.transform.localPosition = new Vector3(0, 2.3f, 0);
            return root;
        }

        private static GameObject MakeShrine(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var base1 = MakeSlab("ShrineBase",   root.transform, new Vector3(0.9f, 0.2f, 0.9f), color, scale);
            base1.transform.localPosition = new Vector3(0, 0.1f * scale, 0);
            var pillar = MakeCylinder("ShrinePillar", root.transform, new Vector3(0.2f, 0.9f, 0.2f), color, scale);
            pillar.transform.localPosition = new Vector3(0, 0.7f * scale, 0);
            var top   = MakeSlab("ShrineTop",    root.transform, new Vector3(0.7f, 0.15f, 0.7f), color, scale);
            top.transform.localPosition = new Vector3(0, 1.3f * scale, 0);
            var orb   = MakeSphere("ShrineOrb",  root.transform, new Vector3(0.2f, 0.2f, 0.2f), new Color(0.55f, 0.45f, 0.85f), scale);
            orb.transform.localPosition = new Vector3(0, 1.55f * scale, 0);
            return root;
        }

        private static GameObject MakeAltar(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var slab = MakeSlab("AltarSlab",   root.transform, new Vector3(1.8f, 0.5f, 0.8f), color, scale);
            slab.transform.localPosition = new Vector3(0, 0.25f * scale, 0);
            for (int i = 0; i < 2; i++)
            {
                var leg = MakeSlab($"Leg{i}", root.transform, new Vector3(0.3f, 0.6f, 0.4f),
                    new Color(color.r * 0.85f, color.g * 0.85f, color.b * 0.85f), scale);
                leg.transform.localPosition = new Vector3((i == 0 ? -0.65f : 0.65f) * scale, 0.3f * scale, 0);
            }
            return root;
        }

        private static GameObject MakeCrystal(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = MakeSlab("CrystalBody", root.transform, new Vector3(0.25f, 0.9f, 0.25f), color, scale);
            body.transform.localPosition = new Vector3(0, 0.45f * scale, 0);
            body.transform.localRotation = Quaternion.Euler(10f, 35f, 5f);
            var tip = MakeSphere("CrystalTip", root.transform, new Vector3(0.2f, 0.2f, 0.2f), new Color(Mathf.Min(color.r + 0.2f, 1f), Mathf.Min(color.g + 0.2f, 1f), Mathf.Min(color.b + 0.2f, 1f)), scale);
            tip.transform.localPosition = new Vector3(0.03f * scale, 0.95f * scale, 0);
            return root;
        }

        private static GameObject MakeCrystalFormation(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Vector3[] offsets = { Vector3.zero, new Vector3(0.5f,0,0.3f), new Vector3(-0.4f,0,0.45f), new Vector3(0.2f,0,-0.5f) };
            float[] heights   = { 1.0f, 0.7f, 0.85f, 0.6f };
            for (int i = 0; i < offsets.Length; i++)
            {
                var c = MakeCrystal($"Cryst{i}", root.transform, color, scale * heights[i]);
                c.transform.localPosition = offsets[i] * scale;
            }
            return root;
        }

        private static GameObject MakeWaterfall(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            float[] ySteps = { 0f, -1.0f, -2.2f, -3.5f };
            float[] xOff   = { 0f, 0.1f, -0.1f, 0.05f };
            for (int i = 0; i < ySteps.Length; i++)
            {
                var slab = MakeSlab($"Fall{i}", root.transform, new Vector3(0.6f, 1.0f, 0.1f), color, scale);
                slab.transform.localPosition = new Vector3(xOff[i] * scale, ySteps[i] * scale, 0);
            }
            var mist = MakeSphere("Mist", root.transform, new Vector3(1.0f, 0.4f, 1.0f), new Color(color.r, color.g, color.b, 0.5f), scale);
            mist.transform.localPosition = new Vector3(0, -3.8f * scale, 0.2f * scale);
            var pool = MakeSphere("Pool", root.transform, new Vector3(1.2f, 0.06f, 1.2f), new Color(color.r*0.8f, color.g*0.85f, color.b), scale);
            pool.transform.localPosition = new Vector3(0, -4.5f * scale, 0.3f * scale);
            return root;
        }

        private static GameObject MakeBossTree(string name, Transform parent, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var trunk = MakeCylinder("BossTrunk", root.transform, new Vector3(2.5f, 10f, 2.5f), new Color(0.18f, 0.10f, 0.08f), scale);
            trunk.transform.localPosition = new Vector3(0, scale * 10f, 0);
            var trunk2 = MakeCylinder("BossTrunk2", root.transform, new Vector3(1.8f, 7f, 1.8f), new Color(0.15f, 0.08f, 0.06f), scale);
            trunk2.transform.localPosition = new Vector3(1.5f * scale, scale * 7f, 0.5f * scale);
            var canopy = MakeSphere("BossCanopy", root.transform, new Vector3(7.5f, 2.2f, 7.5f), new Color(0.22f, 0.10f, 0.12f), scale);
            canopy.transform.localPosition = new Vector3(0, scale * 22f, 0);
            var branch = MakeCylinder("BossBranch", root.transform, new Vector3(0.5f, 3f, 0.5f), new Color(0.15f, 0.08f, 0.07f), scale);
            branch.transform.localPosition = new Vector3(-2.5f * scale, scale * 18f, 0);
            branch.transform.localRotation = Quaternion.Euler(0, 0, 30f);
            return root;
        }

        private static GameObject MakeKingNPC(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var robe = MakeSlab("Robe", root.transform, new Vector3(0.5f, 0.9f, 0.4f), new Color(0.55f, 0.12f, 0.12f));
            robe.transform.localPosition = new Vector3(0, 0.45f, 0);
            var body = MakeCylinder("KingBody", root.transform, new Vector3(0.45f, 0.6f, 0.45f), new Color(0.85f, 0.70f, 0.18f));
            body.transform.localPosition = new Vector3(0, 1.0f, 0);
            var head = MakeSphere("KingHead", root.transform, new Vector3(0.35f, 0.35f, 0.35f), new Color(0.88f, 0.72f, 0.55f));
            head.transform.localPosition = new Vector3(0, 1.55f, 0);
            var crown = MakeCylinder("Crown", root.transform, new Vector3(0.25f, 0.12f, 0.25f), new Color(1.0f, 0.85f, 0.0f));
            crown.transform.localPosition = new Vector3(0, 1.82f, 0);
            return root;
        }

        private static GameObject MakeExitGate(string name, Transform parent, float scale = 1f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            var pL = MakeSlab("Pillar_L", root.transform, new Vector3(0.8f, 3.5f, 0.8f), new Color(0.42f, 0.40f, 0.38f), scale);
            pL.transform.localPosition = new Vector3(-3.5f * scale, 1.75f * scale, 0);
            var pR = MakeSlab("Pillar_R", root.transform, new Vector3(0.8f, 3.5f, 0.8f), new Color(0.42f, 0.40f, 0.38f), scale);
            pR.transform.localPosition = new Vector3(3.5f * scale, 1.75f * scale, 0);

            var span = MakeSlab("SpanBeam", root.transform, new Vector3(7.8f, 0.6f, 0.9f), new Color(0.48f, 0.45f, 0.42f), scale);
            span.transform.localPosition = new Vector3(0, 3.8f * scale, 0);

            var barrier = MakeSlab("GateBarrier", root.transform, new Vector3(6.2f, 3.2f, 0.1f), new Color(0.75f, 0.25f, 0.2f, 0.7f), scale);
            barrier.transform.localPosition = new Vector3(0, 1.6f * scale, 0);

            return root;
        }

        private static GameObject MakeHorsePlaceholder(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = MakeSlab("HorseBody", root.transform, new Vector3(0.8f, 0.6f, 1.4f), new Color(0.55f, 0.40f, 0.25f));
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            var neck = MakeCylinder("HorseNeck", root.transform, new Vector3(0.28f, 0.55f, 0.28f), new Color(0.52f, 0.38f, 0.22f));
            neck.transform.localPosition = new Vector3(0, 1.3f, 0.55f);
            neck.transform.localRotation = Quaternion.Euler(35f, 0, 0);
            var head2 = MakeSphere("HorseHead", root.transform, new Vector3(0.35f, 0.3f, 0.45f), new Color(0.50f, 0.36f, 0.20f));
            head2.transform.localPosition = new Vector3(0, 1.75f, 0.9f);
            for (int i = 0; i < 4; i++)
            {
                float lx = (i % 2 == 0) ? -0.28f : 0.28f;
                float lz = (i < 2) ? 0.5f : -0.5f;
                var leg = MakeCylinder($"Leg{i}", root.transform, new Vector3(0.12f, 0.5f, 0.12f), new Color(0.48f, 0.34f, 0.18f));
                leg.transform.localPosition = new Vector3(lx, 0.25f, lz);
            }
            return root;
        }

        private static GameObject MakeAqueductArch(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var lp = MakeSlab("AqueductLeft", root.transform, new Vector3(0.8f, 4.5f, 0.8f), color, scale);
            lp.transform.localPosition = new Vector3(-scale * 3f, scale * 4.5f, 0);
            var rp = MakeSlab("AqueductRight", root.transform, new Vector3(0.8f, 4.5f, 0.8f), color, scale);
            rp.transform.localPosition = new Vector3(scale * 3f, scale * 4.5f, 0);
            var channel = MakeSlab("Channel", root.transform, new Vector3(7.5f, 0.5f, 0.9f), color, scale);
            channel.transform.localPosition = new Vector3(0, scale * 9.2f, 0);
            var arch = MakeSphere("Arch", root.transform, new Vector3(5.8f, 1.2f, 0.7f), new Color(color.r*0.85f, color.g*0.85f, color.b*0.85f), scale);
            arch.transform.localPosition = new Vector3(0, scale * 8.4f, 0);
            var rubble = MakeSphere("Rubble", root.transform, new Vector3(1.8f, 0.4f, 1.5f), color);
            rubble.transform.localPosition = new Vector3(2.5f * scale, 0.2f, 0.8f * scale);
            return root;
        }

        private static GameObject MakeWatchtower(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var base1 = MakeSlab("TowerBase", root.transform, new Vector3(2.0f, 0.4f, 2.0f), color, scale);
            base1.transform.localPosition = new Vector3(0, 0.2f * scale, 0);
            float[] segHeights = { 0.4f, 3.0f, 5.8f, 8.2f };
            float[] segWidths  = { 1.8f, 1.6f, 1.5f, 1.4f };
            for (int i = 0; i < segHeights.Length; i++)
            {
                var seg = MakeSlab($"Seg{i}", root.transform, new Vector3(segWidths[i], 2.6f, segWidths[i]), color, scale);
                seg.transform.localPosition = new Vector3(0f, (segHeights[i] + 1.3f) * scale, 0f);
            }
            var top = MakeSlab("Battlement", root.transform, new Vector3(2.0f, 0.6f, 2.0f), new Color(color.r*0.85f, color.g*0.85f, color.b*0.85f), scale);
            top.transform.localPosition = new Vector3(0.2f * scale, 11.5f * scale, 0.1f * scale);
            top.transform.localRotation = Quaternion.Euler(0, 12f, 4f);
            return root;
        }

        private static GameObject MakeRiverSegment(string name, Transform parent, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var water = MakeSlab("Water", root.transform, new Vector3(1f, 0.01f, 1f), new Color(0.18f, 0.38f, 0.72f), scale);
            water.transform.localPosition = Vector3.zero;
            water.AddComponent<BoxCollider>().isTrigger = true;
            return root;
        }

        private static GameObject MakeAbandonedCamp(string name, Transform parent, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var tent = MakeTent("Tent", root.transform, new Color(0.45f, 0.32f, 0.18f), scale);
            tent.transform.localPosition = new Vector3(-1.5f * scale, 0, 0);
            var fire = MakeCampfire("ColdFire", root.transform);
            fire.transform.localPosition = new Vector3(0.8f * scale, 0, 0);
            var chest = MakeSlab("AbandonedChest", root.transform, new Vector3(0.6f, 0.45f, 0.4f), new Color(0.40f, 0.28f, 0.10f), scale);
            chest.transform.localPosition = new Vector3(2.0f * scale, 0.22f * scale, 0.5f * scale);
            chest.transform.localRotation = Quaternion.Euler(0, 25f, 0);
            var log = MakeCylinder("LogSeat", root.transform, new Vector3(0.35f, 1.8f, 0.35f), new Color(0.28f, 0.18f, 0.10f));
            log.transform.localPosition = new Vector3(-0.3f * scale, 0.2f, -1.2f * scale);
            log.transform.localRotation = Quaternion.Euler(0, 80f, 90f);
            return root;
        }

        private static GameObject MakeLoreSign(string name, Transform parent, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var post = MakeCylinder("Post", root.transform, new Vector3(0.1f, 0.9f, 0.1f), new Color(0.32f, 0.22f, 0.12f), scale);
            post.transform.localPosition = new Vector3(0, 0.9f * scale, 0);
            var board = MakeSlab("Board", root.transform, new Vector3(0.7f, 0.45f, 0.06f), new Color(0.38f, 0.27f, 0.14f), scale);
            board.transform.localPosition = new Vector3(0, 1.8f * scale, 0);
            board.transform.localRotation = Quaternion.Euler(0, 0, -8f);
            return root;
        }

        private static GameObject MakeWagon(string name, Transform parent, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = MakeSlab("WagonBody", root.transform, new Vector3(1.6f, 0.4f, 2.5f), new Color(0.35f, 0.24f, 0.12f), scale);
            body.transform.localPosition = new Vector3(0, 0.4f * scale, 0);
            body.transform.localRotation = Quaternion.Euler(12f, 0, -8f);
            var wheel = MakeCylinder("BrokenWheel", root.transform, new Vector3(0.8f, 0.1f, 0.8f), new Color(0.25f, 0.18f, 0.10f), scale);
            wheel.transform.localPosition = new Vector3(1.0f * scale, 0.3f * scale, 0.8f * scale);
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 75f);
            var barrel = MakeCylinder("Barrel", root.transform, new Vector3(0.5f, 0.7f, 0.5f), new Color(0.30f, 0.20f, 0.10f), scale);
            barrel.transform.localPosition = new Vector3(-0.8f * scale, 0.35f * scale, -1.2f * scale);
            return root;
        }

        private static GameObject MakeObelisk(string name, Transform parent, Color color, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var baseSlab = MakeSlab("ObeliskBase", root.transform, new Vector3(2.2f, 0.8f, 2.2f), color, scale);
            baseSlab.transform.localPosition = new Vector3(0, 0.4f * scale, 0);
            var shaft = MakeCylinder("Shaft", root.transform, new Vector3(1.2f, 6.0f, 1.2f), color, scale);
            shaft.transform.localPosition = new Vector3(0, 6.8f * scale, 0);
            var runeCap = MakeSphere("RuneOrb", root.transform, new Vector3(0.8f, 0.8f, 0.8f), new Color(0.3f, 0.7f, 1.0f), scale);
            runeCap.transform.localPosition = new Vector3(0, 13.2f * scale, 0);
            return root;
        }

        // ── Robust Built-In Compatible Material Applicator ────────────────────
        private static void Apply(GameObject g, Color color)
        {
            if (!g.TryGetComponent<Renderer>(out var r)) return;

            // Apply vertex color to mesh filter if primitive
            if (g.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
            {
                Mesh m = Object.Instantiate(mf.sharedMesh);
                Color[] cols = new Color[m.vertexCount];
                for (int i = 0; i < cols.Length; i++) cols[i] = color;
                m.colors = cols;
                mf.sharedMesh = m;
            }

            int key = color.GetHashCode();
            if (!_matCache.TryGetValue(key, out Material mat) || mat == null)
            {
                Shader s = Shader.Find("Standard")
                        ?? Shader.Find("Mobile/Diffuse")
                        ?? Shader.Find("Legacy Shaders/Diffuse")
                        ?? Shader.Find("Unlit/Color");

                if (s == null)
                {
                    Debug.LogError($"[WorldPlaceholderFactory] ERROR: Could not find valid 3D opaque shader for color {color}!");
                    return;
                }

                mat = new Material(s);
                mat.name = $"PH_Mat_{color.r:F2}_{color.g:F2}_{color.b:F2}";

                // Enforce 100% Opaque Geometry Queue & ZWrite ON
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry; // 2000
                mat.SetOverrideTag("RenderType", "Opaque");

                if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 1);
                if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);

                Color opaqueColor = new Color(color.r, color.g, color.b, 1.0f);
                mat.color = opaqueColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", opaqueColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", opaqueColor);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);

                _matCache[key] = mat;
            }

            r.sharedMaterial = mat;
        }

        /// Remove the primitive's default collider (caller adds their own if needed).
        private static void Strip(GameObject g)
        {
            if (g.TryGetComponent<Collider>(out var col)) Object.DestroyImmediate(col);
        }
    }
}
