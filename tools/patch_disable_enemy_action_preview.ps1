$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-MethodByName {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Replacement,
        [string]$Label
    )

    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        if ($Source.Contains($Replacement.Trim())) {
            Write-Host "Already patched: $Label"
            return $Source
        }

        throw "Patch anchor not found: $Label"
    }

    $braceStart = $Source.IndexOf("{", $start)
    if ($braceStart -lt 0) {
        throw "Method body start not found: $Label"
    }

    $depth = 0
    $end = -1

    for ($i = $braceStart; $i -lt $Source.Length; $i++) {
        $char = $Source[$i]

        if ($char -eq '{') {
            $depth++
        }
        elseif ($char -eq '}') {
            $depth--
            if ($depth -eq 0) {
                $end = $i + 1
                break
            }
        }
    }

    if ($end -lt 0) {
        throw "Method body end not found: $Label"
    }

    while ($end -lt $Source.Length -and ($Source[$end] -eq "`r" -or $Source[$end] -eq "`n")) {
        $end++
    }

    return $Source.Substring(0, $start) + $Replacement + $Source.Substring($end)
}

function Remove-MethodByName {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Label
    )

    return Replace-MethodByName -Source $Source -Signature $Signature -Replacement "" -Label $Label
}

# Disable UI creation/update/visibility for the deprecated enemy action preview.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void EnsureEnemyActionPreviewPanel()" `
    -Replacement @'
        private void EnsureEnemyActionPreviewPanel()
        {
            // Enemy action preview is intentionally disabled.
        }
'@ `
    -Label "EnsureEnemyActionPreviewPanel"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void UpdateEnemyActionPreview()" `
    -Replacement @'
        private void UpdateEnemyActionPreview()
        {
            // Enemy action preview is intentionally disabled.
        }
'@ `
    -Label "UpdateEnemyActionPreview"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void SetEnemyActionPreviewVisible(bool visible)" `
    -Replacement @'
        private void SetEnemyActionPreviewVisible(bool visible)
        {
            if (_enemyActionPreviewPanelObject != null)
            {
                _enemyActionPreviewPanelObject.SetActive(false);
            }
        }
'@ `
    -Label "SetEnemyActionPreviewVisible"

# Remove now-unused preview text helpers from BattleUIManager.
$text = Remove-MethodByName -Source $text -Signature "        private string BuildEnemyActionPreviewLine(BattleUnit enemy, EnemyActionState action, bool isNext)" -Label "BuildEnemyActionPreviewLine"
$text = Remove-MethodByName -Source $text -Signature "        private string BuildEnemyActionTargetText(BattleUnit enemy, EnemyActionState action)" -Label "BuildEnemyActionTargetText"
$text = Remove-MethodByName -Source $text -Signature "        private static string FormatEnemyPreviewGridPos(GridPos pos)" -Label "FormatEnemyPreviewGridPos"

# Normalize blank lines after broad removals.
$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")
$text = [regex]::Replace($text, "(`r?`n){4,}", "`r`n`r`n")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Disabled enemy action preview in BattleUIManager."
