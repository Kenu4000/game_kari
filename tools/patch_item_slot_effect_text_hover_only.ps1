$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/ItemSlotView.cs'
if (!(Test-Path $path)) { throw 'ItemSlotView.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

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

$text = ReplaceOptional $text @'
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
'@ @'
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_inventoryItem != null && _inventoryItem.Item != null)
            {
                SetText(effectText, BuildSlotEffectText(_inventoryItem.Item));
                SetEffectTextVisible(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetText(effectText, string.Empty);
            SetEffectTextVisible(false);
            _onHoverExit?.Invoke();
        }
'@ 'hover effect text behavior'

$text = ReplaceOptional $text @'
            SetText(nameText, "-");
            SetText(countText, string.Empty);
            SetText(effectText, string.Empty);
        }
'@ @'
            SetText(nameText, "-");
            SetText(countText, string.Empty);
            SetText(effectText, string.Empty);
            SetEffectTextVisible(false);
        }
'@ 'empty hides effect text'

$text = ReplaceOptional $text @'
            SetText(nameText, item.ItemName);
            SetText(countText, $"x{Mathf.Max(0, inventoryItem.Count)}");
            SetText(effectText, BuildSlotEffectText(item));
        }
'@ @'
            SetText(nameText, item.ItemName);
            SetText(countText, $"x{Mathf.Max(0, inventoryItem.Count)}");
            SetText(effectText, string.Empty);
            SetEffectTextVisible(false);
        }
'@ 'filled hides effect text until hover'

$helper = @'
        private void SetEffectTextVisible(bool visible)
        {
            if (effectText != null)
            {
                effectText.gameObject.SetActive(visible);
            }
        }

'@

$text = InsertBeforeIfMissing $text 'private void SetEffectTextVisible(bool visible)' '        private static string BuildSlotEffectText(ItemData item)' $helper 'SetEffectTextVisible helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched ItemSlotView to show EffectText only while hovered.'
