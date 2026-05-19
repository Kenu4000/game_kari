using System;
using UnityEngine;

namespace GameKari.Battle
{
    public static class DefaultSkillAssetProvider
    {
        private const string SlashAssetPath = "Battle/Skills/s1_slash";
        private const string PierceAssetPath = "Battle/Skills/s2_pierce";
        private const string TwinHitAssetPath = "Battle/Skills/s3_twin_hit";
        private const string FocusAssetPath = "Battle/Skills/s4_focus";

        private const string EnemyClawAssetPath = "Battle/Skills/enemy_claw";
        private const string EnemyArrowAssetPath = "Battle/Skills/enemy_arrow";
        private const string EnemyBiteAssetPath = "Battle/Skills/enemy_bite";
        private const string EnemyHexAssetPath = "Battle/Skills/enemy_hex";
        private const string EnemyStrikeAssetPath = "Battle/Skills/enemy_strike";

        public static SkillData GetSlash()
        {
            return LoadRequiredSkillAsset(SlashAssetPath);
        }

        public static SkillData GetPierce()
        {
            return LoadRequiredSkillAsset(PierceAssetPath);
        }

        public static SkillData GetTwinHit()
        {
            return LoadRequiredSkillAsset(TwinHitAssetPath);
        }

        public static SkillData GetFocus()
        {
            return LoadRequiredSkillAsset(FocusAssetPath);
        }

        public static SkillData GetEnemyClaw()
        {
            return LoadRequiredSkillAsset(EnemyClawAssetPath);
        }

        public static SkillData GetEnemyArrow()
        {
            return LoadRequiredSkillAsset(EnemyArrowAssetPath);
        }

        public static SkillData GetEnemyBite()
        {
            return LoadRequiredSkillAsset(EnemyBiteAssetPath);
        }

        public static SkillData GetEnemyHex()
        {
            return LoadRequiredSkillAsset(EnemyHexAssetPath);
        }

        public static SkillData GetEnemyStrike()
        {
            return LoadRequiredSkillAsset(EnemyStrikeAssetPath);
        }

        private static SkillData LoadRequiredSkillAsset(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                throw new InvalidOperationException("Skill asset path is empty.");
            }

            SkillData skill = Resources.Load<SkillData>(resourcesPath);
            if (skill != null)
            {
                return skill;
            }

            throw new InvalidOperationException($"SkillData asset not found at Resources path: {resourcesPath}");
        }
    }
}
