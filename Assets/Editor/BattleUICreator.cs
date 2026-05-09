using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BattleUICreator
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    [MenuItem("Tools/Create Battle UI")]
    public static void CreateBattleUI()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas was not found. Please create a Canvas first.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Create Battle UI");
        CreateMainPanels(canvas.transform);
        Debug.Log("Battle UI created from Tools > Create Battle UI");
    }

    private static void CreateMainPanels(Transform canvas)
    {
        GameObject bossNamePlate = CreatePanel(canvas, "BossNamePlate", new Vector2(960, 1040), new Vector2(700, 52), PanelColor());
        CreateLabel(bossNamePlate.transform, "BossNameText", "Boss Name", 28, Vector2.zero, new Vector2(660, 42));

        GameObject topAction = CreatePanel(canvas, "TopActionPanel", new Vector2(960, 975), new Vector2(640, 88), PanelColor());
        CreateLabel(topAction.transform, "SkillName", "Skill Name", 28, new Vector2(0, 16), new Vector2(600, 28));
        CreateLabel(topAction.transform, "UserName", "User Name", 22, new Vector2(0, -16), new Vector2(600, 28));

        GameObject commandPanel = CreatePanel(canvas, "CommandPanel", new Vector2(290, 760), new Vector2(560, 560), PanelColor());
        CreateCommandPanelChildren(commandPanel.transform);

        GameObject enemyGridPanel = CreatePanel(canvas, "EnemyGridPanel", new Vector2(710, 520), new Vector2(520, 440), PanelColor());
        CreateGridCells(enemyGridPanel.transform, "Enemy", new Vector2(230, 185));

        GameObject allyGridPanel = CreatePanel(canvas, "AllyGridPanel", new Vector2(1260, 520), new Vector2(520, 440), PanelColor());
        CreateGridCells(allyGridPanel.transform, "Ally", new Vector2(230, 185));
        CreateRotateButton(canvas, allyGridPanel.GetComponent<RectTransform>());

        GameObject allyStatusPanel = CreatePanel(canvas, "AllyStatusPanel", new Vector2(1710, 560), new Vector2(320, 520), PanelColor());
        CreateAllyStatusSlots(allyStatusPanel.transform);

        GameObject turnOrderBar = CreatePanel(canvas, "TurnOrderBar", new Vector2(960, 90), new Vector2(1840, 150), PanelColor());
        CreateTurnSlots(turnOrderBar.transform);
    }

    private static void CreateCommandPanelChildren(Transform parent)
    {
        GameObject mainCommandButtons = CreateChildPanel(parent, "MainCommandButtons", new Vector2(-170, 0), new Vector2(170, 500), ChildPanelColor());
        CreateButton(mainCommandButtons.transform, "FightButton", "Fight", new Vector2(0, 160), new Vector2(140, 70));
        CreateButton(mainCommandButtons.transform, "SwapButton", "Swap", new Vector2(0, 60), new Vector2(140, 70));
        CreateButton(mainCommandButtons.transform, "ItemButton", "Item", new Vector2(0, -40), new Vector2(140, 70));

        GameObject skillList = CreateChildPanel(parent, "SkillList", new Vector2(90, 130), new Vector2(320, 240), ChildPanelColor());
        CreateButton(skillList.transform, "Skill1", "Skill 1", new Vector2(0, 78), new Vector2(280, 44));
        CreateButton(skillList.transform, "Skill2", "Skill 2", new Vector2(0, 26), new Vector2(280, 44));
        CreateButton(skillList.transform, "Skill3", "Skill 3", new Vector2(0, -26), new Vector2(280, 44));
        CreateButton(skillList.transform, "Skill4", "Skill 4", new Vector2(0, -78), new Vector2(280, 44));

        GameObject descriptionArea = CreateChildPanel(parent, "DescriptionArea", new Vector2(90, -150), new Vector2(320, 200), ChildPanelColor());
        CreateLabel(descriptionArea.transform, "DescriptionText", "Description", 22, Vector2.zero, new Vector2(290, 160));
    }

    private static void CreateGridCells(Transform parent, string prefix, Vector2 cellSize)
    {
        CreateCell(parent, $"{prefix}_FrontTop", new Vector2(-125, 100), cellSize);
        CreateCell(parent, $"{prefix}_BackTop", new Vector2(125, 100), cellSize);
        CreateCell(parent, $"{prefix}_FrontBottom", new Vector2(-125, -100), cellSize);
        CreateCell(parent, $"{prefix}_BackBottom", new Vector2(125, -100), cellSize);
    }

    private static void CreateAllyStatusSlots(Transform parent)
    {
        for (int i = 0; i < 4; i++)
        {
            float y = 170f - i * 110f;
            GameObject slot = CreateChildPanel(parent, $"AllyStatus_{i + 1}", new Vector2(0, y), new Vector2(282, 92), ChildPanelColor());
            CreateLabel(slot.transform, "HP", "HP: 100 / 100", 19, new Vector2(0, 18), new Vector2(250, 24));
            CreateLabel(slot.transform, "MP", "MP: 30 / 30", 19, new Vector2(0, -18), new Vector2(250, 24));
        }
    }

    private static void CreateTurnSlots(Transform parent)
    {
        for (int i = 0; i < 8; i++)
        {
            float x = -780f + i * 223f;
            GameObject slot = CreateChildPanel(parent, $"TurnSlot_{i + 1}", new Vector2(x, 0), new Vector2(198, 105), ChildPanelColor());
            CreateLabel(slot.transform, "Label", $"Slot {i + 1}", 18, Vector2.zero, new Vector2(178, 44));
        }
    }

    private static void CreateRotateButton(Transform canvas, RectTransform allyGridRect)
    {
        Vector2 pos = allyGridRect.anchoredPosition + new Vector2(0, -260);
        CreateButton(canvas, "RotateButton", "Rotate", pos, new Vector2(140, 58));
    }

    private static Color PanelColor() => new Color(0.83f, 0.85f, 0.88f, 0.95f);
    private static Color ChildPanelColor() => new Color(0.90f, 0.92f, 0.95f, 0.98f);

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
        tmp.fontSize = 24;
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
