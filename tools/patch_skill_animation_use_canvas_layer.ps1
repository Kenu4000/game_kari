$ErrorActionPreference = 'Stop'

$playerPath = 'Assets/Scripts/Battle/Animation/SkillAnimationPlayer.cs'
if (!(Test-Path $playerPath)) { throw "Required file not found: $playerPath" }

$code = @'
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

            RectTransform animationLayer = GetOrCreateAnimationLayer(casterRect);
            if (animationLayer == null)
            {
                yield break;
            }

            Vector2 originalActorPosition = WorldCenterToLayerPosition(animationLayer, casterRect);
            Vector3 originalScale = casterRect.localScale;
            Quaternion originalRotation = casterRect.localRotation;
            Sprite originalSprite = casterImage.sprite;
            bool originalCasterEnabled = casterImage.enabled;
            Color originalCasterColor = casterImage.color;

            Image actorImage = GetOrCreateLayerImage(animationLayer, "SkillAnimationActorImage");
            RectTransform actorRect = actorImage == null ? null : actorImage.rectTransform;
            if (actorImage == null || actorRect == null)
            {
                yield break;
            }

            SetupLayerImageFromSource(actorImage, actorRect, casterImage, casterRect, originalActorPosition);
            casterImage.enabled = false;

            Image projectileImage = GetOrCreateLayerImage(animationLayer, "SkillAnimationProjectileImage");
            RectTransform projectileRect = projectileImage == null ? null : projectileImage.rectTransform;
            HideImage(projectileImage);

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
                        yield return MoveRect(step, actorRect, animationLayer, casterRect, targetRect, originalActorPosition, false, false);
                        break;

                    case SkillAnimationStepType.JumpMove:
                        ApplyImageSprite(actorImage, step.Sprite);
                        yield return MoveRect(step, actorRect, animationLayer, casterRect, targetRect, originalActorPosition, true, false);
                        break;

                    case SkillAnimationStepType.Return:
                        ApplyImageSprite(actorImage, step.Sprite);
                        yield return ReturnRect(step, actorRect, originalActorPosition, originalScale, originalRotation);
                        break;

                    case SkillAnimationStepType.ShakeTarget:
                        yield return ShakeTarget(step, targetRect);
                        break;

                    case SkillAnimationStepType.SpawnProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, animationLayer, casterRect, targetRect, originalActorPosition);
                        yield return Wait(step.Duration);
                        break;

                    case SkillAnimationStepType.MoveProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, animationLayer, casterRect, targetRect, originalActorPosition);
                        yield return MoveRect(step, projectileRect, animationLayer, casterRect, targetRect, originalActorPosition, false, true);
                        break;

                    case SkillAnimationStepType.JumpProjectile:
                        SpawnProjectile(step, projectileImage, projectileRect, animationLayer, casterRect, targetRect, originalActorPosition);
                        yield return MoveRect(step, projectileRect, animationLayer, casterRect, targetRect, originalActorPosition, true, true);
                        break;

                    case SkillAnimationStepType.HideProjectile:
                        HideImage(projectileImage);
                        yield return Wait(step.Duration);
                        break;
                }
            }

            if (data.HideProjectileAtEnd)
            {
                HideImage(projectileImage);
            }

            if (data.RestoreSpriteAtEnd)
            {
                casterImage.sprite = originalSprite;
            }

            casterImage.enabled = originalCasterEnabled;
            casterImage.color = originalCasterColor;
            HideImage(actorImage);
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
            RectTransform layerRect,
            RectTransform casterRect,
            RectTransform targetRect,
            Vector2 originalActorPosition,
            bool jump,
            bool projectile)
        {
            if (movingRect == null || layerRect == null || step == null)
            {
                yield return Wait(step == null ? 0f : step.Duration);
                yield break;
            }

            float duration = Mathf.Max(0f, step.Duration);
            Vector2 from = ResolveAnchor(step.FromAnchor, layerRect, casterRect, targetRect, originalActorPosition) + step.FromOffset;
            Vector2 to = ResolveAnchor(step.ToAnchor, layerRect, casterRect, targetRect, originalActorPosition) + step.ToOffset;
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
            Vector2 originalPosition,
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
                rect.anchoredPosition = originalPosition;
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
                rect.anchoredPosition = Vector2.Lerp(from, originalPosition, eased);
                rect.localScale = Vector3.Lerp(startScale, originalScale, eased);
                rect.localRotation = Quaternion.Lerp(startRotation, originalRotation, eased);
                yield return null;
            }

            rect.anchoredPosition = originalPosition;
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
            RectTransform layerRect,
            RectTransform casterRect,
            RectTransform targetRect,
            Vector2 originalActorPosition)
        {
            if (step == null || projectileImage == null || projectileRect == null || layerRect == null)
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

            projectileRect.anchoredPosition = ResolveAnchor(step.FromAnchor, layerRect, casterRect, targetRect, originalActorPosition) + step.FromOffset;
            projectileRect.localScale = Vector3.one * Mathf.Max(0.01f, step.ProjectileScale);
            projectileRect.localRotation = Quaternion.Euler(0f, 0f, step.ProjectileRotationZ);
            projectileRect.SetAsLastSibling();
        }

        private static void HideImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = false;
            image.gameObject.SetActive(false);
        }

        private static RectTransform GetOrCreateAnimationLayer(RectTransform sourceRect)
        {
            Canvas canvas = sourceRect == null ? null : sourceRect.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas == null ? null : canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return null;
            }

            Transform existing = canvasRect.Find("SkillAnimationLayer");
            if (existing != null)
            {
                RectTransform existingRect = existing as RectTransform;
                if (existingRect != null)
                {
                    existingRect.SetAsLastSibling();
                    return existingRect;
                }
            }

            GameObject obj = new GameObject("SkillAnimationLayer", typeof(RectTransform));
            obj.transform.SetParent(canvasRect, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.SetAsLastSibling();
            return rect;
        }

        private static Image GetOrCreateLayerImage(RectTransform layerRect, string name)
        {
            if (layerRect == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            Transform existing = layerRect.Find(name);
            if (existing != null)
            {
                Image existingImage = existing.GetComponent<Image>();
                if (existingImage != null)
                {
                    return existingImage;
                }
            }

            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(layerRect, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            Image image = obj.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            obj.SetActive(false);
            return image;
        }

        private static void SetupLayerImageFromSource(Image proxyImage, RectTransform proxyRect, Image sourceImage, RectTransform sourceRect, Vector2 layerPosition)
        {
            if (proxyImage == null || proxyRect == null || sourceImage == null || sourceRect == null)
            {
                return;
            }

            proxyRect.sizeDelta = sourceRect.rect.size;
            proxyRect.pivot = sourceRect.pivot;
            proxyRect.anchoredPosition = layerPosition;
            proxyRect.localScale = sourceRect.localScale;
            proxyRect.localRotation = sourceRect.localRotation;
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

        private static IEnumerator Wait(float duration)
        {
            float wait = Mathf.Max(0f, duration);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }
        }

        private static Vector2 ResolveAnchor(
            SkillAnimationAnchor anchor,
            RectTransform layerRect,
            RectTransform casterRect,
            RectTransform targetRect,
            Vector2 originalActorPosition)
        {
            switch (anchor)
            {
                case SkillAnimationAnchor.Original:
                case SkillAnimationAnchor.Caster:
                    return originalActorPosition;

                case SkillAnimationAnchor.Current:
                    return originalActorPosition;

                case SkillAnimationAnchor.Target:
                    return WorldCenterToLayerPosition(layerRect, targetRect);

                case SkillAnimationAnchor.ScreenCenter:
                    return new Vector2(0f, 0f);

                case SkillAnimationAnchor.ScreenTop:
                    return new Vector2(0f, layerRect.rect.height * 0.4f);

                case SkillAnimationAnchor.ScreenBottom:
                    return new Vector2(0f, -layerRect.rect.height * 0.4f);

                default:
                    return originalActorPosition;
            }
        }

        private static Vector2 WorldCenterToLayerPosition(RectTransform layerRect, RectTransform sourceRect)
        {
            if (layerRect == null || sourceRect == null)
            {
                return Vector2.zero;
            }

            Vector3 world = sourceRect.TransformPoint(sourceRect.rect.center);
            Vector3 local = layerRect.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }
    }
}
'@

Set-Content -Path $playerPath -Value $code -Encoding UTF8
Write-Host 'Patched SkillAnimationPlayer to animate on a Canvas-level SkillAnimationLayer.'
