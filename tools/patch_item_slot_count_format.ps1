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

$text = InsertBeforeIfMissing $text 'private string countFormat' '        private InventoryItem _inventoryItem;' @'
        [Header("Display Text")]
        [SerializeField] private string countFormat = "所持数: {0}";

'@ 'countFormat field'

$text = ReplaceOptional $text @'
            return $"所持数: {Mathf.Max(0, inventoryItem.Count)}\n{description}";
'@ @'
            return $"{BuildCountText(inventoryItem.Count)}\n{description}";
'@ 'count text format usage'

$helper = @'
        private string BuildCountText(int count)
        {
            string format = string.IsNullOrWhiteSpace(countFormat)
                ? "所持数: {0}"
                : countFormat;

            try
            {
                return string.Format(format, Mathf.Max(0, count));
            }
            catch (FormatException)
            {
                return $"所持数: {Mathf.Max(0, count)}";
            }
        }

'@

$text = InsertBeforeIfMissing $text 'private string BuildCountText(int count)' '        private static string BuildEffectText(ItemData item)' $helper 'BuildCountText helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched ItemSlotView count format setting.'
