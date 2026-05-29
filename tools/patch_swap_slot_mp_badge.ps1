$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/SwapSlotView.cs'
if (!(Test-Path $path)) { throw 'SwapSlotView.cs not found' }

$text = Get-Content -Path $path -Raw -Encoding UTF8

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

$text = ReplaceOptional $text @'
        [SerializeField] private TMP_Text mpText;
        [SerializeField] private GameObject emptyRoot;
'@ @'
        [SerializeField] private TMP_Text mpText;

        [Header("MP Badge")]
        [SerializeField] private Image mpBadgeImage;
        [SerializeField] private TMP_Text mpBadgeText;
        [SerializeField] private Sprite mpBadgeNormalSprite;
        [SerializeField] private Sprite mpBadgeZeroSprite;
        [SerializeField] private Sprite mpBadgeEmptySprite;

        [SerializeField] private GameObject emptyRoot;
'@ 'MP badge fields'

$text = ReplaceOptional $text @'
            if (mpText == null)
            {
                mpText = FindChildText("MPText");
            }
'@ @'
            if (mpText == null)
            {
                mpText = FindChildText("MPText");
            }

            if (mpBadgeImage == null)
            {
                mpBadgeImage = FindChildImage("MpBadge");
            }

            if (mpBadgeText == null)
            {
                mpBadgeText = FindChildText("MpBadgeText");
            }
'@ 'MP badge auto bind'

$text = ReplaceOptional $text @'
            SetText(nameText, "-");
            SetText(hpText, string.Empty);
            SetText(mpText, string.Empty);
        }
'@ @'
            SetText(nameText, "-");
            SetText(hpText, string.Empty);
            SetText(mpText, string.Empty);
            SetText(mpBadgeText, "-");
            ApplyMpBadgeSprite(null);
        }
'@ 'empty MP badge display'

$text = ReplaceOptional $text @'
            SetText(nameText, unit.Name);
            SetText(hpText, $"HP {unit.CurrentHP}/{unit.Data.MaxHP}");
            SetText(mpText, $"MP {unit.CurrentMP}/{unit.Data.MaxMP}");
        }
'@ @'
            SetText(nameText, unit.Name);
            SetText(hpText, $"HP {unit.CurrentHP}/{unit.Data.MaxHP}");
            SetText(mpText, $"MP {unit.CurrentMP}/{unit.Data.MaxMP}");
            SetText(mpBadgeText, Mathf.Max(0, unit.CurrentMP).ToString());
            ApplyMpBadgeSprite(unit);
        }
'@ 'filled MP badge display'

$text = InsertBeforeIfMissing $text 'private void ApplyMpBadgeSprite(BattleUnit unit)' '        private TMP_Text FindChildText(string childName)' @'
        private void ApplyMpBadgeSprite(BattleUnit unit)
        {
            if (mpBadgeImage == null)
            {
                return;
            }

            Sprite sprite = null;
            if (unit == null || unit.IsDead || unit.Data == null)
            {
                sprite = mpBadgeEmptySprite;
            }
            else if (unit.CurrentMP <= 0)
            {
                sprite = mpBadgeZeroSprite != null ? mpBadgeZeroSprite : mpBadgeNormalSprite;
            }
            else
            {
                sprite = mpBadgeNormalSprite;
            }

            if (sprite != null)
            {
                mpBadgeImage.sprite = sprite;
            }
        }

'@ 'ApplyMpBadgeSprite'

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host 'Patched SwapSlotView MP badge settings.'
