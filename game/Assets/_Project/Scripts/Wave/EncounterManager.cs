using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Wave
{
    public class EncounterManager : MonoBehaviour
    {
        public static EncounterManager Instance { get; private set; }

        public EncounterZone ActiveZone { get; private set; }
        public List<EncounterZone> AllZones { get; private set; } = new List<EncounterZone>();

        public string StatusBannerText { get; private set; } = "";
        public float StatusBannerTimer { get; private set; } = 0f;

        public event Action<string> OnBannerTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            RegisterAllZonesInScene();
        }

        public void RegisterAllZonesInScene()
        {
            AllZones.Clear();
            EncounterZone[] zones = FindObjectsByType<EncounterZone>(FindObjectsSortMode.None);
            foreach (var z in zones)
            {
                AllZones.Add(z);
                z.OnEncounterStarted += HandleZoneStarted;
                z.OnEncounterCompleted += HandleZoneCompleted;
            }
        }

        private void HandleZoneStarted(EncounterZone zone)
        {
            ActiveZone = zone;
        }

        private void HandleZoneCompleted(EncounterZone zone)
        {
            if (ActiveZone == zone) ActiveZone = null;
        }

        public void TriggerBanner(string text)
        {
            StatusBannerText = text;
            StatusBannerTimer = 3.0f;
            OnBannerTriggered?.Invoke(text);
        }

        private void Update()
        {
            if (StatusBannerTimer > 0)
            {
                StatusBannerTimer -= Time.deltaTime;
            }
        }
    }
}
