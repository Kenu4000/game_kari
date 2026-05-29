$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/ItemSlotView.cs'
if (!(Test-Path $path)) { throw 'ItemSlotView.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
            SetText(nameText, item.ItemName);
            SetText(countText, $"x{Mathf.Max(0, inventoryItem.Count)}");
            SetText(effectText, BuildEffectText(item));
        }
'@

$new = @'
            SetText(nameText, item.ItemName);
            SetText(countText, $"x{Mathf.Max(0, inventoryItem.Count)}");
            SetText(effectText, BuildEffectText(item));
        }
'@

# The here-string above keeps literal backslashes when file text contains normal C# quotes.
# Use a simpler literal replacement for the actual C# source.
$oldActual = '            SetText(nameText, item.ItemName);
            SetText(countText, $"x{Mathf.Max(0, inventoryItem.Count)}");
            SetText(effectText, BuildEffectText(item));
        }
'

$newActual = '            SetText(nameText, item.ItemName);
            SetText(countText, $"x{Mathf.Max(0, inventoryItem.Count)}");
            SetText(effectText, BuildSlotEffectText(item));
        }
'

if ($text.Contains($oldActual)) {
    $text = $text.Replace($oldActual, $newActual)
}
elseif ($text.Contains('SetText(effectText, BuildSlotEffectText(item));')) {
    Write-Host 'EffectText already uses BuildSlotEffectText.'
}
else {
    $oldLine = '            SetText(effectText, BuildEffectText(item));'
    $newLine = '            SetText(effectText, BuildSlotEffectText(item));'
    if (!$text.Contains($oldLine)) { throw 'Patch anchor not found: effect text line' }
    $text = $text.Replace($oldLine, $newLine)
}

$insert = @'
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

'@

if (!$text.Contains('private static string BuildSlotEffectText(ItemData item)')) {
    $anchor = '        private static string BuildEffectText(ItemData item)'
    $index = $text.IndexOf($anchor)
    if ($index -lt 0) { throw 'Patch anchor not found: BuildEffectText' }
    $text = $text.Substring(0, $index) + $insert + $text.Substring($index)
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched ItemSlotView to show ItemData.Description in EffectText.'
