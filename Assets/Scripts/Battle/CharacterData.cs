using UnityEngine;

namespace GameKari.Battle
{
    [CreateAssetMenu(
        fileName = "CharacterData",
        menuName = "GameKari/Battle/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public int MaxHP = 100;

        public int Speed = 10;
        public Sprite FaceIcon;
        public Sprite StandingSprite;
    }
}
