namespace GameKari.Battle
{
    public static class DummySkillFactory
    {
        public static void AddDefaultSkills(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            unit.Skills.Add(CreatePersonalDamageSkill(
                "s1",
                "Slash",
                "Attack enemy front top.",
                SkillTargetPattern.FrontTopEnemy,
                20
            ));

            unit.Skills.Add(CreatePersonalDamageSkill(
                "s2",
                "Pierce",
                "Attack enemy front bottom.",
                SkillTargetPattern.FrontBottomEnemy,
                20
            ));

            unit.Skills.Add(CreateLinkDamageSkill(
                "s3",
                "TwinHit",
                "Temporary link skill. Attack both front enemies.",
                SkillTargetPattern.BothFrontEnemies,
                15,
                2,
                1
            ));

            // Temporary buff test skill.
            // Wave is intentionally parked until skill slot/UI handling is expanded.
            unit.Skills.Add(CreateSelfBuffSkill(
                "s4",
                "Focus",
                "Apply AttackUp to self.",
                BuffType.AttackUp,
                2,
                2
            ));
        }

        private static SkillData CreatePersonalDamageSkill(
            string skillId,
            string skillName,
            string description,
            SkillTargetPattern targetPattern,
            int damage,
            int cooldownTurns = 0)
        {
            return CreateSkill(
                skillId,
                skillName,
                description,
                targetPattern,
                damage,
                SkillEffectType.None,
                BuffType.AttackUp,
                0,
                cooldownTurns,
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
            int cooldownTurns,
            int linkCooldownTurns)
        {
            return CreateSkill(
                skillId,
                skillName,
                description,
                targetPattern,
                damage,
                SkillEffectType.None,
                BuffType.AttackUp,
                0,
                cooldownTurns,
                linkCooldownTurns,
                SkillKind.Link
            );
        }

        private static SkillData CreateSelfBuffSkill(
            string skillId,
            string skillName,
            string description,
            BuffType buffType,
            int buffTurns,
            int cooldownTurns)
        {
            return CreateSkill(
                skillId,
                skillName,
                description,
                SkillTargetPattern.Self,
                0,
                SkillEffectType.ApplyBuff,
                buffType,
                buffTurns,
                cooldownTurns,
                0,
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
            SkillEffectType effectType = SkillEffectType.None,
            BuffType buffType = BuffType.AttackUp,
            int buffTurns = 0,
            int cooldownTurns = 0,
            int linkCooldownTurns = 0,
            SkillKind skillKind = SkillKind.Personal,
            SkillEffectTargetType effectTarget = SkillEffectTargetType.Self)
        {
            return new SkillData
            {
                SkillId = skillId,
                SkillName = skillName,
                Description = description,
                TargetPattern = targetPattern,
                SkillKind = skillKind,
                CooldownTurns = cooldownTurns,
                LinkCooldownTurns = linkCooldownTurns,

                Damage = damage,
                EffectType = effectType,
                EffectTarget = effectTarget,
                BuffType = buffType,
                BuffTurns = buffTurns
            };
        }
    }
}
