// Requires: Cordyceps
#region using
	using Convert = System.Convert;
	using Network;
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.Collections;
	using System.Linq;
	using System.Text;
	using Oxide.Core.Libraries.Covalence;
	using Oxide.Plugins;
	using Oxide.Core.Plugins;
	using Oxide.Core;
	using UnityEngine; 
	using UnityEngine.SceneManagement;
	using UnityEngine.AI;
	using Rust.Ai;
	using Oxide.Ext.RustEdit;
	using Oxide.Ext.RustEdit.NPC;
#endregion
namespace Oxide.Plugins{
	[Info("FactionSystem", "obsol", "0.2.1")]
	[Description("AI Rewrite - bandit, scientist, animal, and unaffiliated faction, squad and faction control, animal squadding, in-game zone definition")]
	public class FactionSystem : RustPlugin{
		
	#region economy
		List<EconomyEntry> Economy = new List<EconomyEntry>();
		void launchEconomy(){
			Economy = new List<EconomyEntry>();
			Economy.Add(new EconomyEntry("12 Gauge Buckshot",-1685290200,32,62));
			Economy.Add(new EconomyEntry("12 Gauge Incendiary Shell",-1036635990,32,115));
			Economy.Add(new EconomyEntry("12 Gauge Slug",-727717969,32,62));
			Economy.Add(new EconomyEntry("16x Zoom Scope",174866732,1,300));
			Economy.Add(new EconomyEntry("40mm HE Grenade",349762871,1,54));
			Economy.Add(new EconomyEntry("40mm Shotgun Round",1055319033,1,3));
			Economy.Add(new EconomyEntry("40mm Smoke Grenade",915408809,1,33));
			Economy.Add(new EconomyEntry("5.56 Rifle Ammo",-1211166256,64,57));
			Economy.Add(new EconomyEntry("8x Zoom Scope",567235583,1,100));
			Economy.Add(new EconomyEntry("Adv. Anti-Rad Tea",-1729415579,1,80));
			Economy.Add(new EconomyEntry("Advanced Healing Tea",-2123125470,1,80));
			Economy.Add(new EconomyEntry("Advanced Max Health Tea",603811464,1,80));
			Economy.Add(new EconomyEntry("Advanced Ore Tea",2063916636,1,80));
			Economy.Add(new EconomyEntry("Advanced Rad. Removal Tea",2021351233,1,80));
			Economy.Add(new EconomyEntry("Advanced Scrap Tea",524678627,1,80));
			Economy.Add(new EconomyEntry("Advanced Wood Tea",-541206665,1,80));
			Economy.Add(new EconomyEntry("AND Switch",1171735914,1,10));
			Economy.Add(new EconomyEntry("Anti-Rad Tea",-487356515,1,20));
			Economy.Add(new EconomyEntry("Armored Cockpit Vehicle Module",1874610722,1,25));
			Economy.Add(new EconomyEntry("Armored Door",1353298668,1,665));
			Economy.Add(new EconomyEntry("Armored Double Door",1221063409,1,675));
			Economy.Add(new EconomyEntry("Armored Passenger Vehicle Module",-1615281216,1,25));
			Economy.Add(new EconomyEntry("Assault Rifle",1545779598,1,373));
			Economy.Add(new EconomyEntry("Auto Turret",-2139580305,1,1281));
			Economy.Add(new EconomyEntry("Basic Healing Tea",-929092070,1,20));
			Economy.Add(new EconomyEntry("Basic Max Health Tea",-1184406448,1,20));
			Economy.Add(new EconomyEntry("Basic Ore Tea",1480022580,1,20));
			Economy.Add(new EconomyEntry("Beancan Grenade",1840822026,1,22));
			Economy.Add(new EconomyEntry("Bed",-1273339005,1,48));
			Economy.Add(new EconomyEntry("Black Berry",1771755747,2,10));
			Economy.Add(new EconomyEntry("Blocker",-690968985,1,8));
			Economy.Add(new EconomyEntry("Blue Berry",1112162468,2,10));
			Economy.Add(new EconomyEntry("Blue Keycard",-484206264,1,40));
			Economy.Add(new EconomyEntry("Bolt Action Rifle",1588298435,1,219));
			Economy.Add(new EconomyEntry("Boots",-1549739227,1,57));
			Economy.Add(new EconomyEntry("Button",-1778897469,1,8));
			Economy.Add(new EconomyEntry("Camper Vehicle Module",-1040518150,1,20));
			Economy.Add(new EconomyEntry("CCTV Camera",634478325,1,504));
			Economy.Add(new EconomyEntry("Ceiling Light",1142993169,1,30));
			Economy.Add(new EconomyEntry("Chainsaw",1104520648,1,350));
			Economy.Add(new EconomyEntry("Cloth",-858312878,100,12));
			Economy.Add(new EconomyEntry("Cockpit Vehicle Module",-1501451746,1,17));
			Economy.Add(new EconomyEntry("Cockpit With Engine Vehicle Module",170758448,1,27));
			Economy.Add(new EconomyEntry("Coffee Can Helmet",-803263829,1,23));
			Economy.Add(new EconomyEntry("Combat Knife",2040726127,1,5));
			Economy.Add(new EconomyEntry("Computer Station",-1588628467,1,1287));
			Economy.Add(new EconomyEntry("Corn",1367190888,10,7));
			Economy.Add(new EconomyEntry("Counter",-216999575,1,8));
			Economy.Add(new EconomyEntry("Custom SMG",1796682209,1,95));
			Economy.Add(new EconomyEntry("Diesel Fuel",1568388703,1,150));
			Economy.Add(new EconomyEntry("Diving Fins",296519935,1,25));
			Economy.Add(new EconomyEntry("Diving Mask",-113413047,1,15));
			Economy.Add(new EconomyEntry("Diving Tank",-2022172587,1,35));
			Economy.Add(new EconomyEntry("Door Closer",1409529282,1,63));
			Economy.Add(new EconomyEntry("Door Controller",-502177121,1,8));
			Economy.Add(new EconomyEntry("Double Barrel Shotgun",-765183617,1,250));
			Economy.Add(new EconomyEntry("Electric Fuse",-629028935,1,20));
			Economy.Add(new EconomyEntry("Electric Heater",-784870360,1,75));
			Economy.Add(new EconomyEntry("Electrical Branch",-1448252298,1,8));
			Economy.Add(new EconomyEntry("Elevator",1177596584,1,151));
			Economy.Add(new EconomyEntry("Empty Propane Tank",-1673693549,1,20));
			Economy.Add(new EconomyEntry("Engine Vehicle Module",1559779253,1,16));
			Economy.Add(new EconomyEntry("Explosive 5.56 Rifle Ammo",-1321651331,64,299));
			Economy.Add(new EconomyEntry("Explosives",-592016202,5,104));
			Economy.Add(new EconomyEntry("Explosives",-592016202,1,20));
			Economy.Add(new EconomyEntry("F1 Grenade",143803535,1,13));
			Economy.Add(new EconomyEntry("Flame Thrower",-1215753368,1,262));
			Economy.Add(new EconomyEntry("Flame Turret",528668503,1,250));
			Economy.Add(new EconomyEntry("Flasher Light",-939424778,1,12));
			Economy.Add(new EconomyEntry("Flatbed Vehicle Module",-1880231361,1,12));
			Economy.Add(new EconomyEntry("Fluid Combiner",-265292885,1,30));
			Economy.Add(new EconomyEntry("Fluid Splitter",-1166712463,1,30));
			Economy.Add(new EconomyEntry("Fluid Switch & Pump",443432036,1,30));
			Economy.Add(new EconomyEntry("Fridge",1413014235,1,8));
			Economy.Add(new EconomyEntry("Fuel Tank Vehicle Module",1186655046,1,20));
			Economy.Add(new EconomyEntry("Garage Door",-148794216,1,280));
			Economy.Add(new EconomyEntry("Gears",479143914,1,125));
			Economy.Add(new EconomyEntry("Green Berry",858486327,2,10));
			Economy.Add(new EconomyEntry("Green Keycard",37122747,1,15));
			Economy.Add(new EconomyEntry("Hazmat Suit",1266491000,1,196));
			Economy.Add(new EconomyEntry("HBHF Sensor",-1507239837,1,8));
			Economy.Add(new EconomyEntry("Heavy Plate Helmet",1181207482,1,38));
			Economy.Add(new EconomyEntry("Heavy Plate Jacket",-1102429027,1,68));
			Economy.Add(new EconomyEntry("Heavy Plate Pants",-1778159885,1,38));
			Economy.Add(new EconomyEntry("High External Stone Gate",-691113464,1,640));
			Economy.Add(new EconomyEntry("High External Stone Wall",-967648160,1,75));
			Economy.Add(new EconomyEntry("High External Wooden Gate",-335089230,1,310));
			Economy.Add(new EconomyEntry("High External Wooden Wall",99588025,1,30));
			Economy.Add(new EconomyEntry("High Quality Carburetor",656371026,1,17));
			Economy.Add(new EconomyEntry("High Quality Crankshaft",1158340332,1,12));
			Economy.Add(new EconomyEntry("High Quality Metal",317398316,10,20));
			Economy.Add(new EconomyEntry("High Quality Pistons",1883981800,2,24));
			Economy.Add(new EconomyEntry("High Quality Spark Plugs",1072924620,2,24));
			Economy.Add(new EconomyEntry("High Quality Valves",-1802083073,2,24));
			Economy.Add(new EconomyEntry("High Velocity Rocket",-1841918730,1,64));
			Economy.Add(new EconomyEntry("Holosight",442289265,1,274));
			Economy.Add(new EconomyEntry("Homemade Landmine",-1663759755,1,15));
			Economy.Add(new EconomyEntry("Hoodie",1751045826,1,20));
			Economy.Add(new EconomyEntry("HV 5.56 Rifle Ammo",1712070256,64,246));
			Economy.Add(new EconomyEntry("HV Pistol Ammo",-1691396643,64,164));
			Economy.Add(new EconomyEntry("Incendiary 5.56 Rifle Ammo",605467368,64,166));
			Economy.Add(new EconomyEntry("Incendiary Pistol Bullet",51984655,64,111));
			Economy.Add(new EconomyEntry("Incendiary Rocket",1638322904,1,290));
			Economy.Add(new EconomyEntry("Jackhammer",1488979457,1,150));
			Economy.Add(new EconomyEntry("L96 Rifle",-778367295,1,339));
			Economy.Add(new EconomyEntry("Ladder Hatch",1948067030,1,423));
			Economy.Add(new EconomyEntry("Large Flatbed Vehicle Module",-1693832478,1,18));
			Economy.Add(new EconomyEntry("Large Furnace",-1992717673,1,350));
			Economy.Add(new EconomyEntry("Large Medkit",254522515,1,21));
			Economy.Add(new EconomyEntry("Large Planter Box",1581210395,1,64));
			Economy.Add(new EconomyEntry("Large Rechargeable Battery",553270375,1,520));
			Economy.Add(new EconomyEntry("Large Solar Panel",2090395347,1,260));
			Economy.Add(new EconomyEntry("Large Water Catcher",-1100168350,1,90));
			Economy.Add(new EconomyEntry("Laser Detector",-798293154,1,8));
			Economy.Add(new EconomyEntry("Leather",1381010055,100,200));
			Economy.Add(new EconomyEntry("Leather",1381010055,1,2));
			Economy.Add(new EconomyEntry("Locker",-110921842,1,40));
			Economy.Add(new EconomyEntry("Longsword",-1469578201,1,40));
			Economy.Add(new EconomyEntry("Low Grade Fuel",-946369541,100,50));
			Economy.Add(new EconomyEntry("LR-300 Assault Rifle",-1812555177,1,500));
			Economy.Add(new EconomyEntry("M249",-2069578888,1,624));
			Economy.Add(new EconomyEntry("M39 Rifle",28201841,1,400));
			Economy.Add(new EconomyEntry("M92 Pistol",-852563019,1,250));
			Economy.Add(new EconomyEntry("Medical Syringe",1079279582,2,16));
			Economy.Add(new EconomyEntry("Medium Rechargeable Battery",2023888403,1,260));
			Economy.Add(new EconomyEntry("Memory Cell",-746647361,1,8));
			Economy.Add(new EconomyEntry("Metal Barricade",1655650836,1,50));
			Economy.Add(new EconomyEntry("Metal Blade",1882709339,1,15));
			Economy.Add(new EconomyEntry("Metal Chest plate",1110385766,1,270));
			Economy.Add(new EconomyEntry("Metal Facemask",-194953424,1,220));
			Economy.Add(new EconomyEntry("Metal Fragments",69511070,250,25));
			Economy.Add(new EconomyEntry("Metal horizontal embrasure",-1199897169,1,10));
			Economy.Add(new EconomyEntry("Metal Pipe",95950017,1,30));
			Economy.Add(new EconomyEntry("Metal Spring",-1021495308,1,60));
			Economy.Add(new EconomyEntry("Metal Vertical embrasure",-1199897172,1,10));
			Economy.Add(new EconomyEntry("Mining Quarry",1052926200,1,1000));
			Economy.Add(new EconomyEntry("Mixing Table",1259919256,1,175));
			Economy.Add(new EconomyEntry("MLRS Aiming Module",343045591,1,505));
			Economy.Add(new EconomyEntry("MLRS Rocket",-1843426638,1,94));
			Economy.Add(new EconomyEntry("Modular Car Lift",1696050067,1,155));
			Economy.Add(new EconomyEntry("MP5A4",1318558775,1,169));
			Economy.Add(new EconomyEntry("Multiple Grenade Launcher",-1123473824,1,86));
			Economy.Add(new EconomyEntry("Muzzle Boost",-1405508498,1,20));
			Economy.Add(new EconomyEntry("Muzzle Brake",1478091698,1,16));
			Economy.Add(new EconomyEntry("Night Vision Goggles",-1518883088,1,520));
			Economy.Add(new EconomyEntry("OR Switch",-1286302544,1,10));
			Economy.Add(new EconomyEntry("Paddle",1491189398,1,25));
			Economy.Add(new EconomyEntry("Pants",237239288,1,20));
			Economy.Add(new EconomyEntry("Passenger Vehicle Module",895374329,1,20));
			Economy.Add(new EconomyEntry("Pistol Bullet",785728077,64,43));
			Economy.Add(new EconomyEntry("Potato",-2086926071,10,50));
			Economy.Add(new EconomyEntry("Powered Water Purifier",-365097295,1,150));
			Economy.Add(new EconomyEntry("Pressure Pad",-2049214035,1,188));
			Economy.Add(new EconomyEntry("Pump Jack",-1130709577,1,750));
			Economy.Add(new EconomyEntry("Pump Shotgun",795371088,1,150));
			Economy.Add(new EconomyEntry("Pumpkin",-567909622,10,50));
			Economy.Add(new EconomyEntry("Pure Anti-Rad Tea",-33009419,1,320));
			Economy.Add(new EconomyEntry("Pure Healing Tea",-1677315902,1,320));
			Economy.Add(new EconomyEntry("Pure Max Health Tea",1712261904,1,320));
			Economy.Add(new EconomyEntry("Pure Ore Tea",1729374708,1,320));
			Economy.Add(new EconomyEntry("Pure Rad. Removal Tea",1905387657,1,320));
			Economy.Add(new EconomyEntry("Pure Scrap Tea",2024467711,1,320));
			Economy.Add(new EconomyEntry("Pure Wood Tea",-557539629,1,320));
			Economy.Add(new EconomyEntry("Python Revolver",1373971859,1,200));
			Economy.Add(new EconomyEntry("Rad. Removal Tea",-496584751,1,20));
			Economy.Add(new EconomyEntry("RAND Switch",492357192,1,8));
			Economy.Add(new EconomyEntry("Rear Seats Vehicle Module",1376065505,1,17));
			Economy.Add(new EconomyEntry("Red Berry",1272194103,2,10));
			Economy.Add(new EconomyEntry("Red Keycard",-1880870149,1,80));
			Economy.Add(new EconomyEntry("Reinforced Glass Window",671706427,1,8));
			Economy.Add(new EconomyEntry("RF Broadcaster",-1044468317,1,260));
			Economy.Add(new EconomyEntry("RF Pager",-566907190,1,258));
			Economy.Add(new EconomyEntry("RF Transmitter",596469572,1,260));
			Economy.Add(new EconomyEntry("Rifle Body",176787552,1,29));
			Economy.Add(new EconomyEntry("Road Sign Jacket",-2002277461,1,170));
			Economy.Add(new EconomyEntry("Road Sign Kilt",1850456855,1,100));
			Economy.Add(new EconomyEntry("Road Signs",1199391518,1,50));
			Economy.Add(new EconomyEntry("Roadsign Gloves",-699558439,1,120));
			Economy.Add(new EconomyEntry("Rocket",-742865266,1,319));
			Economy.Add(new EconomyEntry("Rocket Launcher",442886268,1,200));
			Economy.Add(new EconomyEntry("Root Combiner",-458565393,1,8));
			Economy.Add(new EconomyEntry("Rope",1414245522,1,4));
			Economy.Add(new EconomyEntry("Salvaged Axe",-262590403,1,105));
			Economy.Add(new EconomyEntry("Salvaged Cleaver",-1978999529,1,55));
			Economy.Add(new EconomyEntry("Salvaged Hammer",-1506397857,1,35));
			Economy.Add(new EconomyEntry("Salvaged Icepick",-1780802565,1,105));
			Economy.Add(new EconomyEntry("Salvaged Shelves",1950721418,1,25));
			Economy.Add(new EconomyEntry("Salvaged Sword",1326180354,1,17));
			Economy.Add(new EconomyEntry("SAM Ammo",-384243979,1,31));
			Economy.Add(new EconomyEntry("SAM Site",-1009359066,1,500));
			Economy.Add(new EconomyEntry("Satchel Charge",-1878475007,1,94));
			Economy.Add(new EconomyEntry("Scrap",-932201673,1,20));
			Economy.Add(new EconomyEntry("Search Light",2087678962,1,30));
			Economy.Add(new EconomyEntry("Semi Automatic Body",573926264,1,27));
			Economy.Add(new EconomyEntry("Semi-Automatic Pistol",818877484,1,65));
			Economy.Add(new EconomyEntry("Semi-Automatic Rifle",-904863145,1,140));
			Economy.Add(new EconomyEntry("Sewing Kit",1234880403,1,15));
			Economy.Add(new EconomyEntry("Sheet Metal",-1994909036,1,30));
			Economy.Add(new EconomyEntry("Shotgun Trap",352499047,1,293));
			Economy.Add(new EconomyEntry("Silencer",-1850571427,1,10));
			Economy.Add(new EconomyEntry("Siren Light",762289806,1,12));
			Economy.Add(new EconomyEntry("Small Generator",1849887541,1,260));
			Economy.Add(new EconomyEntry("Small Oil Refinery",-1293296287,1,179));
			Economy.Add(new EconomyEntry("Small Planter Box",1903654061,1,32));
			Economy.Add(new EconomyEntry("Small Rechargeable Battery",-692338819,1,10));
			Economy.Add(new EconomyEntry("Small Water Catcher",-132247350,1,37));
			Economy.Add(new EconomyEntry("Smart Alarm",-695978112,1,256));
			Economy.Add(new EconomyEntry("Smart Switch",988652725,1,256));
			Economy.Add(new EconomyEntry("SMG Body",1230323789,1,19));
			Economy.Add(new EconomyEntry("Snow Jacket",-48090175,1,23));
			Economy.Add(new EconomyEntry("Spas-12 Shotgun",-41440462,1,250));
			Economy.Add(new EconomyEntry("Speargun",-1517740219,1,9));
			Economy.Add(new EconomyEntry("Speargun Spear",-1800345240,1,5));
			Economy.Add(new EconomyEntry("Splitter",-563624462,1,10));
			Economy.Add(new EconomyEntry("Sprinkler",-781014061,1,15));
			Economy.Add(new EconomyEntry("Stones",-2099697608,1000,50));
			Economy.Add(new EconomyEntry("Storage Monitor",1149964039,1,256));
			Economy.Add(new EconomyEntry("Storage Vehicle Module",268565518,1,8));
			Economy.Add(new EconomyEntry("Strengthened Glass Window",-1614955425,1,5));
			Economy.Add(new EconomyEntry("Survey Charge",1975934948,1,22));
			Economy.Add(new EconomyEntry("Switch",1951603367,1,10));
			Economy.Add(new EconomyEntry("Tactical Gloves",-1108136649,1,95));
			Economy.Add(new EconomyEntry("Targeting Computer",1523195708,1,757));
			Economy.Add(new EconomyEntry("Tarp",2019042823,1,30));
			Economy.Add(new EconomyEntry("Taxi Vehicle Module",-626174997,1,30));
			Economy.Add(new EconomyEntry("Tech Trash",73681876,1,250));
			Economy.Add(new EconomyEntry("Tesla Coil",1371909803,1,256));
			Economy.Add(new EconomyEntry("Test Generator",-295829489,1,346));
			Economy.Add(new EconomyEntry("Thompson",-1758372725,1,101));
			Economy.Add(new EconomyEntry("Timed Explosive Charge",1248356124,1,918));
			Economy.Add(new EconomyEntry("Timer",665332906,1,8));
			Economy.Add(new EconomyEntry("Torpedo",-1671551935,1,38));
			Economy.Add(new EconomyEntry("Triangle Ladder Hatch",2041899972,1,423));
			Economy.Add(new EconomyEntry("Watch Tower",-463122489,1,4));
			Economy.Add(new EconomyEntry("Water Jug",-119235651,1,5));
			Economy.Add(new EconomyEntry("Water Pump",-1284169891,1,200));
			Economy.Add(new EconomyEntry("Weapon Flashlight",952603248,1,6));
			Economy.Add(new EconomyEntry("Weapon Lasersight",-132516482,1,256));
			Economy.Add(new EconomyEntry("Wetsuit",-1101924344,1,20));
			Economy.Add(new EconomyEntry("White Berry",854447607,2,10));
			Economy.Add(new EconomyEntry("Wind Turbine",-1819763926,1,500));
			Economy.Add(new EconomyEntry("Wood",-151838493,1000,20));
			Economy.Add(new EconomyEntry("Wooden Barricade",866889860,1,10));
			Economy.Add(new EconomyEntry("Wooden Ladder",-316250604,1,18));
			Economy.Add(new EconomyEntry("Workbench Level 1",1524187186,1,70));
			Economy.Add(new EconomyEntry("Workbench Level 2",-41896755,1,590));
			Economy.Add(new EconomyEntry("Workbench Level 3",-1607980696,1,1550));
			Economy.Add(new EconomyEntry("XOR Switch",1293102274,1,10));
			Economy.Add(new EconomyEntry("Yellow Berry",1660145984,2,10));

		}
	
