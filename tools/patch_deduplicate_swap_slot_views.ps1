$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/CommandPanelController.cs"
if (!(Test-Path $path)) {
    throw "CommandPanelController.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

function Remove-Second-And-LaterField {
    param(
        [string]$Source,
        [string]$LinePattern,
        [string]$Label
    )

    $matches = [regex]::Matches($Source, $LinePattern)
    if ($matches.Count -le 1) {
        Write-Host "No duplicate field found: $Label"
        return $Source
    }

    for ($i = $matches.Count - 1; $i -ge 1; $i--) {
        $match = $matches[$i]
        $start = $match.Index
        $length = $match.Length

        # Remove the optional Header immediately above if it belongs to the duplicated field.
        $prefixStart = [Math]::Max(0, $start - 120)
        $prefix = $Source.Substring($prefixStart, $start - $prefixStart)
        $headerMatch = [regex]::Match($prefix, "(?s)(\r?\n\s*\[Header\(\"Swap Slot Views\"\)\]\s*)$")
        if ($headerMatch.Success) {
            $start = $prefixStart + $headerMatch.Index
            $length = ($match.Index + $match.Length) - $start
        }

        while (($start + $length) -lt $Source.Length -and ($Source[$start + $length] -eq "`r" -or $Source[$start + $length] -eq "`n")) {
            $length++
        }

        $Source = $Source.Remove($start, $length)
        Write-Host "Removed duplicate field: $Label"
    }

    return $Source
}

function Remove-Second-And-LaterMethod {
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

        $Source = $Source.Remove($start, $end - $start)
        Write-Host "Removed duplicate method: $Label"
        $searchFrom = $first + $Signature.Length
    }

    return $Source
}

$text = Remove-Second-And-LaterField `
    -Source $text `
    -LinePattern '(?m)^\s*\[SerializeField\]\s+private\s+SwapSlotView\[\]\s+swapSlotViews\s*=\s*new\s+SwapSlotView\[4\];\s*$' `
    -Label "swapSlotViews"

$text = Remove-Second-And-LaterMethod -Source $text -Signature "        private bool HasSwapSlotViews()" -Label "HasSwapSlotViews"
$text = Remove-Second-And-LaterMethod -Source $text -Signature "        private void BindSwapSlotViews()" -Label "BindSwapSlotViews"
$text = Remove-Second-And-LaterMethod -Source $text -Signature "        private void BindLegacySwapButtons()" -Label "BindLegacySwapButtons"

$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")
$text = [regex]::Replace($text, "(`r?`n){4,}", "`r`n`r`n")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Deduplicated swap slot view fields and helper methods."
