$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $managerPath)) { throw "Required file not found: $managerPath" }

$text = Get-Content -Path $managerPath -Raw -Encoding UTF8

if ($text.Contains('private Vector2 GetDamagePopupOffset(BattleUnit unit)')) {
    Write-Host 'Already exists: GetDamagePopupOffset helper'
    exit 0
}

$helper = @'
        private Vector2 GetDamagePopupOffset(BattleUnit unit)
        {
            if (unit != null && unit.Data != null && unit.Data.OverrideDamagePopupOffset)
            {
                return unit.Data.DamagePopupOffset;
            }

            return actionValuePopupOffset;
        }

'@

# Insert before SafeName near the end of BattleUIManager.
$anchor = '        private static string SafeName(BattleUnit unit)'
$index = $text.IndexOf($anchor)
if ($index -lt 0) {
    throw 'Patch anchor not found: SafeName. Open BattleUIManager.cs and paste GetDamagePopupOffset before the final utility methods.'
}

$text = $text.Substring(0, $index) + $helper + $text.Substring($index)
Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Inserted GetDamagePopupOffset helper near the end of BattleUIManager.'
