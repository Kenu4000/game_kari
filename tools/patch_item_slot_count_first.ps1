$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/ItemSlotView.cs'
if (!(Test-Path $path)) { throw 'ItemSlotView.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
            return $"{description}\n所持数: {Mathf.Max(0, inventoryItem.Count)}";
'@

$new = @'
            return $"所持数: {Mathf.Max(0, inventoryItem.Count)}\n{description}";
'@

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
}
elseif ($text.Contains('return $"所持数: {Mathf.Max(0, inventoryItem.Count)}\n{description}";')) {
    Write-Host 'Item count is already shown first.'
}
else {
    throw 'Patch anchor not found: item count line'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched ItemSlotView to show item count first.'
