$ErrorActionPreference = "Stop"

$path = "Assets/Editor/BattleUICreator.cs"
if (!(Test-Path $path)) {
    throw "BattleUICreator.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
    [MenuItem("Tools/Create Battle UI")]
    public static void CreateBattleUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
'@

$new = @'
    [MenuItem("Tools/Battle UI/Legacy/Create Battle UI Template (Rebuilds Generated UI)")]
    public static void CreateBattleUI()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Create Battle UI Template",
            "This is a legacy template generator. It may remove generated battle UI roots under the Canvas. Do not use this for the current hand-edited BattleTest UI unless you intentionally want to rebuild a template.",
            "Create Template",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
'@

if (!$text.Contains($old)) {
    if ($text.Contains("Tools/Battle UI/Legacy/Create Battle UI Template")) {
        Write-Host "BattleUICreator is already legacy-safe."
        exit 0
    }

    throw "Patch anchor not found: CreateBattleUI menu"
}

$text = $text.Replace($old, $new)
$text = $text.Replace(
    '        Debug.Log("Battle UI created from Tools > Create Battle UI");',
    '        Debug.Log("Battle UI template created from Tools > Battle UI > Legacy > Create Battle UI Template.");'
)

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Made BattleUICreator legacy-safe with confirmation dialog."
