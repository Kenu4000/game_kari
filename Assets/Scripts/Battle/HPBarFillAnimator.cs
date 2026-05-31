using System.Collections;
using UnityEngine;

namespace GameKari.Battle
{
    [DisallowMultipleComponent]
    public sealed class HPBarFillAnimator : MonoBehaviour
    {
        [SerializeField] private float animationSeconds = 0.35f;

        private Coroutine _animation;
        private bool _initialized;

        public void SetFill(float targetRate)
        {
            targetRate = Mathf.Clamp01(targetRate);

            if (!Application.isPlaying)
            {
                ApplyFill(targetRate);
                return;
            }

            if (!_initialized)
            {
                _initialized = true;
                ApplyFill(targetRate);
                return;
            }

            if (_animation != null)
            {
                StopCoroutine(_animation);
            }

            _animation = StartCoroutine(AnimateFill(targetRate));
        }

        public void SetFillImmediate(float targetRate)
        {
            targetRate = Mathf.Clamp01(targetRate);

            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }

            _initialized = true;
            ApplyFill(targetRate);
        }
        public void SetAnimationSeconds(float seconds)
        {
            animationSeconds = Mathf.Max(0f, seconds);
        }

        private IEnumerator AnimateFill(float targetRate)
        {
            float startRate = transform.localScale.x;
            float duration = Mathf.Max(0f, animationSeconds);

            if (duration <= 0f)
            {
                ApplyFill(targetRate);
                _animation = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                ApplyFill(Mathf.Lerp(startRate, targetRate, eased));
                yield return null;
            }

            ApplyFill(targetRate);
            _animation = null;
        }

        private void ApplyFill(float rate)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Clamp01(rate);
            transform.localScale = scale;
        }
    }
}

