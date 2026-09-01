using System.Collections.Generic;
using UnityEngine;
using Roguelite.Data;

namespace Roguelite.Core.Managers
{
    public class EventSpawner : MonoBehaviour
    {
        private readonly List<GameObject> activeEvents = new List<GameObject>();

        public GameObject SpawnEvent(WorldEventDefinition eventDef, Vector3 position)
        {
            if (eventDef == null) return null;

            Vector3 validPos = GetValidPosition(position);
            GameObject eventGO = null;

            if (eventDef.eventPrefab != null)
            {
                eventGO = Instantiate(eventDef.eventPrefab, validPos, Quaternion.identity);
            }
            else
            {
                eventGO = BuildProceduralEventVisuals(eventDef.eventName, validPos);
            }

            activeEvents.Add(eventGO);
            // Debug.Log($"[EventSpawner] Spawned event '{eventDef.eventName}' at {validPos}");
            return eventGO;
        }

        private GameObject BuildProceduralEventVisuals(string eventName, Vector3 pos)
        {
            GameObject root = new GameObject($"WorldEvent_{eventName}");
            root.transform.position = pos;

            if (eventName.Contains("Fairy Ritual") || eventName.Contains("Ritual"))
            {
                GameObject obelisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obelisk.name = "RitualStone";
                obelisk.transform.parent = root.transform;
                obelisk.transform.localPosition = Vector3.zero;
                obelisk.transform.localScale = new Vector3(1.2f, 2.5f, 1.2f);
                Renderer r = obelisk.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.6f, 0.2f, 0.8f);
            }
            else if (eventName.Contains("Corrupted Tree") || eventName.Contains("Shrine"))
            {
                GameObject shrine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shrine.name = "CorruptedShrine";
                shrine.transform.parent = root.transform;
                shrine.transform.localPosition = Vector3.zero;
                shrine.transform.localScale = new Vector3(1.8f, 2.2f, 1.8f);
                Renderer r = shrine.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.2f, 0.15f, 0.18f);
            }
            else if (eventName.Contains("Hunter") || eventName.Contains("Camp"))
            {
                GameObject tent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tent.name = "HunterTent";
                tent.transform.parent = root.transform;
                tent.transform.localPosition = Vector3.zero;
                tent.transform.localScale = new Vector3(2.5f, 1.2f, 2.5f);
                Renderer r = tent.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.45f, 0.35f, 0.25f);
            }
            else if (eventName.Contains("Caravan"))
            {
                GameObject wagon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wagon.name = "LostWagon";
                wagon.transform.parent = root.transform;
                wagon.transform.localPosition = Vector3.zero;
                wagon.transform.localScale = new Vector3(2.0f, 1.5f, 3.2f);
                Renderer r = wagon.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.38f, 0.22f, 0.12f);
            }
            else if (eventName.Contains("Relic Grove"))
            {
                GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                altar.name = "RelicAltar";
                altar.transform.parent = root.transform;
                altar.transform.localPosition = Vector3.zero;
                altar.transform.localScale = new Vector3(1.4f, 0.8f, 1.4f);
                Renderer r = altar.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.2f, 0.85f, 0.5f);
            }

            return root;
        }

        private Vector3 GetValidPosition(Vector3 preferredPos)
        {
            Vector3 rayStart = preferredPos + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 25f))
            {
                return hit.point + Vector3.up * 0.1f;
            }
            return preferredPos;
        }
    }
}
