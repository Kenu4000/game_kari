using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    [System.Serializable]
    public class EnemyActionSlot
    {
        public SkillData Skill;
        public int Weight = 1;
    }

    [CreateAssetMenu(
        fileName = "CharacterData",
        menuName = "GameKari/Battle/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public int MaxHP = 100;
        public int MaxMP = 4;

        public int Speed = 10;
        public Sprite FaceIcon;
        public Sprite StandingSprite;
        public List<SkillData> DefaultSkills = new();
        public List<EnemyActionSlot> EnemyActionSlots = new();
    }
}
