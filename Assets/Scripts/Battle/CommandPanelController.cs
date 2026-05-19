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
        [SerializeField] private Button[] itemButtons = new Button[2];

        [Header("Description")]
        [SerializeField] private TMP_Text descriptionText;

        public Action<SkillData> OnSkillClicked;
        public Action<SkillData> OnSkillHovered;
        public Action OnHoverExit;
        public Action<BattleUnit> OnReserveClicked;
        public Action<ItemData> OnItemClicked;

        private BattleUnit _activeUnit;
        private List<BattleUnit> _reserves;
        private List<BattleUnit> _allies;

        private int _hoveredSkillIndex = -1;

        private readonly List<ItemData> _dummyItems = DummyItemCatalog.CreateDefaultItems();

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

        public void Setup(BattleUnit activeUnit, List<BattleUnit> reserves, List<BattleUnit> allies)
        {
            _activeUnit = activeUnit;
            _reserves = reserves;
            _allies = allies;

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
                    ResetButtonVisualState(button);
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
                ApplySkillButtonVisualState(button, skill);

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


        private void ApplySkillButtonVisualState(Button button, SkillData skill)
        {
            if (button == null)
            {
                return;
            }

            bool unavailable = !HasEnoughMP(skill) || !HasEnoughPartnerMP(skill) || (skill != null && skill.SkillKind == SkillKind.Link && !HasRequiredLinkPartner(skill));
            float alpha = unavailable ? 0.45f : 1f;

            SetButtonAlpha(button, alpha);
        }

        private void ResetButtonVisualState(Button button)
        {
            SetButtonAlpha(button, 1f);
        }

        private void SetButtonAlpha(Button button, float alpha)
        {
            if (button == null)
            {
                return;
            }

            Graphic targetGraphic = button.targetGraphic;
            if (targetGraphic != null)
            {
                Color color = targetGraphic.color;
                color.a = alpha;
                targetGraphic.color = color;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                Color labelColor = label.color;
                labelColor.a = alpha;
                label.color = labelColor;
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

            string skillKindText = BuildSkillKindDescription(skill);
            string damageText = $"Damage: {skill.Damage}";
            string mpText = BuildSkillMpDescription(skill);
            string effectText = BuildSkillEffectDescription(skill);
            string linkPartnerText = BuildSkillLinkPartnerDescription(skill);
            string unavailableText = BuildSkillUnavailableDescription(skill);

            var lines = new List<string>
            {
                description
            };

            if (!string.IsNullOrEmpty(skillKindText))
            {
                lines.Add(skillKindText);
            }

            lines.Add(damageText);
            lines.Add(mpText);

            if (!string.IsNullOrEmpty(effectText))
            {
                lines.Add(effectText);
            }

            if (!string.IsNullOrEmpty(linkPartnerText))
            {
                lines.Add(linkPartnerText);
            }

            if (!string.IsNullOrEmpty(unavailableText))
            {
                lines.Add(unavailableText);
            }

            return string.Join(System.Environment.NewLine, lines);
        }

        private static string BuildSkillKindDescription(SkillData skill)
        {
            if (skill == null || skill.SkillKind != SkillKind.Link)
            {
                return string.Empty;
            }

            return "[LINK]";
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
        private string BuildSkillLinkPartnerDescription(SkillData skill)
        {
            if (skill == null || skill.SkillKind != SkillKind.Link)
            {
                return string.Empty;
            }

            BattleUnit partner = GetRequiredLinkPartner(skill);
            if (partner == null)
            {
                return string.Empty;
            }

            return $"Partner: {partner.Name}";
        }

        private string BuildSkillMpDescription(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            return $"MP Cost: {Mathf.Max(0, skill.MpCost)}";
        }

        private string BuildSkillUnavailableDescription(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            if (!HasEnoughMP(skill))
            {
                return $"Not enough MP. Current: {GetCurrentMP(_activeUnit)}, Cost: {Mathf.Max(0, skill.MpCost)}";
            }

            if (skill.SkillKind == SkillKind.Link && !HasRequiredLinkPartner(skill))
            {
                return "No specified link partner.";
            }

            if (!HasEnoughPartnerMP(skill))
            {
                BattleUnit partner = GetRequiredLinkPartner(skill);
                string partnerName = partner == null ? "Partner" : partner.Name;
                int partnerMp = partner == null ? 0 : GetCurrentMP(partner);
                return $"Not enough partner MP. {partnerName}: {partnerMp}, Cost: {Mathf.Max(0, skill.MpCost)}";
            }

            return string.Empty;
        }

        private string BuildSkillButtonLabel(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            string label = $"{skill.SkillName} MP:{Mathf.Max(0, skill.MpCost)}";

            if (!HasEnoughMP(skill))
            {
                label += " NO MP";
            }
            else if (skill.SkillKind == SkillKind.Link && !HasRequiredLinkPartner(skill))
            {
                label += " NO PARTNER";
            }
            else if (!HasEnoughPartnerMP(skill))
            {
                label += " PARTNER NO MP";
            }

            return label;
        }

        private bool HasEnoughMP(SkillData skill)
        {
            if (_activeUnit == null || skill == null)
            {
                return false;
            }

            return GetCurrentMP(_activeUnit) >= Mathf.Max(0, skill.MpCost);
        }

        private static int GetCurrentMP(BattleUnit unit)
        {
            return unit == null ? 0 : Mathf.Max(0, unit.CurrentMP);
        }

        private bool HasEnoughPartnerMP(SkillData skill)
        {
            if (skill == null || skill.SkillKind != SkillKind.Link)
            {
                return true;
            }

            BattleUnit partner = GetRequiredLinkPartner(skill);
            if (partner == null)
            {
                return false;
            }

            return GetCurrentMP(partner) >= Mathf.Max(0, skill.MpCost);
        }

        private bool HasRequiredLinkPartner(SkillData skill)
        {
            if (skill == null || skill.SkillKind != SkillKind.Link)
            {
                return true;
            }

            return GetRequiredLinkPartner(skill) != null;
        }

        private BattleUnit GetRequiredLinkPartner(SkillData skill)
        {
            if (skill == null || skill.SkillKind != SkillKind.Link)
            {
                return null;
            }

            if (string.IsNullOrEmpty(skill.LinkPartnerCharacterId))
            {
                return null;
            }

            BattleUnit partner = FindUnitByCharacterId(_allies, skill.LinkPartnerCharacterId);
            if (IsValidLinkPartner(partner))
            {
                return partner;
            }

            partner = FindUnitByCharacterId(_reserves, skill.LinkPartnerCharacterId);
            if (IsValidLinkPartner(partner))
            {
                return partner;
            }

            return null;
        }

        private bool IsValidLinkPartner(BattleUnit partner)
        {
            return partner != null
                && partner != _activeUnit
                && !partner.IsDead
                && partner.Data != null;
        }

        private static BattleUnit FindUnitByCharacterId(List<BattleUnit> units, string characterId)
        {
            if (units == null || string.IsNullOrEmpty(characterId))
            {
                return null;
            }

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || unit.IsDead || unit.Data == null)
                {
                    continue;
                }

                if (unit.Data.Id == characterId)
                {
                    return unit;
                }
            }

            return null;
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

            switch (item.Kind)
            {
                case ItemKind.Pass:
                    return $"{description}\nEffect: End current action.\n{countText}";

                case ItemKind.Heal:
                default:
                    if (item.Count > 0)
                    {
                        return $"{description}\nHeal: {item.HealAmount}\n{countText}";
                    }

                    return $"{description}\nHeal: {item.HealAmount}\n{countText}\nNo items left.";
            }
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
                SetButtonLabel(button, $"{reserve.Name} HP:{reserve.CurrentHP}");

                button.onClick.AddListener(() => OnReserveClicked?.Invoke(reserve));
            }
        }

        private void BindFixedItemButtons()
        {
            EnsureItemButtonCapacity();

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
                RemoveHoverEvents(button.gameObject);

                ItemData item = GetItemAt(i);
                if (item == null)
                {
                    button.gameObject.SetActive(true);
                    button.interactable = false;
                    SetButtonLabel(button, "-");
                    continue;
                }

                button.gameObject.SetActive(true);

                if (item.Count <= 0)
                {
                    button.interactable = false;
                    SetButtonLabel(button, "-");
                    continue;
                }

                button.interactable = true;
                SetButtonLabel(button, BuildItemButtonLabel(item));

                button.onClick.AddListener(() => OnItemClicked?.Invoke(item));

                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<EventTrigger>();
                }

                AddHoverEvent(trigger, EventTriggerType.PointerEnter, () =>
                {
                    if (descriptionText != null)
                    {
                        descriptionText.text = BuildItemDescription(item);
                    }
                });

                AddHoverEvent(trigger, EventTriggerType.PointerExit, () =>
                {
                    ClearDescription();
                });
            }
        }

        private void EnsureItemButtonCapacity()
        {
            if (itemButtons == null)
            {
                itemButtons = new Button[0];
            }

            int requiredCount = _dummyItems == null ? 0 : _dummyItems.Count;
            if (requiredCount <= 0)
            {
                return;
            }

            Button[] expanded = new Button[requiredCount];

            for (int i = 0; i < itemButtons.Length && i < expanded.Length; i++)
            {
                expanded[i] = itemButtons[i];
            }

            Button template = FindItemButtonTemplate(expanded);
            Transform parent = itemListPanel != null
                ? itemListPanel.transform
                : template == null ? null : template.transform.parent;

            if (parent == null)
            {
                itemButtons = expanded;
                return;
            }

            for (int i = 0; i < requiredCount; i++)
            {
                if (expanded[i] != null)
                {
                    expanded[i].transform.SetParent(parent, false);
                    PositionItemButton(expanded[i], i);
                    continue;
                }

                expanded[i] = template != null
                    ? CreateItemButtonFromTemplate(template, parent, i)
                    : CreateItemButton(parent, i);
            }

            itemButtons = expanded;
        }

        private static Button FindItemButtonTemplate(Button[] buttons)
        {
            if (buttons == null)
            {
                return null;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static Button CreateItemButtonFromTemplate(Button template, Transform parent, int index)
        {
            if (template == null || parent == null)
            {
                return null;
            }

            GameObject clone = Instantiate(template.gameObject, parent);
            clone.name = $"ItemButton_{index + 1}";

            Button button = clone.GetComponent<Button>();
            PositionItemButton(button, index);

            return button;
        }

        private static Button CreateItemButton(Transform parent, int index)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject buttonObject = new GameObject($"ItemButton_{index + 1}");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 36f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            Button button = buttonObject.AddComponent<Button>();

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20f;
            label.raycastTarget = false;

            PositionItemButton(button, index);

            return button;
        }

        private static void PositionItemButton(Button button, int index)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -44f * index);
        }

        private ItemData GetItemAt(int index)
        {
            if (_dummyItems == null)
            {
                return null;
            }

            if (index < 0 || index >= _dummyItems.Count)
            {
                return null;
            }

            return _dummyItems[index];
        }

        private static string BuildItemButtonLabel(ItemData item)
        {
            if (item == null)
            {
                return "-";
            }

            switch (item.Kind)
            {
                case ItemKind.Pass:
                    return $"{item.ItemName} x{item.Count}";

                case ItemKind.Heal:
                default:
                    return $"{item.ItemName} HP:{item.HealAmount} x{item.Count}";
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


















