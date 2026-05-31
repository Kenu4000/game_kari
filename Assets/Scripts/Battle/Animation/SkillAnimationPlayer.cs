using System.Collections;
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

            Image projectileImage = GetOrCreateProjectileImage(casterRect);
            RectTransform projectileRect = projectileImage == null ? null : projectileImage.rectTransform;
            HideProjectile(projectileImage);

            for (int i = 0; i < data.Steps.Count; i++)
            {
                SkillAnimationStep step = data.Steps[i];
                if (step == null)
                {
                    continue;
                }

                switch (step.StepType)
                {
                    case SkillAnimationStepType.Pose:
                    case SkillAnimationStepType.Wait:
                        ApplyCasterSprite(casterImage, step.Sprite);
                        ApplyCasterTransformStep(casterRect, step);
                        yield return Wait(step.Duration);
                        break;

                    case SkillAnimationStepType.Move:
                        ApplyCasterSprite(casterImage, step.Sprite);
                        yield return MoveRect(step, casterRect, casterRect, targetRect, originalLocalPosition, false, false);
                        break;

                    case SkillAnimationStepType.JumpMove:
                        ApplyCasterSprite(casterImage, step.Sprite);
                        yield return MoveRect(step, casterRect, casterRect, targetRect, originalLocalPosition, true, false);
                        break;

                    case SkillAnimationStepType.Return:
                        ApplyCasterSprite(casterImage, step.Sprite);
                        yield return ReturnCaster(step, casterRect, originalLocalPosition, originalScale, originalRotation);
                        break;

                    case SkillAnimationStepType.ShakeTarget:
                        yield return ShakeTarget(step, targetRect);
                        break;

                    case SkillAnimationStepType.SpawnProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, casterRect, targetRect, originalLocalPosition);
                        yield return Wait(step.Duration);
                        break;

                    case SkillAnimationStepType.MoveProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, casterRect, targetRect, originalLocalPosition);
                        yield return MoveRect(step, projectileRect, casterRect, targetRect, originalLocalPosition, false, true);
                        break;

                    case SkillAnimationStepType.JumpProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, casterRect, targetRect, originalLocalPosition);
                        yield return MoveRect(step, projectileRect, casterRect, targetRect, originalLocalPosition, true, true);
                        break;

                    case SkillAnimationStepType.HideProjectile:
                        HideProjectile(projectileImage);
                        yield return Wait(step.Duration);
                        break;
                }
            }

            if (data.HideProjectileAtEnd)
            {
                HideProjectile(projectileImage);
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

        private static void ApplyCasterSprite(Image casterImage, Sprite sprite)
        {
            if (casterImage != null && sprite != null)
            {
                casterImage.sprite = sprite;
            }
        }

        private static void ApplyCasterTransformStep(RectTransform casterRect, SkillAnimationStep step)
        {
            if (casterRect == null || step == null)
            {
                return;
            }

            casterRect.localScale = Vector3.one * Mathf.Max(0.01f, step.Scale);
            casterRect.localRotation = Quaternion.Euler(0f, 0f, step.RotationZ);
        }

        private static IEnumerator MoveRect(
            SkillAnimationStep step,
            RectTransform movingRect,
            RectTransform casterRect,
            RectTransform targetRect,
            Vector3 originalLocalPosition,
            bool jump,
            bool projectile)
        {
            if (movingRect == null)
            {
                yield return Wait(step == null ? 0f : step.Duration);
                yield break;
            }

            float duration = Mathf.Max(0f, step.Duration);
            Vector3 from = ResolveAnchor(step.FromAnchor, casterRect, targetRect, originalLocalPosition) + (Vector3)step.FromOffset;
            Vector3 to = ResolveAnchor(step.ToAnchor, casterRect, targetRect, originalLocalPosition) + (Vector3)step.ToOffset;
            Vector3 startScale = movingRect.localScale;
            Quaternion startRotation = movingRect.localRotation;
            float targetScaleValue = projectile ? step.ProjectileScale : step.Scale;
            float targetRotationValue = projectile ? step.ProjectileRotationZ : step.RotationZ;
            Vector3 targetScale = Vector3.one * Mathf.Max(0.01f, targetScaleValue);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetRotationValue);

            if (duration <= 0f)
            {
                movingRect.localPosition = to;
                movingRect.localScale = targetScale;
                movingRect.localRotation = targetRotation;
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

                movingRect.localPosition = position;
                movingRect.localScale = Vector3.Lerp(startScale, targetScale, eased);
                movingRect.localRotation = Quaternion.Lerp(startRotation, targetRotation, eased);
                yield return null;
            }

            movingRect.localPosition = to;
            movingRect.localScale = targetScale;
            movingRect.localRotation = targetRotation;
        }

        private static IEnumerator ReturnCaster(
            SkillAnimationStep step,
            RectTransform casterRect,
            Vector3 originalLocalPosition,
            Vector3 originalScale,
            Quaternion originalRotation)
        {
            if (casterRect == null)
            {
                yield return Wait(step == null ? 0f : step.Duration);
                yield break;
            }

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

        private static void SpawnProjectile(
            SkillAnimationStep step,
            Image projectileImage,
            RectTransform projectileRect,
            RectTransform casterRect,
            RectTransform targetRect,
            Vector3 originalLocalPosition)
        {
            if (step == null || projectileImage == null || projectileRect == null)
            {
                return;
            }

            if (step.ProjectileSprite != null)
            {
                projectileImage.sprite = step.ProjectileSprite;
            }

            projectileImage.enabled = projectileImage.sprite != null;
            projectileImage.gameObject.SetActive(projectileImage.sprite != null);
            projectileImage.raycastTarget = false;
            projectileImage.preserveAspect = true;

            projectileRect.localPosition = ResolveAnchor(step.FromAnchor, casterRect, targetRect, originalLocalPosition) + (Vector3)step.FromOffset;
            projectileRect.localScale = Vector3.one * Mathf.Max(0.01f, step.ProjectileScale);
            projectileRect.localRotation = Quaternion.Euler(0f, 0f, step.ProjectileRotationZ);
            projectileRect.SetAsLastSibling();
        }

        private static void HideProjectile(Image projectileImage)
        {
            if (projectileImage == null)
            {
                return;
            }

            projectileImage.enabled = false;
            projectileImage.gameObject.SetActive(false);
        }

        private static Image GetOrCreateProjectileImage(RectTransform casterRect)
        {
            if (casterRect == null || casterRect.parent == null)
            {
                return null;
            }

            Transform parent = casterRect.parent;
            Transform existing = parent.Find("SkillAnimationProjectileImage");
            if (existing != null)
            {
                Image existingImage = existing.GetComponent<Image>();
                if (existingImage != null)
                {
                    return existingImage;
                }
            }

            GameObject obj = new GameObject("SkillAnimationProjectileImage", typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = casterRect.anchorMin;
            rect.anchorMax = casterRect.anchorMax;
            rect.pivot = casterRect.pivot;
            rect.sizeDelta = casterRect.sizeDelta;
            rect.offsetMin = casterRect.offsetMin;
            rect.offsetMax = casterRect.offsetMax;
            rect.localScale = Vector3.one;

            Image image = obj.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            obj.SetActive(false);
            return image;
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
