using UnityEngine;

namespace GameKari.Battle
{
    public static class DummySkillCatalog
    {
        private const string SlashAssetPath = "Battle/Skills/s1_slash";
        private const string PierceAssetPath = "Battle/Skills/s2_pierce";
        private const string TwinHitAssetPath = "Battle/Skills/s3_twin_hit";
        private const string FocusAssetPath = "Battle/Skills/s4_focus";

        public static SkillData GetSlash()
        {
            return LoadSkillAsset(SlashAssetPath) ?? CreatePersonalDamageSkill(
                "s1",
                "Slash",
                "Attack enemy front top.",
                SkillTargetPattern.FrontTopEnemy,
                20,
                0
            );
        }

        public static SkillData GetPierce()
        {
            return LoadSkillAsset(PierceAssetPath) ?? CreatePersonalDamageSkill(
                "s2",
                "Pierce",
                "Attack enemy front bottom.",
                SkillTargetPattern.FrontBottomEnemy,
                20,
                1
            );
        }

        public static SkillData GetTwinHit()
        {
            return LoadSkillAsset(TwinHitAssetPath) ?? CreateLinkDamageSkill(
                "s3",
                "TwinHit",
                "Temporary Knight link skill. Attack both front enemies with Rogue.",
                SkillTargetPattern.BothFrontEnemies,
                15,
                2,
                "rogue"
            );
        }

        public static SkillData GetFocus()
        {
            return LoadSkillAsset(FocusAssetPath) ?? CreateSelfBuffSkill(
                "s4",
                "Focus",
                "Apply AttackUp to self.",
                BuffType.AttackUp,
                2,
                0
            );
        }

        private static SkillData LoadSkillAsset(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                return null;
            }

            return Resources.Load<SkillData>(resourcesPath);
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
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();

            skill.SkillId = skillId;
            skill.SkillName = skillName;
            skill.Description = description;
            skill.TargetPattern = targetPattern;
            skill.SkillKind = skillKind;
            skill.MpCost = mpCost;
            skill.LinkPartnerCharacterId = linkPartnerCharacterId;
            skill.Damage = damage;
            skill.EffectType = effectType;
            skill.EffectTarget = effectTarget;
            skill.BuffType = buffType;
            skill.BuffTurns = buffTurns;

            return skill;
        }
    }
}
