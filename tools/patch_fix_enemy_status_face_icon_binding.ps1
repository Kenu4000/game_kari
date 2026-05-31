$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

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

# Ensure enemy status slots bind CharacterData.FaceIcon.
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
$text = ReplaceOptional $text $oldEnemyBlock $newEnemyBlock 'bind enemy status FaceIcon from CharacterData asset'

# Ensure ally status also keeps the same binding if this patch is applied on a partially patched file.
$oldAllyBlock = @'
            SetLabel(slot, "Name", displayName);
            SetLabel(slot, "TurnNumber", BuildBoardMpBadgeText(unit));

            int currentHp = unit.IsDead ? 0 : unit.CurrentHP;
'@
$newAllyBlock = @'
            SetLabel(slot, "Name", displayName);
            SetLabel(slot, "TurnNumber", BuildBoardMpBadgeText(unit));
            SetStatusFaceIcon(slot, unit);

            int currentHp = unit.IsDead ? 0 : unit.CurrentHP;
'@
$text = ReplaceOptional $text $oldAllyBlock $newAllyBlock 'bind ally status FaceIcon from CharacterData asset'

# If an older helper used slot.Find("FaceIcon"), make it recursive so EnemyStatus can use nested UI structures too.
$text = ReplaceOptional $text '            Transform iconTransform = slot.Find("FaceIcon");' '            Transform iconTransform = FindChildRecursive(slot, "FaceIcon");' 'use recursive FaceIcon lookup'

$statusFaceHelper = @'
        private static void SetStatusFaceIcon(Transform slot, BattleUnit unit)
        {
            if (slot == null)
            {
                return;
            }

            Transform iconTransform = FindChildRecursive(slot, "FaceIcon");
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

'@
$text = InsertBeforeIfMissing $text 'private static void SetStatusFaceIcon(Transform slot, BattleUnit unit)' '        private void RedrawEnemyStatusSlot(int slotNumber, BattleUnit unit)' $statusFaceHelper 'SetStatusFaceIcon helper'

$getSpriteHelper = @'
        private static Sprite GetStatusFaceIconSprite(BattleUnit unit)
        {
            CharacterData data = unit == null ? null : unit.Data;
            return data == null ? null : data.FaceIcon;
        }

'@
$text = InsertBeforeIfMissing $text 'private static Sprite GetStatusFaceIconSprite(BattleUnit unit)' '        private static void SetStatusFaceIcon(Transform slot, BattleUnit unit)' $getSpriteHelper 'GetStatusFaceIconSprite helper'

$recursiveFindHelper = @'
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

'@
$text = InsertBeforeIfMissing $text 'private static Transform FindChildRecursive(Transform root, string childName)' '        private static Sprite GetStatusFaceIconSprite(BattleUnit unit)' $recursiveFindHelper 'FindChildRecursive helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched status FaceIcon binding for enemy and ally slots using CharacterData.FaceIcon.'
