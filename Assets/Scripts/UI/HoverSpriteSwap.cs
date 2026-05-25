using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameKari.UI
{
    /// <summary>
    /// Swaps an Image sprite while the mouse cursor is over the UI object.
    /// Attach this to a UI object with an Image, or assign Target Image manually.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverSpriteSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField] private bool restoreOnDisable = true;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage != null && normalSprite == null)
            {
                normalSprite = targetImage.sprite;
            }
        }

        private void OnEnable()
        {
            ApplyNormalSprite();
        }

        private void OnDisable()
        {
            if (restoreOnDisable)
            {
                ApplyNormalSprite();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (targetImage == null || hoverSprite == null)
            {
                return;
            }

            targetImage.sprite = hoverSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ApplyNormalSprite();
        }

        private void ApplyNormalSprite()
        {
            if (targetImage == null || normalSprite == null)
            {
                return;
            }

            targetImage.sprite = normalSprite;
        }
    }
}
