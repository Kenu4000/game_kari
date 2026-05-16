using System.Collections;
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
        private readonly Dictionary<BattleUnit, EnemyActionData> _enemyActions = new();

        private readonly Dictionary<BattleUnit, int> _turnNumbers = new();
        private readonly HashSet<BattleUnit> _actedUnits = new();
        private BattleUnit _active;
        private SkillData _hoveredSkill;
        private bool _battleEnded;
        private BattlePhase _phase;
        [SerializeField] private float rotationSettleSeconds = 0.5f;
        [SerializeField] private float actionResolveDelaySeconds = 0.35f;

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

        private enum BattlePhase
        {
            CommandSelect,
            ResolvingAction,
            BattleEnded
        }

        private enum EnemyTargetPattern
        {
            SameGridPosAlly,
            AllyFrontTop,
            AllyFrontBottom,
            BothFrontAllies,
            AllAllies
        }

        private class EnemyActionData
        {
            public string ActionName;
            public int Damage;
            public EnemyTargetPattern TargetPattern;
        }

        private class DefeatedEnemyInfo
        {
            public BattleUnit Unit;
            public GridPos Position;
        }

        private void Start()
        {
            BootstrapDummyBattle();
            BindUI();
            RedrawBoard();
        }

        private void Update()
        {
            HandleDebugBuffHotkeys();

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
        private void HandleDebugBuffHotkeys()
        {
#if UNITY_EDITOR
            if (_battleEnded || _active == null)
            {
                return;
            }

            bool changed = false;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ApplyBuff(_active, BuffType.AttackUp, 2);
                changed = true;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ApplyBuff(_active, BuffType.AttackDown, 2);
                changed = true;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ApplyBuff(_active, BuffType.DefenseUp, 2);
                changed = true;
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ApplyBuff(_active, BuffType.DefenseDown, 2);
                changed = true;
            }

            if (changed)
            {
                RedrawBoard();
            }
#endif
        }

        private void BootstrapDummyBattle()
        {
            _battleEnded = false;
            _phase = BattlePhase.CommandSelect;
            _formationSettling = false;
            _hoveredSkill = null;

            _grid = new BattleGrid();
            _formation = new FormationController(_grid);
            _turnOrder = new TurnOrderManager();

            ClearBattleLists();

            BattleUnit fallbackActive = SetupDummyAllies();
            SetupDummyEnemies();
            SetupInitialTurnState(fallbackActive);
        }

        private void ClearBattleLists()
        {
            _allies.Clear();
            _enemies.Clear();
            _reserves.Clear();
            _enemyReserves.Clear();
            _enemyActions.Clear();
            _turnNumbers.Clear();
            _actedUnits.Clear();
        }

        private BattleUnit SetupDummyAllies()
        {
            BattleUnit heroA = CreateUnit("Knight", 130, 20, 12);
            BattleUnit heroB = CreateUnit("Mage", 80, 60, 15);
            BattleUnit heroC = CreateUnit("Cleric", 90, 50, 9);
            BattleUnit heroD = CreateUnit("Rogue", 95, 25, 18);
            BattleUnit reserve = CreateUnit("Reserve", 100, 40, 11);

            _grid.SetUnit(true, GridPos.FrontTop, heroA);
            _grid.SetUnit(true, GridPos.BackTop, heroB);
            _grid.SetUnit(true, GridPos.FrontBottom, heroC);
            _grid.SetUnit(true, GridPos.BackBottom, heroD);

            _allies.Add(heroA);
            _allies.Add(heroB);
            _allies.Add(heroC);
            _allies.Add(heroD);

            _reserves.Add(reserve);

            return heroA;
        }

        private void SetupDummyEnemies()
        {
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

            SetEnemyAction(enemyA, "Claw", 60, EnemyTargetPattern.SameGridPosAlly);
            SetEnemyAction(enemyB, "Arrow", 45, EnemyTargetPattern.AllyFrontTop);
            SetEnemyAction(enemyC, "Bite", 60, EnemyTargetPattern.AllyFrontBottom);
            SetEnemyAction(enemyD, "Hex", 25, EnemyTargetPattern.AllAllies);
            SetEnemyAction(enemyReserve, "Strike", 60, EnemyTargetPattern.SameGridPosAlly);
        }

        private void SetEnemyAction(BattleUnit enemy, string actionName, int damage, EnemyTargetPattern targetPattern)
        {
            if (enemy == null)
            {
                return;
            }

            _enemyActions[enemy] = new EnemyActionData
            {
                ActionName = actionName,
                Damage = damage,
                TargetPattern = targetPattern
            };
        }

        private EnemyActionData GetEnemyAction(BattleUnit enemy)
        {
            if (enemy != null && _enemyActions.TryGetValue(enemy, out EnemyActionData action))
            {
                return action;
            }

            return new EnemyActionData
            {
                ActionName = "Strike",
                Damage = 60,
                TargetPattern = EnemyTargetPattern.SameGridPosAlly
            };
        }

        private void SetupInitialTurnState(BattleUnit fallbackActive)
        {
            RebuildTurnOrder();
            _active = FindNextUnactedAlly() ?? fallbackActive;
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

        private bool CanAcceptPlayerCommand()
        {
            return !_battleEnded
                && _phase == BattlePhase.CommandSelect
                && !_formationSettling;
        }

        private bool CanAcceptRotateCommand()
        {
            return !_battleEnded
                && _phase == BattlePhase.CommandSelect;
        }

        private void EnterResolvingAction()
        {
            if (_battleEnded)
            {
                return;
            }

            _phase = BattlePhase.ResolvingAction;
            ClearTargetPreview();

            if (commandPanel != null)
            {
                commandPanel.SetInteractable(false);
            }

            if (rotateButton != null)
            {
                rotateButton.interactable = false;
            }
        }

        private void EnterCommandSelect(BattleUnit activeUnit)
        {
            if (_battleEnded)
            {
                return;
            }

            _phase = BattlePhase.CommandSelect;
            _active = activeUnit;

            if (commandPanel != null)
            {
                commandPanel.Setup(_active, _reserves);
                commandPanel.SetInteractable(true);
            }

            if (rotateButton != null)
            {
                rotateButton.interactable = true;
            }
        }
        private void HandleSkillClicked(SkillData skill)
        {
            if (!CanAcceptPlayerCommand())
            {
                return;
            }

            if (_active.CurrentMP < skill.MpCost)
            {
                Debug.Log($"[Action] Skill failed: {_active.Name} does not have enough MP for {skill.SkillName}. MP: {_active.CurrentMP}/{skill.MpCost}");
                return;
            }

            EnterResolvingAction();

            _active.CurrentMP -= skill.MpCost;

            ShowActionOverlay(skill.SkillName, _active.Name);
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {_active.Name}. MP: {_active.CurrentMP}/{_active.Data.MaxMP}");

            ApplySkillDamage(skill);
            ApplySkillEffect(skill);

            if (_battleEnded)
            {
                RedrawBoard();
                return;
            }

            StartCoroutine(FinishPlayerActionAfterDelay());
        }

        private void ApplySkillDamage(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            List<DefeatedEnemyInfo> defeatedEnemies = new();

            switch (skill.TargetPattern)
            {
                case SkillTargetPattern.FrontTopEnemy:
                    DamageEnemyAt(GridPos.FrontTop, skill.Damage, defeatedEnemies);
                    break;

                case SkillTargetPattern.FrontBottomEnemy:
                    DamageEnemyAt(GridPos.FrontBottom, skill.Damage, defeatedEnemies);
                    break;

                case SkillTargetPattern.BothFrontEnemies:
                    DamageEnemyAt(GridPos.FrontTop, skill.Damage, defeatedEnemies);
                    DamageEnemyAt(GridPos.FrontBottom, skill.Damage, defeatedEnemies);
                    break;

                case SkillTargetPattern.AllEnemies:
                    DamageEnemyAt(GridPos.FrontTop, skill.Damage, defeatedEnemies);
                    DamageEnemyAt(GridPos.BackTop, skill.Damage, defeatedEnemies);
                    DamageEnemyAt(GridPos.FrontBottom, skill.Damage, defeatedEnemies);
                    DamageEnemyAt(GridPos.BackBottom, skill.Damage, defeatedEnemies);
                    break;

                case SkillTargetPattern.Self:
                    break;
            }

            ResolveDefeatedEnemies(defeatedEnemies);
        }
        private void ApplySkillEffect(SkillData skill)
        {
            if (skill == null || _active == null || _active.IsDead)
            {
                return;
            }

            switch (skill.EffectType)
            {
                case SkillEffectType.None:
                    return;

                case SkillEffectType.ApplyBuff:
                    ApplyBuff(_active, skill.BuffType, skill.BuffTurns);
                    return;
            }
        }

        private void DamageEnemyAt(GridPos pos, int damage, List<DefeatedEnemyInfo> defeatedEnemies)
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

            int finalDamage = CalculateDamage(_active, target, damage);

            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);

            Debug.Log($"[Damage] {target.Name} took {finalDamage} damage. HP: {target.CurrentHP}/{target.Data.MaxHP}");

            if (target.CurrentHP <= 0 && !ContainsDefeatedEnemy(defeatedEnemies, target))
            {
                defeatedEnemies.Add(new DefeatedEnemyInfo
                {
                    Unit = target,
                    Position = pos
                });
            }
        }

        private int CalculateDamage(BattleUnit attacker, BattleUnit target, int baseDamage)
        {
            if (attacker == null || target == null)
            {
                return Mathf.Max(0, baseDamage);
            }

            float multiplier = 1f;

            if (HasBuff(attacker, BuffType.AttackUp))
            {
                multiplier *= 1.5f;
            }

            if (HasBuff(attacker, BuffType.AttackDown))
            {
                multiplier *= 0.5f;
            }

            if (HasBuff(target, BuffType.DefenseUp))
            {
                multiplier *= 0.5f;
            }

            if (HasBuff(target, BuffType.DefenseDown))
            {
                multiplier *= 1.5f;
            }

            int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);
            return Mathf.Max(0, finalDamage);
        }

        private bool HasBuff(BattleUnit unit, BuffType type)
        {
            return FindBuff(unit, type) != null;
        }
        private void ApplyBuff(BattleUnit unit, BuffType type, int turns)
        {
            if (unit == null || unit.IsDead || turns <= 0)
            {
                return;
            }

            BuffType? opposite = GetOppositeBuffType(type);
            if (opposite.HasValue)
            {
                BuffState oppositeBuff = FindBuff(unit, opposite.Value);
                if (oppositeBuff != null)
                {
                    unit.Buffs.Remove(oppositeBuff);
                    Debug.Log($"[Buff] {unit.Name}: {type} cancelled {opposite.Value}.");
                    return;
                }
            }

            BuffState existing = FindBuff(unit, type);
            if (existing != null)
            {
                existing.RemainingTurns = turns;
                Debug.Log($"[Buff] {unit.Name}: {type} refreshed to {turns} turns.");
                return;
            }

            unit.Buffs.Add(new BuffState
            {
                Type = type,
                RemainingTurns = turns
            });

            Debug.Log($"[Buff] {unit.Name}: {type} applied for {turns} turns.");
        }

        private BuffState FindBuff(BattleUnit unit, BuffType type)
        {
            if (unit == null || unit.Buffs == null)
            {
                return null;
            }

            for (int i = 0; i < unit.Buffs.Count; i++)
            {
                BuffState buff = unit.Buffs[i];
                if (buff != null && buff.Type == type)
                {
                    return buff;
                }
            }

            return null;
        }

        private BuffType? GetOppositeBuffType(BuffType type)
        {
            switch (type)
            {
                case BuffType.AttackUp:
                    return BuffType.AttackDown;

                case BuffType.AttackDown:
                    return BuffType.AttackUp;

                case BuffType.DefenseUp:
                    return BuffType.DefenseDown;

                case BuffType.DefenseDown:
                    return BuffType.DefenseUp;

                default:
                    return null;
            }
        }

        private void TickBuffsAtTurnStart()
        {
            TickBuffsInUnits(_grid.AllyGrid.Values);
            TickBuffsInUnits(_grid.EnemyGrid.Values);
        }

        private void TickBuffsInUnits(IEnumerable<BattleUnit> units)
        {
            if (units == null)
            {
                return;
            }

            foreach (BattleUnit unit in units)
            {
                TickBuffs(unit);
            }
        }

        private void TickBuffs(BattleUnit unit)
        {
            if (unit == null || unit.IsDead || unit.Buffs == null || unit.Buffs.Count == 0)
            {
                return;
            }

            for (int i = unit.Buffs.Count - 1; i >= 0; i--)
            {
                BuffState buff = unit.Buffs[i];
                if (buff == null)
                {
                    unit.Buffs.RemoveAt(i);
                    continue;
                }

                buff.RemainingTurns--;

                if (buff.RemainingTurns <= 0)
                {
                    Debug.Log($"[Buff] {unit.Name}: {buff.Type} expired.");
                    unit.Buffs.RemoveAt(i);
                }
            }
        }

        private bool ContainsDefeatedEnemy(List<DefeatedEnemyInfo> defeatedEnemies, BattleUnit target)
        {
            if (defeatedEnemies == null || target == null)
            {
                return false;
            }

            for (int i = 0; i < defeatedEnemies.Count; i++)
            {
                if (defeatedEnemies[i].Unit == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveDefeatedEnemies(List<DefeatedEnemyInfo> defeatedEnemies)
        {
            if (defeatedEnemies == null || defeatedEnemies.Count == 0)
            {
                return;
            }

            for (int i = 0; i < defeatedEnemies.Count; i++)
            {
                DefeatedEnemyInfo defeated = defeatedEnemies[i];
                if (defeated == null || defeated.Unit == null || defeated.Unit.IsDead)
                {
                    continue;
                }

                defeated.Unit.IsDead = true;
                _grid.SetUnit(false, defeated.Position, null);
                RemoveTurnState(defeated.Unit);

                Debug.Log($"[KO] {defeated.Unit.Name} is defeated and removed from grid.");
            }

            CompactEnemyFrontlineIfEmpty();
            FillEmptyEnemyCellsFromReserves();

            CheckBattleEnd();
        }

        private void CompactEnemyFrontlineIfEmpty()
        {
            BattleUnit frontTop = _grid.GetUnit(false, GridPos.FrontTop);
            BattleUnit frontBottom = _grid.GetUnit(false, GridPos.FrontBottom);

            bool hasFrontTop = frontTop != null && !frontTop.IsDead;
            bool hasFrontBottom = frontBottom != null && !frontBottom.IsDead;

            if (hasFrontTop || hasFrontBottom)
            {
                return;
            }

            BattleUnit backTop = _grid.GetUnit(false, GridPos.BackTop);
            BattleUnit backBottom = _grid.GetUnit(false, GridPos.BackBottom);

            bool hasBackTop = backTop != null && !backTop.IsDead;
            bool hasBackBottom = backBottom != null && !backBottom.IsDead;

            if (!hasBackTop && !hasBackBottom)
            {
                return;
            }

            if (hasBackTop)
            {
                _grid.SetUnit(false, GridPos.BackTop, null);
                _grid.SetUnit(false, GridPos.FrontTop, backTop);
            }

            if (hasBackBottom)
            {
                _grid.SetUnit(false, GridPos.BackBottom, null);
                _grid.SetUnit(false, GridPos.FrontBottom, backBottom);
            }

            Debug.Log("[Formation] Compacted enemy frontline.");
        }

        private void FillEmptyEnemyCellsFromReserves()
        {
            TryFillEnemyCellFromReserve(GridPos.FrontTop);
            TryFillEnemyCellFromReserve(GridPos.FrontBottom);
            TryFillEnemyCellFromReserve(GridPos.BackTop);
            TryFillEnemyCellFromReserve(GridPos.BackBottom);
        }

        private void TryFillEnemyCellFromReserve(GridPos position)
        {
            BattleUnit current = _grid.GetUnit(false, position);
            if (current != null && !current.IsDead)
            {
                return;
            }

            BattleUnit replacement = GetNextEnemyReserve();
            if (replacement == null)
            {
                return;
            }

            _grid.SetUnit(false, position, replacement);

            if (!_enemies.Contains(replacement))
            {
                _enemies.Add(replacement);
            }

            _enemyReserves.Remove(replacement);
            _actedUnits.Add(replacement);

            Debug.Log($"[KO] {replacement.Name} entered enemy grid at {position}. Replacement cannot act this turn.");
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
            if (!CanAcceptPlayerCommand())
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

            EnterCommandSelect(reserve);
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

        private void HandleItemClicked(ItemData item)
        {
            if (!CanAcceptPlayerCommand())
            {
                return;
            }

            if (item == null)
            {
                return;
            }

            if (item.Count <= 0)
            {
                Debug.Log($"[Item] No {item.ItemName} left. Item cannot be used.");
                return;
            }

            BattleUnit target = TryGetForwardAlly(_active);
            if (target == null)
            {
                Debug.Log("[Item] No forward ally target. Item cannot be used.");
                return;
            }

            EnterResolvingAction();

            int beforeHp = target.CurrentHP;
            target.CurrentHP = Mathf.Min(target.CurrentHP + item.HealAmount, target.Data.MaxHP);
            int healed = target.CurrentHP - beforeHp;

            item.Count--;

            if (commandPanel != null)
            {
                commandPanel.RefreshItems();
            }

            ShowActionOverlay(item.ItemName, _active.Name);
            Debug.Log($"[Action] Item used: {item.ItemName} -> {target.Name} healed {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}. Remaining: {item.Count}");

            StartCoroutine(FinishPlayerActionAfterDelay());
        }

        private IEnumerator FinishPlayerActionAfterDelay()
        {
            if (actionResolveDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(actionResolveDelaySeconds);
            }

            if (_battleEnded)
            {
                yield break;
            }

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
                    EnterCommandSelect(nextUnit);
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

            EnemyActionData action = GetEnemyAction(enemy);

            ShowActionOverlay(action.ActionName, enemy.Name);

            switch (action.TargetPattern)
            {
                case EnemyTargetPattern.SameGridPosAlly:
                    DamageAllyAt(enemy.GridPos, action.Damage, enemy, action.ActionName);
                    break;

                case EnemyTargetPattern.AllyFrontTop:
                    DamageAllyAt(GridPos.FrontTop, action.Damage, enemy, action.ActionName);
                    break;

                case EnemyTargetPattern.AllyFrontBottom:
                    DamageAllyAt(GridPos.FrontBottom, action.Damage, enemy, action.ActionName);
                    break;

                case EnemyTargetPattern.BothFrontAllies:
                    DamageAllyAt(GridPos.FrontTop, action.Damage, enemy, action.ActionName);
                    DamageAllyAt(GridPos.FrontBottom, action.Damage, enemy, action.ActionName);
                    break;

                case EnemyTargetPattern.AllAllies:
                    DamageAllyAt(GridPos.FrontTop, action.Damage, enemy, action.ActionName);
                    DamageAllyAt(GridPos.BackTop, action.Damage, enemy, action.ActionName);
                    DamageAllyAt(GridPos.FrontBottom, action.Damage, enemy, action.ActionName);
                    DamageAllyAt(GridPos.BackBottom, action.Damage, enemy, action.ActionName);
                    break;
            }
        }

        private void DamageAllyAt(GridPos targetPosition, int damage, BattleUnit enemy, string actionName)
        {
            if (_battleEnded)
            {
                return;
            }

            BattleUnit target = _grid.GetUnit(true, targetPosition);

            if (target == null || target.IsDead)
            {
                Debug.Log($"[Enemy] {enemy.Name} used {actionName} and missed unavailable ally cell: {targetPosition}");
                CheckBattleEnd();
                return;
            }

            int finalDamage = CalculateDamage(enemy, target, damage);

            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);

            Debug.Log($"[Enemy] {enemy.Name} used {actionName}: {target.Name} took {finalDamage} damage. HP: {target.CurrentHP}/{target.Data.MaxHP}");

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
            _phase = BattlePhase.BattleEnded;
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

            TickBuffsAtTurnStart();

            RebuildTurnOrder();

            BattleUnit nextAlly = FindNextUnactedAlly();
            if (nextAlly != null)
            {
                EnterCommandSelect(nextAlly);
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
            if (!CanAcceptRotateCommand())
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
            // Enemy side is displayed mirrored on screen.
            // Visual left cells show enemy backline, visual right cells show enemy frontline.
            enemyFrontTop.text = SafeName(_grid.GetUnit(false, GridPos.BackTop));
            enemyBackTop.text = SafeName(_grid.GetUnit(false, GridPos.FrontTop));
            enemyFrontBottom.text = SafeName(_grid.GetUnit(false, GridPos.BackBottom));
            enemyBackBottom.text = SafeName(_grid.GetUnit(false, GridPos.FrontBottom));

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
                    SetCellImageColor(enemyBackTop, color);
                    break;
                case GridPos.BackTop:
                    SetCellImageColor(enemyFrontTop, color);
                    break;
                case GridPos.FrontBottom:
                    SetCellImageColor(enemyBackBottom, color);
                    break;
                case GridPos.BackBottom:
                    SetCellImageColor(enemyFrontBottom, color);
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
            SetOrCreateLabel(slot, "Buffs", BuildBuffText(unit));
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
            SetOrCreateLabel(slot, "Buffs", BuildBuffText(unit));
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

        private static string BuildBuffText(BattleUnit unit)
        {
            if (unit == null || unit.Buffs == null || unit.Buffs.Count == 0)
            {
                return "";
            }

            var lines = new List<string>();

            for (int i = 0; i < unit.Buffs.Count; i++)
            {
                BuffState buff = unit.Buffs[i];
                if (buff == null)
                {
                    continue;
                }

                lines.Add($"{buff.Type} {buff.RemainingTurns}");
            }

            return string.Join("\n", lines);
        }
        private static void SetLabel(Transform root, string childName, string text)
        {
            TMP_Text label = root.Find(childName)?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void SetOrCreateLabel(Transform root, string childName, string text)
        {
            if (root == null)
            {
                return;
            }

            Transform existing = root.Find(childName);
            TMP_Text label = existing == null
                ? null
                : existing.GetComponent<TMP_Text>();

            if (label == null)
            {
                GameObject labelObject = new GameObject(childName);
                labelObject.transform.SetParent(root, false);

                label = labelObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = 16f;
                label.alignment = TextAlignmentOptions.Left;
                label.raycastTarget = false;

                RectTransform rect = labelObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.offsetMin = new Vector2(10f, 8f);
                rect.offsetMax = new Vector2(-10f, 42f);
            }

            label.text = text ?? "";
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
            AddDefaultSkills(unit);

            return unit;
        }

        private static void AddDefaultSkills(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            unit.Skills.Add(CreateSkill(
                "s1",
                "Slash",
                "Attack enemy front top.",
                SkillTargetPattern.FrontTopEnemy,
                0,
                20
            ));

            unit.Skills.Add(CreateSkill(
                "s2",
                "Pierce",
                "Attack enemy front bottom.",
                SkillTargetPattern.FrontBottomEnemy,
                5,
                20
            ));

            unit.Skills.Add(CreateSkill(
                "s3",
                "TwinHit",
                "Attack both front enemies.",
                SkillTargetPattern.BothFrontEnemies,
                8,
                15
            ));

            // Temporary buff test skill.
            // Wave is intentionally parked until skill slot/UI handling is expanded.
            unit.Skills.Add(CreateSkill(
                "s4",
                "Focus",
                "Apply AttackUp to self.",
                SkillTargetPattern.Self,
                6,
                0,
                SkillEffectType.ApplyBuff,
                BuffType.AttackUp,
                2
            ));
        }

        private static SkillData CreateSkill(
            string skillId,
            string skillName,
            string description,
            SkillTargetPattern targetPattern,
            int mpCost,
            int damage,
            SkillEffectType effectType = SkillEffectType.None,
            BuffType buffType = BuffType.AttackUp,
            int buffTurns = 0)
        {
            return new SkillData
            {
                SkillId = skillId,
                SkillName = skillName,
                Description = description,
                TargetPattern = targetPattern,
                MpCost = mpCost,
                Damage = damage,
                EffectType = effectType,
                BuffType = buffType,
                BuffTurns = buffTurns
            };
        }

    }
}















