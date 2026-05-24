$ErrorActionPreference = "Stop"

$path = "Assets/Editor/BattleUIReferenceBinder.cs"
if (!(Test-Path $path)) {
    throw "BattleUIReferenceBinder.cs not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
    [MenuItem("Tools/Battle UI/Bind References On Selected Root", true)]
    public static bool ValidateBindReferencesOnSelectedRoot()
    {
        return Selection.activeGameObject != null;
    }

'@

if (!$text.Contains($old)) {
    Write-Host "Validation block already removed or changed."
}
else {
    $text = $text.Replace($old, "")
    Set-Content -Path $path -Value $text -Encoding UTF8
    Write-Host "Removed menu validation so the binder command is always clickable."
}
