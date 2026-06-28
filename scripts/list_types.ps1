$dll='bin\Debug\net10.0\RestoLoc.dll'
if (-not (Test-Path $dll)) { Write-Error "DLL not found: $dll"; exit 1 }
$asm=[System.Reflection.Assembly]::LoadFrom($dll)
try {
	$asm.GetTypes() | ForEach-Object { Write-Host $_.FullName }
} catch [System.Reflection.ReflectionTypeLoadException] {
	$_.Exception.LoaderExceptions | ForEach-Object { Write-Host 'LoaderException:' $_.Message }
}
