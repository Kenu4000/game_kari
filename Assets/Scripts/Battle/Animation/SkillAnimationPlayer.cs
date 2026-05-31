using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    public static class SkillAnimationPlayer
    {
        public static IEnumerator Play(SkillAnimationData data, RectTransform casterRect, RectTransform targetRect, Image casterImage)
        {
            if (data == null || casterRect == null || casterImage == null || data.Steps == null || data.Steps.Count == 0)
            {
                yield break;
            }

            Image actorImage = GetOrCreateActorProxyImage(casterRect, casterImage);
            RectTransform actorRect = actorImage == null ? null : actorImage.rectTransform;
            if (actorImage == null || actorRect == null)
            {
                yield break;
            }

            Sprite originalSprite = casterImage.sprite;
            bool originalCasterEnabled = casterImage.enabled;
            Color originalCasterColor = casterImage.color;
            Vector2 originalActorAnchoredPosition = actorRect.anchoredPosition;
            Vector3 originalActorScale = actorRect.localScale;
            Quaternion originalActorRotation = actorRect.localRotation;

            SetupProxyFromSource(actorImage, actorRect, casterImage, casterRect);
            casterImage.enabled = false;

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
                        ApplyImageSprite(actorImage, step.Sprite);
                        ApplyActorTransformStep(actorRect, step);
                        yield return Wait(step.Duration);
                        break;

                    case SkillAnimationStepType.Move:
                        ApplyImageSprite(actorImage, step.Sprite);
                        yield return MoveRect(step, actorRect, casterRect, targetRect, originalActorAnchoredPosition, false, false);
                        break;

                    case SkillAnimationStepType.JumpMove:
                        ApplyImageSprite(actorImage, step.Sprite);
                        yield return MoveRect(step, actorRect, casterRect, targetRect, originalActorAnchoredPosition, true, false);
                        break;

                    case SkillAnimationStepType.Return:
                        ApplyImageSprite(actorImage, step.Sprite);
                        yield return ReturnRect(step, actorRect, originalActorAnchoredPosition, originalActorScale, originalActorRotation);
                        break;

                    case SkillAnimationStepType.ShakeTarget:
                        yield return ShakeTarget(step, targetRect);
                        break;

                    case SkillAnimationStepType.SpawnProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, casterRect, targetRect, originalActorAnchoredPosition);
                        yield return Wait(step.Duration);
                        break;

                    case SkillAnimationStepType.MoveProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, casterRect, targetRect, originalActorAnchoredPosition);
                        yield return MoveRect(step, projectileRect, casterRect, targetRect, originalActorAnchoredPosition, false, true);
                        break;

                    case SkillAnimationStepType.JumpProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, casterRect, targetRect, originalActorAnchoredPosition);
                        yield return MoveRect(step, projectileRect, casterRect, targetRect, originalActorAnchoredPosition, true, true);
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

            if (data.RestoreSpriteAtEnd)
            {
                casterImage.sprite = originalSprite;
            }

            casterImage.enabled = originalCasterEnabled;
            casterImage.color = originalCasterColor;

            actorImage.enabled = false;
            actorImage.gameObject.SetActive(false);
            actorRect.anchoredPosition = originalActorAnchoredPosition;
            actorRect.localScale = originalActorScale;
            actorRect.localRotation = originalActorRotation;
        }

        private static void ApplyImageSprite(Image image, Sprite sprite)
        {
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.enabled = true;
                image.gameObject.SetActive(true);
            }
        }

        private static void ApplyActorTransformStep(RectTransform actorRect, SkillAnimationStep step)
        {
            if (actorRect == null || step == null)
            {
                return;
            }

            actorRect.localScale = Vector3.one * Mathf.Max(0.01f, step.Scale);
            actorRect.localRotation = Quaternion.Euler(0f, 0f, step.RotationZ);
        }

        private static IEnumerator MoveRect(
            SkillAnimationStep step,
            RectTransform movingRect,
            RectTransform casterRect,
            RectTransform targetRect,
            Vector2 originalAnchoredPosition,
            bool jump,
            bool projectile)
        {
            if (movingRect == null || step == null)
            {
                yield return Wait(step == null ? 0f : step.Duration);
                yield break;
            }

            float duration = Mathf.Max(0f, step.Duration);
            Vector2 from = ResolveAnchor(step.FromAnchor, casterRect, targetRect, originalAnchoredPosition) + step.FromOffset;
            Vector2 to = ResolveAnchor(step.ToAnchor, casterRect, targetRect, originalAnchoredPosition) + step.ToOffset;
            Vector3 startScale = movingRect.localScale;
            Quaternion startRotation = movingRect.localRotation;
            float targetScaleValue = projectile ? step.ProjectileScale : step.Scale;
            float targetRotationValue = projectile ? step.ProjectileRotationZ : step.RotationZ;
            Vector3 targetScale = Vector3.one * Mathf.Max(0.01f, targetScaleValue);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetRotationValue);

            if (duration <= 0f)
            {
                movingRect.anchoredPosition = to;
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
                Vector2 position = Vector2.Lerp(from, to, eased);

                if (jump)
                {
                    position.y += Mathf.Sin(t * Mathf.PI) * step.JumpHeight;
                }

                movingRect.anchoredPosition = position;
                movingRect.localScale = Vector3.Lerp(startScale, targetScale, eased);
                movingRect.localRotation = Quaternion.Lerp(startRotation, targetRotation, eased);
                yield return null;
            }

            movingRect.anchoredPosition = to;
            movingRect.localScale = targetScale;
            movingRect.localRotation = targetRotation;
        }

        private static IEnumerator ReturnRect(
            SkillAnimationStep step,
            RectTransform rect,
            Vector2 originalAnchoredPosition,
            Vector3 originalScale,
            Quaternion originalRotation)
        {
            if (rect == null)
            {
                yield return Wait(step == null ? 0f : step.Duration);
                yield break;
            }

            float duration = Mathf.Max(0f, step.Duration);
            Vector2 from = rect.anchoredPosition;
            Vector3 startScale = rect.localScale;
            Quaternion startRotation = rect.localRotation;

            if (duration <= 0f)
            {
                rect.anchoredPosition = originalAnchoredPosition;
                rect.localScale = originalScale;
                rect.localRotation = originalRotation;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                rect.anchoredPosition = Vector2.Lerp(from, originalAnchoredPosition, eased);
                rect.localScale = Vector3.Lerp(startScale, originalScale, eased);
                rect.localRotation = Quaternion.Lerp(startRotation, originalRotation, eased);
                yield return null;
            }

            rect.anchoredPosition = originalAnchoredPosition;
            rect.localScale = originalScale;
            rect.localRotation = originalRotation;
        }

        private static IEnumerator ShakeTarget(SkillAnimationStep step, RectTransform targetRect)
        {
            if (targetRect == null || step == null)
            {
                yield return Wait(step == null ? 0f : step.Duration);
                yield break;
            }

            float duration = Mathf.Max(0f, step.Duration);
            if (duration <= 0f)
            {
                yield break;
            }

            Vector2 original = targetRect.anchoredPosition;
            int shakeCount = Mathf.Max(1, step.ShakeCount);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI * 2f * shakeCount);
                targetRect.anchoredPosition = original + new Vector2(wave * step.ShakeDistance, 0f);
                yield return null;
            }

            targetRect.anchoredPosition = original;
        }

        private static void SpawnProjectile(
            SkillAnimationStep step,
            Image projectileImage,
            RectTransform projectileRect,
            RectTransform casterRect,
            RectTransform targetRect,
            Vector2 originalAnchoredPosition)
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

            projectileRect.anchoredPosition = ResolveAnchor(step.FromAnchor, casterRect, targetRect, originalAnchoredPosition) + step.FromOffset;
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

        private static Image GetOrCreateActorProxyImage(RectTransform sourceRect, Image sourceImage)
        {
            if (sourceRect == null || sourceRect.parent == null || sourceImage == null)
            {
                return null;
            }

            Transform parent = sourceRect.parent;
            Transform existing = parent.Find("SkillAnimationActorImage");
            if (existing != null)
            {
                Image existingImage = existing.GetComponent<Image>();
                if (existingImage != null)
                {
                    SetupProxyFromSource(existingImage, existingImage.rectTransform, sourceImage, sourceRect);
                    return existingImage;
                }
            }

            GameObject obj = new GameObject("SkillAnimationActorImage", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            Image image = obj.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            SetupProxyFromSource(image, image.rectTransform, sourceImage, sourceRect);
            return image;
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
                    SetupAnimationRect(existingImage.rectTransform, casterRect);
                    return existingImage;
                }
            }

            GameObject obj = new GameObject("SkillAnimationProjectileImage", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            Image image = obj.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            SetupAnimationRect(image.rectTransform, casterRect);
            obj.SetActive(false);
            return image;
        }

        private static void SetupProxyFromSource(Image proxyImage, RectTransform proxyRect, Image sourceImage, RectTransform sourceRect)
        {
            if (proxyImage == null || proxyRect == null || sourceImage == null || sourceRect == null)
            {
                return;
            }

            SetupAnimationRect(proxyRect, sourceRect);
            proxyImage.sprite = sourceImage.sprite;
            proxyImage.color = sourceImage.color;
            proxyImage.material = sourceImage.material;
            proxyImage.type = sourceImage.type;
            proxyImage.preserveAspect = sourceImage.preserveAspect;
            proxyImage.raycastTarget = false;
            proxyImage.enabled = sourceImage.enabled && sourceImage.sprite != null;
            proxyImage.gameObject.SetActive(proxyImage.enabled);
            proxyRect.SetAsLastSibling();
        }

        private static void SetupAnimationRect(RectTransform rect, RectTransform sourceRect)
        {
            if (rect == null || sourceRect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = sourceRect.pivot;
            rect.sizeDelta = sourceRect.rect.size;
            rect.anchoredPosition = sourceRect.anchoredPosition;
            rect.localScale = sourceRect.localScale;
            rect.localRotation = sourceRect.localRotation;
        }

        private static IEnumerator Wait(float duration)
        {
            float wait = Mathf.Max(0f, duration);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }
        }

        private static Vector2 ResolveAnchor(SkillAnimationAnchor anchor, RectTransform casterRect, RectTransform targetRect, Vector2 originalAnchoredPosition)
        {
            switch (anchor)
            {
                case SkillAnimationAnchor.Original:
                    return originalAnchoredPosition;

                case SkillAnimationAnchor.Caster:
                    return originalAnchoredPosition;

                case SkillAnimationAnchor.Current:
                    return casterRect == null ? originalAnchoredPosition : casterRect.anchoredPosition;

                case SkillAnimationAnchor.Target:
                    return ConvertRectCenterToCasterParentAnchored(casterRect, targetRect, originalAnchoredPosition);

                case SkillAnimationAnchor.ScreenCenter:
                    return ConvertScreenPointToCasterParentAnchored(casterRect, new Vector2(0.5f, 0.5f), originalAnchoredPosition);

                case SkillAnimationAnchor.ScreenTop:
                    return ConvertScreenPointToCasterParentAnchored(casterRect, new Vector2(0.5f, 0.9f), originalAnchoredPosition);

                case SkillAnimationAnchor.ScreenBottom:
                    return ConvertScreenPointToCasterParentAnchored(casterRect, new Vector2(0.5f, 0.1f), originalAnchoredPosition);

                default:
                    return originalAnchoredPosition;
            }
        }

        private static Vector2 ConvertRectCenterToCasterParentAnchored(RectTransform casterRect, RectTransform targetRect, Vector2 fallback)
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
            Vector3 local = parentRect.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        private static Vector2 ConvertScreenPointToCasterParentAnchored(RectTransform casterRect, Vector2 normalizedViewportPoint, Vector2 fallback)
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

            Vector3 local = parentRect.InverseTransformPoint(canvasWorld);
            return new Vector2(local.x, local.y);
        }
    }
}
