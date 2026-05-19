using System.Collections.Generic;

namespace GameKari.Battle
{
    public static class DummySkillCatalog
    {
        public static List<SkillData> CreateDefaultSkills()
        {
            return new List<SkillData>
            {
                CreatePersonalDamageSkill(
                    "s1",
                    "Slash",
                    "Attack enemy front top.",
                    SkillTargetPattern.FrontTopEnemy,
                    20,
                    0
                ),

                CreatePersonalDamageSkill(
                    "s2",
                    "Pierce",
                    "Attack enemy front bottom.",
                    SkillTargetPattern.FrontBottomEnemy,
                    20,
                    1
                ),

                CreateLinkDamageSkill(
                    "s3",
                    "TwinHit",
                    "Temporary link skill. Attack both front enemies.",
                    SkillTargetPattern.BothFrontEnemies,
                    15,
                    2,
                    "rogue"
                ),

                CreateSelfBuffSkill(
                    "s4",
                    "Focus",
                    "Apply AttackUp to self.",
                    BuffType.AttackUp,
                    2,
                    0
                )
            };
        }

        private static SkillData CreatePersonalDamageSkill(
            string skillId,
            string skillName,
            string description,
            SkillTargetPattern targetPattern,
            int damage,
            int mpCost)
        {
            return CreateSkill(
                skillId,
                skillName,
                description,
                targetPattern,
                damage,
                mpCost,
                SkillEffectType.None,
                BuffType.AttackUp,
                0,
                SkillKind.Personal
            );
        }

        private static SkillData CreateLinkDamageSkill(
            string skillId,
            string skillName,
            string description,
            SkillTargetPattern targetPattern,
            int damage,
            int mpCost,
            string linkPartnerCharacterId)
        {
            return CreateSkill(
                skillId,
                skillName,
                description,
                targetPattern,
                damage,
                mpCost,
                SkillEffectType.None,
                BuffType.AttackUp,
                0,
                SkillKind.Link,
                SkillEffectTargetType.Self,
                linkPartnerCharacterId
            );
        }

        private static SkillData CreateSelfBuffSkill(
            string skillId,
            string skillName,
            string description,
            BuffType buffType,
            int buffTurns,
            int mpCost)
        {
            return CreateSkill(
                skillId,
                skillName,
                description,
                SkillTargetPattern.Self,
                0,
                mpCost,
                SkillEffectType.ApplyBuff,
                buffType,
                buffTurns,
                SkillKind.Personal,
                SkillEffectTargetType.Self
            );
        }

        private static SkillData CreateSkill(
            string skillId,
            string skillName,
            string description,
            SkillTargetPattern targetPattern,
            int damage,
            int mpCost,
            SkillEffectType effectType = SkillEffectType.None,
            BuffType buffType = BuffType.AttackUp,
            int buffTurns = 0,
            SkillKind skillKind = SkillKind.Personal,
            SkillEffectTargetType effectTarget = SkillEffectTargetType.Self,
            string linkPartnerCharacterId = "")
        {
            return new SkillData
            {
                SkillId = skillId,
                SkillName = skillName,
                Description = description,
                TargetPattern = targetPattern,
                SkillKind = skillKind,
                MpCost = mpCost,
                LinkPartnerCharacterId = linkPartnerCharacterId,
                CooldownTurns = 0,
                LinkCooldownTurns = 0,

                Damage = damage,
                EffectType = effectType,
                EffectTarget = effectTarget,
                BuffType = buffType,
                BuffTurns = buffTurns
            };
        }
    }
}
