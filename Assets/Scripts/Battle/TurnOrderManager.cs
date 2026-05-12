using System.Collections.Generic;
using System.Linq;

namespace GameKari.Battle
{
    public class TurnOrderManager
    {
        private readonly List<BattleUnit> _turnOrder = new();

        public IReadOnlyList<BattleUnit> TurnOrder => _turnOrder;

        public void RebuildTurnOrder(IEnumerable<BattleUnit> livingUnits)
        {
            _turnOrder.Clear();
            _turnOrder.AddRange(livingUnits.Where(u => u != null && !u.IsDead)
                .OrderByDescending(u => u.Data.Speed));
        }
    }
}
