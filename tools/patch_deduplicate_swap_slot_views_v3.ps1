$ErrorActionPreference = 'Stop'

$path = 'Assets/Scripts/Battle/CommandPanelController.cs'
if (!(Test-Path $path)) { throw 'CommandPanelController.cs not found' }

$src = Get-Content -Path $path -Raw -Encoding UTF8

function RemoveAfterFirstBlock($src, $sig) {
    $first = $src.IndexOf($sig)
    if ($first -lt 0) { return $src }
    $pos = $first + $sig.Length
    while ($true) {
        $start = $src.IndexOf($sig, $pos)
        if ($start -lt 0) { break }
        $open = $src.IndexOf('{', $start)
        if ($open -lt 0) { throw 'open brace not found' }
        $depth = 0
        $end = -1
        for ($i = $open; $i -lt $src.Length; $i++) {
            if ($src[$i] -eq '{') { $depth++ }
            elseif ($src[$i] -eq '}') {
                $depth--
                if ($depth -eq 0) { $end = $i + 1; break }
            }
        }
        if ($end -lt 0) { throw 'end brace not found' }
        while ($end -lt $src.Length -and ($src[$end] -eq [char]13 -or $src[$end] -eq [char]10)) { $end++ }
        $src = $src.Remove($start, $end - $start)
    }
    return $src
}

function RemoveAfterFirstLine($src, $line) {
    $first = $src.IndexOf($line)
    if ($first -lt 0) { return $src }
    $pos = $first + $line.Length
    while ($true) {
        $start = $src.IndexOf($line, $pos)
        if ($start -lt 0) { break }
        $removeStart = $start
        $header = '        [Header(' + [char]34 + 'Swap Slot Views' + [char]34 + ')]'
        $h = $src.LastIndexOf($header, $start)
        if ($h -ge 0) {
            $between = $src.Substring($h + $header.Length, $start - ($h + $header.Length))
            if ([string]::IsNullOrWhiteSpace($between)) { $removeStart = $h }
        }
        $end = $start + $line.Length
        while ($end -lt $src.Length -and ($src[$end] -eq [char]13 -or $src[$end] -eq [char]10)) { $end++ }
        $src = $src.Remove($removeStart, $end - $removeStart)
    }
    return $src
}

$src = RemoveAfterFirstLine $src '        [SerializeField] private SwapSlotView[] swapSlotViews = new SwapSlotView[4];'
$src = RemoveAfterFirstBlock $src '        private bool HasSwapSlotViews()'
$src = RemoveAfterFirstBlock $src '        private void BindSwapSlotViews()'
$src = RemoveAfterFirstBlock $src '        private void BindLegacySwapButtons()'

Set-Content -Path $path -Value $src -Encoding UTF8
Write-Host 'Deduplicated swap slot views.'
