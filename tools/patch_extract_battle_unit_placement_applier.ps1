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

$text = Replace-Required `
    -Source $text `
    -Old "ApplyBattleUnitPlacements(true, setup.AllyPlacements, _allies);" `
    -New "BattleUnitPlacementApplier.Apply(_grid, true, setup.AllyPlacements, _allies);" `
    -Label "ally placements"

$text = Replace-Required `
    -Source $text `
    -Old "ApplyBattleUnitPlacements(false, setup.EnemyPlacements, _enemies);" `
    -New "BattleUnitPlacementApplier.Apply(_grid, false, setup.EnemyPlacements, _enemies);" `
    -Label "enemy setup placements"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ApplyBattleUnitPlacements(" `
    -Replacement @'
        private void ApplyBattleUnitPlacements(
            bool isAlly,
            List<BattleUnitPlacement> placements,
            List<BattleUnit> units)
        {
            BattleUnitPlacementApplier.Apply(_grid, isAlly, placements, units);
        }
'@ `
    -Label "ApplyBattleUnitPlacements"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ReplaceEnemyWave(WaveData wave)" `
    -Replacement @'
        private void ReplaceEnemyWave(WaveData wave)
        {
            BattleUnitPlacementApplier.ReplaceEnemyWave(_grid, wave, _enemies, _enemyReserves);
        }
'@ `
    -Label "ReplaceEnemyWave"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ClearEnemyBoardAndLists()" `
    -Replacement @'
        private void ClearEnemyBoardAndLists()
        {
            BattleUnitPlacementApplier.ClearEnemySide(_grid, _enemies, _enemyReserves);
        }
'@ `
    -Label "ClearEnemyBoardAndLists"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUIManager to use BattleUnitPlacementApplier."
