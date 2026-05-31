$ErrorActionPreference = 'Stop'

$skillDataPath = 'Assets/Scripts/Battle/SkillData.cs'
$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$animDir = 'Assets/Scripts/Battle/Animation'
$dataPath = Join-Path $animDir 'SkillAnimationData.cs'
$playerPath = Join-Path $animDir 'SkillAnimationPlayer.cs'

if (!(Test-Path $skillDataPath)) { throw "Required file not found: $skillDataPath" }
if (!(Test-Path $managerPath)) { throw "Required file not found: $managerPath" }
if (!(Test-Path $animDir)) { New-Item -ItemType Directory -Path $animDir | Out-Null }

$dataCode = @'
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
        ShakeTarget
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
        public float Duration = 0.1f;
        public SkillAnimationAnchor FromAnchor = SkillAnimationAnchor.Current;
        public SkillAnimationAnchor ToAnchor = SkillAnimationAnchor.Current;
        public Vector2 FromOffset;
        public Vector2 ToOffset;
        public float JumpHeight = 0f;
        public float Scale = 1f;
        public float RotationZ = 0f;
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
    }
}
'@
Set-Content -Path $dataPath -Value $dataCode -Encoding UTF8

$playerCode = @'
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    public static class SkillAnimationPlayer
    {
        public static IEnumerator Play(SkillAnimationData data, RectTransform casterRect, RectTransform targetRect, Image casterImage)
        {
            if (data == null || casterRect == null || data.Steps == null || data.Steps.Count == 0)
            {
                yield break;
            }

            Sprite originalSprite = casterImage == null ? null : casterImage.sprite;
            Vector3 originalLocalPosition = casterRect.localPosition;
            Vector3 originalScale = casterRect.localScale;
            Quaternion originalRotation = casterRect.localRotation;

            for (int i = 0; i < data.Steps.Count; i++)
            {
                SkillAnimationStep step = data.Steps[i];
                if (step == null)
                {
                    continue;
                }

                if (casterImage != null && step.Sprite != null)
                {
                    casterImage.sprite = step.Sprite;
                }

                switch (step.StepType)
                {
                    case SkillAnimationStepType.Pose:
                    case SkillAnimationStepType.Wait:
                        yield return Wait(step.Duration);
                        break;

                    case SkillAnimationStepType.Move:
                        yield return MoveCaster(data, step, casterRect, targetRect, originalLocalPosition, false);
                        break;

                    case SkillAnimationStepType.JumpMove:
                        yield return MoveCaster(data, step, casterRect, targetRect, originalLocalPosition, true);
                        break;

                    case SkillAnimationStepType.Return:
                        yield return ReturnCaster(step, casterRect, originalLocalPosition, originalScale, originalRotation);
                        break;

                    case SkillAnimationStepType.ShakeTarget:
                        yield return ShakeTarget(step, targetRect);
                        break;
                }
            }

            if (data.RestoreSpriteAtEnd && casterImage != null)
            {
                casterImage.sprite = originalSprite;
            }

            if (data.RestoreTransformAtEnd)
            {
                casterRect.localPosition = originalLocalPosition;
                casterRect.localScale = originalScale;
                casterRect.localRotation = originalRotation;
            }
        }

        private static IEnumerator MoveCaster(
            SkillAnimationData data,
            SkillAnimationStep step,
            RectTransform casterRect,
            RectTransform targetRect,
            Vector3 originalLocalPosition,
            bool jump)
        {
            float duration = Mathf.Max(0f, step.Duration);
            Vector3 from = ResolveAnchor(step.FromAnchor, casterRect, targetRect, originalLocalPosition) + (Vector3)step.FromOffset;
            Vector3 to = ResolveAnchor(step.ToAnchor, casterRect, targetRect, originalLocalPosition) + (Vector3)step.ToOffset;
            Vector3 startScale = casterRect.localScale;
            Quaternion startRotation = casterRect.localRotation;
            Vector3 targetScale = Vector3.one * Mathf.Max(0.01f, step.Scale);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, step.RotationZ);

            if (duration <= 0f)
            {
                casterRect.localPosition = to;
                casterRect.localScale = targetScale;
                casterRect.localRotation = targetRotation;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                Vector3 position = Vector3.Lerp(from, to, eased);

                if (jump)
                {
                    position.y += Mathf.Sin(t * Mathf.PI) * step.JumpHeight;
                }

                casterRect.localPosition = position;
                casterRect.localScale = Vector3.Lerp(startScale, targetScale, eased);
                casterRect.localRotation = Quaternion.Lerp(startRotation, targetRotation, eased);
                yield return null;
            }

            casterRect.localPosition = to;
            casterRect.localScale = targetScale;
            casterRect.localRotation = targetRotation;
        }

        private static IEnumerator ReturnCaster(
            SkillAnimationStep step,
            RectTransform casterRect,
            Vector3 originalLocalPosition,
            Vector3 originalScale,
            Quaternion originalRotation)
        {
            float duration = Mathf.Max(0f, step.Duration);
            Vector3 from = casterRect.localPosition;
            Vector3 startScale = casterRect.localScale;
            Quaternion startRotation = casterRect.localRotation;

            if (duration <= 0f)
            {
                casterRect.localPosition = originalLocalPosition;
                casterRect.localScale = originalScale;
                casterRect.localRotation = originalRotation;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                casterRect.localPosition = Vector3.Lerp(from, originalLocalPosition, eased);
                casterRect.localScale = Vector3.Lerp(startScale, originalScale, eased);
                casterRect.localRotation = Quaternion.Lerp(startRotation, originalRotation, eased);
                yield return null;
            }

            casterRect.localPosition = originalLocalPosition;
            casterRect.localScale = originalScale;
            casterRect.localRotation = originalRotation;
        }

        private static IEnumerator ShakeTarget(SkillAnimationStep step, RectTransform targetRect)
        {
            if (targetRect == null)
            {
                yield return Wait(step.Duration);
                yield break;
            }

            float duration = Mathf.Max(0f, step.Duration);
            if (duration <= 0f)
            {
                yield break;
            }

            Vector3 original = targetRect.localPosition;
            int shakeCount = Mathf.Max(1, step.ShakeCount);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI * 2f * shakeCount);
                targetRect.localPosition = original + new Vector3(wave * step.ShakeDistance, 0f, 0f);
                yield return null;
            }

            targetRect.localPosition = original;
        }

        private static IEnumerator Wait(float duration)
        {
            float wait = Mathf.Max(0f, duration);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }
        }

        private static Vector3 ResolveAnchor(SkillAnimationAnchor anchor, RectTransform casterRect, RectTransform targetRect, Vector3 originalLocalPosition)
        {
            switch (anchor)
            {
                case SkillAnimationAnchor.Original:
                    return originalLocalPosition;

                case SkillAnimationAnchor.Caster:
                case SkillAnimationAnchor.Current:
                    return casterRect == null ? originalLocalPosition : casterRect.localPosition;

                case SkillAnimationAnchor.Target:
                    return ConvertRectCenterToCasterParentLocal(casterRect, targetRect, originalLocalPosition);

                case SkillAnimationAnchor.ScreenCenter:
                    return ConvertScreenPointToCasterParentLocal(casterRect, new Vector2(0.5f, 0.5f), originalLocalPosition);

                case SkillAnimationAnchor.ScreenTop:
                    return ConvertScreenPointToCasterParentLocal(casterRect, new Vector2(0.5f, 0.9f), originalLocalPosition);

                case SkillAnimationAnchor.ScreenBottom:
                    return ConvertScreenPointToCasterParentLocal(casterRect, new Vector2(0.5f, 0.1f), originalLocalPosition);

                default:
                    return casterRect == null ? originalLocalPosition : casterRect.localPosition;
            }
        }

        private static Vector3 ConvertRectCenterToCasterParentLocal(RectTransform casterRect, RectTransform targetRect, Vector3 fallback)
        {
            if (casterRect == null || targetRect == null || casterRect.parent == null)
            {
                return fallback;
            }

            RectTransform parentRect = casterRect.parent as RectTransform;
            if (parentRect == null)
            {
                return fallback;
            }

            Vector3 world = targetRect.TransformPoint(targetRect.rect.center);
            return parentRect.InverseTransformPoint(world);
        }

        private static Vector3 ConvertScreenPointToCasterParentLocal(RectTransform casterRect, Vector2 normalizedViewportPoint, Vector3 fallback)
        {
            if (casterRect == null || casterRect.parent == null)
            {
                return fallback;
            }

            Canvas canvas = casterRect.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas == null ? null : canvas.transform as RectTransform;
            RectTransform parentRect = casterRect.parent as RectTransform;
            if (canvasRect == null || parentRect == null)
            {
                return fallback;
            }

            Vector3 canvasWorld = canvasRect.TransformPoint(new Vector3(
                Mathf.Lerp(canvasRect.rect.xMin, canvasRect.rect.xMax, normalizedViewportPoint.x),
                Mathf.Lerp(canvasRect.rect.yMin, canvasRect.rect.yMax, normalizedViewportPoint.y),
                0f));

            return parentRect.InverseTransformPoint(canvasWorld);
        }
    }
}
'@
Set-Content -Path $playerPath -Value $playerCode -Encoding UTF8

