$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) { Write-Host "Already exists: $label"; return $src }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) { Write-Host "Already replaced or not found: $label"; return $src }
    return $src.Replace($old, $new)
}

$text = InsertBeforeIfMissing $text 'private float floatingHpBarVisibleSeconds' '        [SerializeField] private float hpBarAnimationSeconds = 0.35f;' @'
        [SerializeField] private float floatingHpBarVisibleSeconds = 0.9f;
        [SerializeField] private float floatingHpBarFadeSeconds = 0.18f;
        [SerializeField] private Vector2 floatingHpBarOffset = new Vector2(0f, 52f);
        [SerializeField] private Vector2 floatingHpBarSize = new Vector2(64f, 10f);

'@ 'floating HP bar settings'

$text = ReplaceOptional $text @'
            ApplyDeferredHpBarFillUpdates();

            ClearPendingActionFlashTargets();
'@ @'
            ApplyDeferredHpBarFillUpdates();
            ShowPendingFloatingHpBars();

            ClearPendingActionFlashTargets();
'@ 'show floating HP bars at impact timing'

$text = ReplaceOptional $text @'
                ApplyDeferredHpBarFillUpdates();
                yield return new WaitForSeconds(actionResolveDelaySeconds);
'@ @'
                ApplyDeferredHpBarFillUpdates();
                ShowPendingFloatingHpBars();
                yield return new WaitForSeconds(actionResolveDelaySeconds);
'@ 'show floating HP bars when no flash targets'

$helpers = @'
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

                FloatingHPBarView floatingBar = GetOrCreateFloatingHpBar(popup.IsAllyBoard, popup.Position);
                if (floatingBar == null)
                {
                    continue;
                }

                floatingBar.Show(
                    unit.CurrentHP,
                    unit.Data.MaxHP,
                    hpBarAnimationSeconds,
                    floatingHpBarVisibleSeconds,
                    floatingHpBarFadeSeconds);
            }
        }

        private FloatingHPBarView GetOrCreateFloatingHpBar(bool isAllyBoard, GridPos position)
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
            rootRect.anchoredPosition = floatingHpBarOffset;

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

'@

$text = InsertBeforeIfMissing $text 'private void ShowPendingFloatingHpBars()' '        private void SetActionSourceFlashTargetsVisible' $helpers 'floating HP bar helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched floating HP bars on hit.'
