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

$text = InsertBeforeIfMissing $text 'private float actionSpriteLungeDistance' '        [SerializeField] private float actionResolveDelaySeconds = 0.35f;' @'
        [SerializeField] private float actionSpriteLungeDistance = 24f;
        [SerializeField] private float actionSpriteLungeSeconds = 0.12f;

'@ 'action sprite lunge fields'

$text = ReplaceOptional $text @'
            List<GridPos> targetPositions = new(_pendingActionFlashTargets);
            List<GridPos> sourcePositions = new(_pendingActionSourceFlashTargets);

            ClearPendingActionFlashTargets();
'@ @'
            List<GridPos> targetPositions = new(_pendingActionFlashTargets);
            List<GridPos> sourcePositions = new(_pendingActionSourceFlashTargets);

            yield return PlayActionSourceLunge(isSourceAllyBoard, sourcePositions);

            ClearPendingActionFlashTargets();
'@ 'PlayPendingActionFlashOrDelay source lunge'

$helpers = @'
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

'@

$text = InsertBeforeIfMissing $text 'private IEnumerator PlayActionSourceLunge' '        private void SetActionSourceFlashTargetsVisible' $helpers 'action sprite lunge helpers'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched action sprite lunge animation.'
