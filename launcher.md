# Rust Launcher

[Probably a dumb idea overengineered to lunacy. Download here](https://github.com/read-0nly/Rust-coop-survival/blob/main/launcher.ps1)

This is a powershell script - Before running it, open it in notepad - there's a section of global variables at the top you should fill. Here's what mine looks like, all set: 

![image](https://user-images.githubusercontent.com/33932119/140439683-4910f439-b2df-4b96-9f19-2333c67e3be4.png)

You'll notice that other that "dir" (which points to the location of the script - this should be the root of the rust server. Different path support is coming though), the settings start with +. You can copy add other server settings here using the same format, as long as they start with +, they'll get added to the command to launch the server


To run it, put it in your rust server folder (alongside RustDedicated.exe), right click on it and click "run in powershell", or click "file > open windows powershell" then run the following cmd:
```
./launcher.ps1
```

You may need to first run the following - please note that this opens up powershell script execution for the system. You must take care not to run dangerous .ps1 files after this
```
set-executionpolicy -scope CurrentUser -ExecutionPolicy Bypass
```

If you really just want to run this for this single powershell session (which I suggest before making the leap) this will open script execution for only the process, until it closes:
```
set-executionpolicy -scope Process -ExecutionPolicy Bypass
```

Here's what it should look like (depending on how up to date your powershell is):
![image](https://user-images.githubusercontent.com/33932119/140439836-2337f1d5-0d2e-45e1-b743-19eeb150c399.png)

This will not work in ISE by the way, it has to be a powershell console. Use up and down arrow keys to move the selection marker. Update Server will update through steamcmd, then download the oxide and rustedit dlls. Pick Identity lets you switch between server identities, so dev/prod divide. Run Server will launch the command with the current settings. It'll ask you for the map name - if you set your server URL, you should just need to specify the FILENAME part of FILENAME.map. If you add "/new" at the end of that, it'll delete the server-side copy of the map before launching.

![image](https://user-images.githubusercontent.com/33932119/140440019-1994bfaa-9dbf-4f7d-ac95-542efbf17675.png)
