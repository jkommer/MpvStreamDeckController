# Remove embedded C2PA provenance metadata from SVG files as claude code includes this which adds 7.7kb per file which is most of the actual file size!

Get-ChildItem "Images" -Recurse -Filter *.svg | ForEach-Object {
    $s = [IO.File]::ReadAllText($_.FullName)
    $t = [regex]::Replace($s, '\s*<metadata>\s*<c2pa:manifest>.*?</c2pa:manifest>\s*</metadata>', '', 'Singleline')
    $t = [regex]::Replace($t, '\s+xmlns:c2pa="[^"]*"', '')
    if ($t -ne $s) { [IO.File]::WriteAllText($_.FullName, $t, (New-Object Text.UTF8Encoding $false)); "$($_.Name)" }
}
