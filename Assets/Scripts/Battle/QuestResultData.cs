namespace GameKari.Battle
{
    /// <summary>
    /// Quest Clear時に表示・集計するためのランタイム結果データ。
    /// 現時点では最低限の集計だけを持つ。
    /// </summary>
    public sealed class QuestResultData
    {
        public int ClearedWaveCount;
        public int TotalWaveCount;
        public int CurrentDistance;
        public int TargetDistance;
        public int AlivePartyCount;
        public int KnockedOutPartyCount;
        public int TotalPartyCount;
    }
}
