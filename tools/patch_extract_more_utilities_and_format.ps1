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

# Replace wrappers with utility delegation.
$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private static int HealLivingPartyMembers(List<BattleUnit> units, int healAmount)" `
    -Replacement @'
        private static int HealLivingPartyMembers(List<BattleUnit> units, int healAmount)
        {
            return BattlePartyStateUtility.HealLivingMembers(units, healAmount);
        }
'@ `
    -Label "HealLivingPartyMembers"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private static int CountLivingPartyMembers(List<BattleUnit> units)" `
    -Replacement @'
        private static int CountLivingPartyMembers(List<BattleUnit> units)
        {
            return BattlePartyStateUtility.CountLivingMembers(units);
        }
'@ `
    -Label "CountLivingPartyMembers"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private static int CountKnownPartyMembers(List<BattleUnit> units)" `
    -Replacement @'
        private static int CountKnownPartyMembers(List<BattleUnit> units)
        {
            return BattlePartyStateUtility.CountKnownMembers(units);
        }
'@ `
    -Label "CountKnownPartyMembers"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private int CountTotalBattleRoutePoints()" `
    -Replacement @'
        private int CountTotalBattleRoutePoints()
        {
            return QuestBattleCountUtility.CountTotalBattleRoutePoints(_questProgress, GetTotalBattleCount());
        }
'@ `
    -Label "CountTotalBattleRoutePoints"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private int CountClearedBattleRoutePoints()" `
    -Replacement @'
        private int CountClearedBattleRoutePoints()
        {
            return QuestBattleCountUtility.CountClearedBattleRoutePoints(_questProgress, GetCurrentBattleNumber());
        }
'@ `
    -Label "CountClearedBattleRoutePoints"

# Formatting cleanup: fix methods accidentally glued onto the same line by earlier scripts.
$text = $text.Replace("}        // Enemy action selection", "}`r`n`r`n        // Enemy action selection")
$text = $text.Replace("}        private void ClearEnemyBoardAndLists()", "}`r`n`r`n        private void ClearEnemyBoardAndLists()")
$text = $text.Replace("}        private void ReturnToBase()", "}`r`n`r`n        private void ReturnToBase()")
$text = $text.Replace("}        private void SetResultButtons(", "}`r`n`r`n        private void SetResultButtons(")
$text = $text.Replace("}        private void ShowQuestFailedPanel()", "}`r`n`r`n        private void ShowQuestFailedPanel()")
$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")
$text = [regex]::Replace($text, "(`r?`n){4,}", "`r`n`r`n")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Extracted additional utility logic and cleaned formatting."
