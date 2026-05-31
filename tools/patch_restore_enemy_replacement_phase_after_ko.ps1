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

$fields = @'
        private bool _pendingEnemyKoReplacementPhase;
        private bool _pendingEnemyAutoReplacementEnterAnimation;

'@
$text = InsertBeforeIfMissing $text '_pendingEnemyKoReplacementPhase' '        private readonly List<ActionValuePopup> _pendingActionValuePopups = new();' $fields 'enemy replacement phase flags'

# Convert reserve fill functions to report whether anything entered.
$text = ReplaceOptional $text '        private void FillEmptyEnemyCellsFromReserves()' '        private bool FillEmptyEnemyCellsFromReserves()' 'FillEmptyEnemyCellsFromReserves bool return'

$oldFill = @'
        private bool FillEmptyEnemyCellsFromReserves()
        {
            TryFillEnemyCellFromReserve(GridPos.FrontTop);
            TryFillEnemyCellFromReserve(GridPos.FrontBottom);
            TryFillEnemyCellFromReserve(GridPos.BackTop);
            TryFillEnemyCellFromReserve(GridPos.BackBottom);
        }
'@
$newFill = @'
        private bool FillEmptyEnemyCellsFromReserves()
        {
            bool changed = false;
            changed |= TryFillEnemyCellFromReserve(GridPos.FrontTop);
            changed |= TryFillEnemyCellFromReserve(GridPos.FrontBottom);
            changed |= TryFillEnemyCellFromReserve(GridPos.BackTop);
            changed |= TryFillEnemyCellFromReserve(GridPos.BackBottom);
            return changed;
        }
'@
$text = ReplaceOptional $text $oldFill $newFill 'FillEmptyEnemyCellsFromReserves changed flag body'

$text = ReplaceOptional $text '        private void TryFillEnemyCellFromReserve(GridPos position)' '        private bool TryFillEnemyCellFromReserve(GridPos position)' 'TryFillEnemyCellFromReserve bool return'

$text = ReplaceOptional $text @'
            if (current != null && !current.IsDead)
            {
                return;
            }
'@ @'
            if (current != null && !current.IsDead)
            {
                return false;
            }
'@ 'TryFill current alive return false'

$text = ReplaceOptional $text @'
            if (replacement == null)
            {
                return;
            }
'@ @'
            if (replacement == null)
            {
                return false;
            }
'@ 'TryFill no reserve return false'

$text = ReplaceOptional $text @'
            Debug.Log($"[KO] {replacement.Name} entered enemy grid at {position}. Replacement cannot act this turn.");
        }
'@ @'
            Debug.Log($"[KO] {replacement.Name} entered enemy grid at {position}. Replacement cannot act this turn.");
            return true;
        }
'@ 'TryFill replacement return true'

# At KO decision time, do not immediately compact/fill. This must wait until after action effects.
$text = ReplaceOptional $text @'
            CompactEnemyFrontlineIfEmpty();
            FillEmptyEnemyCellsFromReserves();

            CheckBattleEnd();
'@ @'
            // Enemy formation movement and reserve entry are deferred until after the action effect.
            CheckBattleEnd();
'@ 'remove immediate replacement from ResolveDefeatedEnemies'

# After KO fade, only remove KO units from board and mark replacement phase pending. No compact/fill/redraw here.
$text = ReplaceOptional $text @'
            for (int i = 0; i < positions.Count; i++)
            {
                _grid.SetUnit(false, positions[i], null);
            }

            CompactEnemyFrontlineIfEmpty();
            FillEmptyEnemyCellsFromReserves();
            RedrawBoard();
'@ @'
            for (int i = 0; i < positions.Count; i++)
            {
                _grid.SetUnit(false, positions[i], null);
            }

            _pendingEnemyKoReplacementPhase = true;
'@ 'defer compact/fill/redraw after KO fade'

# Ensure action effect end calls the replacement phase after popups are cleared.
$text = ReplaceOptional $text @'
                HideActiveActionValuePopups();
                ClearPendingActionValuePopups();
                yield break;
'@ @'
                HideActiveActionValuePopups();
                ClearPendingActionValuePopups();
                yield return PlayPendingAutoReplacementAnimations();
                yield break;
'@ 'call replacement phase in no-flash branch'

$text = ReplaceOptional $text @'
            HideActiveActionValuePopups();
            ClearPendingActionValuePopups();
        }
'@ @'
            HideActiveActionValuePopups();
            ClearPendingActionValuePopups();
            yield return PlayPendingAutoReplacementAnimations();
        }
'@ 'call replacement phase at action effect end'

