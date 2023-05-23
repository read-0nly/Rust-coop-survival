$strt = (pwd).path
cd "$PSSCRIPTROOT\..\Plugins"
cat "$PSSCRIPTROOT\..\Env\ServerInstances.txt" |%{
	if(-not (test-path "c:\rust\$_\oxide\plugins")){
		mkdir "c:\rust\$_\oxide\plugins"
	}
	if(-not (test-path "c:\rust\$_\oxide\config")){
		cp "..\config" "c:\rust\$_\oxide\config" -recurse
	}
	$env = $_;
	cat ("$PSSCRIPTROOT\..\Env\$_"+"-Plugins.txt") | %{
		$tgt = (dir "$_*" -recurse)[0]
		new-item -type symboliclink -target $tgt.fullname ("c:\rust\$env\oxide\plugins\"+$tgt.name)
	}
}
cd $strt