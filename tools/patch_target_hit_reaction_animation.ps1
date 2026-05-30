$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw 'BattleUIManager.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

function InsertBeforeIfMissing($src, $needle, $anchor, $insert, $label) {
    if ($src.Contains($needle)) {
        Write-Host "Already exists: $label"
        return $src
    }
    $index = $src.IndexOf($anchor)
    if ($index -lt 0) { throw "Patch anchor not found: $label" }
    return $src.Substring(0, $index) + $insert + $src.Substring($index)
}

function ReplaceOptional($src, $old, $new, $label) {
    if (!$src.Contains($old)) {
        Write-Host "Already replaced or not found: $label"
        return $src
    }
    return $src.Replace($old, $new)
}

$text = InsertBeforeIfMissing $text 'private float targetHitShakeDistance' '        [SerializeField] private float actionSpriteLungeSeconds = 0.12f;' @'
        [SerializeField] private float targetHitShakeDistance = 10f;
        [SerializeField] private float targetHitShakeSeconds = 0.16f;
        [SerializeField] private int targetHitShakeCount = 3;

'@ 'target hit reaction fields'

$text = ReplaceOptional $text @'
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
'@ @'
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

            HideActiveActionValuePopups();
'@ 'Play hit reaction after flash'

$helpers = @'
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

'@

$text = InsertBeforeIfMissing $text 'private IEnumerator PlayPendingDamageHitReactions' '        private void SetActionSourceFlashTargetsVisible' $helpers 'target hit reaction helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched target hit reaction animation.'
