$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Remove-RequiredText {
    param(
        [string]$Source,
        [string]$Old,
        [string]$Label
    )

    if (!$Source.Contains($Old)) {
        Write-Host "Already removed or not found: $Label"
        return $Source
    }

    return $Source.Replace($Old, "")
}

$text = Remove-RequiredText `
    -Source $text `
    -Old @'
        private sealed class BattleClearResult
        {
            public BattleClearRank Rank;

            public int PartyHealAmount;
            public int BattleNumber;
            public int TotalBattles;
            public bool HasNextWave;
        }

'@ `
    -Label "nested BattleClearResult"

$text = Remove-RequiredText `
    -Source $text `
    -Old @'
        private enum BattleClearRank
        {
            OneTurn,
            TwoTurn,
            ThreeTurn,
            FourPlusTurn
        }

'@ `
    -Label "nested BattleClearRank"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Removed nested BattleClearResult and BattleClearRank from BattleUIManager.cs."
