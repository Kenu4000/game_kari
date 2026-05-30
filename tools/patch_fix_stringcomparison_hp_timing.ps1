$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = 'Application.isPlaying && _deferHpBarFillUntilActionHit && string.Equals(barName, "HPBar", StringComparison.Ordinal)'
$new = 'Application.isPlaying && _deferHpBarFillUntilActionHit && barName == "HPBar"'

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
}
elseif ($text.Contains($new)) {
    Write-Host 'StringComparison usage is already removed.'
}
else {
    throw 'Patch anchor not found: StringComparison usage in SetBarFill'
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Fixed StringComparison usage in HP bar timing patch.'
