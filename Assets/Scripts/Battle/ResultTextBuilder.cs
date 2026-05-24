using UnityEngine;

namespace GameKari.Battle
{
    /// <summary>
    /// Builds result screen text without owning UI or advancing state.
    /// </summary>
    internal static class ResultTextBuilder
    {
        public static string BuildBattleResultText(
            BattleClearResult result,
            int kakeraStock,
            int maxKakeraStock,
            int kakeraGain,
            int expGain)
        {
            if (result == null)
            {
                return "Next: Movement";
            }

            string rankText = FormatBattleClearRank(result.Rank);
            string healText = result.PartyHealAmount > 0
                ? $"HP Bonus: +{result.PartyHealAmount}"
                : "HP Bonus: none";

            return
                $"Clear: {rankText}\n" +
                $"Kakera: +{kakeraGain} / Stock {kakeraStock}/{maxKakeraStock}\n" +
                $"EXP: +{expGain}\n" +
                $"{healText}\n" +
                "Next: Movement";
        }

        public static string BuildQuestEndSummaryText(int clearedBattles, int totalBattles, int totalKakeraEarned, int totalExpEarned)
        {
            return
                $"Battles Cleared: {clearedBattles} / {Mathf.Max(1, totalBattles)}\n" +
                $"Kakera Earned: {totalKakeraEarned}\n" +
                $"EXP: {totalExpEarned}\n" +
                "Next: Return to Base";
        }

        private static string FormatBattleClearRank(BattleClearRank rank)
        {
            return rank switch
            {
                BattleClearRank.OneTurn => "One Turn Clear",
                BattleClearRank.TwoTurn => "Two Turn Clear",
                BattleClearRank.ThreeTurn => "Three Turn Clear",
                _ => "Clear"
            };
        }
    }
}
