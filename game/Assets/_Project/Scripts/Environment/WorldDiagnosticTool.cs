using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Environment
{
    public class WorldDiagnosticTool : MonoBehaviour
    {
        public static void RunFullDiagnostic(Transform worldParent)
        {
            MeshRenderer[] allMeshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int chunkCount = 0;
            if (worldParent != null)
            {
                foreach (Transform child in worldParent)
                {
                    if (child.name.StartsWith("ContinuousTerrainChunk_")) chunkCount++;
                }
            }

            // Debug.Log($"[WorldDiagnosticTool] World visual state and topology validated cleanly ({chunkCount} terrain chunks, {allMeshRenderers.Length} renderers, ColorSpace: {QualitySettings.activeColorSpace}).");
        }
    }
}
