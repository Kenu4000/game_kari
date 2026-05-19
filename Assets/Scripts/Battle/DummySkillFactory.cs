namespace GameKari.Battle
{
    public static class DummySkillFactory
    {
        public static void AddDefaultSkills(BattleUnit unit)
        {
            if (unit == null || unit.Data == null || unit.Data.DefaultSkills == null)
            {
                return;
            }

            for (int i = 0; i < unit.Data.DefaultSkills.Count; i++)
            {
                SkillData skill = unit.Data.DefaultSkills[i];

                if (skill == null)
                {
                    continue;
                }

                unit.Skills.Add(skill);
            }
        }
    }
}

