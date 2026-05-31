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

function ReplaceRequired($src, $old, $new, $label) {
    if (!$src.Contains($old)) { throw "Patch anchor not found: $label" }
    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

$settings = @'
        [Header("Mouse Wheel Rotate")]
        [SerializeField] private bool enableMouseWheelRotate = true;
        [SerializeField] private bool invertMouseWheelRotate = false;
        [SerializeField] private float mouseWheelRotateThreshold = 0.01f;

'@
$text = InsertBeforeIfMissing $text 'enableMouseWheelRotate' '        [Header("Status Panels")]' $settings 'mouse wheel rotate settings'

$oldUpdate = @'
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
'@
$newUpdate = @'
        private void Update()
        {
            HandleDebugBuffHotkeys();
            HandleMouseWheelRotateInput();

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
'@
$text = ReplaceRequired $text $oldUpdate $newUpdate 'Update calls mouse wheel rotate input'

$helper = @'
        private void HandleMouseWheelRotateInput()
        {
            if (!enableMouseWheelRotate || !CanAcceptRotateCommand())
            {
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < Mathf.Max(0.0001f, mouseWheelRotateThreshold))
            {
                return;
            }

            // Current rotation supports the same direction as the existing Rotate button.
            // Wheel up rotates once. Wheel down rotates the same rotate operation three times,
            // which is equivalent to reverse rotation in a four-cell formation.
            bool reverse = invertMouseWheelRotate ? scroll > 0f : scroll < 0f;
            int rotateCount = reverse ? 3 : 1;
            for (int i = 0; i < rotateCount; i++)
            {
                HandleRotateClicked();
            }
        }

'@
$text = InsertBeforeIfMissing $text 'private void HandleMouseWheelRotateInput()' '        private void HandleDebugBuffHotkeys()' $helper 'HandleMouseWheelRotateInput helper'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched mouse wheel rotate input.'
