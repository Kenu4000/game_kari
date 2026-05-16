using UnityEngine;

namespace GameKari.Battle
{
    public enum SkillTargetPattern
    {
        FrontTopEnemy,
        FrontBottomEnemy,
        BothFrontEnemies,
        AllEnemies
    }

    [System.Serializable]
    public class SkillData
    {
        public string SkillId;
        public string SkillName;
        [TextArea] public string Description;
        public SkillTargetPattern TargetPattern;
        public int MpCost;
        public int Damage;
    }
}
