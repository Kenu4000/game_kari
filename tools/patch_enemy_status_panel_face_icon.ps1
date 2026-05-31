$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }

    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    Write-Host "Inserted: $label"
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$oldEnemyBlock = @'
            SetLabel(slot, "Name", unit.Name);
            SetLabel(slot, "TurnNumber", BuildBoardMpBadgeText(unit));

            int currentHp = unit.CurrentHP;
'@
$newEnemyBlock = @'
            SetLabel(slot, "Name", unit.Name);
            SetLabel(slot, "TurnNumber", BuildBoardMpBadgeText(unit));
            SetStatusFaceIcon(slot, unit);

            int currentHp = unit.CurrentHP;
'@
$text = ReplaceOptional $text $oldEnemyBlock $newEnemyBlock 'bind enemy status FaceIcon'

$helper = @'
        private static void SetStatusFaceIcon(Transform slot, BattleUnit unit)
        {
            if (slot == null)
            {
                return;
            }

            Transform iconTransform = slot.Find("FaceIcon");
            if (iconTransform == null)
            {
                return;
            }

            Image image = iconTransform.GetComponent<Image>();
            if (image == null)
            {
                image = iconTransform.gameObject.AddComponent<Image>();
            }

            Sprite sprite = unit == null || unit.Data == null ? null : unit.Data.FaceIcon;
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;

            Color color = image.color;
            color.a = unit != null && unit.IsDead ? 0.45f : 1f;
            image.color = color;
        }

'@
$text = InsertBeforeIfMissing $text 'private static void SetStatusFaceIcon(Transform slot, BattleUnit unit)' '        private void RedrawEnemyStatusSlot(int slotNumber, BattleUnit unit)' $helper 'SetStatusFaceIcon helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched enemy status panel FaceIcon binding.'
