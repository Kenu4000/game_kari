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
        // STATUS PANEL AREA
        // ------------------------------------------------------------
        // Status panel drawing and HP bar display methods live here.
        // These methods should display battle data, not decide battle rules.
        // If displayed HP looks wrong but unit HP is correct, start here.
        // ============================================================


        // READABLE-REFORM: RedrawStatusPanels
        // Redraws status panels from current battle data.
        // Status panels are display objects only. They should show HP and KO state, not decide them.
        // If a bar looks wrong but the BattleUnit HP is correct, debug this area.
        private void RedrawStatusPanels()
        {
            List<BattleUnit> enemyStatusUnits = GetEnemyStatusDisplayUnits();

            for (int i = 0; i < 4; i++)
            {
                RedrawEnemyStatusSlot(i + 1, GetUnitAt(enemyStatusUnits, i));
                RedrawAllyStatusSlot(i + 1, GetUnitAt(_allies, i));
            }

            ResizeEnemyStatusPanel(enemyStatusUnits.Count);
            LayoutEnemyStatusSlots(enemyStatusUnits.Count);
        }


        private void ResetEnemyStatusCanvasGroupAlphas()
        {
            if (enemyStatusPanel == null)
            {
                return;
            }

            for (int i = 1; i <= 4; i++)
            {
                Transform slot = enemyStatusPanel.Find($"EnemyStatus_{i}");
                if (slot == null)
                {
                    continue;
                }

                CanvasGroup group = slot.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = 1f;
                }
            }
        }
    }
}



