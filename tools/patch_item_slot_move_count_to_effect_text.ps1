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
                SetText(effectText, BuildSlotEffectText(_inventoryItem.Item));
                SetEffectTextVisible(true);
'@ @'
                SetText(effectText, BuildSlotEffectText(_inventoryItem));
                SetEffectTextVisible(true);
'@ 'hover uses inventory item'

$text = ReplaceOptional $text @'
            SetText(nameText, item.ItemName);
            SetText(countText, $"x{Mathf.Max(0, inventoryItem.Count)}");
            SetText(effectText, string.Empty);
            SetEffectTextVisible(false);
        }
'@ @'
            SetText(nameText, item.ItemName);
            SetText(countText, string.Empty);
            SetText(effectText, string.Empty);
            SetEffectTextVisible(false);
        }
'@ 'hide count text normally'

$text = ReplaceOptional $text @'
        private static string BuildSlotEffectText(ItemData item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                return item.Description;
            }

            return BuildEffectText(item);
        }
'@ @'
        private static string BuildSlotEffectText(InventoryItem inventoryItem)
        {
            if (inventoryItem == null || inventoryItem.Item == null)
            {
                return string.Empty;
            }

            ItemData item = inventoryItem.Item;
            string description = !string.IsNullOrWhiteSpace(item.Description)
                ? item.Description
                : BuildEffectText(item);

            return $"{description}\n所持数: {Mathf.Max(0, inventoryItem.Count)}";
        }
'@ 'BuildSlotEffectText inventory version'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched ItemSlotView to show item count in EffectText.'
