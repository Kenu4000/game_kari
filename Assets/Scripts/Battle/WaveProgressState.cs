namespace GameKari.Battle
{
    /// <summary>
    /// 固定Battle内のターン進行状態を持つランタイム用クラス。
    /// 旧Distance制は廃止し、現在はClear評価用のWaveTurnだけを管理する。
    /// </summary>
    public sealed class WaveProgressState
    {
        public int WaveTurn { get; private set; } = 1;

        public WaveProgressState()
        {
            ResetForQuest();
        }

        public void ResetForQuest()
        {
            StartWave();
        }

        public void StartWave()
        {
            WaveTurn = 1;
        }

        public void AdvanceTurn()
        {
            WaveTurn++;
        }
    }
}
