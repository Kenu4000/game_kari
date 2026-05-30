$ErrorActionPreference = 'Stop'

$characterPath = 'Assets/Scripts/Battle/CharacterData.cs'
$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'

foreach ($path in @($characterPath, $managerPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }

    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$nl = [Environment]::NewLine

# CharacterData fields.
$characterInsert = @(
'        // Damage / heal popup position override used by battle board popups.',
'        public bool OverrideDamagePopupOffset;',
'        public Vector2 DamagePopupOffset = new Vector2(0f, 64f);',
'',
''
) -join $nl

$characterText = Get-Content -Path $characterPath -Raw -Encoding UTF8
$characterText = InsertBeforeIfMissing $characterText 'public bool OverrideDamagePopupOffset' '        public List<SkillData> DefaultSkills = new();' $characterInsert 'CharacterData damage popup offset fields'
Set-Content -Path $characterPath -Value $characterText -Encoding UTF8

$text = Get-Content -Path $managerPath -Raw -Encoding UTF8

# Replace call site if it has not been replaced yet.
$oldPopup = '            Vector2 anchoredPosition = GetActionPopupAnchoredPosition(isAllyBoard, position);'
$newPopup = @(
'            BattleUnit popupUnit = _grid.GetUnit(isAllyBoard, position);',
'            Vector2 anchoredPosition = GetActionPopupAnchoredPosition(isAllyBoard, position, popupUnit);'
) -join $nl
$text = ReplaceOptional $text $oldPopup $newPopup 'pass unit to action popup position'

# Replace method signature if it has not been replaced yet.
$oldSignature = '        private Vector2 GetActionPopupAnchoredPosition(bool isAllyBoard, GridPos position)'
$newSignature = '        private Vector2 GetActionPopupAnchoredPosition(bool isAllyBoard, GridPos position, BattleUnit unit)'
$text = ReplaceOptional $text $oldSignature $newSignature 'GetActionPopupAnchoredPosition signature'

# Replace shared offset return if it has not been replaced yet.
$oldReturn = '            return basePosition + actionValuePopupOffset;'
$newReturn = '            return basePosition + GetDamagePopupOffset(unit);'
$text = ReplaceOptional $text $oldReturn $newReturn 'use character-specific damage popup offset'

# Add helper. This v2 handles files where the signature was already replaced before this script is run.
$helper = @(
'        private Vector2 GetDamagePopupOffset(BattleUnit unit)',
'        {',
'            if (unit != null && unit.Data != null && unit.Data.OverrideDamagePopupOffset)',
'            {',
'                return unit.Data.DamagePopupOffset;',
'            }',
'',
'            return actionValuePopupOffset;',
'        }',
'',
''
) -join $nl

if (!$text.Contains('private Vector2 GetDamagePopupOffset(BattleUnit unit)')) {
    if ($text.Contains($newSignature)) {
        $text = $text.Replace($newSignature, $helper + $newSignature)
        Write-Host 'Inserted: GetDamagePopupOffset helper before new signature.'
    }
    elseif ($text.Contains($oldSignature)) {
        $text = $text.Replace($oldSignature, $helper + $oldSignature)
        Write-Host 'Inserted: GetDamagePopupOffset helper before old signature.'
    }
    else {
        throw 'Patch anchor not found: GetDamagePopupOffset helper. Search GetActionPopupAnchoredPosition in BattleUIManager.cs.'
    }
}
else {
    Write-Host 'Already exists: GetDamagePopupOffset helper'
}

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched character-specific damage popup offset support.'
