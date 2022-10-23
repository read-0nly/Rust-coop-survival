using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Firewalk", "obsol", "0.0.1")]
    [Description("Campfires are also portals - each one you light gets added to the list and it works like a ring. Light it and walk in to be warped to another lit campfire on the map. Holding a torch lets you walk the subring of the creator of the fire and if it's lit it goes backwards.")]
    class Firewalk : RustPlugin
    {
		List<BaseOven> fires = new List<BaseOven>();
		Dictionary<BasePlayer,long> playerTimeouts = new Dictionary<BasePlayer,long>();
		Dictionary<ulong,List<BaseOven>> playerFires = new Dictionary<ulong,List<BaseOven>>();
		public long timeout = 5000;
		private Configuration config;
		class Configuration{
			[JsonProperty("timeoutTime", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public long timeoutTime = 30;
			[JsonProperty("cooldownTime", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public long cooldownTime = 5;
			
			[JsonProperty("protectionTimeouts", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,long> protectionTimeouts = new Dictionary<string,long>();
			[JsonProperty("protectionCooldowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,long> protectionCooldowns = new Dictionary<string,long>();
			[JsonProperty("protectionState", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,bool> protectionState = new Dictionary<string,bool>();
			[JsonProperty("buildingRefreshers", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, List<string>> buildingRefreshers = new Dictionary<string, List<string>>();
			[JsonProperty("refresherBuildings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, List<string>> refresherBuildings = new Dictionary<string, List<string>>();					
			
			
			public Dictionary<BaseCombatEntity.Faction, Dictionary<string,Vector3>> newFactionSpawns = new Dictionary<BaseCombatEntity.Faction, Dictionary<string,Vector3>>();
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		protected override void LoadDefaultConfig() => config = new Configuration();
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (! config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					SaveConfig();}
			}
			catch{
				LoadDefaultConfig();}
		}
		protected override void SaveConfig(){
			Config.WriteObject(config, true);
		}
	
		void OnServerInitialized()
		{
			 BaseOven[] components = GameObject.FindObjectsOfType<BaseOven>();
			 foreach(BaseOven bp in components.ToArray()){
				if(bp.transform.name.Contains("campfire") && bp.IsOn() && bp.FindBurnable()!=null && !fires.Contains(bp)){
					fires.Add(bp);
					if(!playerFires.ContainsKey(bp.OwnerID)){
						playerFires.Add(bp.OwnerID, new List<BaseOven>());						 
					}
					if(!playerFires[bp.OwnerID].Contains(bp)){
						playerFires[bp.OwnerID].Add(bp);
					}
				}
			 }
			 ConVar.AntiHack.flyhack_protection=0;
		}
		object OnOvenToggle(BaseOven oven, BasePlayer player)
		{
			if(!oven.transform.name.Contains("campfire")) return null;
			if( (oven.IsOn())){
				if(fires.Contains(oven))
					fires.Remove(oven);
				if(!playerFires.ContainsKey(oven.OwnerID)){
					playerFires.Add(oven.OwnerID, new List<BaseOven>());						 
				}
				if(playerFires[oven.OwnerID].Contains(oven)){
					playerFires[oven.OwnerID].Remove(oven);
				}	
			}else{
				fires.Add(oven);
				if(!playerFires.ContainsKey(oven.OwnerID)){
					playerFires.Add(oven.OwnerID, new List<BaseOven>());						 
				}
				if(!playerFires[oven.OwnerID].Contains(oven)){
					playerFires[oven.OwnerID].Add(oven);
				}	
			}
			return null;
		}
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if(info.Initiator==null) return null;
			if(!info.Initiator.transform.name.ToString().Contains("campfire")) return null;
			BaseOven oven = info.Initiator as BaseOven;
			if(!(entity is BasePlayer)) return null;
			BasePlayer bp = (entity as BasePlayer);
			TorchWeapon torch = (bp.GetHeldEntity() as TorchWeapon);
			List<BaseOven> firelist = new List<BaseOven>();
			firelist.AddRange(fires.ToArray());
			if(torch!=null){
				if(playerFires.ContainsKey(oven.OwnerID))
					firelist = playerFires[oven.OwnerID];
				if(torch.HasFlag(BaseEntity.Flags.On)){
					firelist.Reverse();
				}
			}
			if(playerTimeouts.ContainsKey(bp)){
				if(playerTimeouts[bp]<System.DateTime.Now.Ticks){
					playerTimeouts.Remove(bp);
				}
				else{
					return null;
				}
			}
			if(!playerTimeouts.ContainsKey(bp))
				playerTimeouts.Add(bp,System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * timeout ));
			
			bool found = false;
			for(int i = 0; i < firelist.Count()+1;i++){
				BaseOven bo = firelist[i%firelist.Count()];
				if (found && bo.IsOn() && bo.FindBurnable()!=null){
					bp.Teleport(bo.transform.position);
					return null;
				}
				if(bo==info.Initiator){
					found=true;
				}
			}
			return null;
		}//
	}
	
}