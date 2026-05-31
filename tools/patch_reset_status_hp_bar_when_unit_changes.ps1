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

$field = @'
        private readonly Dictionary<Transform, BattleUnit> _statusSlotUnits = new();

'@
$text = InsertBeforeIfMissing $text '_statusSlotUnits' '        private readonly Dictionary<Transform, float> _pendingHpBarFillRates = new();' $field 'status slot unit tracking dictionary'

$text = ReplaceOptional $text '            _pendingEnemyKoReplacementPhase = false;' @'
            _pendingEnemyKoReplacementPhase = false;
            _statusSlotUnits.Clear();
'@ 'clear status slot unit tracking'

$oldEnemy = @'
            int currentHp = unit.CurrentHP;
            int maxHp = unit.Data.MaxHP;
            SetBarFill(slot, "HPBar", currentHp, maxHp);
'@
$newEnemy = @'
            bool unitChanged = UpdateStatusSlotUnit(slot, unit);
            int currentHp = unit.CurrentHP;
            int maxHp = unit.Data.MaxHP;
            SetBarFill(slot, "HPBar", currentHp, maxHp, unitChanged);
'@
$text = ReplaceOptional $text $oldEnemy $newEnemy 'enemy status HP immediate on unit change'

$oldAlly = @'
            int currentHp = unit.IsDead ? 0 : unit.CurrentHP;
            int maxHp = unit.Data.MaxHP;
            SetBarFill(slot, "HPBar", currentHp, maxHp);
'@
$newAlly = @'
            bool unitChanged = UpdateStatusSlotUnit(slot, unit);
            int currentHp = unit.IsDead ? 0 : unit.CurrentHP;
            int maxHp = unit.Data.MaxHP;
            SetBarFill(slot, "HPBar", currentHp, maxHp, unitChanged);
'@
$text = ReplaceOptional $text $oldAlly $newAlly 'ally status HP immediate on unit change'

$oldNullEnemy = @'
            slot.gameObject.SetActive(unit != null);

            if (unit == null)
            {
                return;
            }
'@
$newNullEnemy = @'
            slot.gameObject.SetActive(unit != null);

            if (unit == null)
            {
                ClearStatusSlotUnit(slot);
                return;
            }
'@
$text = ReplaceOptional $text $oldNullEnemy $newNullEnemy 'clear slot unit when status slot is empty'

$oldSetBar = @'
        private void SetBarFill(Transform root, string barName, int current, int max)
        {
            Transform fill = root.Find($"{barName}/Fill");
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            if (Application.isPlaying && _deferHpBarFillUntilActionHit && barName == "HPBar")
            {
                _pendingHpBarFillRates[fill] = rate;
                return;
            }

            SetBarFillRate(fill, rate);
        }
'@
$newSetBar = @'
        private void SetBarFill(Transform root, string barName, int current, int max, bool immediate = false)
        {
            Transform fill = root.Find($"{barName}/Fill");
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            if (immediate)
            {
                SetBarFillRateImmediate(fill, rate);
                return;
            }

            if (Application.isPlaying && _deferHpBarFillUntilActionHit && barName == "HPBar")
            {
                _pendingHpBarFillRates[fill] = rate;
                return;
            }

            SetBarFillRate(fill, rate);
        }
'@
$text = ReplaceOptional $text $oldSetBar $newSetBar 'SetBarFill immediate parameter'

$helpers = @'
        private bool UpdateStatusSlotUnit(Transform slot, BattleUnit unit)
        {
            if (slot == null)
            {
                return false;
            }

            if (!_statusSlotUnits.TryGetValue(slot, out BattleUnit previous) || previous != unit)
            {
                _statusSlotUnits[slot] = unit;
                return true;
            }

            return false;
        }

        private void ClearStatusSlotUnit(Transform slot)
        {
            if (slot != null)
            {
                _statusSlotUnits.Remove(slot);
            }
        }

'@
$text = InsertBeforeIfMissing $text 'private bool UpdateStatusSlotUnit(Transform slot, BattleUnit unit)' '        private void SetBarFill(Transform root, string barName, int current, int max' $helpers 'status slot unit tracking helpers'

$immediateHelper = @'
        private void SetBarFillRateImmediate(Transform fill, float rate)
        {
            if (fill == null)
            {
                return;
            }

            HPBarFillAnimator animator = fill.GetComponent<HPBarFillAnimator>();
            if (animator == null)
            {
                animator = fill.gameObject.AddComponent<HPBarFillAnimator>();
            }

            animator.SetFillImmediate(rate);
        }

'@
$text = InsertBeforeIfMissing $text 'private void SetBarFillRateImmediate(Transform fill, float rate)' '        private void SetBarFillRate(Transform fill, float rate)' $immediateHelper 'SetBarFillRateImmediate helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched status HP bars to reset immediately when a different unit enters a reused slot.'
