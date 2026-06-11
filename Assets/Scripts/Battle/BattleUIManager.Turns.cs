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
        // TURNS AREA
        // ------------------------------------------------------------
        // Turn order and battle phase methods live here.
        // This file should answer whose turn it is and which phase the battle is in.
        // It should call view/action helpers rather than drawing every UI detail inline.
        // ============================================================


        // 読みやすさメモ: EnterCommandSelect
        // プレイヤーがコマンドを選べるフェーズに入る処理。
        // コマンドUIを準備し、前の行動状態を整理する。
        // コマンドが出るタイミングがおかしい場合は、まずここを見る。
        private void EnterCommandSelect(BattleUnit activeUnit)
        {
            if (_battleEnded)
            {
                return;
            }

            ClearEnemyActionSilhouettePreview();
            ClearTargetPreview();
            _phase = BattlePhase.CommandSelect;
            _active = activeUnit;
            EnsureEnemyActionStatesForPreview();
            UpdateEnemyActionPreview();
            RedrawEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(true);
            HideActionOverlay();
            SetCommandUiVisible(true);

            if (commandPanel != null)
            {
                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
                commandPanel.SetInteractable(true);
            }

            if (rotateButton != null)
            {
                rotateButton.interactable = true;
            }
        }


        // Phase transitions
        // 読みやすさメモ: EnterResolvingAction
        // 行動解決中フェーズに入る処理。
        // このフェーズ中は、基本的にコマンド入力を受け付けない。
        // 演出の都合でプレビューを残す場合もあるが、入力状態とは分けて考える。
        private void EnterResolvingAction()
        {
            if (_battleEnded)
            {
                return;
            }

            _phase = BattlePhase.ResolvingAction;
            // Keep skill-hover silhouettes visible during the action animation.
            if (_hoveredSkill != null)
            {
                RedrawTargetPreview();
                ApplySkillHoverSpritePreview();
            }
            else
            {
                ResetEnemyBoardHighlights();
                ResetBoardSpritePreviewColors();
            }

            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            SetActionOverlayVisible(true);

            if (commandPanel != null)
            {
                commandPanel.SetInteractable(false);
            }

            if (rotateButton != null)
            {
                rotateButton.interactable = false;
            }
        }

        // 読みやすさメモ: RedrawTurnOrderBar
        // 行動順バーの表示を更新する。
        // 実際の行動順を決める場所ではなく、現在の行動順データを描画する場所。
        // 実際の行動は正しいのに表示だけ変な場合は、この周辺を見る。
        private void RedrawTurnOrderBar()
        {
            if (CanGenerateTurnOrderSlots())
            {
                RedrawGeneratedTurnOrderSlots();

                if (turnOrderBarText != null)
                {
                    turnOrderBarText.gameObject.SetActive(false);
                }

                return;
            }

            if (turnOrderBarText == null)
            {
                return;
            }

            turnOrderBarText.gameObject.SetActive(true);
            turnOrderBarText.text = BuildTurnOrderBarText();
        }
    }
}



