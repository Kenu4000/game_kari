$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }

    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }

    Write-Host "Inserted: $label"
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

# Enemy status slots created by older UI may use "Icon" instead of "FaceIcon".
# Try FaceIcon, Icon, then IconImage.
$text = ReplaceOptional $text '            Transform iconTransform = FindChildRecursive(slot, "FaceIcon");' '            Transform iconTransform = FindStatusIconTransform(slot);' 'use flexible status icon lookup'
$text = ReplaceOptional $text '            Transform iconTransform = slot.Find("FaceIcon");' '            Transform iconTransform = FindStatusIconTransform(slot);' 'replace direct FaceIcon lookup with flexible lookup'

$helper = @'
        private static Transform FindStatusIconTransform(Transform slot)
        {
            if (slot == null)
            {
                return null;
            }

            Transform icon = FindChildRecursive(slot, "FaceIcon");
            if (icon != null)
            {
                return icon;
            }

            icon = FindChildRecursive(slot, "Icon");
            if (icon != null)
            {
                return icon;
            }

            return FindChildRecursive(slot, "IconImage");
        }

'@
$text = InsertBeforeIfMissing $text 'private static Transform FindStatusIconTransform(Transform slot)' '        private static void SetStatusFaceIcon(Transform slot, BattleUnit unit)' $helper 'FindStatusIconTransform helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched status icon lookup to support FaceIcon, Icon, and IconImage.'
