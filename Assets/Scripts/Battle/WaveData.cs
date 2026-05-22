using System.Collections.Generic;

namespace GameKari.Battle
{
    /// <summary>
    /// 1Wave分の敵配置と敵控えを持つランタイム用データ。
    /// 現時点ではScriptableObject化せず、DefaultBattleSetupFactory内で仮生成する。
    /// </summary>
    public sealed class WaveData
    {
        public readonly List<BattleUnitPlacement> EnemyPlacements = new();
        public readonly List<BattleUnit> EnemyReserves = new();
    }
}
