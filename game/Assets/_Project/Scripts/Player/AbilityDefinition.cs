using UnityEngine;
using Roguelite.Progression;

namespace Roguelite.Player
{
    [CreateAssetMenu(fileName = "NewAbilityDefinition", menuName = "Roguelite/Combat/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Ability Identification")]
        public AbilityId abilityId = AbilityId.None;
        public string abilityName = "Special Ability";
        [TextArea(2, 4)]
        public string description = "Ability Description";

        [Header("Stats")]
        public float cooldown = 15.0f;
        public float damage = 50.0f;
        public float radius = 5.0f;
        public float staminaCost = 0.0f;

        [Header("Visual & Effects")]
        public GameObject effectPrefab;
    }
}
