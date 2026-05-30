using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameKari.UI;

namespace GameKari.Battle
{
    [DisallowMultipleComponent]
    public sealed class SwapSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image characterIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text mpText;

        [Header("MP Badge")]
        [SerializeField] private Image mpBadgeImage;
        [SerializeField] private TMP_Text mpBadgeText;
        [SerializeField] private Sprite mpBadgeNormalSprite;
        [SerializeField] private Sprite mpBadgeZeroSprite;
        [SerializeField] private Sprite mpBadgeEmptySprite;

        [SerializeField] private GameObject emptyRoot;
        [SerializeField] private GameObject filledRoot;
        [SerializeField] private UISpriteStateVisual spriteStateVisual;

        private BattleUnit _unit;
        private Action<BattleUnit> _onClicked;

        private void Awake()
        {
            AutoBindMissingReferences();
            HookButton();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        public void SetUnit(BattleUnit unit, Action<BattleUnit> onClicked)
        {
            AutoBindMissingReferences();
            HookButton();

            _unit = unit;
            _onClicked = onClicked;

            bool hasUnit = unit != null && !unit.IsDead && unit.Data != null;

            if (button != null)
            {
                button.interactable = hasUnit;
            }

            if (spriteStateVisual != null)
            {
                spriteStateVisual.SetDisabledVisual(!hasUnit);
                spriteStateVisual.ClearPointerState();
            }

            if (emptyRoot != null)
            {
                emptyRoot.SetActive(!hasUnit);
            }

            if (filledRoot != null)
            {
                filledRoot.SetActive(hasUnit);
            }

            if (!hasUnit)
            {
                SetEmptyDisplay();
                return;
            }

            SetFilledDisplay(unit);
        }

        private void AutoBindMissingReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (spriteStateVisual == null)
            {
                spriteStateVisual = GetComponent<UISpriteStateVisual>();
            }

            if (characterIcon == null)
            {
                characterIcon = FindChildImage("CharacterIcon");
            }

            if (nameText == null)
            {
                nameText = FindChildText("NameText");
            }

            if (hpText == null)
            {
                hpText = FindChildText("HPText");
            }

            if (mpText == null)
            {
                mpText = FindChildText("MPText");
            }

            if (mpBadgeImage == null)
            {
                mpBadgeImage = FindChildImage("MpBadge");
            }

            if (mpBadgeText == null)
            {
                mpBadgeText = FindChildText("MpBadgeText");
            }
        }

        private void HookButton()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
        }

        private void HandleClicked()
        {
            if (_unit == null || _unit.IsDead)
            {
                return;
            }

            _onClicked?.Invoke(_unit);
        }

        private void SetEmptyDisplay()
        {
            if (characterIcon != null)
            {
                characterIcon.enabled = false;
                characterIcon.sprite = null;
            }

            SetText(nameText, "-");
            SetText(hpText, string.Empty);
            SetText(mpText, string.Empty);
            SetText(mpBadgeText, "-");
            ApplyMpBadgeSprite(null);
        }

        private void SetFilledDisplay(BattleUnit unit)
        {
            if (characterIcon != null)
            {
                Sprite icon = unit.Data.FaceIcon != null ? unit.Data.FaceIcon : unit.Data.BattleSprite;
                characterIcon.sprite = icon;
                characterIcon.enabled = icon != null;
            }

            SetText(nameText, unit.Name);
            SetText(hpText, $"HP {unit.CurrentHP}/{unit.Data.MaxHP}");
            SetText(mpText, $"MP {unit.CurrentMP}/{unit.Data.MaxMP}");
            SetText(mpBadgeText, Mathf.Max(0, unit.CurrentMP).ToString());
            ApplyMpBadgeSprite(unit);
        }

        private void ApplyMpBadgeSprite(BattleUnit unit)
        {
            if (mpBadgeImage == null)
            {
                return;
            }

            Sprite sprite = null;
            if (unit == null || unit.IsDead || unit.Data == null)
            {
                sprite = mpBadgeEmptySprite;
            }
            else if (unit.CurrentMP <= 0)
            {
                sprite = mpBadgeZeroSprite != null ? mpBadgeZeroSprite : mpBadgeNormalSprite;
            }
            else
            {
                sprite = mpBadgeNormalSprite;
            }

            if (sprite != null)
            {
                mpBadgeImage.sprite = sprite;
            }
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
                text.text = value;
            }
        }
    }
}


