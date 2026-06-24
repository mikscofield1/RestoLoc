$lines = Get-Content "Calcul.cs"
for ($i=0; $i -lt $lines.Count; $i++) {
    Write-Output ("{0,4}: {1}" -f ($i+1), $lines[$i])
}