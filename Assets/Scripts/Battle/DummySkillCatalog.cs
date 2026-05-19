using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    public static class DummySkillCatalog
    {
        private const string SlashAssetPath = "Battle/Skills/s1_slash";
        private const string PierceAssetPath = "Battle/Skills/s2_pierce";
        private const string TwinHitAssetPath = "Battle/Skills/s3_twin_hit";
        private const string FocusAssetPath = "Battle/Skills/s4_focus";

        public static List<SkillData> CreateSkillsForUnit(BattleUnit unit)
        {
            string characterId = unit == null || unit.Data == null ? string.Empty : unit.Data.Id;
            return CreateSkillsForCharacter(characterId);
        }

        public static List<SkillData> CreateSkillsForCharacter(string characterId)
        {
            switch (characterId)
            {
                case "knight":
                    return new List<SkillData>
                    {
                        CreateSlash(),
                        CreatePierce(),
                        CreateTwinHit(),
                        CreateFocus()
                    };

                case "mage":
                    return new List<SkillData>
                    {
                        CreateSlash(),
                        CreatePierce(),
                        CreateFocus()
                    };

                case "cleric":
                    return new List<SkillData>
                    {
                        CreateSlash(),
                        CreateFocus()
                    };

                case "rogue":
                    return new List<SkillData>
                    {
                        CreateSlash(),
                        CreatePierce(),
                        CreateFocus()
                    };

                default:
                    return new List<SkillData>
                    {
                        CreateSlash()
                    };
            }
        }

        private static SkillData CreateSlash()
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

        private static SkillData CreatePierce()
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

        private static SkillData CreateTwinHit()
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

        private static SkillData CreateFocus()
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
