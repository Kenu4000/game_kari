$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

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

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$oldSpriteLine = '            Sprite sprite = unit == null || unit.Data == null ? null : unit.Data.FaceIcon;'
$newSpriteLine = '            Sprite sprite = GetStatusFaceIconSprite(unit);'
$text = ReplaceOptional $text $oldSpriteLine $newSpriteLine 'use CharacterData asset FaceIcon getter'

$helper = @'
        private static Sprite GetStatusFaceIconSprite(BattleUnit unit)
        {
            CharacterData data = unit == null ? null : unit.Data;
            return data == null ? null : data.FaceIcon;
        }

'@
$text = InsertBeforeIfMissing $text 'private static Sprite GetStatusFaceIconSprite(BattleUnit unit)' '        private static void SetStatusFaceIcon(Transform slot, BattleUnit unit)' $helper 'GetStatusFaceIconSprite helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched status FaceIcon to read from CharacterData asset FaceIcon.'
