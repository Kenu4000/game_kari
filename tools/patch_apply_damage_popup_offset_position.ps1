$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/BattleUIManager.cs'
if (!(Test-Path $path)) { throw "Required file not found: $path" }

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
                RectTransform rect = popupObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.55f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                TMP_Text label = popupObject.AddComponent<TextMeshProUGUI>();
'@

$new = @'
                RectTransform rect = popupObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(120f, 48f);

                BattleUnit popupUnit = _grid.GetUnit(popup.IsAllyBoard, popup.Position);
                rect.anchoredPosition = GetDamagePopupOffset(popupUnit);

                TMP_Text label = popupObject.AddComponent<TextMeshProUGUI>();
'@

if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
    Set-Content -Path $path -Value $text -Encoding UTF8
    Write-Host 'Applied DamagePopupOffset to ActionValuePopup RectTransform.'
    exit 0
}

if ($text.Contains('rect.anchoredPosition = GetDamagePopupOffset(popupUnit);')) {
    Write-Host 'DamagePopupOffset is already applied to ActionValuePopup.'
    exit 0
}

throw 'Patch anchor not found: ActionValuePopup RectTransform block in ShowPendingActionValuePopups.'
