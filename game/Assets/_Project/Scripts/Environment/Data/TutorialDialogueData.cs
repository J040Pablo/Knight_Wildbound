using UnityEngine;

namespace Roguelite.Environment.Data
{
    [CreateAssetMenu(fileName = "TutorialDialogueData", menuName = "Roguelite/Dialogue/TutorialDialogueData")]
    public class TutorialDialogueData : ScriptableObject
    {
        [Header("Speaker Info")]
        public string speakerName = "KING";

        [Header("Dialogue Content (English)")]
        [TextArea(3, 5)]
        public string introLine = "Hey you... Don't you think you've rested long enough?\n\nThe kingdom needs a hero. Get up and choose your weapon. Your destiny awaits.";

        [TextArea(2, 4)]
        public string weaponPromptLine = "The kingdom needs a hero. Pick your weapon from the pedestals!";

        [TextArea(2, 4)]
        public string postSelectionLine = "May fortune favor your blade, hero! Proceed through the gate.";
    }
}
