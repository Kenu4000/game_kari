using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameKari.UI
{
    /// <summary>
    /// Sprite-state visual controller for UI Images.
    /// Supports hover, selected, and disabled-looking states without relying on Button Color Tint.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UISpriteStateVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite disabledSprite;
        [SerializeField] private bool restoreOnDisable = true;

        private bool _hovered;
        private bool _selected;
        private bool _disabledVisual;

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

            ApplyCurrentSprite();
        }

        private void OnEnable()
        {
            ApplyCurrentSprite();
        }

        private void OnDisable()
        {
            _hovered = false;

            if (restoreOnDisable)
            {
                ApplyNormalSprite();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyCurrentSprite();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ApplyCurrentSprite();
        }

        public void SetSprites(Sprite normal, Sprite hover, Sprite selected, Sprite disabled)
        {
            normalSprite = normal != null ? normal : normalSprite;
            hoverSprite = hover;
            selectedSprite = selected;
            disabledSprite = disabled;
            ApplyCurrentSprite();
        }
        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyCurrentSprite();
        }

        public void SetDisabledVisual(bool disabled)
        {
            _disabledVisual = disabled;
            ApplyCurrentSprite();
        }

        public void ClearPointerState()
        {
            _hovered = false;
            ApplyCurrentSprite();
        }

        private void ApplyCurrentSprite()
        {
            if (targetImage == null)
            {
                return;
            }

            Sprite nextSprite = GetCurrentSprite();
            if (nextSprite != null)
            {
                targetImage.sprite = nextSprite;
            }
        }

        private Sprite GetCurrentSprite()
        {
            if (_disabledVisual && disabledSprite != null)
            {
                return disabledSprite;
            }

            if (_selected && selectedSprite != null)
            {
                return selectedSprite;
            }

            if (_hovered && hoverSprite != null)
            {
                return hoverSprite;
            }

            return normalSprite;
        }

        private void ApplyNormalSprite()
        {
            if (targetImage != null && normalSprite != null)
            {
                targetImage.sprite = normalSprite;
            }
        }
    }
}




