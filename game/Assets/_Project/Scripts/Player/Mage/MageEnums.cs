namespace Roguelite.Player.Mage
{
    public enum MageBuildType
    {
        Elemental = 0,
        Warlock = 1,
        Cosmic = 2
    }

    public enum MageTier
    {
        N1 = 0,
        N2 = 1,
        N3 = 2
    }

    public enum MageSpellId
    {
        None = 0,
        
        // Elemental (N1 Ice -> N2 Fire -> N3 Electricity)
        IceShard,
        FrostWave,
        FireSpark,
        Fireball,
        LightningBolt,
        LightningStrike,
        AbsoluteFreeze,

        // Warlock
        DarkOrb,
        ShadowChain,
        CurseMark,
        HeavyCurse,
        SpectralHand,
        ShadowArmy,

        // Cosmic
        Star,
        Supernova,
        SpaceFragment,
        Portal,
        CosmicRay,
        CosmicCollapse
    }
}
