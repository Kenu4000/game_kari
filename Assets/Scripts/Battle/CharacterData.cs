using UnityEngine;

namespace GameKari.Battle
{
    [System.Serializable]
    public class CharacterData
    {
        public string Id;
        public string DisplayName;
        public int MaxHP = 100;

        public int Speed = 10;
        public Sprite FaceIcon;
        public Sprite StandingSprite;
    }
}


