using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    [DisallowMultipleComponent]
    public sealed class FloatingHPBarView : MonoBehaviour
    {
        [SerializeField] private Transform fill;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float showSeconds = 1.0f;
        [SerializeField] private float fadeSeconds = 0.18f;

        private Coroutine _hideRoutine;

        private void Awake()
        {
            AutoBindMissingReferences();
            HideImmediate();
        }

        public void Show(int current, int max, float hpAnimationSeconds, float visibleSeconds, float fadeOutSeconds)
        {
            ShowTransition(current, current, max, hpAnimationSeconds, visibleSeconds, fadeOutSeconds);
        }

        public void ShowTransition(int previous, int current, int max, float hpAnimationSeconds, float visibleSeconds, float fadeOutSeconds)
        {
            AutoBindMissingReferences();

            showSeconds = Mathf.Max(0f, visibleSeconds);
            fadeSeconds = Mathf.Max(0f, fadeOutSeconds);

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            SetFillImmediate(previous, max);
            SetFill(current, max, hpAnimationSeconds);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private void SetFillImmediate(int current, int max)
        {
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)Mathf.Max(0, current) / max);
            HPBarFillAnimator animator = fill.GetComponent<HPBarFillAnimator>();
            if (animator == null)
            {
                animator = fill.gameObject.AddComponent<HPBarFillAnimator>();
            }

            animator.SetFillImmediate(rate);
        }
        private void SetFill(int current, int max, float hpAnimationSeconds)
        {
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)Mathf.Max(0, current) / max);
            HPBarFillAnimator animator = fill.GetComponent<HPBarFillAnimator>();
            if (animator == null)
            {
                animator = fill.gameObject.AddComponent<HPBarFillAnimator>();
            }

            animator.SetAnimationSeconds(Mathf.Max(0f, hpAnimationSeconds));
            animator.SetFill(rate);
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(showSeconds);

            float duration = Mathf.Max(0f, fadeSeconds);
            if (canvasGroup == null || duration <= 0f)
            {
                HideImmediate();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            HideImmediate();
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);
            _hideRoutine = null;
        }

        private void AutoBindMissingReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (fill == null)
            {
                Transform found = transform.Find("HPBarBG/Fill");
                if (found == null)
                {
                    found = transform.Find("Fill");
                }

                fill = found;
            }
        }
    }
}

