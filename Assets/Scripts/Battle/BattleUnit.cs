using System.Collections.Generic;

namespace GameKari.Battle
{
    public enum BuffType
    {
        AttackUp,
        DefenseUp,
        AttackDown,
        DefenseDown
    }

    [System.Serializable]
    public class BuffState
    {
        public BuffType Type;
        public int RemainingTurns;
    }

    [System.Serializable]
    public class SkillCooldownState
    {
        public string SkillId;
        public int RemainingTurns;
    }

    [System.Serializable]
    public class BattleUnit
    {
        public CharacterData Data;
        public int CurrentHP;
        public int CurrentMP;
        public bool IsDead;
        public bool IsAlly;
        public GridPos GridPos;
        public readonly List<SkillData> Skills = new();
        public readonly List<BuffState> Buffs = new();
        public readonly List<SkillCooldownState> SkillCooldowns = new();
        public int LinkCooldownRemaining;

        public BattleUnit(CharacterData data)
        {
            Data = data;
            CurrentHP = data.MaxHP;
            CurrentMP = data.MaxMP;
        }

        public string Name => Data.DisplayName;
    }
}

