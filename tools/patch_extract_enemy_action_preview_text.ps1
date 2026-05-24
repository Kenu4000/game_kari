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

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void UpdateEnemyActionPreview()" `
    -Replacement @'
        private void UpdateEnemyActionPreview()
        {
            EnsureEnemyActionPreviewPanel();

            if (_enemyActionPreviewText == null)
            {
                return;
            }

            _enemyActionPreviewText.text = EnemyActionPreviewTextBuilder.BuildPreviewText(
                _enemies,
                _actedUnits,
                FindNextUnactedEnemy(),
                GetPreviewEnemyActionState);
        }
'@ `
    -Label "UpdateEnemyActionPreview"

$text = Remove-MethodByName -Source $text -Signature "        private string BuildEnemyActionPreviewLine(BattleUnit enemy, EnemyActionState action, bool isNext)" -Label "BuildEnemyActionPreviewLine"
$text = Remove-MethodByName -Source $text -Signature "        private string BuildEnemyActionTargetText(BattleUnit enemy, EnemyActionState action)" -Label "BuildEnemyActionTargetText"
$text = Remove-MethodByName -Source $text -Signature "        private static string FormatEnemyPreviewGridPos(GridPos pos)" -Label "FormatEnemyPreviewGridPos"

$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")
$text = [regex]::Replace($text, "(`r?`n){4,}", "`r`n`r`n")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Extracted enemy action preview text builder."
