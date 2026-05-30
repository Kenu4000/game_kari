$ErrorActionPreference = 'Stop'

$slotPath = 'Assets/Scripts/Battle/TurnOrderSlotView.cs'
$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$refsPath = 'Assets/Scripts/Battle/BattleUIReferences.cs'

foreach ($path in @($slotPath, $managerPath, $refsPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
}

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

# Replace TurnOrderSlotView with icon-only fixed bar version.
$slotSource = @'
using UnityEngine;
using UnityEngine.UI;

namespace GameKari.Battle
{
    [DisallowMultipleComponent]
    public sealed class TurnOrderSlotView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image currentFrame;

        [Header("Visual State")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color actedColor = new Color(0.35f, 0.35f, 0.35f, 0.65f);
        [SerializeField] private Color enemyColor = Color.white;
        [SerializeField] private Color currentFrameColor = new Color(1f, 0.9f, 0.25f, 1f);

        private void Awake()
        {
            AutoBindMissingReferences();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetUnit(BattleUnit unit, bool isAlly, bool isCurrent, bool isActed)
        {
            AutoBindMissingReferences();

            bool hasUnit = unit != null && !unit.IsDead && unit.Data != null;
            SetVisible(hasUnit);
            if (!hasUnit)
            {
                return;
            }

            Sprite icon = unit.Data.FaceIcon != null ? unit.Data.FaceIcon : unit.Data.BattleSprite;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
                iconImage.color = isActed ? actedColor : isAlly ? normalColor : enemyColor;
            }

            if (currentFrame != null)
            {
                currentFrame.gameObject.SetActive(isCurrent);
                currentFrame.color = currentFrameColor;
                currentFrame.raycastTarget = false;
            }
        }

        private void AutoBindMissingReferences()
        {
            if (iconImage == null)
            {
                iconImage = FindChildImage("IconImage");
            }

            if (currentFrame == null)
            {
                currentFrame = FindChildImage("CurrentFrame");
            }
        }

        private Image FindChildImage(string childName)
        {
            Transform found = transform.Find(childName);
            return found == null ? null : found.GetComponent<Image>();
        }
    }
}
'@
Set-Content -Path $slotPath -Value $slotSource -Encoding UTF8

# BattleUIReferences: add fixed slot positions.
$refs = Get-Content -Path $refsPath -Raw -Encoding UTF8
$refs = InsertBeforeIfMissing $refs 'public Transform[] turnOrderSlotPositions' '        public TurnOrderSlotView turnOrderSlotTemplate;' @'
        public Transform[] turnOrderSlotPositions = new Transform[8];
'@ 'BattleUIReferences fixed turn order positions'
Set-Content -Path $refsPath -Value $refs -Encoding UTF8

# BattleUIManager: add fixed slot positions.
$text = Get-Content -Path $managerPath -Raw -Encoding UTF8
$text = InsertBeforeIfMissing $text 'private Transform[] turnOrderSlotPositions' '        [SerializeField] private TurnOrderSlotView turnOrderSlotTemplate;' @'
        [SerializeField] private Transform[] turnOrderSlotPositions = new Transform[8];
'@ 'BattleUIManager fixed turn order positions field'

$text = ReplaceOptional $text @'
            turnOrderSlotContainer = refs.turnOrderSlotContainer != null ? refs.turnOrderSlotContainer : turnOrderSlotContainer;
            turnOrderSlotTemplate = refs.turnOrderSlotTemplate != null ? refs.turnOrderSlotTemplate : turnOrderSlotTemplate;
'@ @'
            turnOrderSlotContainer = refs.turnOrderSlotContainer != null ? refs.turnOrderSlotContainer : turnOrderSlotContainer;
            turnOrderSlotTemplate = refs.turnOrderSlotTemplate != null ? refs.turnOrderSlotTemplate : turnOrderSlotTemplate;
            if (refs.turnOrderSlotPositions != null && refs.turnOrderSlotPositions.Length > 0)
            {
                turnOrderSlotPositions = refs.turnOrderSlotPositions;
            }
'@ 'ApplyBattleUIReferences fixed slot positions'

$oldBlockPattern = '(?s)        private bool CanGenerateTurnOrderSlots\(\).*?        private string BuildTurnOrderBarText\(\)'
$newBlock = @'
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
'@

if ([regex]::IsMatch($text, $oldBlockPattern)) {
    $text = [regex]::Replace($text, $oldBlockPattern, $newBlock, 1)
}
else {
    Write-Host 'Generated turn order helper block not found. This may mean the previous turn order slot patch was not applied.'
}

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched turn order bar to use fixed slot positions.'
