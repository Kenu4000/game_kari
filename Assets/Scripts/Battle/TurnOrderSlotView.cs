using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    [DisallowMultipleComponent]
    public sealed class TurnOrderSlotView : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text sideText;
        [SerializeField] private TMP_Text mpText;
        [SerializeField] private TMP_Text stateText;

        [Header("Display Format")]
        [SerializeField] private string mpFormat = "MP {0}";
        [SerializeField] private string allyLabel = "A";
        [SerializeField] private string enemyLabel = "E";
        [SerializeField] private string currentLabel = "NOW";
        [SerializeField] private string actedLabel = "DONE";

        [Header("Colors")]
        [SerializeField] private Color allyColor = new Color(0.45f, 0.75f, 1f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color currentColor = new Color(1f, 0.9f, 0.35f, 1f);
        [SerializeField] private float actedAlpha = 0.45f;

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
            }

            SetText(nameText, unit.Name);
            SetText(sideText, isAlly ? allyLabel : enemyLabel);
            SetText(mpText, BuildMpText(unit));
            SetText(stateText, isCurrent ? currentLabel : isActed ? actedLabel : string.Empty);

            ApplyVisualState(isAlly, isCurrent, isActed);
        }

        private void AutoBindMissingReferences()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (iconImage == null)
            {
                iconImage = FindChildImage("IconImage");
            }

            if (nameText == null)
            {
                nameText = FindChildText("NameText");
            }

            if (sideText == null)
            {
                sideText = FindChildText("SideText");
            }

            if (mpText == null)
            {
                mpText = FindChildText("MPText");
            }

            if (stateText == null)
            {
                stateText = FindChildText("StateText");
            }
        }

        private string BuildMpText(BattleUnit unit)
        {
            int mp = unit == null ? 0 : Mathf.Max(0, unit.CurrentMP);
            string format = string.IsNullOrWhiteSpace(mpFormat) ? "MP {0}" : mpFormat;

            try
            {
                return string.Format(format, mp);
            }
            catch (System.FormatException)
            {
                return $"MP {mp}";
            }
        }

        private void ApplyVisualState(bool isAlly, bool isCurrent, bool isActed)
        {
            if (backgroundImage == null)
            {
                return;
            }

            Color color = isCurrent ? currentColor : isAlly ? allyColor : enemyColor;
            if (isActed && !isCurrent)
            {
                color.a *= Mathf.Clamp01(actedAlpha);
            }

            backgroundImage.color = color;
        }

        private TMP_Text FindChildText(string childName)
        {
            Transform found = transform.Find(childName);
            return found == null ? null : found.GetComponent<TMP_Text>();
        }

        private Image FindChildImage(string childName)
        {
            Transform found = transform.Find(childName);
            return found == null ? null : found.GetComponent<Image>();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
