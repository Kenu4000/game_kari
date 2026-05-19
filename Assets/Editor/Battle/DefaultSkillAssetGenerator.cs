using System.IO;
using GameKari.Battle;
using UnityEditor;
using UnityEngine;

namespace GameKari.Battle.Editor
{
    public static class DefaultSkillAssetGenerator
    {
        private const string SkillAssetDirectory = "Assets/Resources/Battle/Skills";

        [MenuItem("Tools/GameKari/Battle/Generate Default Skill Assets")]
        public static void GenerateDefaultSkillAssets()
        {
            EnsureDirectory(SkillAssetDirectory);

            CreateOrUpdateSkill(
                "s1_slash",
                "s1",
                "Slash",
                "Attack opponent front top.",
                SkillTargetPattern.FrontTopOpponent,
                SkillKind.Personal,
                0,
                string.Empty,
                20,
                SkillEffectType.None,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                0
            );

            CreateOrUpdateSkill(
                "s2_pierce",
                "s2",
                "Pierce",
                "Attack opponent front bottom.",
                SkillTargetPattern.FrontBottomOpponent,
                SkillKind.Personal,
                1,
                string.Empty,
                20,
                SkillEffectType.None,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                0
            );

            CreateOrUpdateSkill(
                "s3_twin_hit",
                "s3",
                "TwinHit",
                "Temporary Knight link skill. Attack both front opponents with Rogue.",
                SkillTargetPattern.BothFrontOpponents,
                SkillKind.Link,
                2,
                "rogue",
                15,
                SkillEffectType.None,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                0
            );

            CreateOrUpdateSkill(
                "s4_focus",
                "s4",
                "Focus",
                "Apply AttackUp to self.",
                SkillTargetPattern.Self,
                SkillKind.Personal,
                0,
                string.Empty,
                0,
                SkillEffectType.ApplyBuff,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                2
            );

            CreateOrUpdateSkill(
                "enemy_claw",
                "enemy_claw",
                "Claw",
                "Attack the ally at the same grid position.",
                SkillTargetPattern.SameGridPosOpponent,
                SkillKind.Personal,
                0,
                string.Empty,
                60,
                SkillEffectType.None,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                0
            );

            CreateOrUpdateSkill(
                "enemy_arrow",
                "enemy_arrow",
                "Arrow",
                "Attack opponent front top.",
                SkillTargetPattern.FrontTopOpponent,
                SkillKind.Personal,
                0,
                string.Empty,
                45,
                SkillEffectType.None,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                0
            );

            CreateOrUpdateSkill(
                "enemy_bite",
                "enemy_bite",
                "Bite",
                "Attack opponent front bottom.",
                SkillTargetPattern.FrontBottomOpponent,
                SkillKind.Personal,
                0,
                string.Empty,
                60,
                SkillEffectType.None,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                0
            );

            CreateOrUpdateSkill(
                "enemy_hex",
                "enemy_hex",
                "Hex",
                "Attack all opponents.",
                SkillTargetPattern.AllOpponents,
                SkillKind.Personal,
                0,
                string.Empty,
                25,
                SkillEffectType.None,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                0
            );

            CreateOrUpdateSkill(
                "enemy_strike",
                "enemy_strike",
                "Strike",
                "Attack the ally at the same grid position.",
                SkillTargetPattern.SameGridPosOpponent,
                SkillKind.Personal,
                0,
                string.Empty,
                60,
                SkillEffectType.None,
                SkillEffectTargetType.Self,
                BuffType.AttackUp,
                0
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameKari] Default SkillData assets generated.");
        }

        private static void CreateOrUpdateSkill(
            string assetName,
            string skillId,
            string skillName,
            string description,
            SkillTargetPattern targetPattern,
            SkillKind skillKind,
            int mpCost,
            string linkPartnerCharacterId,
            int damage,
            SkillEffectType effectType,
            SkillEffectTargetType effectTarget,
            BuffType buffType,
            int buffTurns)
        {
            string path = $"{SkillAssetDirectory}/{assetName}.asset";
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);

            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(skill, path);
            }

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

            EditorUtility.SetDirty(skill);
        }

        private static void EnsureDirectory(string directory)
        {
            if (AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string[] parts = directory.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