$skillText = Get-Content -Path $skillDataPath -Raw -Encoding UTF8
if (!$skillText.Contains('public SkillAnimationData Animation;')) {
    $skillText = $skillText.Replace('        public int HealAmount;        public SkillEffectType EffectType;', '        public int HealAmount;`r`n        public SkillAnimationData Animation;`r`n        public SkillEffectType EffectType;')
    Set-Content -Path $skillDataPath -Value $skillText -Encoding UTF8
    Write-Host 'Patched SkillData Animation field.'
} else {
    Write-Host 'Already exists: SkillData Animation field.'
}

$managerText = Get-Content -Path $managerPath -Raw -Encoding UTF8

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

if (!$managerText.Contains('yield return PlaySkillAnimationIfAny(skill);')) {
    $old = @'
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {userDisplayName}.");

            ApplySkillDamage(skill);
'@
    $new = @'
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {userDisplayName}.");

            yield return PlaySkillAnimationIfAny(skill);

            ApplySkillDamage(skill);
'@
    if (!$managerText.Contains($old)) { throw 'Patch anchor not found: player skill animation call' }
    $managerText = $managerText.Replace($old, $new)
    Write-Host 'Inserted: player skill animation call.'
} else {
    Write-Host 'Already exists: player skill animation call.'
}

