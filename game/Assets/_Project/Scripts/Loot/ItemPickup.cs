using UnityEngine;
using Roguelite.Items;
using Roguelite.Inventory;
using Roguelite.Player;
using Roguelite.Progression;

namespace Roguelite.Loot
{
    /// <summary>
    /// Interactive physical 3D drop entity in the game world.
    /// Auto-collects when player walks over the pickup radius.
    /// Implements special vertical beam & particle presentation for Campaign Relics.
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        public ItemData item;
        public int quantity = 1;
        public int goldAmount = 0;

        [Header("Pickup Physics & Magnet")]
        public float autoCollectRadius = 1.25f;
        public float magnetRadius = 10.0f;
        public float baseMagnetSpeed = 15.0f;

        private Transform playerTransform;
        private bool isBeingCollected = false;
        private Vector3 startPosition;
        private float hoverTime = 0f;
        private float currentAttractionSpeed = 15.0f;

        private GameObject relicBeamObject;
        private ParticleSystem relicParticles;

        public static ItemPickup Spawn(Vector3 position, ItemData item, int quantity = 1)
        {
            GameObject go = new GameObject($"Pickup_{item?.itemName ?? "Unknown"}");
            go.transform.position = position;
            ItemPickup pickup = go.AddComponent<ItemPickup>();
            pickup.item = item;
            pickup.quantity = quantity;
            pickup.goldAmount = 0;
            return pickup;
        }

        public static ItemPickup SpawnGold(Vector3 position, int amount)
        {
            GameObject go = new GameObject($"Pickup_Gold_{amount}");
            go.transform.position = position;
            ItemPickup pickup = go.AddComponent<ItemPickup>();
            pickup.item = null;
            pickup.quantity = 1;
            pickup.goldAmount = amount;
            return pickup;
        }

        private void Start()
        {
            startPosition = transform.position;
            currentAttractionSpeed = baseMagnetSpeed;

            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = magnetRadius;

            BuildVisuals();
        }

        private void BuildVisuals()
        {
            bool isRelic = (item != null && (item.isRelic || item.category == ItemCategory.Relic || item.rarity == ItemRarity.Relic));

            // Create Visual Mesh (Sphere or Cube)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "VisualMesh";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;

            Collider vCol = visual.GetComponent<Collider>();
            if (vCol != null) Destroy(vCol);

            Renderer ren = visual.GetComponent<Renderer>();

            if (goldAmount > 0)
            {
                visual.transform.localScale = Vector3.one * 0.35f;
                if (ren != null) ren.material.color = new Color(0.95f, 0.82f, 0.15f); // Bright Gold
            }
            else if (item != null)
            {
                visual.transform.localScale = isRelic ? Vector3.one * 0.7f : Vector3.one * 0.45f;
                if (ren != null) ren.material.color = item.RarityColor;
            }

            // ── SPECIAL RELIC PRESENTATION ──────────────────────
            if (isRelic)
            {
                // 1. Vertical Beam of Light shooting skyward
                relicBeamObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                relicBeamObject.name = "RelicSkyBeam";
                relicBeamObject.transform.SetParent(transform, false);
                relicBeamObject.transform.localPosition = new Vector3(0, 6f, 0); // Center at Y=6
                relicBeamObject.transform.localScale = new Vector3(0.35f, 6.0f, 0.35f); // Height 12m

                Collider bCol = relicBeamObject.GetComponent<Collider>();
                if (bCol != null) Destroy(bCol);

                Renderer bRen = relicBeamObject.GetComponent<Renderer>();
                if (bRen != null)
                {
                    bRen.material.color = new Color(0.20f, 0.85f, 0.40f, 0.45f); // Transparent Emerald Green
                }

                // 2. Green Particle Swirl Aura
                GameObject psGo = new GameObject("RelicParticles");
                psGo.transform.SetParent(transform, false);
                psGo.transform.localPosition = Vector3.zero;

                relicParticles = psGo.AddComponent<ParticleSystem>();
                var main = relicParticles.main;
                main.startColor = new Color(0.25f, 0.90f, 0.45f, 0.9f);
                main.startSize = 0.2f;
                main.startSpeed = 1.2f;
                main.maxParticles = 50;

                var emission = relicParticles.emission;
                emission.rateOverTime = 20;

                var shape = relicParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.5f;
            }
        }

        private void Update()
        {
            // Hover animation & gentle rotation
            hoverTime += Time.deltaTime;
            float hoverY = Mathf.Sin(hoverTime * 2.5f) * 0.12f;
            transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);

            // Auto-collection logic
            if (playerTransform == null)
            {
                PlayerStats stats = FindFirstObjectByType<PlayerStats>();
                if (stats != null) playerTransform = stats.transform;
            }

            if (playerTransform != null)
            {
                Vector3 targetPos = playerTransform.position + Vector3.up * 1.0f;
                float dist = Vector3.Distance(transform.position, targetPos);

                if (dist <= magnetRadius)
                {
                    isBeingCollected = true;
                }

                if (isBeingCollected)
                {
                    currentAttractionSpeed += Time.deltaTime * 30.0f; // Smooth acceleration
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, currentAttractionSpeed * Time.deltaTime);

                    if (dist <= autoCollectRadius)
                    {
                        Collect();
                        return;
                    }
                }
                else
                {
                    transform.position = startPosition + new Vector3(0, hoverY + 0.3f, 0);
                }
            }
            else
            {
                transform.position = startPosition + new Vector3(0, hoverY + 0.3f, 0);
            }
        }

        private bool isCollected = false;

        private void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            if (goldAmount > 0)
            {
                InventoryManager.Instance?.AddGold(goldAmount);
            }
            else if (item != null)
            {
                InventoryManager.Instance?.AddItem(item, quantity);
            }

            Destroy(gameObject);
        }
    }
}
