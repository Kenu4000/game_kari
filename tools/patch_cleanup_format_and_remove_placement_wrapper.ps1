$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Remove-MethodByName {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Label
    )

    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        Write-Host "Already removed or not found: $Label"
        return $Source
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

    return $Source.Substring(0, $start) + $Source.Substring($end)
}

# Remove the old placement wrapper after direct calls were moved to BattleUnitPlacementApplier.
$text = Remove-MethodByName `
    -Source $text `
    -Signature "        private void ApplyBattleUnitPlacements(" `
    -Label "ApplyBattleUnitPlacements wrapper"

# Fix method declarations that were glued to previous method closing braces.
$text = [regex]::Replace($text, "}\s{2,}(private|public|internal|protected)", "}`r`n`r`n        `$1")
$text = [regex]::Replace($text, "}\s{2,}//", "}`r`n`r`n        //")

# Normalize excessive blank lines.
$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")
$text = [regex]::Replace($text, "(`r?`n){4,}", "`r`n`r`n")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Cleaned BattleUIManager formatting and removed placement wrapper."
