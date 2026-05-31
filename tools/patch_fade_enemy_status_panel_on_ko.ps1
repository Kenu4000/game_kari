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

# Add status slot fade collections to the KO fade coroutine.
$oldLists = @'
            var sprites = new List<Image>();
            var rects = new List<RectTransform>();
            var startColors = new List<Color>();
            var startPositions = new List<Vector2>();
            var positions = new List<GridPos>();
'@
$newLists = @'
            var sprites = new List<Image>();
            var rects = new List<RectTransform>();
            var startColors = new List<Color>();
            var startPositions = new List<Vector2>();
            var positions = new List<GridPos>();
            var statusCanvasGroups = new List<CanvasGroup>();
            var statusStartAlphas = new List<float>();
'@
$text = ReplaceOptional $text $oldLists $newLists 'add enemy status fade lists'

# Capture the enemy status slot matching the KO unit.
$oldCapture = @'
                sprites.Add(image);
                rects.Add(rect);
                startColors.Add(image.color);
                startPositions.Add(rect.anchoredPosition);
                positions.Add(popup.Position);
'@
$newCapture = @'
                sprites.Add(image);
                rects.Add(rect);
                startColors.Add(image.color);
                startPositions.Add(rect.anchoredPosition);
                positions.Add(popup.Position);

                CanvasGroup statusGroup = GetOrAddEnemyStatusCanvasGroup(unit);
                if (statusGroup != null && !statusCanvasGroups.Contains(statusGroup))
                {
                    statusCanvasGroups.Add(statusGroup);
                    statusStartAlphas.Add(statusGroup.alpha);
                }
'@
$text = ReplaceOptional $text $oldCapture $newCapture 'capture enemy status slot canvas group'

# Fade status slots in the same loop as KO sprites.
$oldFadeLoopTail = @'
                    Color color = startColors[i];
                    color.a = Mathf.Lerp(startColors[i].a, 0f, eased);
                    sprites[i].color = color;
                    rects[i].anchoredPosition = startPositions[i] + new Vector2(0f, -sink * eased);
                }

                yield return null;
'@
$newFadeLoopTail = @'
                    Color color = startColors[i];
                    color.a = Mathf.Lerp(startColors[i].a, 0f, eased);
                    sprites[i].color = color;
                    rects[i].anchoredPosition = startPositions[i] + new Vector2(0f, -sink * eased);
                }

                for (int i = 0; i < statusCanvasGroups.Count && i < statusStartAlphas.Count; i++)
                {
                    CanvasGroup group = statusCanvasGroups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    group.alpha = Mathf.Lerp(statusStartAlphas[i], 0f, eased);
                }

                yield return null;
'@
$text = ReplaceOptional $text $oldFadeLoopTail $newFadeLoopTail 'fade enemy status slots during KO fade'

# Reset status alpha after RedrawBoard so reused/replaced slots are visible again.
$oldRedraw = @'
            RedrawBoard();

            if (replacementOccurred)
'@
$newRedraw = @'
            RedrawBoard();
            ResetEnemyStatusCanvasGroupAlphas();

            if (replacementOccurred)
'@
$text = ReplaceOptional $text $oldRedraw $newRedraw 'reset enemy status alpha after redraw'

$helper = @'
        private CanvasGroup GetOrAddEnemyStatusCanvasGroup(BattleUnit unit)
        {
            Transform slot = GetEnemyStatusSlotForUnit(unit);
            if (slot == null)
            {
                return null;
            }

            CanvasGroup group = slot.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = slot.gameObject.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private Transform GetEnemyStatusSlotForUnit(BattleUnit unit)
        {
            if (enemyStatusPanel == null || unit == null)
            {
                return null;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] != unit)
                {
                    continue;
                }

                return enemyStatusPanel.Find($"EnemyStatus_{i + 1}");
            }

            return null;
        }

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
$text = InsertBeforeIfMissing $text 'private CanvasGroup GetOrAddEnemyStatusCanvasGroup(BattleUnit unit)' '        private IEnumerator PlayAutoReplacementEnterAnimation(bool isAllyBoard)' $helper 'enemy status KO fade helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy status slots to fade out with KO sprites.'
