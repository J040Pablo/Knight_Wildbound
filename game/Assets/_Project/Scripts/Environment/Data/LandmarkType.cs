namespace Roguelite.Environment
{
    /// <summary>
    /// Semantic category of a landmark in the world.
    /// Used by LandmarkMarker to describe what the landmark IS,
    /// independently of how it is visualized.
    /// </summary>
    public enum LandmarkType
    {
        // Ruins zone
        RuinedTower,
        RuinedStatue,

        // Forest zones
        GiantDeadTree,
        AncestralTree,
        GiantWorldTree,

        // River zone
        WaterfallCascade,
        StoneBridgeArch,

        // Rocky zone
        StoneArchFormation,
        CaveEntrance,
        VistaViewpoint,

        // Ancient grove
        GlowingCrystalFormation,
        AncientAltar,
        SecretPond,

        // Boss zone
        BossHollowTree,

        // Deep Forest mini-boss zone
        ToxicGrove,

        // Stone Biome storytelling & boss zones
        GiantSkeleton,
        AncientRuins,
        BrokenTitanStatue,
        StoneTitanArena,
    }
}
