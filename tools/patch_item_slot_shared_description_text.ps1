$ErrorActionPreference = 'Stop'

$slotPath = 'Assets/Scripts/Battle/ItemSlotView.cs'
$commandPath = 'Assets/Scripts/Battle/CommandPanelController.cs'

foreach ($path in @($slotPath, $commandPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
}

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

# ItemSlotView: send hover description to CommandPanelController and disable local EffectText by default.
$slotText = Get-Content -Path $slotPath -Raw -Encoding UTF8

$slotText = InsertBeforeIfMissing $slotText 'private bool useLocalEffectText' '        [Header("Display Text")]' @'
        [Header("Local Effect Text")]
        [SerializeField] private bool useLocalEffectText = false;

'@ 'useLocalEffectText field'

$slotText = ReplaceOptional $slotText '        [SerializeField] private string countFormat = "謇謖∵焚: {0}";' '        [SerializeField] private string countFormat = "x{0}";' 'countFormat mojibake default'
$slotText = ReplaceOptional $slotText '                ? "謇謖∵焚: {0}"' '                ? "x{0}"' 'BuildCountText default'
$slotText = ReplaceOptional $slotText '                return $"謇謖∵焚: {Mathf.Max(0, count)}";' '                return $"x{Mathf.Max(0, count)}";' 'BuildCountText fallback'

$slotText = ReplaceOptional $slotText '        private Action<InventoryItem> _onHovered;' '        private Action<string> _onHovered;' 'hover action type'
$slotText = ReplaceOptional $slotText '            Action<InventoryItem> onHovered,' '            Action<string> onHovered,' 'SetItem hover action parameter'

$slotText = ReplaceOptional $slotText @'
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_inventoryItem != null && _inventoryItem.Item != null)
            {
                SetText(effectText, BuildSlotEffectText(_inventoryItem));
                SetEffectTextVisible(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetText(effectText, string.Empty);
            SetEffectTextVisible(false);
            _onHoverExit?.Invoke();
        }
'@ @'
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_inventoryItem != null && _inventoryItem.Item != null)
            {
                string description = BuildSlotEffectText(_inventoryItem);
                _onHovered?.Invoke(description);

                if (useLocalEffectText)
                {
                    SetText(effectText, description);
                    SetEffectTextVisible(true);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (useLocalEffectText)
            {
                SetText(effectText, string.Empty);
                SetEffectTextVisible(false);
            }

            _onHoverExit?.Invoke();
        }
'@ 'pointer hover shared description'

Set-Content -Path $slotPath -Value $slotText -Encoding UTF8

# CommandPanelController: add a fixed item description text target and use it for ItemSlot hover.
$text = Get-Content -Path $commandPath -Raw -Encoding UTF8

$text = InsertBeforeIfMissing $text 'private TMP_Text itemDescriptionText' '        [Header("Description")]' @'
        [Header("Item Description")]
        [SerializeField] private TMP_Text itemDescriptionText;

'@ 'itemDescriptionText field'

$text = ReplaceOptional $text 'slotView.SetItem(GetItemAt(i), OnItemClicked, HandleItemHovered, ClearDescription);' 'slotView.SetItem(GetItemAt(i), OnItemClicked, HandleItemHovered, ClearItemDescription);' 'manual item slot hover clear'
$text = ReplaceOptional $text 'slotView.SetItem(visibleItems[i], OnItemClicked, HandleItemHovered, ClearDescription);' 'slotView.SetItem(visibleItems[i], OnItemClicked, HandleItemHovered, ClearItemDescription);' 'generated item slot hover clear'

$text = ReplaceOptional $text @'
        private void HandleItemHovered(InventoryItem inventoryItem)
        {
            if (descriptionText != null)
            {
                descriptionText.text = BuildItemDescription(inventoryItem);
            }
        }
'@ @'
        private void HandleItemHovered(string description)
        {
            TMP_Text targetText = itemDescriptionText != null ? itemDescriptionText : descriptionText;
            if (targetText != null)
            {
                targetText.text = description ?? string.Empty;
            }
        }

        private void ClearItemDescription()
        {
            TMP_Text targetText = itemDescriptionText != null ? itemDescriptionText : descriptionText;
            if (targetText != null)
            {
                targetText.text = string.Empty;
            }
        }
'@ 'HandleItemHovered shared text'

Set-Content -Path $commandPath -Value $text -Encoding UTF8
Write-Host 'Patched item slots to use a shared fixed description text.'
