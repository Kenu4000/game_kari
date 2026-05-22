using UnityEngine;

namespace GameKari.Battle
{
    /// <summary>
    /// 1クエスト内のWave進行状態を持つランタイム用クラス。
    /// 現時点では単発Battleを1Waveとして扱うが、
    /// 将来の複数Wave化に備えて WaveTurn と Distance を BattleUIManager から分離する。
    /// </summary>
    public sealed class WaveProgressState
    {
        public int WaveTurn { get; private set; } = 1;
        public int CurrentDistance { get; private set; }
        public int TargetDistance { get; }
        public int BaseWaveDistance { get; private set; }

        public WaveProgressState(int targetDistance, int baseWaveDistance)
        {
            TargetDistance = Mathf.Max(1, targetDistance);
            BaseWaveDistance = Mathf.Max(0, baseWaveDistance);
            ResetForQuest();
        }

        public void ResetForQuest()
        {
            CurrentDistance = 0;
            StartWave(BaseWaveDistance);
        }

        public void StartWave()
        {
            WaveTurn = 1;
        }

        public void StartWave(int baseWaveDistance)
        {
            BaseWaveDistance = Mathf.Max(0, baseWaveDistance);
            WaveTurn = 1;
        }

        public void AdvanceTurn()
        {
            WaveTurn++;
        }

        public int AddDistance(int amount)
        {
            CurrentDistance = Mathf.Min(TargetDistance, CurrentDistance + Mathf.Max(0, amount));
            return CurrentDistance;
        }
    }
}

