namespace GameKari.Battle
{
    /// <summary>
    /// 1クエスト内の進行状態を持つランタイム用クラス。
    /// 現時点では最初のWaveだけを使うが、
    /// 将来の複数Wave進行に備えて現在Wave参照をここに集約する。
    /// </summary>
    public sealed class QuestProgressState
    {
        public QuestData Quest { get; }
        public int CurrentWaveIndex { get; private set; }

        public QuestProgressState(QuestData quest)
        {
            Quest = quest;
            CurrentWaveIndex = 0;
        }

        public WaveData CurrentWave
        {
            get
            {
                if (Quest == null || Quest.Waves.Count == 0)
                {
                    return null;
                }

                if (CurrentWaveIndex < 0 || CurrentWaveIndex >= Quest.Waves.Count)
                {
                    return null;
                }

                return Quest.Waves[CurrentWaveIndex];
            }
        }

        public bool HasNextWave
        {
            get
            {
                return Quest != null
                    && CurrentWaveIndex + 1 < Quest.Waves.Count;
            }
        }

        public bool MoveNextWave()
        {
            if (!HasNextWave)
            {
                return false;
            }

            CurrentWaveIndex++;
            return true;
        }
    }
}
