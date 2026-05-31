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

# Target sprites should remain normal. Do not apply alpha/overlap emphasis during skill hover.
$text = ReplaceOptional $text '            ApplySkillHoverOverlapAlpha(targetIsAllyBoard, targetPositions);' '            // Target sprites stay at normal color. No extra overlap alpha/emphasis is applied.' 'remove target overlap alpha call'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Removed skill-hover target overlap alpha emphasis.'
