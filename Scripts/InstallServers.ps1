cat "$PSSCRIPTROOT\..\Env\ServerInstances.txt" |%{
rm "c:\rust\$_\*" -force -recurse
c:\steamcmd\steamcmd.exe +force_install_dir "c:\rust\$_" +login anonymous +app_update 258550 validate﻿ +quit
mkdir "c:\rust\$_\server\server1\cfg"
cp "..\Env\users.cfg" "c:\rust\$_\server\server1\cfg\"
}