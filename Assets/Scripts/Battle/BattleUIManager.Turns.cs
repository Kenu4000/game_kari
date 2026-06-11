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


        // READABLE-REFORM: EnterCommandSelect
        // Enters the phase where the player can choose a command.
        // This is a phase transition method. It should prepare command UI and clear old action state.
        // If commands appear at the wrong time, start here.
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
        // READABLE-REFORM: EnterResolvingAction
        // Enters the phase where an action is being resolved.
        // During this phase, command input should be blocked.
        // Some visual previews may intentionally remain until the action animation needs them cleared.
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

        // READABLE-REFORM: RedrawTurnOrderBar
        // Updates the turn order display.
        // This should not decide actual turn order by itself; it should draw the current turn-order state.
        // If the display is wrong but actions occur correctly, debug here.
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



