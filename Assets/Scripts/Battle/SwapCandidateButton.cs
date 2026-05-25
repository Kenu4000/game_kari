using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameKari.UI;

namespace GameKari.Battle
{
    /// <summary>
    /// One selectable reserve character entry used by the Swap panel.
    /// This component owns only the UI object state; the actual swap rule stays in BattleUIManager.
    /// </summary>
    public sealed class SwapCandidateButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image faceIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text mpText;
        [SerializeField] private GameObject disabledOverlay;

        [Header("Optional Sprites")]
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField] private Sprite disabledSprite;

        private BattleUnit _unit;
        private Action<BattleUnit> _onClicked;
        private bool _baseInteractable;

        private void Awake()
        {
            CacheMissingReferences();
        }

        public void Setup(BattleUnit unit, bool interactable, Action<BattleUnit> onClicked)
        {
            CacheMissingReferences();

            _unit = unit;
            _baseInteractable = interactable;
            _onClicked = onClicked;

            ApplyTexts(unit);
            ApplyFaceIcon(unit);
            ApplySprites();
            ApplyInteractable(interactable);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClicked);
            }
        }

        public void SetInteractable(bool interactable)
        {
            ApplyInteractable(_baseInteractable && interactable);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHoverVisual(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHoverVisual(false);
        }

        private void HandleClicked()
        {
            if (_unit == null || !_baseInteractable)
            {
                return;
            }

            _onClicked?.Invoke(_unit);
        }

        private void CacheMissingReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (nameText == null)
            {
                nameText = transform.Find("NameText")?.GetComponent<TMP_Text>();
            }

            if (hpText == null)
            {
                hpText = transform.Find("HpText")?.GetComponent<TMP_Text>();
            }

            if (mpText == null)
            {
                mpText = transform.Find("MpText")?.GetComponent<TMP_Text>();
            }

            if (faceIcon == null)
            {
                faceIcon = transform.Find("FaceIcon")?.GetComponent<Image>();
            }

            if (disabledOverlay == null)
            {
                Transform overlay = transform.Find("DisabledOverlay");
                disabledOverlay = overlay == null ? null : overlay.gameObject;
            }
        }

        private void ApplyTexts(BattleUnit unit)
        {
            if (unit == null)
            {
                SetText(nameText, "-");
                SetText(hpText, "");
                SetText(mpText, "");
                return;
            }

            string unitName = string.IsNullOrEmpty(unit.Name) ? "Reserve" : unit.Name;
            int maxHp = unit.Data == null ? 0 : unit.Data.MaxHP;
            int maxMp = unit.Data == null ? 0 : unit.Data.MaxMP;

            SetText(nameText, unit.IsDead ? $"{unitName} KO" : unitName);
            SetText(hpText, $"HP {unit.CurrentHP}/{maxHp}");
            SetText(mpText, $"MP {unit.CurrentMP}/{maxMp}");
        }

        private void ApplyFaceIcon(BattleUnit unit)
        {
            if (faceIcon == null)
            {
                return;
            }

            Sprite icon = unit == null || unit.Data == null ? null : unit.Data.FaceIcon;
            faceIcon.sprite = icon;
            faceIcon.enabled = icon != null;
        }

        private void ApplySprites()
        {
            UISpriteStateVisual visual = GetComponent<UISpriteStateVisual>();
            if (visual != null)
            {
                visual.SetSprites(normalSprite, hoverSprite, null, disabledSprite);
            }
        }

        private void ApplyInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }

            if (disabledOverlay != null)
            {
                disabledOverlay.SetActive(!interactable);
            }

            UISpriteStateVisual visual = GetComponent<UISpriteStateVisual>();
            if (visual != null)
            {
                visual.SetDisabledVisual(!interactable);
            }

            SetAlpha(interactable ? 1f : 0.45f);
        }

        private void SetHoverVisual(bool hovered)
        {
            if (button != null && !button.interactable)
            {
                return;
            }

            UISpriteStateVisual visual = GetComponent<UISpriteStateVisual>();
            if (visual != null)
            {
                // UISpriteStateVisual handles pointer events on the same GameObject.
                // This method is kept for future expansion and does not override it.
                return;
            }

            if (backgroundImage != null)
            {
                Sprite nextSprite = hovered && hoverSprite != null ? hoverSprite : normalSprite;
                if (nextSprite != null)
                {
                    backgroundImage.sprite = nextSprite;
                }
            }
        }

        private void SetAlpha(float alpha)
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = alpha;
        }

        private static void SetText(TMP_Text target, string text)
        {
            if (target != null)
            {
                target.text = text;
            }
        }
    }
}
