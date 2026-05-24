namespace GameKari.Battle
{
    internal static class BattleClearRewardCalculator
    {
        public static BattleClearRank EvaluateRank(int battleTurn)
        {
            if (battleTurn <= 1)
            {
                return BattleClearRank.OneTurn;
            }

            if (battleTurn == 2)
            {
                return BattleClearRank.TwoTurn;
            }

            if (battleTurn == 3)
            {
                return BattleClearRank.ThreeTurn;
            }

            return BattleClearRank.FourPlusTurn;
        }

        public static string FormatRank(BattleClearRank rank)
        {
            return rank switch
            {
                BattleClearRank.OneTurn => "1Turn Kill",
                BattleClearRank.TwoTurn => "2Turn Kill",
                _ => "3+ Turn"
            };
        }

        public static int CalculateKakeraGain(BattleClearRank rank)
        {
            return rank switch
            {
                BattleClearRank.OneTurn => 3,
                BattleClearRank.TwoTurn => 2,
                _ => 1
            };
        }

        public static int CalculateExpGain(BattleClearResult result)
        {
            if (result == null)
            {
                return 0;
            }

            return result.HasNextWave ? 10 : 30;
        }
    }
}
