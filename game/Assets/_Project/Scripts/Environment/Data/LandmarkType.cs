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
    }
}
