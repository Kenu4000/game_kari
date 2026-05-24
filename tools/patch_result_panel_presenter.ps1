$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Replace-Required {
    param(
        [string]$Source,
        [string]$Old,
        [string]$New,
        [string]$Label
    )

    if (!$Source.Contains($Old)) {
        throw "Patch anchor not found: $Label"
    }

    return $Source.Replace($Old, $New)
}

function Replace-MethodByName {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Replacement,
        [string]$Label
    )

    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        if ($Source.Contains($Replacement.Trim())) {
            Write-Host "Already patched: $Label"
            return $Source
        }

        throw "Patch anchor not found: $Label"
    }

    $braceStart = $Source.IndexOf("{", $start)
    if ($braceStart -lt 0) {
        throw "Method body start not found: $Label"
    }

    $depth = 0
    $end = -1

    for ($i = $braceStart; $i -lt $Source.Length; $i++) {
        $char = $Source[$i]

        if ($char -eq '{') {
            $depth++
        }
        elseif ($char -eq '}') {
            $depth--
            if ($depth -eq 0) {
                $end = $i + 1
                break
            }
        }
    }

    if ($end -lt 0) {
        throw "Method body end not found: $Label"
    }

    return $Source.Substring(0, $start) + $Replacement + $Source.Substring($end)
}

# Add presenter field while leaving legacy fields for this bridge step.
$text = Replace-Required `
    -Source $text `
    -Old @'
        private Button _resultReturnButton;
        private TMP_Text _resultReturnButtonText;
        private readonly RouteOverlayPresenter _routeOverlayPresenter = new();
'@ `
    -New @'
        private Button _resultReturnButton;
        private TMP_Text _resultReturnButtonText;
        private readonly ResultPanelPresenter _resultPanelPresenter = new();
        private readonly RouteOverlayPresenter _routeOverlayPresenter = new();
'@ `
    -Label "result presenter field"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void EnsureResultPanel()" `
    -Replacement @'
        private void EnsureResultPanel()
        {
            _resultPanelPresenter.Ensure(GetOverlayCanvas(), FindUiGameObjectByName, HandleResultFormationClicked, HandleResultReturnClicked);
        }
'@ `
    -Label "EnsureResultPanel"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ApplyResultPanelLayout()" `
    -Replacement @'
        private void ApplyResultPanelLayout()
        {
            EnsureResultPanel();
        }
'@ `
    -Label "ApplyResultPanelLayout"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ApplyResultPanelVisualStyle(Color panelColor, TextAlignmentOptions bodyAlignment, float titleFontSize, float bodyFontSize)" `
    -Replacement @'
        private void ApplyResultPanelVisualStyle(Color panelColor, TextAlignmentOptions bodyAlignment, float titleFontSize, float bodyFontSize)
        {
            EnsureResultPanel();
            _resultPanelPresenter.ApplyVisualStyle(panelColor, bodyAlignment, titleFontSize, bodyFontSize);
        }
'@ `
    -Label "ApplyResultPanelVisualStyle"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void SetResultTitleAndBody(string title, string body)" `
    -Replacement @'
        private void SetResultTitleAndBody(string title, string body)
        {
            EnsureResultPanel();
            _resultPanelPresenter.SetTitleAndBody(title, body);
        }
'@ `
    -Label "SetResultTitleAndBody"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void SetResultReturnButtonHandler(UnityEngine.Events.UnityAction handler)" `
    -Replacement @'
        private void SetResultReturnButtonHandler(UnityEngine.Events.UnityAction handler)
        {
            EnsureResultPanel();
            _resultPanelPresenter.SetRightButtonHandler(handler);
        }
'@ `
    -Label "SetResultReturnButtonHandler"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void SetResultButtons(" `
    -Replacement @'
        private void SetResultButtons(
            bool showLeftButton,
            string leftText,
            bool leftInteractable,
            string rightText)
        {
            EnsureResultPanel();
            _resultPanelPresenter.SetButtons(showLeftButton, leftText, leftInteractable, rightText);
        }
'@ `
    -Label "SetResultButtons"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void ShowResultPanel(string result)" `
    -Replacement @'
        private void ShowResultPanel(string result)
        {
            EnsureResultPanel();
            HideRouteOverlayPanels();
            _resultPanelPresenter.SetVisible(true);
            _resultPanelPresenter.SetTitleAndBody(result, "Battle End");
            _resultPanelPresenter.SetButtons(false, string.Empty, false, "Next");
        }
'@ `
    -Label "ShowResultPanel"

$text = Replace-MethodByName `
    -Source $text `
    -Signature "        private void HideResultPanel()" `
    -Replacement @'
        private void HideResultPanel()
        {
            EnsureResultPanel();
            _resultPanelPresenter.SetVisible(false);
        }
'@ `
    -Label "HideResultPanel"

# Update direct body write in formation click to use presenter path.
$text = Replace-Required `
    -Source $text `
    -Old @'
            if (_resultSubText != null)
            {
                _resultSubText.text =
                    "Formation / Preparation\n" +
                    $"Party: {BuildPartyOverviewText()}\n" +
                    $"Kakera: {_kakeraStock}/{MaxKakeraStock}\n" +
                    "Item / Skill / Link check: deferred";
            }
'@ `
    -New @'
            EnsureResultPanel();
            _resultPanelPresenter.SetBody(
                "Formation / Preparation\n" +
                $"Party: {BuildPartyOverviewText()}\n" +
                $"Kakera: {_kakeraStock}/{MaxKakeraStock}\n" +
                "Item / Skill / Link check: deferred");
'@ `
    -Label "HandleResultFormationClicked body"

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Patched BattleUIManager to delegate ResultPanel view state to ResultPanelPresenter."
