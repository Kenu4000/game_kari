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

function Replace-Required {
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

$text = Replace-Required -Source $text -Old "EvaluateBattleClearRank()" -New "EvaluateBattleClearRank()" -Label "noop"

$text = Replace-Required `
    -Source $text `
    -Old @'
        private BattleClearRank EvaluateBattleClearRank()
        {
            EnsureWaveProgress();

            int battleTurn = _waveProgress.WaveTurn;

            if (battleTurn <= 1)
            {
                return BattleClearRank.OneTurn;
            }

            if (battleTurn == 2)
            {
                return BattleClearRank.TwoTurn;
            }

            if (battleTurn == 3)
            {
                return BattleClearRank.ThreeTurn;
            }

            return BattleClearRank.FourPlusTurn;
        }
'@ `
    -New @'
        private BattleClearRank EvaluateBattleClearRank()
        {
            EnsureWaveProgress();
            return BattleClearRewardCalculator.EvaluateRank(_waveProgress.WaveTurn);
        }
'@ `
    -Label "EvaluateBattleClearRank body"

$text = Replace-Required -Source $text -Old "CalculateExpGain(result)" -New "BattleClearRewardCalculator.CalculateExpGain(result)" -Label "CalculateExpGain calls"
$text = Replace-Required -Source $text -Old "CalculateKakeraGain(result.Rank)" -New "BattleClearRewardCalculator.CalculateKakeraGain(result.Rank)" -Label "CalculateKakeraGain result call"
$text = Replace-Required -Source $text -Old "CalculateKakeraGain(result.Rank);" -New "BattleClearRewardCalculator.CalculateKakeraGain(result.Rank);" -Label "CalculateKakeraGain semicolon call"
$text = Replace-Required -Source $text -Old "FormatBattleClearRank(result.Rank)" -New "BattleClearRewardCalculator.FormatRank(result.Rank)" -Label "FormatBattleClearRank call"

$text = Remove-MethodByName -Source $text -Signature "        private static string FormatBattleClearRank(BattleClearRank rank)" -Label "FormatBattleClearRank"
$text = Remove-MethodByName -Source $text -Signature "        private static int CalculateExpGain(BattleClearResult result)" -Label "CalculateExpGain"
$text = Remove-MethodByName -Source $text -Signature "        private static int CalculateKakeraGain(BattleClearRank rank)" -Label "CalculateKakeraGain"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Extracted battle clear reward calculations to BattleClearRewardCalculator."
