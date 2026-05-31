$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceRegexRequired($src, $pattern, $replacement, $label) {
    $result = [regex]::Replace($src, $pattern, $replacement, 1)
    if ($result -eq $src) { throw "Patch anchor not found: $label" }
    Write-Host "Replaced: $label"
    return $result
}

$replacement = @'
        private void ApplySkillHoverSilhouetteOverlapAlpha(HashSet<BattleUnit> focusedUnits)
        {
            if (_active == null || _active.IsDead || _grid == null)
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
                    return;
            }

            BattleUnit bottomUnit = _grid.GetUnit(true, bottomPosition);
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
            if (activeRect == null || bottomRect == null)
            {
                return;
            }

            if (!RectTransformsOverlap(activeRect, bottomRect))
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

$text = ReplaceRegexRequired $text '(?s)        private void ApplySkillHoverSilhouetteOverlapAlpha\(HashSet<BattleUnit> focusedUnits\)\s*\{.*?\n        \}\s*\n' $replacement 'ApplySkillHoverSilhouetteOverlapAlpha top-bottom only'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Restored top-to-bottom ally overlap alpha rule.'
