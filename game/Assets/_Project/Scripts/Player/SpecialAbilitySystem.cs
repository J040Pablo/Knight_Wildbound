using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;
using Roguelite.Progression;

namespace Roguelite.Player
{
    public class SpecialAbilitySystem : MonoBehaviour
    {
        private PlayerCombat playerCombat;
        private PlayerStats playerStats;
        private PlayerController playerController;

        private float abilityCooldownTimer = 0f;
        private float currentAbilityMaxCooldown = 15f;
        private bool isExecutingAbility = false;

        private void Awake()
        {
            playerCombat = GetComponent<PlayerCombat>();
            playerStats = GetComponent<PlayerStats>();
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (UI.MasteryScreenUI.IsAnyMenuOpen || (Core.InputStateManager.Instance != null && Core.InputStateManager.Instance.CurrentMode == Core.InputMode.UI)) return;

            if (abilityCooldownTimer > 0)
            {
                abilityCooldownTimer -= Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (CanUseAbility())
                {
                    TriggerAbility(transform.forward);
                }
            }
        }

        public bool CanUseAbility()
        {
            if (isExecutingAbility || abilityCooldownTimer > 0) return false;
            if (ProgressionManager.Instance == null) return false;

            return ProgressionManager.Instance.HasAbility(AbilityId.KnightWhirlwind) ||
                   ProgressionManager.Instance.HasAbility(AbilityId.KnightCelestialStrike) ||
                   ProgressionManager.Instance.HasAbility(AbilityId.KnightGuardianImpact);
        }

        public float GetCooldownRatio()
        {
            if (currentAbilityMaxCooldown <= 0) return 0f;
            return Mathf.Clamp01(abilityCooldownTimer / currentAbilityMaxCooldown);
        }

        public void TriggerAbility(Vector3 aimDirection)
        {
            if (ProgressionManager.Instance == null) return;

            if (ProgressionManager.Instance.HasAbility(AbilityId.KnightWhirlwind))
            {
                StartCoroutine(PerformWhirlwind());
            }
            else if (ProgressionManager.Instance.HasAbility(AbilityId.KnightCelestialStrike))
            {
                StartCoroutine(PerformCelestialStrike(aimDirection));
            }
            else if (ProgressionManager.Instance.HasAbility(AbilityId.KnightGuardianImpact))
            {
                StartCoroutine(PerformGuardianImpact());
            }
        }

        private IEnumerator PerformWhirlwind()
        {
            isExecutingAbility = true;
            currentAbilityMaxCooldown = 15f;
            abilityCooldownTimer = currentAbilityMaxCooldown;

            float duration = 2.5f;
            float elapsed = 0f;
            float tickTimer = 0f;
            float tickInterval = 0.25f;

            GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vfx.name = "WhirlwindVFX";
            vfx.transform.SetParent(transform, false);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localScale = new Vector3(8.0f, 0.2f, 8.0f);

            Renderer r = vfx.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.9f, 0.8f, 0.2f, 0.4f);
            Collider c = vfx.GetComponent<Collider>();
            if (c != null) Destroy(c);

            while (elapsed < duration)
            {
                transform.Rotate(Vector3.up, 720f * Time.deltaTime);

                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    Collider[] hits = Physics.OverlapSphere(transform.position, 5.0f);
                    foreach (var hit in hits)
                    {
                        EnemyBase enemy = hit.GetComponent<EnemyBase>();
                        if (enemy == null) enemy = hit.GetComponentInParent<EnemyBase>();

                        if (enemy != null && !enemy.IsDead)
                        {
                            Vector3 pullDir = (transform.position - enemy.transform.position).normalized;
                            DamageInfo info = new DamageInfo(playerCombat.BaseDamage * 0.4f, pullDir, -3.0f, false, gameObject);
                            enemy.TakeDamage(info);
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(vfx);
            isExecutingAbility = false;
        }

        private IEnumerator PerformCelestialStrike(Vector3 aimDirection)
        {
            isExecutingAbility = true;
            currentAbilityMaxCooldown = 20f;
            abilityCooldownTimer = currentAbilityMaxCooldown;

            Vector3 safeDir = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : transform.forward;
            if (safeDir.sqrMagnitude < 0.0001f) safeDir = Vector3.forward;
            Vector3 safeUp = Mathf.Abs(Vector3.Dot(safeDir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion beamRot = Quaternion.LookRotation(safeDir, safeUp);
            Vector3 launchPos = transform.position + Vector3.up * 1.0f + safeDir * 1.5f;

            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "CelestialStrikeBeam";
            beam.transform.position = launchPos;
            beam.transform.rotation = beamRot;
            beam.transform.localScale = new Vector3(3.5f, 5.0f, 16.0f);

            Renderer r = beam.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.3f, 0.85f, 1.0f, 0.85f);
            Collider col = beam.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Collider[] hits = Physics.OverlapBox(launchPos + safeDir * 8.0f, new Vector3(1.75f, 2.5f, 8.0f), beamRot);
            foreach (var hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null) enemy = hit.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    DamageInfo info = new DamageInfo(playerCombat.BaseDamage * 3.5f, safeDir, 15.0f, true, gameObject);
                    enemy.TakeDamage(info);
                }
            }

            yield return new WaitForSeconds(0.6f);
            Destroy(beam);
            isExecutingAbility = false;
        }

        private IEnumerator PerformGuardianImpact()
        {
            isExecutingAbility = true;
            currentAbilityMaxCooldown = 18f;
            abilityCooldownTimer = currentAbilityMaxCooldown;

            // Apply 3.0s Invulnerability Aura
            if (playerStats != null) playerStats.IsInvulnerable = true;

            GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wave.name = "GuardianImpactWave";
            wave.transform.position = transform.position;
            wave.transform.localScale = new Vector3(1.0f, 0.1f, 1.0f);

            Renderer r = wave.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.2f, 0.9f, 0.4f, 0.7f);
            Collider c = wave.GetComponent<Collider>();
            if (c != null) Destroy(c);

            float duration = 0.5f;
            float elapsed = 0f;
            float maxRadius = 10f;

            Collider[] hits = Physics.OverlapSphere(transform.position, maxRadius);
            foreach (var hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null) enemy = hit.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    Vector3 pushDir = (enemy.transform.position - transform.position).normalized;
                    DamageInfo info = new DamageInfo(playerCombat.BaseDamage * 2.0f, pushDir, 18.0f, true, gameObject);
                    enemy.TakeDamage(info);
                }
            }

            while (elapsed < duration)
            {
                float scale = Mathf.Lerp(1.0f, maxRadius * 2.0f, elapsed / duration);
                wave.transform.localScale = new Vector3(scale, 0.1f, scale);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(wave);

            yield return new WaitForSeconds(2.5f); // Remain invulnerable for 3s total
            if (playerStats != null) playerStats.IsInvulnerable = false;

            isExecutingAbility = false;
        }
    }
}
