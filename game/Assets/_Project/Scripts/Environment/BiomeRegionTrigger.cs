using UnityEngine;
using System;

namespace Roguelite.Environment
{
    public class BiomeRegionTrigger : MonoBehaviour
    {
        [Header("Region Info")]
        [SerializeField] private string regionName = "Unknown Region";

        [Header("Atmosphere Configuration")]
        [SerializeField] private Color sunColor = new Color(1.0f, 0.9f, 0.75f);
        [SerializeField] private float sunIntensity = 1.2f;
        [SerializeField] private Color ambientColor = new Color(0.4f, 0.4f, 0.5f);
        [SerializeField] private float fogDensity = 0.015f;
        [SerializeField] private Color fogColor = new Color(0.4f, 0.4f, 0.5f);

        public string RegionName => regionName;
        public static string CurrentRegionName { get; private set; } = "Ruins";

        public static event Action<string> OnRegionChanged;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Roguelite.Player.PlayerController>() != null)
            {
                ApplyRegionSettings();
            }
        }

        public void ApplyRegionSettings()
        {
            CurrentRegionName = regionName;
            OnRegionChanged?.Invoke(regionName);

            // Update Directional Sun Light
            GameObject sun = GameObject.Find("Directional Sun Light");
            if (sun != null)
            {
                Light l = sun.GetComponent<Light>();
                if (l != null)
                {
                    l.color = sunColor;
                    l.intensity = sunIntensity;
                }
            }

            // Update Ambient & Fog Settings
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
        }

        public void SetupRegion(string name, Color sColor, float sIntensity, Color aColor, float fDensity, Color fColor)
        {
            regionName = name;
            sunColor = sColor;
            sunIntensity = sIntensity;
            ambientColor = aColor;
            fogDensity = fDensity;
            fogColor = fColor;
        }
    }
}
