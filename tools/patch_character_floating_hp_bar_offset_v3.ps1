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

    return $src.Replace($old, $new)
}

$nl = [Environment]::NewLine

$characterInsert = @(
'        // 被弾・回復時にBattleSprite上部へ一時表示するHPバーの位置補正。',
'        // OFFの場合はBattleUIManager側の共通Floating HP Bar Offsetを使う。',
'        public bool OverrideFloatingHPBarOffset;',
'        public Vector2 FloatingHPBarOffset = new Vector2(0f, 52f);',
'',
''
) -join $nl

$characterText = Get-Content -Path $characterPath -Raw -Encoding UTF8
$characterText = InsertBeforeIfMissing $characterText 'public bool OverrideFloatingHPBarOffset' '        // 味方キャラがコマンドとして持つ技一覧。' $characterInsert 'CharacterData floating HP bar offset fields'
Set-Content -Path $characterPath -Value $characterText -Encoding UTF8

$text = Get-Content -Path $managerPath -Raw -Encoding UTF8

$oldCall = @(
'                FloatingHPBarView floatingBar = GetOrCreateFloatingHpBar(popup.IsAllyBoard, popup.Position);',
''
) -join $nl
$newCall = @(
'                FloatingHPBarView floatingBar = GetOrCreateFloatingHpBar(popup.IsAllyBoard, popup.Position, unit);',
''
) -join $nl
$text = ReplaceOptional $text $oldCall $newCall 'pass unit to GetOrCreateFloatingHpBar'

$text = ReplaceOptional $text '        private FloatingHPBarView GetOrCreateFloatingHpBar(bool isAllyBoard, GridPos position)' '        private FloatingHPBarView GetOrCreateFloatingHpBar(bool isAllyBoard, GridPos position, BattleUnit unit)' 'GetOrCreateFloatingHpBar signature'

$oldOffset = @(
'            rootRect.sizeDelta = floatingHpBarSize;',
'            rootRect.anchoredPosition = floatingHpBarOffset;',
''
) -join $nl
$newOffset = @(
'            rootRect.sizeDelta = floatingHpBarSize;',
'            rootRect.anchoredPosition = GetFloatingHpBarOffset(unit);',
''
) -join $nl
$text = ReplaceOptional $text $oldOffset $newOffset 'use character-specific floating HP offset'

$helper = @(
'        private Vector2 GetFloatingHpBarOffset(BattleUnit unit)',
'        {',
'            if (unit != null && unit.Data != null && unit.Data.OverrideFloatingHPBarOffset)',
'            {',
'                return unit.Data.FloatingHPBarOffset;',
'            }',
'',
'            return floatingHpBarOffset;',
'        }',
'',
''
) -join $nl
$text = InsertBeforeIfMissing $text 'private Vector2 GetFloatingHpBarOffset(BattleUnit unit)' '        private FloatingHPBarView GetOrCreateFloatingHpBar' $helper 'GetFloatingHpBarOffset helper'

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched character-specific floating HP bar offset support.'
