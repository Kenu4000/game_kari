$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$skillPath = 'Assets/Scripts/Battle/SkillData.cs'
$animDir = 'Assets/Scripts/Battle/Animation'
$dataPath = Join-Path $animDir 'SkillAnimationData.cs'
$playerPath = Join-Path $animDir 'SkillAnimationPlayer.cs'

if (!(Test-Path $managerPath)) { throw "Required file not found: $managerPath" }
if (!(Test-Path $skillPath)) { throw "Required file not found: $skillPath" }
if (!(Test-Path $animDir)) { New-Item -ItemType Directory -Path $animDir | Out-Null }

$data = @'
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
        ReturnHome,
        ShakePrimaryTarget,
        FlashPrimaryTarget
    }

    public enum SkillAnimationAnchor
    {
        Current,
        CasterHome,
        PrimaryTarget,
        ScreenCenter,
        ScreenTop,
        ScreenBottom,
        ScreenLeftOutside,
        ScreenRightOutside
    }

    [System.Serializable]
    public class SkillAnimationStep
    {
        public string Label;
        public SkillAnimationStepType StepType = SkillAnimationStepType.Pose;
        public Sprite Sprite;
        public float Duration = 0.1f;
        public SkillAnimationAnchor ToAnchor = SkillAnimationAnchor.Current;
        public Vector2 ToOffset;
        public float JumpHeight = 80f;
        public float Scale = 1f;
        public float RotationZ;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public int FlashCount = 2;
        public Color FlashColor = Color.white;
        public float ShakeDistance = 12f;
        public int ShakeCount = 2;
    }

    [CreateAssetMenu(
        fileName = "SkillAnimationData",
        menuName = "GameKari/Battle/Skill Animation Data")]
    public class SkillAnimationData : ScriptableObject
    {
        [Header("Sprite Pose Animation")]
        public List<SkillAnimationStep> Steps = new();

        [Header("Finish")]
        public bool RestoreOriginalSprite = true;
        public bool RestoreHomePosition = true;
        public bool RestoreScale = true;
        public bool RestoreRotation = true;
    }
}
'@
Set-Content -Path $dataPath -Value $data -Encoding UTF8

