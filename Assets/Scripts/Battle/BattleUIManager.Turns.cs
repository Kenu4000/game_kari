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
