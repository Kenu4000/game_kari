using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    /// <summary>
    /// Scene/Prefab editable references for the battle screen.
    /// Attach this to BattleScreenRoot and assign references in the Inspector.
    /// BattleUIManager can copy these references at runtime while preserving its legacy fields.
    /// </summary>
    public sealed class BattleUIReferences : MonoBehaviour
    {
        [Header("Controllers")]
        public CommandPanelController commandPanel;
        public Button rotateButton;

        [Header("Battlefield Labels / Enemy")]
        public TMP_Text enemyFrontTop;
        public TMP_Text enemyBackTop;
        public TMP_Text enemyFrontBottom;
        public TMP_Text enemyBackBottom;

        [Header("Battlefield Labels / Ally")]
        public TMP_Text allyFrontTop;
        public TMP_Text allyBackTop;
        public TMP_Text allyFrontBottom;
        public TMP_Text allyBackBottom;

        [Header("Top Action Overlay")]
        public TMP_Text actionSkillName;
        public TMP_Text actionUserName;

        [Header("Target Preview")]
        public Image enemyFTHighlight;
        public Image enemyFBHighlight;

        [Header("Status Panels")]
        public Transform enemyStatusPanel;
        public Transform allyStatusPanel;

        [Header("Turn Order Bar")]
        public Transform turnOrderSlotContainer;
        public Transform[] turnOrderSlotPositions = new Transform[8];        public TurnOrderSlotView turnOrderSlotTemplate;
        public TMP_Text turnOrderBarText;
        [Header("Generated Roots")]
        public GameObject bossNamePlate;
        public GameObject topActionPanel;
        public GameObject enemyGridPanel;
        public GameObject allyGridPanel;
        public GameObject commandPanelRoot;
        public GameObject enemyStatusPanelRoot;
        public GameObject allyStatusPanelRoot;
    }
}



