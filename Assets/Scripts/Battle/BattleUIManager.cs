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

        private readonly List<BattleUnit> _enemyReserves = new();

        private readonly Dictionary<BattleUnit, int> _turnNumbers = new();
        private readonly HashSet<BattleUnit> _actedUnits = new();
        private BattleUnit _active;
        private SkillData _hoveredSkill;
        private bool _battleEnded;
        [SerializeField] private float rotationSettleSeconds = 0.5f;

        private bool _formationSettling;
        private float _lastRotateTime;

        private static readonly Color NormalStatusColor = new Color(0.9f, 0.93f, 0.96f, 1f);
        private static readonly Color ActiveStatusColor = new Color(0.7f, 0.85f, 1f, 1f);
        private static readonly Color NormalCellColor = new Color(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color ActiveCellColor = new Color(0.7f, 0.88f, 1f, 1f);
        private static readonly Color TargetPreviewCellColor = new Color(1f, 0.92f, 0.55f, 1f);

        private const float EnemyStatusPanelVerticalPadding = 24f;
        private const float EnemyStatusSlotHeight = 135f;
        private const float EnemyStatusSlotSpacing = 16f;
        private const float EnemyStatusSlotWidth = 240f;

        private void Start()
        {
            BootstrapDummyBattle();
            BindUI();
            RedrawBoard();
        }

        private void Update()
        {
            if (!_formationSettling)
            {
                return;
            }

            if (Time.time - _lastRotateTime < rotationSettleSeconds)
            {
                return;
            }

            ConfirmFormation();
        }

        private void BootstrapDummyBattle()
        {
            _battleEnded = false;
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
            _enemyReserves.Clear();

            _allies.Add(heroA);
            _allies.Add(heroB);
            _allies.Add(heroC);
            _allies.Add(heroD);

            _reserves.Add(reserve);

            BattleUnit enemyA = CreateUnit("Goblin A", 70, 0, 10);
            BattleUnit enemyB = CreateUnit("Archer", 60, 0, 13);
            BattleUnit enemyC = CreateUnit("Goblin B", 70, 0, 8);
            BattleUnit enemyD = CreateUnit("Shaman", 55, 20, 7);
            BattleUnit enemyReserve = CreateUnit("Enemy Reserve", 65, 0, 11);

            _enemies.Add(enemyA);
            _enemies.Add(enemyB);
            _enemies.Add(enemyC);
            _enemies.Add(enemyD);
            _enemyReserves.Add(enemyReserve);

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
            if (_battleEnded || _formationSettling)
            {
                return;
            }
            ShowActionOverlay(skill.SkillName, _active.Name);
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {_active.Name}");

            ApplySkillDamage(skill);

            if (_battleEnded)
            {
                RedrawBoard();
                return;
            }

            MarkActiveAsActed();
            RedrawBoard();
            AdvanceToNextActor();
        }

        private void ApplySkillDamage(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            switch (skill.TargetPattern)
            {
                case SkillTargetPattern.FrontTopEnemy:
                    DamageEnemyAt(GridPos.FrontTop, 20);
                    break;

                case SkillTargetPattern.FrontBottomEnemy:
                    DamageEnemyAt(GridPos.FrontBottom, 20);
                    break;

                case SkillTargetPattern.BothFrontEnemies:
                    DamageEnemyAt(GridPos.FrontTop, 15);
                    DamageEnemyAt(GridPos.FrontBottom, 15);
                    break;

                case SkillTargetPattern.AllEnemies:
                    DamageEnemyAt(GridPos.FrontTop, 10);
                    DamageEnemyAt(GridPos.BackTop, 10);
                    DamageEnemyAt(GridPos.FrontBottom, 10);
                    DamageEnemyAt(GridPos.BackBottom, 10);
                    break;
            }
        }

        private void DamageEnemyAt(GridPos pos, int damage)
        {
            if (_battleEnded)
            {
                return;
            }

            BattleUnit target = _grid.GetUnit(false, pos);
            if (target == null || target.IsDead)
            {
                Debug.Log($"[Damage] Missed empty enemy cell: {pos}");
                return;
            }

            target.CurrentHP = Mathf.Max(0, target.CurrentHP - damage);

            Debug.Log($"[Damage] {target.Name} took {damage} damage. HP: {target.CurrentHP}/{target.Data.MaxHP}");

            if (target.CurrentHP <= 0)
            {
                HandleEnemyDefeated(target, pos);
            }
        }

        private void HandleEnemyDefeated(BattleUnit defeatedEnemy, GridPos position)
        {
            if (defeatedEnemy == null || defeatedEnemy.IsDead)
            {
                return;
            }

            defeatedEnemy.IsDead = true;
            _grid.SetUnit(false, position, null);
            RemoveTurnState(defeatedEnemy);

            Debug.Log($"[KO] {defeatedEnemy.Name} is defeated and removed from grid.");

            BattleUnit replacement = GetNextEnemyReserve();
            if (replacement == null)
            {
                Debug.Log($"[KO] No enemy reserve available for {defeatedEnemy.Name}.");
                CheckBattleEnd();
                return;
            }

            _grid.SetUnit(false, position, replacement);

            int enemyIndex = _enemies.IndexOf(defeatedEnemy);
            if (enemyIndex >= 0)
            {
                _enemies[enemyIndex] = replacement;
            }
            else
            {
                _enemies.Add(replacement);
            }

            _enemyReserves.Remove(replacement);

            _actedUnits.Add(replacement);

            Debug.Log($"[KO] {replacement.Name} replaced {defeatedEnemy.Name} at {position}. Replacement cannot act this turn.");
            CheckBattleEnd();
        }

        private void HandleSkillHover(SkillData skill)
        {
            if (_battleEnded)
            {
                return;
            }

            _hoveredSkill = skill;
            RedrawTargetPreview();
        }

        private void RedrawTargetPreview()
        {
            ResetEnemyBoardHighlights();

            if (_hoveredSkill == null)
            {
                return;
            }

            switch (_hoveredSkill.TargetPattern)
            {
                case SkillTargetPattern.FrontTopEnemy:
                    SetEnemyBoardCellColor(GridPos.FrontTop, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.FrontBottomEnemy:
                    SetEnemyBoardCellColor(GridPos.FrontBottom, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.BothFrontEnemies:
                    SetEnemyBoardCellColor(GridPos.FrontTop, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.FrontBottom, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.AllEnemies:
                    SetEnemyBoardCellColor(GridPos.FrontTop, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.BackTop, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.FrontBottom, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.BackBottom, TargetPreviewCellColor);
                    break;
            }
        }

        private void ClearTargetPreview()
        {
            _hoveredSkill = null;
            ResetEnemyBoardHighlights();
        }

        private void HandleSwap(BattleUnit reserve)
        {
            if (_battleEnded || _formationSettling)
            {
                return;
            }

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

        private void RemoveTurnState(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            _turnNumbers.Remove(unit);
            _actedUnits.Remove(unit);
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
            if (_battleEnded || _formationSettling)
            {
                return;
            }

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
            if (_active == null || _battleEnded)
            {
                return;
            }

            _actedUnits.Add(_active);
            RedrawStatusPanels();
        }

        private void AdvanceToNextActor()
        {
            if (_battleEnded)
            {
                return;
            }

            while (true)
            {
                BattleUnit nextUnit = FindNextUnactedUnit();

                if (nextUnit == null)
                {
                    StartNextTurn();
                    return;
                }

                if (_allies.Contains(nextUnit))
                {
                    _active = nextUnit;
                    commandPanel.Setup(_active, _reserves);
                    RedrawBoard();
                    Debug.Log($"[Turn] Next active ally: {_active.Name}");
                    return;
                }

                if (_enemies.Contains(nextUnit))
                {
                    _actedUnits.Add(nextUnit);
                    ApplyDummyEnemyAction(nextUnit);
                    RedrawBoard();

                    if (_battleEnded)
                    {
                        return;
                    }

                    continue;
                }

                _actedUnits.Add(nextUnit);
            }
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

        private BattleUnit FindNextUnactedUnit()
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

                return unit;
            }

            return null;
        }



        private void ApplyDummyEnemyAction(BattleUnit enemy)
        {
            if (enemy == null || enemy.IsDead || _battleEnded)
            {
                return;
            }

            GridPos targetPosition = enemy.GridPos;
            BattleUnit target = _grid.GetUnit(true, targetPosition);

            if (target == null || target.IsDead)
            {
                Debug.Log($"[Enemy] Dummy enemy action: {enemy.Name} missed unavailable ally cell: {targetPosition}");
                CheckBattleEnd();
                return;
            }

            const int damage = 80;

            target.CurrentHP = Mathf.Max(0, target.CurrentHP - damage);

            Debug.Log($"[Enemy] Dummy enemy action: {enemy.Name} -> {target.Name} took {damage} damage. HP: {target.CurrentHP}/{target.Data.MaxHP}");

            if (target.CurrentHP <= 0)
            {
                HandleAllyDefeated(target);
            }
        }

        private void HandleAllyDefeated(BattleUnit defeatedAlly)
        {
            if (defeatedAlly == null || defeatedAlly.IsDead)
            {
                return;
            }

            defeatedAlly.IsDead = true;
            Debug.Log($"[KO] {defeatedAlly.Name} is defeated.");

            GridPos position = defeatedAlly.GridPos;

            BattleUnit replacement = GetNextReserve();
            if (replacement == null)
            {
                _grid.SetUnit(true, position, null);
                RemoveTurnState(defeatedAlly);

                Debug.Log($"[KO] No reserve available for {defeatedAlly.Name}. Ally grid cell is now empty: {position}");

                CheckBattleEnd();
                RedrawBoard();
                return;
            }

            _grid.SetUnit(true, position, replacement);

            int allyIndex = _allies.IndexOf(defeatedAlly);
            if (allyIndex >= 0)
            {
                _allies[allyIndex] = replacement;
            }

            _reserves.Remove(replacement);
            RemoveTurnState(defeatedAlly);

            _actedUnits.Add(replacement);

            if (_active == defeatedAlly)
            {
                _active = replacement;
                commandPanel.Setup(_active, _reserves);
            }

            Debug.Log($"[KO] {replacement.Name} replaced {defeatedAlly.Name} at {position}. Replacement cannot act this turn.");
            CheckBattleEnd();
        }

        private BattleUnit GetNextReserve()
        {
            for (int i = 0; i < _reserves.Count; i++)
            {
                BattleUnit reserve = _reserves[i];
                if (reserve != null && !reserve.IsDead)
                {
                    return reserve;
                }
            }

            return null;
        }

        private BattleUnit GetNextEnemyReserve()
        {
            for (int i = 0; i < _enemyReserves.Count; i++)
            {
                BattleUnit reserve = _enemyReserves[i];
                if (reserve != null && !reserve.IsDead)
                {
                    return reserve;
                }
            }

            return null;
        }

        private void CheckBattleEnd()
        {
            if (_battleEnded)
            {
                return;
            }

            if (!HasAliveActiveEnemies() && !HasAliveEnemyReserves())
            {
                EndBattle("Victory");
                return;
            }

            if (!HasAliveActiveAllies() && !HasAliveAllyReserves())
            {
                EndBattle("Defeat");
            }
        }

        private void EndBattle(string result)
        {
            _battleEnded = true;
            ClearTargetPreview();

            if (commandPanel != null)
            {
                commandPanel.SetInteractable(false);
            }

            ShowActionOverlay(result, "Battle End");
            Debug.Log($"[Battle] {result}");
        }

        private bool HasAliveActiveEnemies()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                BattleUnit enemy = _enemies[i];
                if (enemy != null && !enemy.IsDead)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAliveEnemyReserves()
        {
            for (int i = 0; i < _enemyReserves.Count; i++)
            {
                BattleUnit enemy = _enemyReserves[i];
                if (enemy != null && !enemy.IsDead)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAliveActiveAllies()
        {
            for (int i = 0; i < _allies.Count; i++)
            {
                BattleUnit ally = _allies[i];
                if (ally != null && !ally.IsDead)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAliveAllyReserves()
        {
            for (int i = 0; i < _reserves.Count; i++)
            {
                BattleUnit ally = _reserves[i];
                if (ally != null && !ally.IsDead)
                {
                    return true;
                }
            }

            return false;
        }
        private void StartNextTurn()
        {
            if (_battleEnded)
            {
                return;
            }

            CompactFrontlineIfEmpty(true);

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
            if (_battleEnded)
            {
                return;
            }

            _formation.RotateAlliesClockwise();

            _formationSettling = true;
            _lastRotateTime = Time.time;

            if (commandPanel != null)
            {
                commandPanel.SetInteractable(false);
            }

            if (rotateButton != null)
            {
                rotateButton.interactable = true;
            }

            RedrawBoard();
        }

        private void ConfirmFormation()
        {
            _formationSettling = false;

            CompactFrontlineIfEmpty(true);

            RedrawBoard();

            if (!_battleEnded && commandPanel != null)
            {
                commandPanel.SetInteractable(true);
            }

            Debug.Log("[Formation] Formation confirmed.");
        }

        private void CompactFrontlineIfEmpty(bool isAlly)
        {
            BattleUnit frontTop = _grid.GetUnit(isAlly, GridPos.FrontTop);
            BattleUnit frontBottom = _grid.GetUnit(isAlly, GridPos.FrontBottom);

            bool hasFrontTop = frontTop != null && !frontTop.IsDead;
            bool hasFrontBottom = frontBottom != null && !frontBottom.IsDead;

            if (hasFrontTop || hasFrontBottom)
            {
                return;
            }

            BattleUnit backTop = _grid.GetUnit(isAlly, GridPos.BackTop);
            BattleUnit backBottom = _grid.GetUnit(isAlly, GridPos.BackBottom);

            bool hasBackTop = backTop != null && !backTop.IsDead;
            bool hasBackBottom = backBottom != null && !backBottom.IsDead;

            if (!hasBackTop && !hasBackBottom)
            {
                return;
            }

            if (hasBackTop)
            {
                _grid.SetUnit(isAlly, GridPos.BackTop, null);
                _grid.SetUnit(isAlly, GridPos.FrontTop, backTop);
            }

            if (hasBackBottom)
            {
                _grid.SetUnit(isAlly, GridPos.BackBottom, null);
                _grid.SetUnit(isAlly, GridPos.FrontBottom, backBottom);
            }

            Debug.Log($"[Formation] Compacted {(isAlly ? "ally" : "enemy")} frontline.");
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
            RedrawActiveHighlights();
            RedrawTargetPreview();
        }

        private void RedrawStatusPanels()
        {
            List<BattleUnit> aliveEnemies = GetAliveEnemies();

            for (int i = 0; i < 4; i++)
            {
                RedrawEnemyStatusSlot(i + 1, GetUnitAt(aliveEnemies, i));
                RedrawAllyStatusSlot(i + 1, GetUnitAt(_allies, i));
            }

            ResizeEnemyStatusPanel(aliveEnemies.Count);
            LayoutEnemyStatusSlots(aliveEnemies.Count);
        }

        private void RedrawActiveHighlights()
        {
            ResetAllyBoardHighlights();
            ResetAllyStatusHighlights();

            if (_active == null)
            {
                return;
            }

            SetAllyBoardCellColor(_active.GridPos, ActiveCellColor);

            int allyIndex = _allies.IndexOf(_active);
            if (allyIndex >= 0)
            {
                SetStatusSlotColor(allyStatusPanel, $"AllyStatus_{allyIndex + 1}", ActiveStatusColor);
            }
        }

        private void ResetAllyBoardHighlights()
        {
            SetCellImageColor(allyFrontTop, NormalCellColor);
            SetCellImageColor(allyBackTop, NormalCellColor);
            SetCellImageColor(allyFrontBottom, NormalCellColor);
            SetCellImageColor(allyBackBottom, NormalCellColor);
        }

        private void ResetEnemyBoardHighlights()
        {
            SetCellImageColor(enemyFrontTop, NormalCellColor);
            SetCellImageColor(enemyBackTop, NormalCellColor);
            SetCellImageColor(enemyFrontBottom, NormalCellColor);
            SetCellImageColor(enemyBackBottom, NormalCellColor);
        }

        private void SetEnemyBoardCellColor(GridPos pos, Color color)
        {
            switch (pos)
            {
                case GridPos.FrontTop:
                    SetCellImageColor(enemyFrontTop, color);
                    break;
                case GridPos.BackTop:
                    SetCellImageColor(enemyBackTop, color);
                    break;
                case GridPos.FrontBottom:
                    SetCellImageColor(enemyFrontBottom, color);
                    break;
                case GridPos.BackBottom:
                    SetCellImageColor(enemyBackBottom, color);
                    break;
            }
        }

        private void ResetAllyStatusHighlights()
        {
            for (int i = 1; i <= 4; i++)
            {
                SetStatusSlotColor(allyStatusPanel, $"AllyStatus_{i}", NormalStatusColor);
            }
        }

        private void SetAllyBoardCellColor(GridPos pos, Color color)
        {
            switch (pos)
            {
                case GridPos.FrontTop:
                    SetCellImageColor(allyFrontTop, color);
                    break;
                case GridPos.BackTop:
                    SetCellImageColor(allyBackTop, color);
                    break;
                case GridPos.FrontBottom:
                    SetCellImageColor(allyFrontBottom, color);
                    break;
                case GridPos.BackBottom:
                    SetCellImageColor(allyBackBottom, color);
                    break;
            }
        }

        private static void SetCellImageColor(TMP_Text cellLabel, Color color)
        {
            if (cellLabel == null || cellLabel.transform.parent == null)
            {
                return;
            }

            Image image = cellLabel.transform.parent.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetStatusSlotColor(Transform statusPanel, string slotName, Color color)
        {
            if (statusPanel == null)
            {
                return;
            }

            Transform slot = statusPanel.Find(slotName);
            if (slot == null)
            {
                return;
            }

            Image image = slot.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private void ResizeEnemyStatusPanel(int visibleEnemyCount)
        {
            if (enemyStatusPanel == null)
            {
                return;
            }

            enemyStatusPanel.gameObject.SetActive(visibleEnemyCount > 0);
        }
        private void LayoutEnemyStatusSlots(int visibleEnemyCount)
        {
            if (enemyStatusPanel == null)
            {
                return;
            }

            RectTransform panelRect = enemyStatusPanel as RectTransform;
            if (panelRect == null)
            {
                return;
            }

            float panelHeight = panelRect.rect.height;

            for (int i = 0; i < 4; i++)
            {
                Transform slot = enemyStatusPanel.Find($"EnemyStatus_{i + 1}");
                if (slot == null)
                {
                    continue;
                }

                bool visible = i < visibleEnemyCount;
                slot.gameObject.SetActive(visible);

                RectTransform rect = slot as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                if (!visible)
                {
                    continue;
                }

                // Do not change parent panel anchor / pivot here.
                // Place each slot from the top edge of the panel.
                float y =
                    (panelHeight * 0.5f)
                    - EnemyStatusPanelVerticalPadding
                    - (EnemyStatusSlotHeight * 0.5f)
                    - i * (EnemyStatusSlotHeight + EnemyStatusSlotSpacing);

                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
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

            slot.gameObject.SetActive(unit != null);

            if (unit == null)
            {
                return;
            }

            SetLabel(slot, "Name", unit.Name);
            SetLabel(slot, "TurnNumber", GetTurnOrderText(unit));

            int currentHp = unit.CurrentHP;
            int maxHp = unit.Data.MaxHP;
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

            slot.gameObject.SetActive(unit != null);

            if (unit == null)
            {
                return;
            }

            string displayName = unit.IsDead
                ? $"{unit.Name} KO"
                : unit.Name;

            SetLabel(slot, "Name", displayName);
            SetLabel(slot, "TurnNumber", GetTurnOrderText(unit));

            int currentHp = unit.IsDead ? 0 : unit.CurrentHP;
            int maxHp = unit.Data.MaxHP;
            int currentMp = unit.CurrentMP;
            int maxMp = unit.Data.MaxMP;

            SetBarFill(slot, "HPBar", currentHp, maxHp);
            SetBarFill(slot, "MPBar", currentMp, maxMp);
        }

        private List<BattleUnit> GetAliveEnemies()
        {
            var aliveEnemies = new List<BattleUnit>();

            for (int i = 0; i < _enemies.Count; i++)
            {
                BattleUnit enemy = _enemies[i];
                if (enemy != null && !enemy.IsDead)
                {
                    aliveEnemies.Add(enemy);
                }
            }

            return aliveEnemies;
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
            if (unit == null || unit.IsDead)
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

        private static string SafeName(BattleUnit unit)
        {
            if (unit == null)
            {
                return "-";
            }

            return unit.IsDead ? $"{unit.Name} KO" : unit.Name;
        }

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
            unit.Skills.Add(new SkillData { SkillId = "s1", SkillName = "Slash", Description = "Attack enemy front top.", TargetPattern = SkillTargetPattern.FrontTopEnemy });
            unit.Skills.Add(new SkillData { SkillId = "s2", SkillName = "Pierce", Description = "Attack enemy front bottom.", TargetPattern = SkillTargetPattern.FrontBottomEnemy });
            unit.Skills.Add(new SkillData { SkillId = "s3", SkillName = "TwinHit", Description = "Attack both front enemies.", TargetPattern = SkillTargetPattern.BothFrontEnemies });
            unit.Skills.Add(new SkillData { SkillId = "s4", SkillName = "Wave", Description = "Attack all enemies.", TargetPattern = SkillTargetPattern.AllEnemies });
            return unit;
        }
    }
}




