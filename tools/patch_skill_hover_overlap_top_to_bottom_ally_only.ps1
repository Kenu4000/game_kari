$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$old = @'
        private void ApplySkillHoverSilhouetteOverlapAlpha(HashSet<BattleUnit> focusedUnits)
        {
            if (_active == null || _active.IsDead)
            {
                return;
            }

            RectTransform activeRect = GetBoardSpriteRect(true, _active.GridPos);
            if (activeRect == null)
            {
                return;
            }

            var activeOnlyRects = new List<RectTransform> { activeRect };

            ApplySilhouetteOverlapAlphaAt(true, GridPos.FrontTop, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.BackTop, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.FrontBottom, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.BackBottom, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.FrontTop, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.BackTop, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.FrontBottom, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.BackBottom, focusedUnits, activeOnlyRects);
        }
'@

$new = @'
        private void ApplySkillHoverSilhouetteOverlapAlpha(HashSet<BattleUnit> focusedUnits)
        {
            if (_active == null || _active.IsDead)
            {
                return;
            }

            GridPos bottomPosition;
            switch (_active.GridPos)
            {
                case GridPos.FrontTop:
                    bottomPosition = GridPos.FrontBottom;
                    break;

                case GridPos.BackTop:
                    bottomPosition = GridPos.BackBottom;
                    break;

                default:
                    // Overlap transparency is only for an active ally in a Top cell.
                    return;
            }

            BattleUnit bottomUnit = _grid == null ? null : _grid.GetUnit(true, bottomPosition);
            if (bottomUnit == null || bottomUnit.IsDead)
            {
                return;
            }

            if (focusedUnits != null && focusedUnits.Contains(bottomUnit))
            {
                return;
            }

            RectTransform activeRect = GetBoardSpriteRect(true, _active.GridPos);
            RectTransform bottomRect = GetBoardSpriteRect(true, bottomPosition);
            if (activeRect == null || bottomRect == null || !RectTransformsOverlap(activeRect, bottomRect))
            {
                return;
            }

            Image bottomImage = GetBoardSpriteImage(true, bottomPosition);
            if (bottomImage == null)
            {
                return;
            }

            ApplySkillHoverSilhouette(bottomImage, skillHoverSilhouetteOverlapAlpha);
        }
'@

$text = ReplaceOptional $text $old $new 'limit skill hover overlap to active Top ally against corresponding Bottom ally'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched skill hover overlap transparency to only affect corresponding bottom ally when active ally is in a top cell.'
