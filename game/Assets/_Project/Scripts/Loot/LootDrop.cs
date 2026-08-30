using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;

namespace Roguelite.Loot
{
    public static class LootDrop
    {
        public static ItemPickup SpawnSingle(Vector3 origin, ItemData item, int quantity = 1)
        {
            if (item == null) return null;

            Vector3 spawnPos = origin + GetRandomOffset();
            ItemPickup pickup = ItemPickup.Spawn(spawnPos, item, quantity);
            ApplyArcPhysics(pickup.gameObject);
            return pickup;
        }

        public static ItemPickup SpawnGold(Vector3 origin, int amount)
        {
            if (amount <= 0) return null;

            Vector3 spawnPos = origin + GetRandomOffset();
            ItemPickup pickup = ItemPickup.SpawnGold(spawnPos, amount);
            ApplyArcPhysics(pickup.gameObject);
            return pickup;
        }

        public static List<ItemPickup> SpawnFromResult(LootResult result, Vector3 origin)
        {
            List<ItemPickup> spawned = new List<ItemPickup>();
            if (result == null) return spawned;

            // Spawn Gold
            if (result.goldAmount > 0)
            {
                ItemPickup goldPickup = SpawnGold(origin, result.goldAmount);
                if (goldPickup != null) spawned.Add(goldPickup);
            }

            // Spawn Items
            if (result.droppedItems != null)
            {
                foreach (var item in result.droppedItems)
                {
                    if (item != null)
                    {
                        ItemPickup itemPickup = SpawnSingle(origin, item, 1);
                        if (itemPickup != null) spawned.Add(itemPickup);
                    }
                }
            }

            return spawned;
        }

        private static Vector3 GetRandomOffset()
        {
            Vector2 disk = Random.insideUnitCircle * 1.5f;
            return new Vector3(disk.x, 0.2f, disk.y);
        }

        private static void ApplyArcPhysics(GameObject obj)
        {
            if (obj == null) return;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null) rb = obj.AddComponent<Rigidbody>();

            rb.mass = 0.5f;
            rb.linearDamping = 1.0f;
            rb.angularDamping = 2.0f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            Vector3 popDir = new Vector3(Random.Range(-1f, 1f), 2.5f, Random.Range(-1f, 1f)).normalized;
            rb.AddForce(popDir * Random.Range(3.0f, 5.0f), ForceMode.Impulse);
        }
    }
}
