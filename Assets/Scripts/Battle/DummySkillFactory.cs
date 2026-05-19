using System.Collections.Generic;

namespace GameKari.Battle
{
    public static class DummySkillFactory
    {
        public static void AddDefaultSkills(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            List<SkillData> defaultSkills = DummySkillCatalog.CreateSkillsForUnit(unit);

            for (int i = 0; i < defaultSkills.Count; i++)
            {
                SkillData skill = defaultSkills[i];

                if (skill == null)
                {
                    continue;
                }

                unit.Skills.Add(skill);
            }
        }
    }
}