	#endregion
		
	#region config
		public static Configuration config;
		public int shopCount = 0;
		public int scrap = -932201673;
		public class Configuration{
			[JsonProperty("playerScores", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>> playerScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
			[JsonProperty("factionBanks", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<BaseCombatEntity.Faction, float> factionBank = new Dictionary<BaseCombatEntity.Faction, float>();
			[JsonProperty("Economy", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public List<EconomyEntry> Economy = new List<EconomyEntry>();
			
			
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		protected override void LoadDefaultConfig() => config = new Configuration();
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					Puts("Configuration appears to be outdated; updating and saving");
					SaveConfig();
					}
				Economy = config.Economy;
			}
			catch{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
				launchEconomy();
				
			}
		}
		protected override void SaveConfig(){
			config.Economy = Economy;
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		#endregion config
	#region classes
		public class EconomyEntry{
			public string name;
			public int id;
			public int amount;
			public int price;
			public EconomyEntry(string s,int i,int a,int p){
				name=s;id=i;amount=a;price=p;
			}
			
		}
		struct GridCell{
			
			public bool isWater;
			public bool isMonument;
			public bool isShop;
			public bool isAirdrop;
			
			public bool isScientist;
			public bool isBandit;
			public bool isSciAggro;
			public bool isBanAggro;
			
			public byte sciCount;
			public byte banCount;
			
			public byte sciTarget;
			public byte banTarget;
			
			public ushort sciTimeout;
			public ushort banTimeout;
			
			public void setFlags(byte flags){
				isWater = (flags & (byte)0x80)==(byte)0x80;
				isMonument = (flags & (byte)0x40)==(byte)0x40;
				isShop = (flags & (byte)0x20)==(byte)0x20;
				isAirdrop = (flags & (byte)0x10)==(byte)0x10;
				isScientist = (flags & (byte)8)==(byte)8;
				isBandit = (flags & (byte)4)==(byte)4;
				isSciAggro = (flags & (byte)2)==(byte)2;
				isBanAggro = (flags & (byte)1)==(byte)1;
			}
			public byte getFlags(){
				
				return (byte)((isWater?0x80:0)+(isMonument?0x40:0)+(isShop?0x20:0)+(isAirdrop?0x10:0)+(isScientist?8:0)+(isBandit?4:0)+(isSciAggro?2:0)+(isBanAggro?1:0));
			}
			public byte[] pack(){
				byte[] result = new byte[9];
				result[0]=getFlags();
				result[1]=sciCount;
				result[2]=banCount;
				result[3]=sciTarget;
				result[4]=banTarget;
				byte[] i =BitConverter.GetBytes(sciTimeout);
				result[5]=i[0];
				result[6]=i[1];
				i = BitConverter.GetBytes(banTimeout);
				result[7]=i[0];
				result[8]=i[1];
				return result;
			}
			public void unpack(byte[] result){
				setFlags(result[0]);
				sciCount=result[1];
				banCount=result[2];
				sciTarget=result[3];
				banTarget=result[4];
				byte[] shortRes = new byte[2];
				shortRes[0]= result[5];
				shortRes[1]= result[6];
				sciTimeout = BitConverter.ToUInt16(result, 5);
				banTimeout = BitConverter.ToUInt16(result, 7);
			}
			
		}
		public class GridAgent{
			public Vector3Int position;
			public bool state;
			public float lastUpdate;
			public OrderCell currentTarget;
			public BaseAIBrain brain;
		}
		public class MonumentCell{
			public MonumentInfo info;
			public Vector3Int position;
			public string name;
			public float lastTime;
		}
		public class OrderCell{
			public BaseCombatEntity.Faction faction;
			public List<BaseAIBrain> assignedAgents = new List<BaseAIBrain>();
			public Vector3Int position;
			public int targetCount;
			public float timeOut;
		}
		
			#region Custom States
			class FactionCombatState : ScientistBrain.CombatState{
				private global::StateStatus status = global::StateStatus.Error;
				
				public override void StateEnter(BaseAIBrain brain, BaseEntity selfEntity){
					if(this.brain==null){return;}
					if(this.brain.Navigator==null){return;}
					if(this.brain.Navigator.BaseEntity==null){
						return;
					}		
					if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
					{
						(this.brain.Navigator.BaseEntity as BasePlayer).Hurt(10f);
					}
					if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
						return;
					}		
					
					GridAgent ga = getGridAgent(this.brain);
					HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
					if (!(hn.inventory == null || hn.inventory.containerBelt == null))
					{
						Item slot = hn.inventory.containerBelt.GetSlot(0);
						if (slot != null)
						{
							hn.UpdateActiveItem(hn.inventory.containerBelt.GetSlot(0).uid);
							BaseEntity heldEntity = slot.GetHeldEntity();
							if (heldEntity != null)
							{
								AttackEntity component = heldEntity.GetComponent<AttackEntity>();
								if (component != null)
								{
									component.TopUpAmmo();
								}
							}
						}
					}
					
				}
			}
			class FactionCombatStationaryState : ScientistBrain.CombatStationaryState{
				public override void StateEnter(BaseAIBrain brain, BaseEntity selfEntity){
					if(this.brain.Navigator.BaseEntity==null){
						base.StateEnter(brain, selfEntity);					return;
					}		
					if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
						base.StateEnter(brain, selfEntity);					return;
					}		
					HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
					if (hn.inventory == null || hn.inventory.containerBelt == null)
					{
						base.StateEnter(brain, selfEntity);
					}
					
					GridAgent ga = getGridAgent(this.brain);
					Item slot = hn.inventory.containerBelt.GetSlot(0);
					
					if (slot != null)
					{
						hn.UpdateActiveItem(hn.inventory.containerBelt.GetSlot(0).uid);
						BaseEntity heldEntity = slot.GetHeldEntity();
						if (heldEntity != null)
						{
							AttackEntity component = heldEntity.GetComponent<AttackEntity>();
							if (component != null)
							{
								component.TopUpAmmo();
							}
						}
					}
					base.StateEnter(brain, selfEntity);
				}
			}
			class FactionAnimalIdleState : BaseAIBrain.BaseIdleState{
				private global::StateStatus status = global::StateStatus.Error;
				
				public override global::StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity selfEntity)
				{
					if(!waitTimes.ContainsKey(this.brain)) waitTimes.Add(this.brain,0f);
					waitTimes[this.brain]+=delta;
					if(waitTimes[this.brain]<0.1f){return global::StateStatus.Running;}
					waitTimes[this.brain]=0;
					if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
						if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
							if(!(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.IsSet()){
								if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 10f)
								{//
									this.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
									forwardpos.y-=0.25f;
									RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									return StateStatus.Finished;																											
								}
							}else{
								BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Normal;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.SPRINT))speed=BaseNavigator.NavigationSpeed.Fast;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.DUCK))speed=BaseNavigator.NavigationSpeed.Slowest;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.name.Contains("/chair.deployed")){
									this.brain.Navigator.BaseEntity.creatorEntity.transform.position=(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.parent.position+(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.localPosition;
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).syncPosition=true;
									this.brain.Navigator.BaseEntity.creatorEntity.SendNetworkUpdateImmediate(true);
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).SendNetworkUpdateImmediate(true);
								}
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.FIRE_SECONDARY)){
									if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 30f)
									{
										this.brain.Navigator.ClearFacingDirectionOverride();
										Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
										forwardpos.y-=0.25f;
										RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
										return global::StateStatus.Running;
									}else{
										this.brain.Navigator.ClearFacingDirectionOverride();
										//Vector3 forwardpos = this.brain.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*5);
										Vector3 forwardpos = this.brain.transform.position + (this.brain.transform.forward*5);
										this.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																													
									}
								}
								else{
									if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 30f)
									{
										this.brain.Navigator.ClearFacingDirectionOverride();
										Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
										forwardpos.y-=0.25f;
										RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
										return global::StateStatus.Running;
									}else{
										this.brain.Navigator.ClearFacingDirectionOverride();
										//Vector3 forwardpos = this.brain.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*5);
										Vector3 forwardpos = this.brain.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.HeadForward()*5);
										this.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																										
									}
								}
							}
							
						}
					}
					return StateStatus.Running;
				}
			}
			class FactionAnimalRoamState : AnimalBrain.BaseRoamState{
				private global::StateStatus status = global::StateStatus.Error;
			
				public override global::StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity selfEntity)
				{
						if(!waitTimes.ContainsKey(this.brain)) waitTimes.Add(this.brain,0f);
					waitTimes[this.brain]+=delta;
					if(waitTimes[this.brain]<0.1f){return global::StateStatus.Running;}
					waitTimes[this.brain]=0;
					if(this.brain.Navigator.BaseEntity.creatorEntity){
						if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
							if(!(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.IsSet()){
								if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 10f)
								{//
									this.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*UnityEngine.Random.Range(1f,3f));
									forwardpos.y-=0.25f;
									RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									return global::StateStatus.Running;
								}
							}else{
								BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Normal;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.SPRINT))speed=BaseNavigator.NavigationSpeed.Fast;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.DUCK))speed=BaseNavigator.NavigationSpeed.Slowest;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.name.Contains("/chair.deployed")){
									this.brain.Navigator.BaseEntity.creatorEntity.transform.position=(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.parent.position+(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.localPosition;
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).syncPosition=true;
									this.brain.Navigator.BaseEntity.creatorEntity.SendNetworkUpdateImmediate(true);
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).SendNetworkUpdateImmediate(true);
								}
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.FIRE_SECONDARY)){
									if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 30f)
									{
										this.brain.Navigator.ClearFacingDirectionOverride();
										Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
										forwardpos.y-=0.25f;
										RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
										return global::StateStatus.Running;
									}else{
										this.brain.Navigator.ClearFacingDirectionOverride();
										Vector3 forwardpos = this.brain.transform.position + (this.brain.transform.forward*5);
										this.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																						
									}
								}
								else{
									if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 30f)
									{
										this.brain.Navigator.ClearFacingDirectionOverride();
										Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
										forwardpos.y-=0.25f;
										RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
										return global::StateStatus.Running;
									}else{
										this.brain.Navigator.ClearFacingDirectionOverride();
										//Vector3 forwardpos = this.brain.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*5);
										Vector3 forwardpos = this.brain.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.HeadForward()*5);
										this.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																										
									}
								}
							}
							
						}
					}
					return global::StateStatus.Running;
				}			
			}
			class FactionAnimalChaseState : AnimalBrain.ChaseState{
				private global::StateStatus status = global::StateStatus.Error;
				
				public override global::StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity selfEntity)
				{
					if(this.brain==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity.creatorEntity==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity.creatorEntity.transform==null){return base.StateThink(delta,brain,selfEntity);}
					if(!waitTimes.ContainsKey(this.brain)) waitTimes.Add(this.brain,0f);
					waitTimes[this.brain]+=delta;
					if(waitTimes[this.brain]<0.5f){return global::StateStatus.Running;}
					waitTimes[this.brain]=0;
					if(this.brain.Navigator.BaseEntity.creatorEntity){
						if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
							if(!(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.IsSet()){
								if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 20f)
								{//
									this.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*UnityEngine.Random.Range(7f,15f));
									forwardpos.y-=0.25f;
									RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									return global::StateStatus.Running;
								}
							}else{
								BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Normal;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.SPRINT))speed=BaseNavigator.NavigationSpeed.Fast;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.name.Contains("/chair.deployed")){
									this.brain.Navigator.BaseEntity.creatorEntity.transform.position=(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.parent.position+(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.localPosition;
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).syncPosition=true;
									this.brain.Navigator.BaseEntity.creatorEntity.SendNetworkUpdateImmediate(true);
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).SendNetworkUpdateImmediate(true);
								}
								
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.FIRE_SECONDARY) || !(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.SPRINT) || UnityEngine.Random.Range(0,1)!=0){
									return base.StateThink(delta,brain,selfEntity);
								}
								else{
									if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 20f)
									{
										this.brain.Navigator.ClearFacingDirectionOverride();
										Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
										forwardpos.y-=0.25f;
										RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
										return global::StateStatus.Running;
									}else{
										this.brain.Navigator.ClearFacingDirectionOverride();
										//Vector3 forwardpos = this.brain.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*5);
										Vector3 forwardpos = this.brain.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.HeadForward()*5);
										this.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																										
									}
								}
							}
							
						}
					}
					return base.StateThink(delta,brain,selfEntity);
				}	
				
			}
			class FactionAnimalAttackState : AnimalBrain.AttackState{
				private global::StateStatus status = global::StateStatus.Error;
				
				public override global::StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity selfEntity)
				{
					if(this.brain==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity.creatorEntity==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity.creatorEntity.transform==null){return base.StateThink(delta,brain,selfEntity);}
					if(!waitTimes.ContainsKey(this.brain)) waitTimes.Add(this.brain,0f);
					waitTimes[this.brain]+=delta;
					if(waitTimes[this.brain]<0.5f){return global::StateStatus.Running;}
					waitTimes[this.brain]=0;
					if(this.brain.Navigator.BaseEntity.creatorEntity){
						if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
							if(!(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.IsSet()){
								if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 20f)
								{//
									this.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*UnityEngine.Random.Range(7f,15f));
									forwardpos.y-=0.25f;
									RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									return global::StateStatus.Running;
								}
							}else{
								BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Normal;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.SPRINT))speed=BaseNavigator.NavigationSpeed.Fast;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.name.Contains("/chair.deployed")){
									this.brain.Navigator.BaseEntity.creatorEntity.transform.position=(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.parent.position+(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.localPosition;
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).syncPosition=true;
									this.brain.Navigator.BaseEntity.creatorEntity.SendNetworkUpdateImmediate(true);
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).SendNetworkUpdateImmediate(true);
								}
								
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.FIRE_SECONDARY) || !(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.SPRINT) || UnityEngine.Random.Range(0,1)!=0){
									return base.StateThink(delta,brain,selfEntity);
								}
								else{
									if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 20f)
									{
										this.brain.Navigator.ClearFacingDirectionOverride();
										Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
										forwardpos.y-=0.25f;
										RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
										return global::StateStatus.Running;
									}else{
										this.brain.Navigator.ClearFacingDirectionOverride();
										//Vector3 forwardpos = this.brain.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*5);
										Vector3 forwardpos = this.brain.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.HeadForward()*5);
										this.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																										
									}
								}
							}
							
						}
					}
					return base.StateThink(delta,brain,selfEntity);
				}			
				
			}
			class FactionAnimalFleeState : AnimalBrain.FleeState{
				private global::StateStatus status = global::StateStatus.Error;
				
				public override global::StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity selfEntity)
				{
					if(this.brain==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity.creatorEntity==null){return base.StateThink(delta,brain,selfEntity);}
					if(this.brain.Navigator.BaseEntity.creatorEntity.transform==null){return base.StateThink(delta,brain,selfEntity);}
					if(!waitTimes.ContainsKey(this.brain)) waitTimes.Add(this.brain,0f);
					waitTimes[this.brain]+=delta;
					if(waitTimes[this.brain]<0.5f){return global::StateStatus.Running;}
					waitTimes[this.brain]=0;
					if(this.brain.Navigator.BaseEntity.creatorEntity){
						if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
							if(!(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.IsSet()){
								if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 20f)
								{//
									this.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*UnityEngine.Random.Range(7f,15f));
									forwardpos.y-=0.25f;
									RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									return global::StateStatus.Running;
								}
							}else{
								BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Normal;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.SPRINT))speed=BaseNavigator.NavigationSpeed.Fast;
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.name.Contains("/chair.deployed")){
									this.brain.Navigator.BaseEntity.creatorEntity.transform.position=(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.parent.position+(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).transform.localPosition;
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).syncPosition=true;
									this.brain.Navigator.BaseEntity.creatorEntity.SendNetworkUpdateImmediate(true);
									(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).mounted.Get(true).SendNetworkUpdateImmediate(true);
								}
								
								if((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.FIRE_SECONDARY) || !(this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).serverInput.IsDown(BUTTON.SPRINT) || UnityEngine.Random.Range(0,1)!=0){
									return base.StateThink(delta,brain,selfEntity);
								}
								else{
									if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 20f)
									{
										this.brain.Navigator.ClearFacingDirectionOverride();
										Vector3 forwardpos = this.brain.Navigator.BaseEntity.creatorEntity.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
										forwardpos.y-=0.25f;
										RadialPoint(this.brain.Navigator, forwardpos,this.brain.transform.position);	
										return global::StateStatus.Running;
									}else{
										this.brain.Navigator.ClearFacingDirectionOverride();
										//Vector3 forwardpos = this.brain.transform.position + (this.brain.Navigator.BaseEntity.creatorEntity.transform.forward*5);
										Vector3 forwardpos = this.brain.transform.position + ((this.brain.Navigator.BaseEntity.creatorEntity as BasePlayer).eyes.HeadForward()*5);
										this.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																										
									}
								}
							}
							
						}
					}
					return base.StateThink(delta,brain,selfEntity);
				}	
				
			}
			class FactionBaseFollowPathState : BaseAIBrain.BasicAIState{
				public global::StateStatus status = global::StateStatus.Error;
				// Token: 0x040001DF RID: 479
				public global::AIMovePointPath path;
				// Token: 0x040001E1 RID: 481
				public global::AIMovePoint currentTargetPoint;
				// Token: 0x040001E2 RID: 482
				public float currentWaitTime;
				public Vector3 lastLocation = new Vector3(0,0,0);
				// Token: 0x040001E3 RID: 483
				public global::AIMovePointPath.PathDirection pathDirection;

				// Token: 0x040001E4 RID: 484
				public int currentNodeIndex;
				public FactionBaseFollowPathState() : base(AIState.FollowPath){
					pathDirection = (UnityEngine.Random.Range(0,1)==1?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards);
				}
				public override void StateEnter(BaseAIBrain brain, BaseEntity selfEntity){
					int i=0;
					this.currentWaitTime =0;
					
					if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
					{
						(this.brain.Navigator.BaseEntity as BasePlayer).Hurt(10f);
					}
					if(this.brain==null){
						return;
					}		
					if(this.brain.Navigator==null){
						return;}
					if(this.brain.Navigator.BaseEntity==null){
						return;
					}		
					lastLocation = this.brain.Navigator.BaseEntity.gameObject.transform.position;
					
					if(this.brain.Navigator.BaseEntity.creatorEntity){
						if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
							RadialPoint(this.brain.Navigator,this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);						
							return ;
						}
					}
					
					
					GridAgent ga = getGridAgent(this.brain);
					GridCell gc = new GridCell();
					foreach(MonumentCell monument in monuments){
						if(config.factionBank.ContainsKey((this.brain.Navigator.BaseEntity.faction))&&monument.lastTime + coinTick < UnityEngine.Time.realtimeSinceStartup && monument.position.x == ga.position.x && monument.position.z==ga.position.z){
								config.factionBank[this.brain.Navigator.BaseEntity.faction]+=10f;
							monument.lastTime= UnityEngine.Time.realtimeSinceStartup;
						}
						
					}
					OrderCell currentOrder = bestOrder(this.brain);
					
					if(currentOrder!= null){
						try{
							Vector3 position = gridToPos(currentOrder.position);
							RadialPoint(this.brain.Navigator,position,this.brain.transform.position,10,40);		
							return;
						}catch(Exception e){}
						
					}
					
					this.brain.Navigator.Path=null;
					
					RadialPoint(this.brain.Navigator, this.brain.transform.position+(this.brain.transform.forward*10),this.brain.transform.position,5,7);	
					return;
				
				}
				public override global::StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity selfEntity)
				{
					this.currentWaitTime += delta;
					if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
						if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
							if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 10f || this.currentWaitTime >= 60f)
							{//
								return global::StateStatus.Finished;
							}else{
								return global::StateStatus.Running;
							}
						}
					}
					if (this.currentWaitTime >= 15f|| ( this.currentWaitTime >= 5f && Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<0.5f))
					{//
				
						if (getGridAgent(this.brain)==null ||  getGridAgent(this.brain).currentTarget==null){
							RadialPoint(this.brain.Navigator, this.brain.transform.position+((UnityEngine.Random.Range(0.0f,1.0f)>0.5f?this.brain.transform.right:new Vector3(0,0,0)-this.brain.transform.right)*5),this.brain.transform.position);	
						}
						return global::StateStatus.Finished;
					}else{
						return global::StateStatus.Running;
					}
					
				}
				
				public override void StateLeave(BaseAIBrain brain, BaseEntity selfEntity)
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
				}
				
			}
			class FactionBaseRoamState : BaseAIBrain.BaseRoamState{
				public global::StateStatus status = global::StateStatus.Error;
				// Token: 0x040001DF RID: 479
				public global::AIMovePointPath path;
				// Token: 0x040001E1 RID: 481
				public global::AIMovePoint currentTargetPoint;
				// Token: 0x040001E2 RID: 482
				public float currentWaitTime;
				public Vector3 lastLocation = new Vector3(0,0,0);
				public float waitTime = 15;
				public float defaultWaitTime=15;
				public int stuckCount = 0;
				// Token: 0x040001E3 RID: 483
				public global::AIMovePointPath.PathDirection pathDirection;
				public List<AIInformationZone> visitedZones = new List<AIInformationZone>();
				
				public OrderCell currentOrder;
				public GridAgent ga;

				// Token: 0x040001E4 RID: 484
				public int currentNodeIndex;
				public FactionBaseRoamState() : base(){
					pathDirection = (UnityEngine.Random.Range(0,1)==1?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards);
				}
				public override void StateEnter(BaseAIBrain brain, BaseEntity selfEntity){
					int i=0;
					this.currentWaitTime =0;
					
					if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
					{
						(this.brain.Navigator.BaseEntity as BasePlayer).Hurt(10f);
					}
					if(this.brain==null){
						return;
					}		
					if(this.brain.Navigator==null){
						return;}
					if(this.brain.Navigator.BaseEntity==null){
						return;
					}		
					lastLocation = this.brain.Navigator.BaseEntity.gameObject.transform.position;
					
					if(this.brain.Navigator.BaseEntity.creatorEntity){
						if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
							RadialPoint(this.brain.Navigator,this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);						
							return ;
						}
					}
					
					
					GridAgent ga = getGridAgent(this.brain);
					GridCell gc = new GridCell();
					foreach(MonumentCell monument in monuments){
						if(!config.factionBank.ContainsKey((this.brain.Navigator.BaseEntity.faction))){
							config.factionBank.Add((this.brain.Navigator.BaseEntity.faction),0.0f);
						}
						if(config.factionBank.ContainsKey((this.brain.Navigator.BaseEntity.faction))&&monument.lastTime + coinTick < UnityEngine.Time.realtimeSinceStartup && Vector3.Distance(monument.info.gameObject.transform.position,this.brain.gameObject.transform.position)<50f){
							config.factionBank[this.brain.Navigator.BaseEntity.faction]+=10f;
							monument.lastTime= UnityEngine.Time.realtimeSinceStartup;
						}
						
					}
					OrderCell currentOrder = (stuckCount<5?bestOrder(this.brain):null);
					
					if(currentOrder!= null){
						try{
							MonumentCell max = null;
							foreach(MonumentCell mc in monuments){
								if(mc.position.x==currentOrder.position.x&&mc.position.z==currentOrder.position.z){
									max=mc;
								}
							}
							if(max!=null){
								Vector3 position = max.info.gameObject.transform.position;
								RadialPoint(this.brain.Navigator,position,this.brain.transform.position,7,40);		
							}else{
								Vector3 position = gridToPos(currentOrder.position);
								RadialPoint(this.brain.Navigator,position,this.brain.transform.position,10,40);		
								
							}
							return;
						}catch(Exception e){}
						
					}else{
						MonumentCell mc = joinMonument(this.brain.Navigator.BaseEntity.faction);
						if (mc!=null){
							
							this.brain.Navigator.SetFacingDirectionOverride(gridToPos(mc.position));
							this.brain.Navigator.ApplyFacingDirectionOverride();
							Vector3 direction = mc.position-this.brain.transform.position;
							Vector3 position = this.brain.transform.position+(direction.normalized*20) + new Vector3(UnityEngine.Random.Range(-5f,5f),0,UnityEngine.Random.Range(-5f,5f));
							position.y=TerrainMeta.HeightMap.GetHeight(position);
							if(waitTime==defaultWaitTime){RadialPoint(this.brain.Navigator,position ,this.brain.transform.position,5,7);}
							this.brain.Navigator.ClearFacingDirectionOverride();
							
						}
					}
					
					this.brain.Navigator.Path=null;
					
					if(waitTime==defaultWaitTime){RadialPoint(this.brain.Navigator, this.brain.transform.position+(this.brain.transform.forward*10),this.brain.transform.position,5,7);}
					return;
				
				}
				public override global::StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity selfEntity)
				{
					this.currentWaitTime += delta;
					if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
						if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
							if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 10f || this.currentWaitTime >= 60f)
							{//
								return global::StateStatus.Finished;
							}else{
								return global::StateStatus.Running;
							}
						}
					}
					if (this.currentWaitTime >= waitTime)
					{//
						if(Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<2){
							stuckCount++;
						};
						if ((getGridAgent(this.brain)==null ||  getGridAgent(this.brain).currentTarget==null)){
							RadialPoint(this.brain.Navigator, this.brain.transform.position+((UnityEngine.Random.Range(0.0f,1.0f)>0.5f?this.brain.transform.right:new Vector3(0,0,0)-this.brain.transform.right)*5),this.brain.transform.position);	
						}
						return global::StateStatus.Finished;
					}else{
						return global::StateStatus.Running;
					}
					
				}
				
				public override void StateLeave(BaseAIBrain brain, BaseEntity selfEntity)
				{ 
					waitTime=defaultWaitTime;
					this.brain.Navigator.ClearFacingDirectionOverride();
				}
				
			}
			class FactionTakeCoverState : ScientistBrain.TakeCoverState{
				private global::StateStatus status = global::StateStatus.Error;
				private global::BaseEntity coverFromEntity;
				public override void StateEnter(BaseAIBrain brain, BaseEntity selfEntity){
					
					base.StateEnter(brain, selfEntity);					
					if(this.brain==null){return;}
					if(this.brain.Navigator==null){return;}
					
					if(this.brain.Navigator.BaseEntity==null){
						return;
					}		
					if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
						return;
					}		
					
					
					GridAgent ga = getGridAgent(this.brain);
					
					HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
					if (!(hn.inventory == null || hn.inventory.containerBelt == null))
					{
						Item slot = hn.inventory.containerBelt.GetSlot(0);
						if (slot != null)
						{
							hn.UpdateActiveItem(hn.inventory.containerBelt.GetSlot(0).uid);
							BaseEntity heldEntity = slot.GetHeldEntity();
							if (heldEntity != null)
							{
								AttackEntity component = heldEntity.GetComponent<AttackEntity>();
								if (component != null)
								{
									component.TopUpAmmo();
								}
							}
						}
					}
					this.status = global::StateStatus.Running;
					if(!this.brain.Navigator.BaseEntity.creatorEntity==null){
						if(!this.brain.Navigator.BaseEntity.creatorEntity.transform==null){
							RadialPoint(this.brain.Navigator, this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);						
							return;
						}
					}
					global::HumanNPC entity = brain.Navigator.BaseEntity as HumanNPC;
					RadialPoint(this.brain.Navigator, this.brain.transform.position+(entity.LastAttackedDir*15)+new Vector3(UnityEngine.Random.Range(-5f,5f),0,UnityEngine.Random.Range(-5f,5f)),this.brain.transform.position);
					
					return;
				}
				public override void StateLeave(BaseAIBrain brain, BaseEntity selfEntity)
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
					ClearCoverPointUsage();
						base.StateLeave(brain, selfEntity);
						return;
					
				}
				private bool StartMovingToCover()
				{
					if (this.coverFromEntity == null)
					{
						return false;
					}
					global::HumanNPC entity = brain.Navigator.BaseEntity as HumanNPC;
					Vector3 hideFromPosition = this.coverFromEntity ? this.coverFromEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 30f);
					global::AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
					if (informationZone == null)
					{
						return false;
					}
					float minRange = (entity.SecondsSinceAttacked < 2f) ? 2f : 0f;
					float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
					global::AICoverPoint bestCoverPoint = informationZone.GetBestCoverPoint(entity.transform.position, hideFromPosition, minRange, bestCoverPointMaxDistance, entity, true);
					if (bestCoverPoint == null || Vector3.Distance(bestCoverPoint.transform.position,entity.transform.position) > 30)
					{
						return false;
					}
					Vector3 position = bestCoverPoint.transform.position;
					if (!this.brain.Navigator.SetDestination(position, global::BaseNavigator.NavigationSpeed.Fast, 0f, 0f))
					{
						return false;
					}
					this.FaceCoverFromEntity();
					this.brain.Events.Memory.AIPoint.Set(bestCoverPoint, 4);
					bestCoverPoint.SetUsedBy(entity);
					return true;
				}
				private void ClearCoverPointUsage()
				{
					global::AIPoint aipoint = this.brain.Events.Memory.AIPoint.Get(4);
					if (aipoint != null)
					{
						aipoint.ClearIfUsedBy(brain.Navigator.BaseEntity);
				
					}
				}
				private void FaceCoverFromEntity()
				{
					this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if (this.coverFromEntity == null)
					{
						return;
					}
					this.brain.Navigator.SetFacingDirectionEntity(this.coverFromEntity);
				}
				
			}		
			class FactionChaseState : global::BaseAIBrain.BaseChaseState
			{
				Vector3 LastVector = new Vector3(0,-501,0);
				Vector3 Target = new Vector3(0,0,0);
				Vector3 Root = new Vector3(0,0,0);
				Vector3 Direction = new Vector3(0,0,0);
				float LastWet = 0f;
				float startTime = 0;
				bool TurnDirection = false;
				bool hasWarped = false;
				BaseNavigator.NavigationSpeed Speed = BaseNavigator.NavigationSpeed.Normal;
				public FactionChaseState() : base()
				{
					base.AgrresiveState = true;
					TurnDirection = (Oxide.Core.Random.Range(0, 2) == 1);
					startTime = Time.realtimeSinceStartup;
				}

				public override void StateLeave(BaseAIBrain brain, BaseEntity selfEntity)
				{
					base.StateLeave(brain, selfEntity);
							
					this.Stop();
				}

				public override void StateEnter(BaseAIBrain brain, BaseEntity selfEntity)
				{
					base.StateEnter(brain, selfEntity);
					if(this.brain==null){return;}
					if(this.brain.Navigator==null){return;}
					if(this.brain.Navigator.BaseEntity==null){
						return;
					}		
					if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
						return;
					}		
					
					
					GridAgent ga = getGridAgent(this.brain);
					HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
					if (!(hn.inventory == null || hn.inventory.containerBelt == null))
					{
						Item slot = hn.inventory.containerBelt.GetSlot(0);
						if (slot != null)
						{
							hn.UpdateActiveItem(hn.inventory.containerBelt.GetSlot(0).uid);
							BaseEntity heldEntity = slot.GetHeldEntity();
							if (heldEntity != null)
							{
								AttackEntity component = heldEntity.GetComponent<AttackEntity>();
								if (component != null)
								{
									component.TopUpAmmo();
								}
							}
						}
					}
					
				
					this.status = global::StateStatus.Error;
					if (this.brain.PathFinder == null)
					{
						return;
					}
							
					if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
					{
						(this.brain.Navigator.BaseEntity as BasePlayer).Hurt(10f);
					}
					this.status = global::StateStatus.Running;
					this.nextPositionUpdateTime = 0f;
					global::BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if (baseEntity != null)
					{
						this.brain.Navigator.SetDestination(this.brain.PathFinder.GetRandomPositionAround(baseEntity.transform.position,5f, 10f), global::BaseNavigator.NavigationSpeed.Fast, 0.25f, 0f);
					}
				}

				private void Stop()
				{
					try{
					this.brain.Navigator.Stop();
					this.brain.Navigator.ClearFacingDirectionOverride();
					}catch(Exception E){
						UnityEngine.Debug.Log(this.brain.Navigator.CurrentNavigationType.ToString());
					}
				}

				public override global::StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity selfEntity)
				{
					global::BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if (baseEntity == null)
					{
						return global::StateStatus.Error;
					}
					global::HumanNPC entity = brain.Navigator.BaseEntity as HumanNPC;
					float num = Vector3.Distance(baseEntity.transform.position, entity.transform.position);
					if (this.brain.Senses.Memory.IsLOS(baseEntity) || num <= 10f || base.TimeInState <= 5f)
					{
						this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
					}
					else
					{
						this.brain.Navigator.ClearFacingDirectionOverride();
					}
					if (num <= 10f)
					{
						this.brain.Navigator.SetCurrentSpeed(global::BaseNavigator.NavigationSpeed.Normal);
					}
					else
					{
						this.brain.Navigator.SetCurrentSpeed(global::BaseNavigator.NavigationSpeed.Fast);
					}
					if (Time.time > this.nextPositionUpdateTime)
					{
						this.nextPositionUpdateTime = Time.time + UnityEngine.Random.Range(0.5f, 1f);			
						
						this.status = global::StateStatus.Running;
						float currentWet = WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false);
						if(currentWet > 0f)	{
							if (currentWet > 0.8f){
									(this.brain.Navigator.BaseEntity as BasePlayer).Hurt(10f);
							}
							Root = LastVector;
							Direction = (TurnDirection?this.brain.transform.right:(-1*this.brain.transform.right));
							Target = this.brain.PathFinder.GetRandomPositionAround(Root-(this.brain.transform.forward*3)+(Direction*3),7f, 10f);
							if(Vector3.Distance(Target,this.brain.transform.position)<5)
								Speed = BaseNavigator.NavigationSpeed.Slow;
							if(Vector3.Distance(Target,this.brain.transform.position)>15)
								Speed = BaseNavigator.NavigationSpeed.Fast;
							
							if (this.brain.Navigator.SetDestination(Target, Speed, 0f, 0f))
							{
								this.status = global::StateStatus.Running;
								return this.status;
							}
							else{
								status = global::StateStatus.Error;
								if(!hasWarped){
									this.brain.Navigator.Warp(this.brain.transform.position);
									hasWarped=true;
								}
							}
						
						}else{
							LastWet=currentWet;
							LastVector=this.brain.transform.position;				
						}
					}
					if (this.brain.Navigator.Moving)
					{
						return global::StateStatus.Running;
					}
					return global::StateStatus.Finished;
					
					
				}

				private global::StateStatus status = global::StateStatus.Error;

				private float nextPositionUpdateTime;
			}
			#endregion
		#endregion classes
	#region variables
		private static Dictionary<BaseAIBrain, float> waitTimes = new Dictionary<BaseAIBrain, float>();
		
		static float SwapWanderRate = 0.1f;
		static int popLimit = 100;
		public int spawnCost = 1000;
		public int spawnerCost = 500;
		public int pointCost = 250;
		public int spawnThreshold = 2000;
		public float spawnTimeout = 300;
		public float roamChance = 0.7f;
		public static int coinTick = 300;
		public static Dictionary<BaseAIBrain,GridAgent> gridAgents = new Dictionary<BaseAIBrain,GridAgent>();
		public static List<MonumentCell> monuments = new List<MonumentCell>();
		public static  Dictionary<BaseCombatEntity.Faction,List<OrderCell>> orders = new Dictionary<BaseCombatEntity.Faction,List<OrderCell>>();
		List<PatrolHelicopterAI> AIHelis = new List<PatrolHelicopterAI>();
		static float gridAdjustmentX = 0-TerrainMeta.Position.x;
		static float gridAdjustmentZ = 0-TerrainMeta.Position.z;
		static byte[,,] cellGrid;
		
		static byte[,,] cellBucket;
		Cordyceps cordy;
			
		#endregion vaiables
	#region plugin initialization		
		void Loaded(){
			
			cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
			cordy.WalkableOnly = false;
			loadStates();
			
			Subscribe(nameof(CanFactionBuild));
			
			Subscribe(nameof(addOrderExternal));
			Subscribe(nameof(getGrid));
			Subscribe(nameof(getBalance));
			Subscribe(nameof(getAlignments));
			Subscribe(nameof(spawnOnMonument));
			LoadConfig();
			
		}
		void Init(){
				permission.RegisterPermission("factionsystem.squad", this);
				permission.RegisterPermission("factionsystem.command", this);
				permission.RegisterPermission("factionsystem.zoning", this);
			}
		void Unload(){
			SaveConfig();
			foreach(InvisibleVendingMachine s in GameObject.FindObjectsOfType<InvisibleVendingMachine>()){
				if(s.transform.parent!=null){
					HumanNPC hn = s.transform.parent.gameObject.GetComponent<HumanNPC>();
					if(hn!=null || s.shopName.Contains("[NPCShop]")){
						s.Kill();
					}
				}
			}
		}
		void OnServerInitialized(){
			foreach(InvisibleVendingMachine s in GameObject.FindObjectsOfType<InvisibleVendingMachine>()){
				if(s.transform.parent!=null ){
					HumanNPC hn = s.transform.parent.gameObject.GetComponent<HumanNPC>();
					if(hn!=null){
						hn.gameObject.GetComponentInChildren<InvisibleVendingMachine>().Kill();
						AddVending(hn, new Vector3(0,0,0));
					}
				}else if(s.transform.name=="NPC Shop"|| (s.vendingOrders == null)){
					s.Kill();
				}
			}
			foreach(GroundWatch s in GameObject.FindObjectsOfType<GroundWatch>()){
				GameObject.Destroy(s);
			}
			timer.Every(spawnTimeout,()=>{spawnOnJunkpile(BaseCombatEntity.Faction.Bandit);});
			timer.Every(spawnTimeout,()=>{spawnOnJunkpile(BaseCombatEntity.Faction.Scientist);});
			timer.Every(spawnTimeout,()=>{spawnOnJunkpile(BaseCombatEntity.Faction.Player);});
			timer.Every(600f,()=>{pickTargets();});
			
		}
			
		#endregion
	#region AI init
		void loadStates(){
			string[] targets = new string[]{"assets/rust.ai/agents/npcplayer/humannpc/banditguard/npc_bandit_guard.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_mp5.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_pistol.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_shotgun.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_peacekeeper.prefab", 
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab"};
			foreach(string s in targets){
				FactionCombatState fcs= new FactionCombatState();
				FactionCombatStationaryState fcss= new FactionCombatStationaryState();
				FactionBaseFollowPathState fbfps= new FactionBaseFollowPathState();
				FactionBaseRoamState fbrs= new FactionBaseRoamState();
				FactionTakeCoverState ftcs= new FactionTakeCoverState();
				FactionChaseState fchase= new FactionChaseState();
				cordy.AssignHumanState(s, fcs.StateType, fcs);
				cordy.AssignHumanState(s, fcss.StateType, fcss);
				cordy.AssignHumanState(s, fbfps.StateType, fbfps);
				cordy.AssignHumanState(s, fbrs.StateType, fbrs);
				cordy.AssignHumanState(s, fchase.StateType, fchase);
			}
			targets = new string[]{"assets/rust.ai/agents/bear/bear.prefab", "assets/rust.ai/agents/bear/bear.prefab", 
			"assets/rust.ai/agents/boar/boar.prefab",      
			"assets/rust.ai/agents/wolf/wolf.prefab",      
			"assets/rust.ai/agents/stag/stag.prefab"
			};
			foreach(string s in targets){
				FactionAnimalIdleState fcs= new FactionAnimalIdleState();
				FactionAnimalRoamState fcss= new FactionAnimalRoamState();
				FactionAnimalChaseState fbfps= new FactionAnimalChaseState();
				FactionAnimalAttackState fbas= new FactionAnimalAttackState();
				FactionAnimalFleeState fbfs= new FactionAnimalFleeState();
				cordy.AssignAnimalState(s, fcs.StateType, fcs);
				cordy.AssignAnimalState(s, fcss.StateType, fcss);
				cordy.AssignAnimalState(s, fbfps.StateType, fbfps);
				cordy.AssignAnimalState(s, fbas.StateType, fbas);
				cordy.AssignAnimalState(s, fbfs.StateType, fbfs);
			}
			
		}
		void initAnimal(BaseAnimalNPC animal){
			if(animal.IsNpc)Puts(((char)27)+"[96m"+"IsNpc! Did you fix NPCPlayer with dnSpy?"+ (animal.transform.name));
			if(animal.transform.name.Contains("bear") ||animal.transform.name.Contains("boar") ||animal.transform.name.Contains("wolf") ||animal.transform.name.Contains("deer")|| animal.transform.name.Contains("shark")){
				animal.faction = BaseCombatEntity.Faction.Player;	
			}
		}
		bool swapSciRoamState(BasePlayer s){
			s.HasBrain = true;
			if(s.transform == null)return false;
			if(s.IsNpc)Puts(((char)27)+"[96m"+"IsNpc! Did you fix NPCPlayer with dnSpy?" + (s.transform.name));
			if(s.transform.name.ToLower().Contains("scientist")||s.transform.name.ToLower().Contains("apc")||s.transform.name.ToLower().Contains("bradley")) { 
				s.faction = BaseCombatEntity.Faction.Scientist;
				if(s.transform.name.ToLower().Contains("pacifist")){
					s.InitializeHealth(100,100);
				}
			}else if(s.transform.name.ToLower().Contains("bandit")) {
				s.faction = BaseCombatEntity.Faction.Bandit;
				s.InitializeHealth(100,100);
			}else  if(s.transform.name.ToLower().Contains("dweller")) {
				s.faction = BaseCombatEntity.Faction.Player;
			}
			
			try{
				
				HumanNPC hn = ((HumanNPC)s);
				AddVending(hn,new Vector3(0,0,0));
				hn.Brain.Navigator.defaultAreaMask = UnityEngine.AI.NavMesh.AllAreas;
				hn.Brain.Navigator.navMeshQueryFilter.areaMask= UnityEngine.AI.NavMesh.AllAreas;
				if(hn.Brain==null) return false;
				hn.Brain.SenseTypes = (EntityType)67;
				hn.Brain.HostileTargetsOnly = false;
				hn.Brain.CheckVisionCone=true;
				hn.Brain.CheckLOS=true;
				hn.Brain.SenseRange=35f;
				hn.Brain.Senses.senseTypes = (EntityType)67;
				hn.Brain.Senses.hostileTargetsOnly = false;
				hn.Brain.Senses.checkVision=true;
				hn.Brain.Senses.checkLOS=true;
				hn.Brain.Senses.maxRange=35f;
				if(hn.Brain.GetComponent<BaseNavigator>()!=null){
					hn.Brain.GetComponent<BaseNavigator>().StoppingDistance=1f;
					hn.Brain.Navigator.Path = null;
				}
				if(hn.Brain.CurrentState is FactionBaseFollowPathState){
					((FactionBaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
				}
				if(hn.Brain.CurrentState is FactionBaseRoamState){
					((FactionBaseRoamState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
				}
				float terraingDiff = hn.transform.position.y - Terrain.activeTerrain.SampleHeight(hn.transform.position);
					
				((IAISleepable)hn.Brain).WakeAI();
				return true;////
			}catch(Exception e){
				Puts(e.ToString());
				}
				return true;////
		}		
		bool swapTank(BradleyAPC s){	
			s.faction = BaseCombatEntity.Faction.Scientist;	
			return true;////			
		}
		void initPlayer(BasePlayer player){
			if(player.IsConnected){
				changeScore(player, BaseCombatEntity.Faction.Player, 0.00001f);
				player.faction=getNativeFaction(player);
				GenerateFactionLogo(player);					
				foreach(MapMarkerGenericRadius fc in GameObject.FindObjectsOfType<MapMarkerGenericRadius>()){
					fc.SendUpdate();
				}

			}
		}
		void OnPlayerRespawned(BasePlayer player)=>initPlayer(player);
		void OnPlayerSleepEnded(BasePlayer player)=>initPlayer(player);
		object OnInitializeAI(ScientistBrain player){swapSciRoamState(player.Navigator.BaseEntity as HumanNPC); return null;}
		object OnInitializeAI(AnimalBrain player){initAnimal(player.Navigator.BaseEntity as BaseAnimalNPC); return null;}
		object OnBradleyApcInitialize(BradleyAPC apc){swapTank((apc));return null;}
		void OnEntitySpawned(BaseNetworkable entity){
			if(entity is ResourceEntity){
				if(entity.gameObject.GetComponent<NavMeshObstacle>()==null){
					NavMeshObstacle nmo = entity.gameObject.AddComponent<NavMeshObstacle>();
					nmo.carving=true;
				}
				
			}else if(entity is HumanNPC && !entity.transform.name.ToLower().Contains("ch47_gunner")){
				(entity as HumanNPC).GetComponent<BaseNavigator>().defaultAreaMask = UnityEngine.AI.NavMesh.AllAreas;
				(entity as HumanNPC).GetComponent<BaseNavigator>().navMeshQueryFilter.areaMask= UnityEngine.AI.NavMesh.AllAreas;
				int oldID = (entity as HumanNPC).GetComponent<BaseNavigator>().Agent.agentTypeID;
				(entity as HumanNPC).GetComponent<BaseNavigator>().Agent.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
				
				Vector3 position = 	entity.transform.position;
				if(position.y>TerrainMeta.HeightMap.GetHeight(position) && WaterLevel.GetOverallWaterDepth(position, true, null, false) < 0.1f && !entity.transform.name.ToLower().Contains("oilrig") && !entity.transform.name.ToLower().Contains("cargo") ){					
					RadialPoint(out position,position+new Vector3(UnityEngine.Random.Range(-1f,1f),0,UnityEngine.Random.Range(-1f,1f)),position,50,100);
				}
				
				if(!(entity as HumanNPC).GetComponent<BaseNavigator>().Warp(position)){						
					(entity as HumanNPC).GetComponent<BaseNavigator>().Agent.agentTypeID=oldID;
				}
				if(!(entity as HumanNPC).GetComponent<BaseNavigator>().Warp(position)){	
					Puts(((char)27)+"[94m"+"Killing "+entity.transform.name);//
					entity.Kill();
				}
			}else if(entity is BaseAnimalNPC){
				(entity as BaseAnimalNPC).GetComponent<BaseNavigator>().defaultAreaMask = UnityEngine.AI.NavMesh.AllAreas;
				(entity as BaseAnimalNPC).GetComponent<BaseNavigator>().navMeshQueryFilter.areaMask= UnityEngine.AI.NavMesh.AllAreas;
				int oldID = (entity as BaseAnimalNPC).GetComponent<BaseNavigator>().Agent.agentTypeID;
				(entity as BaseAnimalNPC).GetComponent<BaseNavigator>().Agent.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
				Vector3 position = 	entity.transform.position;
				position.y = (position.y>TerrainMeta.HeightMap.GetHeight(position) && WaterLevel.GetOverallWaterDepth(position, true, null, false) < 0.1f?TerrainMeta.HeightMap.GetHeight(position):position.y);
				if(!(entity as BaseAnimalNPC).GetComponent<BaseNavigator>().Warp(position)){						
					(entity as BaseAnimalNPC).GetComponent<BaseNavigator>().Agent.agentTypeID=oldID;
				}
				if(!(entity as BaseAnimalNPC).GetComponent<BaseNavigator>().Warp(position)){	
					Puts(entity.transform.name);
					entity.Kill();
				}
				
			}else if(entity is NPCAutoTurret){
				if(entity.name.Contains("bandit")){
					(entity as NPCAutoTurret).faction = BaseCombatEntity.Faction.Bandit;
				}else if(entity.name.Contains("scientist")){
					(entity as NPCAutoTurret).faction = BaseCombatEntity.Faction.Scientist;
				}
			}else if(entity is AutoTurret){
				
			}
			if(entity.transform.name.ToLower().Contains("patrol helicopter/patrolhelicopter")){
				PatrolHelicopterAI phai = entity.GetComponent<PatrolHelicopterAI>();
				BaseHelicopter heli = entity as BaseHelicopter;
				if(heli!=null){heli.faction=BaseCombatEntity.Faction.Scientist;}
				if(phai!=null && !AIHelis.Contains(phai)){
					AIHelis.Add(phai);
				}
			}
			if (entity.transform.name.ToLower().Contains("ch47_gunner")){
				entity.GetComponent<BaseNavigator>().SetCurrentNavigationType(BaseNavigator.NavigationType.None);
				entity.GetComponent<BaseNavigator>().CanUseNavMesh=false;
			}
		}
		object OnPlayerDeath(BasePlayer player, HitInfo info){return null;}
		object OnDoRespawn(Oxide.Ext.RustEdit.NPC.NPCSpawner npcSpawner){//*/){
			
			return null;
		}
		#endregion
	#region Grid
		private void pickTargets(){
			Puts("Picking auto-target");
			int leftEdge = (int)(0+gridAdjustmentX)/100;
			int topEdge = (int)(0+gridAdjustmentZ)/100;
			int width=(int)(System.Math.Abs(leftEdge)*2);
			int height=(int)(System.Math.Abs(topEdge)*2);
			if(orders.ContainsKey(BaseCombatEntity.Faction.Bandit) ){
				int need = 0;
				foreach(OrderCell oc in orders[BaseCombatEntity.Faction.Bandit]){
					need+=oc.targetCount-oc.assignedAgents.Count();
				}
				if(orders[BaseCombatEntity.Faction.Bandit].Count()==0||need==0){
					MonumentInfo mm = ((MonumentCell)(Shuffle(monuments.ToArray(),0)[0])).info;
					Vector3Int gridPos = (Vector3Int)posToGrid(mm.transform.position);
					if(gridPos.x>0 && gridPos.z>0 && gridPos.x<width && gridPos.z<height && mm.transform!=null  && !mm.displayPhrase.translated.ToLower().Contains("underwater") && !mm.displayPhrase.translated.ToLower().Contains("lighthouse") && !mm.displayPhrase.translated.ToLower().Contains("fishing")){
						addOrderExternal(BaseCombatEntity.Faction.Bandit, gridPos, UnityEngine.Random.Range(4,8),UnityEngine.Random.Range(600,1800));
					}
					
				}
			}else{
				orders.Add(BaseCombatEntity.Faction.Bandit,new List<OrderCell>());
			}
			
			if(orders.ContainsKey(BaseCombatEntity.Faction.Scientist) ){
					int need = 0;
					foreach(OrderCell oc in orders[BaseCombatEntity.Faction.Scientist]){
						need+=oc.targetCount-oc.assignedAgents.Count();
					}
					if(orders[BaseCombatEntity.Faction.Scientist].Count()==0||need==0){
					MonumentInfo mm = ((MonumentCell)(Shuffle(monuments.ToArray(),0)[0])).info;
					Vector3Int gridPos = (Vector3Int)posToGrid(mm.transform.position);
					if(gridPos.x>0 && gridPos.z>0 && gridPos.x<width && gridPos.z<height && mm.transform!=null  && !mm.displayPhrase.translated.ToLower().Contains("underwater") && !mm.displayPhrase.translated.ToLower().Contains("lighthouse") && !mm.displayPhrase.translated.ToLower().Contains("fishing")){
						addOrderExternal(BaseCombatEntity.Faction.Scientist, gridPos, UnityEngine.Random.Range(4,8),UnityEngine.Random.Range(600,1800));
					}
					
				}
			}else{
				orders.Add(BaseCombatEntity.Faction.Scientist,new List<OrderCell>());
			}
			
			if(orders.ContainsKey(BaseCombatEntity.Faction.Player) ){
					int need = 0;
					foreach(OrderCell oc in orders[BaseCombatEntity.Faction.Player]){
						need+=oc.targetCount-oc.assignedAgents.Count();
					}
					if(orders[BaseCombatEntity.Faction.Player].Count()==0||need==0){
					MonumentInfo mm = ((MonumentCell)(Shuffle(monuments.ToArray(),0)[0])).info;
					Vector3Int gridPos = (Vector3Int)posToGrid(mm.transform.position);
					if(gridPos.x>0 && gridPos.z>0 && gridPos.x<width && gridPos.z<height && mm.transform!=null  && !mm.displayPhrase.translated.ToLower().Contains("underwater") && !mm.displayPhrase.translated.ToLower().Contains("lighthouse") && !mm.displayPhrase.translated.ToLower().Contains("fishing")){
						addOrderExternal(BaseCombatEntity.Faction.Player, gridPos, UnityEngine.Random.Range(4,8),UnityEngine.Random.Range(600,1800));
					}
					
				}
			}else{
				orders.Add(BaseCombatEntity.Faction.Player,new List<OrderCell>());
			}
		
		}
		
		public static MonumentCell nearestMonument(Vector3Int position){
		
			MonumentCell max = monuments[0];
			foreach(MonumentCell mc in monuments){
				if(Vector3Int.Distance(mc.position, position)<Vector3Int.Distance(max.position,position)){
					max=mc;
				}
			}
			return max;
		}
		private void spawnOnJunkpile(BaseCombatEntity.Faction faction){
			JunkPile[] jps = GameObject.FindObjectsOfType<JunkPile>();
			if(jps.Length<2){
				Puts("Not enough junkpiles");
				return;
				}
			Shuffle(jps,0);
			int i=1;
			JunkPile target = jps[0];
			while(target==jps[0] && i < jps.Length){
				Puts(jps[i].gameObject.transform.name);
				if(jps[i].gameObject.transform.name.Contains("junkpile_") && !jps[i].gameObject.transform.name.Contains("water")){
					target=jps[i];
				}else{i++;}
			}
			if(target!=jps[0]){
				if(!config.factionBank.ContainsKey(faction))
					config.factionBank[faction]=0;
				if(config.factionBank[faction]>spawnThreshold && (Resources.FindObjectsOfTypeAll((faction==BaseCombatEntity.Faction.Bandit?typeof(BanditGuard):(faction==BaseCombatEntity.Faction.Scientist?typeof(ScientistNPC):typeof(TunnelDweller)))).Length < popLimit)){
						if(config.factionBank[faction]>spawnThreshold){
							Vector3 position = target.gameObject.transform.position;
							position.x += (UnityEngine.Random.Range(0.0f,1.0f)>0.5f?-1:1)*UnityEngine.Random.Range(30,50);
							position.z += (UnityEngine.Random.Range(0.0f,1.0f)>0.5f?-1:1)*UnityEngine.Random.Range(30,50);
							position.y=TerrainMeta.HeightMap.GetHeight(position);
							config.factionBank[faction]-=spawnCost;
							//UnityEngine.Debug.Log(max.name+":"+position.ToString());
							ConVar.Entity.svspawn(
							(faction==BaseCombatEntity.Faction.Bandit?"it_g":(faction==BaseCombatEntity.Faction.Scientist?"scientistnpc_roam":"npc_tunneldweller")),position,new Vector3(0,0,0));
						}
					}
			}else{Puts("No good junkpile "+ jps.Length.ToString());}
		}
		
		private static MonumentCell joinMonument(BaseCombatEntity.Faction faction){
			MonumentCell max = new MonumentCell();
			int self = (faction==BaseCombatEntity.Faction.Bandit?1:2);
			int enemy = (faction==BaseCombatEntity.Faction.Bandit?2:1);
			foreach(MonumentCell mc in monuments){
				//UnityEngine.Debug.Log(cellBucket[mc.position.x,mc.position.z,1].ToString()+":"+cellBucket[mc.position.x,mc.position.z,2].ToString()+":"+cellBucket[max.position.x,max.position.z,2].ToString());
				if(max.name==null||max.name==""|| (cellBucket[mc.position.x,mc.position.z,enemy]==0 && cellBucket[mc.position.x,mc.position.z,self]>0 && cellBucket[mc.position.x,mc.position.z,self]> cellBucket[max.position.x,max.position.z,self])){
					Vector3 v = gridToPos(mc.position);
					v.y = TerrainMeta.HeightMap.GetHeight(v);
					//UnityEngine.Debug.Log(mc.name+":"+v.ToString()+":"+(WaterLevel.GetOverallWaterDepth(v, true, null, false)).ToString()+":"+(WaterLevel.GetOverallWaterDepth(v, true, null, false)<1f).ToString());
					if(WaterLevel.GetOverallWaterDepth(v, true, null, false)<1f){
						max=mc;
					}
				}
			}
			if(max.name!=null&&max.name!=""){
				return max;
			}
			return null;
		}
		
		private void spawnOnMonument(BaseCombatEntity.Faction faction){
			if(faction == BaseCombatEntity.Faction.Bandit){
				MonumentCell max = new MonumentCell();
				foreach(MonumentCell mc in monuments){
					//UnityEngine.Debug.Log(cellBucket[mc.position.x,mc.position.z,1].ToString()+":"+cellBucket[mc.position.x,mc.position.z,2].ToString()+":"+cellBucket[max.position.x,max.position.z,2].ToString());
					if(max.name==null||max.name==""|| (cellBucket[mc.position.x,mc.position.z,1]==0 && cellBucket[mc.position.x,mc.position.z,2]>0 && cellBucket[mc.position.x,mc.position.z,2]> cellBucket[max.position.x,max.position.z,2])){
						Vector3 v = gridToPos(mc.position);
						v.y = TerrainMeta.HeightMap.GetHeight(v);
						//UnityEngine.Debug.Log(mc.name+":"+v.ToString()+":"+(WaterLevel.GetOverallWaterDepth(v, true, null, false)).ToString()+":"+(WaterLevel.GetOverallWaterDepth(v, true, null, false)<1f).ToString());
						if(WaterLevel.GetOverallWaterDepth(v, true, null, false)<1f){
							max=mc;
						}
					}
				}
				if(max.name!=null&&max.name!=""){
					if(!config.factionBank.ContainsKey(BaseCombatEntity.Faction.Bandit))
						config.factionBank[BaseCombatEntity.Faction.Bandit]=0;
					if(config.factionBank[BaseCombatEntity.Faction.Bandit]>spawnThreshold && (Resources.FindObjectsOfTypeAll(typeof(BanditGuard)).Length < popLimit)){
						Vector3 position = gridToPos(max.position);
						position.x += (UnityEngine.Random.Range(0.0f,1.0f)>0.5f?-1:1)*UnityEngine.Random.Range(30,50);
						position.z += (UnityEngine.Random.Range(0.0f,1.0f)>0.5f?-1:1)*UnityEngine.Random.Range(30,50);
						position.y=TerrainMeta.HeightMap.GetHeight(position);
						config.factionBank[BaseCombatEntity.Faction.Bandit]-=spawnCost;
						//UnityEngine.Debug.Log(max.name+":"+position.ToString());
						ConVar.Entity.svspawn("it_g",position,new Vector3(0,0,0));
					}
				}
			}
			else if(faction == BaseCombatEntity.Faction.Scientist){
				MonumentCell max = new MonumentCell();
				foreach(MonumentCell mc in monuments){
					//UnityEngine.Debug.Log(cellBucket[mc.position.x,mc.position.z,1].ToString()+":"+cellBucket[mc.position.x,mc.position.z,2].ToString()+":"+cellBucket[max.position.x,max.position.z,2].ToString());
					if(max.name==null||max.name==""|| (cellBucket[mc.position.x,mc.position.z,2]==0 && cellBucket[mc.position.x,mc.position.z,1]>0 && cellBucket[mc.position.x,mc.position.z,1]> cellBucket[max.position.x,max.position.z,1])){
						Vector3 v = gridToPos(mc.position);
						v.y = TerrainMeta.HeightMap.GetHeight(v);
						//UnityEngine.Debug.Log(mc.name+":"+v.ToString()+":"+(WaterLevel.GetOverallWaterDepth(v, true, null, false)).ToString()+":"+(WaterLevel.GetOverallWaterDepth(v, true, null, false)<1f).ToString());
						if(WaterLevel.GetOverallWaterDepth(v, true, null, false)<1f){
							max=mc;
						}
					}
				}
				if(max.name!=null&&max.name!=""){
					if(!config.factionBank.ContainsKey(BaseCombatEntity.Faction.Scientist))
						config.factionBank[BaseCombatEntity.Faction.Scientist]=0;
					if(config.factionBank[BaseCombatEntity.Faction.Scientist]>spawnThreshold && (Resources.FindObjectsOfTypeAll(typeof(ScientistNPC)).Length < popLimit)){
						Vector3 position = gridToPos(max.position);
						position.x += (UnityEngine.Random.Range(0.0f,1.0f)>0.5f?-1:1)*UnityEngine.Random.Range(30,50);
						position.z += (UnityEngine.Random.Range(0.0f,1.0f)>0.5f?-1:1)*UnityEngine.Random.Range(30,50);
						position.y=TerrainMeta.HeightMap.GetHeight(position);
						config.factionBank[BaseCombatEntity.Faction.Scientist]-=spawnCost;
						//UnityEngine.Debug.Log(max.name+":"+position.ToString());
						ConVar.Entity.svspawn("scientistnpc_roam",position,new Vector3(0,0,0));
					}
				}
			}
		}
		private void initGrid(){
			
			gridAdjustmentX = 0-TerrainMeta.Position.x;
			gridAdjustmentZ = 0-TerrainMeta.Position.z;
			int leftEdge = (int)(0+gridAdjustmentX)/100;
			int topEdge = (int)(0+gridAdjustmentZ)/100;
			int width=(int)(System.Math.Abs(leftEdge)*2);
			int height=(int)(System.Math.Abs(topEdge)*2);
			cellGrid = new byte[width,height,9];
			for(int i = 0; i< width;i++){
				for(int j = 0; j< height;j++){
					GridCell gc = new GridCell();
					Vector3 realPos =gridToPos(new Vector3(i,0,j));
					float newY =TerrainMeta.HeightMap.GetHeight(realPos);
					realPos.y = newY;
					gc.isWater = (WaterLevel.GetOverallWaterDepth(realPos, true, null, false) > 0);
					cellGrid[i,j,0]=gc.getFlags();
				}	
			}
			foreach(MonumentInfo mm in Resources.FindObjectsOfTypeAll(typeof(MonumentInfo))){
				Vector3Int gridPos = posToGrid(mm.transform.position);
				if(gridPos.x>0 && gridPos.z>0 && gridPos.x<width && gridPos.z<height && !(mm is DungeonBaseLandmarkInfo) && mm.MapLayer != null && (int)mm.MapLayer == -1 && mm.shouldDisplayOnMap && mm.gameObject.activeInHierarchy  && mm.transform!=null  && !mm.displayPhrase.translated.ToLower().Contains("underwater")){
					MonumentCell mc = new MonumentCell();
					mc.position = new Vector3Int(gridPos.x,0,gridPos.z);
					mc.name = mm.displayPhrase.translated;
					mc.info=mm;
					monuments.Add(mc);
					GridCell gc = new GridCell();
					gc.setFlags(cellGrid[gridPos.x,gridPos.z,0]);
					gc.isMonument = true;
					cellGrid[gridPos.x,gridPos.z,0]=gc.getFlags();
				}
			}
		}
		private byte[,,] getGrid(){
			if(cellGrid==null||cellGrid.Length==0)initGrid();
			cellBucket = (byte[,,])cellGrid.Clone();
			gridAdjustmentX = 0-TerrainMeta.Position.x;
			gridAdjustmentZ = 0-TerrainMeta.Position.z;
			int leftEdge = (int)(0+gridAdjustmentX)/100;
			int topEdge = (int)(0+gridAdjustmentZ)/100;
			int width=(int)(System.Math.Abs(leftEdge)*2);
			int height=(int)(System.Math.Abs(topEdge)*2);
			foreach(BaseAIBrain mm in gridAgents.Keys){
				if(mm!=null){
					Vector3Int gridPos = posToGrid(mm.transform.position);
					
					if(gridPos.x>0 && gridPos.z>0 && gridPos.x<width && gridPos.z<height && (mm.Navigator.BaseEntity is HumanNPC)){
						GridCell gc = new GridCell();
						gc.setFlags(cellBucket[gridPos.x,gridPos.z,0]);
						if((mm.Navigator.BaseEntity as HumanNPC).faction==BaseCombatEntity.Faction.Bandit){
							
							gc.isBanAggro= gc.isBanAggro||mm.CurrentState.AgrresiveState;
							cellBucket[gridPos.x,gridPos.z,2]+=1;
						}
						if((mm.Navigator.BaseEntity as HumanNPC).faction==BaseCombatEntity.Faction.Scientist){
							
							gc.isSciAggro= gc.isSciAggro||mm.CurrentState.AgrresiveState;
							cellBucket[gridPos.x,gridPos.z,1]+=1;
						}
						cellBucket[gridPos.x,gridPos.z,0]=gc.getFlags();
					}
				}
			}
			if(orders.ContainsKey(BaseCombatEntity.Faction.Bandit)){
				OrderCell[] bucket = (orders.ContainsKey(BaseCombatEntity.Faction.Bandit)?orders[BaseCombatEntity.Faction.Bandit].ToArray():new OrderCell[0]);
				foreach(OrderCell mm in bucket){
					if(mm!=null && mm.timeOut< UnityEngine.Time.realtimeSinceStartup){
						if(orders[BaseCombatEntity.Faction.Bandit].Contains(mm)){
							orders[BaseCombatEntity.Faction.Bandit].Remove(mm);
						}
						if(mm.assignedAgents!=null){
							mm.assignedAgents.Clear();
						}
					}
				}
				foreach(OrderCell mm in orders[BaseCombatEntity.Faction.Bandit]){
						GridCell gc = new GridCell();
						byte[] y = new byte[9];
						y[0]=cellBucket[mm.position.x,mm.position.z,0];
						y[1]=cellBucket[mm.position.x,mm.position.z,1];
						y[2]=cellBucket[mm.position.x,mm.position.z,2];
						y[3]=cellBucket[mm.position.x,mm.position.z,3];
						y[4]=cellBucket[mm.position.x,mm.position.z,4];
						y[5]=cellBucket[mm.position.x,mm.position.z,5];
						y[6]=cellBucket[mm.position.x,mm.position.z,6];
						y[7]=cellBucket[mm.position.x,mm.position.z,7];
						y[8]=cellBucket[mm.position.x,mm.position.z,8];
						gc.unpack(y);
						gc.banTarget = (byte)mm.targetCount;
						gc.banTimeout = (ushort)((int)mm.timeOut);
						byte[]x = gc.pack();
						cellBucket[mm.position.x,mm.position.z,4]=x[4];
						cellBucket[mm.position.x,mm.position.z,7]=x[7];
						cellBucket[mm.position.x,mm.position.z,8]=x[8];
						
				}
			}
			if(orders.ContainsKey(BaseCombatEntity.Faction.Scientist)){
				OrderCell[] bucket = (orders.ContainsKey(BaseCombatEntity.Faction.Scientist)?orders[BaseCombatEntity.Faction.Scientist].ToArray():new OrderCell[0]);
				foreach(OrderCell mm in bucket){
					if(mm!=null && mm.timeOut< UnityEngine.Time.realtimeSinceStartup){
						if(orders[BaseCombatEntity.Faction.Scientist].Contains(mm)){
							orders[BaseCombatEntity.Faction.Scientist].Remove(mm);
						}
						if(mm.assignedAgents!=null){
							mm.assignedAgents.Clear();
						}
					}
				}
				foreach(OrderCell mm in orders[BaseCombatEntity.Faction.Scientist]){
						GridCell gc = new GridCell();
						byte[] y = new byte[9];
						y[0]=cellBucket[mm.position.x,mm.position.z,0];
						y[1]=cellBucket[mm.position.x,mm.position.z,1];
						y[2]=cellBucket[mm.position.x,mm.position.z,2];
						y[3]=cellBucket[mm.position.x,mm.position.z,3];
						y[4]=cellBucket[mm.position.x,mm.position.z,4];
						y[5]=cellBucket[mm.position.x,mm.position.z,5];
						y[6]=cellBucket[mm.position.x,mm.position.z,6];
						y[7]=cellBucket[mm.position.x,mm.position.z,7];
						y[8]=cellBucket[mm.position.x,mm.position.z,8];
						gc.unpack(y);
						gc.sciTarget = (byte)mm.targetCount;
						gc.sciTimeout = (ushort)((int)mm.timeOut);
						byte[]x = gc.pack();
						cellBucket[mm.position.x,mm.position.z,3]=x[3];
						cellBucket[mm.position.x,mm.position.z,5]=x[5];
						cellBucket[mm.position.x,mm.position.z,6]=x[6];
					
				}
			}
			
			//UnityEngine.Debug.Log(s);
			return cellBucket;
		}
		public static Vector3Int posToGrid(Vector3 v){
			Vector3 corrected= ((v + new Vector3(gridAdjustmentX,0,gridAdjustmentZ))/100);
			corrected.y=v.y*100f;
			return new Vector3Int((int)(Mathf.Floor(corrected.x)),(int)(Mathf.Floor(corrected.y)),(int)(Mathf.Floor(corrected.z)));
		}
		public static Vector3 gridToPos(Vector3 v){
			Vector3 result =  (Vector3)(((v*100) - new Vector3(gridAdjustmentX,0,gridAdjustmentZ)) + new Vector3(50,0,50));
			result.y=v.y/100f;
			return result;
		}
		public static bool removeOrder(OrderCell oc){
			if(orders[BaseCombatEntity.Faction.Bandit].Contains(oc)){
				foreach(BaseAIBrain hn in oc.assignedAgents){
					if(gridAgents.ContainsKey(hn)){
						gridAgents[hn].currentTarget=null;
					}
				}
				orders[BaseCombatEntity.Faction.Bandit].Remove(oc);
				return true;
			}
			if(orders[BaseCombatEntity.Faction.Scientist].Contains(oc)){
				foreach(BaseAIBrain hn in oc.assignedAgents){
					if(gridAgents.ContainsKey(hn)){
						gridAgents[hn].currentTarget=null;
					}
				}
				orders[BaseCombatEntity.Faction.Scientist].Remove(oc);
				return true;
			}
			return false;
		}
		public static bool addOrder(BaseCombatEntity.Faction bce,OrderCell order, bool max = false){
			if(order.timeOut==0){order.timeOut = UnityEngine.Time.realtimeSinceStartup+900;}
			if(orders.ContainsKey(bce)){
				foreach(OrderCell oc in orders[bce]){
					if((oc.position.x-order.position.x==0)&&(oc.position.z-order.position.z==0)){
						if(max){
							oc.targetCount = System.Math.Max(oc.targetCount,order.targetCount);
							oc.timeOut = System.Math.Max(oc.timeOut,order.timeOut);
							
						}else{
							oc.targetCount = order.targetCount;
							oc.timeOut = order.timeOut;
						}
						return true;
						
					}
				}
				orders[bce].Add(order);
						return true;
			}else{
				orders.Add(bce,new List<OrderCell>());
				orders[bce].Add(order);
						return true;
			}
			
			return false;
			
		}
		public static OrderCell bestOrder(BaseAIBrain bn){	
			if(bn==null){return null;}
			if((bn.Navigator)==null){return null;}
			if((bn.Navigator.BaseEntity as BaseCombatEntity)==null){return null;}
			if(bn.Navigator.Agent==null){return null;}
			//UnityEngine.Debug.Log("Has agent - "+bn.Navigator.Agent.agentTypeID.ToString());
			//if(bn.Navigator.Agent.agentTypeID== 0){return null;}
			GridAgent ga = getGridAgent(bn);	
			if(ga==null){return null;}
			if(orders==null){orders=new Dictionary<BaseCombatEntity.Faction,List<OrderCell>>();}
			if(!orders.ContainsKey(BaseCombatEntity.Faction.Bandit)){orders.Add(BaseCombatEntity.Faction.Bandit, new List<OrderCell>());}
			if(!orders.ContainsKey(BaseCombatEntity.Faction.Scientist)){orders.Add(BaseCombatEntity.Faction.Scientist, new List<OrderCell>());}
			if(ga.currentTarget!= null){
				if(!orders[BaseCombatEntity.Faction.Bandit].Contains(ga.currentTarget) && !orders[BaseCombatEntity.Faction.Scientist].Contains(ga.currentTarget)){
					if(ga.currentTarget.assignedAgents!=null){
						ga.currentTarget.assignedAgents.Remove(bn);
					}
					ga.currentTarget=null;
				}
				if(ga.currentTarget!=null && ga.currentTarget.assignedAgents==null){
					ga.currentTarget.assignedAgents =  new List<BaseAIBrain>();
				}
				if(ga.currentTarget!=null && ga.currentTarget.assignedAgents.Count()>ga.currentTarget.targetCount){
					ga.currentTarget.assignedAgents.Remove(bn);
					ga.currentTarget= null;
				}
				if(ga.currentTarget!=null && ga.currentTarget.timeOut< UnityEngine.Time.realtimeSinceStartup){
					if(orders[BaseCombatEntity.Faction.Bandit].Contains(ga.currentTarget)){
						orders[BaseCombatEntity.Faction.Bandit].Remove(ga.currentTarget);
					}
					if(orders[BaseCombatEntity.Faction.Scientist].Contains(ga.currentTarget)){
						orders[BaseCombatEntity.Faction.Scientist].Remove(ga.currentTarget);
					}
					if(ga.currentTarget.assignedAgents!=null){
						ga.currentTarget.assignedAgents.Remove(bn);
					}
					ga.currentTarget= null;
				}
			}
			//UnityEngine.Debug.Log("ga:"+(ga==null).ToString());
			if(!orders.ContainsKey((bn.Navigator.BaseEntity as BaseCombatEntity).faction)){
				orders.Add((bn.Navigator.BaseEntity as BaseCombatEntity).faction,new List<OrderCell>());
			}
			OrderCell betterTarget = null;
			GridAgent removalTarget = ga;
			foreach(OrderCell ic in orders[(bn.Navigator.BaseEntity as BaseCombatEntity).faction].OrderBy(a => Vector3.Distance(gridToPos(a.position),bn.Navigator.transform.position))){
				if(ic.assignedAgents== null){
					ic.assignedAgents =  new List<BaseAIBrain>();
				}
				if(ga.currentTarget == null || ic != ga.currentTarget){
					if(ic.assignedAgents.Count()<ic.targetCount){
						betterTarget = ic;
						break;
					}else{
						foreach(BaseAIBrain hn in ic.assignedAgents.ToArray()){
							//UnityEngine.Debug.Log("hn:"+(hn==null).ToString());
							if(hn==null){ic.assignedAgents.Remove(hn);continue;}
							if(hn.transform==null){ic.assignedAgents.Remove(hn);continue;}
							GridAgent ga2=getGridAgent(hn);
							if(ga2==null||removalTarget.brain==null){continue;}
							if(Vector3.Distance(hn.transform.position, gridToPos(ic.position))>Vector3.Distance(removalTarget.brain.Navigator.transform.position, gridToPos(ic.position))){
								//UnityEngine.Debug.Log("getGridAgent:"+(hn==null).ToString());
								removalTarget = ga2;
								betterTarget=ic;
								break;
							}
						}
					}
				}
			}
			lock(ga){ 
				
				if(betterTarget==null)return(ga.currentTarget);
				lock(betterTarget){
					//UnityEngine.Debug.Log("bt:"+(betterTarget==null).ToString());
					//UnityEngine.Debug.Log("ga:"+(ga==null).ToString());
					//UnityEngine.Debug.Log("ga.currentTarget:"+(ga.currentTarget==null).ToString());
					if(ga.currentTarget!=null){
						//UnityEngine.Debug.Log("ga.currentTarget.targetCount:"+(ga.currentTarget.targetCount==null).ToString());
						if(ga.currentTarget.targetCount==null)return(betterTarget);
						//UnityEngine.Debug.Log("ga.currentTarget.assignedAgents:"+(ga.currentTarget.assignedAgents==null).ToString());
						if(ga.currentTarget.assignedAgents==null)return(betterTarget);
					}
					//UnityEngine.Debug.Log("betterTarget.targetCount:"+(betterTarget.targetCount==null).ToString());
					if(betterTarget.targetCount==null)return(ga.currentTarget);
					//UnityEngine.Debug.Log("betterTarget.assignedAgents:"+(betterTarget.assignedAgents==null).ToString());
					if(betterTarget.assignedAgents==null)return(ga.currentTarget);
					if((ga.currentTarget==null&& betterTarget!=null) || (betterTarget!=null && ga!=null && (betterTarget.targetCount-betterTarget.assignedAgents.Count())>(ga.currentTarget.targetCount-ga.currentTarget.assignedAgents.Count()))){
						if(betterTarget.assignedAgents.Count()<betterTarget.targetCount){
							//UnityEngine.Debug.Log("BetterPick");
							if(ga.currentTarget!=null){
								if(ga.currentTarget.assignedAgents== null){
									ga.currentTarget.assignedAgents =  new List<BaseAIBrain>();
								}
								ga.currentTarget.assignedAgents.Remove(bn);
							}
							ga.currentTarget = betterTarget;
							if(ga.currentTarget!=null){
								if(ga.currentTarget.assignedAgents== null){
									ga.currentTarget.assignedAgents =  new List<BaseAIBrain>();
								}
								ga.currentTarget.assignedAgents.Add(bn);
							}
						}else if (removalTarget!=null && removalTarget!=ga){
							//UnityEngine.Debug.Log("BetterAgent");
				
							removalTarget.currentTarget=null;
							betterTarget.assignedAgents.Remove((removalTarget.brain));
										
							if(ga.currentTarget!=null){
								if(ga.currentTarget.assignedAgents== null){
									ga.currentTarget.assignedAgents =  new List<BaseAIBrain>();
								}
								ga.currentTarget.assignedAgents.Remove(bn);
							}
							ga.currentTarget = betterTarget;
							if(ga.currentTarget!=null){
								if(ga.currentTarget.assignedAgents== null){
									ga.currentTarget.assignedAgents =  new List<BaseAIBrain>();
								}
								ga.currentTarget.assignedAgents.Add(bn);
							}
						}
					}
				}
			}
			return ga.currentTarget;
		}
		public static GridAgent getGridAgent(BaseAIBrain bn){
			if((bn.Navigator.BaseEntity as BaseCombatEntity)==null){return null;}
			if(gridAgents.ContainsKey(bn)){
				gridAgents[bn] = gridAgents[bn];
				gridAgents[bn].position= posToGrid(bn.transform.position);
				gridAgents[bn].lastUpdate = UnityEngine.Time.realtimeSinceStartup;
				gridAgents[bn].state = bn.CurrentState.AgrresiveState;
				gridAgents[bn].brain=bn;
				return gridAgents[bn];
			}else{
				GridAgent ga = new GridAgent();
				ga.position= posToGrid(bn.transform.position);
				ga.lastUpdate = UnityEngine.Time.realtimeSinceStartup;
				ga.state = bn.CurrentState.AgrresiveState;
				ga.brain=bn;
				gridAgents.Add(bn,ga);
				return gridAgents[bn];
			}
		}
		private void addOrderExternal(BaseCombatEntity.Faction bce, Vector3 position, int targetCount = 5, float timeOut = 900, bool max=false){
			OrderCell oc = new OrderCell();
			oc.faction = bce;
			oc.position = posToGrid(position);
			oc.targetCount=targetCount;
			oc.timeOut=timeOut+UnityEngine.Time.realtimeSinceStartup;
			addOrder(bce,oc,max);
			
		}
		private void addOrderExternal(BaseCombatEntity.Faction bce, Vector3Int position, int targetCount = 5, float timeOut = 900, bool max=false){
			OrderCell oc = new OrderCell();
			oc.faction = bce;
			oc.position = position;
			oc.targetCount=targetCount;
			oc.timeOut=timeOut+UnityEngine.Time.realtimeSinceStartup;
			addOrder(bce,oc,max).ToString();
			
		}
		#endregion
	#region ConsolCmd		
			[ConsoleCommand("fact.picktgts")]
			private void PickMonumentServerCommand(ConsoleSystem.Arg arg){
				pickTargets();
			}
			[ConsoleCommand("fact.spawnonmonument")]
			private void SpawnOnMonumentServerCommand(ConsoleSystem.Arg arg){
				spawnOnMonument(BaseCombatEntity.Faction.Bandit);
			}
			
		#endregion
	#region ChatCmd
		[ChatCommand("hz_save")] void surv_save(BasePlayer player, string command, string[] args){		
				BasePlayer bp = (BasePlayer)player;
				SaveConfig();
				SendReply(bp,"Saving!");
			}
			[ChatCommand("hz_load")] void surv_load(BasePlayer player, string command, string[] args){		
				BasePlayer bp = (BasePlayer)player;
				LoadConfig();
				SendReply(bp,"Saving!");
			}
			[ChatCommand("hz_stats")] void stats(BasePlayer player, string command, string[] args){	
				BasePlayer bp = (BasePlayer)player;
				SendReply(bp,"Current Score [Bandit]: "+(config.playerScores.ContainsKey(bp.userID)?(config.playerScores[bp.userID].ContainsKey(BaseCombatEntity.Faction.Bandit)?(config.playerScores[bp.userID][BaseCombatEntity.Faction.Bandit].ToString()):"No Bandit Score"):"No Score"));
				SendReply(bp,"Current Score [Scientist]: "+(config.playerScores.ContainsKey(bp.userID)?(config.playerScores[bp.userID].ContainsKey(BaseCombatEntity.Faction.Scientist)?(config.playerScores[bp.userID][BaseCombatEntity.Faction.Scientist].ToString()):"No Scientist Score"):"No Score"));
				SendReply(bp,posToGrid(bp.transform.position).ToString());
			}
			[ChatCommand("hz_setOrder")] void setOrderToPlayerPos(BasePlayer player, string command, string[] args){
				BasePlayer bp = (BasePlayer)player;
				OrderCell oc = new OrderCell();
				oc.position = posToGrid(bp.transform.position);
				oc.targetCount=5;
				addOrder(bp.faction,oc,false);
				
			}
			
		#endregion
	#region Faction Handlers
		private float getBalance(BaseCombatEntity.Faction faction){
			if(config.factionBank.ContainsKey(faction)){
				return config.factionBank[faction];
			}
			else {
				config.factionBank.Add(faction,0);
				return -1;
			
			}
			
		}
		private Dictionary<BaseCombatEntity.Faction,float> getAlignments(BasePlayer bp){
			ulong id= bp.userID;
			if(config.playerScores==null) 
				config.playerScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
			if(!config.playerScores.ContainsKey(id)) 
				config.playerScores.Add(id, new Dictionary<BaseCombatEntity.Faction,float>());
			if(!config.playerScores[id].ContainsKey(BaseCombatEntity.Faction.Bandit))
				config.playerScores[id].Add(BaseCombatEntity.Faction.Bandit,0f);
			if(!config.playerScores[id].ContainsKey(BaseCombatEntity.Faction.Scientist))
				config.playerScores[id].Add(BaseCombatEntity.Faction.Scientist,0f);
			return config.playerScores[id];
			
		}
		private Dictionary<BaseCombatEntity.Faction,float> getAlignments(ulong id){
			if(config.playerScores==null) 
				config.playerScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
			if(!config.playerScores.ContainsKey(id)) 
				config.playerScores.Add(id, new Dictionary<BaseCombatEntity.Faction,float>());
			if(!config.playerScores[id].ContainsKey(BaseCombatEntity.Faction.Bandit))
				config.playerScores[id].Add(BaseCombatEntity.Faction.Bandit,0f);
			if(!config.playerScores[id].ContainsKey(BaseCombatEntity.Faction.Scientist))
				config.playerScores[id].Add(BaseCombatEntity.Faction.Scientist,0f);
			return config.playerScores[id];
			
		}
		void AddSeat(BaseAnimalNPC ent, Vector3 locPos, bool chair=false) {
			BaseEntity seat = (chair ? 
				GameManager.server.CreateEntity("assets/prefabs/deployable/chair/chair.deployed.prefab", ent.transform.position, new Quaternion()) as BaseEntity :
				GameManager.server.CreateEntity("assets/prefabs/misc/xmas/sled/sled.deployed.prefab", ent.transform.position, new Quaternion()) as BaseEntity) ;
			if (seat == null) return;
			seat.Spawn();
			seat.SetParent(ent);
			seat.transform.localPosition = locPos;
			GameObject.Destroy(seat.GetComponent<GroundWatch>());
			if(seat.gameObject.GetComponent<Rigidbody>()!=null)
				seat.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ| RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX| RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ ;
			foreach(Transform t in ((BaseMountable)seat).dismountPositions){
				t.parent=seat.transform;
				t.localPosition=locPos+new Vector3(0,1,0);
			}
			seat.SendNetworkUpdateImmediate(true);
		}
		void makeShop(InvisibleVendingMachine seat,int offset){
			
			float factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.9f;
			List<EconomyEntry> lEcon = new List<EconomyEntry>();
			Shuffle(Economy,offset+(int)Math.Abs(seat.transform.position.x*100)+(int)Math.Abs(seat.transform.position.z*100));
			lEcon.AddRange(Economy.ToArray());
			Shuffle(lEcon,UnityEngine.Random.Range(0,20000));
			EconomyEntry econ = lEcon[(0+shopCount)%lEcon.Count()];
			NPCVendingOrder.Entry[] entries = seat.vendingOrders.orders.ToArray();
			seat.vendingOrders = ScriptableObject.CreateInstance("NPCVendingOrder") as NPCVendingOrder;
			seat.vendingOrders.orders = entries;
			seat.shopName = seat.shopName.Replace("[NPCShop]","")+"[NPCShop]";
			for(int i = 0; i < seat.vendingOrders.orders.Count();i++){
				NPCVendingOrder.Entry newOrder = new NPCVendingOrder.Entry();				
				
				newOrder.sellItem=seat.vendingOrders.orders[i].sellItem;
				newOrder.sellItemAmount=seat.vendingOrders.orders[i].sellItemAmount;
				newOrder.sellItemAsBP=seat.vendingOrders.orders[i].sellItemAsBP;
				newOrder.currencyItem=seat.vendingOrders.orders[i].currencyItem;
				newOrder.currencyAmount=seat.vendingOrders.orders[i].currencyAmount;
				newOrder.currencyAsBP=seat.vendingOrders.orders[i].currencyAsBP;
				newOrder.weight=seat.vendingOrders.orders[i].weight;
				newOrder.refillAmount=seat.vendingOrders.orders[i].refillAmount;
				newOrder.refillDelay=seat.vendingOrders.orders[i].refillDelay;
				
				seat.vendingOrders.orders[i] = newOrder;
			}
			seat.vendingOrders.orders[0].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[0].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[0].currencyAsBP=false;
			seat.vendingOrders.orders[0].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[0].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[0].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+1f;
			econ = lEcon[(1+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[1].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[1].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[1].currencyAsBP=false;
			seat.vendingOrders.orders[1].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[1].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[1].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.9f;
			econ = lEcon[(2+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[2].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[2].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[2].currencyAsBP=false;
			seat.vendingOrders.orders[2].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[2].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[2].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.9f;
			econ = lEcon[(3+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[3].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[3].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[3].currencyAsBP=false;
			seat.vendingOrders.orders[3].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[3].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[3].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.8f;
			econ = lEcon[(4+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[4].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[4].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[4].currencyAsBP=false;
			seat.vendingOrders.orders[4].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[4].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[4].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.8f;
			econ = lEcon[(5+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[5].currencyItem = ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[5].currencyAmount = econ.amount;
			seat.vendingOrders.orders[5].currencyAsBP=false;
			seat.vendingOrders.orders[5].sellItemAmount=(int)(econ.price*factor);
			seat.vendingOrders.orders[5].sellItem=ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[5].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.7f;
			econ = lEcon[(6+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[6].currencyItem = ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[6].currencyAmount = econ.amount;
			seat.vendingOrders.orders[6].currencyAsBP=false;
			seat.vendingOrders.orders[6].sellItemAmount=(int)(econ.price*factor);
			seat.vendingOrders.orders[6].sellItem=ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[6].sellItemAsBP=false;
			
			
			
			shopCount++;
			seat.SendNetworkUpdateImmediate(true);
		}
		void AddVending(HumanNPC ent, Vector3 locPos) {
			//BaseEntity seat = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", ent.transform.position, new Quaternion()) as BaseEntity;
			InvisibleVendingMachine seat = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab", ent.transform.position, new Quaternion()) as InvisibleVendingMachine;
			if (seat == null) return;
			seat.Spawn();
			seat.SetParent(ent);		
			seat.transform.Rotate(new Vector3(90,0,0));
			seat.transform.SetParent(ent.transform);	
			seat.transform.localPosition = new Vector3(0,2f,-1f);
			seat.SetFlag(VendingMachine.VendingMachineFlags.Broadcasting,false);
			seat.syncPosition=true;
			seat.transform.name="NPC Shop";
			GameObject.Destroy(seat.GetComponent<GroundWatch>());
			if(seat.gameObject.GetComponent<Rigidbody>()!=null)
				seat.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ| RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ ;
			
			makeShop(seat,(int)ent.userID);
			//seat.SendNetworkUpdateImmediate(true);
			
		}

			
			public bool changeScore(BasePlayer bp, BaseCombatEntity.Faction ft, float score, float penaltyScale = 2){
				try{
					if(bp==null)return false;
					if(ft==null)return false;
					if(bp.IsConnected != true) return false;
					ulong id= bp.userID;
					if(config.playerScores==null) config.playerScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
					if(!config.playerScores.ContainsKey(id)) config.playerScores.Add(id, new Dictionary<BaseCombatEntity.Faction,float>());
					if(!config.playerScores[id].ContainsKey(BaseCombatEntity.Faction.Bandit))config.playerScores[id].Add(BaseCombatEntity.Faction.Bandit,0f);
					if(!config.playerScores[id].ContainsKey(BaseCombatEntity.Faction.Scientist))config.playerScores[id].Add(BaseCombatEntity.Faction.Scientist,0f);
					
					switch(ft){
						case BaseCombatEntity.Faction.Bandit:
							config.playerScores[id][BaseCombatEntity.Faction.Bandit]+=score;
							config.playerScores[id][BaseCombatEntity.Faction.Scientist]+=-score*penaltyScale;
							break;
						case BaseCombatEntity.Faction.Scientist:
							config.playerScores[id][BaseCombatEntity.Faction.Bandit]+=-score*penaltyScale;
							config.playerScores[id][BaseCombatEntity.Faction.Scientist]+=score;
							
							break;
						case BaseCombatEntity.Faction.Player:
							config.playerScores[id][BaseCombatEntity.Faction.Bandit]+=score*0.25f;
							config.playerScores[id][BaseCombatEntity.Faction.Scientist]+=score*0.25f;						
							break;
					}
					return true;
				}catch(Exception e){return false;}
			}
			public BaseCombatEntity.Faction getNativeFaction(BasePlayer bp){		
				if(bp==null)return BaseCombatEntity.Faction.Player;
				if(bp.IsConnected != true) return BaseCombatEntity.Faction.Player;
				ulong id= bp.userID;
				if(config.playerScores==null) config.playerScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
				if(!config.playerScores.ContainsKey(id)) config.playerScores.Add(id, new Dictionary<BaseCombatEntity.Faction,float>());
				if(!config.playerScores[id].ContainsKey(BaseCombatEntity.Faction.Bandit))config.playerScores[id].Add(BaseCombatEntity.Faction.Bandit,0f);
				if(!config.playerScores[id].ContainsKey(BaseCombatEntity.Faction.Scientist))config.playerScores[id].Add(BaseCombatEntity.Faction.Scientist,0f);				
				if(config.playerScores[id][BaseCombatEntity.Faction.Bandit]<0){
					if(config.playerScores[id][BaseCombatEntity.Faction.Scientist]<0){
						return BaseCombatEntity.Faction.Player;
					}else{//
						return BaseCombatEntity.Faction.Scientist;						
					}					
				}else{					
					if(config.playerScores[id][BaseCombatEntity.Faction.Scientist]<0){
						return BaseCombatEntity.Faction.Bandit;						
					}else{
						return BaseCombatEntity.Faction.Default;						
					}
				}
			}
			public void closeFactionLogo(BasePlayer p){
				
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo
				{
					connection = p.net.connection
				}, null, "DestroyUI", "HotzoneFactionLogo");
			}
			public void GenerateFactionLogo(BasePlayer p){
				closeFactionLogo(p);
				string imageurl = "";
				switch(p.faction){
					case BaseCombatEntity.Faction.Default:
						 imageurl = "https://i.imgur.com/lfhowUy.png";
						break;
					case BaseCombatEntity.Faction.Scientist:
						 imageurl = "https://i.imgur.com/VGYGFqO.png";
						break;
					case BaseCombatEntity.Faction.Bandit:
						 imageurl = "https://i.imgur.com/2jbNNmG.png";
						break;
					case BaseCombatEntity.Faction.Player:
						 imageurl = "https://i.imgur.com/FZOkDzY.png";
						break;					
				}
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo
					{
						connection = p.net.connection
					}, null, "AddUI", "[\n\t{\n\t\t\"name\": \"HotzoneFactionLogo\",\n\t\t\"parent\": \"Hud\",\n\n\t\t\"components\":\n\t\t[\n\t\t\t{\n\t\t\t\t\"type\":\"UnityEngine.UI.RawImage\",\n\t\t\t\t\"imagetype\": \"Tiled\",\n\t\t\t\t\"color\": \"1.0 1.0 1.0 1.0\",\n\t\t\t\t\"url\": \""+imageurl+"\",\n\t\t\t},\n\n\t\t\t{\n\t\t\t\t\"type\":\"RectTransform\",\n\t\t\t\t\"anchormin\": \"0.974 0.948\",\n\t\t\t\t\"anchormax\": \"0.9989 0.998\"\n\t\t\t}\n\t\t]\n\t}\n]\n");
			}
			
			public bool validTarget(BaseEntity self, BaseEntity target, bool threatCheck = false){
				return (
					((((BaseCombatEntity)self).faction!=((BaseCombatEntity)target).faction) && 
						((((BaseCombatEntity)self).faction!=BaseCombatEntity.Faction.Default)) && 
						(((BaseCombatEntity)target).faction!=BaseCombatEntity.Faction.Default))
					|| (((BaseCombatEntity)self).faction==BaseCombatEntity.Faction.Default && !threatCheck)
				);
			}
			
		
		#endregion
	#region Oxide Hooks
		object CanDeployItem(BasePlayer player, Deployer deployer, uint entityId){
			Puts("CanDeployItem works!");
			return null;
		}
		//void OnItemDeployed(Deployer deployer, BaseEntity entity, BaseEntity slotEntity)
		private object CanBuild(Planner deployer, Construction construction,Construction.Target target){
			BasePlayer bp = deployer.GetOwnerPlayer();
			if(bp==null) return null;
			if(bp.faction==null) return null;
			
			return null;				
		}
		private object CanFactionBuild(Planner deployer, Construction construction,Construction.Target target){
			return CanBuild(deployer,construction,target);
		}
		void OnItemDropped(Item item, BaseEntity entity){
			try{
				if(!item.parent.playerOwner.IsConnected) return;
				BasePlayer bp = item.parent.playerOwner;
				BaseCombatEntity bce = (getLookingAt(bp) as BaseCombatEntity);
				if(bce==null) return;
				if(bce.faction==bp.faction){
					WorldItem wi = entity as WorldItem;
					if(wi==null) return;
					if((bce is HumanNPC)){
						HumanNPC hn = (bce as HumanNPC);
						Item item2;
						item2 = global::ItemManager.CreateByItemID(((int)wi.item.info.itemid),1, 0UL);
						item2.OnVirginSpawn();
						bool isWeapon = item2.info.category ==ItemCategory.Weapon;
						if(isWeapon){
							hn.inventory.containerBelt.GetSlot(0).Drop(hn.inventory.containerBelt.playerOwner.GetDropPosition(), hn.inventory.containerBelt.playerOwner.GetDropVelocity(), default(Quaternion));
						}
						if (!item2.MoveToContainer((isWeapon||hn.inventory.containerMain.IsFull()?hn.inventory.containerBelt:hn.inventory.containerMain), 0, true, false))
						{
							if (hn.inventory.containerBelt.playerOwner)
							{
								item2.Drop(hn.inventory.containerBelt.playerOwner.GetDropPosition(), hn.inventory.containerBelt.playerOwner.GetDropVelocity(), default(Quaternion));
							}
							else
							{
								item2.Remove(0f);
							}
						}
						hn.inventory.containerBelt.MarkDirty();
						hn.EquipWeapon();
						BaseProjectile bpro = (item2.GetHeldEntity() as BaseProjectile);
						if(bpro){
							bpro.TopUpAmmo();
						}
						wi.Kill();
					}else if ((bce is BaseAnimalNPC)){	
						if(entity.transform.name.Contains("sled")){
							if((bce as BaseAnimalNPC).transform.name.Contains("bear") ||(bce as BaseAnimalNPC).transform.name.Contains("stag"))
								AddSeat((bce as BaseAnimalNPC), new Vector3(0.0f,-0.1f,-2f));
							else if((bce as BaseAnimalNPC).transform.name.Contains("wolf") ||(bce as BaseAnimalNPC).transform.name.Contains("boar")){
								AddSeat((bce as BaseAnimalNPC), new Vector3(0.0f,0.2f,-1.5f));
							}			
							wi.RemoveItem();	
							wi.Kill();							
							bce.SendNetworkUpdateImmediate(true);
						}else if(entity.transform.name.Contains("chair")){
							Puts("Chair");
							if((bce as BaseAnimalNPC).transform.name.Contains("bear") ||(bce as BaseAnimalNPC).transform.name.Contains("stag")){
								Puts("bearchair");
								AddSeat((bce as BaseAnimalNPC), new Vector3(0.0f,0.6f,0f),true);
								Puts("bearchaird");
							}
							else if((bce as BaseAnimalNPC).transform.name.Contains("wolf") ||(bce as BaseAnimalNPC).transform.name.Contains("boar")){
								AddSeat((bce as BaseAnimalNPC), new Vector3(0.0f,0.2f,0f),true);
							}
							Puts("SendNetworkUpdateImmediate");
							bce.SendNetworkUpdateImmediate(true);
							Puts("remove");
							wi.RemoveItem();	
							Puts("Killin");
							wi.Kill();
							Puts("Killed");
						}
					}
				}
			}catch(Exception e){}
		}
		object OnBasePlayerAttacked(BasePlayer victimbp, HitInfo info){
			if(victimbp==null)return null;	
			var result = ((BaseCombatEntity)(info.Initiator)).faction!=victimbp.faction||((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Default||(((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Player && !info.damageTypes.Has(Rust.DamageType.Heat));
			if(result==false) return victimbp;
			return null;
		}
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info){
				if(info.HitEntity==null||info.Initiator==null) return null;
				if(info.HitEntity.gameObject==null||info.Initiator.gameObject==null) return null;
				bool? returnvar = null;
				try{
					returnvar = ((BaseCombatEntity)(info.Initiator)).faction!=((BaseCombatEntity)(info.HitEntity)).faction||((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Default||((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Player||(!(info.HitEntity as HumanNPC) && !(info.HitEntity as BaseNpc));
					
					float switcher = UnityEngine.Random.Range(0.0f,1.0f);
					((HumanNPC)info.HitEntity).Brain.Navigator.Resume();
					if(!info.damageTypes.Has(Rust.DamageType.Heat)){
						if(switcher < 0.4f){
							((HumanNPC)info.HitEntity).Brain.Navigator.SetDestination(info.HitEntity.transform.position+new Vector3(UnityEngine.Random.Range(-5.0f,5.0f),0,UnityEngine.Random.Range(-5.0f,5.0f)), global::BaseNavigator.NavigationSpeed.Fast, 0f, 0f);
							((HumanNPC)info.HitEntity).SetDucked(false);
							}
						else if(switcher < 0.7f){
							((HumanNPC)info.HitEntity).Brain.Navigator.SetDestination(info.HitEntity.transform.position, global::BaseNavigator.NavigationSpeed.Normal, 0f, 0f);
							((HumanNPC)info.HitEntity).SetDucked(true);
						}else{
							((HumanNPC)info.HitEntity).SetDucked(false);
						}
						if(!((HumanNPC)info.HitEntity).Brain.Senses.Memory.Targets.Contains((BaseCombatEntity)info.Initiator))
							((HumanNPC)info.HitEntity).Brain.Senses.Memory.Targets.Add((BaseCombatEntity)info.Initiator);
					}
				}catch(Exception e){}
				if (info.damageTypes.Has(Rust.DamageType.Heat) ){
					try{
						if( ((BaseCombatEntity)(info.Initiator)).faction==((BaseCombatEntity)(info.HitEntity)).faction)
						{
							Puts("Squadding?");
							if(info.HitEntity is HumanNPC){
								if(((HumanNPC)info.HitEntity).creatorEntity==null){
									((HumanNPC)info.HitEntity).creatorEntity=((BasePlayer)info.Initiator);
								}else{						
									((HumanNPC)info.HitEntity).creatorEntity=null;
								}
							}
							if(info.HitEntity is BaseAnimalNPC){
								if(((BaseAnimalNPC)info.HitEntity).creatorEntity==null){
									((BaseAnimalNPC)info.HitEntity).creatorEntity=((BasePlayer)info.Initiator);
								}else{						
									((BaseAnimalNPC)info.HitEntity).creatorEntity=null;
								}
							}
							
							return false;
						}	
					}catch(Exception e){
						Puts(e.ToString());
					}//			
				}
				try{///
					BasePlayer bp = (BasePlayer)info.Initiator;	
					if(bp.IsConnected && (info.HitEntity is BasePlayer|| info.HitEntity is BaseAnimalNPC)){		
							BaseCombatEntity.Faction oldFaction = bp.faction;
							switch(((BaseCombatEntity)info.HitEntity).faction){
								case BaseCombatEntity.Faction.Bandit:
									changeScore(bp, BaseCombatEntity.Faction.Scientist, 0.05f);									
									bp.faction =getNativeFaction(bp);				
									break;
								case BaseCombatEntity.Faction.Scientist:
									changeScore(bp, BaseCombatEntity.Faction.Bandit, 0.05f);									
									bp.faction =getNativeFaction(bp);				
									break;
								case BaseCombatEntity.Faction.Player:
									changeScore(bp, BaseCombatEntity.Faction.Player, 0.05f);									
									bp.faction =getNativeFaction(bp);				
									break;
							}
							if(oldFaction!=bp.faction) GenerateFactionLogo(bp);
					}else{
					}
				}catch(Exception e){}
				try{
					float b = UnityEngine.Time.realtimeSinceStartup + 60f;
					if(returnvar==true)((BaseCombatEntity)info.Initiator).unHostileTime = Math.Max(((BaseCombatEntity)info.Initiator).unHostileTime , b);
				}catch(Exception e){}
				if((returnvar==null || returnvar == false ) && info.HitEntity is HumanNPC){
					return false;
				}else{
					return null;
				}
			}
		object CanEntityBeHostile(BasePlayer entity){
			if(entity.IsConnected==false){
				return entity.unHostileTime> UnityEngine.Time.realtimeSinceStartup;
			}
			return null;
		}
		object OnTurretAuthorize(AutoTurret at, BasePlayer bp){
			at.faction=bp.faction;
			return null;
		}
		object CanBradleyApcTarget(BradleyAPC apc, BaseCombatEntity bce){
			return validTarget(((BaseCombatEntity)apc),bce,true);
		}
		
		void OnSenseBrains(AIBrainSenses aibs){
			foreach (PatrolHelicopterAI ent in AIHelis){
				if(Vector3.Distance(ent.transform.position,aibs.owner.transform.position)<200){
					aibs.Memory.Players.Add(ent.helicopterBase);
				}
			}
		}
		object OnGetBestTarget(HumanNPC hn){
			global::BaseEntity result = null;
			float num = -1f;
			foreach (global::BaseEntity current in hn.Brain.Senses.Memory.Targets)
			{
				if (!(current == null) && current.Health() > 0f && ((BaseCombatEntity)hn).faction!=((BaseCombatEntity)current).faction&& ((BaseCombatEntity)current).faction !=BaseCombatEntity.Faction.Default)
				{
					float value = Vector3.Distance(current.transform.position, hn.transform.position);
					float num2 = 1f - Mathf.InverseLerp(1f, hn.Brain.SenseRange, value);
					float value2 = Vector3.Dot((current.transform.position - hn.eyes.position).normalized, hn.eyes.BodyForward());
					num2 += Mathf.InverseLerp(hn.Brain.VisionCone, 1f, value2) / 2f;
					num2 += (hn.Brain.Senses.Memory.IsLOS(current) ? 2f : 0f);
					if (num2 > num)
					{
						result = current;
						num = num2;
					}
				}
			}
			return result;
		}			
		object OnNpcTarget(BaseEntity hn, BaseEntity be){
			try{
				bool result = ((BaseCombatEntity)hn).faction!=((BaseCombatEntity)be).faction;//
				return (result==true?(bool?)null:(bool?)false);
			}catch(Exception e){return null;}
		}
		//set ConVar.Sentry.targetall to true, this'll handle authed players. This allows npcs to also get shot. Requires IsNPC hack
		object OnTurretTarget(AutoTurret at, BaseCombatEntity bce){
			if(bce==null)return null;
			if(at==null)return null;
			BasePlayer bp = (bce as BasePlayer);
			BaseHelicopter heli = (bce as BaseHelicopter);
			if(bp!=null) if(at.IsAuthed(bp)) return false;
			if(!validTarget(((BaseCombatEntity)at),bce,true)) 		return false;
			return null;
		}
		object OnShouldTarget (AutoTurret turret, BaseCombatEntity ent){
			if(ent!=null){
				if(validTarget(((BaseCombatEntity)turret),ent,true)){
					return true;
				}
			}
			return null;
		}
		object OnTurretCheckHostile(NPCAutoTurret turret, BaseCombatEntity entity){
			if((entity is BaseHelicopter && ((BaseCombatEntity)turret).faction!=BaseCombatEntity.Faction.Scientist) || (entity is BaseTrap)){
				return true;
			}
			if(entity==null)return null;
			if(((entity.faction != BaseCombatEntity.Faction.Scientist) && turret.gameObject.name.Contains("sentry.sci")) || ((entity.faction != BaseCombatEntity.Faction.Bandit) && turret.gameObject.name.Contains("sentry.ban"))){
				if(entity is BaseAnimalNPC || (entity is BasePlayer && (entity as BasePlayer).IsConnected==false)){
					return entity.unHostileTime> UnityEngine.Time.realtimeSinceStartup;
				}
			}
			return null;
		}
		bool? OnIsFriendly(BaseCombatEntity hn, BaseEntity be){
				return null;
		}
		object OnHelicopterUpdateTargets(PatrolHelicopterAI phai,Vector3 strafePos,bool flag,bool shouldUseNapalm){
			foreach (global::BasePlayer basePlayer in global::BasePlayer.activePlayerList)
			{
				if (!basePlayer.HasPlayerFlag(global::BasePlayer.PlayerFlags.SafeZone) && Vector3Ex.Distance2D(phai.transform.position, basePlayer.transform.position) <= 150f &&basePlayer.faction!=BaseCombatEntity.Faction.Scientist)
				{
					bool flag4 = false;
					using (List<global::PatrolHelicopterAI.targetinfo>.Enumerator enumerator2 = phai._targetList.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							if (enumerator2.Current.ply == basePlayer)
							{
								flag4 = true;
								break;
							}
						}
					}
					if (!flag4 && basePlayer.GetThreatLevel() > 0.5f && phai.PlayerVisible(basePlayer) && basePlayer.faction != BaseCombatEntity.Faction.Scientist)
					{
						if(basePlayer.faction != BaseCombatEntity.Faction.Scientist){
							phai._targetList.Add(new global::PatrolHelicopterAI.targetinfo(basePlayer, basePlayer));
						}
					}
				}
			}
			BaseEntity[] beList = new BaseEntity[12];
			int brainsInSphere = BaseEntity.Query.Server.GetBrainsInSphere(phai.transform.position, 50, beList);
			for (int i = 0; i < brainsInSphere; i++)
			{
				global::BaseEntity ent = beList[i];
				if(!(ent is ScientistNPC) && (ent is BasePlayer)){						
					bool isVis = false;
					BasePlayer ply = (ent as BasePlayer);
					Vector3 position = (ply.eyes==null?ent.transform.position+new Vector3(0,0.4f,0):ply.eyes.position);
					if (TOD_Sky.Instance.IsNight && Vector3.Distance(position, phai.interestZoneOrigin) > 40f)
					{
						isVis=false;
					}else{
						Vector3 vector = phai.transform.position - Vector3.up * 6f;
						float num = Vector3.Distance(position, vector);
						Vector3 normalized = (position - vector).normalized;
						RaycastHit raycastHit;
						isVis= GamePhysics.Trace(new Ray(vector + normalized * 5f, normalized), 0f, out raycastHit, num * 1.1f, 1218652417, QueryTriggerInteraction.UseGlobal, null) && raycastHit.collider.gameObject.ToBaseEntity() == ply;
					}
					if(isVis){
						bool flag4 = false;
						using (List<global::PatrolHelicopterAI.targetinfo>.Enumerator enumerator2 = phai._targetList.GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								if (enumerator2.Current.ply == ply)
								{
									flag4 = true;
									break;
								}
							}
						}
						if(!flag4)
							phai._targetList.Add(new global::PatrolHelicopterAI.targetinfo(ent, ply));
					}
					
				}
			}
			if (flag)
			{
				phai.ExitCurrentState();
				phai.State_Strafe_Enter(strafePos, shouldUseNapalm);
			}
			return phai;
		}
		bool? OnIsTarget(BaseCombatEntity hn, BaseEntity be){
			try{
				if((be is BaseHelicopter && ((BaseCombatEntity)hn).faction!=BaseCombatEntity.Faction.Scientist) || (be is BaseTrap)){
					return true;
				}
				bool result = ((BaseCombatEntity)hn).faction!=((BaseCombatEntity)be).faction;//
				return (result==true?(bool?)true:(bool?)false);
			}catch(Exception e){return null;}
		}
		bool? OnIsThreat(BaseCombatEntity hn, BaseEntity be){
			try{
				if((be is BaseHelicopter && ((BaseCombatEntity)hn).faction!=BaseCombatEntity.Faction.Scientist) || (be is BaseTrap)){
					return true;
				}
				bool result = ((BaseCombatEntity)hn).faction!=((BaseCombatEntity)be).faction;//
				return (result==true?(bool?)true:(bool?)false);
			}catch(Exception e){return null;}
		}
		bool? OnCaresAbout(AIBrainSenses hn, BaseEntity be){
			BaseAnimalNPC bn = (be as BaseAnimalNPC);
			
			bool result = ((BaseCombatEntity)hn.owner).faction!=((BaseCombatEntity)be).faction && !(((BaseCombatEntity)be).faction ==BaseCombatEntity.Faction.Default);//
			if(((be is BaseHelicopter) && ((BaseCombatEntity)hn.owner).faction!=BaseCombatEntity.Faction.Scientist) || (be is BaseTrap)){
				return true;
			}
			if(bn && result){	
				UnityEngine.Vector3 vector;
				if((hn.owner as BasePlayer)){
					if ((hn.owner as BasePlayer).isMounted)
					{
						vector = (hn.owner as BasePlayer).eyes.worldMountedPosition;
					}
					else if ((hn.owner as BasePlayer).IsDucked())
					{
						vector = (hn.owner as BasePlayer).eyes.worldCrouchedPosition;
					}
					else if ((hn.owner as BasePlayer).IsCrawling())
					{
						vector = (hn.owner as BasePlayer).eyes.worldCrawlingPosition;
					}
					else
					{
						vector = (hn.owner as BasePlayer).eyes.worldStandingPosition;
					}
				}else{
					vector = hn.owner.transform.position;
					vector.y+=0.3f;
				}
				bool canSee =  (bn.IsVisibleSpecificLayers(vector, bn.CenterPoint(), Physics.DefaultRaycastLayers, float.PositiveInfinity));
			
				hn.Memory.SetLOS(bn,canSee);
				result = canSee;
			}
			return (result==true?(bool?)null:(bool?)false);
		}
		void OnOpenVendingShop(VendingMachine machine, BasePlayer player){
			if(machine is InvisibleVendingMachine){
				if(machine.transform.parent==null){return;}
				BaseAIBrain hn = machine.transform.parent.gameObject.GetComponent<BaseAIBrain>();
				if(hn.states!=null){
					if(hn.states[AIState.Roam]!=null){
						FactionBaseRoamState fbrs = hn.states[AIState.Roam]	as FactionBaseRoamState;
						if(fbrs != null){
							fbrs.waitTime=60;
							fbrs.StateLeave(hn,hn.Navigator.BaseEntity);
						}
					}					
				}				
			}
		}
		void OnEntityKill(BaseHelicopter bh){
			PatrolHelicopterAI phai = bh.GetComponent<PatrolHelicopterAI>();
			if(phai!=null && AIHelis.Contains(phai)){
				AIHelis.Remove(phai);
			}
		}
		void OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId, int numberOfTransactions)
		{
			
			if(machine is InvisibleVendingMachine){
				if(machine.transform.parent==null && (machine as InvisibleVendingMachine).GetNPCShopKeeper()!=null){
					
					int amount = 0;
					ProtoBuf.VendingMachine.SellOrder so = machine.sellOrders.sellOrders[sellOrderId];
					amount = (so.currencyID==scrap?so.currencyAmountPerItem:so.itemToSellAmount);
					config.factionBank[BaseCombatEntity.Faction.Bandit]+=amount;
					changeScore(buyer, BaseCombatEntity.Faction.Bandit, amount/250.0f,0.25f);
					return;
				}else{
					HumanNPC hn = machine.transform.parent.gameObject.GetComponent<HumanNPC>();
					if(hn==null){return;}
					int amount = 0;
					ProtoBuf.VendingMachine.SellOrder so = machine.sellOrders.sellOrders[sellOrderId];
					amount = (so.currencyID==scrap?so.currencyAmountPerItem:so.itemToSellAmount);
					config.factionBank[hn.faction]+=amount;
					changeScore(buyer, hn.faction, amount/250.0f,0.25f);
				}
			}else{
				int amount = 0;
				Vector3Int vi = posToGrid(machine.transform.position);
				GridCell gc = new GridCell();				
				byte[] y = new byte[9];
				y[0]=cellBucket[vi.x,vi.z,0];
				y[1]=cellBucket[vi.x,vi.z,1];
				y[2]=cellBucket[vi.x,vi.z,2];
				y[3]=cellBucket[vi.x,vi.z,3];
				y[4]=cellBucket[vi.x,vi.z,4];
				y[5]=cellBucket[vi.x,vi.z,5];
				y[6]=cellBucket[vi.x,vi.z,6];
				y[7]=cellBucket[vi.x,vi.z,7];
				y[8]=cellBucket[vi.x,vi.z,8];
				gc.unpack(y);
				if(gc.banCount>gc.sciCount){
					config.factionBank[BaseCombatEntity.Faction.Bandit]+=amount;
					changeScore(buyer, BaseCombatEntity.Faction.Bandit, amount/500.0f,0.25f);
					
				}else if (gc.sciCount>gc.banCount){
					config.factionBank[BaseCombatEntity.Faction.Scientist]+=amount;
					changeScore(buyer, BaseCombatEntity.Faction.Scientist, amount/500.0f,0.25f);
					
				}else{
					config.factionBank[buyer.faction]+=amount;
					changeScore(buyer, (buyer.faction!=BaseCombatEntity.Faction.Default?buyer.faction:BaseCombatEntity.Faction.Player), (buyer.faction!=BaseCombatEntity.Faction.Default?amount/500f:-amount/1000.0f),0.25f);
					
				}
				
			}
			return;
		}
		
		void OnEntitySpawned(CH47Helicopter entity){
			entity.faction = BaseCombatEntity.Faction.Scientist;
			Puts("Spawned ch47");
		}
		
		void OnCorpsePopulate(HumanNPC hn, NPCPlayerCorpse co){
			if(hn.Brain!=null&&gridAgents.ContainsKey(hn.Brain)){
				if(gridAgents[hn.Brain].currentTarget!=null){
					if(gridAgents[hn.Brain].currentTarget.assignedAgents.Contains(hn.Brain)){
						gridAgents[hn.Brain].currentTarget.assignedAgents.Remove(hn.Brain);
					}
				}
				gridAgents.Remove(hn.Brain);
			}
			if(hn.gameObject.GetComponentInChildren<InvisibleVendingMachine>()!=null){
				hn.gameObject.GetComponentInChildren<InvisibleVendingMachine>().Kill();
			}
		}
		#endregion
	#region DirPos functions
		public BaseEntity getLookingAt(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
			}
			return null;
		}
		static bool RadialPoint(out Vector3 outvect, Vector3 target, Vector3 self,float minDist = 5,float maxDist=8){
				bool destRes = false;
				float dist = UnityEngine.Random.Range(minDist,maxDist);
				float angle =UnityEngine.Random.Range(-360f,360f);
				float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
				float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
				Vector3 newPosition = target;
				newPosition.x += x;
				newPosition.z += y;
				newPosition.y = TerrainMeta.HeightMap.GetHeight(newPosition);
				outvect = newPosition;
				return true;
			
		}
		static bool RadialPoint(BaseNavigator nav, Vector3 target, Vector3 self,float minDist = 5,float maxDist=8){
				bool destRes = false;
				float dist = UnityEngine.Random.Range(minDist,maxDist);
				float angle = (180+nav.transform.eulerAngles.y) + UnityEngine.Random.Range(-60f,60f);
				float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
				float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
				Vector3 newPosition = target;
				newPosition.x += x;
				newPosition.z += y;
				newPosition.y = TerrainMeta.HeightMap.GetHeight(newPosition);
				//newPosition.y = Terrain.activeTerrain.SampleHeight(newPosition);
				float distance = Vector3.Distance(newPosition,self);
				if (distance<5f){
					 destRes=nav.SetDestination(newPosition, global::BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
					return destRes;
					}
				else if (distance<25f){
					 destRes=nav.SetDestination(newPosition, global::BaseNavigator.NavigationSpeed.Normal, 0f, 0f);
					return destRes;
					}
				else{
					destRes=nav.SetDestination(newPosition, global::BaseNavigator.NavigationSpeed.Fast, 0f, 0f);							
					return destRes;
					}							
				return destRes;	
			}
		#endregion
	
		
	System.Random rng = new System.Random(); 
	private void Shuffle(List<EconomyEntry> list,int seed)  
	{  
		int n = list.Count;  
		while (n > 1) {  
			n--;  
			int k = rng.Next(n + 1);  
			EconomyEntry v = list[k];  
			list[k] = list[n];  
			list[n] = v;  
		}  
	}
	private void Shuffle(UnityEngine.Object[] list,int seed)  
	{  
		int n = list.Length;  
		while (n > 1) {  
			n--;  
			int k = rng.Next(n + 1);  
			UnityEngine.Object v = list[k];  
			list[k] = list[n];  
			list[n] = v;  
		}  
	}
	private System.Object[] Shuffle(System.Object[] list,int seed)  
	{  
		int n = list.Length;  
		while (n > 1) {  
			n--;  
			int k = rng.Next(n + 1);  
			System.Object v = list[k];  
			list[k] = list[n];  
			list[n] = v;  
		}  
		return list;
	}
	

	}
}