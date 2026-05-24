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

    return $Source.Substring(0, $start) + $Replacement + $Source.Substring($end)
}

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private string BuildBattleResultSubText(BattleClearResult result)" `
    -Replacement @'
        private string BuildBattleResultSubText(BattleClearResult result)
        {
            int kakeraGain = result == null ? 0 : CalculateKakeraGain(result.Rank);
            int expGain = result == null ? 0 : CalculateExpGain(result);

            return ResultTextBuilder.BuildBattleResultText(
                result,
                _kakeraStock,
                MaxKakeraStock,
                kakeraGain,
                expGain);
        }
'@ `
    -Label "BuildBattleResultSubText"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private string BuildQuestEndSummaryText()" `
    -Replacement @'
        private string BuildQuestEndSummaryText()
        {
            return ResultTextBuilder.BuildQuestEndSummaryText(
                CountClearedBattleRoutePoints(),
                CountTotalBattleRoutePoints(),
                _totalKakeraEarned,
                _totalExpEarned);
        }
'@ `
    -Label "BuildQuestEndSummaryText"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUIManager to delegate result text to ResultTextBuilder."
