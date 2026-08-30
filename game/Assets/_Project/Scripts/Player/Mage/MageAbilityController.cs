using System.Collections.Generic;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Progression;
using Roguelite.Player.Mage.Spells.Fire;
using Roguelite.Player.Mage.Spells.Lightning;
using Roguelite.Player.Mage.Spells.Ice;
using Roguelite.Player.Mage.Spells.Warlock;
using Roguelite.Player.Mage.Spells.Cosmic;

namespace Roguelite.Player.Mage
{
    public class MageAbilityController : MonoBehaviour
    {
        private PlayerCombat playerCombat;
        private PlayerStats playerStats;

        private readonly Dictionary<MageSpellId, MageSpell> spellRegistry = new Dictionary<MageSpellId, MageSpell>();

        [Header("Default Build Definitions")]
        [SerializeField] private MageBuildDefinition elementalBuild;
        [SerializeField] private MageBuildDefinition warlockBuild;
        [SerializeField] private MageBuildDefinition cosmicBuild;

        public MageBuildType ActiveBuild { get; private set; } = MageBuildType.Elemental;
        public MageTier ActiveTier { get; private set; } = MageTier.N1;

        private GameObject chargeVFXInstance;

        public void Initialize(PlayerCombat combat, PlayerStats stats)
        {
            playerCombat = combat;
            playerStats = stats;

            BuildDefaultDefinitionsIfMissing();
            RegisterAllSpellBehaviors();
            SyncWithProgression();
        }

        private void BuildDefaultDefinitionsIfMissing()
        {
            if (elementalBuild == null)
            {
                elementalBuild = ScriptableObject.CreateInstance<MageBuildDefinition>();
                elementalBuild.buildType = MageBuildType.Elemental;
                elementalBuild.buildName = "Elemental Mage";
                elementalBuild.themeColor = new Color(0.3f, 0.85f, 1.0f);

                // N1 — ICE
                elementalBuild.n1Basic = CreateAbilityDef(MageSpellId.IceShard, "Ice Shards", MageBuildType.Elemental, MageTier.N1, false, 1.2f, 0.35f, 20f, 1.0f, new Color(0.3f, 0.85f, 1.0f));
                elementalBuild.n1Charged = CreateAbilityDef(MageSpellId.FrostWave, "Frost Wave", MageBuildType.Elemental, MageTier.N1, true, 2.0f, 0.8f, 0f, 4.2f, new Color(0.5f, 0.95f, 1.0f));

                // N2 — FIRE
                elementalBuild.n2Basic = CreateAbilityDef(MageSpellId.FireSpark, "Fire Spark", MageBuildType.Elemental, MageTier.N2, false, 1.5f, 0.35f, 26f, 1.4f, new Color(1.0f, 0.45f, 0.1f));
                elementalBuild.n2Charged = CreateAbilityDef(MageSpellId.Fireball, "Fireball", MageBuildType.Elemental, MageTier.N2, true, 2.5f, 1.0f, 18f, 4.5f, new Color(1.0f, 0.25f, 0.05f));

                // N3 — ELECTRICITY
                elementalBuild.n3Basic = CreateAbilityDef(MageSpellId.LightningBolt, "Lightning Bolt", MageBuildType.Elemental, MageTier.N3, false, 1.8f, 0.3f, 45f, 0.5f, new Color(0.9f, 0.95f, 0.2f));
                elementalBuild.n3Charged = CreateAbilityDef(MageSpellId.LightningStrike, "Lightning Strike", MageBuildType.Elemental, MageTier.N3, true, 3.2f, 1.4f, 0f, 4.5f, new Color(0.85f, 0.9f, 1.0f));
            }

            if (warlockBuild == null)
            {
                warlockBuild = ScriptableObject.CreateInstance<MageBuildDefinition>();
                warlockBuild.buildType = MageBuildType.Warlock;
                warlockBuild.buildName = "Warlock";
                warlockBuild.themeColor = new Color(0.55f, 0.15f, 0.75f);

                warlockBuild.n1Basic = CreateAbilityDef(MageSpellId.DarkOrb, "Dark Orb", MageBuildType.Warlock, MageTier.N1, false, 1.3f, 0.4f, 22f, 1.8f, new Color(0.5f, 0.1f, 0.65f));
                warlockBuild.n1Charged = CreateAbilityDef(MageSpellId.ShadowChain, "Shadow Chain", MageBuildType.Warlock, MageTier.N1, true, 2.4f, 1.0f, 30f, 0.5f, new Color(0.4f, 0.05f, 0.55f));

                warlockBuild.n2Basic = CreateAbilityDef(MageSpellId.CurseMark, "Curse Mark", MageBuildType.Warlock, MageTier.N2, false, 1.5f, 0.45f, 24f, 1.0f, new Color(0.3f, 0.0f, 0.4f));
                warlockBuild.n2Charged = CreateAbilityDef(MageSpellId.HeavyCurse, "Heavy Curse", MageBuildType.Warlock, MageTier.N2, true, 2.6f, 1.4f, 0f, 5.0f, new Color(0.45f, 0.05f, 0.5f));

                warlockBuild.n3Basic = CreateAbilityDef(MageSpellId.SpectralHand, "Spectral Hand", MageBuildType.Warlock, MageTier.N3, false, 1.8f, 0.5f, 0f, 3.5f, new Color(0.2f, 0.8f, 0.6f));
                warlockBuild.n3Charged = CreateAbilityDef(MageSpellId.ShadowArmy, "Shadow Army", MageBuildType.Warlock, MageTier.N3, true, 3.0f, 2.0f, 0f, 4.0f, new Color(0.35f, 0.1f, 0.45f));
            }

            if (cosmicBuild == null)
            {
                cosmicBuild = ScriptableObject.CreateInstance<MageBuildDefinition>();
                cosmicBuild.buildType = MageBuildType.Cosmic;
                cosmicBuild.buildName = "Cosmic Sorcerer";
                cosmicBuild.themeColor = new Color(0.2f, 0.5f, 1.0f);

                cosmicBuild.n1Basic = CreateAbilityDef(MageSpellId.Star, "Star", MageBuildType.Cosmic, MageTier.N1, false, 1.3f, 0.35f, 24f, 1.0f, new Color(0.4f, 0.7f, 1.0f));
                cosmicBuild.n1Charged = CreateAbilityDef(MageSpellId.Supernova, "Supernova", MageBuildType.Cosmic, MageTier.N1, true, 2.5f, 1.0f, 20f, 4.5f, new Color(0.7f, 0.4f, 1.0f));

                cosmicBuild.n2Basic = CreateAbilityDef(MageSpellId.SpaceFragment, "Space Fragment", MageBuildType.Cosmic, MageTier.N2, false, 1.6f, 0.4f, 30f, 1.0f, new Color(0.5f, 0.8f, 1.0f));
                cosmicBuild.n2Charged = CreateAbilityDef(MageSpellId.Portal, "Portal", MageBuildType.Cosmic, MageTier.N2, true, 2.8f, 1.3f, 0f, 3.8f, new Color(0.3f, 0.3f, 0.95f));

                cosmicBuild.n3Basic = CreateAbilityDef(MageSpellId.CosmicRay, "Cosmic Ray", MageBuildType.Cosmic, MageTier.N3, false, 2.0f, 0.8f, 0f, 1.2f, new Color(0.6f, 0.5f, 1.0f));
                cosmicBuild.n3Charged = CreateAbilityDef(MageSpellId.CosmicCollapse, "Cosmic Collapse", MageBuildType.Cosmic, MageTier.N3, true, 3.5f, 2.5f, 0f, 6.0f, new Color(0.1f, 0.1f, 0.3f));
            }
        }

