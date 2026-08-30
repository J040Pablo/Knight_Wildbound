using UnityEngine;
using Roguelite.Core;
using Roguelite.Player;
using Roguelite.Progression;

namespace Roguelite.Environment
{
    public class WeaponInteractable : MonoBehaviour, IInteractable
    {
        [Header("Weapon Choice")]
        [SerializeField] private CharacterType targetClass = CharacterType.Knight;

        public CharacterType TargetClass => targetClass;

        public string InteractionPrompt
        {
            get
            {
                switch (targetClass)
                {
                    case CharacterType.Mage: return "E — Pick up Staff (Mage)";
                    case CharacterType.Druid: return "E — Pick up Nature Staff (Druid)";
                    case CharacterType.Knight:
                    default: return "E — Pick up Sword (Knight)";
                }
            }
        }

        public bool CanInteract(GameObject player)
        {
            if (GameSessionManager.Instance != null && GameSessionManager.Instance.HasSelectedCharacter)
            {
                return false;
            }

            if (ProgressionManager.Instance != null && ProgressionManager.Instance.CurrentClass != ClassType.None)
            {
                return false;
            }

            return true;
        }

        public void Interact(GameObject player)
        {
            Debug.Log("[Weapon] Interact called");
            Debug.Log($"[Weapon] Selected class: {targetClass}");

            // Map CharacterType to ClassType
            ClassType pClass = ClassType.Knight;
            if (targetClass == CharacterType.Mage) pClass = ClassType.Mage;
            else if (targetClass == CharacterType.Druid) pClass = ClassType.Druid;

            // 1. Update ProgressionManager state (Permanent class selection)
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.SetClass(pClass);
            }

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.SelectedCharacter = targetClass;
                GameSessionManager.Instance.HasSelectedCharacter = true;
            }
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.SelectedCharacter = targetClass;
            }

            // Find player if null
            if (player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj;
            }

            // 2. Apply weapon visual & combat behavior to player
            if (player != null)
            {
                PlayerCombat combat = player.GetComponent<PlayerCombat>();
                PlayerStats stats = player.GetComponent<PlayerStats>();
                SetupPlayerClassVisualsAndBehavior(player, targetClass, combat, stats);
            }

            // 3. Deactivate all weapon pickups on ground so player cannot pick multiple classes
            WeaponInteractable[] allWeapons = FindObjectsByType<WeaponInteractable>(FindObjectsSortMode.None);
            foreach (var weapon in allWeapons)
            {
                weapon.gameObject.SetActive(false);
            }

            // 4. Unlock forest path gate in Ruins if present
            RuinsExitGate gate = FindFirstObjectByType<RuinsExitGate>();
            if (gate != null)
            {
                gate.UnlockGate();
            }

            // 5. Trigger notification banner
            if (Wave.EncounterManager.Instance != null)
            {
                Wave.EncounterManager.Instance.TriggerBanner($"⚔️ {targetClass.ToString().ToUpper()} CLASS UNLOCKED! Press [Q] for Masteries!");
            }
        }

        public static void SetupPlayerClassVisualsAndBehavior(GameObject playerObj, CharacterType selectedChar, PlayerCombat combat, PlayerStats stats)
        {
            if (playerObj == null) return;

            ClassType pClass = ClassType.Knight;
            if (selectedChar == CharacterType.Mage) pClass = ClassType.Mage;
            else if (selectedChar == CharacterType.Druid) pClass = ClassType.Druid;

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.SetClass(pClass);
            }

            // Remove previous visual weapons/hats
            foreach (Transform child in playerObj.transform)
            {
                if (child.name.EndsWith("_Visual"))
                {
                    Destroy(child.gameObject);
                }
            }

            Renderer playerRenderer = playerObj.GetComponent<Renderer>();

            switch (selectedChar)
            {
                case CharacterType.Mage:
                    if (playerRenderer != null) playerRenderer.material.color = new Color(0.55f, 0.2f, 0.85f);

                    GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    hat.name = "WizardHat_Visual";
                    hat.transform.parent = playerObj.transform;
                    hat.transform.localPosition = new Vector3(0, 2.1f, 0);
                    hat.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
                    Destroy(hat.GetComponent<Collider>());
                    Renderer hR = hat.GetComponent<Renderer>();
                    if (hR != null) hR.material.color = new Color(0.35f, 0.1f, 0.6f);

                    GameObject mageStaff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    mageStaff.name = "MageStaff_Visual";
                    mageStaff.transform.parent = playerObj.transform;
                    mageStaff.transform.localPosition = new Vector3(0.55f, 1.0f, 0.3f);
                    mageStaff.transform.localScale = new Vector3(0.08f, 1.3f, 0.08f);
                    Destroy(mageStaff.GetComponent<Collider>());
                    Renderer mR = mageStaff.GetComponent<Renderer>();
                    if (mR != null) mR.material.color = new Color(0.8f, 0.7f, 0.2f);

                    // Glowing Staff Tip Crystal
                    GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    crystal.name = "StaffCrystal_Visual";
                    crystal.transform.parent = mageStaff.transform;
                    crystal.transform.localPosition = new Vector3(0, 1.1f, 0);
                    crystal.transform.localScale = new Vector3(3.5f, 0.25f, 3.5f);
                    Destroy(crystal.GetComponent<Collider>());
                    Renderer cR = crystal.GetComponent<Renderer>();
                    if (cR != null) cR.material.color = new Color(0.3f, 0.85f, 1.0f);

                    if (combat != null) combat.SetCombatBehavior(new MageCombatBehavior());
                    break;

                case CharacterType.Druid:
                    if (playerRenderer != null) playerRenderer.material.color = new Color(0.2f, 0.7f, 0.35f);

                    GameObject druidStaff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    druidStaff.name = "DruidStaff_Visual";
                    druidStaff.transform.parent = playerObj.transform;
                    druidStaff.transform.localPosition = new Vector3(0.55f, 1.0f, 0.3f);
                    druidStaff.transform.localScale = new Vector3(0.1f, 1.4f, 0.1f);
                    Destroy(druidStaff.GetComponent<Collider>());
                    Renderer dR = druidStaff.GetComponent<Renderer>();
                    if (dR != null) dR.material.color = new Color(0.45f, 0.3f, 0.15f);

                    if (combat != null) combat.SetCombatBehavior(new DruidCombatBehavior());
                    break;

                case CharacterType.Knight:
                default:
                    if (playerRenderer != null) playerRenderer.material.color = new Color(0.2f, 0.55f, 0.95f);

                    GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sword.name = "Greatsword_Visual";
                    sword.transform.parent = playerObj.transform;
                    sword.transform.localPosition = new Vector3(0.6f, 1.0f, 0.5f);
                    sword.transform.localScale = new Vector3(0.15f, 1.4f, 0.2f);
                    sword.transform.localRotation = Quaternion.Euler(30, 0, 0);
                    Destroy(sword.GetComponent<Collider>());
                    Renderer sR = sword.GetComponent<Renderer>();
                    if (sR != null) sR.material.color = new Color(0.85f, 0.85f, 0.9f);

                    if (combat != null) combat.SetCombatBehavior(new KnightCombatBehavior());
                    break;
            }
        }
    }
}
