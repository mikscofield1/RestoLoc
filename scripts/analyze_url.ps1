$dll='bin\Debug\net10.0\RestoLoc.dll'
if (-not (Test-Path $dll)) { Write-Error "DLL not found: $dll"; exit 1 }
$asm=[System.Reflection.Assembly]::LoadFrom($dll)
$type=$asm.GetType('RestoLoc.Calculs')
$m1=$type.GetMethod('ResolveGoogleMapsShortUrlAsync')
$t1=$m1.Invoke($null, @('https://maps.app.goo.gl/ZfEACc2qV8MVeBBc8'))
$long=$t1.GetAwaiter().GetResult()
Write-Host "Resolved URL:"
Write-Host $long
$m2=$type.GetMethod('AnalyserUrlGoogleMapsAsync')
$t2=$m2.Invoke($null, @($long))
$res=$t2.GetAwaiter().GetResult()
if ($res -eq $null) { Write-Host 'Analysis returned null'; exit 0 }
$resto=$res.Resto
Write-Host '--- Analysis ---'
Write-Host ('Nom: ' + ($resto.Nom))
Write-Host ('Ville: ' + ($resto.Ville))
Write-Host ('EstFiable: ' + $resto.EstFiable)
Write-Host ('RawUrl: ' + $res.RawUrl)
