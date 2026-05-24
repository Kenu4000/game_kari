$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Remove-MethodByName {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Label
    )

    $start = $Source.IndexOf($Signature)
    if ($start -lt 0) {
        Write-Host "Already removed or not found: $Label"
        return $Source
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

    while ($end -lt $Source.Length -and ($Source[$end] -eq "`r" -or $Source[$end] -eq "`n")) {
        $end++
    }

    return $Source.Remove($start, $end - $start)
}

function Remove-RequiredText {
    param(
        [string]$Source,
        [string]$Old,
        [string]$Label
    )

    if (!$Source.Contains($Old)) {
        Write-Host "Already removed or not found: $Label"
        return $Source
    }

    return $Source.Replace($Old, "")
}

$text = Remove-RequiredText `
    -Source $text `
    -Old @'
        private GameObject _resultPanelObject;
        private TMP_Text _resultTitleText;
        private TMP_Text _resultSubText;
        private Button _resultFormationButton;
        private TMP_Text _resultFormationButtonText;
        private Button _resultReturnButton;
        private TMP_Text _resultReturnButtonText;
'@ `
    -Label "legacy result fields"

$methods = @(
    @{ Signature = "        private void ApplyResultPanelLayout()"; Label = "ApplyResultPanelLayout" },
    @{ Signature = "        private static void ApplyResultButtonLayout(Button button, float minX, float maxX)"; Label = "ApplyResultButtonLayout" },
    @{ Signature = "        private void TryBindExistingResultFormationButton()"; Label = "TryBindExistingResultFormationButton" },
    @{ Signature = "        private void CreateResultFormationButton()"; Label = "CreateResultFormationButton" },
    @{ Signature = "        private void TryBindExistingResultReturnButton()"; Label = "TryBindExistingResultReturnButton" },
    @{ Signature = "        private void CreateResultReturnButton()"; Label = "CreateResultReturnButton" }
)

foreach ($method in $methods) {
    $text = Remove-MethodByName -Source $text -Signature $method.Signature -Label $method.Label
}

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Removed legacy ResultPanel fields and creation helpers from BattleUIManager.cs."
