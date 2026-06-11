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
        // ANIMATION AREA
        // ------------------------------------------------------------
        // BattleUIManager-side animation bridge methods live here.
        // The actual frame-by-frame skill animation is handled by SkillAnimationPlayer.
        // These methods should prepare RectTransforms and call animation helpers.
        // ============================================================


        // 読みやすさメモ: PlaySkillAnimationIfAny
        // 戦闘ロジックからスキルアニメーション再生へつなぐ橋渡し。
        // ここでは使用者と対象のUI位置を探し、SkillAnimationPlayerへ渡す。
        // 実際のコマ送り、弾の移動、Canvas上の動きはアニメーション側の担当。
        private IEnumerator PlaySkillAnimationIfAny(SkillData skill)
        {
            if (skill == null || skill.Animation == null || _active == null)
            {
                yield break;
            }

            RectTransform casterRect = GetBoardSpriteRect(true, _active.GridPos);
            Image casterImage = GetBoardSpriteImage(true, _active.GridPos);
            RectTransform targetRect = GetPrimarySkillAnimationTargetRect(skill);

            yield return SkillAnimationPlayer.Play(skill.Animation, casterRect, targetRect, casterImage);
        }

        // 読みやすさメモ: GetPrimarySkillAnimationTargetRect
        // スキルアニメーションで向かう主対象のRectTransformを探す。
        // これは見た目の対象であり、ダメージ対象そのものを決める場所ではない。
        // アニメーションが違うセルへ飛ぶ場合は、ここかSkillAnimationDataのAnchor設定を確認する。
        private RectTransform GetPrimarySkillAnimationTargetRect(SkillData skill)
        {
            if (skill == null)
            {
                return null;
            }

            bool targetIsAllyBoard = skill.TargetPattern == SkillTargetPattern.Self;
            List<GridPos> targetPositions = GetSkillAnimationTargetPositions(skill);
            for (int i = 0; i < targetPositions.Count; i++)
            {
                RectTransform targetRect = GetBoardSpriteRect(targetIsAllyBoard, targetPositions[i]);
                if (targetRect != null)
                {
                    return targetRect;
                }
            }

            return null;
        }


        private List<GridPos> GetSkillAnimationTargetPositions(SkillData skill)
        {
            var targets = new List<GridPos>();

            if (skill == null)
            {
                return targets;
            }

            switch (skill.TargetPattern)
            {
                case SkillTargetPattern.FrontTopOpponent:
                    targets.Add(GridPos.FrontTop);
                    break;

                case SkillTargetPattern.FrontBottomOpponent:
                    targets.Add(GridPos.FrontBottom);
                    break;

                case SkillTargetPattern.BothFrontOpponents:
                    targets.Add(GridPos.FrontTop);
                    targets.Add(GridPos.FrontBottom);
                    break;

                case SkillTargetPattern.AllOpponents:
                    AddAllGridPositions(targets);
                    break;

                case SkillTargetPattern.Self:
                    if (_active != null)
                    {
                        targets.Add(_active.GridPos);
                    }
                    break;
            }

            return targets;
        }

        private IEnumerator PlayPendingAutoReplacementAnimations()
        {
            if (_pendingEnemyKoReplacementPhase)
            {
                _pendingEnemyKoReplacementPhase = false;

                CompactEnemyFrontlineIfEmpty();
                bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
                SyncBoardUnitGridPositions();
                _statusSlotUnits.Clear();
                RedrawBoard();
                ResetEnemyStatusCanvasGroupAlphas();
                ReapplySkillHoverPreviewIfNeeded();

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
    }
}



