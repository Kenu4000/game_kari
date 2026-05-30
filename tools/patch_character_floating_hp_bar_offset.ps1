$ErrorActionPreference = 'Stop'

$characterPath = 'Assets/Scripts/Battle/CharacterData.cs'
$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'

foreach ($path in @($characterPath, $managerPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) { Write-Host "Already exists: $label"; return $src }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) { Write-Host "Already replaced or not found: $label"; return $src }
    return $src.Replace($old, $new)
}

# CharacterData: add per-character floating HP bar offset.
$characterText = Get-Content -Path $characterPath -Raw -Encoding UTF8
$characterText = InsertBeforeIfMissing $characterText 'public bool OverrideFloatingHPBarOffset' '        // 味方キャラがコマンドとして持つ技一覧。' @'
        // 被弾・回復時にBattleSprite上部へ一時表示するHPバーの位置補正。
        // OFFの場合はBattleUIManager側の共通Floating HP Bar Offsetを使う。
        public bool OverrideFloatingHPBarOffset;
        public Vector2 FloatingHPBarOffset = new Vector2(0f, 52f);

'@ 'CharacterData floating HP bar offset fields'
Set-Content -Path $characterPath -Value $characterText -Encoding UTF8

# BattleUIManager: pass unit-specific offset when creating the floating HP bar.
$text = Get-Content -Path $managerPath -Raw -Encoding UTF8

$text = ReplaceOptional $text @'
                FloatingHPBarView floatingBar = GetOrCreateFloatingHpBar(popup.IsAllyBoard, popup.Position);
'@ @'
                FloatingHPBarView floatingBar = GetOrCreateFloatingHpBar(popup.IsAllyBoard, popup.Position, unit);
'@ 'pass unit to GetOrCreateFloatingHpBar'

$text = ReplaceOptional $text @'
        private FloatingHPBarView GetOrCreateFloatingHpBar(bool isAllyBoard, GridPos position)
'@ @'
        private FloatingHPBarView GetOrCreateFloatingHpBar(bool isAllyBoard, GridPos position, BattleUnit unit)
'@ 'GetOrCreateFloatingHpBar signature'

$text = ReplaceOptional $text @'
            rootRect.sizeDelta = floatingHpBarSize;
            rootRect.anchoredPosition = floatingHpBarOffset;
'@ @'
            rootRect.sizeDelta = floatingHpBarSize;
            rootRect.anchoredPosition = GetFloatingHpBarOffset(unit);
'@ 'use character-specific floating HP offset'

$helper = @'
        private Vector2 GetFloatingHpBarOffset(BattleUnit unit)
        {
            if (unit != null && unit.Data != null && unit.Data.OverrideFloatingHPBarOffset)
            {
                return unit.Data.FloatingHPBarOffset;
            }

            return floatingHpBarOffset;
        }

'@

$text = InsertBeforeIfMissing $text 'private Vector2 GetFloatingHpBarOffset(BattleUnit unit)' '        private FloatingHPBarView GetOrCreateFloatingHpBar' $helper 'GetFloatingHpBarOffset helper'

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched character-specific floating HP bar offset support.'
