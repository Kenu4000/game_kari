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
            OpenSkills();
        }

        private void HookRootButtons()
        {
            fightButton.onClick.RemoveAllListeners();
            swapButton.onClick.RemoveAllListeners();
            itemButton.onClick.RemoveAllListeners();

            fightButton.onClick.AddListener(OpenSkills);
            swapButton.onClick.AddListener(OpenSwap);
            itemButton.onClick.AddListener(OpenItems);
        }

        public void OpenSkills()
        {
            _mode = CommandViewMode.Skills;
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);

            foreach (SkillData skill in _activeUnit.Skills)
            {
                Button btn = Instantiate(simpleButtonPrefab, skillListRoot);
                btn.GetComponentInChildren<TMP_Text>().text = skill.SkillName;
                btn.onClick.AddListener(() => OnSkillClicked?.Invoke(skill));

                EventTrigger trigger = btn.gameObject.AddComponent<EventTrigger>();
                AddHoverEvent(trigger, EventTriggerType.PointerEnter, () =>
                {
                    descriptionText.text = skill.Description;
                    OnSkillHovered?.Invoke(skill);
                });
                AddHoverEvent(trigger, EventTriggerType.PointerExit, () => OnHoverExit?.Invoke());
            }
        }

        private void OpenSwap()
        {
            _mode = CommandViewMode.Swap;
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);

            foreach (BattleUnit reserve in _reserves)
            {
                Button btn = Instantiate(simpleButtonPrefab, sideListRoot);
                btn.GetComponentInChildren<TMP_Text>().text = $"{reserve.Name} HP:{reserve.CurrentHP} MP:{reserve.CurrentMP}";
                btn.onClick.AddListener(() => OnReserveClicked?.Invoke(reserve));
            }
            descriptionText.text = "控えを選択して即交代（行動権消費なし）";
        }

        private void OpenItems()
        {
            _mode = CommandViewMode.Items;
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);

            Button btn = Instantiate(simpleButtonPrefab, sideListRoot);
            btn.GetComponentInChildren<TMP_Text>().text = "Potion (仮)";
            btn.onClick.AddListener(() => OnItemClicked?.Invoke("Potion"));
            descriptionText.text = "前方マスの味方に回復アイテムを使用";
        }

        public void SetInteractable(bool interactable)
        {
            fightButton.interactable = interactable;
            swapButton.interactable = interactable;
            itemButton.interactable = interactable;
            foreach (Button btn in skillListRoot.GetComponentsInChildren<Button>()) btn.interactable = interactable;
            foreach (Button btn in sideListRoot.GetComponentsInChildren<Button>()) btn.interactable = interactable;
        }

        private static void ClearChildren(Transform root)
        {
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
