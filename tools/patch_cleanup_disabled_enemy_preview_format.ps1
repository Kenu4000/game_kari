$ErrorActionPreference = "Stop"

$path = "Assets/Scripts/Battle/BattleUIManager.cs"
if (!(Test-Path $path)) {
    throw "BattleUIManager.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

# Fix methods glued together by previous broad patch operations.
$text = [regex]::Replace($text, "}\s{2,}(private|public|internal|protected)", "}`r`n`r`n        `$1")
$text = [regex]::Replace($text, "}\s{2,}//", "}`r`n`r`n        //")

# Normalize blank lines.
$text = [regex]::Replace($text, "(?m)^[ \t]+$", "")
$text = [regex]::Replace($text, "(`r?`n){4,}", "`r`n`r`n")

Set-Content -Path $path -Value $text -Encoding UTF8
Write-Host "Cleaned formatting after disabling enemy action preview."
