$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/ItemSlotView.cs'
if (!(Test-Path $path)) { throw 'ItemSlotView.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = '        private static string BuildSlotEffectText(InventoryItem inventoryItem)'
$new = '        private string BuildSlotEffectText(InventoryItem inventoryItem)'

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
}
elseif ($text.Contains($new)) {
    Write-Host 'BuildSlotEffectText is already non-static.'
}
else {
    throw 'Patch anchor not found: BuildSlotEffectText signature'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Fixed BuildSlotEffectText static call issue.'
