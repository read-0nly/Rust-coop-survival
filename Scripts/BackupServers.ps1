param(
$backup = $true
)

function backupFile($backupPath3,$instancepath,$filepath){
	
	
	write-host "[$backupPath3]    [$instancepath]   [$filepath]"
	$pathFolder = ("$backupPath3\"+$filepath.split("\")[-1])
	if(-not (test-path $backupPath3)){
		mkdir $backupPath3
	}
	cp ($filepath) $backupPath3 
	
}
function backupFolder($path,$backupPath2,$folder){
	$subfolder = "$path\$folder\".replace("\\","\")
	$subfolder2 = "$backupPath2\$folder\".replace("\\","\")
	if(test-path "$subfolder"){
		if (-not (test-path $backupPath2)){mkdir $backupPath2}
		#dir C:\rust\grubhub\oxide\data\ | ?{$_.name -notlike "*.data"}
		$dataPaths= ((dir "$subfolder\*" -recurse -File).fullname |?{$_ -notlike "*.data"}).replace($path,"")
		$dataPaths|%{
			write-host "[$path]    [$backupPath2]   [$folder]"
			backupFile ("$backupPath2"+$_.replace($_.split("\")[-1],"")) ("$subfolder\") ($path+$_)
		}

	}
}
cat "$PSSCRIPTROOT\..\Env\ServerInstances.txt" |%{
	$instance=$_
	$path=""
	$backupPath = ""
	
	if(-not (test-path "C:\rust\$instance\oxide\")){
		mkdir "C:\rust\$instance\oxide\"
	}
	if(-not (test-path ("$PSScriptRoot\..\"+"Oxide Backup\$instance\"))){
		mkdir ("$PSScriptRoot\..\"+"Oxide Backup\$instance\")
	}
	if($backup){
		$path=(get-item "C:\rust\$instance\oxide\").fullname
		$backupPath=(get-item "$PSScriptRoot\..\").fullname+"Oxide Backup\$instance\"
	}else{
		$backupPath=(get-item "C:\rust\$instance\oxide\").fullname
		$path=(get-item "$PSScriptRoot\..\").fullname+"Oxide Backup\$instance\"
		
	}
	
	backupFolder $path $backupPath "data"
	backupFolder $path $backupPath "config" 
}
