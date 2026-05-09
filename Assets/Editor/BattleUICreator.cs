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
            Debug.LogError("Canvas がシーンに見つかりません。先に Canvas を作成してください。");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Create Battle UI");

        CreateMainPanels(canvas.transform);
        Debug.Log("Battle UI を生成しました。Tools > Create Battle UI");
    }

    private static void CreateMainPanels(Transform canvas)
    {
        GameObject topAction = CreatePanel(canvas, "TopActionPanel", new Vector2(960, 985), new Vector2(500, 90), new Color(0f, 0f, 0f, 0.4f));
        CreateLabel(topAction.transform, "SkillName", "SkillName", 30, new Vector2(0, 18), new Vector2(460, 30));
        CreateLabel(topAction.transform, "UserName", "UserName", 24, new Vector2(0, -18), new Vector2(460, 30));

        GameObject commandPanel = CreatePanel(canvas, "CommandPanel", new Vector2(250, 780), new Vector2(440, 520), new Color(0f, 0f, 0f, 0.55f));
        CreateCommandPanelChildren(commandPanel.transform);

        GameObject enemyGridPanel = CreatePanel(canvas, "EnemyGridPanel", new Vector2(660, 560), new Vector2(420, 360), new Color(0.35f, 0.1f, 0.1f, 0.5f));
        CreateGridCells(enemyGridPanel.transform, "Enemy", new Vector2(180, 140));

        GameObject allyGridPanel = CreatePanel(canvas, "AllyGridPanel", new Vector2(1250, 560), new Vector2(420, 360), new Color(0.1f, 0.2f, 0.35f, 0.5f));
        CreateGridCells(allyGridPanel.transform, "Ally", new Vector2(180, 140));
        CreateRotateButton(canvas, allyGridPanel.GetComponent<RectTransform>());

        GameObject allyStatusPanel = CreatePanel(canvas, "AllyStatusPanel", new Vector2(1710, 610), new Vector2(340, 460), new Color(0f, 0f, 0f, 0.55f));
        CreateAllyStatusSlots(allyStatusPanel.transform);

        GameObject turnOrderBar = CreatePanel(canvas, "TurnOrderBar", new Vector2(960, 105), new Vector2(1820, 140), new Color(0f, 0f, 0f, 0.55f));
        CreateTurnSlots(turnOrderBar.transform);

        CreatePanel(canvas, "BossNamePlate", new Vector2(960, 1040), new Vector2(700, 56), new Color(0.2f, 0.05f, 0.05f, 0.75f));
    }

    private static void CreateCommandPanelChildren(Transform parent)
    {
        GameObject leftColumn = CreateChildPanel(parent, "LeftCommandColumn", new Vector2(70, 0), new Vector2(120, 470), new Color(0f, 0f, 0f, 0.3f));
        CreateButton(leftColumn.transform, "FightButton", "戦う", new Vector2(0, 130), new Vector2(100, 60));
        CreateButton(leftColumn.transform, "SwapButton", "交代", new Vector2(0, 40), new Vector2(100, 60));
        CreateButton(leftColumn.transform, "ItemButton", "アイテム", new Vector2(0, -50), new Vector2(100, 60));

        GameObject skillArea = CreateChildPanel(parent, "SkillListArea", new Vector2(230, 90), new Vector2(280, 260), new Color(0f, 0f, 0f, 0.25f));
        CreateButton(skillArea.transform, "Skill1", "わざ1", new Vector2(0, 90), new Vector2(240, 44));
        CreateButton(skillArea.transform, "Skill2", "わざ2", new Vector2(0, 30), new Vector2(240, 44));
        CreateButton(skillArea.transform, "Skill3", "わざ3", new Vector2(0, -30), new Vector2(240, 44));
        CreateButton(skillArea.transform, "Skill4", "わざ4", new Vector2(0, -90), new Vector2(240, 44));

        GameObject description = CreateChildPanel(parent, "DescriptionPanel", new Vector2(230, -150), new Vector2(280, 170), new Color(0f, 0f, 0f, 0.35f));
        CreateLabel(description.transform, "DescriptionText", "説明欄", 22, Vector2.zero, new Vector2(250, 130));
    }

    private static void CreateGridCells(Transform parent, string prefix, Vector2 cellSize)
    {
        CreateCell(parent, $"{prefix}_FrontTop", new Vector2(-100, 75), cellSize);
        CreateCell(parent, $"{prefix}_BackTop", new Vector2(100, 75), cellSize);
        CreateCell(parent, $"{prefix}_FrontBottom", new Vector2(-100, -75), cellSize);
        CreateCell(parent, $"{prefix}_BackBottom", new Vector2(100, -75), cellSize);
    }

    private static void CreateAllyStatusSlots(Transform parent)
    {
        for (int i = 0; i < 4; i++)
        {
            float y = 150f - i * 100f;
            GameObject slot = CreateChildPanel(parent, $"AllyStatus_{i + 1}", new Vector2(0, y), new Vector2(300, 80), new Color(0.1f, 0.1f, 0.1f, 0.45f));
            CreateLabel(slot.transform, "HP", "HP: 100/100", 18, new Vector2(0, 16), new Vector2(260, 24));
            CreateLabel(slot.transform, "MP", "MP: 30/30", 18, new Vector2(0, -16), new Vector2(260, 24));
        }
    }

    private static void CreateTurnSlots(Transform parent)
    {
        for (int i = 0; i < 8; i++)
        {
            float x = -770f + i * 220f;
            GameObject slot = CreateChildPanel(parent, $"TurnSlot_{i + 1}", new Vector2(x, 0), new Vector2(190, 96), new Color(0.2f, 0.2f, 0.2f, 0.6f));
            CreateLabel(slot.transform, "Label", $"Slot {i + 1}", 18, Vector2.zero, new Vector2(170, 40));
        }
    }

    private static void CreateRotateButton(Transform canvas, RectTransform allyGridRect)
    {
        Vector2 pos = allyGridRect.anchoredPosition + new Vector2(250, -140);
        CreateButton(canvas, "RotateButton", "回転", pos, new Vector2(120, 56));
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
        GameObject cell = CreateChildPanel(parent, name, pos, size, new Color(1f, 1f, 1f, 0.15f));
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
        img.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);

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
        tmp.color = Color.black;

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
