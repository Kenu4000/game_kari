using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    public class BattleUIManager : MonoBehaviour
    {
        // Serialized references
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

        // Runtime state
        private BattleGrid _grid;
        private FormationController _formation;
        private TurnOrderManager _turnOrder;

        private readonly List<BattleUnit> _allies = new();
        private readonly List<BattleUnit> _enemies = new();
        private readonly List<BattleUnit> _reserves = new();
        private readonly List<BattleUnit> _partyMembers = new();

        private readonly List<BattleUnit> _enemyReserves = new();
        private readonly List<InventoryItem> _inventoryItems = new();
        private readonly Dictionary<BattleUnit, EnemyActionState> _previewEnemyActionStates = new();

        private readonly Dictionary<BattleUnit, int> _turnNumbers = new();
        private readonly HashSet<BattleUnit> _actedUnits = new();
        private BattleUnit _active;
        private SkillData _hoveredSkill;
        private GameObject _resultPanelObject;
        private TMP_Text _resultTitleText;
        private TMP_Text _resultSubText;
        private Button _resultFormationButton;
        private TMP_Text _resultFormationButtonText;
        private Button _resultReturnButton;
        private TMP_Text _resultReturnButtonText;
        private GameObject _enemyActionPreviewPanelObject;
        private TMP_Text _enemyActionPreviewText;
        private readonly List<GridPos> _pendingActionFlashTargets = new();
        private bool _pendingActionFlashIsAllyBoard;
        private readonly List<GridPos> _pendingActionSourceFlashTargets = new();
        private bool _pendingActionSourceFlashIsAllyBoard;
        private readonly List<ActionValuePopup> _pendingActionValuePopups = new();
        private readonly List<TMP_Text> _activeActionValuePopupLabels = new();

        private bool _battleEnded;
        private BattlePhase _phase;
        private WaveProgressState _waveProgress;
        private QuestProgressState _questProgress;
        private int _oneTurnClearPartyHeal = DefaultOneTurnClearPartyHeal;
        private int _kakeraStock;
        private int _totalKakeraEarned;
        [SerializeField] private float rotationSettleSeconds = 0.5f;
        [SerializeField] private float actionResolveDelaySeconds = 0.35f;
        [SerializeField] private int actionFlashCount = 3;
        [SerializeField] private Color damagePopupColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color healPopupColor = new Color(0.45f, 1f, 0.45f, 1f);

        private bool _formationSettling;
        private float _lastRotateTime;

        // Constants and phase types
        private static readonly Color NormalStatusColor = new Color(0.9f, 0.93f, 0.96f, 1f);
        private static readonly Color ActiveStatusColor = new Color(0.7f, 0.85f, 1f, 1f);
        private static readonly Color NormalCellColor = new Color(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color ActiveCellColor = new Color(0.7f, 0.88f, 1f, 1f);
        private static readonly Color TargetPreviewCellColor = new Color(1f, 0.92f, 0.55f, 1f);
        private static readonly Color EnemyActionPreviewCellColor = new Color(1f, 0.65f, 0.65f, 1f);
        private static readonly Color ActionFlashCellColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color ActionSourceFlashCellColor = new Color(0.75f, 1f, 1f, 1f);

        private const float EnemyStatusPanelVerticalPadding = 24f;
        private const float EnemyStatusSlotHeight = 135f;
        private const float EnemyStatusSlotSpacing = 16f;
        private const float EnemyStatusSlotWidth = 240f;
        // 盤面Spriteをセルより少し大きく表示するためのアンカー値。
        // -0.10〜1.10 にすることで、セル範囲から少しはみ出して表示できる。
        private const float BoardSpriteMinAnchor = -0.10f;
        private const float BoardSpriteMaxAnchor = 1.10f;

        // BattleSprite未設定キャラの名前だけ表示するときの文字サイズ。
        private const float BoardTextOnlyFontSize = 24f;

        // BattleSetupDataにWave/Distance設定がない場合の保険値。
        // 通常はBattleSetupData側の値を使う。
        private const int DefaultTargetDistance = 100;
        private const int DefaultBaseWaveDistance = 20;
        private const int DefaultOneTurnClearPartyHeal = 5;
        private const int MaxKakeraStock = 9;

        private sealed class ActionValuePopup
        {
            public bool IsAllyBoard;
            public GridPos Position;
            public string Text;
        }

        private sealed class WaveClearResult
        {
            public WaveClearRank Rank;
            public int DistanceGain;
            public int CurrentDistance;
            public int TargetDistance;
            public int PartyHealAmount;
            public int WaveNumber;
            public int TotalWaves;
            public bool HasNextWave;
            public QuestResultData QuestResult;
        }
        private enum BattlePhase
        {
            CommandSelect,
            ResolvingAction,
            BattleEnded
        }

        private enum WaveClearRank
        {
            OneTurn,
            TwoTurn,
            ThreeTurn,
            FourPlusTurn
        }

        private class DefeatedEnemyInfo
        {
            public BattleUnit Unit;
            public GridPos Position;
        }

        // Battle setup
        private void Start()
        {
            BootstrapBattle();
            BindUI();
            EnsureResultPanel();
            EnsureEnemyActionPreviewPanel();
            RedrawBoard();
            HideActionOverlay();
            HideResultPanel();
            SetEnemyActionPreviewVisible(false);
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

        private void BootstrapBattle()
        {
            _battleEnded = false;
            _phase = BattlePhase.CommandSelect;
            _formationSettling = false;
            _hoveredSkill = null;

            _grid = new BattleGrid();
            _formation = new FormationController(_grid);
            _turnOrder = new TurnOrderManager();

            ClearBattleLists();

            BattleSetupData setup = DefaultBattleSetupFactory.CreateDefaultSetup();
            ApplyWaveProgressSettings(setup);
            ApplyBattleSetup(setup);
            SetupInitialTurnState(setup.FallbackActive);
        }

        private void ClearBattleLists()
        {
            _allies.Clear();
            _enemies.Clear();
            _reserves.Clear();
            _partyMembers.Clear();
            _enemyReserves.Clear();
            _inventoryItems.Clear();
            _kakeraStock = 0;
            _totalKakeraEarned = 0;
            _previewEnemyActionStates.Clear();
            _turnNumbers.Clear();
            _actedUnits.Clear();
        }

        private void ApplyWaveProgressSettings(BattleSetupData setup)
        {
            _questProgress = setup == null
                ? null
                : setup.QuestProgress;

            int targetDistance = setup == null
                ? DefaultTargetDistance
                : setup.TargetDistance;

            int baseWaveDistance = setup == null
                ? DefaultBaseWaveDistance
                : setup.BaseWaveDistance;

            _oneTurnClearPartyHeal = setup == null
                ? DefaultOneTurnClearPartyHeal
                : Mathf.Max(0, setup.OneTurnClearPartyHeal);

            _waveProgress = new WaveProgressState(targetDistance, baseWaveDistance);
        }

        private void ApplyBattleSetup(BattleSetupData setup)
        {
            if (setup == null)
            {
                return;
            }

            ApplyBattleUnitPlacements(true, setup.AllyPlacements, _allies);
            ApplyBattleUnitPlacements(false, setup.EnemyPlacements, _enemies);

            _reserves.AddRange(setup.AllyReserves);
            RegisterPartyMembers(_allies);
            RegisterPartyMembers(_reserves);

            _enemyReserves.AddRange(setup.EnemyReserves);
            _inventoryItems.AddRange(setup.InventoryItems);
        }

        private void RegisterPartyMembers(List<BattleUnit> units)
        {
            if (units == null)
            {
                return;
            }

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || _partyMembers.Contains(unit))
                {
                    continue;
                }

                _partyMembers.Add(unit);
            }
        }

        private void ApplyBattleUnitPlacements(
            bool isAlly,
            List<BattleUnitPlacement> placements,
            List<BattleUnit> units)
        {
            if (placements == null || units == null)
            {
                return;
            }

            for (int i = 0; i < placements.Count; i++)
            {
                BattleUnitPlacement placement = placements[i];

                if (placement == null || placement.Unit == null)
                {
                    continue;
                }

                _grid.SetUnit(isAlly, placement.Position, placement.Unit);
                units.Add(placement.Unit);
            }
        }
        // Enemy action selection
        private EnemyActionState ResolveEnemyActionState(BattleUnit enemy)
        {
            return EnemyActionSelector.ResolveEnemyActionState(enemy);
        }

        private void EnsureEnemyActionStatesForPreview()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                BattleUnit enemy = _enemies[i];

                if (enemy == null || enemy.IsDead || _actedUnits.Contains(enemy))
                {
                    continue;
                }

                if (_previewEnemyActionStates.ContainsKey(enemy))
                {
                    continue;
                }

                _previewEnemyActionStates[enemy] = ResolveEnemyActionState(enemy);
            }
        }

        private EnemyActionState GetPreviewEnemyActionState(BattleUnit enemy)
        {
            if (enemy == null)
            {
                return null;
            }

            if (_previewEnemyActionStates.TryGetValue(enemy, out EnemyActionState selectedAction))
            {
                return selectedAction;
            }

            EnemyActionState action = ResolveEnemyActionState(enemy);
            _previewEnemyActionStates[enemy] = action;
            return action;
        }

        private void ClearPreviewEnemyActionState(BattleUnit enemy)
        {
            if (enemy == null)
            {
                return;
            }

            _previewEnemyActionStates.Remove(enemy);
        }

        private void SetupInitialTurnState(BattleUnit fallbackActive)
        {
            RebuildTurnOrder();
            _active = FindNextUnactedAlly() ?? fallbackActive;
        }

        private void BindUI()
        {
            commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
            commandPanel.OnSkillClicked += HandleSkillClicked;
            commandPanel.OnSkillHovered += HandleSkillHover;
            commandPanel.OnHoverExit += ClearTargetPreview;
            commandPanel.OnReserveClicked += HandleSwap;
            commandPanel.OnItemClicked += HandleItemClicked;
            rotateButton.onClick.AddListener(HandleRotateClicked);
        }

        // Skill availability helpers
        private bool CanUseSkill(BattleUnit user, SkillData skill)
        {
            if (user == null || skill == null)
            {
                return false;
            }

            int mpCost = Mathf.Max(0, skill.MpCost);
            if (user.CurrentMP < mpCost)
            {
                Debug.Log($"[MP] Skill blocked: {user.Name} cannot use {skill.SkillName}. MP {user.CurrentMP}/{user.Data.MaxMP}, Cost {mpCost}.");
                return false;
            }

            if (skill.SkillKind == SkillKind.Link)
            {
                BattleUnit linkPartner = GetLinkPartnerForSkill(user, skill);
                if (linkPartner == null)
                {
                    Debug.Log($"[Link] Skill blocked: {user.Name} cannot use {skill.SkillName}. Specified partner is unavailable.");
                    return false;
                }

                if (linkPartner.CurrentMP < mpCost)
                {
                    Debug.Log($"[MP] Link skill blocked: {linkPartner.Name} cannot support {skill.SkillName}. MP {linkPartner.CurrentMP}/{linkPartner.Data.MaxMP}, Cost {mpCost}.");
                    return false;
                }
            }

            return true;
        }

        private BattleUnit GetLinkPartnerForSkill(BattleUnit user, SkillData skill)
        {
            if (user == null || skill == null || skill.SkillKind != SkillKind.Link)
            {
                return null;
            }

            if (string.IsNullOrEmpty(skill.LinkPartnerCharacterId))
            {
                return null;
            }

            BattleUnit partner = FindUnitByCharacterId(_allies, skill.LinkPartnerCharacterId);
            if (partner != null && partner != user)
            {
                return partner;
            }

            partner = FindUnitByCharacterId(_reserves, skill.LinkPartnerCharacterId);
            if (partner != null && partner != user)
            {
                return partner;
            }

            return null;
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

        private static string BuildSkillUserDisplayName(BattleUnit user, BattleUnit linkPartner)
        {
            if (user == null)
            {
                return "";
            }

            if (linkPartner == null)
            {
                return user.Name;
            }

            return $"{user.Name} + {linkPartner.Name}";
        }

        private bool IsActiveAllyUnit(BattleUnit unit)
        {
            return unit != null && _allies != null && _allies.Contains(unit);
        }

        private static List<GridPos> BuildSkillSourceFlashTargets(BattleUnit user, BattleUnit linkPartner)
        {
            var targets = new List<GridPos>();

            if (user != null)
            {
                targets.Add(user.GridPos);
            }

            if (linkPartner != null && !targets.Contains(linkPartner.GridPos))
            {
                targets.Add(linkPartner.GridPos);
            }

            return targets;
        }
        private void ConsumeSkillMP(BattleUnit user, SkillData skill, BattleUnit linkPartner = null)
        {
            if (user == null || skill == null)
            {
                return;
            }

            int mpCost = Mathf.Max(0, skill.MpCost);
            ConsumeMP(user, mpCost, skill.SkillName, "user");

            if (skill.SkillKind == SkillKind.Link && linkPartner != null)
            {
                ConsumeMP(linkPartner, mpCost, skill.SkillName, "link partner");
            }
        }

        private static void ConsumeMP(BattleUnit unit, int mpCost, string skillName, string role)
        {
            if (unit == null)
            {
                return;
            }

            unit.CurrentMP = Mathf.Max(0, unit.CurrentMP - mpCost);
            Debug.Log($"[MP] {unit.Name} paid {mpCost} MP as {role} for {skillName}. MP {unit.CurrentMP}/{unit.Data.MaxMP}.");
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

        // Phase transitions
        private void EnterResolvingAction()
        {
            if (_battleEnded)
            {
                return;
            }

            _phase = BattlePhase.ResolvingAction;
            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            SetActionOverlayVisible(true);

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
            EnsureEnemyActionStatesForPreview();
            UpdateEnemyActionPreview();
            RedrawEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(true);
            HideActionOverlay();
            SetCommandUiVisible(true);

            if (commandPanel != null)
            {
                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
                commandPanel.SetInteractable(true);
            }

            if (rotateButton != null)
            {
                rotateButton.interactable = true;
            }
        }

        // Player actions
        private void HandleSkillClicked(SkillData skill)
        {
            if (!CanAcceptPlayerCommand())
            {
                return;
            }

            if (!CanUseSkill(_active, skill))
            {
                return;
            }

            BattleUnit linkPartner = GetLinkPartnerForSkill(_active, skill);

            EnterResolvingAction();
            ConsumeSkillMP(_active, skill, linkPartner);

            ShowActionOverlay(skill.SkillName, BuildSkillUserDisplayName(_active, linkPartner));
            PrepareSkillActionFlashTargets(skill);
            BattleUnit flashableLinkPartner = IsActiveAllyUnit(linkPartner) ? linkPartner : null;
            SetPendingActionSourceFlashTargets(true, BuildSkillSourceFlashTargets(_active, flashableLinkPartner));
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {BuildSkillUserDisplayName(_active, linkPartner)}.");

            ApplySkillDamage(skill);
            ApplySkillEffect(skill);

            if (_battleEnded)
            {
                RedrawBoard();
                return;
            }

            StartCoroutine(FinishPlayerActionAfterDelay());
        }

        // Skill effects and damage
        private void ApplySkillDamage(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            List<DefeatedEnemyInfo> defeatedEnemies = new();
            List<GridPos> targets = GetSkillDamageTargetPositions(skill);

            for (int i = 0; i < targets.Count; i++)
            {
                DamageEnemyAt(targets[i], skill.Damage, defeatedEnemies);
            }

            ResolveDefeatedEnemies(defeatedEnemies);
        }

        private void ApplySkillEffect(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            switch (skill.EffectType)
            {
                case SkillEffectType.None:
                    return;

                case SkillEffectType.ApplyBuff:
                    List<BattleUnit> effectTargets = GetSkillEffectTargets(skill);
                    for (int i = 0; i < effectTargets.Count; i++)
                    {
                        ApplyBuff(effectTargets[i], skill.BuffType, skill.BuffTurns);
                    }
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
            AddPendingActionValuePopup(false, pos, $"-{finalDamage}");

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

        // Buff handling
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

        // KO and replacement handling
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


        // Enemy action preview
        private void RedrawEnemyActionPreviewHighlights()
        {
            ResetEnemyActionPreviewHighlights();

            if (_battleEnded || _phase != BattlePhase.CommandSelect)
            {
                return;
            }

            BattleUnit nextEnemy = FindNextUnactedEnemy();
            if (nextEnemy == null)
            {
                return;
            }

            EnemyActionState action = GetPreviewEnemyActionState(nextEnemy);
            if (action == null || action.Skill == null)
            {
                return;
            }

            HighlightEnemyActionTargets(nextEnemy, action);
        }

        private BattleUnit FindNextUnactedEnemy()
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

                if (_enemies.Contains(unit))
                {
                    return unit;
                }
            }

            return null;
        }

        private void ResetEnemyActionPreviewHighlights()
        {
            ResetAllyBoardHighlights();
            RedrawActiveHighlights();
        }

        private void HighlightEnemyActionTargets(BattleUnit enemy, EnemyActionState action)
        {
            if (enemy == null || action == null || action.Skill == null)
            {
                return;
            }

            switch (action.Skill.TargetPattern)
            {
                case SkillTargetPattern.SameGridPosOpponent:
                    SetAllyBoardCellColor(enemy.GridPos, EnemyActionPreviewCellColor);
                    break;

                case SkillTargetPattern.FrontTopOpponent:
                    SetAllyBoardCellColor(GridPos.FrontTop, EnemyActionPreviewCellColor);
                    break;

                case SkillTargetPattern.FrontBottomOpponent:
                    SetAllyBoardCellColor(GridPos.FrontBottom, EnemyActionPreviewCellColor);
                    break;

                case SkillTargetPattern.BothFrontOpponents:
                    SetAllyBoardCellColor(GridPos.FrontTop, EnemyActionPreviewCellColor);
                    SetAllyBoardCellColor(GridPos.FrontBottom, EnemyActionPreviewCellColor);
                    break;

                case SkillTargetPattern.AllOpponents:
                    SetAllyBoardCellColor(GridPos.FrontTop, EnemyActionPreviewCellColor);
                    SetAllyBoardCellColor(GridPos.BackTop, EnemyActionPreviewCellColor);
                    SetAllyBoardCellColor(GridPos.FrontBottom, EnemyActionPreviewCellColor);
                    SetAllyBoardCellColor(GridPos.BackBottom, EnemyActionPreviewCellColor);
                    break;
            }

            if (_active != null)
            {
                SetAllyBoardCellColor(_active.GridPos, ActiveCellColor);
            }
        }

        // Player preview
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
                case SkillTargetPattern.FrontTopOpponent:
                    SetEnemyBoardCellColor(GridPos.FrontTop, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.FrontBottomOpponent:
                    SetEnemyBoardCellColor(GridPos.FrontBottom, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.BothFrontOpponents:
                    SetEnemyBoardCellColor(GridPos.FrontTop, TargetPreviewCellColor);
                    SetEnemyBoardCellColor(GridPos.FrontBottom, TargetPreviewCellColor);
                    break;

                case SkillTargetPattern.AllOpponents:
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

        private void HandleItemClicked(InventoryItem inventoryItem)
        {
            if (!CanAcceptPlayerCommand())
            {
                return;
            }

            if (inventoryItem == null || inventoryItem.Item == null)
            {
                return;
            }

            if (inventoryItem.Count <= 0)
            {
                Debug.Log($"[Item] No {inventoryItem.Item.ItemName} left. Item cannot be used.");
                return;
            }

            switch (inventoryItem.Item.Kind)
            {
                case ItemKind.Pass:
                    HandlePassItem(inventoryItem);
                    return;

                case ItemKind.Heal:
                default:
                    HandleHealItem(inventoryItem);
                    return;
            }
        }

        private void HandlePassItem(InventoryItem inventoryItem)
        {
            ItemData item = inventoryItem.Item;

            EnterResolvingAction();

            inventoryItem.Count--;

            if (commandPanel != null)
            {
                commandPanel.RefreshItems();
            }

            ShowActionOverlay(item.ItemName, _active == null ? "" : _active.Name);
            ClearPendingActionFlashTargets();
            ClearPendingActionValuePopups();

            Debug.Log($"[Action] Item used: {item.ItemName}. No MP spent and no extra MP recovered. Remaining: {inventoryItem.Count}");

            StartCoroutine(FinishPlayerActionAfterDelay());
        }

        private void HandleHealItem(InventoryItem inventoryItem)
        {
            ItemData item = inventoryItem.Item;

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

            inventoryItem.Count--;

            if (commandPanel != null)
            {
                commandPanel.RefreshItems();
            }

            ShowActionOverlay(item.ItemName, _active.Name);
            SetPendingActionFlashTargets(true, new List<GridPos> { target.GridPos });
            SetPendingActionSourceFlashTargets(true, new List<GridPos> { _active.GridPos });
            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}");
            Debug.Log($"[Action] Item used: {item.ItemName} -> {target.Name} healed {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}. Remaining: {inventoryItem.Count}");

            StartCoroutine(FinishPlayerActionAfterDelay());
        }

        // Action animation and value popups
        private List<GridPos> GetSkillDamageTargetPositions(SkillData skill)
        {
            var targets = new List<GridPos>();

            if (skill == null)
            {
                return targets;
            }

            switch (skill.TargetPattern)
            {
                case SkillTargetPattern.FrontTopOpponent:
                    targets.Add(GridPos.FrontTop);
                    break;

                case SkillTargetPattern.FrontBottomOpponent:
                    targets.Add(GridPos.FrontBottom);
                    break;

                case SkillTargetPattern.BothFrontOpponents:
                    targets.Add(GridPos.FrontTop);
                    targets.Add(GridPos.FrontBottom);
                    break;

                case SkillTargetPattern.AllOpponents:
                    AddAllGridPositions(targets);
                    break;

                case SkillTargetPattern.Self:
                    // Self skills are effect/animation targets, not enemy damage targets.
                    break;
            }

            return targets;
        }

        private List<BattleUnit> GetSkillEffectTargets(SkillData skill)
        {
            var targets = new List<BattleUnit>();

            if (skill == null || _active == null || _active.IsDead)
            {
                return targets;
            }

            switch (skill.EffectTarget)
            {
                case SkillEffectTargetType.Self:
                    targets.Add(_active);
                    break;

                case SkillEffectTargetType.Target:
                    // Reserved for future ally/enemy effect targeting.
                    break;

                case SkillEffectTargetType.AllAllies:
                    // Reserved for future party-wide effects.
                    break;

                case SkillEffectTargetType.AllEnemies:
                    // Reserved for future enemy-wide effects.
                    break;
            }

            return targets;
        }

        private List<GridPos> GetSkillAnimationTargetPositions(SkillData skill)
        {
            var targets = new List<GridPos>();

            if (skill == null)
            {
                return targets;
            }

            switch (skill.TargetPattern)
            {
                case SkillTargetPattern.FrontTopOpponent:
                    targets.Add(GridPos.FrontTop);
                    break;

                case SkillTargetPattern.FrontBottomOpponent:
                    targets.Add(GridPos.FrontBottom);
                    break;

                case SkillTargetPattern.BothFrontOpponents:
                    targets.Add(GridPos.FrontTop);
                    targets.Add(GridPos.FrontBottom);
                    break;

                case SkillTargetPattern.AllOpponents:
                    AddAllGridPositions(targets);
                    break;

                case SkillTargetPattern.Self:
                    if (_active != null)
                    {
                        targets.Add(_active.GridPos);
                    }
                    break;
            }

            return targets;
        }

        private List<GridPos> GetEnemyActionTargetPositions(BattleUnit enemy, EnemyActionState action)
        {
            var targets = new List<GridPos>();

            if (enemy == null || action == null || action.Skill == null)
            {
                return targets;
            }

            switch (action.Skill.TargetPattern)
            {
                case SkillTargetPattern.SameGridPosOpponent:
                    targets.Add(enemy.GridPos);
                    break;

                case SkillTargetPattern.FrontTopOpponent:
                    targets.Add(GridPos.FrontTop);
                    break;

                case SkillTargetPattern.FrontBottomOpponent:
                    targets.Add(GridPos.FrontBottom);
                    break;

                case SkillTargetPattern.BothFrontOpponents:
                    targets.Add(GridPos.FrontTop);
                    targets.Add(GridPos.FrontBottom);
                    break;

                case SkillTargetPattern.AllOpponents:
                    AddAllGridPositions(targets);
                    break;
            }

            return targets;
        }

        private static void AddAllGridPositions(List<GridPos> targets)
        {
            if (targets == null)
            {
                return;
            }

            targets.Add(GridPos.FrontTop);
            targets.Add(GridPos.BackTop);
            targets.Add(GridPos.FrontBottom);
            targets.Add(GridPos.BackBottom);
        }

        private void PrepareSkillActionFlashTargets(SkillData skill)
        {
            if (skill == null)
            {
                ClearPendingActionFlashTargets();
                return;
            }

            bool isAllyBoard = skill.TargetPattern == SkillTargetPattern.Self;
            List<GridPos> targets = GetSkillAnimationTargetPositions(skill);

            if (targets.Count == 0)
            {
                ClearPendingActionFlashTargets();
                return;
            }

            SetPendingActionFlashTargets(isAllyBoard, targets);
        }

        private void PrepareEnemyActionFlashTargets(BattleUnit enemy, EnemyActionState action)
        {
            if (enemy == null || action == null || action.Skill == null)
            {
                ClearPendingActionFlashTargets();
                return;
            }

            List<GridPos> targets = GetEnemyActionTargetPositions(enemy, action);

            if (targets.Count == 0)
            {
                ClearPendingActionFlashTargets();
                return;
            }

            SetPendingActionFlashTargets(true, targets);
        }

        private void SetPendingActionFlashTargets(bool isAllyBoard, List<GridPos> targets)
        {
            _pendingActionFlashIsAllyBoard = isAllyBoard;
            _pendingActionFlashTargets.Clear();

            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                GridPos pos = targets[i];

                if (_pendingActionFlashTargets.Contains(pos))
                {
                    continue;
                }

                _pendingActionFlashTargets.Add(pos);
            }
        }

        private void SetPendingActionSourceFlashTargets(bool isAllyBoard, List<GridPos> targets)
        {
            _pendingActionSourceFlashIsAllyBoard = isAllyBoard;
            _pendingActionSourceFlashTargets.Clear();

            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                GridPos pos = targets[i];

                if (_pendingActionSourceFlashTargets.Contains(pos))
                {
                    continue;
                }

                _pendingActionSourceFlashTargets.Add(pos);
            }
        }

        private void ClearPendingActionSourceFlashTargets()
        {
            _pendingActionSourceFlashTargets.Clear();
        }

        private void ClearPendingActionFlashTargets()
        {
            _pendingActionFlashTargets.Clear();
            ClearPendingActionSourceFlashTargets();
        }

        private void AddPendingActionValuePopup(bool isAllyBoard, GridPos position, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _pendingActionValuePopups.Add(new ActionValuePopup
            {
                IsAllyBoard = isAllyBoard,
                Position = position,
                Text = text
            });
        }

        private void ClearPendingActionValuePopups()
        {
            _pendingActionValuePopups.Clear();
        }

        private void ShowPendingActionValuePopups()
        {
            HideActiveActionValuePopups();

            if (_pendingActionValuePopups.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingActionValuePopups.Count; i++)
            {
                ActionValuePopup popup = _pendingActionValuePopups[i];
                if (popup == null)
                {
                    continue;
                }

                TMP_Text cellLabel = GetBoardCellLabel(popup.IsAllyBoard, popup.Position);
                if (cellLabel == null || cellLabel.transform.parent == null)
                {
                    continue;
                }

                GameObject popupObject = new GameObject("ActionValuePopup");
                popupObject.transform.SetParent(cellLabel.transform.parent, false);

                RectTransform rect = popupObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.55f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                TMP_Text label = popupObject.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 28f;
                label.raycastTarget = false;
                label.text = popup.Text;
                label.color = popup.Text.StartsWith("+")
                    ? healPopupColor
                    : damagePopupColor;

                _activeActionValuePopupLabels.Add(label);
            }
        }

        private void HideActiveActionValuePopups()
        {
            for (int i = 0; i < _activeActionValuePopupLabels.Count; i++)
            {
                TMP_Text label = _activeActionValuePopupLabels[i];
                if (label == null)
                {
                    continue;
                }

                Destroy(label.gameObject);
            }

            _activeActionValuePopupLabels.Clear();
        }

        private TMP_Text GetBoardCellLabel(bool isAllyBoard, GridPos position)
        {
            if (isAllyBoard)
            {
                switch (position)
                {
                    case GridPos.FrontTop:
                        return allyFrontTop;

                    case GridPos.BackTop:
                        return allyBackTop;

                    case GridPos.FrontBottom:
                        return allyFrontBottom;

                    case GridPos.BackBottom:
                        return allyBackBottom;

                    default:
                        return null;
                }
            }

            switch (position)
            {
                case GridPos.FrontTop:
                    return enemyBackTop;

                case GridPos.BackTop:
                    return enemyFrontTop;

                case GridPos.FrontBottom:
                    return enemyBackBottom;

                case GridPos.BackBottom:
                    return enemyFrontBottom;

                default:
                    return null;
            }
        }

        private IEnumerator PlayPendingActionFlashOrDelay()
        {
            float duration = Mathf.Max(0f, actionResolveDelaySeconds);

            bool hasTargetFlash = _pendingActionFlashTargets.Count > 0;
            bool hasSourceFlash = _pendingActionSourceFlashTargets.Count > 0;

            if (!hasTargetFlash && !hasSourceFlash)
            {
                ShowPendingActionValuePopups();

                if (duration > 0f)
                {
                    yield return new WaitForSeconds(duration);
                }

                HideActiveActionValuePopups();
                ClearPendingActionValuePopups();
                yield break;
            }

            bool isTargetAllyBoard = _pendingActionFlashIsAllyBoard;
            bool isSourceAllyBoard = _pendingActionSourceFlashIsAllyBoard;

            List<GridPos> targetPositions = new(_pendingActionFlashTargets);
            List<GridPos> sourcePositions = new(_pendingActionSourceFlashTargets);

            ClearPendingActionFlashTargets();

            if (duration <= 0f)
            {
                yield break;
            }

            int blinkCount = Mathf.Max(1, actionFlashCount);
            float interval = duration / (blinkCount * 2f);

            ShowPendingActionValuePopups();

            for (int i = 0; i < blinkCount; i++)
            {
                SetActionSourceFlashTargetsVisible(isSourceAllyBoard, sourcePositions, true);
                SetActionFlashTargetsVisible(isTargetAllyBoard, targetPositions, true);
                yield return new WaitForSeconds(interval);

                SetActionSourceFlashTargetsVisible(isSourceAllyBoard, sourcePositions, false);
                SetActionFlashTargetsVisible(isTargetAllyBoard, targetPositions, false);
                yield return new WaitForSeconds(interval);
            }

            HideActiveActionValuePopups();
            ClearPendingActionValuePopups();
        }

        private void SetActionSourceFlashTargetsVisible(bool isAllyBoard, List<GridPos> targets, bool visible)
        {
            SetActionFlashTargetsVisible(isAllyBoard, targets, visible, ActionSourceFlashCellColor);
        }

        private void SetActionFlashTargetsVisible(bool isAllyBoard, List<GridPos> targets, bool visible)
        {
            SetActionFlashTargetsVisible(isAllyBoard, targets, visible, ActionFlashCellColor);
        }

        private void SetActionFlashTargetsVisible(bool isAllyBoard, List<GridPos> targets, bool visible, Color flashColor)
        {
            if (targets == null)
            {
                return;
            }

            if (!visible)
            {
                if (isAllyBoard)
                {
                    ResetAllyBoardHighlights();
                }
                else
                {
                    ResetEnemyBoardHighlights();
                }

                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                GridPos pos = targets[i];

                if (isAllyBoard)
                {
                    SetAllyBoardCellColor(pos, flashColor);
                }
                else
                {
                    SetEnemyBoardCellColor(pos, flashColor);
                }
            }
        }


        // Turn progression
        private IEnumerator FinishPlayerActionAfterDelay()
        {
            yield return PlayPendingActionFlashOrDelay();

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

        private IEnumerator ResolveEnemyActionAndAdvance(BattleUnit enemy)
        {
            if (enemy == null || enemy.IsDead || _battleEnded)
            {
                yield break;
            }

            EnterResolvingAction();

            EnemyActionState action = GetPreviewEnemyActionState(enemy);

            _actedUnits.Add(enemy);
            ExecuteEnemyAction(enemy, action);
            ClearPreviewEnemyActionState(enemy);
            RedrawBoard();

            yield return PlayPendingActionFlashOrDelay();

            if (_battleEnded)
            {
                yield break;
            }

            AdvanceToNextActor();
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
                    StartCoroutine(ResolveEnemyActionAndAdvance(nextUnit));
                    return;
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

        private void ExecuteEnemyAction(BattleUnit enemy, EnemyActionState action)
        {
            if (enemy == null || enemy.IsDead || action == null || action.Skill == null || _battleEnded)
            {
                return;
            }

            ShowActionOverlay(action.Skill.SkillName, enemy.Name);
            PrepareEnemyActionFlashTargets(enemy, action);
            SetPendingActionSourceFlashTargets(false, new List<GridPos> { enemy.GridPos });

            List<GridPos> targets = GetEnemyActionTargetPositions(enemy, action);
            for (int i = 0; i < targets.Count; i++)
            {
                DamageAllyAt(targets[i], action.Skill.Damage, enemy, action.Skill.SkillName);
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
            AddPendingActionValuePopup(true, targetPosition, $"-{finalDamage}");

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
                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
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
                EndWaveClear();
                return;
            }

            if (!HasAliveActiveAllies() && !HasAliveAllyReserves())
            {
                EndBattle("Defeat");
            }
        }

        private void EndWaveClear()
        {
            WaveClearResult result = CreateWaveClearResult();
            ApplyKakeraReward(result);
            ApplyWaveClearRewards(result);

            _battleEnded = true;
            _phase = BattlePhase.BattleEnded;
            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            HideActionOverlay();
            ShowWaveResultPanel(result);
            RedrawBoard();

            Debug.Log($"[Battle] Clear: {FormatWaveClearRank(result.Rank)}, Kakera +{CalculateKakeraGain(result.Rank)}.");
        }

        private WaveClearResult CreateWaveClearResult()
        {
            EnsureWaveProgress();

            WaveClearRank rank = EvaluateWaveClearRank();
            int distanceGain = CalculateWaveDistanceGain(rank);
            int currentDistance = _waveProgress.AddDistance(distanceGain);

            bool hasNextWave = _questProgress != null && _questProgress.HasNextWave;

            WaveClearResult result = new WaveClearResult
            {
                Rank = rank,
                DistanceGain = distanceGain,
                CurrentDistance = currentDistance,
                TargetDistance = _waveProgress.TargetDistance,
                PartyHealAmount = rank == WaveClearRank.OneTurn
                    ? _oneTurnClearPartyHeal
                    : 0,
                WaveNumber = GetCurrentWaveNumber(),
                TotalWaves = GetTotalWaveCount(),
                HasNextWave = hasNextWave
            };

            if (!hasNextWave)
            {
                result.QuestResult = CreateQuestResultData(result);
            }

            return result;
        }

        private void EnsureWaveProgress()
        {
            if (_waveProgress == null)
            {
                _waveProgress = new WaveProgressState(DefaultTargetDistance, DefaultBaseWaveDistance);
            }
        }

        private int GetCurrentWaveNumber()
        {
            if (_questProgress == null)
            {
                return 1;
            }

            return _questProgress.CurrentWaveIndex + 1;
        }

        private int GetTotalWaveCount()
        {
            if (_questProgress == null || _questProgress.Quest == null)
            {
                return 1;
            }

            return Mathf.Max(1, _questProgress.Quest.Waves.Count);
        }

        private void ApplyKakeraReward(WaveClearResult result)
        {
            if (result == null)
            {
                return;
            }

            int gain = CalculateKakeraGain(result.Rank);
            int before = _kakeraStock;

            _kakeraStock = Mathf.Clamp(_kakeraStock + gain, 0, MaxKakeraStock);
            _totalKakeraEarned += Mathf.Max(0, gain);

            Debug.Log($"[Kakera] Gain +{gain}. Stock {before}->{_kakeraStock}/{MaxKakeraStock}, TotalEarned={_totalKakeraEarned}.");
        }
        private void ApplyWaveClearRewards(WaveClearResult result)
        {
            if (result == null || result.PartyHealAmount <= 0)
            {
                return;
            }

            int eligibleCount = CountLivingPartyMembers(_allies) + CountLivingPartyMembers(_reserves);
            int changedCount = HealLivingPartyMembers(_allies, result.PartyHealAmount)
                + HealLivingPartyMembers(_reserves, result.PartyHealAmount);

            Debug.Log($"[Battle] 1Turn Kill HP bonus applied. Eligible={eligibleCount}, Changed={changedCount}, Amount=+{result.PartyHealAmount}.");
        }

        private WaveClearRank EvaluateWaveClearRank()
        {
            EnsureWaveProgress();

            int waveTurn = _waveProgress.WaveTurn;

            if (waveTurn <= 1)
            {
                return WaveClearRank.OneTurn;
            }

            if (waveTurn == 2)
            {
                return WaveClearRank.TwoTurn;
            }

            if (waveTurn == 3)
            {
                return WaveClearRank.ThreeTurn;
            }

            return WaveClearRank.FourPlusTurn;
        }

        private int CalculateWaveDistanceGain(WaveClearRank rank)
        {
            EnsureWaveProgress();

            float multiplier = rank switch
            {
                WaveClearRank.OneTurn => 2.0f,
                WaveClearRank.TwoTurn => 1.5f,
                WaveClearRank.ThreeTurn => 1.2f,
                _ => 1.0f
            };

            return Mathf.RoundToInt(_waveProgress.BaseWaveDistance * multiplier);
        }

        private static string FormatWaveClearRank(WaveClearRank rank)
        {
            return rank switch
            {
                WaveClearRank.OneTurn => "1Turn Kill",
                WaveClearRank.TwoTurn => "2Turn Kill",
                _ => "3+ Turn"
            };
        }

        private static int HealLivingPartyMembers(List<BattleUnit> units, int healAmount)
        {
            if (units == null || healAmount <= 0)
            {
                return 0;
            }

            int changedCount = 0;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || unit.IsDead || unit.Data == null)
                {
                    continue;
                }

                int beforeHp = unit.CurrentHP;
                unit.CurrentHP = Mathf.Min(unit.Data.MaxHP, unit.CurrentHP + healAmount);

                if (unit.CurrentHP != beforeHp)
                {
                    changedCount++;
                    Debug.Log($"[Battle] {unit.Name} recovered HP {beforeHp}->{unit.CurrentHP}/{unit.Data.MaxHP}.");
                }
                else
                {
                    Debug.Log($"[Battle] {unit.Name} was eligible for HP reward but stayed at {unit.CurrentHP}/{unit.Data.MaxHP}.");
                }
            }

            return changedCount;
        }

        private static int CountLivingPartyMembers(List<BattleUnit> units)
        {
            if (units == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || unit.IsDead || unit.Data == null)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private QuestResultData CreateQuestResultData(WaveClearResult waveResult)
        {
            QuestResultData questResult = new QuestResultData
            {
                ClearedWaveCount = waveResult == null ? GetCurrentWaveNumber() : waveResult.WaveNumber,
                TotalWaveCount = GetTotalWaveCount(),
                CurrentDistance = _waveProgress == null ? 0 : _waveProgress.CurrentDistance,
                TargetDistance = _waveProgress == null ? DefaultTargetDistance : _waveProgress.TargetDistance,
                AlivePartyCount = CountLivingPartyMembers(_partyMembers),
                KnockedOutPartyCount = CountKnockedOutPartyMembers(_partyMembers),
                TotalPartyCount = CountKnownPartyMembers(_partyMembers),
                ReturnsToBase = true
            };

            Debug.Log($"[Quest] Result created. Waves={questResult.ClearedWaveCount}/{questResult.TotalWaveCount}, Distance={questResult.CurrentDistance}/{questResult.TargetDistance}, Alive={questResult.AlivePartyCount}, KO={questResult.KnockedOutPartyCount}.");

            return questResult;
        }

        private static int CountKnockedOutPartyMembers(List<BattleUnit> units)
        {
            if (units == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || unit.Data == null || !unit.IsDead)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static int CountKnownPartyMembers(List<BattleUnit> units)
        {
            if (units == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit unit = units[i];
                if (unit == null || unit.Data == null)
                {
                    continue;
                }

                count++;
            }

            return count;
        }
        private void EndBattle(string result)
        {
            _battleEnded = true;
            _phase = BattlePhase.BattleEnded;
            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            HideActionOverlay();
            ShowResultPanel(result);
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

            _previewEnemyActionStates.Clear();

            EnsureWaveProgress();
            _waveProgress.AdvanceTurn();
            RecoverAllAllyMP();
            TickBuffsAtTurnStart();

            RebuildTurnOrder();

            BattleUnit nextAlly = FindNextUnactedAlly();
            if (nextAlly != null)
            {
                EnterCommandSelect(nextAlly);
            }

            RedrawBoard();
            Debug.Log($"[Turn] New turn started. WaveTurn={_waveProgress.WaveTurn}.");
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

            if (!_battleEnded && _phase == BattlePhase.CommandSelect)
            {
                UpdateEnemyActionPreview();
                RedrawEnemyActionPreviewHighlights();
                SetEnemyActionPreviewVisible(true);
            }

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
            SetActionOverlayVisible(true);
            SetActionOverlayText(skillName, userName);
        }

        private void SetActionOverlayText(string skillName, string userName)
        {
            TMP_Text skillLabel = actionSkillName;
            TMP_Text userLabel = actionUserName;

            GameObject topPanel = FindUiGameObjectByName("TopActionPanel");
            if (topPanel != null)
            {
                TMP_Text[] labels = topPanel.GetComponentsInChildren<TMP_Text>(true);

                for (int i = 0; i < labels.Length; i++)
                {
                    TMP_Text label = labels[i];
                    if (label == null)
                    {
                        continue;
                    }

                    string lowerName = label.name.ToLowerInvariant();

                    if (lowerName.Contains("skill"))
                    {
                        skillLabel = label;
                    }
                    else if (lowerName.Contains("user"))
                    {
                        userLabel = label;
                    }
                }
            }

            if (skillLabel != null)
            {
                skillLabel.gameObject.SetActive(true);
                skillLabel.text = skillName;
            }

            if (userLabel != null)
            {
                userLabel.gameObject.SetActive(true);
                userLabel.text = userName;
            }
        }

        private void HideActionOverlay()
        {
            if (actionSkillName != null)
            {
                actionSkillName.text = "";
            }

            if (actionUserName != null)
            {
                actionUserName.text = "";
            }

            SetActionOverlayVisible(false);
        }

        private void SetActionOverlayVisible(bool visible)
        {
            SetUiObjectsByNameVisible("TopActionPanel", visible);

            // BossNamePlate is not used yet, so keep it hidden for every phase.
            SetUiObjectsByNameVisible("BossNamePlate", false);

            if (actionSkillName != null)
            {
                actionSkillName.gameObject.SetActive(visible);
            }

            if (actionUserName != null)
            {
                actionUserName.gameObject.SetActive(visible);
            }
        }

        private GameObject FindUiGameObjectByName(string objectName)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null || candidate.name != objectName)
                {
                    continue;
                }

                GameObject candidateObject = candidate.gameObject;
                if (!candidateObject.scene.IsValid())
                {
                    continue;
                }

                return candidateObject;
            }

            return null;
        }

        private void SetUiObjectsByNameVisible(string objectName, bool visible)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null || candidate.name != objectName)
                {
                    continue;
                }

                GameObject candidateObject = candidate.gameObject;

                if (!candidateObject.scene.IsValid())
                {
                    continue;
                }

                candidateObject.SetActive(visible);
            }
        }

        private void SetCommandUiVisible(bool visible)
        {
            if (commandPanel != null)
            {
                commandPanel.gameObject.SetActive(visible);
            }

            if (rotateButton != null)
            {
                rotateButton.gameObject.SetActive(visible);
            }
        }

        private void EnsureEnemyActionPreviewPanel()
        {
            if (_enemyActionPreviewPanelObject != null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null && commandPanel != null)
            {
                canvas = commandPanel.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            GameObject existing = FindUiGameObjectByName("EnemyActionPreviewPanel");
            if (existing != null)
            {
                _enemyActionPreviewPanelObject = existing;
                _enemyActionPreviewText = existing.GetComponentInChildren<TMP_Text>(true);
                return;
            }

            _enemyActionPreviewPanelObject = new GameObject("EnemyActionPreviewPanel");
            _enemyActionPreviewPanelObject.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = _enemyActionPreviewPanelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.74f);
            panelRect.anchorMax = new Vector2(0.28f, 0.96f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = _enemyActionPreviewPanelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject textObject = new GameObject("EnemyActionPreviewText");
            textObject.transform.SetParent(_enemyActionPreviewPanelObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            _enemyActionPreviewText = textObject.AddComponent<TextMeshProUGUI>();
            _enemyActionPreviewText.alignment = TextAlignmentOptions.TopLeft;
            _enemyActionPreviewText.fontSize = 18f;
            _enemyActionPreviewText.raycastTarget = false;
        }

        private string BuildEnemyActionPreviewLine(BattleUnit enemy, EnemyActionState action, bool isNext)
        {
            if (enemy == null || action == null || action.Skill == null)
            {
                return "-";
            }

            string prefix = isNext ? "NEXT > " : "";
            return $"{prefix}{enemy.Name}: {action.Skill.SkillName} -> {BuildEnemyActionTargetText(enemy, action)}";
        }

        private string BuildEnemyActionTargetText(BattleUnit enemy, EnemyActionState action)
        {
            if (action == null || action.Skill == null)
            {
                return "Unknown";
            }

            switch (action.Skill.TargetPattern)
            {
                case SkillTargetPattern.SameGridPosOpponent:
                    return enemy == null
                        ? "Ally same position"
                        : $"Ally {FormatEnemyPreviewGridPos(enemy.GridPos)}";

                case SkillTargetPattern.FrontTopOpponent:
                    return "Ally FrontTop";

                case SkillTargetPattern.FrontBottomOpponent:
                    return "Ally FrontBottom";

                case SkillTargetPattern.BothFrontOpponents:
                    return "Ally front row";

                case SkillTargetPattern.AllOpponents:
                    return "All allies";

                default:
                    return "Unknown";
            }
        }

        private static string FormatEnemyPreviewGridPos(GridPos pos)
        {
            switch (pos)
            {
                case GridPos.FrontTop:
                    return "FrontTop";

                case GridPos.BackTop:
                    return "BackTop";

                case GridPos.FrontBottom:
                    return "FrontBottom";

                case GridPos.BackBottom:
                    return "BackBottom";

                default:
                    return pos.ToString();
            }
        }

        private void UpdateEnemyActionPreview()
        {
            EnsureEnemyActionPreviewPanel();

            if (_enemyActionPreviewText == null)
            {
                return;
            }

            List<string> lines = new()
            {
                "Enemy Actions"
            };

            BattleUnit nextEnemy = FindNextUnactedEnemy();

            for (int i = 0; i < _enemies.Count; i++)
            {
                BattleUnit enemy = _enemies[i];

                if (enemy == null || enemy.IsDead || _actedUnits.Contains(enemy))
                {
                    continue;
                }

                EnemyActionState action = GetPreviewEnemyActionState(enemy);
                if (action == null || action.Skill == null)
                {
                    continue;
                }

                lines.Add(BuildEnemyActionPreviewLine(enemy, action, enemy == nextEnemy));
            }

            if (lines.Count == 1)
            {
                lines.Add("-");
            }

            _enemyActionPreviewText.text = string.Join("\n", lines);
        }

        private void SetEnemyActionPreviewVisible(bool visible)
        {
            EnsureEnemyActionPreviewPanel();

            if (_enemyActionPreviewPanelObject != null)
            {
                _enemyActionPreviewPanelObject.SetActive(visible);
            }
        }

        // Result UI
        private void EnsureResultPanel()
        {
            if (_resultPanelObject != null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null && commandPanel != null)
            {
                canvas = commandPanel.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            GameObject existing = FindUiGameObjectByName("ResultPanel");
            if (existing != null)
            {
                _resultPanelObject = existing;
                TMP_Text[] labels = _resultPanelObject.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i] == null)
                    {
                        continue;
                    }

                    string lowerName = labels[i].name.ToLowerInvariant();
                    if (lowerName.Contains("title"))
                    {
                        _resultTitleText = labels[i];
                    }
                    else if (lowerName.Contains("sub"))
                    {
                        _resultSubText = labels[i];
                    }
                }

                TryBindExistingResultFormationButton();
                TryBindExistingResultReturnButton();
                ApplyResultPanelLayout();
                return;
            }

            _resultPanelObject = new GameObject("ResultPanel");
            _resultPanelObject.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = _resultPanelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.32f, 0.28f);
            panelRect.anchorMax = new Vector2(0.68f, 0.72f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = _resultPanelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.78f);

            GameObject titleObject = new GameObject("ResultTitle");
            titleObject.transform.SetParent(_resultPanelObject.transform, false);

            _resultTitleText = titleObject.AddComponent<TextMeshProUGUI>();
            _resultTitleText.alignment = TextAlignmentOptions.Center;
            _resultTitleText.fontSize = 38f;
            _resultTitleText.raycastTarget = false;

            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.70f);
            titleRect.anchorMax = new Vector2(1f, 0.90f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            GameObject subObject = new GameObject("ResultSubText");
            subObject.transform.SetParent(_resultPanelObject.transform, false);

            _resultSubText = subObject.AddComponent<TextMeshProUGUI>();
            _resultSubText.alignment = TextAlignmentOptions.Center;
            _resultSubText.fontSize = 22f;
            _resultSubText.raycastTarget = false;

            RectTransform subRect = subObject.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.28f);
            subRect.anchorMax = new Vector2(1f, 0.68f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            CreateResultFormationButton();
            CreateResultReturnButton();
            ApplyResultPanelLayout();
        }

        private void ApplyResultPanelLayout()
        {
            if (_resultPanelObject != null)
            {
                RectTransform panelRect = _resultPanelObject.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.32f, 0.28f);
                    panelRect.anchorMax = new Vector2(0.68f, 0.72f);
                    panelRect.offsetMin = Vector2.zero;
                    panelRect.offsetMax = Vector2.zero;
                }
            }

            if (_resultTitleText != null)
            {
                _resultTitleText.fontSize = 38f;

                RectTransform titleRect = _resultTitleText.GetComponent<RectTransform>();
                if (titleRect != null)
                {
                    titleRect.anchorMin = new Vector2(0f, 0.70f);
                    titleRect.anchorMax = new Vector2(1f, 0.90f);
                    titleRect.offsetMin = Vector2.zero;
                    titleRect.offsetMax = Vector2.zero;
                }
            }

            if (_resultSubText != null)
            {
                _resultSubText.fontSize = 22f;

                RectTransform subRect = _resultSubText.GetComponent<RectTransform>();
                if (subRect != null)
                {
                    subRect.anchorMin = new Vector2(0f, 0.28f);
                    subRect.anchorMax = new Vector2(1f, 0.68f);
                    subRect.offsetMin = Vector2.zero;
                    subRect.offsetMax = Vector2.zero;
                }
            }

            ApplyResultButtonLayout(_resultFormationButton, 0.08f, 0.46f);
            ApplyResultButtonLayout(_resultReturnButton, 0.54f, 0.92f);
        }

        private static void ApplyResultButtonLayout(Button button, float minX, float maxX)
        {
            if (button == null)
            {
                return;
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect == null)
            {
                return;
            }

            buttonRect.anchorMin = new Vector2(minX, 0.08f);
            buttonRect.anchorMax = new Vector2(maxX, 0.22f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
        }

        private void TryBindExistingResultFormationButton()
        {
            if (_resultPanelObject == null)
            {
                return;
            }

            Transform buttonTransform = _resultPanelObject.transform.Find("FormationButton");
            if (buttonTransform == null)
            {
                CreateResultFormationButton();
                return;
            }

            _resultFormationButton = buttonTransform.GetComponent<Button>();
            _resultFormationButtonText = buttonTransform.GetComponentInChildren<TMP_Text>(true);

            if (_resultFormationButton != null)
            {
                _resultFormationButton.onClick.RemoveListener(HandleResultFormationClicked);
                _resultFormationButton.onClick.AddListener(HandleResultFormationClicked);
            }
        }

        private void CreateResultFormationButton()
        {
            if (_resultPanelObject == null)
            {
                return;
            }

            Transform existing = _resultPanelObject.transform.Find("FormationButton");
            if (existing != null)
            {
                _resultFormationButton = existing.GetComponent<Button>();
                _resultFormationButtonText = existing.GetComponentInChildren<TMP_Text>(true);

                if (_resultFormationButton != null)
                {
                    _resultFormationButton.onClick.RemoveListener(HandleResultFormationClicked);
                    _resultFormationButton.onClick.AddListener(HandleResultFormationClicked);
                }

                return;
            }

            GameObject buttonObject = new GameObject("FormationButton");
            buttonObject.transform.SetParent(_resultPanelObject.transform, false);

            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.08f, 0.08f);
            buttonRect.anchorMax = new Vector2(0.46f, 0.22f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            _resultFormationButton = buttonObject.AddComponent<Button>();
            _resultFormationButton.onClick.AddListener(HandleResultFormationClicked);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _resultFormationButtonText = textObject.AddComponent<TextMeshProUGUI>();
            _resultFormationButtonText.alignment = TextAlignmentOptions.Center;
            _resultFormationButtonText.fontSize = 24f;
            _resultFormationButtonText.raycastTarget = false;
            _resultFormationButtonText.text = "Formation";
        }
        private void TryBindExistingResultReturnButton()
        {
            if (_resultPanelObject == null)
            {
                return;
            }

            Transform buttonTransform = _resultPanelObject.transform.Find("ReturnButton");
            if (buttonTransform == null)
            {
                CreateResultReturnButton();
                return;
            }

            _resultReturnButton = buttonTransform.GetComponent<Button>();
            _resultReturnButtonText = buttonTransform.GetComponentInChildren<TMP_Text>(true);

            if (_resultReturnButton != null)
            {
                _resultReturnButton.onClick.RemoveListener(HandleResultReturnClicked);
                _resultReturnButton.onClick.AddListener(HandleResultReturnClicked);
            }
        }

        private void CreateResultReturnButton()
        {
            if (_resultPanelObject == null)
            {
                return;
            }

            Transform existing = _resultPanelObject.transform.Find("ReturnButton");
            if (existing != null)
            {
                _resultReturnButton = existing.GetComponent<Button>();
                _resultReturnButtonText = existing.GetComponentInChildren<TMP_Text>(true);

                if (_resultReturnButton != null)
                {
                    _resultReturnButton.onClick.RemoveListener(HandleResultReturnClicked);
                    _resultReturnButton.onClick.AddListener(HandleResultReturnClicked);
                }

                return;
            }

            GameObject buttonObject = new GameObject("ReturnButton");
            buttonObject.transform.SetParent(_resultPanelObject.transform, false);

            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.54f, 0.08f);
            buttonRect.anchorMax = new Vector2(0.92f, 0.22f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            _resultReturnButton = buttonObject.AddComponent<Button>();
            _resultReturnButton.onClick.AddListener(HandleResultReturnClicked);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _resultReturnButtonText = textObject.AddComponent<TextMeshProUGUI>();
            _resultReturnButtonText.alignment = TextAlignmentOptions.Center;
            _resultReturnButtonText.fontSize = 24f;
            _resultReturnButtonText.raycastTarget = false;
            _resultReturnButtonText.text = "Return";
        }

        private void HandleResultFormationClicked()
        {
            Debug.Log("[Result] Formation clicked.");

            if (_resultSubText != null)
            {
                _resultSubText.text =
                    "Formation / Preparation\n" +
                    $"Party: {BuildPartyOverviewText()}\n" +
                    $"Kakera: {_kakeraStock}/{MaxKakeraStock}\n" +
                    "Item / Skill / Link check: deferred";
            }
        }

        private string BuildPartyOverviewText()
        {
            int livingCount = CountLivingPartyMembers(_allies) + CountLivingPartyMembers(_reserves);
            int totalCount = CountKnownPartyMembers(_allies) + CountKnownPartyMembers(_reserves);

            return $"{livingCount}/{Mathf.Max(1, totalCount)} alive";
        }
        private void HandleResultReturnClicked()
        {
            if (_questProgress != null && _questProgress.HasNextWave)
            {
                Debug.Log("[Result] Next Wave clicked.");
                StartNextWave();
                return;
            }

            Debug.Log("[Result] Return to Base clicked.");

            ReturnToBase();
        }

        private void StartNextWave()
        {
            if (_questProgress == null || !_questProgress.MoveNextWave())
            {
                RestartBattle();
                return;
            }

            StopAllCoroutines();

            HideResultPanel();
            HideActionOverlay();
            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            ClearPendingActionFlashTargets();
            ClearPendingActionValuePopups();

            _battleEnded = false;
            _phase = BattlePhase.CommandSelect;
            _formationSettling = false;
            _hoveredSkill = null;

            ReplaceEnemyWave(_questProgress.CurrentWave);

            _actedUnits.Clear();
            _turnNumbers.Clear();
            _previewEnemyActionStates.Clear();

            EnsureWaveProgress();

            int baseWaveDistance = _questProgress.CurrentWave == null
                ? DefaultBaseWaveDistance
                : _questProgress.CurrentWave.BaseDistance;

            _waveProgress.StartWave(baseWaveDistance);
            RecoverAllAllyMP();

            RebuildTurnOrder();

            BattleUnit nextAlly = FindNextUnactedAlly();
            if (nextAlly != null)
            {
                EnterCommandSelect(nextAlly);
            }
            else
            {
                CheckBattleEnd();
            }

            if (commandPanel != null)
            {
                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
                commandPanel.SetInteractable(true);
            }

            if (rotateButton != null)
            {
                rotateButton.gameObject.SetActive(true);
                rotateButton.interactable = true;
            }

            SetCommandUiVisible(true);
            RedrawBoard();

            Debug.Log($"[Wave] Started next wave: {GetCurrentWaveNumber()}/{GetTotalWaveCount()}.");
        }

        private void ReplaceEnemyWave(WaveData wave)
        {
            ClearEnemyBoardAndLists();

            if (wave == null)
            {
                wave = DefaultWaveFactory.CreateDefaultWave();
            }

            ApplyBattleUnitPlacements(false, wave.EnemyPlacements, _enemies);
            _enemyReserves.AddRange(wave.EnemyReserves);
        }

        private void ClearEnemyBoardAndLists()
        {
            _grid.SetUnit(false, GridPos.FrontTop, null);
            _grid.SetUnit(false, GridPos.BackTop, null);
            _grid.SetUnit(false, GridPos.FrontBottom, null);
            _grid.SetUnit(false, GridPos.BackBottom, null);

            _enemies.Clear();
            _enemyReserves.Clear();
        }
        private void ReturnToBase()
        {
            // 現時点では拠点画面がないため、拠点帰還処理の仮実装としてBattleを再初期化する。
            // BootstrapBattle()により、味方HP/MP/KO状態・Inventory・QuestProgressは初期状態に戻る。
            _kakeraStock = 0;
            _totalKakeraEarned = 0;

            Debug.Log("[Base] Returned to base. Party state and Kakera will be reset by restarting the default quest.");

            RestartBattle();
        }
        private void RestartBattle()
        {
            StopAllCoroutines();

            HideResultPanel();
            HideActionOverlay();
            SetCommandUiVisible(true);

            BootstrapBattle();

            if (commandPanel != null)
            {
                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
                commandPanel.SetInteractable(true);
            }

            if (rotateButton != null)
            {
                rotateButton.gameObject.SetActive(true);
                rotateButton.interactable = true;
            }

            RedrawBoard();

            Debug.Log("[Battle] Restarted battle.");
        }

        private void ShowWaveResultPanel(WaveClearResult result)
        {
            EnsureResultPanel();

            if (_resultPanelObject != null)
            {
                _resultPanelObject.SetActive(true);
            }

            if (_resultTitleText != null)
            {
                _resultTitleText.text = result.HasNextWave
                    ? "Battle Clear"
                    : "Boss Clear";
            }

            if (_resultSubText != null)
            {
                _resultSubText.text = BuildWaveResultSubText(result);
            }

            if (_resultFormationButton != null)
            {
                _resultFormationButton.gameObject.SetActive(true);
            }

            if (_resultFormationButtonText != null)
            {
                _resultFormationButtonText.text = "Formation";
            }

            if (_resultReturnButton != null)
            {
                _resultReturnButton.gameObject.SetActive(true);
            }

            if (_resultReturnButtonText != null)
            {
                _resultReturnButtonText.text = "Next";
            }
        }

        private string BuildWaveResultSubText(WaveClearResult result)
        {
            if (result == null)
            {
                return "";
            }

            int kakeraGain = CalculateKakeraGain(result.Rank);
            int expGain = result.HasNextWave ? 10 : 30;
            string nextText = result.HasNextWave
                ? "Next Battle"
                : "Return to Base";

            return
                $"Clear: {FormatWaveClearRank(result.Rank)}\n" +
                $"Kakera: +{kakeraGain}  Stock: {_kakeraStock}/{MaxKakeraStock}\n" +
                $"EXP: +{expGain}\n" +
                $"Lv Up: None\n" +
                $"Next: {nextText}";
        }

        private static int CalculateKakeraGain(WaveClearRank rank)
        {
            return rank switch
            {
                WaveClearRank.OneTurn => 3,
                WaveClearRank.TwoTurn => 2,
                _ => 1
            };
        }
        private void ShowResultPanel(string result)
        {
            EnsureResultPanel();

            if (_resultPanelObject != null)
            {
                _resultPanelObject.SetActive(true);
            }

            if (_resultTitleText != null)
            {
                _resultTitleText.text = result;
            }

            if (_resultSubText != null)
            {
                _resultSubText.text = "Battle End";
            }

            if (_resultReturnButton != null)
            {
                _resultReturnButton.gameObject.SetActive(true);
            }

            if (_resultReturnButtonText != null)
            {
                _resultReturnButtonText.text = "Next";
            }
        }

        private void HideResultPanel()
        {
            EnsureResultPanel();

            if (_resultPanelObject != null)
            {
                _resultPanelObject.SetActive(false);
            }
        }

        // Board redraw and status UI
        private void RedrawBoard()
        {
            // Enemy side is displayed mirrored on screen.
            // Visual left cells show enemy backline, visual right cells show enemy frontline.
            SetBoardCellUnit(enemyFrontTop, _grid.GetUnit(false, GridPos.BackTop));
            SetBoardCellUnit(enemyBackTop, _grid.GetUnit(false, GridPos.FrontTop));
            SetBoardCellUnit(enemyFrontBottom, _grid.GetUnit(false, GridPos.BackBottom));
            SetBoardCellUnit(enemyBackBottom, _grid.GetUnit(false, GridPos.FrontBottom));

            SetBoardCellUnit(allyFrontTop, _grid.GetUnit(true, GridPos.FrontTop));
            SetBoardCellUnit(allyBackTop, _grid.GetUnit(true, GridPos.BackTop));
            SetBoardCellUnit(allyFrontBottom, _grid.GetUnit(true, GridPos.FrontBottom));
            SetBoardCellUnit(allyBackBottom, _grid.GetUnit(true, GridPos.BackBottom));

            RedrawStatusPanels();
            RedrawActiveHighlights();
            RedrawTargetPreview();

            if (!_battleEnded && _phase == BattlePhase.CommandSelect)
            {
                RedrawEnemyActionPreviewHighlights();
            }
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

        // 盤面セルにユニット情報を反映する入口。
        // BattleSpriteがある場合はSpriteだけを表示し、名前テキストは消す。
        // BattleSpriteがない場合は仮表示としてユニット名を出す。
        private static void SetBoardCellUnit(TMP_Text cellLabel, BattleUnit unit)
        {
            if (cellLabel == null)
            {
                return;
            }

            Sprite sprite = unit == null || unit.Data == null ? null : unit.Data.BattleSprite;
            cellLabel.text = sprite == null ? SafeName(unit) : "";
            ApplyBoardCellLabelLayout(cellLabel, sprite != null);
            SetBoardCellSprite(cellLabel, unit);
        }

        // セル内テキストの基本レイアウトを整える。
        // 現在はSpriteありの場合テキストを空にしているため、
        // 主にSprite未設定時の名前仮表示を中央に置くための処理。
        private static void ApplyBoardCellLabelLayout(TMP_Text cellLabel, bool hasSprite)
        {
            if (cellLabel == null)
            {
                return;
            }

            RectTransform labelRect = cellLabel.GetComponent<RectTransform>();
            if (labelRect == null)
            {
                return;
            }

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            cellLabel.alignment = TextAlignmentOptions.Center;
            cellLabel.fontSize = BoardTextOnlyFontSize;
        }

        // BattleSpriteImageを生成または取得し、盤面セルにSpriteを表示する。
        // ここでキャラごとのScale/Offset、描画順、KO時の半透明表示をまとめて反映する。
        private static void SetBoardCellSprite(TMP_Text cellLabel, BattleUnit unit)
        {
            if (cellLabel == null || cellLabel.transform.parent == null)
            {
                return;
            }

            // CharacterDataから表示用Spriteと個別補正値を取り出す。
            // Scaleは0以下になると表示が破綻するため、最低0.01に丸める。
            Sprite sprite = unit == null || unit.Data == null ? null : unit.Data.BattleSprite;
            float spriteScale = unit == null || unit.Data == null ? 1f : Mathf.Max(0.01f, unit.Data.BattleSpriteScale);
            Vector2 spriteOffset = unit == null || unit.Data == null ? Vector2.zero : unit.Data.BattleSpriteOffset;

            Transform cellTransform = cellLabel.transform.parent;

            // 既に作成済みのBattleSpriteImageがあれば再利用する。
            // なければ、このセル専用のImageオブジェクトを作る。
            Transform existing = cellTransform.Find("BattleSpriteImage");

            Image spriteImage;
            if (existing == null)
            {
                GameObject spriteObject = new GameObject("BattleSpriteImage");
                spriteObject.transform.SetParent(cellTransform, false);

                RectTransform rect = spriteObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(BoardSpriteMinAnchor, BoardSpriteMinAnchor);
                rect.anchorMax = new Vector2(BoardSpriteMaxAnchor, BoardSpriteMaxAnchor);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                spriteImage = spriteObject.AddComponent<Image>();
                spriteImage.raycastTarget = false;
                spriteImage.preserveAspect = true;

                // Keep the sprite behind the cell text.
                spriteObject.transform.SetSiblingIndex(0);
            }
            else
            {
                spriteImage = existing.GetComponent<Image>();
                if (spriteImage == null)
                {
                    spriteImage = existing.gameObject.AddComponent<Image>();
                    spriteImage.raycastTarget = false;
                    spriteImage.preserveAspect = true;
                }
            }

            // Spriteはセル内の最背面へ置く。
            // cellLabelは後で最前面へ戻す。
            spriteImage.transform.SetSiblingIndex(0);

            RectTransform imageRect = spriteImage.GetComponent<RectTransform>();
            if (imageRect != null)
            {
                // アンカーで基本サイズを決め、OffsetとScaleでキャラごとの見た目を微調整する。
                imageRect.anchorMin = new Vector2(BoardSpriteMinAnchor, BoardSpriteMinAnchor);
                imageRect.anchorMax = new Vector2(BoardSpriteMaxAnchor, BoardSpriteMaxAnchor);
                imageRect.offsetMin = spriteOffset;
                imageRect.offsetMax = spriteOffset;
                imageRect.localScale = new Vector3(spriteScale, spriteScale, 1f);
            }

            // テキストは最前面に戻す。
            // 現在はSpriteありの場合テキストを空にしているが、
            // Sprite未設定時の名前表示や将来のデバッグ表示で隠れないようにする。
            cellLabel.transform.SetAsLastSibling();

            // 盤面Spriteはクリック判定を持たず、画像比率を維持して表示する。
            spriteImage.raycastTarget = false;
            spriteImage.preserveAspect = true;
            spriteImage.sprite = sprite;
            spriteImage.enabled = sprite != null;

            // KO/dead状態のユニットは半透明にして、戦闘不能であることを見た目で示す。
            spriteImage.color = unit != null && unit.IsDead
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;
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
            SetBarFill(slot, "HPBar", currentHp, maxHp);
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

            // Acted units should not display a number, but they must still
            // occupy their original turn-order position when numbering
            // the remaining visible units.
            bool shouldHideNumber = _actedUnits.Contains(unit);

            if (_turnOrder == null)
            {
                return "";
            }

            IReadOnlyList<BattleUnit> order = _turnOrder.TurnOrder;
            int displayNumber = 0;

            for (int i = 0; i < order.Count; i++)
            {
                BattleUnit current = order[i];

                if (current == null || current.IsDead)
                {
                    continue;
                }

                displayNumber++;

                if (current == unit)
                {
                    return shouldHideNumber
                        ? ""
                        : displayNumber.ToString();
                }
            }

            return "";
        }

        private static string BuildBuffText(BattleUnit unit)
        {
            if (unit == null)
            {
                return "";
            }

            var lines = new List<string>();

            if (unit.IsAlly && unit.Data != null)
            {
                lines.Add($"MP {unit.CurrentMP}/{unit.Data.MaxMP}");
            }

            if (unit.Buffs != null)
            {
                for (int i = 0; i < unit.Buffs.Count; i++)
                {
                    BuffState buff = unit.Buffs[i];
                    if (buff == null)
                    {
                        continue;
                    }

                    lines.Add($"{buff.Type} {buff.RemainingTurns}");
                }
            }

            return string.Join("\n", lines);
        }

        private void RecoverAllAllyMP()
        {
            RecoverMPInUnits(_allies);
            RecoverMPInUnits(_reserves);
            RedrawStatusPanels();
        }

        private static void RecoverMPInUnits(IEnumerable<BattleUnit> units)
        {
            if (units == null)
            {
                return;
            }

            foreach (BattleUnit unit in units)
            {
                if (unit == null || unit.IsDead || unit.Data == null)
                {
                    continue;
                }

                int before = unit.CurrentMP;
                unit.CurrentMP = Mathf.Min(unit.Data.MaxMP, unit.CurrentMP + 1);

                if (unit.CurrentMP != before)
                {
                    Debug.Log($"[MP] {unit.Name} recovered MP {before}->{unit.CurrentMP}/{unit.Data.MaxMP}.");
                }
            }
        }

        // Utility
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


    }
}






























































































