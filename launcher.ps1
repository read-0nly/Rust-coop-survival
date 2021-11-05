<#	
	--=={ RustPS Launcher }==--
	A Deluxe/Overengineered Rust server launcher/manager
	Come visit me on github~ https://read-0nly.github.io
#>	


#region Global Vars
$global:Settings=@{
	"identities" = @("coop_survival","skunkworks")
	"+server.identity" = ""
	"+server.port"=28999
	"+server.maxplayers"=5
	"+server.hostname"= "Test Server"
	#"+server.globalchat"=0
	"+server.description"="Dev server for various plugins"
	"+server.url"=""
	"dir"=$PSScriptRoot
}

$global:mapStr=""
$global:map = ""
if(test-path "$($global:Settings['dir'])\lastmap"){
	$global:map = (cat "$($global:Settings['dir'])\lastmap")
}
#endregion



function updateServer(){
	C:\steamcmd\steamcmd.exe +login anonymous +force_install_dir D:\rustserver\ +app_update 258550 +quit
	$downloadurl = "https://umod.org/games/rust/download?tag=public"
	iwr $downloadurl -UseBasicParsing -OutFile "$($global:Settings['dir'])\oxide.zip"
	Expand-Archive .\oxide.zip -DestinationPath "$($global:Settings['dir'])" -force
	$downloadurl = "https://github.com/k1lly0u/Oxide.Ext.RustEdit/blob/master/Oxide.Ext.RustEdit.dll?raw=true"
	iwr $downloadurl -UseBasicParsing -OutFile "$($global:Settings['dir'])\RustDedicated_Data\Managed\Oxide.Ext.RustEdit.dll"
}

function pickID(){
	for($i = 0; $i -lt $global:Settings["identities"].length;$i++){
		write-host ("["+$i+"]: "+$global:Settings["identities"][$i]) -foregroundcolor cyan
	}
	$idChoice = (read-host "Enter the number of the desired identity")
	if(([int32]::tryparse($idChoice,[ref]$null) -ne $false) -and ($idChoice -lt $global:Settings["identities"].length)){
		$global:Settings["+server.identity"]=$global:Settings["identities"][$idChoice]
	}else{ $global:Settings["+server.identity"]=$global:Settings["identities"][0]}
}

