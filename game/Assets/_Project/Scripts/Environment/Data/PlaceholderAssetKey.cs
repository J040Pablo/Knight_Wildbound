namespace Roguelite.Environment
{
    /// <summary>
    /// Canonical identifier for every placeholderable asset.
    /// When real assets are ready, WorldPlaceholderFactory maps each key
    /// to a prefab/model — SceneEnvironmentBuilder never needs to change.
    /// </summary>
    public enum PlaceholderAssetKey
    {
        // ── Terrain ──────────────────────────────
        GroundTile,
        HillCap,
        CliffFace,
        CliffPillar,
        WaterPlane,
        WaterWaterfall,
        RiverSlab,
        RiverBank,
        TerrainHillSlab,
        LakeWater,
        LakeIsland,
        RockShelf,

        // ── Vegetation ────────────────────────────
        TreeDeciduous,
        TreePine,
        TreeWillow,
        WillowTree = TreeWillow,
        TreeAncient,
        TreeDeadSmall,
        TreeDeadGiant,
        BushSmall,
        BushLarge,
        FlowerCluster,
        GrassClump,
        MushroomSingle,
        MushroomGroup,
        FallenLog,
        HeroTree,

        // ── Rocks ─────────────────────────────────
        RockPebble,
        RockMedium,
        RockBoulder,
        RockPillarGiant,
        RockArch,
        RockCliffWall,
        RockCaveEntrance,
        RockClusterGroup,

        // ── Ruins / Structures ────────────────────
        RuinWallSegment,
        RuinPillar,
        RuinTowerLandmark,
        RuinGate,
        RuinStatue,
        RuinAqueductArch,
        RuinWatchtower,
        WoodenBridge,
        StoneBridge,
        StoneSteps,

        // ── Props ─────────────────────────────────
        Campfire,
        CampTent,
        LanternPost,
        Chest,
        WeaponPedestal,
        WeaponSword,
        WeaponStaff,
        WeaponBranch,
        ForgottenShrine,
        AncientAltar,
        GlowingCrystal,
        Pond,
        AbandonedCamp,
        LoreSignPost,
        DestroyedWagon,
        FairyHouseRoot,

        // ── NPCs / Entities ───────────────────────
        KingNPC,
        FriendlyHorse,

        // ── Landmarks ─────────────────────────────
        LandmarkWaterfall,
        LandmarkGiantAncestralTree,
        LandmarkCrystalFormation,
        LandmarkBossHollowTree,
        LandmarkStoneArch,
        LandmarkVistaPoint,
        LandmarkRuinedTower,
        LandmarkGiantObelisk,

        // ── Gameplay Volumes ──────────────────────
        ExitGate,

        // ── River ─────────────────────────────────
        RiverSegment,

        // ── Forest Density & Biome Exit ───────────
        TreeStump,
        Fern,
        RootEmerging,
        MossStone,
        CorruptedRootBarrier,
        TransitionGateSign,
    }
}
