$ErrorActionPreference = 'Stop'

$managerPath = 'Assets/Scripts/Battle/BattleUIManager.cs'
$viewPath = 'Assets/Scripts/Battle/FloatingHPBarView.cs'
$animatorPath = 'Assets/Scripts/Battle/HPBarFillAnimator.cs'

foreach ($path in @($managerPath, $viewPath, $animatorPath)) {
    if (!(Test-Path $path)) { throw "Required file not found: $path" }
}

function ReplaceRequired($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        throw "Patch anchor not found: $label"
    }

    Write-Host "Replaced: $label"
    return $src.Replace($old, $new)
}

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

# HPBarFillAnimator: add immediate fill setter so floating bars can start at previous HP.
$animatorText = Get-Content -Path $animatorPath -Raw -Encoding UTF8
$animatorInsert = @'
        public void SetFillImmediate(float targetRate)
        {
            targetRate = Mathf.Clamp01(targetRate);

            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }

            _initialized = true;
            ApplyFill(targetRate);
        }

'@
$animatorText = InsertBeforeIfMissing $animatorText 'public void SetFillImmediate(float targetRate)' '        public void SetAnimationSeconds(float seconds)' $animatorInsert 'HPBarFillAnimator.SetFillImmediate'
Set-Content -Path $animatorPath -Value $animatorText -Encoding UTF8

# FloatingHPBarView: add transition method that starts from previous HP and animates to current HP.
$viewText = Get-Content -Path $viewPath -Raw -Encoding UTF8
$oldShow = @'
        public void Show(int current, int max, float hpAnimationSeconds, float visibleSeconds, float fadeOutSeconds)
        {
            AutoBindMissingReferences();

            showSeconds = Mathf.Max(0f, visibleSeconds);
            fadeSeconds = Mathf.Max(0f, fadeOutSeconds);

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            SetFill(current, max, hpAnimationSeconds);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }
'@
$newShow = @'
        public void Show(int current, int max, float hpAnimationSeconds, float visibleSeconds, float fadeOutSeconds)
        {
            ShowTransition(current, current, max, hpAnimationSeconds, visibleSeconds, fadeOutSeconds);
        }

        public void ShowTransition(int previous, int current, int max, float hpAnimationSeconds, float visibleSeconds, float fadeOutSeconds)
        {
            AutoBindMissingReferences();

            showSeconds = Mathf.Max(0f, visibleSeconds);
            fadeSeconds = Mathf.Max(0f, fadeOutSeconds);

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            SetFillImmediate(previous, max);
            SetFill(current, max, hpAnimationSeconds);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }
'@
$viewText = ReplaceOptional $viewText $oldShow $newShow 'FloatingHPBarView.Show transition support'
$viewInsert = @'
        private void SetFillImmediate(int current, int max)
        {
            if (fill == null)
            {
                return;
            }

            float rate = max <= 0 ? 0f : Mathf.Clamp01((float)Mathf.Max(0, current) / max);
            HPBarFillAnimator animator = fill.GetComponent<HPBarFillAnimator>();
            if (animator == null)
            {
                animator = fill.gameObject.AddComponent<HPBarFillAnimator>();
            }

            animator.SetFillImmediate(rate);
        }

'@
$viewText = InsertBeforeIfMissing $viewText 'private void SetFillImmediate(int current, int max)' '        private void SetFill(int current, int max, float hpAnimationSeconds)' $viewInsert 'FloatingHPBarView.SetFillImmediate'
Set-Content -Path $viewPath -Value $viewText -Encoding UTF8

# BattleUIManager: extend popup snapshot data.
$text = Get-Content -Path $managerPath -Raw -Encoding UTF8
$text = InsertBeforeIfMissing $text 'public bool HasHpSnapshot;' '            public string Text;' @'
            public bool HasHpSnapshot;
            public int PreviousHP;
            public int CurrentHP;
            public int MaxHP;
'@ 'ActionValuePopup HP snapshot fields'

$oldAddPopup = @'
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
'@
$newAddPopup = @'
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
'@
$text = ReplaceOptional $text $oldAddPopup $newAddPopup 'AddPendingActionValuePopup HP snapshot parameters'

# Add snapshots at HP-changing call sites.
$text = ReplaceOptional $text '            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}");' '            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}", beforeHp, target.CurrentHP, target.Data.MaxHP);' 'HealAllyUnit popup snapshot'
$text = ReplaceOptional $text '            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}");' '            AddPendingActionValuePopup(true, target.GridPos, $"+{healed}", beforeHp, target.CurrentHP, target.Data.MaxHP);' 'HandleHealItem popup snapshot'

$oldDamageEnemy = @'
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);
            AddPendingActionValuePopup(false, pos, $"-{finalDamage}");
'@
$newDamageEnemy = @'
            int beforeHp = target.CurrentHP;
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);
            AddPendingActionValuePopup(false, pos, $"-{finalDamage}", beforeHp, target.CurrentHP, target.Data.MaxHP);
'@
$text = ReplaceOptional $text $oldDamageEnemy $newDamageEnemy 'DamageEnemyAt popup snapshot'

$oldDamageAlly = @'
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);
            AddPendingActionValuePopup(true, targetPosition, $"-{finalDamage}");
'@
$newDamageAlly = @'
            int beforeHp = target.CurrentHP;
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);
            AddPendingActionValuePopup(true, targetPosition, $"-{finalDamage}", beforeHp, target.CurrentHP, target.Data.MaxHP);
'@
$text = ReplaceOptional $text $oldDamageAlly $newDamageAlly 'DamageAllyAt popup snapshot'

# Make floating HP bars use the snapshot transition when available.
$oldFloatingShow = @'
                floatingBar.Show(
                    unit.CurrentHP,
                    unit.Data.MaxHP,
                    hpBarAnimationSeconds,
                    floatingHpBarVisibleSeconds,
                    floatingHpBarFadeSeconds);
'@
$newFloatingShow = @'
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
'@
$text = ReplaceOptional $text $oldFloatingShow $newFloatingShow 'ShowPendingFloatingHpBars transition sync'

Set-Content -Path $managerPath -Value $text -Encoding UTF8
Write-Host 'Patched floating HP bars to animate from previous HP in sync with status HP bars.'
