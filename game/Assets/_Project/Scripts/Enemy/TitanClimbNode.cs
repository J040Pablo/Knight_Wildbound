using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Player;
using Roguelite.Core;

namespace Roguelite.Enemy
{
    /// <summary>
    /// Interaction & Climbing Node for the Ancient Stone Titan.
    /// Features:
    /// - Physical parented climb attachment (no artificial script lerping/teleporting).
    /// - Dual leg activation nodes (Left & Right Leg) at ground level.
    /// - 3-Stage Crystal Exposure Progression requiring multiple climbs:
    ///   Stage 1: Cracked (25% HP damage) -> Shake off & Stand up.
    ///   Stage 2: Exposed Core (35% HP damage) -> Shake off & Enrage.
    ///   Stage 3: Destroyed (40% HP finisher kill blow).
    /// - OnDrawGizmos() visualization for Unity Editor debugging.
    /// </summary>
    public class TitanClimbNode : MonoBehaviour
    {
        public AncientStoneTitanAI parentTitan;
        public Transform napePosition;
        public GameObject napeCrystalVisual;

        private bool isActiveNode = false;
        private bool isMounted = false;
        private int crystalExposureStage = 0; // 0=Intact, 1=Cracked, 2=Exposed Core, 3=Shattered
        private PlayerController climbingPlayer;
        private Transform originalPlayerParent;
        private SphereCollider triggerCol;
        private Renderer crystalRenderer;

        private static readonly Color IntactColor = new Color(0.2f, 0.85f, 0.95f);
        private static readonly Color CrackedColor = new Color(0.95f, 0.75f, 0.2f);
        private static readonly Color ExposedCoreColor = new Color(1.0f, 0.25f, 0.1f);
        private static readonly Color ShatteredColor = new Color(0.2f, 0.1f, 0.1f);

        public int CrystalStage => crystalExposureStage;
        public bool IsMounted => isMounted;

        private void Awake()
        {
            triggerCol = gameObject.GetComponent<SphereCollider>();
            if (triggerCol == null) triggerCol = gameObject.AddComponent<SphereCollider>();
            triggerCol.isTrigger = true;
            triggerCol.radius = 5.0f; // Wide ground-level grab radius

            if (napeCrystalVisual != null)
            {
                crystalRenderer = napeCrystalVisual.GetComponent<Renderer>();
                if (crystalRenderer != null) crystalRenderer.material.color = IntactColor;
            }
        }

        public void SetNodeActive(bool active)
        {
            isActiveNode = active;
            if (!active && isMounted)
            {
                DismountPlayer();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckClimbInteraction(other, isJump: true);
        }

        private void OnTriggerStay(Collider other)
        {
            CheckClimbInteraction(other, isJump: false);
        }

        private void CheckClimbInteraction(Collider other, bool isJump)
        {
            if (!isActiveNode || isMounted) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null && other.CompareTag("Player"))
            {
                player = other.GetComponentInParent<PlayerController>();
            }

            if (player != null && !player.GetComponent<PlayerStats>().IsDead)
            {
                // Mount if player presses [E] OR jumps into node trigger
                bool airborneJump = isJump && !player.IsGrounded;
                bool pressedE = Input.GetKeyDown(KeyCode.E);

                if (pressedE || airborneJump)
                {
                    MountPlayer(player);
                }
            }
        }

        public void MountPlayer(PlayerController player)
        {
            if (player == null || napePosition == null || isMounted) return;

            climbingPlayer = player;
            originalPlayerParent = player.transform.parent;
            isMounted = true;

            // Physical Parented Attachment: Lock player to Titan's nape transform
            player.transform.SetParent(napePosition, true);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.RequestOwnership(CameraOwnerType.TitanClimb, napePosition, "TitanClimbNode.MountPlayer");
            }

