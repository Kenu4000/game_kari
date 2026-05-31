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

# Inspector settings for the auto replacement entrance animation.
$fieldInsert = @'
        [SerializeField] private float autoReplacementEnterSeconds = 0.18f;
        [SerializeField] private float autoReplacementEnterDistance = 32f;

'@
$text = InsertBeforeIfMissing $text 'private float autoReplacementEnterSeconds' '        [SerializeField] private float defeatFadeSeconds' $fieldInsert 'auto replacement animation settings'

# Do not refill enemies at KO decision time. This keeps KO animation and replacement separated.
$oldResolve = @'
            CompactEnemyFrontlineIfEmpty();
            FillEmptyEnemyCellsFromReserves();

            CheckBattleEnd();
'@
$newResolve = @'
            // Enemy grid movement and reserve entry are deferred until the KO fadeout finishes.
            CheckBattleEnd();
'@
$text = ReplaceOptional $text $oldResolve $newResolve 'defer enemy replacement from ResolveDefeatedEnemies'

# Let FillEmptyEnemyCellsFromReserves report whether replacement occurred.
$text = ReplaceOptional $text '        private void FillEmptyEnemyCellsFromReserves()' '        private bool FillEmptyEnemyCellsFromReserves()' 'FillEmptyEnemyCellsFromReserves return type'

$oldFillBody = @'
        private bool FillEmptyEnemyCellsFromReserves()
        {
            TryFillEnemyCellFromReserve(GridPos.FrontTop);
            TryFillEnemyCellFromReserve(GridPos.FrontBottom);
            TryFillEnemyCellFromReserve(GridPos.BackTop);
            TryFillEnemyCellFromReserve(GridPos.BackBottom);
        }
'@
$newFillBody = @'
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
$text = ReplaceOptional $text $oldFillBody $newFillBody 'FillEmptyEnemyCellsFromReserves changed flag body'

$text = ReplaceOptional $text '        private void TryFillEnemyCellFromReserve(GridPos position)' '        private bool TryFillEnemyCellFromReserve(GridPos position)' 'TryFillEnemyCellFromReserve return type'
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

# After KO fadeout, refill and play a replacement entrance animation only if replacement occurred.
$oldAfterFade = @'
            for (int i = 0; i < positions.Count; i++)
            {
                _grid.SetUnit(false, positions[i], null);
            }

            CompactEnemyFrontlineIfEmpty();
            FillEmptyEnemyCellsFromReserves();
            RedrawBoard();
        }
'@
$newAfterFade = @'
            for (int i = 0; i < positions.Count; i++)
            {
                _grid.SetUnit(false, positions[i], null);
            }

            CompactEnemyFrontlineIfEmpty();
            bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
            RedrawBoard();

            if (replacementOccurred)
            {
                yield return PlayAutoReplacementEnterAnimation(false);
            }
        }
'@
$text = ReplaceOptional $text $oldAfterFade $newAfterFade 'play replacement animation after enemy KO fadeout'

$helper = @'
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
$text = InsertBeforeIfMissing $text 'private IEnumerator PlayAutoReplacementEnterAnimation(bool isAllyBoard)' '        private void SetActionSourceFlashTargetsVisible' $helper 'auto replacement enter animation helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy auto replacement to occur after KO fadeout with a separate entrance animation.'
