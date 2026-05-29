using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameKari.UI;

namespace GameKari.Battle
{
    [DisallowMultipleComponent]
    public sealed class ItemSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private GameObject emptyRoot;
        [SerializeField] private GameObject filledRoot;
        [SerializeField] private UISpriteStateVisual spriteStateVisual;

        private InventoryItem _inventoryItem;
        private Action<InventoryItem> _onClicked;
        private Action<InventoryItem> _onHovered;
        private Action _onHoverExit;

        private void Awake()
        {
            AutoBindMissingReferences();
            HookButton();
        }

        public void SetItem(
            InventoryItem inventoryItem,
            Action<InventoryItem> onClicked,
            Action<InventoryItem> onHovered,
            Action onHoverExit)
        {
            AutoBindMissingReferences();
            HookButton();

            _inventoryItem = inventoryItem;
            _onClicked = onClicked;
            _onHovered = onHovered;
            _onHoverExit = onHoverExit;

            bool hasItem = inventoryItem != null && inventoryItem.Item != null;
            bool usable = hasItem && inventoryItem.Count > 0;

            if (button != null)
            {
                button.interactable = usable;
            }

            if (spriteStateVisual != null)
            {
                spriteStateVisual.SetDisabledVisual(!usable);
                spriteStateVisual.ClearPointerState();
            }

            if (emptyRoot != null)
            {
                emptyRoot.SetActive(!hasItem);
            }

            if (filledRoot != null)
            {
                filledRoot.SetActive(hasItem);
            }

            if (!hasItem)
            {
                SetEmptyDisplay();
                return;
            }

            SetFilledDisplay(inventoryItem);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_inventoryItem != null && _inventoryItem.Item != null)
            {
                _onHovered?.Invoke(_inventoryItem);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onHoverExit?.Invoke();
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

            if (iconImage == null)
            {
                iconImage = FindChildImage("IconImage");
            }

            if (nameText == null)
            {
                nameText = FindChildText("NameText");
            }

            if (countText == null)
            {
                countText = FindChildText("CountText");
            }

            if (effectText == null)
            {
                effectText = FindChildText("EffectText");
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
            if (_inventoryItem == null || _inventoryItem.Item == null || _inventoryItem.Count <= 0)
            {
                return;
            }

            _onClicked?.Invoke(_inventoryItem);
        }

        private void SetEmptyDisplay()
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }

            SetText(nameText, "-");
            SetText(countText, string.Empty);
            SetText(effectText, string.Empty);
        }

        private void SetFilledDisplay(InventoryItem inventoryItem)
        {
            ItemData item = inventoryItem.Item;

            if (iconImage != null)
            {
                iconImage.sprite = item.Icon;
                iconImage.enabled = item.Icon != null;
            }

            SetText(nameText, item.ItemName);
            SetText(countText, $"x{Mathf.Max(0, inventoryItem.Count)}");
            SetText(effectText, BuildEffectText(item));
        }

        private static string BuildEffectText(ItemData item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            switch (item.Kind)
            {
                case ItemKind.Pass:
                    return "Pass";

                case ItemKind.Heal:
                default:
                    return $"Heal {Mathf.Max(0, item.HealAmount)}";
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
