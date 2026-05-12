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

        private void Awake()
        {
            HookRootButtons();
        }

        private void Start()
        {
            ShowSkills();
        }

        public void Setup(BattleUnit activeUnit, List<BattleUnit> reserves)
        {
            _activeUnit = activeUnit;
            _reserves = reserves;
            ShowSkills();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                ShowSkills();
            }
        }

        public void ShowMain()
        {
            ShowSkills();
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
            if (fightButton != null)
            {
                fightButton.onClick.RemoveListener(ShowSkills);
                fightButton.onClick.AddListener(ShowSkills);
            }

            if (swapButton != null)
            {
                swapButton.onClick.RemoveListener(ShowSwap);
                swapButton.onClick.AddListener(ShowSwap);
            }

            if (itemButton != null)
            {
                itemButton.onClick.RemoveListener(ShowItems);
                itemButton.onClick.AddListener(ShowItems);
            }
        }

        public void OpenSkills()
        {
            _mode = CommandViewMode.Skills;
            SetPanelStates(true, true, false, false);
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);

            if (_activeUnit == null || _activeUnit.Skills == null || skillListRoot == null || simpleButtonPrefab == null)
            {
                return;
            }

            foreach (SkillData skill in _activeUnit.Skills)
            {
                Button btn = Instantiate(simpleButtonPrefab, skillListRoot);

                TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    btnText.text = skill.SkillName;
                }

                btn.onClick.AddListener(() => OnSkillClicked?.Invoke(skill));

                EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = btn.gameObject.AddComponent<EventTrigger>();
                }

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

            if (descriptionText != null)
            {
                descriptionText.text = "控えを選択して即交代（行動権消費なし）";
            }

            if (_reserves == null || sideListRoot == null || simpleButtonPrefab == null)
            {
                return;
            }

            foreach (BattleUnit reserve in _reserves)
            {
                Button btn = Instantiate(simpleButtonPrefab, sideListRoot);

                TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    btnText.text = $"{reserve.Name} HP:{reserve.CurrentHP} MP:{reserve.CurrentMP}";
                }

                btn.onClick.AddListener(() => OnReserveClicked?.Invoke(reserve));
            }
        }

        private void OpenItems()
        {
            _mode = CommandViewMode.Items;
            SetPanelStates(true, false, false, true);
            ClearChildren(skillListRoot);
            ClearChildren(sideListRoot);

            if (descriptionText != null)
            {
                descriptionText.text = "前方マスの味方に回復アイテムを使用";
            }

            if (sideListRoot == null || simpleButtonPrefab == null)
            {
                return;
            }

            Button btn = Instantiate(simpleButtonPrefab, sideListRoot);

            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = "Potion (仮)";
            }

            btn.onClick.AddListener(() => OnItemClicked?.Invoke("Potion"));
        }

        public void SetInteractable(bool interactable)
        {
            if (fightButton != null)
            {
                fightButton.interactable = interactable;
            }

            if (swapButton != null)
            {
                swapButton.interactable = interactable;
            }

            if (itemButton != null)
            {
                itemButton.interactable = interactable;
            }

            if (skillListRoot != null)
            {
                foreach (Button btn in skillListRoot.GetComponentsInChildren<Button>())
                {
                    btn.interactable = interactable;
                }
            }

            if (sideListRoot != null)
            {
                foreach (Button btn in sideListRoot.GetComponentsInChildren<Button>())
                {
                    btn.interactable = interactable;
                }
            }
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
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private static void AddHoverEvent(EventTrigger trigger, EventTriggerType type, Action callback)
        {
            if (trigger == null || callback == null)
            {
                return;
            }

            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
        }
    }
}