        private MageAbilityDefinition CreateAbilityDef(MageSpellId id, string name, MageBuildType build, MageTier tier, bool charged, float dmgMult, float cd, float projSpeed, float area, Color color)
        {
            var def = ScriptableObject.CreateInstance<MageAbilityDefinition>();
            def.spellId = id;
            def.spellName = name;
            def.buildType = build;
            def.tier = tier;
            def.isCharged = charged;
            def.damageMultiplier = dmgMult;
            def.cooldown = cd;
            def.projectileSpeed = projSpeed;
            def.areaRadius = area;
            def.primaryColor = color;
            def.secondaryColor = Color.white;
            return def;
        }

        private void RegisterAllSpellBehaviors()
        {
            RegisterSpell(elementalBuild.n1Basic, new IceShardSpell());
            RegisterSpell(elementalBuild.n1Charged, new FrostWaveSpell());
            RegisterSpell(elementalBuild.n2Basic, new FireSparkSpell());
            RegisterSpell(elementalBuild.n2Charged, new FireballSpell());
            RegisterSpell(elementalBuild.n3Basic, new LightningBoltSpell());
            RegisterSpell(elementalBuild.n3Charged, new LightningStrikeSpell());

            RegisterSpell(warlockBuild.n1Basic, new DarkOrbSpell());
            RegisterSpell(warlockBuild.n1Charged, new ShadowChainSpell());
            RegisterSpell(warlockBuild.n2Basic, new CurseMarkSpell());
            RegisterSpell(warlockBuild.n2Charged, new HeavyCurseSpell());
            RegisterSpell(warlockBuild.n3Basic, new SpectralHandSpell());
            RegisterSpell(warlockBuild.n3Charged, new ShadowArmySpell());

            RegisterSpell(cosmicBuild.n1Basic, new StarSpell());
            RegisterSpell(cosmicBuild.n1Charged, new SupernovaSpell());
            RegisterSpell(cosmicBuild.n2Basic, new SpaceFragmentSpell());
            RegisterSpell(cosmicBuild.n2Charged, new PortalSpell());
            RegisterSpell(cosmicBuild.n3Basic, new CosmicRaySpell());
            RegisterSpell(cosmicBuild.n3Charged, new CosmicCollapseSpell());
        }