$player = @'
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    public sealed class SkillAnimationContext
    {
        public Image ActorImage;
        public RectTransform ActorRect;
        public Vector2 ActorHomePosition;
        public Sprite OriginalSprite;
        public Vector3 OriginalScale;
        public Quaternion OriginalRotation;
        public Func<SkillAnimationAnchor, Vector2> ResolveAnchor;
        public Func<RectTransform> ResolvePrimaryTargetRect;
        public Action AfterVisualChanged;
    }

    public static class SkillAnimationPlayer
    {
        public static IEnumerator Play(SkillAnimationData data, SkillAnimationContext context)
        {
            if (data == null || context == null || context.ActorImage == null || context.ActorRect == null)
            {
                yield break;
            }

            context.OriginalSprite = context.ActorImage.sprite;
            context.OriginalScale = context.ActorRect.localScale;
            context.OriginalRotation = context.ActorRect.localRotation;

            for (int i = 0; i < data.Steps.Count; i++)
            {
                SkillAnimationStep step = data.Steps[i];
                if (step == null)
                {
                    continue;
                }

                if (step.Sprite != null)
                {
                    context.ActorImage.sprite = step.Sprite;
                    context.AfterVisualChanged?.Invoke();
                }

                switch (step.StepType)
                {
                    case SkillAnimationStepType.Pose:
                    case SkillAnimationStepType.Wait:
                        ApplyTransformStep(context, step);
                        yield return Wait(step.Duration);
                        break;

                    case SkillAnimationStepType.Move:
                        yield return Move(context, step, useArc: false);
                        break;

                    case SkillAnimationStepType.JumpMove:
                        yield return Move(context, step, useArc: true);
                        break;

                    case SkillAnimationStepType.ReturnHome:
                        yield return ReturnHome(context, step);
                        break;

                    case SkillAnimationStepType.ShakePrimaryTarget:
                        yield return ShakePrimaryTarget(context, step);
                        break;

                    case SkillAnimationStepType.FlashPrimaryTarget:
                        yield return FlashPrimaryTarget(context, step);
                        break;
                }
            }

            if (data.RestoreOriginalSprite)
            {
                context.ActorImage.sprite = context.OriginalSprite;
            }

            if (data.RestoreHomePosition)
            {
                context.ActorRect.anchoredPosition = context.ActorHomePosition;
            }

            if (data.RestoreScale)
            {
                context.ActorRect.localScale = context.OriginalScale;
            }

            if (data.RestoreRotation)
            {
                context.ActorRect.localRotation = context.OriginalRotation;
            }

            context.AfterVisualChanged?.Invoke();
        }

        private static IEnumerator Wait(float duration)
        {
            float wait = Mathf.Max(0f, duration);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }
        }

        private static void ApplyTransformStep(SkillAnimationContext context, SkillAnimationStep step)
        {
            if (context == null || context.ActorRect == null || step == null)
            {
                return;
            }

            if (step.Scale > 0f)
            {
                context.ActorRect.localScale = context.OriginalScale * step.Scale;
            }

            context.ActorRect.localRotation = Quaternion.Euler(0f, 0f, step.RotationZ);
        }

        private static IEnumerator Move(SkillAnimationContext context, SkillAnimationStep step, bool useArc)
        {
            if (context == null || context.ActorRect == null || step == null)
            {
                yield break;
            }

            float duration = Mathf.Max(0f, step.Duration);
            Vector2 start = context.ActorRect.anchoredPosition;
            Vector2 end = Resolve(context, step.ToAnchor) + step.ToOffset;

            if (duration <= 0f)
            {
                context.ActorRect.anchoredPosition = end;
                ApplyTransformStep(context, step);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float rawT = Mathf.Clamp01(elapsed / duration);
                float t = step.Curve == null ? rawT : step.Curve.Evaluate(rawT);
                Vector2 position = Vector2.LerpUnclamped(start, end, t);

                if (useArc)
                {
                    position.y += Mathf.Sin(rawT * Mathf.PI) * step.JumpHeight;
                }

                context.ActorRect.anchoredPosition = position;
                ApplyTransformStep(context, step);
                yield return null;
            }

            context.ActorRect.anchoredPosition = end;
        }

        private static IEnumerator ReturnHome(SkillAnimationContext context, SkillAnimationStep step)
        {
            if (context == null || context.ActorRect == null || step == null)
            {
                yield break;
            }

            SkillAnimationStep returnStep = new SkillAnimationStep
            {
                StepType = SkillAnimationStepType.Move,
                Duration = step.Duration,
                ToAnchor = SkillAnimationAnchor.CasterHome,
                ToOffset = step.ToOffset,
                Scale = step.Scale,
                RotationZ = step.RotationZ,
                Curve = step.Curve
            };

            yield return Move(context, returnStep, useArc: false);
        }

        private static IEnumerator ShakePrimaryTarget(SkillAnimationContext context, SkillAnimationStep step)
        {
            RectTransform target = context?.ResolvePrimaryTargetRect?.Invoke();
            if (target == null || step == null)
            {
                yield break;
            }

            float duration = Mathf.Max(0f, step.Duration);
            if (duration <= 0f)
            {
                yield break;
            }

            Vector2 start = target.anchoredPosition;
            float elapsed = 0f;
            int shakeCount = Mathf.Max(1, step.ShakeCount);
            float distance = Mathf.Max(0f, step.ShakeDistance);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI * 2f * shakeCount);
                target.anchoredPosition = start + new Vector2(wave * distance, 0f);
                yield return null;
            }

            target.anchoredPosition = start;
        }

        private static IEnumerator FlashPrimaryTarget(SkillAnimationContext context, SkillAnimationStep step)
        {
            RectTransform target = context?.ResolvePrimaryTargetRect?.Invoke();
            if (target == null || step == null)
            {
                yield break;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                yield break;
            }

            Color original = image.color;
            float duration = Mathf.Max(0f, step.Duration);
            int count = Mathf.Max(1, step.FlashCount);
            float interval = count <= 0 ? duration : duration / (count * 2f);

            for (int i = 0; i < count; i++)
            {
                image.color = step.FlashColor;
                yield return new WaitForSeconds(interval);
                image.color = original;
                yield return new WaitForSeconds(interval);
            }

            image.color = original;
        }

        private static Vector2 Resolve(SkillAnimationContext context, SkillAnimationAnchor anchor)
        {
            if (context == null)
            {
                return Vector2.zero;
            }

            if (anchor == SkillAnimationAnchor.Current && context.ActorRect != null)
            {
                return context.ActorRect.anchoredPosition;
            }

            if (anchor == SkillAnimationAnchor.CasterHome)
            {
                return context.ActorHomePosition;
            }

            return context.ResolveAnchor == null ? Vector2.zero : context.ResolveAnchor(anchor);
        }
    }
}
'@
Set-Content -Path $playerPath -Value $player -Encoding UTF8

