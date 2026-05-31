$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $managerPath)) { throw "Required file not found: $managerPath" }

$text = Get-Content -Path $managerPath -Raw -Encoding UTF8

$replaced = $false

if ($text.Contains('            return actionPopupOffset;')) {
    $text = $text.Replace('            return actionPopupOffset;', '            return Vector2.zero;')
    $replaced = $true
}

if ($text.Contains('            return actionValuePopupOffset;')) {
    $text = $text.Replace('            return actionValuePopupOffset;', '            return Vector2.zero;')
    $replaced = $true
}

if ($text.Contains('            return Vector2.zero;')) {
    Set-Content -Path $managerPath -Value $text -Encoding UTF8
    if ($replaced) {
        Write-Host 'Replaced missing damage popup fallback field with Vector2.zero.'
    }
    else {
        Write-Host 'Damage popup fallback already uses Vector2.zero.'
    }
    exit 0
}

throw 'Could not find GetDamagePopupOffset fallback return. Open BattleUIManager.cs and search GetDamagePopupOffset.'
