$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

$before = $text

# Fix accidental double qualification produced by the previous broad string replacement.
$text = $text.Replace(
    "BattleClearRewardCalculator.BattleClearRewardCalculator.",
    "BattleClearRewardCalculator."
)

if ($text -eq $before) {
    Write-Host "No double BattleClearRewardCalculator qualifier found. Nothing changed."
}
else {
    Set-Content -Path $path -Value $text -Encoding UTF8
    Write-Host "Fixed double BattleClearRewardCalculator qualifier."
}
