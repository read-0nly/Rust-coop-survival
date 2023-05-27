param($Instance)
if(test-path "$PSScriptRoot\..\Env\$instance-3rdparty.txt"){
	cat ("$PSScriptRoot\..\Env\$instance-3rdparty.txt") |%{
		wget $_ -OutFile ("C:\rust\$instance\oxide\plugins\"+($_.split("/")[-1]))}
}