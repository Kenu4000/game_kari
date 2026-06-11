namespace GameKari.Battle
{
    public partial class BattleUIManager
    {
        // ============================================================
        // KO / REPLACEMENT AREA
        // ------------------------------------------------------------
        // Future destination for defeated-unit handling.
        //
        // This is the most fragile area. Move code here slowly.
        // Known sensitive rules:
        //   - Ally KO can trigger reserve replacement.
        //   - Enemy KO can trigger backline compacting.
        //   - StatusPanel timing must not make HP bars look restored.
        //
        // First pass should add comments before changing behavior.
        // ============================================================
    }
}