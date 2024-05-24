cd C:\rust\grubhub\
$targeturlroot = "https://github.com/read-0nly/Rust-coop-survival/raw/main/MapStorage/Breaktargets/"
$targetlist = @(
	"Fishing_a_1.map",
	"Fishing_b_1.map",
	"Fishing_c_1.map",
	"airfield_1.map",
	"bandit_1.map",
	"compound_1.map",
	"dome_1.map",
	"ferry_1.map",
	"gas_1.map",
	"harbor_1.map",
	"harbor_2.map",
	"junk_1.map",
	"lighthouse.map",
	"satdish_1.map",
	"stables_a_1.map",
	"stables_b_1.map",
	"supermarket_1.map",
	"warehouse_1.map"
	)
$targetlist| %{
	write-host ("Current:"+$_) -foregroundcolor cyan
	C:\rust\grubhub\RustDedicated.exe -batchmode +server.port 28015 +server.level "Procedural Map" +server.levelurl ("$targeturlroot"+$_) +server.maxplayers 1 +server.identity "server1"
}





