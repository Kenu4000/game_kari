namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // KO / REPLACEMENT AREA
        // ------------------------------------------------------------
        // Defeat, KO, reserve replacement, and enemy compacting methods live here.
        // This area is fragile because visual timing and battle data timing interact.
        // Move slowly and keep comments near any delayed status-panel update.
        // ============================================================


        private void HandleAllyDefeated(BattleUnit defeatedAlly)
        {
            if (defeatedAlly == null || defeatedAlly.IsDead)
            {
                return;
            }

            defeatedAlly.IsDead = true;
            Debug.Log($"[KO] {defeatedAlly.Name} is defeated.");

            GridPos position = defeatedAlly.GridPos;

            BattleUnit replacement = GetNextReserve();
            if (replacement == null)
            {
                _grid.SetUnit(true, position, null);
                RemoveTurnState(defeatedAlly);

                Debug.Log($"[KO] No reserve available for {defeatedAlly.Name}. Ally grid cell is now empty: {position}");

                CheckBattleEnd();
                RedrawBoard();
                return;
            }

            _grid.SetUnit(true, position, replacement);

            int allyIndex = _allies.IndexOf(defeatedAlly);
            if (allyIndex >= 0)
            {
                _allies[allyIndex] = replacement;
            }

            _reserves.Remove(replacement);
            RemoveTurnState(defeatedAlly);

            _actedUnits.Add(replacement);

            if (_active == defeatedAlly)
            {
                _active = replacement;
                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
            }

            Debug.Log($"[KO] {replacement.Name} replaced {defeatedAlly.Name} at {position}. Replacement cannot act this turn.");
            CheckBattleEnd();
        }


        // KO and replacement handling
        private void ResolveDefeatedEnemies(List<DefeatedEnemyInfo> defeatedEnemies)
        {
            if (defeatedEnemies == null || defeatedEnemies.Count == 0)
            {
                return;
            }

            for (int i = 0; i < defeatedEnemies.Count; i++)
            {
                DefeatedEnemyInfo defeated = defeatedEnemies[i];
                if (defeated == null || defeated.Unit == null || defeated.Unit.IsDead)
                {
                    continue;
                }

                defeated.Unit.IsDead = true;
                if (!_enemyStatusKoVisibleUnits.Contains(defeated.Unit))
                {
                    _enemyStatusKoVisibleUnits.Add(defeated.Unit);
                }
                RemoveTurnState(defeated.Unit);

                Debug.Log($"[KO] {defeated.Unit.Name} is defeated. Grid removal is deferred until fadeout completes.");
            }

            // Enemy grid movement and reserve entry are deferred until the KO fadeout finishes.
            CheckBattleEnd();
        }

        private void CompactEnemyFrontlineIfEmpty()
        {
            BattleUnit frontTop = _grid.GetUnit(false, GridPos.FrontTop);
            BattleUnit frontBottom = _grid.GetUnit(false, GridPos.FrontBottom);

            bool hasFrontTop = frontTop != null && !frontTop.IsDead;
            bool hasFrontBottom = frontBottom != null && !frontBottom.IsDead;

            if (hasFrontTop || hasFrontBottom)
            {
                return;
            }

            BattleUnit backTop = _grid.GetUnit(false, GridPos.BackTop);
            BattleUnit backBottom = _grid.GetUnit(false, GridPos.BackBottom);

            bool hasBackTop = backTop != null && !backTop.IsDead;
            bool hasBackBottom = backBottom != null && !backBottom.IsDead;

            if (!hasBackTop && !hasBackBottom)
            {
                return;
            }

            if (hasBackTop)
            {
                _grid.SetUnit(false, GridPos.BackTop, null);
                _grid.SetUnit(false, GridPos.FrontTop, backTop);
            }

            if (hasBackBottom)
            {
                _grid.SetUnit(false, GridPos.BackBottom, null);
                _grid.SetUnit(false, GridPos.FrontBottom, backBottom);
            }

            Debug.Log("[Formation] Compacted enemy frontline.");
        }

        private bool FillEmptyEnemyCellsFromReserves()
        {
            bool changed = false;
            changed |= TryFillEnemyCellFromReserve(GridPos.FrontTop);
            changed |= TryFillEnemyCellFromReserve(GridPos.FrontBottom);
            changed |= TryFillEnemyCellFromReserve(GridPos.BackTop);
            changed |= TryFillEnemyCellFromReserve(GridPos.BackBottom);
            return changed;
        }
    }
}