function runServer(){
	$identity = $global:Settings["+server.identity"]
	$dir = $global:Settings["dir"]
	$global:mapStr = (read-host "Enter map name [$global:map]").replace(".map","")
	if($global:mapStr -eq "/new"){
		$global:mapStr = ""
		if(test-path "$($global:Settings['dir'])\server\$identity\$global:map.map"){
			rm "$($global:Settings['dir'])\server\$identity\$global:map.map"
		}
	}
	$global:map = if($global:mapStr -ne ""){echo $global:mapStr}else{echo $global:map}
	if($global:map -like "*/new"){
		$global:map = $global:map.replace("/new","")
		if(test-path "$($global:Settings['dir'])\server\$identity\$global:map.map"){
			rm "$($global:Settings['dir'])\server\$identity\$global:map.map"
		}
	}
	if($global:map -eq ""){exit 0}
	$global:map | out-file "$($global:Settings['dir'])\lastmap"
	$serverConfigString = (-join (
		$global:Settings.keys.split("`n")|%{
			 $x = $global:Settings[$_]
			 $y=""
			 if($_[0] -eq '+'){
				 if($x -is [int]){
					$y+=" "+$_+" "+$x
				 }else{
					$y+=" "+$_+' "'+$x+'"'
				 }
			 }
			 if($y -ne ""){echo ($y)}
		 }))
		 write-host (@"
	RustDedicated.exe -batchmode -nographics$serverConfigString -dir="$dir" -levelurl "https://github.com/read-0nly/Rust-coop-survival/blob/main/maps/$global:map.map?raw=true" && exit
"@) -foregroundcolor yellow
	cmd /c (@"
	RustDedicated.exe -batchmode -nographics$serverConfigString -dir="$dir" -levelurl "https://github.com/read-0nly/Rust-coop-survival/blob/main/maps/$global:map.map?raw=true" && exit
"@)
	$global:mapStr=""
}



#region Legacy
<#
if((read-host "Would you like to update? (y/n)") -eq "y"){updateServer}

pickID
while((read-host "Continue? (y/n)") -eq "y"){
	runServer
}#>
#endregion

#Loads a VT100 terminal code object for more readable coloring
iex (iwr https://raw.githubusercontent.com/read-0nly/PSNet/master/Formatter.ps1 -usebasicparsing).content.substring(1)

$global:menu = [pscustomobject]@{
    "MenuState" = 1
    "Settings" = @{
        "Vertical" = $true;
        "Width"=63;
        "Spacer" = "|";
        "SelectionRange" = @(-1,0,1);
        "SelectionColors" = @("DarkRed","DarkGray","DarkGreen")
        "SelectionColorcodes" = @(([char]27+"[0m"),($Format.Back["Red"]),($Format.Back["Gray"]),($Format.Back["Green"]))
        "ColorMiddle" = 2
    };
    "Cursor" = 1;
    "Items" = @(
        [pscustomobject]@{
            "Name" = "Update Server";
            "Command" = [scriptBlock]{updateServer};
            "Selected" = 0
            "Selectable" = 0
        },    
        [pscustomobject]@{
            "Name" = "Pick Server Identity";
            "Command" = [scriptBlock]{pickID};
            "Selected" = 0
            "Selectable" = 0
        },    
        [pscustomobject]@{
            "Name" = "Run Server";
            "Command" = [scriptBlock]{runServer};
            "Selected" = 0
            "Selectable" = 0
        }
    )
}

$currentMenu = [ref]$global:Menu

function padString($entry, $width){   
    $string = ""   
    $string = (&{if($entry.Name -eq $Menu.Items[$menu.Cursor].name){"> "}else{""}})+$entry.name+(&{if($entry.Name -eq $Menu.Items[$menu.Cursor].name){" <"}else{""}})
    $pad = $menu.Settings["Width"] - $string.length
    $string = $string.PadLeft([int]($pad/2)+$string.length)
    $string = $string.PadRight($width)
    $string = $menu.settings["selectionColorCodes"][$menu.settings["ColorMiddle"] + $entry.selected] + $string + $menu.settings["selectionColorCodes"][$menu.settings["ColorMiddle"]]
    return $string
}

function paintMenu($curMenu){
    if($curMenu.Settings["Vertical"]){
        $curMenu.Items | %{ 
            write-host $curMenu.Settings["Spacer"] -backgroundcolor $curMenu.settings["SelectionColors"][$curMenu.settings["SelectionRange"].indexof(0)] -nonewline           
            write-host ((padString $_ $curMenu.Settings["Width"])) -backgroundcolor $curMenu.settings["SelectionColors"][$curMenu.settings["SelectionRange"].indexof($_.selected)] -nonewline
            write-host $curMenu.Settings["Spacer"] -backgroundcolor $curMenu.settings["SelectionColors"][$curMenu.settings["SelectionRange"].indexof(0)]
        }
    }
    else{
        $out = $menu.settings["selectionColorCodes"][$menu.settings["ColorMiddle"]]  + $curMenu.Settings["Spacer"] + ($curMenu.Items | %{
            ((padString $_ $curMenu.Settings["Width"])) +
            $curMenu.Settings["Spacer"]})+$menu.settings["selectionColorCodes"][0] 
        cls;    
        $out
        echo ""
    }
    echo "";
}

function paintScreen(){
    cls
    write-host "#################################################################"
    write-host "#                                                               #"
    write-host ("#                  "+$Format.Fore["Red"]+"--=={ "+ $Format.Fore["Red+"]+"RustPS Launcher"+$Format.Fore["Red"]+" }==--"+$Format.ResetAll+"                  #");
    write-host ("#                                                               #");
    write-host ("#             "+$Format.Fore["Green+"]+"Brought to you by read-0nly.github.io"+$Format.ResetAll+"             #")
    write-host "#                                                               #"
    write-host "#################################################################"
    write-host
    write-host
    write-host "#################################################################" -foregroundcolor yellow
    $Settings
    write-host "#################################################################" -foregroundcolor yellow
	write-host
	write-host
	
    paintMenu $currentMenu.Value

}

function menuStep(){
    if($psISE -eq $null){
        switch -wildcard ([system.console]::readkey().Key){
            "UpArrow" {
                if($menu.cursor -gt 0){
                    $Menu.Cursor--
                }
            }
            "LeftArrow" {
                if($menu.cursor -gt 0){
                    $menu.Cursor--
                }
            }
            "RightArrow" {
                if($menu.cursor -lt $menu.Items.Count-1){
                    $Menu.Cursor++
                }
            }
            "DownArrow" {
            if($menu.cursor -lt $menu.Items.Count-1){$Menu.Cursor++}}
            "Spacebar" {
                if(($global:Menu.Items[$global:Menu.Cursor].Selected -eq 0) -and ($global:Menu.Items[$global:Menu.Cursor].Selectable -gt 0)){
                    $global:Menu.Items[$global:Menu.Cursor].Selected = 1
                }else{
                    if($global:Menu.Items[$global:Menu.Cursor].Selected -gt 0 -and ($global:Menu.Items[$global:Menu.Cursor].Selectable -gt 0)){
                        $global:Menu.Items[$global:Menu.Cursor].Selected = -1
                    }else{
                        $global:Menu.Items[$global:Menu.Cursor].Selected = 0
                    }
                }
            }
            "*"{
                paintScreen;
            }
            "Enter" {
                & $global:Menu.Items[$global:Menu.Cursor].Command
                paintScreen;
            }
        }
    }
    else{
        switch -wildcard (read-host "Enter + or - to move the cursor, Space to switch selection status of an item, = to run the menu item"){
            "+"{if($global:menu.cursor -lt $global:menu.Items.Count-1){$global:Menu.Cursor++}}
            "-"{if($global:menu.cursor -gt 0){$global:Menu.Cursor--}}
            " "{
                if(($global:Menu.Items[$global:Menu.Cursor].Selected -eq 0) -and ($global:Menu.Items[$global:Menu.Cursor].Selectable -gt 0)){
                    $global:Menu.Items[$global:Menu.Cursor].Selected = 1
                }else{
                    if($global:Menu.Items[$global:Menu.Cursor].Selected -gt 0 -and ($global:Menu.Items[$global:Menu.Cursor].Selectable -gt 0)){
                        $global:Menu.Items[$global:Menu.Cursor].Selected = -1
                    }else{
                        $global:Menu.Items[$global:Menu.Cursor].Selected = 0
                    }
                }
            }
            "*"{paintScreen}
            "="{& $global:Menu.Items[$global:Menu.Cursor].Command;write-host "";paintScreen;}       
        }
    }
}

paintScreen

while($true){
    menuStep
}