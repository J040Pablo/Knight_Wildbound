using UnityEngine;

namespace Roguelite.Environment
{
    /// <summary>
    /// ScriptableObject containing all atmosphere / lighting settings for one biome.
    /// Create .asset instances via Assets > Create > Roguelite > Environment Theme.
    /// At runtime the builder can also create instances via ScriptableObject.CreateInstance.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelite/Environment Theme", fileName = "Theme_")]
    public class EnvironmentThemeData : ScriptableObject
    {
        [Header("Identity")]
        public string biomeName = "Unknown Biome";

        [Header("Sun / Directional Light")]
        public Color  sunColor     = new Color(1.0f, 0.90f, 0.75f);
        public float  sunIntensity = 1.2f;

        [Header("Ambient")]
        public Color ambientColor = new Color(0.40f, 0.40f, 0.48f);

        [Header("Fog")]
        public float fogDensity   = 0.012f;
        public Color fogColor     = new Color(0.40f, 0.40f, 0.48f);

        [Header("Terrain Tint")]
        public Color groundColor  = new Color(0.38f, 0.32f, 0.22f);
        public Color hillColor    = new Color(0.32f, 0.28f, 0.18f);

        [Header("Transition")]
        [Range(0.5f, 4f)]
        public float transitionDuration = 1.8f;

        // ── Factory helpers ────────────────────────────────────────────
        public static EnvironmentThemeData Create(
            string name,
            Color sun, float sunInt,
            Color ambient,
            float fogDens, Color fog,
            Color ground, Color hill,
            float transition = 1.8f)
        {
            var d = CreateInstance<EnvironmentThemeData>();
            d.biomeName          = name;
            d.sunColor           = sun;
            d.sunIntensity       = sunInt;
            d.ambientColor       = ambient;
            d.fogDensity         = fogDens;
            d.fogColor           = fog;
            d.groundColor        = ground;
            d.hillColor          = hill;
            d.transitionDuration = transition;
            return d;
        }
    }
}
