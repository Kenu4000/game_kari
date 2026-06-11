using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // PREVIEW AREA
        // ------------------------------------------------------------
        // Visual-only preview methods live here.
        // They may highlight cells, recolor sprites, or show silhouettes.
        // They should not change HP, KO state, reserves, or turn order.
        // ============================================================


        private void ClearTargetPreview()
        {
            _hoveredSkill = null;
            ResetEnemyBoardHighlights();
            ResetBoardSpritePreviewColors();
        }


        private void RedrawTargetPreview()
        {
            ResetEnemyBoardHighlights();

            if (_hoveredSkill == null)
            {
                return;
            }

            switch (_hoveredSkill.TargetPattern)
            {
                case SkillTargetPattern.FrontTopOpponent:
                    SetEnemyBoardCellColor(GridPos.FrontTop, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.FrontBottomOpponent:
                    SetEnemyBoardCellColor(GridPos.FrontBottom, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.BothFrontOpponents:
                    SetEnemyBoardCellColor(GridPos.FrontTop, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.FrontBottom, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.AllOpponents:
                    SetEnemyBoardCellColor(GridPos.FrontTop, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.BackTop, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.FrontBottom, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.BackBottom, TargetPreviewCellColor);
                    break;
            }
        }

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
            ApplySkillHoverSilhouetteOverlapAlpha(focusedUnits);
        }


        private void ApplySkillHoverSilhouette(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Material material = GetSkillHoverSilhouetteMaterial();
            if (material != null)
            {
                image.material = material;
            }

            float resolvedAlpha = Mathf.Clamp01(alpha);
            image.color = new Color(0.5f, 0.5f, 0.5f, resolvedAlpha);

            bool showOutline = resolvedAlpha >= 0.99f;
            SetSkillHoverSilhouetteOutlineVisible(image, showOutline, resolvedAlpha);
        }

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


        private void ApplyEnemyActionSilhouettePreview(List<GridPos> targetPositions)
        {
            _enemyActionSilhouettePreviewActive = true;
            _enemyActionSilhouetteFocusPositions.Clear();

            if (targetPositions != null)
            {
                for (int i = 0; i < targetPositions.Count; i++)
                {
                    GridPos position = targetPositions[i];
                    if (!_enemyActionSilhouetteFocusPositions.Contains(position))
                    {
                        _enemyActionSilhouetteFocusPositions.Add(position);
                    }
                }
            }

            ReapplyEnemyActionSilhouettePreviewIfNeeded();
        }

        private void ClearEnemyActionSilhouettePreview()
        {
            _enemyActionSilhouettePreviewActive = false;
            _enemyActionSilhouetteFocusPositions.Clear();
        }


        private void EnsureEnemyActionPreviewPanel()
        {
            // Enemy action preview is intentionally disabled.
        }
    }
}
