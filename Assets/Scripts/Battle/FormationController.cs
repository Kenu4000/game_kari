using UnityEngine;

namespace GameKari.Battle
{
    public class FormationController
    {
        private readonly BattleGrid _grid;

        public FormationController(BattleGrid grid)
        {
            _grid = grid;
        }

        public void RotateAlliesClockwise()
        {
            BattleUnit ft = _grid.GetUnit(true, GridPos.FrontTop);
            BattleUnit bt = _grid.GetUnit(true, GridPos.BackTop);
            BattleUnit bb = _grid.GetUnit(true, GridPos.BackBottom);
            BattleUnit fb = _grid.GetUnit(true, GridPos.FrontBottom);

            _grid.SetUnit(true, GridPos.FrontTop, fb);
            _grid.SetUnit(true, GridPos.BackTop, ft);
            _grid.SetUnit(true, GridPos.BackBottom, bt);
            _grid.SetUnit(true, GridPos.FrontBottom, bb);

            Debug.Log("[Formation] Allies rotated clockwise.");
        }

        public void SwapActiveWithReserve(BattleUnit active, BattleUnit reserve)
        {
            GridPos activePos = active.GridPos;
            _grid.SetUnit(true, activePos, reserve);
        }
    }
}


