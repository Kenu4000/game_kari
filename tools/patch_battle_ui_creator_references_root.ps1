$ErrorActionPreference = "Stop"

$path = "Assets/Editor/BattleUICreator.cs"
if (!(Test-Path $path)) {
    throw "BattleUICreator.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-Required {
    param([string]$Source, [string]$Old, [string]$New, [string]$Label)
    if (!$Source.Contains($Old)) {
        Write-Host "Already replaced or not found: $Label"
        return $Source
    }
    return $Source.Replace($Old, $New)
}

function Insert-Before {
    param([string]$Source, [string]$Anchor, [string]$Insertion, [string]$Label)
    if ($Source.Contains($Insertion.Trim())) {
        Write-Host "Already inserted: $Label"
        return $Source
    }
    $index = $Source.IndexOf($Anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $Label" }
    return $Source.Substring(0, $index) + $Insertion + $Source.Substring($index)
}

# Add namespace import for BattleUIReferences / CommandPanelController.
$text = Replace-Required `
    -Source $text `
    -Old @'
using TMPro;

public static class BattleUICreator
'@ `
    -New @'
using TMPro;
using GameKari.Battle;

public static class BattleUICreator
'@ `
    -Label "using GameKari.Battle"

# Create under BattleScreenRoot and auto-fill references.
$text = Replace-Required `
    -Source $text `
    -Old @'
        RemoveExistingBattleUI(canvas.transform);
        CreateMainPanels(canvas.transform);
        Debug.Log("Battle UI created from Tools > Create Battle UI");
'@ `
    -New @'
        RemoveExistingBattleUI(canvas.transform);
        GameObject battleScreenRoot = CreateBattleScreenRoot(canvas.transform);
        CreateMainPanels(battleScreenRoot.transform);
        AssignBattleUIReferences(battleScreenRoot);
        Debug.Log("Battle UI created from Tools > Create Battle UI");
'@ `
    -Label "Create under BattleScreenRoot"

$text = Replace-Required `
    -Source $text `
    -Old @'
    private static readonly string[] GeneratedRootNames =
    {
        "TopActionPanel",
'@ `
    -New @'
    private static readonly string[] GeneratedRootNames =
    {
        "BattleScreenRoot",
        "TopActionPanel",
'@ `
    -Label "GeneratedRootNames BattleScreenRoot"

# Ensure CommandPanel has controller in generated UI.
$text = Replace-Required `
    -Source $text `
    -Old @'
        GameObject commandPanel = CreatePanel(canvas, "CommandPanel", new Vector2(960, 800), new Vector2(840, 220), PanelColor());
        CreateCommandPanelChildren(commandPanel.transform);
'@ `
    -New @'
        GameObject commandPanel = CreatePanel(canvas, "CommandPanel", new Vector2(960, 800), new Vector2(840, 220), PanelColor());
        EnsureComponent<CommandPanelController>(commandPanel);
        CreateCommandPanelChildren(commandPanel.transform);
'@ `
    -Label "Ensure CommandPanelController"

# Add helper methods before CreateMainPanels.
$text = Insert-Before `
    -Source $text `
    -Anchor "    private static void CreateMainPanels(Transform canvas)" `
    -Insertion @'
    private static GameObject CreateBattleScreenRoot(Transform canvas)
    {
        GameObject root = GetOrCreate(canvas, "BattleScreenRoot");
        RectTransform rt = EnsureRectTransform(root);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        EnsureComponent<BattleUIReferences>(root);
        return root;
    }

'@ `
    -Label "CreateBattleScreenRoot"

# Add assignment helpers before PanelColor.
$text = Insert-Before `
    -Source $text `
    -Anchor "    private static Color PanelColor()" `
    -Insertion @'
    private static void AssignBattleUIReferences(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        BattleUIReferences refs = EnsureComponent<BattleUIReferences>(root);

        GameObject commandPanelObject = FindChild(root.transform, "CommandPanel");
        GameObject rotateButtonObject = FindChild(root.transform, "RotateButton");
        GameObject enemyGridPanelObject = FindChild(root.transform, "EnemyGridPanel");
        GameObject allyGridPanelObject = FindChild(root.transform, "AllyGridPanel");
        GameObject enemyStatusPanelObject = FindChild(root.transform, "EnemyStatusPanel");
        GameObject allyStatusPanelObject = FindChild(root.transform, "AllyStatusPanel");
        GameObject topActionPanelObject = FindChild(root.transform, "TopActionPanel");

        refs.commandPanelRoot = commandPanelObject;
        refs.commandPanel = commandPanelObject == null ? null : commandPanelObject.GetComponent<CommandPanelController>();
        refs.rotateButton = rotateButtonObject == null ? null : rotateButtonObject.GetComponent<Button>();

        refs.enemyGridPanel = enemyGridPanelObject;
        refs.allyGridPanel = allyGridPanelObject;
        refs.enemyStatusPanelRoot = enemyStatusPanelObject;
        refs.allyStatusPanelRoot = allyStatusPanelObject;
        refs.enemyStatusPanel = enemyStatusPanelObject == null ? null : enemyStatusPanelObject.transform;
        refs.allyStatusPanel = allyStatusPanelObject == null ? null : allyStatusPanelObject.transform;

        refs.bossNamePlate = FindChild(root.transform, "BossNamePlate");
        refs.topActionPanel = topActionPanelObject;
        refs.actionSkillName = FindText(topActionPanelObject, "SkillName");
        refs.actionUserName = FindText(topActionPanelObject, "UserName");

        refs.enemyFrontTop = FindText(enemyGridPanelObject, "Enemy_FrontTop/Name");
        refs.enemyBackTop = FindText(enemyGridPanelObject, "Enemy_BackTop/Name");
        refs.enemyFrontBottom = FindText(enemyGridPanelObject, "Enemy_FrontBottom/Name");
        refs.enemyBackBottom = FindText(enemyGridPanelObject, "Enemy_BackBottom/Name");

        refs.allyFrontTop = FindText(allyGridPanelObject, "Ally_FrontTop/Name");
        refs.allyBackTop = FindText(allyGridPanelObject, "Ally_BackTop/Name");
        refs.allyFrontBottom = FindText(allyGridPanelObject, "Ally_FrontBottom/Name");
        refs.allyBackBottom = FindText(allyGridPanelObject, "Ally_BackBottom/Name");

        EditorUtility.SetDirty(refs);
    }

    private static GameObject FindChild(Transform parent, string path)
    {
        if (parent == null || string.IsNullOrEmpty(path))
        {
            return null;
        }

        Transform child = parent.Find(path);
        return child == null ? null : child.gameObject;
    }

    private static TMP_Text FindText(GameObject root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path))
        {
            return null;
        }

        Transform target = root.transform.Find(path);
        return target == null ? null : target.GetComponent<TMP_Text>();
    }

'@ `
    -Label "AssignBattleUIReferences"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUICreator to generate BattleScreenRoot and BattleUIReferences."
