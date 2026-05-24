using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameKari.Battle;

public static class BattleUICreator
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

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
        if (canvas == null)
        {
            Debug.LogError("Canvas was not found. Please create a Canvas first.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Create Battle UI");
        RemoveExistingBattleUI(canvas.transform);
        GameObject battleScreenRoot = CreateBattleScreenRoot(canvas.transform);
        CreateMainPanels(battleScreenRoot.transform);
        AssignBattleUIReferences(battleScreenRoot);
        Debug.Log("Battle UI template created from Tools > Battle UI > Legacy > Create Battle UI Template.");
    }

    private static readonly string[] GeneratedRootNames =
    {
        "BattleScreenRoot",
        "TopActionPanel",
        "CommandPanel",
        "EnemyGridPanel",
        "AllyGridPanel",
        "EnemyStatusPanel",
        "AllyStatusPanel",
        "BossNamePlate",
        "RotateButton"
    };

    private static void RemoveExistingBattleUI(Transform canvas)
    {
        foreach (string rootName in GeneratedRootNames)
        {
            Transform child = canvas.Find(rootName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

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
    private static void CreateMainPanels(Transform canvas)
    {
        GameObject bossNamePlate = CreatePanel(canvas, "BossNamePlate", new Vector2(960, 1035), new Vector2(640, 58), PanelColor());
        CreateLabel(bossNamePlate.transform, "BossNameText", "Boss Name", 28, Vector2.zero, new Vector2(600, 40));

        GameObject topAction = CreatePanel(canvas, "TopActionPanel", new Vector2(960, 965), new Vector2(700, 90), PanelColor());
        CreateLabel(topAction.transform, "SkillName", "Skill Name", 28, new Vector2(0, 16), new Vector2(660, 28));
        CreateLabel(topAction.transform, "UserName", "User Name", 22, new Vector2(0, -16), new Vector2(660, 28));

        GameObject commandPanel = CreatePanel(canvas, "CommandPanel", new Vector2(960, 800), new Vector2(840, 220), PanelColor());
        EnsureComponent<CommandPanelController>(commandPanel);
        CreateCommandPanelChildren(commandPanel.transform);

        GameObject enemyGridPanel = CreatePanel(canvas, "EnemyGridPanel", new Vector2(700, 500), new Vector2(520, 420), PanelColor());
        CreateGridCells(enemyGridPanel.transform, "Enemy", new Vector2(230, 175));

        GameObject allyGridPanel = CreatePanel(canvas, "AllyGridPanel", new Vector2(1220, 500), new Vector2(520, 420), PanelColor());
        CreateGridCells(allyGridPanel.transform, "Ally", new Vector2(230, 175));

        GameObject enemyStatusPanel = CreatePanel(canvas, "EnemyStatusPanel", new Vector2(160, 540), new Vector2(280, 620), PanelColor());
        CreateEnemyStatusSlots(enemyStatusPanel.transform);

        GameObject allyStatusPanel = CreatePanel(canvas, "AllyStatusPanel", new Vector2(1760, 540), new Vector2(280, 620), PanelColor());
        CreateAllyStatusSlots(allyStatusPanel.transform);

        CreateRotateButton(canvas, allyGridPanel.GetComponent<RectTransform>());
    }

    private static void CreateCommandPanelChildren(Transform parent)
    {
        GameObject mainCommandButtons = CreateChildPanel(parent, "MainCommandButtons", new Vector2(-290, 0), new Vector2(180, 190), ChildPanelColor());
        CreateButton(mainCommandButtons.transform, "FightButton", "Fight", new Vector2(0, 52), new Vector2(150, 44));
        CreateButton(mainCommandButtons.transform, "SwapButton", "Swap", new Vector2(0, 0), new Vector2(150, 44));
        CreateButton(mainCommandButtons.transform, "ItemButton", "Item", new Vector2(0, -52), new Vector2(150, 44));

        GameObject skillListPanel = CreateChildPanel(parent, "SkillListPanel", new Vector2(-60, 0), new Vector2(210, 190), ChildPanelColor());
        CreateButton(skillListPanel.transform, "Skill1", "Skill 1", new Vector2(0, 60), new Vector2(180, 36));
        CreateButton(skillListPanel.transform, "Skill2", "Skill 2", new Vector2(0, 20), new Vector2(180, 36));
        CreateButton(skillListPanel.transform, "Skill3", "Skill 3", new Vector2(0, -20), new Vector2(180, 36));
        CreateButton(skillListPanel.transform, "Skill4", "Skill 4", new Vector2(0, -60), new Vector2(180, 36));

        GameObject swapListPanel = CreateChildPanel(parent, "SwapListPanel", new Vector2(170, 40), new Vector2(220, 110), ChildPanelColor());
        CreateLabel(swapListPanel.transform, "ReserveListPlaceholder", "Reserve List", 20, Vector2.zero, new Vector2(200, 80));

        GameObject itemListPanel = CreateChildPanel(parent, "ItemListPanel", new Vector2(170, -60), new Vector2(220, 110), ChildPanelColor());
        CreateLabel(itemListPanel.transform, "ItemListPlaceholder", "Item List", 20, Vector2.zero, new Vector2(200, 80));
    }

    private static void CreateGridCells(Transform parent, string prefix, Vector2 cellSize)
    {
        CreateCell(parent, $"{prefix}_FrontTop", new Vector2(-125, 95), cellSize);
        CreateCell(parent, $"{prefix}_BackTop", new Vector2(125, 95), cellSize);
        CreateCell(parent, $"{prefix}_FrontBottom", new Vector2(-125, -95), cellSize);
        CreateCell(parent, $"{prefix}_BackBottom", new Vector2(125, -95), cellSize);
    }

    private static void CreateEnemyStatusSlots(Transform parent)
    {
        for (int i = 0; i < 4; i++)
        {
            float y = 230f - i * 150f;
            GameObject slot = CreateChildPanel(parent, $"EnemyStatus_{i + 1}", new Vector2(0, y), new Vector2(248, 130), ChildPanelColor());
            CreateLabel(slot.transform, "TurnNumber", (i + 1).ToString(), 20, new Vector2(-102, 42), new Vector2(40, 30));
            CreateIcon(slot.transform, "Icon", new Vector2(-62, 32), new Vector2(42, 42), new Color(0.86f, 0.68f, 0.68f, 1f));
            CreateLabel(slot.transform, "Name", $"Enemy {i + 1}", 19, new Vector2(32, 34), new Vector2(120, 28));
            CreateBar(slot.transform, "HPBar", new Vector2(0, -2), new Vector2(220, 20), new Color(0.75f, 0.2f, 0.2f, 1f));
            CreateChildPanel(slot.transform, "BuffIconArea", new Vector2(0, -42), new Vector2(220, 30), new Color(0.84f, 0.88f, 0.92f, 1f));
        }
    }

    private static void CreateAllyStatusSlots(Transform parent)
    {
        for (int i = 0; i < 4; i++)
        {
            float y = 230f - i * 150f;
            GameObject slot = CreateChildPanel(parent, $"AllyStatus_{i + 1}", new Vector2(0, y), new Vector2(248, 130), ChildPanelColor());
            CreateLabel(slot.transform, "TurnNumber", (i + 1).ToString(), 20, new Vector2(-102, 42), new Vector2(40, 30));
            CreateIcon(slot.transform, "FaceIcon", new Vector2(-62, 32), new Vector2(42, 42), new Color(0.67f, 0.78f, 0.9f, 1f));
            CreateLabel(slot.transform, "Name", $"Ally {i + 1}", 19, new Vector2(32, 34), new Vector2(120, 28));
            CreateLabel(slot.transform, "HPLabel", "HP", 16, new Vector2(-90, 6), new Vector2(30, 20));
            CreateBar(slot.transform, "HPBar", new Vector2(20, 6), new Vector2(160, 18), new Color(0.28f, 0.75f, 0.38f, 1f));
            CreateLabel(slot.transform, "MPLabel", "MP", 16, new Vector2(-90, -18), new Vector2(30, 20));
            CreateBar(slot.transform, "MPBar", new Vector2(20, -18), new Vector2(160, 18), new Color(0.25f, 0.52f, 0.86f, 1f));
            CreateChildPanel(slot.transform, "BuffIconArea", new Vector2(0, -46), new Vector2(220, 24), new Color(0.84f, 0.88f, 0.92f, 1f));
        }
    }

    private static void CreateRotateButton(Transform canvas, RectTransform allyGridRect)
    {
        Vector2 pos = allyGridRect.anchoredPosition + new Vector2(0, -248);
        CreateButton(canvas, "RotateButton", "Rotate", pos, new Vector2(140, 54));
    }

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
    private static Color PanelColor() => new Color(0.83f, 0.85f, 0.88f, 0.98f);
    private static Color ChildPanelColor() => new Color(0.9f, 0.93f, 0.96f, 1f);

    private static GameObject CreatePanel(Transform parent, string name, Vector2 centerPos, Vector2 size, Color color)
    {
        GameObject panel = GetOrCreate(parent, name);
        RectTransform rt = EnsureRectTransform(panel);
        ConfigureCenterRect(rt, centerPos, size);

        Image img = EnsureComponent<Image>(panel);
        img.color = color;
        return panel;
    }

    private static GameObject CreateChildPanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject panel = GetOrCreate(parent, name);
        RectTransform rt = EnsureRectTransform(panel);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = EnsureComponent<Image>(panel);
        img.color = color;
        return panel;
    }

    private static void CreateCell(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject cell = CreateChildPanel(parent, name, pos, size, new Color(0.95f, 0.96f, 0.98f, 1f));
        CreateLabel(cell.transform, "Name", name, 18, Vector2.zero, new Vector2(size.x - 14f, 36));
    }

    private static void CreateIcon(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject icon = GetOrCreate(parent, name);
        RectTransform rt = EnsureRectTransform(icon);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = EnsureComponent<Image>(icon);
        img.color = color;
    }

    private static void CreateBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor)
    {
        GameObject root = CreateChildPanel(parent, name, anchoredPos, size, new Color(0.78f, 0.8f, 0.83f, 1f));
        GameObject fill = CreateChildPanel(root.transform, "Fill", Vector2.zero, new Vector2(size.x - 4f, size.y - 4f), fillColor);
        fill.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
        fill.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
        fill.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
        fill.GetComponent<RectTransform>().anchoredPosition = new Vector2(2f, 0f);
    }

    private static GameObject CreateButton(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = GetOrCreate(parent, name);
        RectTransform rt = EnsureRectTransform(go);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = EnsureComponent<Image>(go);
        img.color = new Color(0.97f, 0.97f, 0.98f, 1f);

        Button btn = EnsureComponent<Button>(go);
        btn.targetGraphic = img;

        GameObject textObj = GetOrCreate(go.transform, "Text");
        RectTransform textRt = EnsureRectTransform(textObj);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TMP_Text tmp = EnsureComponent<TextMeshProUGUI>(textObj);
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.16f, 0.18f, 0.2f, 1f);

        return go;
    }

    private static GameObject CreateLabel(Transform parent, string name, string text, int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = GetOrCreate(parent, name);
        RectTransform rt = EnsureRectTransform(go);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TMP_Text tmp = EnsureComponent<TextMeshProUGUI>(go);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.12f, 0.13f, 0.15f, 1f);

        return go;
    }

    private static GameObject GetOrCreate(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        return rt;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    private static void ConfigureCenterRect(RectTransform rt, Vector2 centerPos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(centerPos.x - ReferenceWidth * 0.5f, centerPos.y - ReferenceHeight * 0.5f);
        rt.sizeDelta = size;
    }
}


