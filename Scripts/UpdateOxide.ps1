	
	write-host "-- Assuring folders exist" -foregroundcolor Green
	if(-not(test-path "$PSSCRIPTROOT\..\Patches\oxideexport")){
		mkdir "$PSSCRIPTROOT\..\Patches\oxideexport"
	}if(-not(test-path "$PSSCRIPTROOT\..\Patches\OxidePatcher")){
		mkdir "$PSSCRIPTROOT\..\Patches\OxidePatcher"
	}
	
	write-host "-- Flushing old oxide" -foregroundcolor Green
	rm "$PSSCRIPTROOT\..\Patches\oxideexport" -recurse -force
	
	write-host "-- Fetching new oxide files" -foregroundcolor Green
	$downloadurl = "https://umod.org/games/rust/download?tag=public"
	iwr $downloadurl -UseBasicParsing -OutFile "$PSSCRIPTROOT\..\Patches\oxide.zip"
	Expand-Archive "$PSSCRIPTROOT\..\Patches\oxide.zip" -DestinationPath "$PSSCRIPTROOT\..\Patches\oxideexport" -force
	
	write-host "-- Updating Rustedit DLL" -foregroundcolor Green
	$downloadurl = "https://github.com/k1lly0u/Oxide.Ext.RustEdit/blob/master/Oxide.Ext.RustEdit.dll?raw=true"
	iwr $downloadurl -UseBasicParsing -OutFile "$PSSCRIPTROOT\..\Patches\oxideexport\RustDedicated_Data\Managed\Oxide.Ext.RustEdit.dll"
	
	write-host "-- Deleting OPJ" -foregroundcolor Green
	rm "$PSSCRIPTROOT\..\Patches\OxidePatcher\OxidePatcher.exe"
	rm "$PSSCRIPTROOT\..\Patches\OxidePatcher\rust.opj"
	
	write-host "-- Downloading OPJ" -foregroundcolor Green
	$downloadurl = "https://raw.githubusercontent.com/OxideMod/Oxide.Rust/develop/resources/Rust.opj"
	iwr $downloadurl -UseBasicParsing -OutFile "$PSSCRIPTROOT\..\Patches\OxidePatcher\rust.opj"
	$downloadurl = "https://github.com/OxideMod/Oxide.Patcher/releases/download/latest/OxidePatcher.exe"
	iwr $downloadurl -UseBasicParsing -OutFile "$PSSCRIPTROOT\..\Patches\OxidePatcher\OxidePatcher.exe"
	
	write-host "-- Patching servers" -foregroundcolor Green
	(cat "$PSSCRIPTROOT\..\env\CustomInstances.txt") |%{
		$pathRep = (get-item "$PSSCRIPTROOT\..\patches\oxideexport").fullname
		$instance = $_;
		if(-not (test-path "c:\rust\$_\RustDedicated_Data\Managed\x86")){
			mkdir "c:\rust\$_\RustDedicated_Data\Managed\x86"
		}
		if(-not (test-path "c:\rust\$_\RustDedicated_Data\Managed\x64")){
			mkdir "c:\rust\$_\RustDedicated_Data\Managed\x64"
		}
		dir "$PSSCRIPTROOT\..\patches\oxideexport\" -file -recurse| %{
			$newpath =($_.fullname.replace($pathRep,"c:\rust\$instance"))
			if(-not(test-path $newpath)){
				copy-item $_.fullname $newpath
			}
		}
		. "$PSSCRIPTROOT\MergeOPJ.ps1" -instance $_
		rm "c:\rust\$_\RustDedicated_Data\Managed\oxidepatcher.exe"
		copy-item "$PSSCRIPTROOT\..\patches\OxidePatcher\oxidepatcher.exe" "c:\rust\$_\RustDedicated_Data\Managed\oxidepatcher.exe" 
		rm "c:\rust\$_\RustDedicated_Data\Managed\rust.opj"
		new-item "c:\rust\$_\RustDedicated_Data\Managed\rust.opj" -target "$PSSCRIPTROOT\..\patches\OxidePatcher\rust.opj" -type symboliclink
		rm "c:\rust\$_\RustDedicated_Data\Managed\aiplus.opj"
		new-item "c:\rust\$_\RustDedicated_Data\Managed\aiplus.opj" -target "$PSSCRIPTROOT\..\patches\OxidePatcher\aiplus.opj" -type symboliclink
		
	}
	(cat "$PSSCRIPTROOT\..\env\OxideInstances.txt") |%{
		copy-item "$PSSCRIPTROOT\..\patches\oxideexport\*" "c:\rust\$_\" -force -recurse
	}