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



