using UnityEngine;

namespace Roguelite.Core
{
    public enum CharacterType
    {
        Knight,
        Mage,
        Druid
    }

    public class GameSettings : MonoBehaviour
    {
        private static GameSettings instance;
        public static GameSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("GameSettings");
                    instance = obj.AddComponent<GameSettings>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        public CharacterType SelectedCharacter { get; set; } = CharacterType.Knight;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
