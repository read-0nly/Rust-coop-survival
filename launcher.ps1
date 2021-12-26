<#	
	--=={ RustPS Launcher }==--
	A Deluxe/Overengineered Rust server launcher/manager
	Come visit me on github~ https://read-0nly.github.io
#>	



#region Global Vars
	$global:Settings=@{
		"identities" = @("coop_survival","skunkworks","hookdev")
		"+server.identity" = ""
		"+server.port"=28999
		"+server.maxplayers"=5
		"+server.hostname"= "GRUBHUB - Coop Survival - Dev server"
		#"+server.globalchat"=0
		"+server.description"="Lots of custom stuff going on. Going in raw will probably be a bad time. Save yourself the trouble, click View Vebsite to get to the github page with all the details."
		"+server.url"="https://github.com/read-0nly/Rust-coop-survival"
		"dir"=$PSScriptRoot
		"steamcmd"="C:\steamcmd\steamcmd.exe"
		"verbose"=$false
	}
	$ServerPath="https://github.com/read-0nly/Rust-coop-survival/raw/main/MapStorage/"
	$ServerSuffix=""#?raw=true"
#endregion #https://github.com/read-0nly/Rust-coop-survival/raw/main/MapStorage/NPCpen.map





$global:mapStr=""
$global:map = ""
if(test-path "$($global:Settings['dir'])\lastmap"){
	$global:map = (cat "$($global:Settings['dir'])\lastmap")
}
function updateServer(){
	cls
    write-host
    write-host "#################################################################" -foregroundcolor yellow
    write-host
	write-host "              Dev update - Rust, Oxide, Rustedit" -foregroundcolor Green
    write-host
    write-host "#################################################################" -foregroundcolor yellow
    write-host
    write-host	
	$newfile = "Rust"+((get-date).ticks)+".opj"
	if(test-path "$($global:Settings['dir'])\RustDedicated_Data\Managed\Rust.opj"){
		if((get-filehash "$($global:Settings['dir'])\OxidePatcher\Rust.opj") -ne (get-filehash "$($global:Settings['dir'])\RustDedicated_Data\Managed\Rust.opj")){
			copy-item "$($global:Settings['dir'])\OxidePatcher\Rust.opj" ("$($global:Settings['dir'])\OxidePatcher\$newfile") -force
			copy-item "$($global:Settings['dir'])\RustDedicated_Data\Managed\Rust.opj" ("$($global:Settings['dir'])\OxidePatcher\Rust.opj") -force
		}
	}
	write-host "-- Removing folders" -foregroundcolor Green
	Remove-Item -Recurse -Force "$($global:Settings['dir'])\RustDedicated_Data" -erroraction silentlycontinue
	Remove-Item -Recurse -Force "$($global:Settings['dir'])\oxideexport" -erroraction silentlycontinue
	write-host "-- Forcing steamcmd update" -foregroundcolor Green
	$x = (.$($global:Settings['steamcmd']) +force_install_dir $($global:Settings['dir']) +login anonymous +app_update 258550 validate +quit)
	if($global:Settings['verbose']){write-host ($x[-140..-1]) -foregroundcolor darkgray}
	write-host "-- Fetching new oxide files" -foregroundcolor Green
	$downloadurl = "https://umod.org/games/rust/download?tag=public"
	iwr $downloadurl -UseBasicParsing -OutFile "$($global:Settings['dir'])\oxide.zip"
	Expand-Archive .\oxide.zip -DestinationPath "$($global:Settings['dir'])\oxideexport" -force
	write-host "-- Copying patcher (overwrite)" -foregroundcolor Green
	copy-item "$($global:Settings['dir'])\OxidePatcher\*" "$($global:Settings['dir'])\RustDedicated_Data\Managed\" -force
	write-host "-- Safe-copying oxide files (no overwrite)" -foregroundcolor Green
	safecopy -p1 ("$($global:Settings['dir'])\oxideexport\") -p2 ("$($global:Settings['dir'])\")
	remove-item "$($global:Settings['dir'])\oxideexport" -recurse -force 
	remove-item "$($global:Settings['dir'])\oxide.zip" -force 
	write-host "-- Updating Rustedit DLL" -foregroundcolor Green
	$downloadurl = "https://github.com/k1lly0u/Oxide.Ext.RustEdit/blob/master/Oxide.Ext.RustEdit.dll?raw=true"
	iwr $downloadurl -UseBasicParsing -OutFile "$($global:Settings['dir'])\RustDedicated_Data\Managed\Oxide.Ext.RustEdit.dll"
	write-host "-- Launching Patcher - please apply all hooks, fields, methods" -foregroundcolor Green
	(cat "$($global:Settings['dir'])\RustDedicated_Data\Managed\Rust.opj" -raw).replace('"Flagged": true','"Flagged": false') | out-file "$($global:Settings['dir'])\RustDedicated_Data\Managed\Rust.opj"
	cd "$($global:Settings['dir'])\RustDedicated_Data\Managed\" 
	cmd /C "start OxidePatcher.exe"
	cd "$($global:Settings['dir'])" 
	read-host "Now patch and continue to the post-update steps - it should have open in your browser"
	start https://github.com/read-0nly/Rust-coop-survival/blob/main/postupdate_tasks.md
	cls
	paintscreen
}
function safecopy(){
	param($p1,$p2)
	(dir $p1 -recurse) |
		?{
			$x=$_;$b=$true;dir $p2 -recurse | 
			%{
				if($_.fullname.replace($p2," ") -eq $x.fullname.replace($p1," ")){
					$b=$false
				}
			};
			
			if($global:Settings['verbose']){
				if(-not $b){write-host ("----"+$_) -foregroundcolor yellow}
				else{write-host ("----"+$_.fullname) -foregroundcolor gray}
			}
			echo $b 
		}| 
		%{ 
			copy-item $_.fullname -destination ($p2+$_.fullname.replace($p1,"").replace($_.name,""))
		}
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
	cd $global:Settings['dir']
write-host (@"
RustDedicated.exe -batchmode -nographics$serverConfigString -dir="$dir" -levelurl "$ServerPath/$global:map.map$ServerSuffix" && exit
"@) -foregroundcolor yellow
cmd /c (@"
RustDedicated.exe -batchmode -nographics$serverConfigString -dir="$dir" -levelurl "$ServerPath/$global:map.map$ServerSuffix" && exit
"@)
$global:mapStr=""
read-host "Enter to continue"
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
		"Name" = "Set Description";
		"Command" = [scriptBlock]{
			$value = read-host "new description"
			if($value -ne ""){
				$global:Settings["+server.description"] = $value
				paintMenu
			}
		};
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