$skill = Get-Content -Path $skillPath -Raw -Encoding UTF8
if (!$skill.Contains('public SkillAnimationData AnimationData;')) {
    $skill = $skill.Replace('        public int Damage;', "        public SkillAnimationData AnimationData;`r`n`r`n        public int Damage;")
    Set-Content -Path $skillPath -Value $skill -Encoding UTF8
    Write-Host 'Patched: SkillData AnimationData field'
} else {
    Write-Host 'Already exists: SkillData AnimationData field'
}

$manager = Get-Content -Path $managerPath -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    Write-Host "Inserted: $label"
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$old = @'
            PrepareSkillActionFlashTargets(skill);
            BattleUnit flashableLinkPartner = IsActiveAllyUnit(linkPartner) ? linkPartner : null;
            SetPendingActionSourceFlashTargets(true, BuildSkillSourceFlashTargets(actor, flashableLinkPartner));
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {userDisplayName}.");

            ApplySkillDamage(skill);
'@
$new = @'
            PrepareSkillActionFlashTargets(skill);
            BattleUnit flashableLinkPartner = IsActiveAllyUnit(linkPartner) ? linkPartner : null;
            SetPendingActionSourceFlashTargets(true, BuildSkillSourceFlashTargets(actor, flashableLinkPartner));
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {userDisplayName}.");

            yield return PlaySkillSpriteAnimation(skill, actor);

            ApplySkillDamage(skill);
'@
$manager = ReplaceOptional $manager $old $new 'play SkillAnimationData before skill damage'

$helpers = @'
        private IEnumerator PlaySkillSpriteAnimation(SkillData skill, BattleUnit actor)
        {
            if (skill == null || skill.AnimationData == null || actor == null)
            {
                yield break;
            }

            RectTransform actorRect = GetBoardSpriteRect(true, actor.GridPos);
            if (actorRect == null)
            {
                yield break;
            }

            Image actorImage = actorRect.GetComponent<Image>();
            if (actorImage == null)
            {
                yield break;
            }

            Vector2 homePosition = actorRect.anchoredPosition;
            GridPos primaryTargetPosition = GetPrimarySkillAnimationTargetPosition(skill, actor);
            RectTransform primaryTargetRect = GetBoardSpriteRect(IsSkillAnimationTargetAllyBoard(skill), primaryTargetPosition);

            var context = new SkillAnimationContext
            {
                ActorImage = actorImage,
                ActorRect = actorRect,
                ActorHomePosition = homePosition,
                ResolvePrimaryTargetRect = () => primaryTargetRect,
                ResolveAnchor = anchor => ResolveSkillAnimationAnchor(anchor, actorRect, homePosition, primaryTargetRect),
                AfterVisualChanged = ReapplySkillHoverPreviewDuringActionIfNeeded
            };

            yield return SkillAnimationPlayer.Play(skill.AnimationData, context);
        }

        private bool IsSkillAnimationTargetAllyBoard(SkillData skill)
        {
            return skill != null && skill.TargetPattern == SkillTargetPattern.Self;
        }

        private GridPos GetPrimarySkillAnimationTargetPosition(SkillData skill, BattleUnit actor)
        {
            List<GridPos> positions = GetSkillAnimationTargetPositions(skill);
            if (positions != null && positions.Count > 0)
            {
                return positions[0];
            }

            return actor == null ? GridPos.FrontTop : actor.GridPos;
        }

        private Vector2 ResolveSkillAnimationAnchor(SkillAnimationAnchor anchor, RectTransform actorRect, Vector2 homePosition, RectTransform primaryTargetRect)
        {
            switch (anchor)
            {
                case SkillAnimationAnchor.CasterHome:
                    return homePosition;

                case SkillAnimationAnchor.PrimaryTarget:
                    return primaryTargetRect == null ? homePosition : primaryTargetRect.anchoredPosition;

                case SkillAnimationAnchor.ScreenCenter:
                    return Vector2.zero;

                case SkillAnimationAnchor.ScreenTop:
                    return new Vector2(0f, 360f);

                case SkillAnimationAnchor.ScreenBottom:
                    return new Vector2(0f, -360f);

                case SkillAnimationAnchor.ScreenLeftOutside:
                    return new Vector2(-960f, 0f);

                case SkillAnimationAnchor.ScreenRightOutside:
                    return new Vector2(960f, 0f);

                case SkillAnimationAnchor.Current:
                default:
                    return actorRect == null ? homePosition : actorRect.anchoredPosition;
            }
        }

'@
$manager = InsertBeforeIfMissing $manager 'private IEnumerator PlaySkillSpriteAnimation(SkillData skill, BattleUnit actor)' '        // Skill effects and damage' $helpers 'SkillAnimationData playback helpers'

Set-Content -Path $managerPath -Value $manager -Encoding UTF8
Write-Host 'Patched skill sprite animation MVP.'
