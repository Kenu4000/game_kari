using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameKari.Battle
{

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

        [Header("Fixed Skill Buttons")]
        [SerializeField] private Button[] skillButtons = new Button[4];

        [Header("Fixed Swap Buttons")]
        [SerializeField] private Button[] swapButtons = new Button[4];

        [Header("Fixed Item Buttons")]
        [SerializeField] private Button[] itemButtons = new Button[1];

        [Header("Description")]
        [SerializeField] private TMP_Text descriptionText;

        public Action<SkillData> OnSkillClicked;
        public Action<SkillData> OnSkillHovered;
        public Action OnHoverExit;
        public Action<BattleUnit> OnReserveClicked;
        public Action<string> OnItemClicked;

        private BattleUnit _activeUnit;
        private List<BattleUnit> _reserves;

        private void Awake()
        {
            HookRootButtons();
            BindFixedItemButtons();
        }

        private void Start()
        {
            ShowSkills();
        }

        public void Setup(BattleUnit activeUnit, List<BattleUnit> reserves)
        {
            _activeUnit = activeUnit;
            _reserves = reserves;

            BindSkillButtons();
            BindSwapButtons();
            BindFixedItemButtons();

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
            SetPanelStates(true, true, false, false);
        }

        public void ShowSwap()
        {
            SetPanelStates(true, false, true, false);
        }

        public void ShowItems()
        {
            SetPanelStates(true, false, false, true);
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

        private void BindSkillButtons()
        {
            if (skillButtons == null)
            {
                return;
            }

            for (int i = 0; i < skillButtons.Length; i++)
            {
                Button button = skillButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                RemoveHoverEvents(button.gameObject);

                SkillData skill = GetSkillAt(i);
                if (skill == null)
                {
                    SetButtonLabel(button, $"Skill {i + 1}");
                    button.interactable = false;
                    button.gameObject.SetActive(true);
                    continue;
                }

                button.gameObject.SetActive(true);

                bool hasEnoughMp = _activeUnit != null && _activeUnit.CurrentMP >= skill.MpCost;
                button.interactable = hasEnoughMp;

                string label = skill.MpCost > 0
                    ? $"{skill.SkillName} MP:{skill.MpCost}"
                    : skill.SkillName;

                SetButtonLabel(button, label);

                button.onClick.AddListener(() =>
                {
                    if (_activeUnit == null || _activeUnit.CurrentMP < skill.MpCost)
                    {
                        return;
                    }

                    OnSkillClicked?.Invoke(skill);
                });

                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<EventTrigger>();
                }

                AddHoverEvent(trigger, EventTriggerType.PointerEnter, () =>
                {
                    if (descriptionText != null)
                    {
                        descriptionText.text = skill.Description;
                    }

                    OnSkillHovered?.Invoke(skill);
                });

                AddHoverEvent(trigger, EventTriggerType.PointerExit, () =>
                {
                    OnHoverExit?.Invoke();
                });
            }
        }

        private void BindSwapButtons()
        {
            if (swapButtons == null)
            {
                return;
            }

            for (int i = 0; i < swapButtons.Length; i++)
            {
                Button button = swapButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();

                BattleUnit reserve = GetReserveAt(i);
                if (reserve == null)
                {
                    SetButtonLabel(button, "-");
                    button.interactable = false;
                    button.gameObject.SetActive(true);
                    continue;
                }

                button.gameObject.SetActive(true);
                button.interactable = true;
                SetButtonLabel(button, $"{reserve.Name} HP:{reserve.CurrentHP} MP:{reserve.CurrentMP}");

                button.onClick.AddListener(() => OnReserveClicked?.Invoke(reserve));
            }
        }

        private void BindFixedItemButtons()
        {
            if (itemButtons == null)
            {
                return;
            }

            for (int i = 0; i < itemButtons.Length; i++)
            {
                Button button = itemButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();

                if (i == 0)
                {
                    button.gameObject.SetActive(true);
                    button.interactable = true;
                    SetButtonLabel(button, "Potion");
                    button.onClick.AddListener(() => OnItemClicked?.Invoke("Potion"));
                }
                else
                {
                    button.gameObject.SetActive(true);
                    button.interactable = false;
                    SetButtonLabel(button, "-");
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            SetButtonInteractable(fightButton, interactable);
            SetButtonInteractable(swapButton, interactable);
            SetButtonInteractable(itemButton, interactable);

            SetButtonArrayInteractable(skillButtons, interactable);
            SetButtonArrayInteractable(swapButtons, interactable);
            SetButtonArrayInteractable(itemButtons, interactable);
        }

        private SkillData GetSkillAt(int index)
        {
            if (_activeUnit == null || _activeUnit.Skills == null)
            {
                return null;
            }

            if (index < 0 || index >= _activeUnit.Skills.Count)
            {
                return null;
            }

            return _activeUnit.Skills[index];
        }

        private BattleUnit GetReserveAt(int index)
        {
            if (_reserves == null)
            {
                return null;
            }

            if (index < 0 || index >= _reserves.Count)
            {
                return null;
            }

            return _reserves[index];
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

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static void SetButtonArrayInteractable(Button[] buttons, bool interactable)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (Button button in buttons)
            {
                SetButtonInteractable(button, interactable);
            }
        }

        private static void RemoveHoverEvents(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
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