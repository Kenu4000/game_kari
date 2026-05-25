using UnityEngine;

namespace GameKari.Battle
{
    public enum SkillCategory
    {
        Attack,
        Heal,
        Change,
        Defense
    }
    public enum SkillTargetPattern
    {
        SameGridPosOpponent,
        FrontTopOpponent,
        FrontBottomOpponent,
        BothFrontOpponents,
        AllOpponents,
        Self
    }

    public enum SkillEffectType
    {
        None,
        ApplyBuff
    }

    public enum SkillEffectTargetType
    {
        Self,
        Target,
        AllAllies,
        AllEnemies
    }

    public enum SkillKind
    {
        Personal,
        Link
    }

    [CreateAssetMenu(
        fileName = "SkillData",
        menuName = "GameKari/Battle/Skill Data")]
    public class SkillData : ScriptableObject
    {
        public string SkillId;
        public string SkillName;
        [TextArea] public string Description;
        public SkillCategory Category = SkillCategory.Attack;
        public SkillTargetPattern TargetPattern;
        public SkillKind SkillKind;
        public int MpCost;
        public string LinkPartnerCharacterId;

        public int Damage;
        public SkillEffectType EffectType;
        public SkillEffectTargetType EffectTarget;
        public BuffType BuffType;
        public int BuffTurns;
    }
}