$helper = @'
        private IEnumerator PlaySkillAnimationIfAny(SkillData skill)
        {
            if (skill == null || skill.Animation == null || _active == null)
            {
                yield break;
            }

            RectTransform casterRect = GetBoardSpriteRect(true, _active.GridPos);
            Image casterImage = GetBoardSpriteImage(true, _active.GridPos);
            RectTransform targetRect = GetPrimarySkillAnimationTargetRect(skill);

            yield return SkillAnimationPlayer.Play(skill.Animation, casterRect, targetRect, casterImage);
        }

        private RectTransform GetPrimarySkillAnimationTargetRect(SkillData skill)
        {
            if (skill == null)
            {
                return null;
            }

            bool targetIsAllyBoard = skill.TargetPattern == SkillTargetPattern.Self;
            List<GridPos> targetPositions = GetSkillAnimationTargetPositions(skill);
            for (int i = 0; i < targetPositions.Count; i++)
            {
                RectTransform targetRect = GetBoardSpriteRect(targetIsAllyBoard, targetPositions[i]);
                if (targetRect != null)
                {
                    return targetRect;
                }
            }

            return null;
        }

'@
$managerText = InsertBeforeIfMissing $managerText 'private IEnumerator PlaySkillAnimationIfAny(SkillData skill)' '        // Skill effects and damage' $helper 'SkillAnimation playback helpers'

Set-Content -Path $managerPath -Value $managerText -Encoding UTF8
Write-Host 'Patched skill sprite animation foundation.'
