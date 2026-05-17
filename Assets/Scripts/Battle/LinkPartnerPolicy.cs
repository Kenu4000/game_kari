using System.Collections.Generic;

namespace GameKari.Battle
{
    public static class LinkPartnerPolicy
    {
        public static bool HasLivingPartnerCandidate(BattleUnit user, IReadOnlyList<BattleUnit> allies)
        {
            if (user == null || user.IsDead || allies == null)
            {
                return false;
            }

            for (int i = 0; i < allies.Count; i++)
            {
                BattleUnit ally = allies[i];
                if (ally == null || ally == user || ally.IsDead)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public static BattleUnit FindFirstAvailablePartner(BattleUnit user, IReadOnlyList<BattleUnit> allies)
        {
            if (user == null || user.IsDead || allies == null)
            {
                return null;
            }

            for (int i = 0; i < allies.Count; i++)
            {
                BattleUnit ally = allies[i];
                if (ally == null || ally == user || ally.IsDead || ally.LinkCooldownRemaining > 0)
                {
                    continue;
                }

                return ally;
            }

            return null;
        }

        public static bool HasAvailablePartner(BattleUnit user, IReadOnlyList<BattleUnit> allies)
        {
            return FindFirstAvailablePartner(user, allies) != null;
        }

        public static string BuildUnavailableReason(BattleUnit user, IReadOnlyList<BattleUnit> allies)
        {
            return HasLivingPartnerCandidate(user, allies)
                ? "No ready link partner."
                : "No available link partner.";
        }
    }
}