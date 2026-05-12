using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BattleUICreator
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private static readonly string[] GeneratedRootNames =
    {
        "TopActionPanel",
        "CommandPanel",
        "EnemyGridPanel",
        "AllyGridPanel",
        "AllyStatusPanel",
        "TurnOrderBar",
        "BossNamePlate",
        "RotateButton"
    };

    [MenuItem("Tools/Create Battle UI")]
    public static void CreateBattleUI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas was not found in the scene. Please create a Canvas first.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Create Battle UI");

        ClearExistingGeneratedUI(canvas.transform);
        CreateMainPanels(canvas.transform);
        Debug.Log("Battle UI generated. Tools > Create Battle UI");
    }

    private static void ClearExistingGeneratedUI(Transform canvas)
    {
        foreach (string rootName in GeneratedRootNames)
        {
            Transform existing = canvas.Find(rootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }
    }

    private static void CreateMainPanels(Transform canvas)
    {
        CreatePanel(canvas, "TopActionPanel", new Vector2(960, 1000), new Vector2(720, 90), new Color(0.06f, 0.08f, 0.12f, 0.88f));
        GameObject topAction = canvas.Find("TopActionPanel").gameObject;
        CreateLabel(topAction.transform, "SkillName", "Skill Name", 30, new Vector2(0, 18), new Vector2(680, 32));
        CreateLabel(topAction.transform, "UserName", "User Name", 24, new Vector2(0, -18), new Vector2(680, 30));

        GameObject commandPanel = CreatePanel(canvas, "CommandPanel", new Vector2(320, 690), new Vector2(560, 640), new Color(0.08f, 0.11f, 0.16f, 0.9f));
        CreateCommandPanelChildren(commandPanel.transform);

        GameObject enemyGridPanel = CreatePanel(canvas, "EnemyGridPanel", new Vector2(790, 540), new Vector2(580, 420), new Color(0.36f, 0.11f, 0.11f, 0.75f));
        CreateGridCells(enemyGridPanel.transform, "Enemy", new Vector2(250, 160));

        GameObject allyGridPanel = CreatePanel(canvas, "AllyGridPanel", new Vector2(1330, 540), new Vector2(580, 420), new Color(0.12f, 0.24f, 0.4f, 0.75f));
        CreateGridCells(allyGridPanel.transform, "Ally", new Vector2(250, 160));
        CreateRotateButton(canvas, allyGridPanel.GetComponent<RectTransform>());

        GameObject allyStatusPanel = CreatePanel(canvas, "AllyStatusPanel", new Vector2(1710, 590), new Vector2(360, 520), new Color(0.07f, 0.09f, 0.14f, 0.9f));
        CreateAllyStatusSlots(allyStatusPanel.transform);

        GameObject turnOrderBar = CreatePanel(canvas, "TurnOrderBar", new Vector2(960, 90), new Vector2(1800, 140), new Color(0.09f, 0.11f, 0.16f, 0.92f));
        CreateTurnSlots(turnOrderBar.transform);

        CreatePanel(canvas, "BossNamePlate", new Vector2(960, 1045), new Vector2(760, 50), new Color(0.28f, 0.08f, 0.08f, 0.9f));
    }

    private static void CreateCommandPanelChildren(Transform parent)
    {
        GameObject mainCommands = CreateChildPanel(parent, "MainCommandButtons", new Vector2(0, 210), new Vector2(500, 190), new Color(0.13f, 0.18f, 0.24f, 0.9f));
        CreateButton(mainCommands.transform, "FightButton", "Fight", new Vector2(0, 60), new Vector2(440, 46));
        CreateButton(mainCommands.transform, "SwapButton", "Swap", new Vector2(0, 0), new Vector2(440, 46));
        CreateButton(mainCommands.transform, "ItemButton", "Item", new Vector2(0, -60), new Vector2(440, 46));

        GameObject skillList = CreateChildPanel(parent, "SkillList", new Vector2(0, -20), new Vector2(500, 260), new Color(0.11f, 0.15f, 0.21f, 0.9f));
        CreateButton(skillList.transform, "Skill1", "Skill 1", new Vector2(0, 90), new Vector2(450, 44));
        CreateButton(skillList.transform, "Skill2", "Skill 2", new Vector2(0, 30), new Vector2(450, 44));
        CreateButton(skillList.transform, "Skill3", "Skill 3", new Vector2(0, -30), new Vector2(450, 44));
        CreateButton(skillList.transform, "Skill4", "Skill 4", new Vector2(0, -90), new Vector2(450, 44));

        GameObject descriptionArea = CreateChildPanel(parent, "DescriptionArea", new Vector2(0, -245), new Vector2(500, 140), new Color(0.1f, 0.14f, 0.2f, 0.9f));
        CreateLabel(descriptionArea.transform, "DescriptionText", "Select a command or skill.", 22, Vector2.zero, new Vector2(460, 110));
    }

    private static void CreateGridCells(Transform parent, string prefix, Vector2 cellSize)
    {
        CreateCell(parent, $"{prefix}_FrontTop", new Vector2(-130, 90), cellSize);
        CreateCell(parent, $"{prefix}_BackTop", new Vector2(130, 90), cellSize);
        CreateCell(parent, $"{prefix}_FrontBottom", new Vector2(-130, -90), cellSize);
        CreateCell(parent, $"{prefix}_BackBottom", new Vector2(130, -90), cellSize);
    }

    private static void CreateAllyStatusSlots(Transform parent)
    {
        for (int i = 0; i < 4; i++)
        {
            float y = 170f - i * 112f;
            GameObject slot = CreateChildPanel(parent, $"AllyStatus_{i + 1}", new Vector2(0, y), new Vector2(320, 92), new Color(0.16f, 0.2f, 0.26f, 0.88f));
            CreateLabel(slot.transform, "HP", "HP: 100/100", 18, new Vector2(0, 20), new Vector2(280, 24));
            CreateLabel(slot.transform, "MP", "MP: 30/30", 18, new Vector2(0, -20), new Vector2(280, 24));
        }
    }

    private static void CreateTurnSlots(Transform parent)
    {
        for (int i = 0; i < 8; i++)
        {
            float x = -777f + i * 222f;
            GameObject slot = CreateChildPanel(parent, $"TurnSlot_{i + 1}", new Vector2(x, 0), new Vector2(196, 94), new Color(0.2f, 0.24f, 0.3f, 0.92f));
            CreateLabel(slot.transform, "Label", $"Turn {i + 1}", 18, Vector2.zero, new Vector2(176, 40));
        }
    }

    private static void CreateRotateButton(Transform canvas, RectTransform allyGridRect)
    {
        Vector2 pos = allyGridRect.anchoredPosition + new Vector2(0, -250);
        CreateButton(canvas, "RotateButton", "Rotate", pos, new Vector2(180, 50));
    }

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
        GameObject cell = CreateChildPanel(parent, name, pos, size, new Color(1f, 1f, 1f, 0.18f));
        CreateLabel(cell.transform, "Name", name, 18, Vector2.zero, new Vector2(size.x - 10f, 36));
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
        img.color = new Color(0.93f, 0.95f, 0.98f, 0.98f);

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
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.08f, 0.11f, 0.16f);

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
        tmp.color = Color.white;

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
