using System.Collections.Generic;
using UnityEngine;

namespace GameKari.Battle
{
    public enum SkillAnimationStepType
    {
        Pose,
        Move,
        JumpMove,
        Wait,
        Return,
        ShakeTarget,
        SpawnProjectile,
        MoveProjectile,
        JumpProjectile,
        HideProjectile
    }

    public enum SkillAnimationAnchor
    {
        Current,
        Original,
        Caster,
        Target,
        ScreenCenter,
        ScreenTop,
        ScreenBottom
    }

    [System.Serializable]
    public class SkillAnimationStep
    {
        public SkillAnimationStepType StepType = SkillAnimationStepType.Pose;
        public Sprite Sprite;
        public Sprite ProjectileSprite;
        public float Duration = 0.1f;
        public SkillAnimationAnchor FromAnchor = SkillAnimationAnchor.Current;
        public SkillAnimationAnchor ToAnchor = SkillAnimationAnchor.Current;
        public Vector2 FromOffset;
        public Vector2 ToOffset;
        public float JumpHeight = 0f;
        public float Scale = 1f;
        public float RotationZ = 0f;
        public float ProjectileScale = 1f;
        public float ProjectileRotationZ = 0f;
        public float ShakeDistance = 8f;
        public int ShakeCount = 2;
    }

    [CreateAssetMenu(
        fileName = "SkillAnimationData",
        menuName = "GameKari/Battle/Skill Animation Data")]
    public class SkillAnimationData : ScriptableObject
    {
        public List<SkillAnimationStep> Steps = new();
        public bool RestoreSpriteAtEnd = true;
        public bool RestoreTransformAtEnd = true;
        public bool HideProjectileAtEnd = true;
    }
}
