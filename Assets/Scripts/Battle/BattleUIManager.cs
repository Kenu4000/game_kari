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

        private BattleGrid _grid;
        private FormationController _formation;
        private TurnOrderManager _turnOrder;

        private readonly List<BattleUnit> _reserves = new();
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
            _reserves.Add(reserve);

            _grid.SetUnit(false, GridPos.FrontTop, CreateUnit("Goblin A", 70, 0, 10));
            _grid.SetUnit(false, GridPos.BackTop, CreateUnit("Archer", 60, 0, 13));
            _grid.SetUnit(false, GridPos.FrontBottom, CreateUnit("Goblin B", 70, 0, 8));
            _grid.SetUnit(false, GridPos.BackBottom, CreateUnit("Shaman", 55, 20, 7));

            _active = heroA;
            RebuildTurnOrder();
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
            _formation.SwapActiveWithReserve(_active, reserve);
            _reserves.Remove(reserve);
            _reserves.Add(_active);
            _active = reserve;
            commandPanel.Setup(_active, _reserves);
            RedrawBoard();
            Debug.Log("[Action] Swapped active unit with reserve (no action consumption). ");
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
            RedrawBoard();
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

            RebuildTurnOrder();
        }

        private void RebuildTurnOrder()
        {
            var all = new List<BattleUnit>();
            all.AddRange(_grid.AllyGrid.Values);
            all.AddRange(_grid.EnemyGrid.Values);
            _turnOrder.RebuildTurnOrder(all);
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
