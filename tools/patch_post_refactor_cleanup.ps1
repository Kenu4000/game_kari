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

    return $Source.Remove($start, $end - $start)
}

function Replace-Optional {
    param(
        [string]$Source,
        [string]$Old,
        [string]$New,
        [string]$Label
    )

    if (!$Source.Contains($Old)) {
        Write-Host "Already replaced or not found: $Label"
        return $Source
    }

    return $Source.Replace($Old, $New)
}

# Remove tiny result text wrapper methods now that ResultTextBuilder owns the common text.
$text = Replace-Optional `
    -Source $text `
    -Old 'SetResultTitleAndBody("Quest Failed", BuildQuestFailedText());' `
    -New 'SetResultTitleAndBody("Quest Failed", BuildQuestEndSummaryText());' `
    -Label "Quest Failed text wrapper call"

$text = Replace-Optional `
    -Source $text `
    -Old 'SetResultTitleAndBody("Quest Clear", BuildQuestResultText());' `
    -New 'SetResultTitleAndBody("Quest Clear", BuildQuestEndSummaryText());' `
    -Label "Quest Clear text wrapper call"

$text = Remove-MethodByName -Source $text -Signature "        private string BuildQuestFailedText()" -Label "BuildQuestFailedText"
$text = Remove-MethodByName -Source $text -Signature "        private string BuildQuestResultText()" -Label "BuildQuestResultText"

# Keep at most two blank lines in a row inside the file. This removes large gaps left by previous patch-based extraction.
$text = [regex]::Replace($text, "(`r?`n)[ \t]*(`r?`n)[ \t]*(`r?`n)([ \t]*`r?`n)+", "`r`n`r`n")
$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Cleaned up post-refactor BattleUIManager wrappers and excess blank lines."
