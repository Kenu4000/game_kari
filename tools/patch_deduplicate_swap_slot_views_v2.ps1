$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/CommandPanelController.cs"
if (!(Test-Path $path)) {
    throw "CommandPanelController.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Remove-SecondAndLaterBlock {
    param(
        [string]$Source,
        [string]$Signature,
        [string]$Label
    )

    $first = $Source.IndexOf($Signature)
    if ($first -lt 0) {
        Write-Host "Not found: $Label"
        return $Source
    }

    $searchFrom = $first + $Signature.Length

    while ($true) {
        $start = $Source.IndexOf($Signature, $searchFrom)
        if ($start -lt 0) {
            break
        }

        $braceStart = $Source.IndexOf("{", $start)
        if ($braceStart -lt 0) {
            throw "Block body start not found: $Label"
        }

        $depth = 0
        $end = -1
        for ($i = $braceStart; $i -lt $Source.Length; $i++) {
            $ch = $Source[$i]
            if ($ch -eq '{') {
                $depth++
            }
            elseif ($ch -eq '}') {
                $depth--
                if ($depth -eq 0) {
                    $end = $i + 1
                    break
                }
            }
        }

        if ($end -lt 0) {
            throw "Block body end not found: $Label"
        }

        while ($end -lt $Source.Length -and ($Source[$end] -eq "`r" -or $Source[$end] -eq "`n")) {
            $end++
        }

        $Source = $Source.Remove($start, $end - $start)
        Write-Host "Removed duplicate block: $Label"
        $searchFrom = $first + $Signature.Length
    }

    return $Source
}

function Remove-DuplicateSwapSlotField {
    param([string]$Source)

    $fieldLine = "        [SerializeField] private SwapSlotView[] swapSlotViews = new SwapSlotView[4];"
    $first = $Source.IndexOf($fieldLine)
    if ($first -lt 0) {
        Write-Host "swapSlotViews field not found."
        return $Source
    }

    $searchFrom = $first + $fieldLine.Length
    while ($true) {
        $start = $Source.IndexOf($fieldLine, $searchFrom)
        if ($start -lt 0) {
            break
        }

        $removeStart = $start
        $header = "        [Header(\"Swap Slot Views\")]"
        $headerStart = $Source.LastIndexOf($header, $start)
        if ($headerStart -ge 0) {
            $between = $Source.Substring($headerStart + $header.Length, $start - ($headerStart + $header.Length))
            if ([string]::IsNullOrWhiteSpace($between)) {
                $removeStart = $headerStart
            }
        }

        $removeEnd = $start + $fieldLine.Length
        while ($removeEnd -lt $Source.Length -and ($Source[$removeEnd] -eq "`r" -or $Source[$removeEnd] -eq "`n")) {
            $removeEnd++
        }

        $Source = $Source.Remove($removeStart, $removeEnd - $removeStart)
        Write-Host "Removed duplicate field: swapSlotViews"
        $searchFrom = $first + $fieldLine.Length
    }

    return $Source
}

$text = Remove-DuplicateSwapSlotField -Source $text
$text = Remove-SecondAndLaterBlock -Source $text -Signature "        private bool HasSwapSlotViews()" -Label "HasSwapSlotViews"
$text = Remove-SecondAndLaterBlock -Source $text -Signature "        private void BindSwapSlotViews()" -Label "BindSwapSlotViews"
$text = Remove-SecondAndLaterBlock -Source $text -Signature "        private void BindLegacySwapButtons()" -Label "BindLegacySwapButtons"

$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")
$text = [regex]::Replace($text, "(`r?`n){4,}", "`r`n`r`n")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Deduplicated swap slot view field and helper methods."
