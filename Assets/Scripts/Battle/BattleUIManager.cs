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
        [Header("Battle UI References")]
        [SerializeField] private BattleUIReferences uiReferences;

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

        [Header("Skill Hover Sprite Preview")]
        [SerializeField] private Color skillHoverInactiveSpriteColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private float skillHoverSilhouetteOverlapAlpha = 0.45f;
        [SerializeField] private float skillHoverOverlapTargetAlpha = 0.55f;
        [Header("Turn Order Bar")]
        [SerializeField] private Transform turnOrderSlotContainer;
        [SerializeField] private Transform[] turnOrderSlotPositions = new Transform[8];        [SerializeField] private TurnOrderSlotView turnOrderSlotTemplate;
        [SerializeField] private bool hideTurnOrderSlotTemplateOnPlay = true;
        [SerializeField] private TMP_Text turnOrderBarText;
        [SerializeField] private string turnOrderSeparator = "  >  ";
        [SerializeField] private string turnOrderAllyPrefix = "A";
        [SerializeField] private string turnOrderEnemyPrefix = "E";
        [SerializeField] private string currentTurnPrefix = ">";
        [SerializeField] private string actedTurnPrefix = "x";
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

        private readonly ResultPanelPresenter _resultPanelPresenter = new();
        private readonly RouteOverlayPresenter _routeOverlayPresenter = new();
        private GameObject _enemyActionPreviewPanelObject;
        private TMP_Text _enemyActionPreviewText;
        private readonly List<GridPos> _pendingActionFlashTargets = new();
        private bool _pendingActionFlashIsAllyBoard;
        private readonly List<GridPos> _pendingActionSourceFlashTargets = new();
        private bool _pendingActionSourceFlashIsAllyBoard;
        private bool _pendingEnemyKoReplacementPhase;
        private bool _pendingEnemyAutoReplacementEnterAnimation;
        private readonly List<BattleUnit> _enemyStatusKoVisibleUnits = new();
        private readonly List<ActionValuePopup> _pendingActionValuePopups = new();
        private readonly List<TurnOrderSlotView> _generatedTurnOrderSlotViews = new();
        private bool _deferHpBarFillUntilActionHit;
        private readonly Dictionary<Transform, BattleUnit> _statusSlotUnits = new();
        private readonly Dictionary<Transform, float> _pendingHpBarFillRates = new();
        private Material _skillHoverSilhouetteMaterial;
        private readonly List<TMP_Text> _activeActionValuePopupLabels = new();
        private readonly HashSet<int> _scoutedWaveIndices = new();

        private bool _battleEnded;
        private bool _showingRouteEvent;
        private bool _showingRouteMovement;
        private bool _showingBattlePreparation;
        private bool _showingBattleResult;
        private bool _showingQuestResult;
        private bool _showingQuestFailed;
        private BattlePhase _phase;
        private WaveProgressState _waveProgress;
        private QuestProgressState _questProgress;
        private int _oneTurnClearPartyHeal = DefaultOneTurnClearPartyHeal;
        private int _kakeraStock;
        private int _totalKakeraEarned;
        private int _totalExpEarned;
        [SerializeField] private float rotationSettleSeconds = 0.5f;
        [SerializeField] private float actionSpriteLungeDistance = 24f;
        [SerializeField] private float targetHitShakeDistance = 10f;
        [SerializeField] private float targetHitShakeSeconds = 0.16f;
        [SerializeField] private float autoReplacementEnterSeconds = 0.18f;
        [SerializeField] private float autoReplacementEnterDistance = 32f;
        [SerializeField] private float enemyStatusKoFadeDelaySeconds = 0.0f;
        [SerializeField] private float defeatFadeSeconds = 0.22f;
        [SerializeField] private float floatingHpBarVisibleSeconds = 0.9f;
        [SerializeField] private float floatingHpBarFadeSeconds = 0.18f;
        [SerializeField] private Vector2 floatingHpBarOffset = new Vector2(0f, 52f);
        [SerializeField] private Vector2 floatingHpBarSize = new Vector2(64f, 10f);
        [SerializeField] private float hpBarAnimationSeconds = 0.35f;
        [SerializeField] private float defeatSinkDistance = 18f;
        [SerializeField] private int targetHitShakeCount = 3;
        [SerializeField] private float actionSpriteLungeSeconds = 0.12f;
        [SerializeField] private float actionIntroDelaySeconds = 0.5f;        [SerializeField] private float actionResolveDelaySeconds = 0.35f;
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

        private const int DefaultOneTurnClearPartyHeal = 5;
        private const int MaxKakeraStock = 9;

        private sealed class ActionValuePopup
        {
            public bool IsAllyBoard;
            public GridPos Position;
            public bool HasHpSnapshot;
            public int PreviousHP;
            public int CurrentHP;
            public int MaxHP;            public string Text;
        }

        private enum BattlePhase
        {
            CommandSelect,
            ResolvingAction,
            BattleEnded
        }

        private class DefeatedEnemyInfo
        {
            public BattleUnit Unit;
            public GridPos Position;
        }

        private void ApplyBattleUIReferences()
        {
            BattleUIReferences refs = uiReferences;
            if (refs == null)
            {
                refs = GetComponentInChildren<BattleUIReferences>(true);
            }

            if (refs == null)
            {
                BattleUIReferences[] sceneRefs = FindObjectsByType<BattleUIReferences>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (sceneRefs != null && sceneRefs.Length > 0)
                {
                    refs = sceneRefs[0];
                }
            }

            if (refs == null)
            {
                Debug.LogWarning("[BattleUI] BattleUIReferences was not found. Some Inspector-bound UI references may not update.");
                return;
            }

            commandPanel = refs.commandPanel != null ? refs.commandPanel : commandPanel;
            rotateButton = refs.rotateButton != null ? refs.rotateButton : rotateButton;

            enemyFrontTop = refs.enemyFrontTop != null ? refs.enemyFrontTop : enemyFrontTop;
            enemyBackTop = refs.enemyBackTop != null ? refs.enemyBackTop : enemyBackTop;
            enemyFrontBottom = refs.enemyFrontBottom != null ? refs.enemyFrontBottom : enemyFrontBottom;
            enemyBackBottom = refs.enemyBackBottom != null ? refs.enemyBackBottom : enemyBackBottom;

            allyFrontTop = refs.allyFrontTop != null ? refs.allyFrontTop : allyFrontTop;
            allyBackTop = refs.allyBackTop != null ? refs.allyBackTop : allyBackTop;
            allyFrontBottom = refs.allyFrontBottom != null ? refs.allyFrontBottom : allyFrontBottom;
            allyBackBottom = refs.allyBackBottom != null ? refs.allyBackBottom : allyBackBottom;

            actionSkillName = refs.actionSkillName != null ? refs.actionSkillName : actionSkillName;
            actionUserName = refs.actionUserName != null ? refs.actionUserName : actionUserName;

            enemyFTHighlight = refs.enemyFTHighlight != null ? refs.enemyFTHighlight : enemyFTHighlight;
            enemyFBHighlight = refs.enemyFBHighlight != null ? refs.enemyFBHighlight : enemyFBHighlight;

            enemyStatusPanel = refs.enemyStatusPanel != null ? refs.enemyStatusPanel : enemyStatusPanel;
            allyStatusPanel = refs.allyStatusPanel != null ? refs.allyStatusPanel : allyStatusPanel;
            turnOrderBarText = refs.turnOrderBarText != null ? refs.turnOrderBarText : turnOrderBarText;
            turnOrderSlotContainer = refs.turnOrderSlotContainer != null ? refs.turnOrderSlotContainer : turnOrderSlotContainer;
            turnOrderSlotTemplate = refs.turnOrderSlotTemplate != null ? refs.turnOrderSlotTemplate : turnOrderSlotTemplate;
            if (refs.turnOrderSlotPositions != null && refs.turnOrderSlotPositions.Length > 0)
            {
                turnOrderSlotPositions = refs.turnOrderSlotPositions;
            }
        }
        // Battle setup
        private void Start()
        {
            ApplyBattleUIReferences();
            BootstrapBattle();
            BindUI();
            EnsureResultPanel();
            EnsureRouteOverlayPanels();
            EnsureEnemyActionPreviewPanel();
            RedrawBoard();
            HideActionOverlay();
            HideResultPanel();
            HideRouteOverlayPanels();
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
            _totalExpEarned = 0;
            _scoutedWaveIndices.Clear();
            _previewEnemyActionStates.Clear();
            _turnNumbers.Clear();
            _actedUnits.Clear();
            _enemyStatusKoVisibleUnits.Clear();
            _pendingEnemyAutoReplacementEnterAnimation = false;
            _pendingEnemyKoReplacementPhase = false;
            _statusSlotUnits.Clear();
        }

        private void ApplyBattleSetup(BattleSetupData setup)
        {
            if (setup == null)
            {
                return;
            }

            _questProgress = setup.QuestProgress;
            _oneTurnClearPartyHeal = setup.OneTurnClearPartyHeal;

            BattleUnitPlacementApplier.Apply(_grid, true, setup.AllyPlacements, _allies);
            BattleUnitPlacementApplier.Apply(_grid, false, setup.EnemyPlacements, _enemies);

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
            if (commandPanel != null)
            {
                commandPanel.OnSkillClicked -= HandleSkillClicked;
                commandPanel.OnSkillHovered -= HandleSkillHover;
                commandPanel.OnHoverExit -= ClearTargetPreview;
                commandPanel.OnReserveClicked -= HandleSwap;
                commandPanel.OnItemClicked -= HandleItemClicked;

                commandPanel.OnSkillClicked += HandleSkillClicked;
                commandPanel.OnSkillHovered += HandleSkillHover;
                commandPanel.OnHoverExit += ClearTargetPreview;
                commandPanel.OnReserveClicked += HandleSwap;
                commandPanel.OnItemClicked += HandleItemClicked;

                commandPanel.Setup(_active, _reserves, _allies, _inventoryItems);
            }

            if (rotateButton != null)
            {
                rotateButton.onClick.RemoveListener(HandleRotateClicked);
                rotateButton.onClick.AddListener(HandleRotateClicked);
            }
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
            // HP bar deferral starts immediately before the actual HP-changing resolution.
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

            BattleUnit actor = _active;
            BattleUnit linkPartner = GetLinkPartnerForSkill(actor, skill);
            string userDisplayName = BuildSkillUserDisplayName(actor, linkPartner);

            EnterResolvingAction();
            ShowActionOverlay(skill.SkillName, userDisplayName);

            StartCoroutine(ResolvePlayerSkillAfterIntroDelay(skill, actor, linkPartner, userDisplayName));
        }

        private IEnumerator ResolvePlayerSkillAfterIntroDelay(SkillData skill, BattleUnit actor, BattleUnit linkPartner, string userDisplayName)
        {
            float delay = Mathf.Max(0f, actionIntroDelaySeconds);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (_battleEnded || actor == null || actor.IsDead || skill == null)
            {
                yield break;
            }

            _active = actor;
            BeginDeferredHpBarFill();
            ConsumeSkillMP(actor, skill, linkPartner);

            PrepareSkillActionFlashTargets(skill);
            BattleUnit flashableLinkPartner = IsActiveAllyUnit(linkPartner) ? linkPartner : null;
            SetPendingActionSourceFlashTargets(true, BuildSkillSourceFlashTargets(actor, flashableLinkPartner));
            Debug.Log($"[Action] Skill used: {skill.SkillName} by {userDisplayName}.");

            ApplySkillDamage(skill);
            ApplySkillEffect(skill);
            RedrawBoard();

            if (_battleEnded)
            {
                RedrawBoard();
                yield break;
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
                    List<BattleUnit> buffTargets = GetSkillEffectTargets(skill);
                    for (int i = 0; i < buffTargets.Count; i++)
                    {
                        ApplyBuff(buffTargets[i], skill.BuffType, skill.BuffTurns);
                    }
                    return;

                case SkillEffectType.Heal:
                    ApplySkillHeal(skill);
                    return;
            }
        }

        private void ApplySkillHeal(SkillData skill)
        {
            if (skill == null || skill.HealAmount <= 0)
            {
                return;
            }

            List<BattleUnit> healTargets = GetSkillEffectTargets(skill);
            for (int i = 0; i < healTargets.Count; i++)
            {
                HealAllyUnit(healTargets[i], skill.HealAmount);
            }
        }

        private void HealAllyUnit(BattleUnit target, int healAmount)
        {
            if (target == null || target.IsDead || target.Data == null || healAmount <= 0)
            {
                return;
            }

            int beforeHp = target.CurrentHP;
            target.CurrentHP = Mathf.Min(target.Data.MaxHP, target.CurrentHP + healAmount);
            int healed = target.CurrentHP - beforeHp;

            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}", beforeHp, target.CurrentHP, target.Data.MaxHP);
            Debug.Log($"[Heal] {target.Name} recovered {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}");
        }

        private static void AddLivingUnits(List<BattleUnit> source, List<BattleUnit> destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                BattleUnit unit = source[i];
                if (unit != null && !unit.IsDead && unit.Data != null && !destination.Contains(unit))
                {
                    destination.Add(unit);
                }
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

            int beforeHp = target.CurrentHP;
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);
            AddPendingActionValuePopup(false, pos, $"-{finalDamage}", beforeHp, target.CurrentHP, target.Data.MaxHP);

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
                if (!_enemyStatusKoVisibleUnits.Contains(defeated.Unit))
                {
                    _enemyStatusKoVisibleUnits.Add(defeated.Unit);
                }
                RemoveTurnState(defeated.Unit);

                Debug.Log($"[KO] {defeated.Unit.Name} is defeated. Grid removal is deferred until fadeout completes.");
            }

            // Enemy grid movement and reserve entry are deferred until the KO fadeout finishes.
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

        private bool FillEmptyEnemyCellsFromReserves()
        {
            bool changed = false;
            changed |= TryFillEnemyCellFromReserve(GridPos.FrontTop);
            changed |= TryFillEnemyCellFromReserve(GridPos.FrontBottom);
            changed |= TryFillEnemyCellFromReserve(GridPos.BackTop);
            changed |= TryFillEnemyCellFromReserve(GridPos.BackBottom);
            return changed;
        }

        private bool TryFillEnemyCellFromReserve(GridPos position)
        {
            BattleUnit current = _grid.GetUnit(false, position);
            if (current != null && !current.IsDead)
            {
                return false;
            }

            BattleUnit replacement = GetNextEnemyReserve();
            if (replacement == null)
            {
                return false;
            }

            _grid.SetUnit(false, position, replacement);

            if (!_enemies.Contains(replacement))
            {
                _enemies.Add(replacement);
            }

            _enemyReserves.Remove(replacement);
            _actedUnits.Add(replacement);

            Debug.Log($"[KO] {replacement.Name} entered enemy grid at {position}. Replacement cannot act this turn.");
            return true;
        }

        // Enemy action preview
        private void RedrawEnemyActionPreviewHighlights()
        {
            // Enemy grid/action preview is intentionally disabled.
            // Keep active/target highlights independent from enemy forecast visuals.
            ResetAllyBoardHighlights();
            RedrawActiveHighlights();
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
            // Enemy grid/action preview is intentionally disabled.
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
            ApplySkillHoverSpritePreview();
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
            ResetBoardSpritePreviewColors();
        }

        private void ApplySkillHoverSpritePreview()
        {
            ResetBoardSpritePreviewColors();

            if (_hoveredSkill == null || _active == null || _active.IsDead)
            {
                return;
            }

            var focusedUnits = new HashSet<BattleUnit>();
            focusedUnits.Add(_active);

            bool targetIsAllyBoard = _hoveredSkill.TargetPattern == SkillTargetPattern.Self;
            List<GridPos> targetPositions = GetSkillAnimationTargetPositions(_hoveredSkill);
            for (int i = 0; i < targetPositions.Count; i++)
            {
                BattleUnit targetUnit = _grid.GetUnit(targetIsAllyBoard, targetPositions[i]);
                if (targetUnit != null && !targetUnit.IsDead)
                {
                    focusedUnits.Add(targetUnit);
                }
            }

            ApplySpriteFocusColors(true, focusedUnits);
            ApplySpriteFocusColors(false, focusedUnits);
            ApplySkillHoverSilhouetteOverlapAlpha(focusedUnits);
        }

        private void ApplySpriteFocusColors(bool isAllyBoard, HashSet<BattleUnit> focusedUnits)
        {
            ApplySpriteFocusColorAt(isAllyBoard, GridPos.FrontTop, focusedUnits);
            ApplySpriteFocusColorAt(isAllyBoard, GridPos.BackTop, focusedUnits);
            ApplySpriteFocusColorAt(isAllyBoard, GridPos.FrontBottom, focusedUnits);
            ApplySpriteFocusColorAt(isAllyBoard, GridPos.BackBottom, focusedUnits);
        }

        private void ApplySpriteFocusColorAt(bool isAllyBoard, GridPos position, HashSet<BattleUnit> focusedUnits)
        {
            BattleUnit unit = _grid.GetUnit(isAllyBoard, position);
            Image image = GetBoardSpriteImage(isAllyBoard, position);
            if (image == null)
            {
                return;
            }

            if (unit == null || unit.IsDead || focusedUnits == null || !focusedUnits.Contains(unit))
            {
                ApplySkillHoverSilhouette(image, 1f);
                return;
            }

            ApplyNormalBoardSpriteMaterial(image);
            image.color = Color.white;
        }

        private void ApplySkillHoverSilhouette(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Material material = GetSkillHoverSilhouetteMaterial();
            if (material != null)
            {
                image.material = material;
            }

            Color color = skillHoverInactiveSpriteColor;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private Material GetSkillHoverSilhouetteMaterial()
        {
            if (_skillHoverSilhouetteMaterial != null)
            {
                return _skillHoverSilhouetteMaterial;
            }

            Shader shader = Shader.Find("GameKari/UIAlphaSilhouette");
            if (shader == null)
            {
                Debug.LogWarning("[Preview] Shader not found: GameKari/UIAlphaSilhouette. Falling back to normal Image tint.");
                return null;
            }

            _skillHoverSilhouetteMaterial = new Material(shader)
            {
                name = "SkillHoverSilhouetteMaterial_Runtime"
            };
            return _skillHoverSilhouetteMaterial;
        }

        private static void ApplyNormalBoardSpriteMaterial(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.material = null;
        }

        private void ApplySkillHoverSilhouetteOverlapAlpha(HashSet<BattleUnit> focusedUnits)
        {
            if (_active == null || _active.IsDead)
            {
                return;
            }

            RectTransform activeRect = GetBoardSpriteRect(true, _active.GridPos);
            if (activeRect == null)
            {
                return;
            }

            var activeOnlyRects = new List<RectTransform> { activeRect };

            ApplySilhouetteOverlapAlphaAt(true, GridPos.FrontTop, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.BackTop, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.FrontBottom, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(true, GridPos.BackBottom, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.FrontTop, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.BackTop, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.FrontBottom, focusedUnits, activeOnlyRects);
            ApplySilhouetteOverlapAlphaAt(false, GridPos.BackBottom, focusedUnits, activeOnlyRects);
        }

        private void AddFocusedSpriteRects(bool isAllyBoard, HashSet<BattleUnit> focusedUnits, List<RectTransform> focusedRects)
        {
            AddFocusedSpriteRectAt(isAllyBoard, GridPos.FrontTop, focusedUnits, focusedRects);
            AddFocusedSpriteRectAt(isAllyBoard, GridPos.BackTop, focusedUnits, focusedRects);
            AddFocusedSpriteRectAt(isAllyBoard, GridPos.FrontBottom, focusedUnits, focusedRects);
            AddFocusedSpriteRectAt(isAllyBoard, GridPos.BackBottom, focusedUnits, focusedRects);
        }

        private void AddFocusedSpriteRectAt(bool isAllyBoard, GridPos position, HashSet<BattleUnit> focusedUnits, List<RectTransform> focusedRects)
        {
            BattleUnit unit = _grid == null ? null : _grid.GetUnit(isAllyBoard, position);
            if (unit == null || unit.IsDead || focusedUnits == null || !focusedUnits.Contains(unit))
            {
                return;
            }

            RectTransform rect = GetBoardSpriteRect(isAllyBoard, position);
            if (rect != null && focusedRects != null && !focusedRects.Contains(rect))
            {
                focusedRects.Add(rect);
            }
        }

        private void ApplySilhouetteOverlapAlphaAt(bool isAllyBoard, GridPos position, HashSet<BattleUnit> focusedUnits, List<RectTransform> focusedRects)
        {
            BattleUnit unit = _grid == null ? null : _grid.GetUnit(isAllyBoard, position);
            if (unit == null || unit.IsDead || focusedUnits == null || focusedUnits.Contains(unit))
            {
                return;
            }

            RectTransform rect = GetBoardSpriteRect(isAllyBoard, position);
            if (rect == null || focusedRects == null)
            {
                return;
            }

            bool overlapsFocused = false;
            for (int i = 0; i < focusedRects.Count; i++)
            {
                RectTransform focusedRect = focusedRects[i];
                if (focusedRect != null && focusedRect != rect && RectTransformsOverlap(rect, focusedRect))
                {
                    overlapsFocused = true;
                    break;
                }
            }

            if (!overlapsFocused)
            {
                return;
            }

            Image image = GetBoardSpriteImage(isAllyBoard, position);
            if (image == null)
            {
                return;
            }

            ApplySkillHoverSilhouette(image, skillHoverSilhouetteOverlapAlpha);
        }
        private void ApplySkillHoverOverlapAlpha(bool targetIsAllyBoard, List<GridPos> targetPositions)
        {
            if (_active == null || targetPositions == null || targetPositions.Count == 0)
            {
                return;
            }

            RectTransform activeRect = GetBoardSpriteRect(true, _active.GridPos);
            if (activeRect == null)
            {
                return;
            }

            for (int i = 0; i < targetPositions.Count; i++)
            {
                GridPos targetPosition = targetPositions[i];
                RectTransform targetRect = GetBoardSpriteRect(targetIsAllyBoard, targetPosition);
                if (targetRect == null || targetRect == activeRect)
                {
                    continue;
                }

                if (!RectTransformsOverlap(activeRect, targetRect))
                {
                    continue;
                }

                Image targetImage = GetBoardSpriteImage(targetIsAllyBoard, targetPosition);
                if (targetImage == null)
                {
                    continue;
                }

                Color color = targetImage.color;
                color.a = Mathf.Clamp01(skillHoverOverlapTargetAlpha);
                targetImage.color = color;
            }
        }

        private void ResetBoardSpritePreviewColors()
        {
            ResetBoardSpritePreviewColors(true);
            ResetBoardSpritePreviewColors(false);
        }

        private void ResetBoardSpritePreviewColors(bool isAllyBoard)
        {
            ResetBoardSpritePreviewColorAt(isAllyBoard, GridPos.FrontTop);
            ResetBoardSpritePreviewColorAt(isAllyBoard, GridPos.BackTop);
            ResetBoardSpritePreviewColorAt(isAllyBoard, GridPos.FrontBottom);
            ResetBoardSpritePreviewColorAt(isAllyBoard, GridPos.BackBottom);
        }

        private void ResetBoardSpritePreviewColorAt(bool isAllyBoard, GridPos position)
        {
            BattleUnit unit = _grid == null ? null : _grid.GetUnit(isAllyBoard, position);
            Image image = GetBoardSpriteImage(isAllyBoard, position);
            if (image == null)
            {
                return;
            }

            ApplyNormalBoardSpriteMaterial(image);
            image.color = unit != null && unit.IsDead
                ? new Color(1f, 1f, 1f, 0.45f)
                : Color.white;
        }

        private Image GetBoardSpriteImage(bool isAllyBoard, GridPos position)
        {
            TMP_Text cellLabel = GetBoardCellLabel(isAllyBoard, position);
            if (cellLabel == null || cellLabel.transform.parent == null)
            {
                return null;
            }

            Transform spriteTransform = cellLabel.transform.parent.Find("BattleSpriteImage");
            return spriteTransform == null ? null : spriteTransform.GetComponent<Image>();
        }

        private static bool RectTransformsOverlap(RectTransform a, RectTransform b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            Vector3[] aCorners = new Vector3[4];
            Vector3[] bCorners = new Vector3[4];
            a.GetWorldCorners(aCorners);
            b.GetWorldCorners(bCorners);

            Rect aRect = CornersToRect(aCorners);
            Rect bRect = CornersToRect(bCorners);
            return aRect.Overlaps(bRect);
        }

        private static Rect CornersToRect(Vector3[] corners)
        {
            if (corners == null || corners.Length < 4)
            {
                return Rect.zero;
            }

            float minX = corners[0].x;
            float maxX = corners[0].x;
            float minY = corners[0].y;
            float maxY = corners[0].y;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 corner = corners[i];
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
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
            BeginDeferredHpBarFill();

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
            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}", beforeHp, target.CurrentHP, target.Data.MaxHP);
            Debug.Log($"[Action] Item used: {item.ItemName} -> {target.Name} healed {healed}. HP: {target.CurrentHP}/{target.Data.MaxHP}. Remaining: {inventoryItem.Count}");
            RedrawBoard();

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
                    BattleUnit forwardAlly = TryGetForwardAlly(_active);
                    if (forwardAlly != null)
                    {
                        targets.Add(forwardAlly);
                    }
                    break;

                case SkillEffectTargetType.AllAllies:
                    AddLivingUnits(_allies, targets);
                    break;

                case SkillEffectTargetType.AllEnemies:
                    AddLivingUnits(_enemies, targets);
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

        private void AddPendingActionValuePopup(bool isAllyBoard, GridPos position, string text, int previousHp = -1, int currentHp = -1, int maxHp = -1)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            bool hasHpSnapshot = maxHp > 0 && previousHp >= 0 && currentHp >= 0;

            _pendingActionValuePopups.Add(new ActionValuePopup
            {
                IsAllyBoard = isAllyBoard,
                Position = position,
                Text = text,
                HasHpSnapshot = hasHpSnapshot,
                PreviousHP = hasHpSnapshot ? previousHp : 0,
                CurrentHP = hasHpSnapshot ? currentHp : 0,
                MaxHP = hasHpSnapshot ? maxHp : 0
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
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(120f, 48f);

                BattleUnit popupUnit = _grid.GetUnit(popup.IsAllyBoard, popup.Position);
                rect.anchoredPosition = GetDamagePopupOffset(popupUnit);

                TMP_Text label = popupObject.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 28f;
                label.raycastTarget = false;
                label.text = popup.Text;
                ApplyActionValuePopupColor(label, popup.Text);

                _activeActionValuePopupLabels.Add(label);
            }
        }

        private void ApplyActionValuePopupColor(TMP_Text label, string text)
        {
            if (label == null)
            {
                return;
            }

            Color color = !string.IsNullOrEmpty(text) && text.StartsWith("+")
                ? healPopupColor
                : damagePopupColor;

            label.enableVertexGradient = false;
            label.color = color;
            label.faceColor = color;

            if (label.fontSharedMaterial != null)
            {
                Material material = new Material(label.fontSharedMaterial);
                material.name = "ActionValuePopup_TMP_Material_Instance";
                if (material.HasProperty("_FaceColor"))
                {
                    material.SetColor("_FaceColor", color);
                }

                label.fontMaterial = material;
            }

            label.SetAllDirty();
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
                yield return PlayPendingAutoReplacementAnimations();
                yield break;
            }

            bool isTargetAllyBoard = _pendingActionFlashIsAllyBoard;
            bool isSourceAllyBoard = _pendingActionSourceFlashIsAllyBoard;

            List<GridPos> targetPositions = new(_pendingActionFlashTargets);
            List<GridPos> sourcePositions = new(_pendingActionSourceFlashTargets);

            yield return PlayActionSourceLunge(isSourceAllyBoard, sourcePositions);
            ApplyDeferredHpBarFillUpdates();
            ShowPendingFloatingHpBars();

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

            yield return PlayPendingDamageHitReactions();
            yield return PlayPendingDefeatFadeOuts();
            ApplyDeferredHpBarFillUpdates();

            HideActiveActionValuePopups();
            ClearPendingActionValuePopups();
            yield return PlayPendingAutoReplacementAnimations();
        }

        private IEnumerator PlayActionSourceLunge(bool isAllyBoard, List<GridPos> sourcePositions)
        {
            if (sourcePositions == null || sourcePositions.Count == 0)
            {
                yield break;
            }

            float duration = Mathf.Max(0f, actionSpriteLungeSeconds);
            float distance = Mathf.Max(0f, actionSpriteLungeDistance);
            if (duration <= 0f || distance <= 0f)
            {
                yield break;
            }

            var sprites = new List<RectTransform>();
            var startPositions = new List<Vector2>();

            for (int i = 0; i < sourcePositions.Count; i++)
            {
                RectTransform spriteRect = GetBoardSpriteRect(isAllyBoard, sourcePositions[i]);
                if (spriteRect == null)
                {
                    continue;
                }

                sprites.Add(spriteRect);
                startPositions.Add(spriteRect.anchoredPosition);
            }

            if (sprites.Count == 0)
            {
                yield break;
            }

            Vector2 offset = new Vector2(isAllyBoard ? distance : -distance, 0f);
            yield return MoveActionSprites(sprites, startPositions, offset, duration);
            yield return MoveActionSprites(sprites, startPositions, Vector2.zero, duration);
        }

        private IEnumerator MoveActionSprites(List<RectTransform> sprites, List<Vector2> startPositions, Vector2 offset, float duration)
        {
            if (sprites == null || startPositions == null || duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);

                for (int i = 0; i < sprites.Count && i < startPositions.Count; i++)
                {
                    RectTransform sprite = sprites[i];
                    if (sprite == null)
                    {
                        continue;
                    }

                    sprite.anchoredPosition = Vector2.Lerp(startPositions[i], startPositions[i] + offset, eased);
                }

                yield return null;
            }

            for (int i = 0; i < sprites.Count && i < startPositions.Count; i++)
            {
                RectTransform sprite = sprites[i];
                if (sprite != null)
                {
                    sprite.anchoredPosition = startPositions[i] + offset;
                }
            }
        }

        private RectTransform GetBoardSpriteRect(bool isAllyBoard, GridPos position)
        {
            TMP_Text cellLabel = GetBoardCellLabel(isAllyBoard, position);
            if (cellLabel == null || cellLabel.transform.parent == null)
            {
                return null;
            }

            Transform spriteTransform = cellLabel.transform.parent.Find("BattleSpriteImage");
            return spriteTransform == null ? null : spriteTransform as RectTransform;
        }
        private IEnumerator PlayPendingDamageHitReactions()
        {
            List<ActionValuePopup> damagePopups = GetPendingDamagePopups();
            if (damagePopups.Count == 0)
            {
                yield break;
            }

            float duration = Mathf.Max(0f, targetHitShakeSeconds);
            float distance = Mathf.Max(0f, targetHitShakeDistance);
            int shakeCount = Mathf.Max(1, targetHitShakeCount);

            if (duration <= 0f || distance <= 0f)
            {
                yield break;
            }

            var sprites = new List<RectTransform>();
            var startPositions = new List<Vector2>();

            for (int i = 0; i < damagePopups.Count; i++)
            {
                ActionValuePopup popup = damagePopups[i];
                if (popup == null)
                {
                    continue;
                }

                RectTransform spriteRect = GetBoardSpriteRect(popup.IsAllyBoard, popup.Position);
                if (spriteRect == null || sprites.Contains(spriteRect))
                {
                    continue;
                }

                sprites.Add(spriteRect);
                startPositions.Add(spriteRect.anchoredPosition);
            }

            if (sprites.Count == 0)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI * 2f * shakeCount);
                Vector2 offset = new Vector2(wave * distance, 0f);

                for (int i = 0; i < sprites.Count && i < startPositions.Count; i++)
                {
                    RectTransform sprite = sprites[i];
                    if (sprite == null)
                    {
                        continue;
                    }

                    sprite.anchoredPosition = startPositions[i] + offset;
                }

                yield return null;
            }

            for (int i = 0; i < sprites.Count && i < startPositions.Count; i++)
            {
                RectTransform sprite = sprites[i];
                if (sprite != null)
                {
                    sprite.anchoredPosition = startPositions[i];
                }
            }
        }

        private List<ActionValuePopup> GetPendingDamagePopups()
        {
            var damagePopups = new List<ActionValuePopup>();

            for (int i = 0; i < _pendingActionValuePopups.Count; i++)
            {
                ActionValuePopup popup = _pendingActionValuePopups[i];
                if (popup == null || string.IsNullOrEmpty(popup.Text))
                {
                    continue;
                }

                if (popup.Text.StartsWith("-"))
                {
                    damagePopups.Add(popup);
                }
            }

            return damagePopups;
        }
        private IEnumerator PlayPendingDefeatFadeOuts()
        {
            List<ActionValuePopup> damagePopups = GetPendingDamagePopups();
            if (damagePopups.Count == 0)
            {
                yield break;
            }

            var sprites = new List<Image>();
            var rects = new List<RectTransform>();
            var startColors = new List<Color>();
            var startPositions = new List<Vector2>();
            var positions = new List<GridPos>();
            var koStatusUnits = new List<BattleUnit>();
            var statusCanvasGroups = new List<CanvasGroup>();
            var statusStartAlphas = new List<float>();

            for (int i = 0; i < damagePopups.Count; i++)
            {
                ActionValuePopup popup = damagePopups[i];
                if (popup == null || popup.IsAllyBoard)
                {
                    continue;
                }

                BattleUnit unit = _grid.GetUnit(false, popup.Position);
                if (unit == null || !unit.IsDead)
                {
                    continue;
                }

                RectTransform rect = GetBoardSpriteRect(false, popup.Position);
                if (rect == null || rects.Contains(rect))
                {
                    continue;
                }

                Image image = rect.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                sprites.Add(image);
                rects.Add(rect);
                startColors.Add(image.color);
                startPositions.Add(rect.anchoredPosition);
                positions.Add(popup.Position);
                if (!koStatusUnits.Contains(unit))
                {
                    koStatusUnits.Add(unit);
                }

                CanvasGroup statusGroup = GetOrAddEnemyStatusCanvasGroup(unit);
                if (statusGroup != null && !statusCanvasGroups.Contains(statusGroup))
                {
                    statusCanvasGroups.Add(statusGroup);
                    statusStartAlphas.Add(statusGroup.alpha);
                }
            }

            if (sprites.Count == 0)
            {
                yield break;
            }

            float duration = Mathf.Max(0f, defeatFadeSeconds);
            float sink = Mathf.Max(0f, defeatSinkDistance);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);

                for (int i = 0; i < sprites.Count && i < rects.Count; i++)
                {
                    if (sprites[i] == null || rects[i] == null)
                    {
                        continue;
                    }

                    Color color = startColors[i];
                    color.a = Mathf.Lerp(startColors[i].a, 0f, eased);
                    sprites[i].color = color;
                    rects[i].anchoredPosition = startPositions[i] + new Vector2(0f, -sink * eased);
                }

                for (int i = 0; i < statusCanvasGroups.Count && i < statusStartAlphas.Count; i++)
                {
                    CanvasGroup group = statusCanvasGroups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    ApplyEnemyStatusKoFadeAlpha(group, statusStartAlphas[i], elapsed, duration);
                }

                yield return null;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                _grid.SetUnit(false, positions[i], null);
            }

            for (int i = 0; i < koStatusUnits.Count; i++)
            {
                _enemyStatusKoVisibleUnits.Remove(koStatusUnits[i]);
            }

            _pendingEnemyKoReplacementPhase = true;
        }
        private void ShowPendingFloatingHpBars()
        {
            if (_pendingActionValuePopups == null || _pendingActionValuePopups.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingActionValuePopups.Count; i++)
            {
                ActionValuePopup popup = _pendingActionValuePopups[i];
                if (popup == null || string.IsNullOrEmpty(popup.Text))
                {
                    continue;
                }

                if (!popup.Text.StartsWith("-") && !popup.Text.StartsWith("+"))
                {
                    continue;
                }

                BattleUnit unit = _grid.GetUnit(popup.IsAllyBoard, popup.Position);
                if (unit == null || unit.Data == null)
                {
                    continue;
                }

                FloatingHPBarView floatingBar = GetOrCreateFloatingHpBar(popup.IsAllyBoard, popup.Position, unit);
                if (floatingBar == null)
                {
                    continue;
                }

                if (popup.HasHpSnapshot)
                {
                    floatingBar.ShowTransition(
                        popup.PreviousHP,
                        popup.CurrentHP,
                        popup.MaxHP,
                        hpBarAnimationSeconds,
                        floatingHpBarVisibleSeconds,
                        floatingHpBarFadeSeconds);
                }
                else
                {
                    floatingBar.Show(
                        unit.CurrentHP,
                        unit.Data.MaxHP,
                        hpBarAnimationSeconds,
                        floatingHpBarVisibleSeconds,
                        floatingHpBarFadeSeconds);
                }
            }
        }

        private Vector2 GetFloatingHpBarOffset(BattleUnit unit)
        {
            if (unit != null && unit.Data != null && unit.Data.OverrideFloatingHPBarOffset)
            {
                return unit.Data.FloatingHPBarOffset;
            }

            return floatingHpBarOffset;
        }

        private FloatingHPBarView GetOrCreateFloatingHpBar(bool isAllyBoard, GridPos position, BattleUnit unit)
        {
            TMP_Text cellLabel = GetBoardCellLabel(isAllyBoard, position);
            if (cellLabel == null || cellLabel.transform.parent == null)
            {
                return null;
            }

            Transform cellRoot = cellLabel.transform.parent;
            Transform existing = cellRoot.Find("FloatingHPBarRoot");
            if (existing != null)
            {
                return existing.GetComponent<FloatingHPBarView>();
            }

            GameObject rootObject = new GameObject("FloatingHPBarRoot", typeof(RectTransform));
            rootObject.transform.SetParent(cellRoot, false);

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = floatingHpBarSize;
            rootRect.anchoredPosition = GetFloatingHpBarOffset(unit);

            Image bg = rootObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            bg.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
            fillObject.transform.SetParent(rootObject.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 1f, 0.35f, 0.95f);
            fillImage.raycastTarget = false;

            FloatingHPBarView view = rootObject.AddComponent<FloatingHPBarView>();
            rootObject.SetActive(false);
            return view;
        }
        private void ApplyEnemyStatusKoFadeAlpha(CanvasGroup group, float startAlpha, float elapsed, float duration)
        {
            if (group == null)
            {
                return;
            }

            float delay = Mathf.Max(0f, enemyStatusKoFadeDelaySeconds);
            if (elapsed < delay)
            {
                group.alpha = startAlpha;
                return;
            }

            float remaining = Mathf.Max(0.0001f, duration - delay);
            float t = duration <= 0f ? 1f : Mathf.Clamp01((elapsed - delay) / remaining);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            group.alpha = Mathf.Lerp(startAlpha, 0f, eased);
        }
        private CanvasGroup GetOrAddEnemyStatusCanvasGroup(BattleUnit unit)
        {
            Transform slot = GetEnemyStatusSlotForUnit(unit);
            if (slot == null)
            {
                return null;
            }

            CanvasGroup group = slot.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = slot.gameObject.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private Transform GetEnemyStatusSlotForUnit(BattleUnit unit)
        {
            if (enemyStatusPanel == null || unit == null)
            {
                return null;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] != unit)
                {
                    continue;
                }

                return enemyStatusPanel.Find($"EnemyStatus_{i + 1}");
            }

            return null;
        }

        private void ResetEnemyStatusCanvasGroupAlphas()
        {
            if (enemyStatusPanel == null)
            {
                return;
            }

            for (int i = 1; i <= 4; i++)
            {
                Transform slot = enemyStatusPanel.Find($"EnemyStatus_{i}");
                if (slot == null)
                {
                    continue;
                }

                CanvasGroup group = slot.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = 1f;
                }
            }
        }
        private IEnumerator PlayPendingAutoReplacementAnimations()
        {
            if (_pendingEnemyKoReplacementPhase)
            {
                _pendingEnemyKoReplacementPhase = false;

                CompactEnemyFrontlineIfEmpty();
                bool replacementOccurred = FillEmptyEnemyCellsFromReserves();
                _statusSlotUnits.Clear();
                RedrawBoard();
                ResetEnemyStatusCanvasGroupAlphas();

                if (replacementOccurred)
                {
                    _pendingEnemyAutoReplacementEnterAnimation = true;
                }
            }

            if (_pendingEnemyAutoReplacementEnterAnimation)
            {
                _pendingEnemyAutoReplacementEnterAnimation = false;
                yield return PlayAutoReplacementEnterAnimation(false);
            }
        }
        private IEnumerator PlayAutoReplacementEnterAnimation(bool isAllyBoard)
        {
            float duration = Mathf.Max(0f, autoReplacementEnterSeconds);
            float distance = Mathf.Max(0f, autoReplacementEnterDistance);
            if (duration <= 0f || distance <= 0f)
            {
                yield break;
            }

            var sprites = new List<RectTransform>();
            var endPositions = new List<Vector2>();
            AddBoardSpriteRectIfPresent(isAllyBoard, GridPos.FrontTop, sprites, endPositions);
            AddBoardSpriteRectIfPresent(isAllyBoard, GridPos.FrontBottom, sprites, endPositions);
            AddBoardSpriteRectIfPresent(isAllyBoard, GridPos.BackTop, sprites, endPositions);
            AddBoardSpriteRectIfPresent(isAllyBoard, GridPos.BackBottom, sprites, endPositions);

            if (sprites.Count == 0)
            {
                yield break;
            }

            Vector2 enterOffset = new Vector2(isAllyBoard ? -distance : distance, 0f);
            for (int i = 0; i < sprites.Count && i < endPositions.Count; i++)
            {
                if (sprites[i] != null)
                {
                    sprites[i].anchoredPosition = endPositions[i] + enterOffset;
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);

                for (int i = 0; i < sprites.Count && i < endPositions.Count; i++)
                {
                    RectTransform sprite = sprites[i];
                    if (sprite == null)
                    {
                        continue;
                    }

                    sprite.anchoredPosition = Vector2.Lerp(endPositions[i] + enterOffset, endPositions[i], eased);
                }

                yield return null;
            }

            for (int i = 0; i < sprites.Count && i < endPositions.Count; i++)
            {
                if (sprites[i] != null)
                {
                    sprites[i].anchoredPosition = endPositions[i];
                }
            }
        }

        private void AddBoardSpriteRectIfPresent(bool isAllyBoard, GridPos position, List<RectTransform> sprites, List<Vector2> endPositions)
        {
            if (sprites == null || endPositions == null)
            {
                return;
            }

            RectTransform rect = GetBoardSpriteRect(isAllyBoard, position);
            if (rect == null || sprites.Contains(rect))
            {
                return;
            }

            sprites.Add(rect);
            endPositions.Add(rect.anchoredPosition);
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
            if (action == null || action.Skill == null)
            {
                ClearPreviewEnemyActionState(enemy);
                AdvanceToNextActor();
                yield break;
            }

            ShowActionOverlay(action.Skill.SkillName, enemy.Name);

            float delay = Mathf.Max(0f, actionIntroDelaySeconds);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (enemy == null || enemy.IsDead || _battleEnded)
            {
                yield break;
            }

            BeginDeferredHpBarFill();
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

            int beforeHp = target.CurrentHP;
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);
            AddPendingActionValuePopup(true, targetPosition, $"-{finalDamage}", beforeHp, target.CurrentHP, target.Data.MaxHP);

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
                EndBattleClear();
                return;
            }

            if (!HasAliveActiveAllies() && !HasAliveAllyReserves())
            {
                ShowQuestFailedPanel();
            }
        }

        private void EndBattleClear()
        {
            BattleClearResult result = CreateBattleClearResult();
            ApplyKakeraReward(result);
            ApplyExpReward(result);
            ApplyBattleClearRewards(result);

            _battleEnded = true;
            _phase = BattlePhase.BattleEnded;
            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            HideActionOverlay();
            ShowBattleResultPanel(result);
            RedrawBoard();

            Debug.Log($"[Battle] Clear: {BattleClearRewardCalculator.FormatRank(result.Rank)}, Kakera +{BattleClearRewardCalculator.CalculateKakeraGain(result.Rank)}.");
        }

        private BattleClearResult CreateBattleClearResult()
        {
            EnsureWaveProgress();

            BattleClearRank rank = EvaluateBattleClearRank();

            bool hasNextWave = _questProgress != null && _questProgress.HasNextRoutePoint;

            BattleClearResult result = new BattleClearResult
            {
                Rank = rank,
                PartyHealAmount = rank == BattleClearRank.OneTurn
                    ? _oneTurnClearPartyHeal
                    : 0,
                BattleNumber = GetCurrentBattleNumber(),
                TotalBattles = GetTotalBattleCount(),
                HasNextWave = hasNextWave
            };

            return result;
        }

        private void EnsureWaveProgress()
        {
            if (_waveProgress == null)
            {
                _waveProgress = new WaveProgressState();
            }
        }

        private int GetCurrentBattleNumber()
        {
            if (_questProgress == null)
            {
                return 1;
            }

            return _questProgress.CurrentWaveIndex + 1;
        }

        private int GetTotalBattleCount()
        {
            if (_questProgress == null || _questProgress.Quest == null)
            {
                return 1;
            }

            return Mathf.Max(1, _questProgress.Quest.Waves.Count);
        }

        private void ApplyExpReward(BattleClearResult result)
        {
            if (result == null)
            {
                return;
            }

            int expGain = BattleClearRewardCalculator.CalculateExpGain(result);
            _totalExpEarned += Mathf.Max(0, expGain);

            Debug.Log($"[EXP] Gain +{expGain}. TotalEarned={_totalExpEarned}.");
        }

        private void ApplyKakeraReward(BattleClearResult result)
        {
            if (result == null)
            {
                return;
            }

            int gain = BattleClearRewardCalculator.CalculateKakeraGain(result.Rank);
            int before = _kakeraStock;

            _kakeraStock = Mathf.Clamp(_kakeraStock + gain, 0, MaxKakeraStock);
            _totalKakeraEarned += Mathf.Max(0, gain);

            Debug.Log($"[Kakera] Gain +{gain}. Stock {before}->{_kakeraStock}/{MaxKakeraStock}, TotalEarned={_totalKakeraEarned}.");
        }

        private void ApplyBattleClearRewards(BattleClearResult result)
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

        private BattleClearRank EvaluateBattleClearRank()
        {
            EnsureWaveProgress();
            return BattleClearRewardCalculator.EvaluateRank(_waveProgress.WaveTurn);
        }

        private static int HealLivingPartyMembers(List<BattleUnit> units, int healAmount)
        {
            return BattlePartyStateUtility.HealLivingMembers(units, healAmount);
        }

        private static int CountLivingPartyMembers(List<BattleUnit> units)
        {
            return BattlePartyStateUtility.CountLivingMembers(units);
        }

        private static int CountKnownPartyMembers(List<BattleUnit> units)
        {
            return BattlePartyStateUtility.CountKnownMembers(units);
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
            // Enemy action preview is intentionally disabled.
        }

        private void UpdateEnemyActionPreview()
        {
            // Enemy action preview is intentionally disabled.
        }

        private void SetEnemyActionPreviewVisible(bool visible)
        {
            if (_enemyActionPreviewPanelObject != null)
            {
                _enemyActionPreviewPanelObject.SetActive(false);
            }
        }

        private Canvas GetOverlayCanvas()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null && commandPanel != null)
            {
                canvas = commandPanel.GetComponentInParent<Canvas>();
            }

            return canvas;
        }

        private void EnsureRouteOverlayPanels()
        {
            _routeOverlayPresenter.Ensure(GetOverlayCanvas(), FindUiGameObjectByName);
        }

        private void HideRouteOverlayPanels()
        {
            EnsureRouteOverlayPanels();
            _routeOverlayPresenter.HideAll();
        }

        private void PrepareRouteOverlayForOverlay()
        {
            EnsureRouteOverlayPanels();

            _battleEnded = true;
            _phase = BattlePhase.BattleEnded;

            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            HideActionOverlay();
            HideResultPanel();
            HideRouteOverlayPanels();
        }

        // Result UI
        private void EnsureResultPanel()
        {
            _resultPanelPresenter.Ensure(GetOverlayCanvas(), FindUiGameObjectByName, HandleResultFormationClicked, HandleResultReturnClicked);
        }

        private void HandleResultFormationClicked()
        {
            if (_showingBattlePreparation)
            {
                HandlePreparationScoutClicked();
                return;
            }

            Debug.Log("[Result] Formation clicked.");

            EnsureResultPanel();
            _resultPanelPresenter.SetBody(
                "Formation / Preparation\n" +
                $"Party: {BuildPartyOverviewText()}\n" +
                $"Kakera: {_kakeraStock}/{MaxKakeraStock}\n" +
                "Item / Skill / Link check: deferred");
        }

        private void HandlePreparationScoutClicked()
        {
            if (_questProgress == null)
            {
                return;
            }

            RoutePointData point = _questProgress.CurrentBattleRoutePoint;
            if (point == null || !point.HasBattleData)
            {
                return;
            }

            if (IsRoutePointScouted(point))
            {
                RefreshBattlePreparationPanel(point);
                return;
            }

            if (_kakeraStock <= 0)
            {
                Debug.Log("[Preparation] Scout failed. Kakera is empty.");
                RefreshBattlePreparationPanel(point);
                return;
            }

            _kakeraStock = Mathf.Max(0, _kakeraStock - 1);
            _scoutedWaveIndices.Add(point.WaveIndex);

            Debug.Log($"[Preparation] Scouted {point.DisplayName}. Kakera={_kakeraStock}/{MaxKakeraStock}.");

            RefreshBattlePreparationPanel(point);
        }

        private string BuildPartyOverviewText()
        {
            int livingCount = CountLivingPartyMembers(_allies) + CountLivingPartyMembers(_reserves);
            int totalCount = CountKnownPartyMembers(_allies) + CountKnownPartyMembers(_reserves);

            return $"{livingCount}/{Mathf.Max(1, totalCount)} alive";
        }

        private void HandleResultReturnClicked()
        {
            if (_showingQuestFailed)
            {
                Debug.Log("[Quest] Failed return to Base clicked.");
                _showingQuestFailed = false;
                ReturnToBase();
                return;
            }

            if (_showingQuestResult)
            {
                Debug.Log("[Quest] Return to Base clicked.");
                _showingQuestResult = false;
                ReturnToBase();
                return;
            }

            if (_showingBattleResult)
            {
                Debug.Log("[Result] Battle Result Next clicked. Showing Movement before route advance.");
                _showingBattleResult = false;

                if (_questProgress == null)
                {
                    ShowQuestResultPanel();
                    return;
                }

                if (!_questProgress.HasNextRoutePoint)
                {
                    ShowQuestResultPanel();
                    return;
                }

                ShowRouteMovementPanel();
                return;
            }

            if (_showingRouteEvent)
            {
                Debug.Log("[Route] Event Next clicked.");
                _showingRouteEvent = false;
                ShowRouteMovementPanel();
                return;
            }

            if (_showingRouteMovement)
            {
                Debug.Log("[Route] Movement Next clicked.");
                _showingRouteMovement = false;
                ContinueRouteAdvance();
                return;
            }

            if (_showingBattlePreparation)
            {
                Debug.Log("[Preparation] Start Battle clicked.");
                _showingBattlePreparation = false;
                StartBattleAtCurrentRoutePoint();
                return;
            }

            Debug.Log("[Result] Fallback Next clicked.");

            if (_questProgress == null)
            {
                ReturnToBase();
                return;
            }

            if (!_questProgress.HasNextRoutePoint)
            {
                ShowQuestResultPanel();
                return;
            }

            ShowRouteMovementPanel();
        }

        private void PrepareResultPanelForOverlay()
        {
            EnsureResultPanel();
            HideRouteOverlayPanels();

            _battleEnded = true;
            _phase = BattlePhase.BattleEnded;

            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            SetCommandUiVisible(false);
            HideActionOverlay();

            _resultPanelPresenter.SetVisible(true);
        }

        private void ApplyResultPanelVisualStyle(Color panelColor, TextAlignmentOptions bodyAlignment, float titleFontSize, float bodyFontSize)
        {
            EnsureResultPanel();
            _resultPanelPresenter.ApplyVisualStyle(panelColor, bodyAlignment, titleFontSize, bodyFontSize);
        }

        private void SetResultTitleAndBody(string title, string body)
        {
            EnsureResultPanel();
            _resultPanelPresenter.SetTitleAndBody(title, body);
        }

        private void SetResultReturnButtonHandler(UnityEngine.Events.UnityAction handler)
        {
            EnsureResultPanel();
            _resultPanelPresenter.SetRightButtonHandler(handler);
        }

        private void HandleBattleResultNextClicked()
        {
            Debug.Log("[Result] Battle Result Next clicked. Showing Movement before route advance.");
            _showingBattleResult = false;

            if (_questProgress == null)
            {
                ShowQuestResultPanel();
                return;
            }

            if (!_questProgress.HasNextRoutePoint)
            {
                ShowQuestResultPanel();
                return;
            }

            ShowRouteMovementPanel();
        }

        private void HandleRouteMovementMoveClicked()
        {
            Debug.Log("[Route] Movement Move clicked. Advancing route.");
            _showingRouteMovement = false;
            ContinueRouteAdvance();
        }

        private void HandleRouteEventNextClicked()
        {
            Debug.Log("[Route] Event Next clicked. Showing Movement before next route advance.");
            _showingRouteEvent = false;
            ShowRouteMovementPanel();
        }

        private void HandleBattlePreparationStartClicked()
        {
            Debug.Log("[Preparation] Start Battle clicked.");
            _showingBattlePreparation = false;
            StartBattleAtCurrentRoutePoint();
        }

        private void HandleQuestReturnToBaseClicked()
        {
            Debug.Log("[Quest] Return to Base clicked.");
            _showingQuestResult = false;
            _showingQuestFailed = false;
            ReturnToBase();
        }

        private void SetResultButtons(
            bool showLeftButton,
            string leftText,
            bool leftInteractable,
            string rightText)
        {
            EnsureResultPanel();
            _resultPanelPresenter.SetButtons(showLeftButton, leftText, leftInteractable, rightText);
        }

        private void HideResultLeftButton(string rightText)
        {
            SetResultButtons(false, string.Empty, false, rightText);
        }

        private void ShowQuestFailedPanel()
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = true;

            PrepareResultPanelForOverlay();
            ApplyResultPanelVisualStyle(new Color(0f, 0f, 0f, 0.78f), TextAlignmentOptions.Center, 38f, 22f);
            SetResultTitleAndBody("Quest Failed", BuildQuestEndSummaryText());
            HideResultLeftButton("Return to Base");
            SetResultReturnButtonHandler(HandleQuestReturnToBaseClicked);

            RedrawBoard();

            Debug.Log("[Quest] Quest Failed shown.");
        }

        private string BuildQuestEndSummaryText()
        {
            return ResultTextBuilder.BuildQuestEndSummaryText(
                CountClearedBattleRoutePoints(),
                CountTotalBattleRoutePoints(),
                _totalKakeraEarned,
                _totalExpEarned);
        }

        private void ShowQuestResultPanel()
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = true;
            _showingQuestFailed = false;

            PrepareResultPanelForOverlay();
            ApplyResultPanelVisualStyle(new Color(0f, 0f, 0f, 0.78f), TextAlignmentOptions.Center, 38f, 22f);
            SetResultTitleAndBody("Quest Clear", BuildQuestEndSummaryText());
            HideResultLeftButton("Return to Base");
            SetResultReturnButtonHandler(HandleQuestReturnToBaseClicked);

            Debug.Log("[Quest] Quest Result shown.");
        }

        private int CountTotalBattleRoutePoints()
        {
            return QuestBattleCountUtility.CountTotalBattleRoutePoints(_questProgress, GetTotalBattleCount());
        }

        private int CountClearedBattleRoutePoints()
        {
            return QuestBattleCountUtility.CountClearedBattleRoutePoints(_questProgress, GetCurrentBattleNumber());
        }

        private void ShowRouteMovementPanel()
        {
            _showingRouteEvent = false;
            _showingRouteMovement = true;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay();
            _routeOverlayPresenter.ShowMovement(BuildRouteMovementText(), HandleRouteMovementMoveClicked);

            Debug.Log("[Route] Movement panel shown.");
        }

        private string BuildRouteMovementText()
        {
            return RouteOverlayTextBuilder.BuildRouteMovementText(_questProgress);
        }

        private void ContinueRouteAdvance()
        {
            int routeCount = _questProgress == null || _questProgress.Quest == null || _questProgress.Quest.RoutePoints == null
                ? 0
                : _questProgress.Quest.RoutePoints.Count;
            bool hasNext = _questProgress != null && _questProgress.HasNextRoutePoint;
            int currentIndex = _questProgress == null ? -1 : _questProgress.CurrentRoutePointIndex;

            Debug.Log($"[Route] Advance start. CurrentIndex={currentIndex}, RouteCount={routeCount}, HasNext={hasNext}.");

            RouteAdvanceResult result = RouteAdvanceResolver.Advance(_questProgress);
            if (result != null)
            {
                for (int i = 0; i < result.Logs.Count; i++)
                {
                    Debug.Log(result.Logs[i]);
                }
            }

            if (result == null)
            {
                ShowQuestResultPanel();
                return;
            }

            switch (result.DestinationType)
            {
                case RouteAdvanceDestinationType.Event:
                    ShowRouteEventPanel(result.Point);
                    return;

                case RouteAdvanceDestinationType.BattlePreparation:
                    ShowBattlePreparationPanel(result.Point);
                    return;

                case RouteAdvanceDestinationType.QuestResult:
                default:
                    ShowQuestResultPanel();
                    return;
            }
        }

        private void ShowRouteEventPanel(RoutePointData point)
        {
            _showingRouteEvent = true;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay();

            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Route Event"
                : point.DisplayName;

            _routeOverlayPresenter.ShowEvent(BuildRouteEventText(point), HandleRouteEventNextClicked);

            Debug.Log($"[Route] Event shown: {displayName}");
        }

        private string BuildRouteEventText(RoutePointData point)
        {
            return RouteOverlayTextBuilder.BuildRouteEventText(point);
        }

        private void ShowBattlePreparationPanel(RoutePointData point)
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = true;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareRouteOverlayForOverlay();

            string title = point != null && point.PointType == RoutePointType.Boss
                ? "BOSS PREPARATION"
                : "BATTLE PREPARATION";

            _routeOverlayPresenter.ShowPreparation(title, BuildBattlePreparationText(point), CanScoutRoutePoint(point), HandlePreparationScoutClicked, HandleBattlePreparationStartClicked);

            string displayName = point == null || string.IsNullOrEmpty(point.DisplayName)
                ? "Battle Point"
                : point.DisplayName;

            Debug.Log($"[Preparation] Shown for {displayName}.");
        }

        private string BuildBattlePreparationText(RoutePointData point)
        {
            return RouteOverlayTextBuilder.BuildBattlePreparationText(
                point,
                BuildPartyOverviewText(),
                _kakeraStock,
                MaxKakeraStock,
                IsRoutePointScouted(point),
                GetWaveDataForRoutePoint(point));
        }

        private void RefreshBattlePreparationPanel(RoutePointData point)
        {
            EnsureRouteOverlayPanels();
            _routeOverlayPresenter.RefreshPreparation(BuildBattlePreparationText(point), CanScoutRoutePoint(point), HandlePreparationScoutClicked, HandleBattlePreparationStartClicked);
        }

        private void RefreshBattlePreparationButtons(RoutePointData point)
        {
            EnsureRouteOverlayPanels();
            _routeOverlayPresenter.SetPreparationButtons(CanScoutRoutePoint(point), HandlePreparationScoutClicked, HandleBattlePreparationStartClicked);
        }

        private bool CanScoutRoutePoint(RoutePointData point)
        {
            return point != null
                && point.HasBattleData
                && !IsRoutePointScouted(point)
                && _kakeraStock > 0;
        }

        private bool IsRoutePointScouted(RoutePointData point)
        {
            return point != null
                && point.WaveIndex >= 0
                && _scoutedWaveIndices.Contains(point.WaveIndex);
        }

        private WaveData GetWaveDataForRoutePoint(RoutePointData point)
        {
            if (_questProgress == null || _questProgress.Quest == null || point == null)
            {
                return null;
            }

            if (point.WaveIndex < 0 || point.WaveIndex >= _questProgress.Quest.Waves.Count)
            {
                return null;
            }

            return _questProgress.Quest.Waves[point.WaveIndex];
        }

        private void StartBattleAtCurrentRoutePoint()
        {
            if (_questProgress == null)
            {
                ReturnToBase();
                return;
            }

            RoutePointData point = _questProgress.CurrentBattleRoutePoint;
            if (point == null)
            {
                ReturnToBase();
                return;
            }

            StopAllCoroutines();

            ResetOverlayAndPreviewBeforeBattleStart();
            ResetRouteAndResultFlagsForBattleStart();
            ResetBattleRuntimeStateForBattleStart();
            ReplaceEnemyWave(_questProgress.CurrentWave);
            StartCurrentWaveProgress();
            EnterInitialActorForStartedBattle();
            RestoreBattleCommandUi();
            RedrawBoard();

            Debug.Log($"[Route] Started battle point: {point.DisplayName} ({point.PointType}), WaveIndex={_questProgress.CurrentWaveIndex}.");
        }

        private void ResetOverlayAndPreviewBeforeBattleStart()
        {
            HideResultPanel();
            HideRouteOverlayPanels();
            HideActionOverlay();
            ClearTargetPreview();
            ResetEnemyActionPreviewHighlights();
            SetEnemyActionPreviewVisible(false);
            ClearPendingActionFlashTargets();
            ClearPendingActionValuePopups();
        }

        private void ResetRouteAndResultFlagsForBattleStart()
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;
        }

        private void ResetBattleRuntimeStateForBattleStart()
        {
            _battleEnded = false;
            _phase = BattlePhase.CommandSelect;
            _formationSettling = false;
            _hoveredSkill = null;

            _actedUnits.Clear();
            _enemyStatusKoVisibleUnits.Clear();
            _pendingEnemyAutoReplacementEnterAnimation = false;
            _pendingEnemyKoReplacementPhase = false;
            _statusSlotUnits.Clear();
            _turnNumbers.Clear();
            _previewEnemyActionStates.Clear();
        }

        private void StartCurrentWaveProgress()
        {
            EnsureWaveProgress();
            _waveProgress.StartWave();
            RecoverAllAllyMP();
            RebuildTurnOrder();
        }

        private void EnterInitialActorForStartedBattle()
        {
            BattleUnit nextAlly = FindNextUnactedAlly();
            if (nextAlly != null)
            {
                EnterCommandSelect(nextAlly);
                return;
            }

            CheckBattleEnd();
        }

        private void RestoreBattleCommandUi()
        {
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
        }

        private void ReplaceEnemyWave(WaveData wave)
        {
            BattleUnitPlacementApplier.ReplaceEnemyWave(_grid, wave, _enemies, _enemyReserves);
        }

        private void ClearEnemyBoardAndLists()
        {
            BattleUnitPlacementApplier.ClearEnemySide(_grid, _enemies, _enemyReserves);
        }

        private void ReturnToBase()
        {
            // 現時点では拠点画面がないため、拠点帰還処理の仮実装としてBattleを再初期化する。
            // BootstrapBattle()により、味方HP/MP/KO状態・Inventory・QuestProgressは初期状態に戻る。
            _kakeraStock = 0;
            _totalKakeraEarned = 0;
            _totalExpEarned = 0;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            Debug.Log("[Base] Returned to base. Party state, Kakera, and EXP display totals will be reset by restarting the default quest.");

            RestartBattle();
        }

        private void RestartBattle()
        {
            StopAllCoroutines();

            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = false;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            HideResultPanel();
            HideRouteOverlayPanels();
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

        private void ShowBattleResultPanel(BattleClearResult result)
        {
            _showingRouteEvent = false;
            _showingRouteMovement = false;
            _showingBattlePreparation = false;
            _showingBattleResult = true;
            _showingQuestResult = false;
            _showingQuestFailed = false;

            PrepareResultPanelForOverlay();
            ApplyResultPanelVisualStyle(new Color(0f, 0f, 0f, 0.78f), TextAlignmentOptions.Center, 38f, 22f);
            SetResultTitleAndBody("Battle Result", BuildBattleResultSubText(result));
            SetResultButtons(true, "Formation", true, "Next");

            SetResultReturnButtonHandler(HandleBattleResultNextClicked);

            Debug.Log("[Battle] Battle Result shown.");
        }

        private string BuildBattleResultSubText(BattleClearResult result)
        {
            int kakeraGain = result == null ? 0 : BattleClearRewardCalculator.CalculateKakeraGain(result.Rank);
            int expGain = result == null ? 0 : BattleClearRewardCalculator.CalculateExpGain(result);
            int partyHealAmount = result == null ? 0 : result.PartyHealAmount;

            return ResultTextBuilder.BuildBattleResultText(
                result != null,
                result == null ? string.Empty : BattleClearRewardCalculator.FormatRank(result.Rank),
                _kakeraStock,
                MaxKakeraStock,
                kakeraGain,
                expGain,
                partyHealAmount);
        }

        private void ShowResultPanel(string result)
        {
            EnsureResultPanel();
            HideRouteOverlayPanels();
            _resultPanelPresenter.SetVisible(true);
            _resultPanelPresenter.SetTitleAndBody(result, "Battle End");
            _resultPanelPresenter.SetButtons(false, string.Empty, false, "Next");
        }

        private void HideResultPanel()
        {
            EnsureResultPanel();
            _resultPanelPresenter.SetVisible(false);
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
            RedrawTurnOrderBar();
            RedrawActiveHighlights();
            RedrawTargetPreview();

            if (!_battleEnded && _phase == BattlePhase.CommandSelect)
            {
                RedrawEnemyActionPreviewHighlights();
            }
        }

        private void RedrawStatusPanels()
        {
            List<BattleUnit> enemyStatusUnits = GetEnemyStatusDisplayUnits();

            for (int i = 0; i < 4; i++)
            {
                RedrawEnemyStatusSlot(i + 1, GetUnitAt(enemyStatusUnits, i));
                RedrawAllyStatusSlot(i + 1, GetUnitAt(_allies, i));
            }

            ResizeEnemyStatusPanel(enemyStatusUnits.Count);
            LayoutEnemyStatusSlots(enemyStatusUnits.Count);
        }

        private List<BattleUnit> GetEnemyStatusDisplayUnits()
        {
            var result = new List<BattleUnit>();

            for (int i = 0; i < _enemies.Count; i++)
            {
                BattleUnit unit = _enemies[i];
                if (unit == null)
                {
                    continue;
                }

                if (!unit.IsDead || _enemyStatusKoVisibleUnits.Contains(unit))
                {
                    result.Add(unit);
                }
            }

            return result;
        }
        private void RedrawTurnOrderBar()
        {
            if (CanGenerateTurnOrderSlots())
            {
                RedrawGeneratedTurnOrderSlots();

                if (turnOrderBarText != null)
                {
                    turnOrderBarText.gameObject.SetActive(false);
                }

                return;
            }

            if (turnOrderBarText == null)
            {
                return;
            }

            turnOrderBarText.gameObject.SetActive(true);
            turnOrderBarText.text = BuildTurnOrderBarText();
        }

        private bool CanGenerateTurnOrderSlots()
        {
            return turnOrderSlotTemplate != null
                && ((turnOrderSlotPositions != null && turnOrderSlotPositions.Length > 0)
                    || turnOrderSlotContainer != null);
        }

        private void RedrawGeneratedTurnOrderSlots()
        {
            List<BattleUnit> visibleOrder = GetVisibleTurnOrderUnits();
            int slotCount = GetTurnOrderSlotCapacity();
            EnsureGeneratedTurnOrderSlotCapacity(slotCount);

            if (turnOrderSlotTemplate != null && hideTurnOrderSlotTemplateOnPlay)
            {
                turnOrderSlotTemplate.SetVisible(false);
            }

            for (int i = 0; i < _generatedTurnOrderSlotViews.Count; i++)
            {
                TurnOrderSlotView slotView = _generatedTurnOrderSlotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                bool visible = i < visibleOrder.Count && IsUsableTurnOrderSlotIndex(i);
                slotView.SetVisible(visible);
                if (!visible)
                {
                    continue;
                }

                BattleUnit unit = visibleOrder[i];
                bool isAlly = _allies.Contains(unit);
                bool isCurrent = unit == _active && _phase == BattlePhase.CommandSelect && !_actedUnits.Contains(unit);
                bool isActed = _actedUnits.Contains(unit);
                slotView.SetUnit(unit, isAlly, isCurrent, isActed);
            }
        }

        private int GetTurnOrderSlotCapacity()
        {
            int positionCount = 0;
            if (turnOrderSlotPositions != null)
            {
                for (int i = 0; i < turnOrderSlotPositions.Length; i++)
                {
                    if (turnOrderSlotPositions[i] != null)
                    {
                        positionCount++;
                    }
                }
            }

            if (positionCount > 0)
            {
                return positionCount;
            }

            if (_turnOrder == null || _turnOrder.TurnOrder == null)
            {
                return 0;
            }

            return _turnOrder.TurnOrder.Count;
        }

        private bool IsUsableTurnOrderSlotIndex(int index)
        {
            if (turnOrderSlotPositions == null || turnOrderSlotPositions.Length == 0)
            {
                return true;
            }

            return index >= 0 && index < turnOrderSlotPositions.Length && turnOrderSlotPositions[index] != null;
        }

        private List<BattleUnit> GetVisibleTurnOrderUnits()
        {
            var units = new List<BattleUnit>();

            if (_turnOrder == null || _turnOrder.TurnOrder == null)
            {
                return units;
            }

            IReadOnlyList<BattleUnit> order = _turnOrder.TurnOrder;
            int maxCount = GetTurnOrderSlotCapacity();

            for (int i = 0; i < order.Count; i++)
            {
                BattleUnit unit = order[i];
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                if (maxCount > 0 && units.Count >= maxCount)
                {
                    break;
                }

                units.Add(unit);
            }

            return units;
        }

        private void EnsureGeneratedTurnOrderSlotCapacity(int requiredCount)
        {
            if (turnOrderSlotTemplate == null)
            {
                return;
            }

            for (int i = _generatedTurnOrderSlotViews.Count; i < requiredCount; i++)
            {
                Transform parent = GetTurnOrderSlotParent(i);
                if (parent == null)
                {
                    continue;
                }

                TurnOrderSlotView slotView = Instantiate(turnOrderSlotTemplate, parent);
                slotView.name = $"TurnOrderSlot_{i + 1}";
                slotView.SetVisible(true);

                RectTransform rect = slotView.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.localScale = Vector3.one;
                }

                _generatedTurnOrderSlotViews.Add(slotView);
            }
        }

        private Transform GetTurnOrderSlotParent(int index)
        {
            if (turnOrderSlotPositions != null
                && index >= 0
                && index < turnOrderSlotPositions.Length
                && turnOrderSlotPositions[index] != null)
            {
                return turnOrderSlotPositions[index];
            }

            return turnOrderSlotContainer;
        }

        private string BuildTurnOrderBarText()
        {
            if (_turnOrder == null || _turnOrder.TurnOrder == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            IReadOnlyList<BattleUnit> order = _turnOrder.TurnOrder;

            for (int i = 0; i < order.Count; i++)
            {
                BattleUnit unit = order[i];
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                string sidePrefix = _allies.Contains(unit) ? turnOrderAllyPrefix : turnOrderEnemyPrefix;
                string statePrefix = string.Empty;

                if (unit == _active && _phase == BattlePhase.CommandSelect && !_actedUnits.Contains(unit))
                {
                    statePrefix = currentTurnPrefix;
                }
                else if (_actedUnits.Contains(unit))
                {
                    statePrefix = actedTurnPrefix;
                }

                parts.Add($"{statePrefix}{sidePrefix}:{unit.Name}");
            }

            return string.Join(turnOrderSeparator, parts);
        }

        private static string BuildBoardMpBadgeText(BattleUnit unit)
        {
            if (unit == null || unit.IsDead || unit.Data == null)
            {
                return string.Empty;
            }

            return Mathf.Max(0, unit.CurrentMP).ToString();
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

        private int CountStatusVisibleEnemyUnits()
        {
            int count = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] != null)
                {
                    count++;
                }
            }

            return count;
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
                ClearStatusSlotUnit(slot);
                return;
            }

            SetLabel(slot, "Name", unit.Name);
            SetLabel(slot, "TurnNumber", BuildBoardMpBadgeText(unit));
            SetStatusFaceIcon(slot, unit);

            bool unitChanged = UpdateStatusSlotUnit(slot, unit);
            int currentHp = unit.CurrentHP;
            int maxHp = unit.Data.MaxHP;
            SetBarFill(slot, "HPBar", currentHp, maxHp, unitChanged);
            SetOrCreateLabel(slot, "Buffs", BuildBuffText(unit));
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            Transform direct = root.Find(childName);
            if (direct != null)
            {
                return direct;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
        private static Sprite GetStatusFaceIconSprite(BattleUnit unit)
        {
            CharacterData data = unit == null ? null : unit.Data;
            return data == null ? null : data.FaceIcon;
        }
        private static Transform FindStatusIconTransform(Transform slot)
        {
            if (slot == null)
            {
                return null;
            }

            Transform icon = FindChildRecursive(slot, "FaceIcon");
            if (icon != null)
            {
                return icon;
            }

            icon = FindChildRecursive(slot, "Icon");
            if (icon != null)
            {
                return icon;
            }

            return FindChildRecursive(slot, "IconImage");
        }
        private static void SetStatusFaceIcon(Transform slot, BattleUnit unit)
        {
            if (slot == null)
            {
                return;
            }

            Transform iconTransform = FindStatusIconTransform(slot);
            if (iconTransform == null)
            {
                return;
            }

            Image image = iconTransform.GetComponent<Image>();
            if (image == null)
            {
                image = iconTransform.gameObject.AddComponent<Image>();
            }

            Sprite sprite = GetStatusFaceIconSprite(unit);
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;

            Color color = image.color;
            color.a = unit != null && unit.IsDead ? 0.45f : 1f;
            image.color = color;
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
                ClearStatusSlotUnit(slot);
                return;
            }

            string displayName = unit.IsDead
                ? $"{unit.Name} KO"
                : unit.Name;

            SetLabel(slot, "Name", displayName);
            SetLabel(slot, "TurnNumber", BuildBoardMpBadgeText(unit));
            SetStatusFaceIcon(slot, unit);

            bool unitChanged = UpdateStatusSlotUnit(slot, unit);
            int currentHp = unit.IsDead ? 0 : unit.CurrentHP;
            int maxHp = unit.Data.MaxHP;
            SetBarFill(slot, "HPBar", currentHp, maxHp, unitChanged);
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

        private bool UpdateStatusSlotUnit(Transform slot, BattleUnit unit)
        {
            if (slot == null)
            {
                return false;
            }

            if (!_statusSlotUnits.TryGetValue(slot, out BattleUnit previous) || previous != unit)
            {
                _statusSlotUnits[slot] = unit;
                return true;
            }

            return false;
        }

        private void ClearStatusSlotUnit(Transform slot)
        {
            if (slot != null)
            {
                _statusSlotUnits.Remove(slot);
            }
        }
        private void SetBarFill(Transform root, string barName, int current, int max, bool immediate = false)
        {
            Transform fill = root.Find($"{barName}/Fill");
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            if (immediate)
            {
                SetBarFillRateImmediate(fill, rate);
                return;
            }

            if (Application.isPlaying && _deferHpBarFillUntilActionHit && barName == "HPBar")
            {
                _pendingHpBarFillRates[fill] = rate;
                return;
            }

            SetBarFillRate(fill, rate);
        }

        private void SetBarFillRateImmediate(Transform fill, float rate)
        {
            if (fill == null)
            {
                return;
            }

            HPBarFillAnimator animator = fill.GetComponent<HPBarFillAnimator>();
            if (animator == null)
            {
                animator = fill.gameObject.AddComponent<HPBarFillAnimator>();
            }

            animator.SetFillImmediate(rate);
        }
        private void SetBarFillRate(Transform fill, float rate)
        {
            if (fill == null)
            {
                return;
            }

            HPBarFillAnimator animator = fill.GetComponent<HPBarFillAnimator>();
            if (animator == null)
            {
                animator = fill.gameObject.AddComponent<HPBarFillAnimator>();
            }

            animator.SetAnimationSeconds(hpBarAnimationSeconds);
            animator.SetFill(rate);
        }

        private void BeginDeferredHpBarFill()
        {
            _deferHpBarFillUntilActionHit = true;
            _pendingHpBarFillRates.Clear();
        }

        private void ApplyDeferredHpBarFillUpdates()
        {
            if (!_deferHpBarFillUntilActionHit && _pendingHpBarFillRates.Count == 0)
            {
                return;
            }

            _deferHpBarFillUntilActionHit = false;

            foreach (KeyValuePair<Transform, float> pair in _pendingHpBarFillRates)
            {
                SetBarFillRate(pair.Key, pair.Value);
            }

            _pendingHpBarFillRates.Clear();
        }

        private void RebuildTurnOrder()
        {
            _actedUnits.Clear();
            _enemyStatusKoVisibleUnits.Clear();
            _pendingEnemyAutoReplacementEnterAnimation = false;
            _pendingEnemyKoReplacementPhase = false;
            _statusSlotUnits.Clear();

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

        private Vector2 GetDamagePopupOffset(BattleUnit unit)
        {
            if (unit != null && unit.Data != null && unit.Data.OverrideDamagePopupOffset)
            {
                return unit.Data.DamagePopupOffset;
            }

            return Vector2.zero;
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

























































