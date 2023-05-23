param($instance)
$OPJs=cat "$PSSCRIPTROOT\..\Patches\OxidePatcher\*.opj" -Raw | convertfrom-json -depth 24
$assemblies = @{};
$modifiers = @{};
$opjs[0].manifests |%{
	$manifest = $_; 
	$result = $_.hooks |?{
		-not ($_.hook.name -in ($opjs[1].manifests |?{
			$_.assemblyname -eq $manifest.assemblyname
		}).hooks.hook.name)
	};
	$assemblies.Add($manifest.assemblyname,$result)
	$result = $_.modifiers |?{
		-not ($_.name -in ($opjs[1].manifests |?{
			$_.assemblyname -eq $manifest.assemblyname
		}).modifiers.name)
	};
	$modifiers.Add($manifest.assemblyname,$result)
}

$opjs[1].manifests | %{
	if($assemblies.ContainsKey($_.assemblyname) -and $assemblies[$_.assemblyname] -ne $null){
		$_.hooks= $_.hooks+($assemblies[$_.assemblyname])
	}
	if($modifiers.ContainsKey($_.assemblyname) -and $modifiers[$_.assemblyname] -ne $null){
		$_.modifiers= $_.modifiers+($modifiers[$_.assemblyname])
	}
}
$opjs[1].TargetDirectory = "c:\rust\$instance\RustDedicated_Data\Managed\"
$opjs[1] | convertto-json -depth 24 | out-file "$PSSCRIPTROOT\..\Patches\OxidePatcher\rust.opj"