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
#endregion
namespace Oxide.Plugins{
	[Info("SurvivalMods", "obsol", "0.0.1")]
	[Description("Various mods and tweaks to bring out the survival aspects")]
	public class SurvivalMods : CovalencePlugin{	
		#region Generic Vars
			[PluginReference]
			private Plugin ImageLibrary;
			private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
			private void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,  "<color=#00ff00>[Survival]</color>", 0, Array.Empty<object>());
			bool debugOnBoot=false;
			private BasePlayer dev;
		#endregion
		#region Configuration
			private Configuration config;
			[Command("surv_saveconfig")]
			private void surv_saveconfig(IPlayer player, string command, string[] args){
				SaveConfig();
			}
			[Command("surv_loadconfig")]
			private void surv_loadconfig(IPlayer player, string command, string[] args){
				LoadConfig();
			}
			
			class Configuration{
				[JsonProperty("envUpdateArmed", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public bool envUpdateArmed = true;
				[JsonProperty("defaultHealth", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int defaultHealth = 50;
				[JsonProperty("maxHealth", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int maxHealth = 150;
				[JsonProperty("defaultCals", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int defaultCals = 50;
				[JsonProperty("maxCals", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int maxCals = 75;
				[JsonProperty("defaultWater", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int defaultWater = 50;
				[JsonProperty("maxWater", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int maxWater = 50;
				[JsonProperty("waterIncrease", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float waterIncrease = 50;
				[JsonProperty("waterStep", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float waterStep = 50;
				[JsonProperty("debugOnBoot", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public bool debugOnBoot = false;
				public string ToJson() => JsonConvert.SerializeObject(this);				
				public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
			}
			
			protected override void LoadDefaultConfig() => config = new Configuration();
			
			protected override void LoadConfig(){
				base.LoadConfig();
				try{
					config = Config.ReadObject<Configuration>();
					if (config == null){
						throw new JsonException();
					}	
					if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
						LogWarning("Configuration appears to be outdated; updating and saving");
						SaveConfig();
					}
				}
				catch{
					LogWarning($"Configuration file {Name}.json is invalid; using defaults");
					LoadDefaultConfig();
				}
				envUpdateArmed=config.envUpdateArmed;
				defaultHealth = config.defaultHealth;
				maxHealth = config.maxHealth;
				defaultCals = config.defaultCals;
				maxCals = config.maxCals;
				defaultWater = config.defaultWater;
				maxWater = config.maxWater;
				waterIncrease = config.waterIncrease;
				waterStep = config.waterStep;
				debugOnBoot=config.debugOnBoot;
			}
			
			protected override void SaveConfig(){
				LogWarning($"Configuration changes saved to {Name}.json");
				config.envUpdateArmed=envUpdateArmed;
				config.defaultHealth = defaultHealth;
				config.maxHealth = maxHealth;
				config.defaultCals = defaultCals;
				config.maxCals = maxCals;
				config.defaultWater = defaultWater;
				config.maxWater = maxWater;
				config.waterIncrease = waterIncrease;
				config.waterStep = waterStep;
				config.debugOnBoot=debugOnBoot;
				Config.WriteObject(config, true);
			}
		#endregion Configuration
		#region PlayerMods
			int defaultHealth = 50;
			int maxHealth = 150;
			int defaultCals = 75;
			int maxCals = 400;
			bool CanPickupEntity(BasePlayer player, BaseEntity entity){return false;}		
			private void OnPlayerRespawned(BasePlayer player){			
				setDefaults(player);
				player.blueprints.Reset();
			}			
			void setDefaults(BasePlayer player){
				player._maxHealth = defaultHealth;						
				player.metabolism.hydration.max = defaultWater;
				player.metabolism.calories.max = defaultCals;
			}
			void OnItemUse(Item item, int amountToUse){
				string ItemToEat = item.info.shortname.ToString();
				if (ItemToEat == null){return;}
				ItemContainer Container = item.GetRootContainer();
				if (Container == null){return;}
				if(ItemToEat.ToLower().Contains("cactus")){
						//
				}
			}	
			void PlayerMetabolismTick(){
				foreach (global::BasePlayer basePlayer in global::BasePlayer.activePlayerList.ToArray()){					
					if(basePlayer.metabolism.calories.value < 10 && basePlayer.metabolism.calories.max > 20){
						basePlayer.metabolism.calories.max+= -1.5f;									
					}else if (basePlayer.metabolism.calories.value<20 && basePlayer.metabolism.calories.max > 30){
						basePlayer.metabolism.calories.max+= -0.5f;
					}else if (basePlayer.metabolism.calories.max-basePlayer.metabolism.calories.value<10){
						basePlayer.metabolism.calories.max+= (basePlayer.metabolism.calories.max < maxCals?0.5f:0);				
					}				
					basePlayer.metabolism.hydration.max+= (basePlayer.metabolism.hydration.max < maxWater?0.1f:0);
					basePlayer._maxHealth+= (basePlayer._maxHealth < maxHealth?((0.005f*(maxHealth-basePlayer._maxHealth))*(0.005f*(maxHealth-basePlayer._maxHealth))):0);
				}			
			}
		#endregion
		#region EnviroMods
			private bool envUpdateArmed=false;
			int defaultWater = 100;
			int maxWater = 500;
			float waterIncrease = 0f;
			float waterStep = 0.5f;
			void flushForestTopo(){
				//blocks resource spawn - plant hemp to grow trees
				for(int x = 0; x<1024; x++){
					for(int y = 0; y<1024; y++){
						try{
							TerrainMeta.TopologyMap.RemoveTopology(x,y,32); //32
							TerrainMeta.TopologyMap.AddTopology(x,y,2); //262144
							TerrainMeta.TopologyMap.AddTopology(x,y,262144); //262144
						}
						catch{};
					}
				}		
			}
			void PlantTree(GrowableEntity plant, string prefabName){
				BaseEntity entity = GameManager.server.CreateEntity(prefabName, plant.transform.position, Quaternion.identity);
				if (entity == null) return;
				entity.Spawn();
				plant?.Kill();
				
			}
			void updateOcean(){
				if(ConVar.Env.time > 23 && envUpdateArmed){
					envUpdateArmed=false;
					WaterSystem.OceanLevel+=waterIncrease;
					waterIncrease=waterStep;					
					foreach (global::BasePlayer basePlayer in global::BasePlayer.activePlayerList.ToArray()){
						if(basePlayer == null){return;}
						if(basePlayer.IsConnected == false){return;}
						basePlayer.Kick("Night Cycle - Reconnect to wake up!");
					}					
					BaseBoat[] components = GameObject.FindObjectsOfType<BaseBoat>();
					foreach (BaseBoat boat in components){
						boat.WakeUp();
						if(boat.gameObject.GetComponent<MotorRowboat>()==null){break;}
						MotorRowboat mrb = boat.gameObject.GetComponent<MotorRowboat>();//
						if (mrb.IsFlipped()){
							mrb.transform.Rotate(180f,0,0,Space.Self);
						}
					}
					ConVar.Env.time = 5;
					envUpdateArmed=true;
				}
			}
			void growTrees(){
				List<GrowableEntity> list = new List<GrowableEntity>(Resources.FindObjectsOfTypeAll<GrowableEntity>());
				foreach (GrowableEntity growableEntity in list){
					if(growableEntity.transform.name == "assets/prefabs/plants/hemp/hemp.entity.prefab"){						
						if(growableEntity.State.ToString() == "Dying"){
							string tree = "assets/bundled/prefabs/autospawn/resource/v3_temp_field/birch_tiny_temp.prefab";
							PlantTree(growableEntity,tree);
						}
					}
				}
			}
		#endregion
		private void OnServerInitialized(){			
			LoadConfig();			
			flushForestTopo();			
            timer.Every(53f, () => {updateOcean();});			
            timer.Every(11f, () => {PlayerMetabolismTick();});			
            timer.Every(73f, () => {growTrees();});
		}
	}
}