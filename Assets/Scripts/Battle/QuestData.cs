using System.Collections.Generic;

namespace GameKari.Battle
{
    /// <summary>
    /// 1クエスト分の進行用ランタイムデータ。
    /// 現時点ではScriptableObject化せず、DefaultQuestFactoryで仮生成する。
    /// </summary>
    public sealed class QuestData
    {
        public readonly List<WaveData> Waves = new();

        // 現在は単発Battleを1Waveとして扱うための仮設定。
        // 将来はQuestData / WaveData ScriptableObject側へ移す。
        public int TargetDistance = 100;
        public int BaseWaveDistance = 20;
        public int OneTurnClearPartyHeal = 5;
    }
}
