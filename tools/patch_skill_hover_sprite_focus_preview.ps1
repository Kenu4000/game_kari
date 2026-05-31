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

$settings = @'
        [Header("Skill Hover Sprite Preview")]
        [SerializeField] private Color skillHoverInactiveSpriteColor = new Color(0.55f, 0.68f, 0.72f, 0.85f);
        [SerializeField] private float skillHoverOverlapTargetAlpha = 0.55f;

'@
$text = InsertBeforeIfMissing $text 'skillHoverInactiveSpriteColor' '        [Header("Turn Order Bar")]' $settings 'skill hover sprite preview settings'

$oldHover = @'
        private void HandleSkillHover(SkillData skill)
        {
            if (_battleEnded)
            {
                return;
            }

            _hoveredSkill = skill;
            RedrawTargetPreview();
        }
'@
$newHover = @'
        private void HandleSkillHover(SkillData skill)
        {
            if (_battleEnded)
            {
                return;
            }

            _hoveredSkill = skill;
            RedrawTargetPreview();
            ApplySkillHoverSpritePreview();
        }
'@
$text = ReplaceOptional $text $oldHover $newHover 'apply sprite preview on skill hover'

$oldClear = @'
        private void ClearTargetPreview()
        {
            _hoveredSkill = null;
            ResetEnemyBoardHighlights();
        }
'@
$newClear = @'
        private void ClearTargetPreview()
        {
            _hoveredSkill = null;
            ResetEnemyBoardHighlights();
            ResetBoardSpritePreviewColors();
        }
'@
$text = ReplaceOptional $text $oldClear $newClear 'reset sprite preview on hover exit'

$helpers = @'
        private void ApplySkillHoverSpritePreview()
        {
            ResetBoardSpritePreviewColors();

            if (_hoveredSkill == null || _active == null || _active.IsDead)
            {
                return;
            }

            var focusedUnits = new HashSet<BattleUnit>();
            focusedUnits.Add(_active);

            bool targetIsAllyBoard = _hoveredSkill.TargetPattern == SkillTargetPattern.Self;
            List<GridPos> targetPositions = GetSkillAnimationTargetPositions(_hoveredSkill);
            for (int i = 0; i < targetPositions.Count; i++)
            {
                BattleUnit targetUnit = _grid.GetUnit(targetIsAllyBoard, targetPositions[i]);
                if (targetUnit != null && !targetUnit.IsDead)
                {
                    focusedUnits.Add(targetUnit);
                }
            }

            ApplySpriteFocusColors(true, focusedUnits);
            ApplySpriteFocusColors(false, focusedUnits);
            ApplySkillHoverOverlapAlpha(targetIsAllyBoard, targetPositions);
        }

        private void ApplySpriteFocusColors(bool isAllyBoard, HashSet<BattleUnit> focusedUnits)
        {
            ApplySpriteFocusColorAt(isAllyBoard, GridPos.FrontTop, focusedUnits);
            ApplySpriteFocusColorAt(isAllyBoard, GridPos.BackTop, focusedUnits);
            ApplySpriteFocusColorAt(isAllyBoard, GridPos.FrontBottom, focusedUnits);
            ApplySpriteFocusColorAt(isAllyBoard, GridPos.BackBottom, focusedUnits);
        }

        private void ApplySpriteFocusColorAt(bool isAllyBoard, GridPos position, HashSet<BattleUnit> focusedUnits)
        {
            BattleUnit unit = _grid.GetUnit(isAllyBoard, position);
            Image image = GetBoardSpriteImage(isAllyBoard, position);
            if (image == null)
            {
                return;
            }

            if (unit == null || unit.IsDead || focusedUnits == null || !focusedUnits.Contains(unit))
            {
                image.color = skillHoverInactiveSpriteColor;
                return;
            }

            image.color = Color.white;
        }

        private void ApplySkillHoverOverlapAlpha(bool targetIsAllyBoard, List<GridPos> targetPositions)
        {
            if (_active == null || targetPositions == null || targetPositions.Count == 0)
            {
                return;
            }

            RectTransform activeRect = GetBoardSpriteRect(true, _active.GridPos);
            if (activeRect == null)
            {
                return;
            }

            for (int i = 0; i < targetPositions.Count; i++)
            {
                GridPos targetPosition = targetPositions[i];
                RectTransform targetRect = GetBoardSpriteRect(targetIsAllyBoard, targetPosition);
                if (targetRect == null || targetRect == activeRect)
                {
                    continue;
                }

                if (!RectTransformsOverlap(activeRect, targetRect))
                {
                    continue;
                }

                Image targetImage = GetBoardSpriteImage(targetIsAllyBoard, targetPosition);
                if (targetImage == null)
                {
                    continue;
                }

                Color color = targetImage.color;
                color.a = Mathf.Clamp01(skillHoverOverlapTargetAlpha);
                targetImage.color = color;
            }
        }

        private void ResetBoardSpritePreviewColors()
        {
            ResetBoardSpritePreviewColors(true);
            ResetBoardSpritePreviewColors(false);
        }

        private void ResetBoardSpritePreviewColors(bool isAllyBoard)
        {
            ResetBoardSpritePreviewColorAt(isAllyBoard, GridPos.FrontTop);
            ResetBoardSpritePreviewColorAt(isAllyBoard, GridPos.BackTop);
            ResetBoardSpritePreviewColorAt(isAllyBoard, GridPos.FrontBottom);
            ResetBoardSpritePreviewColorAt(isAllyBoard, GridPos.BackBottom);
        }

        private void ResetBoardSpritePreviewColorAt(bool isAllyBoard, GridPos position)
        {
            BattleUnit unit = _grid == null ? null : _grid.GetUnit(isAllyBoard, position);
            Image image = GetBoardSpriteImage(isAllyBoard, position);
            if (image == null)
            {
                return;
            }

            image.color = unit != null && unit.IsDead
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;
        }

        private Image GetBoardSpriteImage(bool isAllyBoard, GridPos position)
        {
            TMP_Text cellLabel = GetBoardCellLabel(isAllyBoard, position);
            if (cellLabel == null || cellLabel.transform.parent == null)
            {
                return null;
            }

            Transform spriteTransform = cellLabel.transform.parent.Find("BattleSpriteImage");
            return spriteTransform == null ? null : spriteTransform.GetComponent<Image>();
        }

        private static bool RectTransformsOverlap(RectTransform a, RectTransform b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            Vector3[] aCorners = new Vector3[4];
            Vector3[] bCorners = new Vector3[4];
            a.GetWorldCorners(aCorners);
            b.GetWorldCorners(bCorners);

            Rect aRect = CornersToRect(aCorners);
            Rect bRect = CornersToRect(bCorners);
            return aRect.Overlaps(bRect);
        }

        private static Rect CornersToRect(Vector3[] corners)
        {
            if (corners == null || corners.Length < 4)
            {
                return Rect.zero;
            }

            float minX = corners[0].x;
            float maxX = corners[0].x;
            float minY = corners[0].y;
            float maxY = corners[0].y;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 corner = corners[i];
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

'@
$text = InsertBeforeIfMissing $text 'private void ApplySkillHoverSpritePreview()' '        private void HandleSwap(BattleUnit reserve)' $helpers 'skill hover sprite preview helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched skill hover sprite focus preview.'
