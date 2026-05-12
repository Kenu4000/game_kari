using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameKari.Battle
{
    public enum CommandViewMode
    {
        Main,
        Skills,
        Swap,
        Items
    }

    public class CommandPanelController : MonoBehaviour
    {
        [Header("Root Buttons")]
        [SerializeField] private Button fightButton;
        [SerializeField] private Button swapButton;
        [SerializeField] private Button itemButton;

        [Header("Panel References")]
        [SerializeField] private GameObject mainCommandButtons;
        [SerializeField] private GameObject skillListPanel;
        [SerializeField] private GameObject swapListPanel;
        [SerializeField] private GameObject itemListPanel;

        [Header("Runtime Content")]
        [SerializeField] private Transform skillListRoot;
        [SerializeField] private Transform sideListRoot;
        [SerializeField] private Button simpleButtonPrefab;
        [SerializeField] private TMP_Text descriptionText;

        public Action<SkillData> OnSkillClicked;
        public Action<SkillData> OnSkillHovered;
        public Action OnHoverExit;
        public Action<BattleUnit> OnReserveClicked;
        public Action<string> OnItemClicked;

        private BattleUnit _activeUnit;
        private List<BattleUnit> _reserves;
        private CommandViewMode _mode;

        public void Setup(BattleUnit activeUnit, List<BattleUnit> reserves)
        {
            _activeUnit = activeUnit;
            _reserves = reserves;
            HookRootButtons();
            ShowMain();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                ShowMain();
            }
        }

        public void ShowMain()
        {
            _mode = CommandViewMode.Main;
            SetPanelStates(true, false, false, false);
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);
        }

        public void ShowSkills()
        {
            OpenSkills();
        }

        public void ShowSwap()
        {
            OpenSwap();
        }

        public void ShowItems()
        {
            OpenItems();
        }

        private void HookRootButtons()
        {
            fightButton.onClick.RemoveAllListeners();
            swapButton.onClick.RemoveAllListeners();
            itemButton.onClick.RemoveAllListeners();

            fightButton.onClick.AddListener(ShowSkills);
            swapButton.onClick.AddListener(ShowSwap);
            itemButton.onClick.AddListener(ShowItems);
        }

        public void OpenSkills()
        {
            _mode = CommandViewMode.Skills;
            SetPanelStates(true, true, false, false);
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);

            if (_activeUnit == null || _activeUnit.Skills == null) return;

            foreach (SkillData skill in _activeUnit.Skills)
            {
                Button btn = Instantiate(simpleButtonPrefab, skillListRoot);
                btn.GetComponentInChildren<TMP_Text>().text = skill.SkillName;
                btn.onClick.AddListener(() => OnSkillClicked?.Invoke(skill));

                EventTrigger trigger = btn.gameObject.AddComponent<EventTrigger>();
                AddHoverEvent(trigger, EventTriggerType.PointerEnter, () =>
                {
                    if (descriptionText != null)
                    {
                        descriptionText.text = skill.Description;
                    }
                    OnSkillHovered?.Invoke(skill);
                });
                AddHoverEvent(trigger, EventTriggerType.PointerExit, () => OnHoverExit?.Invoke());
            }
        }

        private void OpenSwap()
        {
            _mode = CommandViewMode.Swap;
            SetPanelStates(true, false, true, false);
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);

            if (_reserves == null) return;

            foreach (BattleUnit reserve in _reserves)
            {
                Button btn = Instantiate(simpleButtonPrefab, sideListRoot);
                btn.GetComponentInChildren<TMP_Text>().text = $"{reserve.Name} HP:{reserve.CurrentHP} MP:{reserve.CurrentMP}";
                btn.onClick.AddListener(() => OnReserveClicked?.Invoke(reserve));
            }
            if (descriptionText != null)
            {
                descriptionText.text = "控えを選択して即交代（行動権消費なし）";
            }
        }

        private void OpenItems()
        {
            _mode = CommandViewMode.Items;
            SetPanelStates(true, false, false, true);
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);

            Button btn = Instantiate(simpleButtonPrefab, sideListRoot);
            btn.GetComponentInChildren<TMP_Text>().text = "Potion (仮)";
            btn.onClick.AddListener(() => OnItemClicked?.Invoke("Potion"));
            if (descriptionText != null)
            {
                descriptionText.text = "前方マスの味方に回復アイテムを使用";
            }
        }

        public void SetInteractable(bool interactable)
        {
            fightButton.interactable = interactable;
            swapButton.interactable = interactable;
            itemButton.interactable = interactable;
            foreach (Button btn in skillListRoot.GetComponentsInChildren<Button>()) btn.interactable = interactable;
            foreach (Button btn in sideListRoot.GetComponentsInChildren<Button>()) btn.interactable = interactable;
        }

        private void SetPanelStates(bool showMain, bool showSkills, bool showSwap, bool showItems)
        {
            SetActive(mainCommandButtons, showMain);
            SetActive(skillListPanel, showSkills);
            SetActive(swapListPanel, showSwap);
            SetActive(itemListPanel, showItems);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        }

        private static void AddHoverEvent(EventTrigger trigger, EventTriggerType type, Action callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
        }
    }
}
