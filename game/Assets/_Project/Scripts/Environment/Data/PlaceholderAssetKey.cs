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
    }
}
