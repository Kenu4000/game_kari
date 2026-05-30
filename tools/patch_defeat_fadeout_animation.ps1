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

$text = InsertBeforeIfMissing $text 'private float defeatFadeSeconds' '        [SerializeField] private int targetHitShakeCount = 3;' @'
        [SerializeField] private float defeatFadeSeconds = 0.22f;
        [SerializeField] private float defeatSinkDistance = 18f;

'@ 'defeat fade fields'

$text = ReplaceOptional $text @'
            yield return PlayPendingDamageHitReactions();

            HideActiveActionValuePopups();
'@ @'
            yield return PlayPendingDamageHitReactions();
            yield return PlayPendingDefeatFadeOuts();

            HideActiveActionValuePopups();
'@ 'run defeat fade after hit reaction'

$text = ReplaceOptional $text @'
                defeated.Unit.IsDead = true;
                _grid.SetUnit(false, defeated.Position, null);
                RemoveTurnState(defeated.Unit);

                Debug.Log($"[KO] {defeated.Unit.Name} is defeated and removed from grid.");
'@ @'
                defeated.Unit.IsDead = true;
                RemoveTurnState(defeated.Unit);

                Debug.Log($"[KO] {defeated.Unit.Name} is defeated. Grid removal is deferred until fadeout completes.");
'@ 'defer defeated enemy removal'

$helpers = @'
        private IEnumerator PlayPendingDefeatFadeOuts()
        {
            List<ActionValuePopup> damagePopups = GetPendingDamagePopups();
            if (damagePopups.Count == 0)
            {
                yield break;
            }

            var sprites = new List<Image>();
            var rects = new List<RectTransform>();
            var startColors = new List<Color>();
            var startPositions = new List<Vector2>();
            var positions = new List<GridPos>();

            for (int i = 0; i < damagePopups.Count; i++)
            {
                ActionValuePopup popup = damagePopups[i];
                if (popup == null || popup.IsAllyBoard)
                {
                    continue;
                }

                BattleUnit unit = _grid.GetUnit(false, popup.Position);
                if (unit == null || !unit.IsDead)
                {
                    continue;
                }

                RectTransform rect = GetBoardSpriteRect(false, popup.Position);
                if (rect == null || rects.Contains(rect))
                {
                    continue;
                }

                Image image = rect.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                sprites.Add(image);
                rects.Add(rect);
                startColors.Add(image.color);
                startPositions.Add(rect.anchoredPosition);
                positions.Add(popup.Position);
            }

            if (sprites.Count == 0)
            {
                yield break;
            }

            float duration = Mathf.Max(0f, defeatFadeSeconds);
            float sink = Mathf.Max(0f, defeatSinkDistance);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);

                for (int i = 0; i < sprites.Count && i < rects.Count; i++)
                {
                    if (sprites[i] == null || rects[i] == null)
                    {
                        continue;
                    }

                    Color color = startColors[i];
                    color.a = Mathf.Lerp(startColors[i].a, 0f, eased);
                    sprites[i].color = color;
                    rects[i].anchoredPosition = startPositions[i] + new Vector2(0f, -sink * eased);
                }

                yield return null;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                _grid.SetUnit(false, positions[i], null);
            }

            CompactEnemyFrontlineIfEmpty();
            FillEmptyEnemyCellsFromReserves();
            RedrawBoard();
        }

'@

$text = InsertBeforeIfMissing $text 'private IEnumerator PlayPendingDefeatFadeOuts' '        private void SetActionSourceFlashTargetsVisible' $helpers 'defeat fade helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched defeat fadeout animation.'
