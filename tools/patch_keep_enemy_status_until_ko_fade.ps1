$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }

    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }

    Write-Host "Inserted: $label"
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

# Keep KO enemies in status UI until the KO fade coroutine explicitly releases them.
$field = @'
        private readonly List<BattleUnit> _enemyStatusKoVisibleUnits = new();

'@
$text = InsertBeforeIfMissing $text '_enemyStatusKoVisibleUnits' '        private readonly List<ActionValuePopup> _pendingActionValuePopups = new();' $field 'enemy status KO visible list'

# Clear runtime list on battle reset/setup.
$text = ReplaceOptional $text '            _actedUnits.Clear();' @'
            _actedUnits.Clear();
            _enemyStatusKoVisibleUnits.Clear();
'@ 'clear KO-visible status list'

# Add KO unit to the status-hold list when KO is resolved.
$text = ReplaceOptional $text @'
                defeated.Unit.IsDead = true;
                RemoveTurnState(defeated.Unit);
'@ @'
                defeated.Unit.IsDead = true;
                if (!_enemyStatusKoVisibleUnits.Contains(defeated.Unit))
                {
                    _enemyStatusKoVisibleUnits.Add(defeated.Unit);
                }
                RemoveTurnState(defeated.Unit);
'@ 'register KO enemy as status-visible until fade'

# Redraw status panel from status-display list, not alive-only list.
$oldRedraw = @'
        private void RedrawStatusPanels()
        {
            List<BattleUnit> aliveEnemies = GetAliveEnemies();

            for (int i = 0; i < 4; i++)
            {
                RedrawEnemyStatusSlot(i + 1, GetUnitAt(aliveEnemies, i));
                RedrawAllyStatusSlot(i + 1, GetUnitAt(_allies, i));
            }

            ResizeEnemyStatusPanel(aliveEnemies.Count);
            LayoutEnemyStatusSlots(aliveEnemies.Count);
        }
'@
$newRedraw = @'
        private void RedrawStatusPanels()
        {
            List<BattleUnit> enemyStatusUnits = GetEnemyStatusDisplayUnits();

            for (int i = 0; i < 4; i++)
            {
                RedrawEnemyStatusSlot(i + 1, GetUnitAt(enemyStatusUnits, i));
                RedrawAllyStatusSlot(i + 1, GetUnitAt(_allies, i));
            }

            ResizeEnemyStatusPanel(enemyStatusUnits.Count);
            LayoutEnemyStatusSlots(enemyStatusUnits.Count);
        }
'@
$text = ReplaceOptional $text $oldRedraw $newRedraw 'use status-display enemy list'

$displayHelper = @'
        private List<BattleUnit> GetEnemyStatusDisplayUnits()
        {
            var result = new List<BattleUnit>();

            for (int i = 0; i < _enemies.Count; i++)
            {
                BattleUnit unit = _enemies[i];
                if (unit == null)
                {
                    continue;
                }

                if (!unit.IsDead || _enemyStatusKoVisibleUnits.Contains(unit))
                {
                    result.Add(unit);
                }
            }

            return result;
        }

'@
$text = InsertBeforeIfMissing $text 'private List<BattleUnit> GetEnemyStatusDisplayUnits()' '        private void RedrawTurnOrderBar()' $displayHelper 'GetEnemyStatusDisplayUnits helper'

# Track KO units captured by the fade coroutine, then release their status visibility after fade completes.
$text = ReplaceOptional $text '            var positions = new List<GridPos>();' @'
            var positions = new List<GridPos>();
            var koStatusUnits = new List<BattleUnit>();
'@ 'add KO status unit list to fade coroutine'

$text = ReplaceOptional $text @'
                positions.Add(popup.Position);
'@ @'
                positions.Add(popup.Position);
                if (!koStatusUnits.Contains(unit))
                {
                    koStatusUnits.Add(unit);
                }
'@ 'capture KO status unit'

$text = ReplaceOptional $text @'
            for (int i = 0; i < positions.Count; i++)
            {
                _grid.SetUnit(false, positions[i], null);
            }
'@ @'
            for (int i = 0; i < positions.Count; i++)
            {
                _grid.SetUnit(false, positions[i], null);
            }

            for (int i = 0; i < koStatusUnits.Count; i++)
            {
                _enemyStatusKoVisibleUnits.Remove(koStatusUnits[i]);
            }
'@ 'release KO status unit after fade'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy status to remain visible until KO fade completes.'
