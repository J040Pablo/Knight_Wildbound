using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    [Serializable]
    public class LandmarkEntry
    {
        public string landmarkName;
        public LandmarkType type;
        public Vector3 position;
        public float discoveryRadius = 15f;
        public bool isDiscovered = false;
        public GameObject associatedObject;
    }

    public class ForestLandmarkManager : MonoBehaviour
    {
        public static ForestLandmarkManager Instance { get; private set; }

        private readonly List<LandmarkEntry> registeredLandmarks = new List<LandmarkEntry>();
        public IReadOnlyList<LandmarkEntry> Landmarks => registeredLandmarks;

        public event Action<LandmarkEntry> OnLandmarkDiscovered;

        private Transform playerTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ClearLandmarks()
        {
            registeredLandmarks.Clear();
        }

        public LandmarkEntry RegisterLandmark(string name, LandmarkType type, Vector3 worldPos, GameObject obj = null, float discoveryRadius = 16f)
        {
            var entry = new LandmarkEntry
            {
                landmarkName = name,
                type = type,
                position = worldPos,
                discoveryRadius = discoveryRadius,
                isDiscovered = false,
                associatedObject = obj
            };

            registeredLandmarks.Add(entry);
            return entry;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
                if (playerTransform == null) return;
            }

            Vector3 playerPos = playerTransform.position;

            for (int i = 0; i < registeredLandmarks.Count; i++)
            {
                var lm = registeredLandmarks[i];
                if (lm.isDiscovered) continue;

                float distSqr = (playerPos - lm.position).sqrMagnitude;
                if (distSqr <= lm.discoveryRadius * lm.discoveryRadius)
                {
                    lm.isDiscovered = true;
                    OnLandmarkDiscovered?.Invoke(lm);

                    if (EncounterManager.Instance != null)
                    {
                        EncounterManager.Instance.TriggerBanner($"📍 DISCOVERED: {lm.landmarkName.ToUpper()}");
                    }
                    // Debug.Log($"[ForestLandmarkManager] Discovered landmark: {lm.landmarkName} at {lm.position}");
                }
            }
        }

        public LandmarkEntry GetNearestLandmark(Vector3 position, out float distance)
        {
            LandmarkEntry nearest = null;
            float minDist = float.MaxValue;

            foreach (var lm in registeredLandmarks)
            {
                float dist = Vector3.Distance(position, lm.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = lm;
                }
            }

            distance = minDist;
            return nearest;
        }
    }
}