            ThirdPersonCamera cam = Object.FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.5f, 0.35f);
        }

        public void DismountPlayer()
        {
            if (!isMounted || climbingPlayer == null) return;

            CharacterController cc = climbingPlayer.GetComponent<CharacterController>();
            PlayerController pc = climbingPlayer.GetComponent<PlayerController>();

            // Always unparent player to null and restore scale 1,1,1
            climbingPlayer.transform.SetParent(null, true);
            climbingPlayer.transform.localScale = Vector3.one;

            // Throw player back with an ejection impulse
            Vector3 throwDir = napePosition != null ? -napePosition.forward + Vector3.up * 0.6f : Vector3.back + Vector3.up * 0.6f;
            climbingPlayer.transform.position += throwDir * 4.5f;

            if (cc != null) cc.enabled = true;
            if (pc != null) pc.enabled = true;

            isMounted = false;
            climbingPlayer = null;

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.ReleaseOwnership(CameraOwnerType.TitanClimb, "TitanClimbNode.DismountPlayer");
                CameraManager.Instance.ForceRestorePlayerCamera("TitanClimbNode.DismountPlayer");
            }
        }

        /// <summary>
        /// Called when the player strikes the nape crystal while attached to top of Titan.
        /// Advances 3-Stage Crystal Exposure progression across multiple climb phases!
        /// </summary>
        public void StrikeNapeCrystal(float baseDamage)
        {
            if (parentTitan == null || parentTitan.IsDead) return;

            crystalExposureStage++;

            ThirdPersonCamera cam = Object.FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.7f, 0.5f);

            switch (crystalExposureStage)
            {
                case 1: // Stage 1: Cracked (25% max HP damage) -> Shake Off
                    if (crystalRenderer != null) crystalRenderer.material.color = CrackedColor;
                    parentTitan.TakeNapeDirectDamage(parentTitan.MaxHP * 0.25f);
                    SpawnSparks(transform.position, CrackedColor);
                    DismountPlayer();
                    parentTitan.ShakeOffPlayerAndStandUp();
                    break;

                case 2: // Stage 2: Exposed Core (35% max HP damage) -> Shake Off & Enrage
                    if (crystalRenderer != null) crystalRenderer.material.color = ExposedCoreColor;
                    parentTitan.TakeNapeDirectDamage(parentTitan.MaxHP * 0.35f);
                    SpawnSparks(transform.position, ExposedCoreColor);
                    DismountPlayer();
                    parentTitan.ShakeOffPlayerAndStandUp();
                    break;

                case 3: // Stage 3: Shattered Finisher Blow!
                default:
                    if (crystalRenderer != null) crystalRenderer.material.color = ShatteredColor;
                    parentTitan.TakeNapeDirectDamage(parentTitan.MaxHP * 0.50f); // Catastrophic finisher!
                    SpawnSparks(transform.position, Color.white);
                    DismountPlayer();
                    break;
            }
        }

        private void SpawnSparks(Vector3 pos, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spark.name = "CrystalSpark_VFX";
                Destroy(spark.GetComponent<Collider>());
                spark.transform.position = pos + Random.insideUnitSphere * 1.2f;
                spark.transform.localScale = Vector3.one * 0.5f;

                Renderer rend = spark.GetComponent<Renderer>();
                if (rend != null) rend.material.color = color;

                Destroy(spark, 0.5f);
            }
        }

        private void OnGUI()
        {
            if (isActiveNode && !isMounted)
            {
                PlayerController p = Object.FindFirstObjectByType<PlayerController>();
                if (p != null && Vector3.Distance(p.transform.position, transform.position) < 6.0f)
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.fontSize = 22;
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = Color.yellow;
                    style.alignment = TextAnchor.MiddleCenter;

                    GUI.Label(new Rect(Screen.width / 2f - 250, Screen.height * 0.72f, 500, 40), "[E] or JUMP to Climb Ancient Stone Titan!", style);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isActiveNode ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 2.5f);

            if (napePosition != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, napePosition.position);
                Gizmos.DrawWireSphere(napePosition.position, 1.2f);
            }
        }
    }
}
