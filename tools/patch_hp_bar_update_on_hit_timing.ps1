$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

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

$text = InsertBeforeIfMissing $text 'private bool _deferHpBarFillUntilActionHit' '        private readonly List<TMP_Text> _activeActionValuePopupLabels = new();' @'
        private bool _deferHpBarFillUntilActionHit;
        private readonly Dictionary<Transform, float> _pendingHpBarFillRates = new();

'@ 'pending HP bar fill fields'

$text = ReplaceOptional $text @'
            _phase = BattlePhase.ResolvingAction;
            ClearTargetPreview();
'@ @'
            _phase = BattlePhase.ResolvingAction;
            BeginDeferredHpBarFill();
            ClearTargetPreview();
'@ 'begin HP bar deferral on resolving action'

$text = ReplaceOptional $text @'
            List<GridPos> targetPositions = new(_pendingActionFlashTargets);
            List<GridPos> sourcePositions = new(_pendingActionSourceFlashTargets);

            yield return PlayActionSourceLunge(isSourceAllyBoard, sourcePositions);

            ClearPendingActionFlashTargets();
'@ @'
            List<GridPos> targetPositions = new(_pendingActionFlashTargets);
            List<GridPos> sourcePositions = new(_pendingActionSourceFlashTargets);

            yield return PlayActionSourceLunge(isSourceAllyBoard, sourcePositions);
            ApplyDeferredHpBarFillUpdates();

            ClearPendingActionFlashTargets();
'@ 'apply HP bar fills after source lunge'

$text = ReplaceOptional $text @'
            if (targetPositions.Count == 0 && sourcePositions.Count == 0)
            {
                yield return new WaitForSeconds(actionResolveDelaySeconds);
                yield break;
            }
'@ @'
            if (targetPositions.Count == 0 && sourcePositions.Count == 0)
            {
                ApplyDeferredHpBarFillUpdates();
                yield return new WaitForSeconds(actionResolveDelaySeconds);
                yield break;
            }
'@ 'apply HP bar fills when no flash targets'

$text = ReplaceOptional $text @'
            yield return PlayPendingDefeatFadeOuts();

            HideActiveActionValuePopups();
'@ @'
            yield return PlayPendingDefeatFadeOuts();
            ApplyDeferredHpBarFillUpdates();

            HideActiveActionValuePopups();
'@ 'flush HP bars after action animations'

$text = ReplaceOptional $text @'
        private void SetBarFill(Transform root, string barName, int current, int max)
        {
            Transform fill = root.Find($"{barName}/Fill");
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            HPBarFillAnimator animator = fill.GetComponent<HPBarFillAnimator>();
            if (animator == null)
            {
                animator = fill.gameObject.AddComponent<HPBarFillAnimator>();
            }

            animator.SetAnimationSeconds(hpBarAnimationSeconds);
            animator.SetFill(rate);
        }
'@ @'
        private void SetBarFill(Transform root, string barName, int current, int max)
        {
            Transform fill = root.Find($"{barName}/Fill");
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            if (Application.isPlaying && _deferHpBarFillUntilActionHit && string.Equals(barName, "HPBar", StringComparison.Ordinal))
            {
                _pendingHpBarFillRates[fill] = rate;
                return;
            }

            SetBarFillRate(fill, rate);
        }

        private void SetBarFillRate(Transform fill, float rate)
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

            animator.SetAnimationSeconds(hpBarAnimationSeconds);
            animator.SetFill(rate);
        }

        private void BeginDeferredHpBarFill()
        {
            _deferHpBarFillUntilActionHit = true;
            _pendingHpBarFillRates.Clear();
        }

        private void ApplyDeferredHpBarFillUpdates()
        {
            if (!_deferHpBarFillUntilActionHit && _pendingHpBarFillRates.Count == 0)
            {
                return;
            }

            _deferHpBarFillUntilActionHit = false;

            foreach (KeyValuePair<Transform, float> pair in _pendingHpBarFillRates)
            {
                SetBarFillRate(pair.Key, pair.Value);
            }

            _pendingHpBarFillRates.Clear();
        }
'@ 'defer SetBarFill HP updates until impact timing'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched HP bar fill updates to start on hit timing.'
