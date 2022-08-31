/*README:

Chat Commands:
/clearorbit Removes the orbit points (MonumentInfo) that are actively loaded
/clearLZ Removes active landing zones
/clearDZ Removes active drop zones
/setorbit sets an orbit points
/setlz sets a landing zones
/setdz sets a drop zone

Config:
CustomOnly - removes all the built-in orbit and drop points, only loading the points you set
Land - unimplemented - creates landing zones defined in config
Drop - unimplemented - creates drop points defined in config
Orbit - unimplemented - creates orbit points in config

the other three - vector lists, best to leave them alone and use chat commands to set the points, but since there's no way to delete points right now it's handy for cleanup

*/


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
	[Info("Dropzone", "obsol", "0.0.1")]
	[Description("Creates/Deletes defined CH47 drop, orbit, and landing zones")]
	public class Dropzone : RustPlugin{
	
		public class Configuration{
			[JsonProperty("CustomOnly", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool CustomOnly=false;
			[JsonProperty("Land", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool Land=true;
			[JsonProperty("Drop", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool Drop=true;
			[JsonProperty("Orbit", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool Orbit=true;
			[JsonProperty("DropPoints", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public List<Vector3> DropPoints = new List<Vector3>();
			[JsonProperty("LandingZones", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public List<Vector3> LandingZone = new List<Vector3>();
			[JsonProperty("OrbitPoints", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public List<Vector3> OrbitPoints = new List<Vector3>();
			
			
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		Configuration config = new Configuration();
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
			}
			catch{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
				
			}
		}
		protected override void SaveConfig(){
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		List<CH47DropZone> customDZ = new List<CH47DropZone>();
		List<CH47LandingZone> customLZ = new List<CH47LandingZone>();
		List<MonumentInfo> customOrbit = new List<MonumentInfo>();

		void OnServerInitialized(){
			LoadConfig();
			if(config.CustomOnly){
				clearLZ();
				clearDZ();
				clearOrbit();
			}
			if(config.Drop){
				foreach(Vector3 v in config.DropPoints){
					setDZ(v);
				}
			}
			if(config.Land){
				foreach(Vector3 v in config.LandingZone){
					setLZ(v);
				}
			}
			if(config.Orbit){
				foreach(Vector3 v in config.OrbitPoints){
					setOrbit(v);
				}
			}
		}
		[ChatCommand("ClearLZ")] void ClearLZ(BasePlayer player, string command, string[] args){
			clearLZ();
		}
		[ChatCommand("ClearDZ")] void ClearDZ(BasePlayer player, string command, string[] args){
			clearDZ();
		}
		[ChatCommand("ClearOrbit")] void ClearOrbit(BasePlayer player, string command, string[] args){
			clearOrbit();
		}
		
		[ChatCommand("SetLZ")] void SetLZ(BasePlayer player, string command, string[] args){
			setLZ(player.transform.position);
			config.DropPoints.Add(player.transform.position);
			SaveConfig();
			SendReply(player,"Landing Zone created at "+(player.transform.position.ToString()));	
			
	
		}
		[ChatCommand("SetDZ")] void SetDZ(BasePlayer player, string command, string[] args){
			setDZ(player.transform.position);
			config.DropPoints.Add(player.transform.position);
			SaveConfig();
			SendReply(player,"Dropzone created at "+(player.transform.position.ToString()));


		}
		[ChatCommand("SetOrbit")] void SetOrbit(BasePlayer player, string command, string[] args){
			setOrbit(player.transform.position);
			config.OrbitPoints.Add(player.transform.position);
			SaveConfig();
			SendReply(player,"Orbit created at "+(player.transform.position.ToString()));
		}
		void OnEntitySpawned(CH47Helicopter entity){
			entity.faction = BaseCombatEntity.Faction.Scientist;
			Puts("Spawned ch47");
		}

		void setLZ(Vector3 position){
			GameObject go = new GameObject("CustomLandzone");
			go.transform.position=position;
			CH47LandingZone cdz = go.AddComponent(typeof (CH47LandingZone)) as CH47LandingZone;
			cdz.Awake();		
			customLZ.Add(cdz);
			
		}
		void setDZ(Vector3 position){
			GameObject go = new GameObject("CustomDropzone");
			go.transform.position=position;
			CH47DropZone cdz = go.AddComponent(typeof (CH47DropZone)) as CH47DropZone;
			cdz.Awake();	
			customDZ.Add(cdz);		
		}
		void setOrbit(Vector3 position){
			GameObject go = new GameObject("CustomOrbit");
			go.transform.position=position;
			MonumentInfo mi = go.AddComponent(typeof(MonumentInfo)) as MonumentInfo;
			mi.displayPhrase = new Translate.Phrase("","Custom Orbit");
			mi.shouldDisplayOnMap = true;
			mi.Bounds = new Bounds(position,new Vector3(50,20,50));
			mi.HasNavmesh=false;
			mi.IsSafeZone=false;
			mi.Tier = (MonumentTier)7;
			customOrbit.Add(mi);
			
		}
	
		void clearLZ(){			
			foreach(CH47LandingZone s in GameObject.FindObjectsOfType<CH47LandingZone>()){
				GameObject.Destroy(s);
				
			}
		}
		void clearDZ(){
			foreach(CH47DropZone s in GameObject.FindObjectsOfType<CH47DropZone>()){
				GameObject.Destroy(s);
			}			
		}
		void clearOrbit(){
			TerrainMeta.Path.Monuments.Clear();	
		}
		
		
		
		public BaseEntity getLookingAt(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
			}
			return null;
		}	
	}
	
}