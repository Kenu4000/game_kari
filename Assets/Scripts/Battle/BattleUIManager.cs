using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    public class BattleUIManager : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private CommandPanelController commandPanel;
        [SerializeField] private Button rotateButton;

        [Header("Battlefield Labels")]
        [SerializeField] private TMP_Text enemyFrontTop;
        [SerializeField] private TMP_Text enemyBackTop;
        [SerializeField] private TMP_Text enemyFrontBottom;
        [SerializeField] private TMP_Text enemyBackBottom;

        [SerializeField] private TMP_Text allyFrontTop;
        [SerializeField] private TMP_Text allyBackTop;
        [SerializeField] private TMP_Text allyFrontBottom;
        [SerializeField] private TMP_Text allyBackBottom;

        [Header("Top Action Overlay")]
        [SerializeField] private TMP_Text actionSkillName;
        [SerializeField] private TMP_Text actionUserName;

        [Header("Preview")]
        [SerializeField] private Image enemyFTHighlight;
        [SerializeField] private Image enemyFBHighlight;

        [Header("Status Panels")]
        [SerializeField] private Transform enemyStatusPanel;
        [SerializeField] private Transform allyStatusPanel;

        private BattleGrid _grid;
        private FormationController _formation;
        private TurnOrderManager _turnOrder;

        private readonly List<BattleUnit> _allies = new();
        private readonly List<BattleUnit> _enemies = new();
        private readonly List<BattleUnit> _reserves = new();

        private readonly Dictionary<BattleUnit, int> _turnNumbers = new();
        private readonly HashSet<BattleUnit> _actedUnits = new();
        private BattleUnit _active;

        private void Start()
        {
            BootstrapDummyBattle();
            BindUI();
            RedrawBoard();
        }

        private void BootstrapDummyBattle()
        {
            _grid = new BattleGrid();
            _formation = new FormationController(_grid);
            _turnOrder = new TurnOrderManager();

            BattleUnit heroA = CreateUnit("Knight", 130, 20, 12);
            BattleUnit heroB = CreateUnit("Mage", 80, 60, 15);
            BattleUnit heroC = CreateUnit("Cleric", 90, 50, 9);
            BattleUnit heroD = CreateUnit("Rogue", 95, 25, 18);
            BattleUnit reserve = CreateUnit("Reserve", 100, 40, 11);

            _grid.SetUnit(true, GridPos.FrontTop, heroA);
            _grid.SetUnit(true, GridPos.BackTop, heroB);
            _grid.SetUnit(true, GridPos.FrontBottom, heroC);
            _grid.SetUnit(true, GridPos.BackBottom, heroD);

            _allies.Clear();
            _enemies.Clear();
            _reserves.Clear();

            _allies.Add(heroA);
            _allies.Add(heroB);
            _allies.Add(heroC);
            _allies.Add(heroD);

            _reserves.Add(reserve);

            BattleUnit enemyA = CreateUnit("Goblin A", 70, 0, 10);
            BattleUnit enemyB = CreateUnit("Archer", 60, 0, 13);
            BattleUnit enemyC = CreateUnit("Goblin B", 70, 0, 8);
            BattleUnit enemyD = CreateUnit("Shaman", 55, 20, 7);

            _enemies.Add(enemyA);
            _enemies.Add(enemyB);
            _enemies.Add(enemyC);
            _enemies.Add(enemyD);

            _grid.SetUnit(false, GridPos.FrontTop, enemyA);
            _grid.SetUnit(false, GridPos.BackTop, enemyB);
            _grid.SetUnit(false, GridPos.FrontBottom, enemyC);
            _grid.SetUnit(false, GridPos.BackBottom, enemyD);

        RebuildTurnOrder();
        _active = FindNextUnactedAlly() ?? heroA;
        }

        private void BindUI()
        {
            commandPanel.Setup(_active, _reserves);
            commandPanel.OnSkillClicked += HandleSkillClicked;
            commandPanel.OnSkillHovered += HandleSkillHover;
            commandPanel.OnHoverExit += ClearTargetPreview;
            commandPanel.OnReserveClicked += HandleSwap;
            commandPanel.OnItemClicked += HandleItemClicked;
            rotateButton.onClick.AddListener(HandleRotateClicked);
        }

        private void HandleSkillClicked(SkillData skill)
        {
            ShowActionOverlay(skill.SkillName, _active.Name);
            Debug.Log($"[Action] Skill used (dummy): {skill.SkillName} by {_active.Name}");

            MarkActiveAsActed();
            AdvanceToNextActor();
        }

        private void HandleSkillHover(SkillData skill)
        {
            ClearTargetPreview();
            if (skill.TargetPattern is SkillTargetPattern.FrontTopEnemy or SkillTargetPattern.BothFrontEnemies or SkillTargetPattern.AllEnemies)
                enemyFTHighlight.enabled = true;
            if (skill.TargetPattern is SkillTargetPattern.FrontBottomEnemy or SkillTargetPattern.BothFrontEnemies or SkillTargetPattern.AllEnemies)
                enemyFBHighlight.enabled = true;
        }

        private void ClearTargetPreview()
        {
            enemyFTHighlight.enabled = false;
            enemyFBHighlight.enabled = false;
        }

        private void HandleSwap(BattleUnit reserve)
        {
            BattleUnit previousActive = _active;

            _formation.SwapActiveWithReserve(previousActive, reserve);
            TransferTurnNumber(previousActive, reserve);
            TransferActedState(previousActive, reserve);

            int allyIndex = _allies.IndexOf(previousActive);
            if (allyIndex >= 0)
            {
                _allies[allyIndex] = reserve;
            }

            _reserves.Remove(reserve);
            _reserves.Add(previousActive);

            _active = reserve;

            commandPanel.Setup(_active, _reserves);
            RedrawBoard();

            Debug.Log("[Action] Swapped active unit with reserve (no action consumption). ");
        }

        private void TransferTurnNumber(BattleUnit from, BattleUnit to)
        {
            if (from == null || to == null)
            {
                return;
            }

            if (_turnNumbers.TryGetValue(from, out int number))
            {
                _turnNumbers.Remove(from);
                _turnNumbers[to] = number;
            }
        }

        private void TransferActedState(BattleUnit from, BattleUnit to)
        {
            if (from == null || to == null)
            {
                return;
            }

            if (_actedUnits.Contains(from))
            {
                _actedUnits.Remove(from);
                _actedUnits.Add(to);
            }
        }

        private void HandleItemClicked(string itemId)
        {
            BattleUnit target = TryGetForwardAlly(_active);
            if (target == null)
            {
                Debug.Log("[Item] No forward ally target. Item cannot be used.");
                return;
            }

        target.CurrentHP = Mathf.Min(target.CurrentHP + 20, target.Data.MaxHP);
        ShowActionOverlay(itemId, _active.Name);
        Debug.Log($"[Action] Item used (dummy): {itemId} -> {target.Name}");

        MarkActiveAsActed();
        RedrawBoard();
        AdvanceToNextActor();
        }

        private void MarkActiveAsActed()
        {
            if (_active == null)
            {
                return;
            }

            _actedUnits.Add(_active);
            RedrawStatusPanels();
        }

        private void AdvanceToNextActor()
        {
            BattleUnit nextAlly = FindNextUnactedAlly();
            if (nextAlly != null)
            {
                _active = nextAlly;
                commandPanel.Setup(_active, _reserves);
                RedrawBoard();
                Debug.Log($"[Turn] Next active ally: {_active.Name}");
                return;
            }

            if (ProcessEnemyTurnsUntilNextAlly())
            {
                return;
            }

            StartNextTurn();
        }

        private BattleUnit FindNextUnactedAlly()
        {
            if (_turnOrder == null)
            {
                return null;
            }

            IReadOnlyList<BattleUnit> order = _turnOrder.TurnOrder;
            for (int i = 0; i < order.Count; i++)
            {
                BattleUnit unit = order[i];

                if (unit == null || unit.IsDead || _actedUnits.Contains(unit))
                {
                    continue;
                }

                if (_allies.Contains(unit))
                {
                    return unit;
                }
            }

            return null;
        }

        private bool ProcessEnemyTurnsUntilNextAlly()
        {
            if (_turnOrder == null)
            {
                return false;
            }

            IReadOnlyList<BattleUnit> order = _turnOrder.TurnOrder;
            for (int i = 0; i < order.Count; i++)
            {
                BattleUnit unit = order[i];

                if (unit == null || unit.IsDead || _actedUnits.Contains(unit))
                {
                    continue;
                }

                if (_enemies.Contains(unit))
                {
                    _actedUnits.Add(unit);
                    Debug.Log($"[Enemy] Dummy enemy action: {unit.Name}");
                    RedrawStatusPanels();

                    BattleUnit nextAlly = FindNextUnactedAlly();
                    if (nextAlly != null)
                    {
                        _active = nextAlly;
                        commandPanel.Setup(_active, _reserves);
                        RedrawBoard();
                        Debug.Log($"[Turn] Next active ally: {_active.Name}");
                        return true;
                    }
                }
            }

            return false;
        }

        private void StartNextTurn()
        {
            RebuildTurnOrder();

            BattleUnit nextAlly = FindNextUnactedAlly();
            if (nextAlly != null)
            {
                _active = nextAlly;
                commandPanel.Setup(_active, _reserves);
            }

            RedrawBoard();
            Debug.Log("[Turn] New turn started.");
        }

        private BattleUnit TryGetForwardAlly(BattleUnit user)
        {
            return user.GridPos switch
            {
                GridPos.BackTop => _grid.GetUnit(true, GridPos.FrontTop),
                GridPos.BackBottom => _grid.GetUnit(true, GridPos.FrontBottom),
                _ => null
            };
        }

        private void HandleRotateClicked()
        {
            _formation.RotateAlliesClockwise();
            RedrawBoard();
        }

        private void ShowActionOverlay(string skillName, string userName)
        {
            actionSkillName.text = skillName;
            actionUserName.text = userName;
        }

        private void RedrawBoard()
        {
            enemyFrontTop.text = SafeName(_grid.GetUnit(false, GridPos.FrontTop));
            enemyBackTop.text = SafeName(_grid.GetUnit(false, GridPos.BackTop));
            enemyFrontBottom.text = SafeName(_grid.GetUnit(false, GridPos.FrontBottom));
            enemyBackBottom.text = SafeName(_grid.GetUnit(false, GridPos.BackBottom));

            allyFrontTop.text = SafeName(_grid.GetUnit(true, GridPos.FrontTop));
            allyBackTop.text = SafeName(_grid.GetUnit(true, GridPos.BackTop));
            allyFrontBottom.text = SafeName(_grid.GetUnit(true, GridPos.FrontBottom));
            allyBackBottom.text = SafeName(_grid.GetUnit(true, GridPos.BackBottom));

            RedrawStatusPanels();
        }

        private void RedrawStatusPanels()
        {
            for (int i = 0; i < 4; i++)
            {
                RedrawEnemyStatusSlot(i + 1, GetUnitAt(_enemies, i));
                RedrawAllyStatusSlot(i + 1, GetUnitAt(_allies, i));
            }
        }

        private void RedrawEnemyStatusSlot(int slotNumber, BattleUnit unit)
        {
            Transform slot = enemyStatusPanel == null
                ? null
                : enemyStatusPanel.Find($"EnemyStatus_{slotNumber}");

            if (slot == null)
            {
                return;
            }

            SetLabel(slot, "Name", unit == null ? "-" : unit.Name);
            SetLabel(slot, "TurnNumber", GetTurnOrderText(unit));

            int currentHp = unit == null ? 0 : unit.CurrentHP;
            int maxHp = unit == null ? 1 : unit.Data.MaxHP;
            SetBarFill(slot, "HPBar", currentHp, maxHp);
        }

        private void RedrawAllyStatusSlot(int slotNumber, BattleUnit unit)
        {
            Transform slot = allyStatusPanel == null
                ? null
                : allyStatusPanel.Find($"AllyStatus_{slotNumber}");

            if (slot == null)
            {
                return;
            }

            SetLabel(slot, "Name", unit == null ? "-" : unit.Name);
            SetLabel(slot, "TurnNumber", GetTurnOrderText(unit));

            int currentHp = unit == null ? 0 : unit.CurrentHP;
            int maxHp = unit == null ? 1 : unit.Data.MaxHP;
            int currentMp = unit == null ? 0 : unit.CurrentMP;
            int maxMp = unit == null ? 1 : unit.Data.MaxMP;

            SetBarFill(slot, "HPBar", currentHp, maxHp);
            SetBarFill(slot, "MPBar", currentMp, maxMp);
        }

        private static BattleUnit GetUnitAt(List<BattleUnit> units, int index)
        {
            if (units == null)
            {
                return null;
            }

            if (index < 0 || index >= units.Count)
            {
                return null;
            }

            return units[index];
        }

        private string GetTurnOrderText(BattleUnit unit)
        {
            if (unit == null)
            {
                return "";
            }

            if (_actedUnits.Contains(unit))
            {
                return "";
            }

            return _turnNumbers.TryGetValue(unit, out int number)
                ? number.ToString()
                : "";
        }

        private static void SetLabel(Transform root, string childName, string text)
        {
            TMP_Text label = root.Find(childName)?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void SetBarFill(Transform root, string barName, int current, int max)
        {
            Transform fill = root.Find($"{barName}/Fill");
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            fill.localScale = new Vector3(rate, 1f, 1f);
        }

        private void RebuildTurnOrder()
        {
            _actedUnits.Clear();

            var all = new List<BattleUnit>();
            all.AddRange(_grid.AllyGrid.Values);
            all.AddRange(_grid.EnemyGrid.Values);
            _turnOrder.RebuildTurnOrder(all);
            RebuildTurnNumbersFromCurrentOrder();
        }

        private void RebuildTurnNumbersFromCurrentOrder()
        {
            _turnNumbers.Clear();

            IReadOnlyList<BattleUnit> order = _turnOrder.TurnOrder;
            for (int i = 0; i < order.Count; i++)
            {
                _turnNumbers[order[i]] = i + 1;
            }
        }

        private static string SafeName(BattleUnit unit) => unit == null ? "-" : unit.Name;

        private static BattleUnit CreateUnit(string name, int hp, int mp, int speed)
        {
            var data = new CharacterData
            {
                Id = name.ToLower().Replace(" ", "_"),
                DisplayName = name,
                MaxHP = hp,
                MaxMP = mp,
                Speed = speed
            };

            var unit = new BattleUnit(data);
            unit.Skills.Add(new SkillData { SkillId = "s1", SkillName = "Slash", Description = "単体前列上を攻撃", TargetPattern = SkillTargetPattern.FrontTopEnemy });
            unit.Skills.Add(new SkillData { SkillId = "s2", SkillName = "Pierce", Description = "単体前列下を攻撃", TargetPattern = SkillTargetPattern.FrontBottomEnemy });
            unit.Skills.Add(new SkillData { SkillId = "s3", SkillName = "TwinHit", Description = "前列2体を攻撃", TargetPattern = SkillTargetPattern.BothFrontEnemies });
            unit.Skills.Add(new SkillData { SkillId = "s4", SkillName = "Wave", Description = "全体攻撃", TargetPattern = SkillTargetPattern.AllEnemies });
            return unit;
        }
    }
}
