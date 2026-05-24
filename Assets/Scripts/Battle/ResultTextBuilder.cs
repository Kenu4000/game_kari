using UnityEngine;

namespace GameKari.Battle
{
    /// <summary>
    /// Builds result screen text without owning UI or advancing state.
    /// </summary>
    internal static class ResultTextBuilder
    {
        public static string BuildBattleResultText(
            bool hasResult,
            string rankText,
            int kakeraStock,
            int maxKakeraStock,
            int kakeraGain,
            int expGain,
            int partyHealAmount)
        {
            if (!hasResult)
            {
                return "Next: Movement";
            }

            string healText = partyHealAmount > 0
                ? $"HP Bonus: +{partyHealAmount}"
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
    }
}
