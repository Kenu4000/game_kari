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
        public Action<ItemData> OnItemClicked;

        private BattleUnit _activeUnit;
        private List<BattleUnit> _reserves;

        private int _hoveredSkillIndex = -1;

        private readonly ItemData _dummyPotion = CreateDummyPotion();

        private void Awake()
        {
            HookRootButtons();
            BindFixedItemButtons();
        }

        private void Start()
        {
            ClearDescription();
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
            if (_hoveredSkillIndex >= 0)
            {
                RefreshHoveredSkillDescription();
            }
            else
            {
                ClearDescription();
            }
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
            _hoveredSkillIndex = -1;
            ClearDescription();
            OnHoverExit?.Invoke();

            SetPanelStates(true, false, true, false);
        }

        public void ShowItems()
        {
            _hoveredSkillIndex = -1;
            ClearDescription();
            OnHoverExit?.Invoke();

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

                // MP不足でもhover説明を出したいので、ここでは無効化しない。
                // 実際に使えるかどうかは BattleUIManager.HandleSkillClicked() 側で判定する。
                button.interactable = true;

                string label = BuildSkillButtonLabel(skill);
                SetButtonLabel(button, label);

                button.onClick.AddListener(() => OnSkillClicked?.Invoke(skill));

                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<EventTrigger>();
                }

                int skillIndex = i;

                AddHoverEvent(trigger, EventTriggerType.PointerEnter, () =>
                {
                    _hoveredSkillIndex = skillIndex;

                    if (descriptionText != null)
                    {
                        descriptionText.text = BuildSkillDescription(skill);
                    }

                    OnSkillHovered?.Invoke(skill);
                });

                AddHoverEvent(trigger, EventTriggerType.PointerExit, () =>
                {
                    if (_hoveredSkillIndex == skillIndex)
                    {
                        _hoveredSkillIndex = -1;
                    }

                    OnHoverExit?.Invoke();
                });
            }
        }


        private string BuildSkillDescription(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            string description = string.IsNullOrWhiteSpace(skill.Description)
                ? "-"
                : skill.Description;

            string mpText = skill.MpCost > 0
                ? $"MP Cost: {skill.MpCost}"
                : "MP Cost: 0";

            string damageText = $"Damage: {skill.Damage}";
            string effectText = BuildSkillEffectDescription(skill);
            string cooldownText = BuildSkillCooldownDescription(skill);

            bool hasEnoughMp = _activeUnit != null && _activeUnit.CurrentMP >= skill.MpCost;
            int currentMp = _activeUnit == null ? 0 : _activeUnit.CurrentMP;

            var lines = new List<string>
            {
                description,
                mpText,
                damageText
            };

            if (!string.IsNullOrEmpty(effectText))
            {
                lines.Add(effectText);
            }

            if (!string.IsNullOrEmpty(cooldownText))
            {
                lines.Add(cooldownText);
            }

            if (!hasEnoughMp)
            {
                lines.Add($"Not enough MP. Current MP: {currentMp}");
            }

            return string.Join(System.Environment.NewLine, lines);
        }

        private string BuildSkillEffectDescription(SkillData skill)
        {
            if (skill == null || skill.EffectType == SkillEffectType.None)
            {
                return string.Empty;
            }

            switch (skill.EffectType)
            {
                case SkillEffectType.ApplyBuff:
                    return $"Effect: {skill.BuffType} {skill.BuffTurns} turns";

                default:
                    return string.Empty;
            }
        }

        private string BuildSkillCooldownDescription(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            int skillCooldown = GetSkillCooldownRemaining(skill);
            if (skillCooldown > 0)
            {
                return $"Cooldown: WAIT {skillCooldown}";
            }

            if (IsLinkSkillBlocked(skill))
            {
                return $"LinkCooldown: {GetLinkCooldownRemaining()}";
            }

            return string.Empty;
        }

        private string BuildSkillButtonLabel(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            string label = skill.MpCost > 0
                ? $"{skill.SkillName} MP:{skill.MpCost}"
                : skill.SkillName;

            int skillCooldown = GetSkillCooldownRemaining(skill);
            if (skillCooldown > 0)
            {
                label += $" WAIT:{skillCooldown}";
            }
            else if (IsLinkSkillBlocked(skill))
            {
                label += $" LINK:{GetLinkCooldownRemaining()}";
            }

            bool hasEnoughMp = _activeUnit != null && _activeUnit.CurrentMP >= skill.MpCost;
            if (!hasEnoughMp)
            {
                label += " ×";
            }

            return label;
        }

        private int GetSkillCooldownRemaining(SkillData skill)
        {
            return GetSkillCooldownRemaining(_activeUnit, skill);
        }

        private int GetSkillCooldownRemaining(BattleUnit unit, SkillData skill)
        {
            SkillCooldownState state = FindSkillCooldownState(unit, skill);
            return state == null ? 0 : Mathf.Max(0, state.RemainingTurns);
        }

        private SkillCooldownState FindSkillCooldownState(BattleUnit unit, SkillData skill)
        {
            if (unit == null || skill == null || unit.SkillCooldowns == null)
            {
                return null;
            }

            string key = GetSkillCooldownKey(skill);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            for (int i = 0; i < unit.SkillCooldowns.Count; i++)
            {
                SkillCooldownState state = unit.SkillCooldowns[i];
                if (state != null && state.SkillId == key)
                {
                    return state;
                }
            }

            return null;
        }

        private static string GetSkillCooldownKey(SkillData skill)
        {
            return skill == null ? string.Empty : skill.SkillId ?? string.Empty;
        }

        private int GetLinkCooldownRemaining()
        {
            if (_activeUnit == null)
            {
                return 0;
            }

            return Mathf.Max(0, _activeUnit.LinkCooldownRemaining);
        }

        private bool IsLinkSkillBlocked(SkillData skill)
        {
            if (skill == null || _activeUnit == null)
            {
                return false;
            }

            return skill.SkillKind == SkillKind.Link && GetLinkCooldownRemaining() > 0;
        }

        private string BuildItemDescription(ItemData item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            string description = string.IsNullOrWhiteSpace(item.Description)
                ? "-"
                : item.Description;

            string countText = $"Count: {item.Count}";

            if (item.Count > 0)
            {
                return $"{description}\nHeal: {item.HealAmount}\n{countText}";
            }

            return $"{description}\nHeal: {item.HealAmount}\n{countText}\nNo items left.";
        }

        private static ItemData CreateDummyPotion()
        {
            return new ItemData
            {
                ItemId = "potion",
                ItemName = "Potion",
                Description = "Heal the ally in front of the active unit.",
                HealAmount = 20,
                Count = 3
            };
        }

        private void ClearDescription()
        {
            if (descriptionText != null)
            {
                descriptionText.text = "";
            }
        }

        private void RefreshHoveredSkillDescription()
        {
            if (_hoveredSkillIndex < 0)
            {
                return;
            }

            SkillData hoveredSkill = GetSkillAt(_hoveredSkillIndex);
            if (hoveredSkill == null)
            {
                return;
            }

            if (descriptionText != null)
            {
                descriptionText.text = BuildSkillDescription(hoveredSkill);
            }

            OnSkillHovered?.Invoke(hoveredSkill);
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

                    if (_dummyPotion.Count <= 0)
                    {
                        button.interactable = false;
                        SetButtonLabel(button, "-");
                        RemoveHoverEvents(button.gameObject);
                        continue;
                    }

                    button.interactable = true;
                    SetButtonLabel(button, $"{_dummyPotion.ItemName} HP:{_dummyPotion.HealAmount} x{_dummyPotion.Count}");
                    button.onClick.AddListener(() => OnItemClicked?.Invoke(_dummyPotion));

                    RemoveHoverEvents(button.gameObject);

                    EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                    if (trigger == null)
                    {
                        trigger = button.gameObject.AddComponent<EventTrigger>();
                    }

                    AddHoverEvent(trigger, EventTriggerType.PointerEnter, () =>
                    {
                        if (descriptionText != null)
                        {
                            descriptionText.text = BuildItemDescription(_dummyPotion);
                        }
                    });

                    AddHoverEvent(trigger, EventTriggerType.PointerExit, () =>
                    {
                        ClearDescription();
                    });
                }
                else
                {
                    button.gameObject.SetActive(true);
                    button.interactable = false;
                    SetButtonLabel(button, "-");
                }
            }
        }

        public void RefreshItems()
        {
            BindFixedItemButtons();
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



