using GameKari.Battle;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BattleUIReferenceBinder
{
    [MenuItem("Tools/Battle UI/Bind References On Selected Root")]
    public static void BindReferencesOnSelectedRoot()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogError("Select the existing battle UI root GameObject in the Hierarchy first.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Bind Battle UI References");

        BattleUIReferences refs = root.GetComponent<BattleUIReferences>();
        if (refs == null)
        {
            refs = Undo.AddComponent<BattleUIReferences>(root);
        }

        AssignRootReferences(root, refs);
        AssignGridReferences(root, refs);
        AssignTopActionReferences(root, refs);
        AssignStatusReferences(root, refs);
        AssignControlReferences(root, refs);

        EditorUtility.SetDirty(refs);
        Debug.Log($"Battle UI references bound on {root.name}. Check Inspector for missing fields.");
    }


    private static void AssignRootReferences(GameObject root, BattleUIReferences refs)
    {
        refs.bossNamePlate = FindGameObject(root, "BossNamePlate");
        refs.topActionPanel = FindGameObject(root, "TopActionPanel");
        refs.enemyGridPanel = FindGameObject(root, "EnemyGridPanel");
        refs.allyGridPanel = FindGameObject(root, "AllyGridPanel");
        refs.commandPanelRoot = FindGameObject(root, "CommandPanel");
        refs.enemyStatusPanelRoot = FindGameObject(root, "EnemyStatusPanel");
        refs.allyStatusPanelRoot = FindGameObject(root, "AllyStatusPanel");
    }

    private static void AssignGridReferences(GameObject root, BattleUIReferences refs)
    {
        refs.enemyFrontTop = FindTextByCandidates(root, "Enemy_FrontTop/Name", "EnemyFrontTop", "enemyFrontTop", "Enemy Front Top");
        refs.enemyBackTop = FindTextByCandidates(root, "Enemy_BackTop/Name", "EnemyBackTop", "enemyBackTop", "Enemy Back Top");
        refs.enemyFrontBottom = FindTextByCandidates(root, "Enemy_FrontBottom/Name", "EnemyFrontBottom", "enemyFrontBottom", "Enemy Front Bottom");
        refs.enemyBackBottom = FindTextByCandidates(root, "Enemy_BackBottom/Name", "EnemyBackBottom", "enemyBackBottom", "Enemy Back Bottom");

        refs.allyFrontTop = FindTextByCandidates(root, "Ally_FrontTop/Name", "AllyFrontTop", "allyFrontTop", "Ally Front Top");
        refs.allyBackTop = FindTextByCandidates(root, "Ally_BackTop/Name", "AllyBackTop", "allyBackTop", "Ally Back Top");
        refs.allyFrontBottom = FindTextByCandidates(root, "Ally_FrontBottom/Name", "AllyFrontBottom", "allyFrontBottom", "Ally Front Bottom");
        refs.allyBackBottom = FindTextByCandidates(root, "Ally_BackBottom/Name", "AllyBackBottom", "allyBackBottom", "Ally Back Bottom");
    }

    private static void AssignTopActionReferences(GameObject root, BattleUIReferences refs)
    {
        refs.actionSkillName = FindTextByCandidates(root, "TopActionPanel/SkillName", "SkillName", "ActionSkillName", "actionSkillName");
        refs.actionUserName = FindTextByCandidates(root, "TopActionPanel/UserName", "UserName", "ActionUserName", "actionUserName");
    }

    private static void AssignStatusReferences(GameObject root, BattleUIReferences refs)
    {
        refs.enemyStatusPanel = refs.enemyStatusPanelRoot == null ? FindTransform(root, "EnemyStatusPanel") : refs.enemyStatusPanelRoot.transform;
        refs.allyStatusPanel = refs.allyStatusPanelRoot == null ? FindTransform(root, "AllyStatusPanel") : refs.allyStatusPanelRoot.transform;
    }

    private static void AssignControlReferences(GameObject root, BattleUIReferences refs)
    {
        GameObject commandPanelObject = refs.commandPanelRoot == null ? FindGameObject(root, "CommandPanel") : refs.commandPanelRoot;
        refs.commandPanel = commandPanelObject == null ? null : commandPanelObject.GetComponent<CommandPanelController>();

        GameObject rotateObject = FindGameObject(root, "RotateButton");
        refs.rotateButton = rotateObject == null ? null : rotateObject.GetComponent<Button>();

        refs.enemyFTHighlight = FindImageByCandidates(root, "EnemyFTHighlight", "EnemyFrontTopHighlight", "enemyFTHighlight");
        refs.enemyFBHighlight = FindImageByCandidates(root, "EnemyFBHighlight", "EnemyFrontBottomHighlight", "enemyFBHighlight");
    }

    private static GameObject FindGameObject(GameObject root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == name)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static Transform FindTransform(GameObject root, string name)
    {
        GameObject found = FindGameObject(root, name);
        return found == null ? null : found.transform;
    }

    private static TMP_Text FindTextByCandidates(GameObject root, params string[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            TMP_Text byPath = FindTextByPath(root, candidates[i]);
            if (byPath != null)
            {
                return byPath;
            }

            GameObject byName = FindGameObject(root, candidates[i]);
            if (byName != null)
            {
                TMP_Text text = byName.GetComponent<TMP_Text>();
                if (text != null)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static TMP_Text FindTextByPath(GameObject root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path))
        {
            return null;
        }

        Transform found = root.transform.Find(path);
        return found == null ? null : found.GetComponent<TMP_Text>();
    }

    private static Image FindImageByCandidates(GameObject root, params string[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject byName = FindGameObject(root, candidates[i]);
            if (byName != null)
            {
                Image image = byName.GetComponent<Image>();
                if (image != null)
                {
                    return image;
                }
            }
        }

        return null;
    }
}

