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

# Only the active actor should be used as the overlap reference.
# Target sprites and other focused sprites must not make silhouettes transparent.
$old = @'
        private void ApplySkillHoverSilhouetteOverlapAlpha(HashSet<BattleUnit> focusedUnits)
        {
            if (focusedUnits == null || focusedUnits.Count == 0)
            {
                return;
            }

            var focusedRects = new List<RectTransform>();
            AddFocusedSpriteRects(true, focusedUnits, focusedRects);
            AddFocusedSpriteRects(false, focusedUnits, focusedRects);

            if (focusedRects.Count == 0)
            {
                return;
            }

            ApplySilhouetteOverlapAlphaAt(true, GridPos.FrontTop, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.BackTop, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.FrontBottom, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.BackBottom, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.FrontTop, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.BackTop, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.FrontBottom, focusedUnits, focusedRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.BackBottom, focusedUnits, focusedRects);
        }
'@

$new = @'
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

$text = ReplaceOptional $text $old $new 'limit silhouette overlap reference to active unit only'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched skill hover silhouette overlap to use only the active unit as reference.'
