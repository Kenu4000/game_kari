$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $managerPath)) { throw "Required file not found: $managerPath" }

$text = Get-Content -Path $managerPath -Raw -Encoding UTF8

$old = '            return actionValuePopupOffset;'
$new = '            return actionPopupOffset;'

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
    Set-Content -Path $managerPath -Value $text -Encoding UTF8
    Write-Host 'Replaced missing actionValuePopupOffset with actionPopupOffset.'
    exit 0
}

if ($text.Contains($new)) {
    Write-Host 'Fallback already uses actionPopupOffset.'
    exit 0
}

throw 'Could not find fallback return. Open BattleUIManager.cs and search GetDamagePopupOffset.'