$pendingHelper = @'
        private IEnumerator PlayPendingAutoReplacementAnimations()
        {
            if (_pendingEnemyKoReplacementPhase)
            {
                _pendingEnemyKoReplacementPhase = false;

                CompactEnemyFrontlineIfEmpty();
                bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
                RedrawBoard();
                ResetEnemyStatusCanvasGroupAlphas();

                if (replacementOccurred)
                {
                    _pendingEnemyAutoReplacementEnterAnimation = true;
                }
            }

            if (_pendingEnemyAutoReplacementEnterAnimation)
            {
                _pendingEnemyAutoReplacementEnterAnimation = false;
                yield return PlayAutoReplacementEnterAnimation(false);
            }
        }

'@
$text = InsertBeforeIfMissing $text 'private IEnumerator PlayPendingAutoReplacementAnimations()' '        private void SetActionSourceFlashTargetsVisible' $pendingHelper 'PlayPendingAutoReplacementAnimations helper'

$resetHelper = @'
        private void ResetEnemyStatusCanvasGroupAlphas()
        {
            if (enemyStatusPanel == null)
            {
                return;
            }

            for (int i = 1; i <= 4; i++)
            {
                Transform slot = enemyStatusPanel.Find($"EnemyStatus_{i}");
                if (slot == null)
                {
                    continue;
                }

                CanvasGroup group = slot.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = 1f;
                }
            }
        }

'@
$text = InsertBeforeIfMissing $text 'private void ResetEnemyStatusCanvasGroupAlphas()' '        private IEnumerator PlayPendingAutoReplacementAnimations()' $resetHelper 'ResetEnemyStatusCanvasGroupAlphas helper'

$enterHelpers = @'
        private IEnumerator PlayAutoReplacementEnterAnimation(bool isAllyBoard)
        {
            float duration = Mathf.Max(0f, autoReplacementEnterSeconds);
            float distance = Mathf.Max(0f, autoReplacementEnterDistance);
            if (duration <= 0f || distance <= 0f)
            {
                yield break;
            }

            var sprites = new List<RectTransform>();
            var endPositions = new List<Vector2>();
            AddBoardSpriteRectIfPresent(isAllyBoard, GridPos.FrontTop, sprites, endPositions);
            AddBoardSpriteRectIfPresent(isAllyBoard, GridPos.FrontBottom, sprites, endPositions);
            AddBoardSpriteRectIfPresent(isAllyBoard, GridPos.BackTop, sprites, endPositions);
            AddBoardSpriteRectIfPresent(isAllyBoard, GridPos.BackBottom, sprites, endPositions);

            if (sprites.Count == 0)
            {
                yield break;
            }

            Vector2 enterOffset = new Vector2(isAllyBoard ? -distance : distance, 0f);
            for (int i = 0; i < sprites.Count && i < endPositions.Count; i++)
            {
                if (sprites[i] != null)
                {
                    sprites[i].anchoredPosition = endPositions[i] + enterOffset;
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);

                for (int i = 0; i < sprites.Count && i < endPositions.Count; i++)
                {
                    RectTransform sprite = sprites[i];
                    if (sprite == null)
                    {
                        continue;
                    }

                    sprite.anchoredPosition = Vector2.Lerp(endPositions[i] + enterOffset, endPositions[i], eased);
                }

                yield return null;
            }

            for (int i = 0; i < sprites.Count && i < endPositions.Count; i++)
            {
                if (sprites[i] != null)
                {
                    sprites[i].anchoredPosition = endPositions[i];
                }
            }
        }

        private void AddBoardSpriteRectIfPresent(bool isAllyBoard, GridPos position, List<RectTransform> sprites, List<Vector2> endPositions)
        {
            if (sprites == null || endPositions == null)
            {
                return;
            }

            RectTransform rect = GetBoardSpriteRect(isAllyBoard, position);
            if (rect == null || sprites.Contains(rect))
            {
                return;
            }

            sprites.Add(rect);
            endPositions.Add(rect.anchoredPosition);
        }

'@
$text = InsertBeforeIfMissing $text 'private IEnumerator PlayAutoReplacementEnterAnimation(bool isAllyBoard)' '        private void SetActionSourceFlashTargetsVisible' $enterHelpers 'auto replacement enter helpers'

$fieldSettings = @'
        [SerializeField] private float autoReplacementEnterSeconds = 0.18f;
        [SerializeField] private float autoReplacementEnterDistance = 32f;

'@
$text = InsertBeforeIfMissing $text 'autoReplacementEnterSeconds' '        [SerializeField] private float defeatFadeSeconds' $fieldSettings 'auto replacement inspector settings'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy compact/reserve replacement to run after action effects.'
