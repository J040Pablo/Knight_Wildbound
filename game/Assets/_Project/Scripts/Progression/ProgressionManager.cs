using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Player;
using Roguelite.Data;

namespace Roguelite.Progression
{
    public class ProgressionManager : MonoBehaviour
    {
        private static ProgressionManager instance;
        private static bool applicationIsQuitting = false;

        public static ProgressionManager Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<ProgressionManager>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("ProgressionManager");
                        instance = go.AddComponent<ProgressionManager>();
                    }
                }
                return instance;
            }
        }

        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public const int MAX_LEVEL = 50;

        [Header("Class Data Assets")]
        [SerializeField] private ClassDefinition knightClassDefinition;
        [SerializeField] private ClassDefinition mageClassDefinition;
        [SerializeField] private ClassDefinition druidClassDefinition;

        // Current Progression State
        public ClassType CurrentClass { get; private set; } = ClassType.None;
        public int CurrentLevel { get; private set; } = 1;
        public int CurrentLevelXP { get; private set; } = 0;
        public int TotalXP { get; private set; } = 0;
        public int PendingLevelUpCount { get; private set; } = 0;

        public AttackProfileDefinition CurrentBasicAttack { get; private set; }
        public AttackProfileDefinition CurrentChargedAttack { get; private set; }

        private ClassDefinition activeClassDefinition;
        private readonly Dictionary<MasteryPath, MasteryTier> pathTiers = new Dictionary<MasteryPath, MasteryTier>();
        private readonly HashSet<AbilityId> unlockedAbilities = new HashSet<AbilityId>();
        private readonly HashSet<ClassUpgradeDefinition> unlockedUpgrades = new HashSet<ClassUpgradeDefinition>();

        // Events
        public event Action<int, int> OnXPChanged; // currentLevelXP, targetXP
        public event Action<int> OnLevelChanged; // currentLevel
        public event Action<ClassType> OnClassSelected;
        public event Action<MasteryPath, MasteryTier> OnMasteryUnlocked;
        public event Action OnLevelUpPending;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePathTiers();
            LoadDefaultDefinitionsIfMissing();
        }

        private void InitializePathTiers()
        {
            pathTiers[MasteryPath.Path1] = MasteryTier.None;
            pathTiers[MasteryPath.Path2] = MasteryTier.None;
            pathTiers[MasteryPath.Path3] = MasteryTier.None;
        }

        private void LoadDefaultDefinitionsIfMissing()
        {
            if (knightClassDefinition == null)
            {
                knightClassDefinition = ScriptableObject.CreateInstance<ClassDefinition>();
                knightClassDefinition.classType = ClassType.Knight;
                knightClassDefinition.className = "Knight";
                knightClassDefinition.path1Name = "Helmet";
                knightClassDefinition.path1Abbrev = "HEL";
                knightClassDefinition.path2Name = "Sword";
                knightClassDefinition.path2Abbrev = "SW";
                knightClassDefinition.path3Name = "Armor";
                knightClassDefinition.path3Abbrev = "ARM";

                PopulateDefaultKnightUpgrades(knightClassDefinition);
            }

            if (mageClassDefinition == null)
            {
                mageClassDefinition = ScriptableObject.CreateInstance<ClassDefinition>();
                mageClassDefinition.classType = ClassType.Mage;
                mageClassDefinition.className = "Mage";
                mageClassDefinition.path1Name = "Elemental";
                mageClassDefinition.path1Abbrev = "ELE";
                mageClassDefinition.path2Name = "Warlock";
                mageClassDefinition.path2Abbrev = "WAR";
                mageClassDefinition.path3Name = "Cosmic";
                mageClassDefinition.path3Abbrev = "COS";

                PopulateDefaultMageUpgrades(mageClassDefinition);
            }

            if (druidClassDefinition == null)
            {
                druidClassDefinition = ScriptableObject.CreateInstance<ClassDefinition>();
                druidClassDefinition.classType = ClassType.Druid;
                druidClassDefinition.className = "Druid";
                druidClassDefinition.path1Name = "Shapeshift";
                druidClassDefinition.path1Abbrev = "SHP";
                druidClassDefinition.path2Name = "Summoner";
                druidClassDefinition.path2Abbrev = "SUM";
                druidClassDefinition.path3Name = "Nature";
                druidClassDefinition.path3Abbrev = "NAT";

                PopulateDefaultDruidUpgrades(druidClassDefinition);
            }
        }

        private void PopulateDefaultKnightUpgrades(ClassDefinition def)
        {
            // HELMET PATH (Mobility)
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N1, "Helmet N1 — Quick Slash & Charge Dash", "+15% Move Speed\nBasic: Quick Slash (Fast horizontal slash, small lunge)\nCharged: Charge Dash (Dashes forward, strong impact hit)", "Visual: Cape", 0.15f, 0f, 0f, "Quick Slash", "Charge Dash", "", null);
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N2, "Helmet N2 — Double Slash & Furious Charge", "+30% Move Speed\nBasic: Double Slash (Two quick slashes, 2nd hit stronger)\nCharged: Furious Charge (Longer dash, pierces enemies, knocks back small enemies)", "Visual: Closed Helmet", 0.30f, 0f, 0f, "Double Slash", "Furious Charge", "", null);
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N3, "Helmet N3 — Wind Slash & Whirlwind", "Basic: Wind Slash (Air blade projectile)\nCharged: Whirlwind (Spins sword, small tornado, pulls enemies, multi-hit)\nPassive: Dodge Roll becomes Dash", "Visual: Helmet Crest & Shoulder Armor", 0.35f, 0f, 0f, "Wind Slash", "Whirlwind", "Dodge Roll becomes Dash", CreateAbility(AbilityId.KnightWhirlwind, "Whirlwind", 15f, 40f, 4.5f));

            // SWORD PATH (Damage)
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N1, "Sword N1 — Heavy Slash & Vertical Strike", "+20% Attack Damage\nBasic: Heavy Slash\nCharged: Vertical Strike (Overhead strike, small shockwave)", "Visual: Large Sword", 0f, 0.20f, 0f, "Heavy Slash", "Vertical Strike", "", null);
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N2, "Sword N2 — Power Slash & Ground Breaker", "+40% Attack Damage\nBasic: Power Slash (Larger hit arc, white energy effect)\nCharged: Ground Breaker (Sword slam, ground crack wave, knockdown)", "Visual: Broadsword", 0f, 0.40f, 0f, "Power Slash", "Ground Breaker", "", null);
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N3, "Sword N3 — Aura Slash & Celestial Strike", "+60% Attack Damage\nBasic: Aura Slash (Energy blade projectile on swing)\nCharged: Celestial Strike (Massive charge, thrust attack, energy wave)\nPassive: Charged Energy Wave", "Visual: Greatsword & Energy Trail", 0f, 0.60f, 0f, "Aura Slash", "Celestial Strike", "Charged Energy Wave", CreateAbility(AbilityId.KnightCelestialStrike, "Celestial Strike", 20f, 75f, 8.0f));

            // ARMOR PATH (Tank)
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N1, "Armor N1 — Shield Bash & Shield Charge", "+20 Max HP\nBasic: Shield Bash\nCharged: Shield Charge (Advance raised shield, reduced incoming damage)", "Visual: Small Shield", 0f, 0f, 20f, "Shield Bash", "Shield Charge", "", null);
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N2, "Armor N2 — Heavy Bash & Wall Stance", "+40 Max HP\nBasic: Heavy Bash\nCharged: Wall Stance (Plant shield, major damage reduction)", "Visual: Tower Shield", 0f, 0f, 40f, "Heavy Bash", "Wall Stance", "", null);
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N3, "Armor N3 — Armored Strike & Guardian Impact", "+60 Max HP\nBasic: Armored Strike (Uninterruptible attack)\nCharged: Guardian Impact (Slow advance, massive impact wave)\nPassive: Hyper Armor", "Visual: Guardian Armor", 0f, 0f, 60f, "Armored Strike", "Guardian Impact", "Hyper Armor", CreateAbility(AbilityId.KnightGuardianImpact, "Guardian Impact", 18f, 50f, 6.0f));
        }

        private void PopulateDefaultMageUpgrades(ClassDefinition def)
        {
            // ELEMENTAL PATH
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N1, "Elemental N1 — Ice", "+15% Magic Damage\nBasic: Ice Shards\nCharged: Frost Wave", "Visual: Cyan Robe & Ice Crystal", 0.05f, 0.15f, 0f, "Ice Shards", "Frost Wave", "Slow Effect", null);
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N2, "Elemental N2 — Fire", "+30% Magic Damage\nBasic: Fire Spark\nCharged: Fireball", "Visual: Fiery Robe & Fire Crystal", 0.10f, 0.30f, 0f, "Fire Spark", "Fireball", "Explosive Damage", null);
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N3, "Elemental N3 — Electricity", "+50% Magic Damage\nBasic: Lightning Bolt\nCharged: Lightning Strike", "Visual: Lightning Aura & Electric Crystal", 0.15f, 0.50f, 0f, "Lightning Bolt", "Lightning Strike", "Chain & Stun Effects", CreateAbility(AbilityId.MageMeteor, "Lightning Strike", 16f, 90f, 7.0f));

            // WARLOCK PATH
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N1, "Warlock N1 — Dark Magic", "+10% Damage & Cooldown Reduction\nBasic: Dark Orb\nCharged: Shadow Chain", "Visual: Dark Hood", 0.10f, 0.10f, 10f, "Dark Orb", "Shadow Chain", "", null);
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N2, "Warlock N2 — Curse", "+25% Damage\nBasic: Curse Mark\nCharged: Heavy Curse\nPassive: Damage Over Time", "Visual: Floating Runes", 0.15f, 0.25f, 25f, "Curse Mark", "Heavy Curse", "Damage Over Time", null);
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N3, "Warlock N3 — Necromancer", "+40% Damage\nBasic: Spectral Hand\nCharged: Shadow Army\nPassive: Summon Spirits", "Visual: Skull Crown & Necromancer Aura", 0.20f, 0.40f, 40f, "Spectral Hand", "Shadow Army", "Summon Spirits", CreateAbility(AbilityId.MageArcaneStorm, "Shadow Army", 18f, 110f, 9.0f));

            // COSMIC PATH
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N1, "Cosmic N1 — Cosmic Energy", "+15 Max HP & Energy Shield\nBasic: Star Shot\nCharged: Supernova", "Visual: Galaxy Particles", 0f, 0.10f, 15f, "Star Shot", "Supernova", "", null);
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N2, "Cosmic N2 — Portal Magic", "+35 Max HP & Spatial Distortion\nBasic: Spatial Fragment\nCharged: Portal Blast", "Visual: Floating Rings", 0f, 0.25f, 35f, "Spatial Fragment", "Portal Blast", "", null);
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N3, "Cosmic N3 — Universe", "+60 Max HP\nBasic: Cosmic Beam\nCharged: Cosmic Collapse\nPassive: Mini Black Hole Pull", "Visual: Cosmic Crown & Universe Aura", 0f, 0.40f, 60f, "Cosmic Beam", "Cosmic Collapse", "Mini Black Hole Pull", CreateAbility(AbilityId.MageArcaneStorm, "Cosmic Collapse", 22f, 125f, 10.0f));
        }

        private void PopulateDefaultDruidUpgrades(ClassDefinition def)
        {
            // SHAPESHIFT PATH
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N1, "Shapeshift N1 — Wolf", "+15% Move Speed\nBasic: Bite\nCharged: Wild Charge", "Visual: Wolf Ears & Claws", 0.15f, 0.10f, 0f, "Bite", "Wild Charge", "", null);
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N2, "Shapeshift N2 — Bear", "+30 Max HP\nBasic: Bear Swipe\nCharged: Bear Fury", "Visual: Bear Fur Armor", 0.05f, 0.25f, 30f, "Bear Swipe", "Bear Fury", "", null);
            CreateUpgrade(def, MasteryPath.Path1, MasteryTier.N3, "Shapeshift N3 — Mini Dragon", "+50% Damage & Dragon Form\nBasic: Flame Claws\nCharged: Dragon Breath\nPassive: Dragon Form", "Visual: Dragon Wings & Tail", 0.20f, 0.50f, 50f, "Flame Claws", "Dragon Breath", "Dragon Form", CreateAbility(AbilityId.DruidNatureWrath, "Dragon Breath", 15f, 85f, 8.5f));

            // SUMMONER PATH
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N1, "Summoner N1 — Wolf Companion", "+15% Damage & Wolf Companion\nBasic: Pack Command\nCharged: Wild Howl", "Visual: Wolf Companion", 0.10f, 0.15f, 10f, "Pack Command", "Wild Howl", "", null);
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N2, "Summoner N2 — Bear Companion", "+30% Damage & Bear Companion\nBasic: Companion Strike\nCharged: Bear Charge", "Visual: Bear Companion", 0.10f, 0.30f, 25f, "Companion Strike", "Bear Charge", "", null);
            CreateUpgrade(def, MasteryPath.Path2, MasteryTier.N3, "Summoner N3 — Legendary Creatures", "+50% Damage & Legendary Spirits\nBasic: Pack Assault\nCharged: Call of the Forest\nPassive: Multiple Creature Summons", "Visual: Legendary Spirits", 0.15f, 0.50f, 40f, "Pack Assault", "Call of the Forest", "Multiple Creature Summons", CreateAbility(AbilityId.DruidNatureWrath, "Call of the Forest", 17f, 95f, 7.5f));

            // NATURE PATH
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N1, "Nature N1 — Healing", "+20 Max HP & Thorn Shot\nBasic: Thorn Shot\nCharged: Nature Heal", "Visual: Green Aura", 0.05f, 0.10f, 20f, "Thorn Shot", "Nature Heal", "", null);
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N2, "Nature N2 — Vines", "+40 Max HP & Living Vines\nBasic: Vine Whip\nCharged: Root Prison", "Visual: Living Vines", 0.10f, 0.25f, 40f, "Vine Whip", "Root Prison", "", null);
            CreateUpgrade(def, MasteryPath.Path3, MasteryTier.N3, "Nature N3 — Forest Guardian", "+70 Max HP & Tree Armor\nBasic: Leaf Burst\nCharged: Wrath of the Forest\nPassive: Area Heal + Root Attacks", "Visual: Forest Crown & Tree Armor", 0.15f, 0.40f, 70f, "Leaf Burst", "Wrath of the Forest", "Area Heal + Root Attacks", CreateAbility(AbilityId.DruidNatureWrath, "Wrath of the Forest", 20f, 100f, 9.5f));
        }

        private ClassUpgradeDefinition CreateUpgrade(
            ClassDefinition parentDef,
            MasteryPath p,
            MasteryTier t,
            string title,
            string desc,
            string vis,
            float moveSpeed,
            float dmg,
            float hp,
            string basicName,
            string chargedName,
            string passiveName,
            AbilityDefinition ability)
        {
            var u = ScriptableObject.CreateInstance<ClassUpgradeDefinition>();
            u.classType = parentDef.classType;
            u.path = p;
            u.tier = t;
            u.upgradeTitle = title;
            u.description = desc;
            u.visualPreviewText = vis;
            u.moveSpeedBonusPercent = moveSpeed;
            u.attackDamageBonusPercent = dmg;
            u.maxHpBonusFlat = hp;
            u.basicAttackName = basicName;
            u.chargedAttackName = chargedName;
            u.specialPassiveName = passiveName;
            u.specialAbility = ability;

            if (!string.IsNullOrEmpty(basicName))
            {
                u.basicAttack = ScriptableObject.CreateInstance<AttackProfileDefinition>();
                u.basicAttack.attackName = basicName;
                u.basicAttack.damageMultiplier = 1.0f + dmg;
            }

            if (!string.IsNullOrEmpty(chargedName))
            {
                u.chargedAttack = ScriptableObject.CreateInstance<AttackProfileDefinition>();
                u.chargedAttack.attackName = chargedName;
                u.chargedAttack.damageMultiplier = 1.8f + dmg;
            }

            parentDef.upgrades.Add(u);
            return u;
        }

        private AbilityDefinition CreateAbility(AbilityId id, string name, float cd, float dmg, float rad)
        {
            var a = ScriptableObject.CreateInstance<AbilityDefinition>();
            a.abilityId = id;
            a.abilityName = name;
            a.cooldown = cd;
            a.damage = dmg;
            a.radius = rad;
            return a;
        }

        public int GetXPRequired(int level)
        {
            switch (level)
            {
                case 1: return 100;
                case 2: return 150;
                case 3: return 225;
                case 4: return 325;
                default:
                    float val = 325f;
                    for (int l = 5; l <= level; l++)
                    {
                        val *= 1.4f;
                    }
                    return Mathf.RoundToInt(val);
            }
        }

        public MasteryTier GetTier(MasteryPath path)
        {
            return pathTiers.TryGetValue(path, out var tier) ? tier : MasteryTier.None;
        }

        public bool HasAbility(AbilityId abilityId)
        {
            return unlockedAbilities.Contains(abilityId);
        }

        public ClassDefinition GetActiveClassDefinition()
        {
            return activeClassDefinition;
        }

        public void SetClass(ClassType classType)
        {
            if (CurrentClass != ClassType.None) return; // Strict lock once selected!

            CurrentClass = classType;
            switch (classType)
            {
                case ClassType.Mage: activeClassDefinition = mageClassDefinition; break;
                case ClassType.Druid: activeClassDefinition = druidClassDefinition; break;
                case ClassType.Knight:
                default: activeClassDefinition = knightClassDefinition; break;
            }

            Debug.Log($"Class selected: {classType}");

            OnClassSelected?.Invoke(CurrentClass);
            Debug.Log("[Progression] OnClassSelected fired");

            if (PendingLevelUpCount > 0)
            {
                Debug.Log($"[ProgressionManager] OnLevelUpPending Fired via SetClass (Pending Count: {PendingLevelUpCount})");
                OnLevelUpPending?.Invoke();
            }
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            TotalXP += amount;
            CurrentLevelXP += amount;
            Debug.Log($"[ProgressionManager] XP Added: {amount} | CurrentLevelXP: {CurrentLevelXP} | TotalXP: {TotalXP}");

            int targetXP = GetXPRequired(CurrentLevel);
            while (CurrentLevel < MAX_LEVEL && CurrentLevelXP >= targetXP)
            {
                CurrentLevelXP -= targetXP;
                CurrentLevel++;
                PendingLevelUpCount++;

                Debug.Log($"[ProgressionManager] Level Up -> {CurrentLevel} | Pending LevelUps -> {PendingLevelUpCount}");

                OnLevelChanged?.Invoke(CurrentLevel);
                targetXP = GetXPRequired(CurrentLevel);

                if (CurrentClass != ClassType.None)
                {
                    Debug.Log($"[ProgressionManager] OnLevelUpPending Fired (Pending LevelUps: {PendingLevelUpCount})");
                    OnLevelUpPending?.Invoke();
                }
            }

            OnXPChanged?.Invoke(CurrentLevelXP, targetXP);
        }

        public List<ClassUpgradeDefinition> GetUpgradeChoices()
        {
            List<ClassUpgradeDefinition> choices = new List<ClassUpgradeDefinition>();
            if (activeClassDefinition == null || activeClassDefinition.upgrades == null)
            {
                Debug.LogWarning($"[ProgressionManager] GetUpgradeChoices: activeClassDefinition is null or has no upgrades for class {CurrentClass}");
                return choices;
            }

            foreach (MasteryPath path in Enum.GetValues(typeof(MasteryPath)))
            {
                MasteryTier currentTier = GetTier(path);
                if (currentTier >= MasteryTier.N3) continue;

                MasteryTier nextTier = (MasteryTier)((int)currentTier + 1);
                ClassUpgradeDefinition nextUpgrade = activeClassDefinition.upgrades.Find(u => u.path == path && u.tier == nextTier);

                if (nextUpgrade != null)
                {
                    choices.Add(nextUpgrade);
                }
            }

            if (choices.Count == 0 && PendingLevelUpCount > 0)
            {
                PendingLevelUpCount = 0; // Consume pending level ups when all paths are maxed
            }

            Debug.Log($"[ProgressionManager] GetUpgradeChoices() returned {choices.Count} choices for class {CurrentClass}");
            return choices;
        }

        public bool SelectUpgrade(ClassUpgradeDefinition upgrade)
        {
            if (upgrade == null) return false;

            pathTiers[upgrade.path] = upgrade.tier;
            unlockedUpgrades.Add(upgrade);

            if (upgrade.specialAbility != null && upgrade.specialAbility.abilityId != AbilityId.None)
            {
                unlockedAbilities.Add(upgrade.specialAbility.abilityId);
            }

            if (upgrade.basicAttack != null) CurrentBasicAttack = upgrade.basicAttack;
            if (upgrade.chargedAttack != null) CurrentChargedAttack = upgrade.chargedAttack;

            // Apply stat boosts to PlayerStats
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null)
            {
                if (upgrade.moveSpeedBonusPercent > 0)
                {
                    UpgradeData dummy = ScriptableObject.CreateInstance<UpgradeData>();
                    dummy.type = UpgradeType.MoveSpeedPercent;
                    dummy.statValue = upgrade.moveSpeedBonusPercent;
                    stats.ApplyUpgrade(dummy);
                }
                if (upgrade.attackDamageBonusPercent > 0)
                {
                    UpgradeData dummy = ScriptableObject.CreateInstance<UpgradeData>();
                    dummy.type = UpgradeType.AttackDamagePercent;
                    dummy.statValue = upgrade.attackDamageBonusPercent;
                    stats.ApplyUpgrade(dummy);
                }
                if (upgrade.maxHpBonusFlat > 0)
                {
                    UpgradeData dummy = ScriptableObject.CreateInstance<UpgradeData>();
                    dummy.type = UpgradeType.MaxHealthFlat;
                    dummy.statValue = upgrade.maxHpBonusFlat;
                    stats.ApplyUpgrade(dummy);
                }
            }

            PendingLevelUpCount = Mathf.Max(0, PendingLevelUpCount - 1);
            OnMasteryUnlocked?.Invoke(upgrade.path, upgrade.tier);

            return PendingLevelUpCount > 0;
        }

        public void ResetRun()
        {
            CurrentClass = ClassType.None;
            CurrentLevel = 1;
            CurrentLevelXP = 0;
            TotalXP = 0;
            PendingLevelUpCount = 0;

            CurrentBasicAttack = null;
            CurrentChargedAttack = null;

            unlockedAbilities.Clear();
            unlockedUpgrades.Clear();
            InitializePathTiers();

            activeClassDefinition = null;

            OnXPChanged?.Invoke(0, GetXPRequired(1));
            OnLevelChanged?.Invoke(1);
        }
    }
}
