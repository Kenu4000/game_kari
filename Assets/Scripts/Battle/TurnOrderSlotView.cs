using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    [DisallowMultipleComponent]
    public sealed class TurnOrderSlotView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image currentFrame;

        [Header("Visual State")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color actedColor = new Color(0.35f, 0.35f, 0.35f, 0.65f);
        [SerializeField] private Color enemyColor = Color.white;
        [SerializeField] private Color currentFrameColor = new Color(1f, 0.9f, 0.25f, 1f);

        private void Awake()
        {
            AutoBindMissingReferences();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetUnit(BattleUnit unit, bool isAlly, bool isCurrent, bool isActed)
        {
            AutoBindMissingReferences();

            bool hasUnit = unit != null && !unit.IsDead && unit.Data != null;
            SetVisible(hasUnit);
            if (!hasUnit)
            {
                return;
            }

            Sprite icon = unit.Data.FaceIcon != null ? unit.Data.FaceIcon : unit.Data.BattleSprite;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
                iconImage.color = isActed ? actedColor : isAlly ? normalColor : enemyColor;
            }

            if (currentFrame != null)
            {
                currentFrame.gameObject.SetActive(isCurrent);
                currentFrame.color = currentFrameColor;
                currentFrame.raycastTarget = false;
            }
        }

        private void AutoBindMissingReferences()
        {
            if (iconImage == null)
            {
                iconImage = FindChildImage("IconImage");
            }

            if (currentFrame == null)
            {
                currentFrame = FindChildImage("CurrentFrame");
            }
        }

        private Image FindChildImage(string childName)
        {
            Transform found = transform.Find(childName);
            return found == null ? null : found.GetComponent<Image>();
        }
    }
}