        private void RegisterSpell(MageAbilityDefinition def, MageSpell spellInstance)
        {
            if (def == null || spellInstance == null) return;
            spellInstance.Initialize(def, playerCombat, playerStats);
            spellRegistry[def.spellId] = spellInstance;
        }

        public void UpdateController()
        {
            SyncWithProgression();

            float dt = Time.deltaTime;
            foreach (var kvp in spellRegistry)
            {
                kvp.Value.UpdateSpell(dt);
            }
        }

        public void SyncWithProgression()
        {
            if (ProgressionManager.Instance == null) return;

            MasteryTier t1 = ProgressionManager.Instance.GetTier(MasteryPath.Path1);
            MasteryTier t2 = ProgressionManager.Instance.GetTier(MasteryPath.Path2);
            MasteryTier t3 = ProgressionManager.Instance.GetTier(MasteryPath.Path3);

            if (t3 > t2 && t3 > t1)
            {
                ActiveBuild = MageBuildType.Cosmic;
                ActiveTier = (MageTier)((int)t3 - 1);
            }
            else if (t2 > t1 && t2 >= t3)
            {
                ActiveBuild = MageBuildType.Warlock;
                ActiveTier = (MageTier)((int)t2 - 1);
            }
            else if (t1 > MasteryTier.None)
            {
                ActiveBuild = MageBuildType.Elemental;
                ActiveTier = (MageTier)((int)t1 - 1);
            }
            else
            {
                ActiveBuild = MageBuildType.Elemental;
                ActiveTier = MageTier.N1;
            }
        }

        public MageBuildDefinition GetBuildDefinition(MageBuildType buildType)
        {
            switch (buildType)
            {
                case MageBuildType.Warlock: return warlockBuild;
                case MageBuildType.Cosmic: return cosmicBuild;
                case MageBuildType.Elemental:
                default: return elementalBuild;
            }
        }

        public void UpdateChargeFeedback(float chargeRatio)
        {
            MageBuildDefinition currentBuildDef = GetBuildDefinition(ActiveBuild);
            Color themeCol = currentBuildDef != null ? currentBuildDef.themeColor : Color.cyan;

            if (chargeVFXInstance == null && playerCombat != null)
            {
                chargeVFXInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                chargeVFXInstance.name = "MageChargeFeedbackVFX";
                Destroy(chargeVFXInstance.GetComponent<Collider>());
                var rend = chargeVFXInstance.GetComponent<Renderer>();
                if (rend != null) rend.material.color = themeCol;
            }

            if (chargeVFXInstance != null && playerCombat != null)
            {
                Vector3 staffPos = playerCombat.transform.position + playerCombat.transform.forward * 0.8f + Vector3.up * 1.5f;
                chargeVFXInstance.transform.position = staffPos;

                float s = 0.3f + chargeRatio * 0.7f;
                chargeVFXInstance.transform.localScale = new Vector3(s, s, s);
            }
        }

        public void StopChargeFeedback()
        {
            if (chargeVFXInstance != null)
            {
                Destroy(chargeVFXInstance);
                chargeVFXInstance = null;
            }
        }

        public void ExecuteBasicAttack(Vector3 aimDirection)
        {
            MageBuildDefinition currentBuildDef = GetBuildDefinition(ActiveBuild);
            MageAbilityDefinition abilityDef = currentBuildDef.GetAbility(ActiveTier, false);

            if (abilityDef != null && spellRegistry.TryGetValue(abilityDef.spellId, out MageSpell spell))
            {
                spell.Cast(aimDirection, 0f);
            }
        }

        public void ExecuteChargedAttack(Vector3 aimDirection, float chargeRatio)
        {
            MageBuildDefinition currentBuildDef = GetBuildDefinition(ActiveBuild);
            MageAbilityDefinition abilityDef = currentBuildDef.GetAbility(ActiveTier, true);

            if (abilityDef != null && spellRegistry.TryGetValue(abilityDef.spellId, out MageSpell spell))
            {
                spell.Cast(aimDirection, chargeRatio);
            }
        }
    }
}
