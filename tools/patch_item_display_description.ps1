$ErrorActionPreference = 'Stop'

$itemPath = 'Assets/Scripts/Battle/ItemData.cs'
$slotPath = 'Assets/Scripts/Battle/ItemSlotView.cs'

foreach ($path in @($itemPath, $slotPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
}

# ItemData: add a UI-specific display description.
$itemText = Get-Content -Path $itemPath -Raw -Encoding UTF8
$itemText = ReplaceOptional $itemText @'
        public string ItemName;
        [TextArea] public string Description;
        public Sprite Icon;
'@ @'
        public string ItemName;
        [TextArea] public string Description;
        [TextArea] public string DisplayDescription;
        public Sprite Icon;
'@ 'ItemData DisplayDescription field'
Set-Content -Path $itemPath -Value $itemText -Encoding UTF8

# ItemSlotView: prefer DisplayDescription, then Description, then generated effect text.
$slotText = Get-Content -Path $slotPath -Raw -Encoding UTF8
$slotText = ReplaceOptional $slotText @'
            string description = !string.IsNullOrWhiteSpace(item.Description)
                ? item.Description
                : BuildEffectText(item);

            return $"{description}\n所持数: {Mathf.Max(0, inventoryItem.Count)}";
'@ @'
            string description = !string.IsNullOrWhiteSpace(item.DisplayDescription)
                ? item.DisplayDescription
                : !string.IsNullOrWhiteSpace(item.Description)
                    ? item.Description
                    : BuildEffectText(item);

            return $"{description}\n所持数: {Mathf.Max(0, inventoryItem.Count)}";
'@ 'ItemSlotView DisplayDescription priority'
Set-Content -Path $slotPath -Value $slotText -Encoding UTF8

Write-Host 'Patched ItemData.DisplayDescription and ItemSlotView display priority.'
