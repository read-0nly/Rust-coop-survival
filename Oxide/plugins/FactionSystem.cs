
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
	[Info("FactionSystem", "obsol", "0.1.1")]
	[Description("AI Rewrite - bandit, scientist, animal, and unaffiliated faction, squad and faction control, animal squadding, in-game zone definition")]
	public class FactionSystem : CovalencePlugin{	
		#region Generic Vars
			private static Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
			private static void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,  "<color=#00ff00>[FactionSystem]</color>", 0, Array.Empty<object>());
			static Dictionary<string,AIInformationZone> ActiveAIZ = new Dictionary<string,AIInformationZone>();
			private static Dictionary<BaseAIBrain<BaseAnimalNPC>, float> waitTimes = new Dictionary<BaseAIBrain<BaseAnimalNPC>, float>();
			static Dictionary<NPCPlayer, AIInformationZone> AssignedAgents = new Dictionary<NPCPlayer, AIInformationZone>();
			static Dictionary<BaseCombatEntity.Faction, Dictionary<Oxide.Ext.RustEdit.NPC.NPCSpawner, BaseCombatEntity>> SpawnPointBank = new Dictionary<BaseCombatEntity.Faction, Dictionary<Oxide.Ext.RustEdit.NPC.NPCSpawner, BaseCombatEntity>>();
			static Dictionary<BasePlayer, HashSet<BaseCombatEntity>> AIZSquads = new Dictionary<BasePlayer, HashSet<BaseCombatEntity>>();
			static ConVar.Admin.ServerInfoOutput serverInfo ;
			static float AIZSwapRate = 0.01f;
			static float SwapWanderRate = 0.1f;
			static int popLimit = 50;
		#endregion
		#region Configuration
			private Configuration config;
			class Configuration{
				[JsonProperty("newScores", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>> newScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
				[JsonProperty("pointGroups", ObjectCreationHandling = ObjectCreationHandling.Replace)]		
				public Dictionary<string,Dictionary<string,List<Vector3>>> pointGroups = new Dictionary<string,Dictionary<string,List<Vector3>>>();
				[JsonProperty("factionBanks", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
				public Dictionary<BaseCombatEntity.Faction, int> factionBank = new Dictionary<BaseCombatEntity.Faction, int>();
				//[JsonProperty("factionSpawns", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
				//public Dictionary<BaseCombatEntity.Faction, List<Vector3>> factionSpawns = new Dictionary<BaseCombatEntity.Faction, List<Vector3>>();
				[JsonProperty("newFactionSpawns", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
				public Dictionary<BaseCombatEntity.Faction, Dictionary<string,Vector3>> newFactionSpawns = new Dictionary<BaseCombatEntity.Faction, Dictionary<string,Vector3>>();
				public string ToJson() => JsonConvert.SerializeObject(this);				
				public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
			}
				public int spawnCost = 500;
				public int spawnerCost = 250;
				public int pointCost = 250;
				public int spawnThreshold = 2000;
				public float spawnTimeout = 600;
				public float roamChance = 0.7f;
			protected override void LoadDefaultConfig() => config = new Configuration();
			protected override void LoadConfig(){
				base.LoadConfig();
				try{
					config = Config.ReadObject<Configuration>();
					if (config == null) throw new JsonException();
					if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
						LogWarning("Configuration appears to be outdated; updating and saving");SaveConfig();}
				}
				catch{LogWarning($"Configuration file {Name}.json is invalid; using defaults");LoadDefaultConfig();}
			}
			protected override void SaveConfig(){
				LogWarning($"Configuration changes saved to {Name}.json");
				Config.WriteObject(config, true);
			}
			
			Cordyceps cordy;
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
				targets = new string[]{"assets/rust.ai/agents/bear/bear.prefab", 
				"assets/rust.ai/agents/boar/boar.prefab",      
				"assets/rust.ai/agents/wolf/wolf.prefab",      
				"assets/rust.ai/agents/stag/stag.prefab"
				};
				foreach(string s in targets){
					FactionAnimalIdleState fcs= new FactionAnimalIdleState();
					FactionAnimalRoamState fcss= new FactionAnimalRoamState();
					FactionAnimalChaseState fbfps= new FactionAnimalChaseState();
					cordy.AssignAnimalState(s, fcs.StateType, fcs);
					cordy.AssignAnimalState(s, fcss.StateType, fcss);
					cordy.AssignAnimalState(s, fbfps.StateType, fbfps);
				}
				
			}
			void Loaded(){
				
				cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
				cordy.WalkableOnly = false;
				loadStates();
				
				Subscribe(nameof(CanFactionBuild));
			}
			
			void Init(){
				permission.RegisterPermission("factionsystem.squad", this);
				permission.RegisterPermission("factionsystem.command", this);
				permission.RegisterPermission("factionsystem.zoning", this);
			}
			void Unload(){
				SaveConfig();
				foreach(BaseCombatEntity s in GameObject.FindObjectsOfType<BaseCombatEntity>()){
					if(s.transform.name.Contains("AIZ Spawner"))s.Kill();//
				}
			}
			void OnServerInitialized(){
				LoadConfig();
				serverInfo = ConVar.Admin.ServerInfo();
				if(!config.pointGroups.ContainsKey(serverInfo.Map))
					config.pointGroups.Add(serverInfo.Map, new Dictionary<string, List<Vector3>>());
				foreach(Sled s in GameObject.FindObjectsOfType<Sled>()){
					s.Kill();
				}
				foreach(AIInformationZone fc in GameObject.FindObjectsOfType<AIInformationZone>()){
					if(fc.transform.name.Contains("[AIZ]")){
						if(ActiveAIZ.ContainsKey(fc.transform.name.Replace(" [AIZ]",""))){
							ActiveAIZ.Remove((fc.transform.name.Replace(" [AIZ]","")));
						}
						fc.gameObject.GetComponent<MapMarkerGenericRadius>()?.DestroyShared();
						fc.gameObject.GetComponent<MapMarkerGenericRadius>()?.Kill();
						GameObject.Destroy(fc.gameObject);
					}
				}
				timer.Once(spawnTimeout,() =>{
					timer.Every(spawnTimeout, () => {
						respawnFromBank();
					});
				});
				initAIZ();
				initSpawnPoints();
				respawnFromBank();
			}
		#endregion Configuration
		#region Faction logic		
			#region Utility
			/*
				//more dictionaries, use structs to keep things cleané
				//on deploy apply faction to item - on attack, score shift - on destroy, score shift
				//bigger scoreshift on death
				//if target is owner, chill
				//when holding food, animals won't add you as a target if they have no owner
				//giving food makes you ownerplayer

			*/
			bool? CanPickupEntity(BasePlayer player, BaseEntity entity){
				
				if(entity.name.Contains("skullspikes" )){
					removeSpawnPoint(player.faction, entity.transform.position,(BaseCombatEntity)entity);
				}
				return null;
			}		
			public void respawnFromBank(){
				if(!config.factionBank.ContainsKey(BaseCombatEntity.Faction.Bandit))
					config.factionBank[BaseCombatEntity.Faction.Bandit]=0;
				if(config.factionBank[BaseCombatEntity.Faction.Bandit]>spawnThreshold && (Resources.FindObjectsOfTypeAll(typeof(BanditGuard)).Length < popLimit)){
					if(!SpawnPointBank.ContainsKey(BaseCombatEntity.Faction.Bandit))
						SpawnPointBank.Add(BaseCombatEntity.Faction.Bandit, new Dictionary<Oxide.Ext.RustEdit.NPC.NPCSpawner, BaseCombatEntity>());
					else{		
						if((UnityEngine.Random.Range(0,popLimit)) < SpawnPointBank[BaseCombatEntity.Faction.Bandit].Keys.ToList().Count()+(popLimit/3)){
							List<Oxide.Ext.RustEdit.NPC.NPCSpawner> spawners = SpawnPointBank[BaseCombatEntity.Faction.Bandit].Keys.ToList();
							if(spawners.Count()>0){
								config.factionBank[BaseCombatEntity.Faction.Bandit]+=-spawnCost;
								spawners[UnityEngine.Random.Range(0,spawners.Count())].SpawnNPC();
							}
						}
					}
				}		
				if(!config.factionBank.ContainsKey(BaseCombatEntity.Faction.Scientist))
					config.factionBank[BaseCombatEntity.Faction.Scientist]=0;
				if(config.factionBank[BaseCombatEntity.Faction.Scientist]>spawnThreshold&& (Resources.FindObjectsOfTypeAll(typeof(ScientistNPC)).Length < popLimit*2)){
					if(!SpawnPointBank.ContainsKey(BaseCombatEntity.Faction.Scientist))
						SpawnPointBank.Add(BaseCombatEntity.Faction.Scientist, new Dictionary<Oxide.Ext.RustEdit.NPC.NPCSpawner, BaseCombatEntity>());
					else{		
						if(UnityEngine.Random.Range(0,popLimit*2) < SpawnPointBank[BaseCombatEntity.Faction.Scientist].Keys.ToList().Count()+(popLimit*2/3)){
							List<Oxide.Ext.RustEdit.NPC.NPCSpawner> spawners = SpawnPointBank[BaseCombatEntity.Faction.Scientist].Keys.ToList();
							if(spawners.Count()>0){
								config.factionBank[BaseCombatEntity.Faction.Scientist]+=-spawnCost;
								spawners[UnityEngine.Random.Range(0,spawners.Count())].SpawnNPC();
							}
						}
					}
				}					
			}
			public BaseEntity getLookingAt(BasePlayer player){			
				RaycastHit hit;
				if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
					var entity = hit.GetEntity();
					if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
				}
				return null;
			}
			public void GoHome(string faction, float fraction = 1f, BasePlayer bp = null){
				foreach(HumanNPC hn in GameObject.FindObjectsOfType<HumanNPC>()){
					if(bp!=null){	
						if(hn.Brain.OwningPlayer==bp){
							hn.VirtualInfoZone = findNearestAIZ(hn.spawnPos, hn.faction);
							hn.Brain.Navigator.Path = hn.VirtualInfoZone.paths[0];
							if(hn.Brain.CurrentState is FactionBaseFollowPathState){
								((FactionBaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
								((FactionBaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
							}
							hn.Brain.OwningPlayer=null;
						}
						if(fraction==0f){continue;}
					}
					if(hn.faction.ToString()==faction){		
						if((fraction==1f?true:(UnityEngine.Random.Range(0f,1f)<fraction))){
							hn.VirtualInfoZone = findNearestAIZ(hn.spawnPos, hn.faction);
							if(hn.VirtualInfoZone.paths.Count()>0){
								hn.Brain.Navigator.Path = hn.VirtualInfoZone.paths[0];
								if(hn.Brain.CurrentState is FactionBaseFollowPathState){
									((FactionBaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
									((FactionBaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
								}
							}else
								hn.VirtualInfoZone=null;
						}
					}
				}
			}
			public void SwitchFactionToPath(AIInformationZone aiz, string faction, float fraction = 1f, BasePlayer bp = null, bool wander = false){			
				foreach(HumanNPC hn in GameObject.FindObjectsOfType<HumanNPC>()){
					if(bp!=null){	
						if(hn.Brain.OwningPlayer==bp){
							if(wander){
								hn.VirtualInfoZone = null;
								hn.Brain.Navigator.Path = null;
								if(hn.Brain.CurrentState is FactionBaseFollowPathState || hn.Brain.CurrentState is FactionBaseRoamState){
									((FactionBaseFollowPathState)hn.Brain.CurrentState).path = null;
									((FactionBaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=0;
								}
								hn.Brain.OwningPlayer=null;
							}else{
								hn.VirtualInfoZone = aiz;
								hn.Brain.Navigator.Path = hn.VirtualInfoZone.paths[0];
								if(hn.Brain.CurrentState is FactionBaseFollowPathState ){
									((FactionBaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
									((FactionBaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
								}						
								if(hn.Brain.CurrentState is FactionBaseRoamState){
									((FactionBaseRoamState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
									((FactionBaseRoamState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
								}									
								hn.Brain.OwningPlayer=null;
							}
						}
						if(fraction==0f){continue;}
					}
					if( hn.faction.ToString()==faction){		
						if((fraction==1f?true:(UnityEngine.Random.Range(0f,1f)<fraction))){
							if(wander){
								hn.VirtualInfoZone = null;
								hn.Brain.Navigator.Path = null;
								if(hn.Brain.CurrentState is FactionBaseFollowPathState){
									((FactionBaseFollowPathState)hn.Brain.CurrentState).path = null;
									((FactionBaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=0;
								}
							}else{
								hn.VirtualInfoZone = aiz;
								hn.Brain.Navigator.Path = hn.VirtualInfoZone.paths[0];
								if(hn.Brain.CurrentState is FactionBaseFollowPathState){
									((FactionBaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
									((FactionBaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
								}						
								if(hn.Brain.CurrentState is FactionBaseRoamState){
									((FactionBaseRoamState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
									((FactionBaseRoamState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
								}				
							}
						}
					}
				}
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
			static bool RadialPoint(BaseNavigator nav, Vector3 target, Vector3 self){
				bool destRes = false;
				float dist = UnityEngine.Random.Range(5f,8f);
				float angle = (180+nav.transform.eulerAngles.y) + UnityEngine.Random.Range(-30f,30f);
				float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
				float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
				Vector3 newPosition = target;
				newPosition.x += x;
				newPosition.z += y;
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
			void initAnimal(BaseAnimalNPC animal){
				if(animal.IsNpc)Puts(((char)27)+"[96m"+"IsNpc! Did you fix NPCPlayer with dnSpy?"+ (animal.transform.name));
				if(animal.transform.name.Contains("bear") ||animal.transform.name.Contains("boar") ||animal.transform.name.Contains("wolf") ||animal.transform.name.Contains("deer")|| animal.transform.name.Contains("shark")){
					animal.faction = BaseCombatEntity.Faction.Player;	
				}
			}
			bool swapSciRoamState(BasePlayer s){
				s.HasBrain = true;
				if(s.IsNpc)Puts(((char)27)+"[96m"+"IsNpc! Did you fix NPCPlayer with dnSpy?" + (s.transform.name));
				if(s.transform == null)return false;
				if(s.transform.name.ToLower().Contains("scientist")||s.transform.name.ToLower().Contains("apc")||s.transform.name.ToLower().Contains("bradley")) { 
					s.faction = BaseCombatEntity.Faction.Scientist;
				}else if(s.transform.name.ToLower().Contains("bandit")) {
					s.faction = BaseCombatEntity.Faction.Bandit;
				}else  if(s.transform.name.ToLower().Contains("dweller")) {
					s.faction = BaseCombatEntity.Faction.Player;
				}
				try{
					HumanNPC hn = ((HumanNPC)s);
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
					}
					float terraingDiff = hn.transform.position.y - Terrain.activeTerrain.SampleHeight(hn.transform.position);
					
					if(hn.gameObject.transform.name.Contains("bandit_guard")){
						hn.Brain.Navigator.Agent.agentTypeID=-1372625422;
					}
					if(UnityEngine.Random.Range(0.0f,1.0f)>roamChance){
						hn.VirtualInfoZone = findNearestAIZ(hn.spawnPos, hn.faction);
						if(hn.VirtualInfoZone!=null){
							hn.Brain.Navigator.Path = hn.VirtualInfoZone.paths[0];
							if(hn.Brain.CurrentState is FactionBaseFollowPathState){
								((FactionBaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
								((FactionBaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
							}
							if(hn.Brain.CurrentState is FactionBaseRoamState){
								((FactionBaseRoamState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
								((FactionBaseRoamState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
							}
						}
						
					}else{
						hn.VirtualInfoZone=null;
						hn.Brain.Navigator.Path = null;
						if(hn.Brain.CurrentState is FactionBaseFollowPathState){
								((FactionBaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
						}
						if(hn.Brain.CurrentState is FactionBaseRoamState){
								((FactionBaseRoamState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
						}
					}
				
						
					
					((IAISleepable)hn.Brain).WakeAI();
					return true;////
				}catch(Exception e){Puts(e.ToString());}
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
			void initAIZ(){
				Dictionary<string,List<Vector3>>.KeyCollection keys = config.pointGroups[serverInfo.Map].Keys;  
				foreach (string key in keys)  
				{  
					initSingleAIZ(key);
				}  
			}
			bool initSingleAIZ(string key){	
				if(!config.pointGroups[serverInfo.Map].ContainsKey(key))	return false;	
				MapMarkerGenericRadius mapmark=GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab",
						 config.pointGroups[serverInfo.Map][key][0], new Quaternion()) as MapMarkerGenericRadius;	
				mapmark.Spawn();
				mapmark.InitShared();
				mapmark.alpha=0.5f;
				mapmark.radius=1;
				GameObject aiz = mapmark.gameObject;
				aiz.name=key;
				if(ActiveAIZ.ContainsKey(key)){					
					MapMarkerGenericRadius innerMarker = ActiveAIZ[key].gameObject.GetComponent<MapMarkerGenericRadius>();
					innerMarker.alpha=0;
					innerMarker.SendUpdate();
					ActiveAIZ[key].gameObject.GetComponent<MapMarkerGenericRadius>()?.DestroyShared();
					ActiveAIZ[key].gameObject.GetComponent<MapMarkerGenericRadius>()?.Kill();
					GameObject.Destroy(ActiveAIZ[key]);
					ActiveAIZ[key] = aiz.AddComponent<AIInformationZone>();
				}else{
					ActiveAIZ.Add(key, aiz.AddComponent<AIInformationZone>());
				}
				ActiveAIZ[key].bounds =new Bounds(aiz.transform.position,new Vector3(60,20,60));
				aiz.AddComponent<AIInformationGrid>();
				if(key.Contains("Bandit")){
					mapmark.color1=new UnityEngine.Color(0.2f,0,0);
					mapmark.color2=new UnityEngine.Color(1f,0,0);
				}
				if(key.Contains("Scientist")){
					mapmark.color1=new UnityEngine.Color(0,0,0.2f);
					mapmark.color2=new UnityEngine.Color(0,0,1f);
				}
				mapmark.SendUpdate();
				GameObject aizmppgo  = new GameObject();
				aizmppgo.transform.position = config.pointGroups[serverInfo.Map][key][0];				
				AIMovePointPath AIZMPP = aizmppgo.AddComponent<AIMovePointPath>();	
				aizmppgo.transform.parent = ActiveAIZ[key].transform;
				AIZMPP.LoopMode = AIMovePointPath.Mode.Loop;
				foreach(Vector3 v3 in config.pointGroups[serverInfo.Map][key]){
					GameObject go  = new GameObject();
					go.transform.position = v3;
					go.AddComponent<AICoverPoint>();
					AIMovePoint aip = go.AddComponent<AIMovePoint>();
					aip.radius = 5.0f;
					aip.WaitTime = 10f;
					AIZMPP.AddPoint(aip);
					go.transform.parent = ActiveAIZ[key].transform;
				}				
				ActiveAIZ[key].Start();
				ActiveAIZ[key].MarkDirty(true);
				return true;
			}
			static AIInformationZone findNearestAIZ(Vector3 position){
				float lowestDist=9999;
				AIInformationZone result = null;
				foreach(AIInformationZone aiz in ActiveAIZ.Values){
					if(Vector3.Distance(aiz.transform.position,position)<lowestDist){
						lowestDist=Vector3.Distance(aiz.transform.position,position);
						result=aiz;
					}
				}
				return result;
			}
			static AIInformationZone findNearestAIZ(Vector3 position, BaseCombatEntity.Faction faction){
				float lowestDist=9999;
				AIInformationZone result = null;
				foreach(AIInformationZone aiz in ActiveAIZ.Values){
					if(Vector3.Distance(aiz.transform.position,position)<lowestDist &&
						aiz.name.Contains("{"+faction.ToString()+"}")){
						lowestDist=Vector3.Distance(aiz.transform.position,position);
						result=aiz;
					}
				}
				return result;
			}
			static AIInformationZone findNearestAIZ(Vector3 position, AIInformationZone lastZone, BaseCombatEntity.Faction faction = BaseCombatEntity.Faction.Default){
				float lowestDist=9999;
				AIInformationZone result = null;
				if(lastZone==null) return (faction == BaseCombatEntity.Faction.Default?findNearestAIZ(position):findNearestAIZ(position,faction));
				foreach(AIInformationZone aiz in ActiveAIZ.Values){
					if(Vector3.Distance(aiz.transform.position,position)<lowestDist && (faction == BaseCombatEntity.Faction.Default || aiz.name.Contains("{"+faction.ToString()+"}")) && aiz!=lastZone){
						lowestDist=Vector3.Distance(aiz.transform.position,position);
						result=aiz;
					}
				}
				return result;
			}
			static AIInformationZone findNearestAIZ(Vector3 position, AIInformationZone[] lastZones, BaseCombatEntity.Faction faction = BaseCombatEntity.Faction.Default){
				float lowestDist=9999;
				AIInformationZone result = null;
				if(lastZones==null) return (faction == BaseCombatEntity.Faction.Default?findNearestAIZ(position):findNearestAIZ(position,faction));
				foreach(AIInformationZone aiz in ActiveAIZ.Values){
					if(Vector3.Distance(aiz.transform.position,position)<lowestDist && (faction == BaseCombatEntity.Faction.Default || aiz.name.Contains("{"+faction.ToString()+"}")) && Array.IndexOf(lastZones, aiz)==-1){
						lowestDist=Vector3.Distance(aiz.transform.position,position);
						result=aiz;
					}
				}
				return result;
			}
			void CommandFirework(BaseFirework bf, HitInfo info){
				BasePlayer bp = ((BasePlayer) info.Initiator);
				if(bf.transform.name.Contains("blue")){
					if(ActiveAIZ.ContainsKey(bp.faction.ToString()+" {"+bp.faction.ToString()+"} [AIZ]")){
						if(bf.transform.name.Contains("mortar")){			
							Puts(bf.transform.name);///				
							SendChatMsg(((BasePlayer)info.Initiator), "<color=#4040FF>[ EVERYONE = > GOTO TOWN ]</color>");
							SwitchFactionToPath(ActiveAIZ[bp.faction.ToString()+" {"+
							bp.faction.ToString()+"} [AIZ]"],bp.faction.ToString());
						}else if(bf.transform.name.Contains("candle")){							
							SendChatMsg(((BasePlayer)info.Initiator), "<color=#4040FF>[ HALF = > GOTO TOWN ]</color>");
							SwitchFactionToPath((ActiveAIZ[bp.faction.ToString()+" {"+
							bp.faction.ToString()+"} [AIZ]"]),bp.faction.ToString(),0.5f);
						}else if(bf.transform.name.Contains("volcano")){							
							SendChatMsg(((BasePlayer)info.Initiator), "<color=#4040FF>[ SQUAD = > GOTO TOWN ]</color>");
							SwitchFactionToPath((ActiveAIZ[bp.faction.ToString()+" {"+
							bp.faction.ToString()+"} [AIZ]"]),bp.faction.ToString(),0f,bp);
						}
					}
				}else if(bf.transform.name.Contains("green")){
					if(bf.transform.name.Contains("mortar")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#40FF40>[ EVERYONE = > GOTO SPAWN ]</color>");
						GoHome(bp.faction.ToString());
					}else if(bf.transform.name.Contains("candle")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#40FF40>[ HALF = > GOTO SPAWN ]</color>");
						GoHome(bp.faction.ToString(),0.5f);
					}else if(bf.transform.name.Contains("volcano")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#40FF40>[ SQUAD = > GOTO SPAWN ]</color>");
						GoHome(bp.faction.ToString(),0.0f,bp);
					}
				}else if(bf.transform.name.Contains("red")){
					if(bf.transform.name.Contains("mortar")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#FF4040>[ EVERYONE = > GOTO HERE ]</color>");
						SwitchFactionToPath(findNearestAIZ(bf.transform.position),bp.faction.ToString());
					}else if(bf.transform.name.Contains("volcano")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#FF4040>[ SQUAD = > GOTO HERE ]</color>");
						SwitchFactionToPath(findNearestAIZ(bf.transform.position),bp.faction.ToString(),0f,bp);
					}
				}else if(bf.transform.name.Contains("violet")){
					if(bf.transform.name.Contains("mortar")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#FF40FF>[ EVERYONE = > WANDER ]</color>");
						SwitchFactionToPath(null,bp.faction.ToString(),1.0f,null,true);
					}else if(bf.transform.name.Contains("candle")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#FF40FF>[ HALF = > WANDER ]</color>");
						SwitchFactionToPath(null,bp.faction.ToString(),0.5f,null,true);
					}else if(bf.transform.name.Contains("volcano")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#FF40FF>[ SQUAD = > WANDER ]</color>");
						SwitchFactionToPath(null,bp.faction.ToString(),0.0f,bp,true);
					}
				}else if(bf.transform.name.Contains("/volcanofirework.prefab") &&bf.transform.name.Contains("volcano")){
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#FFFFFF>[ SQUAD = > HOLD ]</color><color=#FF0000>Not Implemented</color>");						
				}else if(bf.transform.name.Contains("/romancandle.prefab")){							
						SendChatMsg(((BasePlayer)info.Initiator), "<color=#FF4040>[ HALF = > GOTO HERE ]</color>");
						SwitchFactionToPath(findNearestAIZ(bf.transform.position),bp.faction.ToString(),0.5f);
					}
				
			}
			void ZoneLantern(SkyLantern sl, HitInfo hitInfo){
				string s = "";
				s+="Sent zoning instruction\r\n";
				Item note = sl.inventory.FindItemsByItemName("note");
				if(!(note!=null && (hitInfo.Initiator is BasePlayer) && note.text !=null)){
					return;
				}
				else if(sl.transform.name.Contains(".green.")){
					note.text+="\r\npoint";
					s+="Create Move Point";
				}
				else if(sl.transform.name.Contains(".orange.")){
					note.text+="\r\npoint";
					s+="Create Cover Point";
				}
				else if(sl.transform.name.Contains(".purple.")){
					note.text+="\r\nload";
					s+="Finalized Zone";

				}
				else if(sl.transform.name.Contains(".red.")){
					note.text+="\r\ndelete";
					s+="Delete";

				}
				
				string[] lines = note.text.ToLower().Split('\n');
				Dictionary<string,string> AIZSettings = new Dictionary<string,string>();
					if(lines!=null&&lines.Count()>1){
						foreach(string line in lines){
							string[] entries = line.Replace("\r","").Split(':');
							if(entries != null && entries.Count()==2)
							{
								if(AIZSettings.ContainsKey(entries[0])){
									AIZSettings[entries[0]]=entries[1];
								}else{
									AIZSettings.Add(entries[0],entries[1]);
								}
							}else if(entries.Count()==1 && entries[0]!="zone"){
								if(AIZSettings.ContainsKey(entries[0])){
									AIZSettings[entries[0]]="";
								}else{
									AIZSettings.Add(entries[0],"");
								}

							}
						}
					}else{
						string[] entries = note.text.ToLower().Split(':');
						if(entries!=null){
							if(entries.Count()==2)
							{
								if(AIZSettings.ContainsKey(entries[0])){
									AIZSettings[entries[0]]=entries[1];
								}else{
									AIZSettings.Add(entries[0],entries[1]);
								}
							}else if(entries.Count()==1 && entries[0]!="zone"){
								if(AIZSettings.ContainsKey(entries[0])){
									AIZSettings[entries[0]]="";
								}else{
									AIZSettings.Add(entries[0],"");
								}

							}
						}
					}
					BaseCombatEntity.Faction faction = ((BaseCombatEntity)hitInfo.Initiator).faction;
					BaseCombatEntity.Faction oppositeFaction = (faction==BaseCombatEntity.Faction.Bandit?
						BaseCombatEntity.Faction.Scientist:BaseCombatEntity.Faction.Bandit);
					if(AIZSettings.ContainsKey("zone")&&AIZSettings.ContainsKey("point")){
						AIInformationZone closestAIZ = findNearestAIZ(hitInfo.Initiator.transform.position);
						if(Vector3.Distance(hitInfo.Initiator.transform.position, closestAIZ.gameObject.transform.position)>30){
							if(config.pointGroups[serverInfo.Map].ContainsKey(AIZSettings["zone"]+" {"+
							faction.ToString()+
							"} [AIZ]"))
								addPointToGroup(AIZSettings["zone"]+" {"+
								faction.ToString()+
								"} [AIZ]", hitInfo.Initiator.transform.position);
							else
								addPointToGroup(AIZSettings["zone"]+" {"+
								oppositeFaction.ToString()+
								"} [AIZ]", hitInfo.Initiator.transform.position);
							s+= "Point Added to "+AIZSettings["zone"]+"\r\n";
						}else{
							s+= "Too close to "+closestAIZ.gameObject.name+"\r\n";
							
						}
					}
					if(AIZSettings.ContainsKey("zone")&&AIZSettings.ContainsKey("load")){
						if(config.pointGroups[serverInfo.Map].ContainsKey(AIZSettings["zone"]+" {"+
						faction.ToString()+
						"} [AIZ]"))
							s+= (initSingleAIZ(AIZSettings["zone"]+" {"+
							faction.ToString()+
							"} [AIZ]")?
						 	"Reloaded "+AIZSettings["zone"]+" {"+
							faction.ToString()+
							"} [AIZ]\r\n":
						 	"Zone undefined (it needs at least one point)\r\n");
						else
							s+= (initSingleAIZ(AIZSettings["zone"]+" {"+
							oppositeFaction.ToString()+
							"} [AIZ]")?
						 	"Reloaded "+AIZSettings["zone"]+" {"+
							updateFactionZone(((BaseCombatEntity)hitInfo.Initiator)).ToString()+
							"} [AIZ]\r\n":
						 	"Zone undefined (it needs at least one point)\r\n");
						SaveConfig();
						//findNearestAIZ(entity.transform.position);
					}
					if(AIZSettings.ContainsKey("zone")&&AIZSettings.ContainsKey("delete")){
						if(ActiveAIZ.ContainsKey(AIZSettings["zone"]+" {"+
						((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
						"} [AIZ]")){
							ActiveAIZ[AIZSettings["zone"]+" {"+
								((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
								"} [AIZ]"].GetComponent<MapMarkerGenericRadius>()?.DestroyShared();
							ActiveAIZ[AIZSettings["zone"]+" {"+
								((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
								"} [AIZ]"].GetComponent<MapMarkerGenericRadius>()?.Kill();
							GameObject.Destroy(ActiveAIZ[AIZSettings["zone"]+" {"+
								((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
								"} [AIZ]"]);
							ActiveAIZ.Remove(AIZSettings["zone"]+" {"+
							((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
							"} [AIZ]"
							);
						}
						if(config.pointGroups[serverInfo.Map].ContainsKey(AIZSettings["zone"]+" {"+
						((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
						"} [AIZ]")){
							config.pointGroups[serverInfo.Map].Remove(AIZSettings["zone"]+" {"+
							((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
							"} [AIZ]"
							);
							s+= "Deleted "+AIZSettings["zone"]+" {"+
							((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
							"} [AIZ]\r\n";
						}
						SaveConfig();
					}
				
				if(hitInfo.Initiator is BasePlayer) SendChatMsg((hitInfo.Initiator as BasePlayer),s);
			}
			void addPointToGroup(string key, Vector3 position){
				
				if(!config.pointGroups[serverInfo.Map].ContainsKey(key))config.pointGroups[serverInfo.Map].Add(key,new List<Vector3>());
				if(config.pointGroups[serverInfo.Map][key]==null) config.pointGroups[serverInfo.Map][key] = new List<Vector3>();
				config.pointGroups[serverInfo.Map][key].Add(position);
			}
			public bool changeScore(BasePlayer bp, BaseCombatEntity.Faction ft, float score, float penaltyScale = 2){
				try{
					if(bp==null)return false;
					if(ft==null)return false;
					if(bp.IsConnected != true) return false;
					ulong id= bp.userID;
					if(config.newScores==null) config.newScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
					if(!config.newScores.ContainsKey(id)) config.newScores.Add(id, new Dictionary<BaseCombatEntity.Faction,float>());
					if(!config.newScores[id].ContainsKey(BaseCombatEntity.Faction.Bandit))config.newScores[id].Add(BaseCombatEntity.Faction.Bandit,0f);
					if(!config.newScores[id].ContainsKey(BaseCombatEntity.Faction.Scientist))config.newScores[id].Add(BaseCombatEntity.Faction.Scientist,0f);
					
					switch(ft){
						case BaseCombatEntity.Faction.Bandit:
							config.newScores[id][BaseCombatEntity.Faction.Bandit]+=score;
							config.newScores[id][BaseCombatEntity.Faction.Scientist]+=-score*penaltyScale;
							break;
						case BaseCombatEntity.Faction.Scientist:
							config.newScores[id][BaseCombatEntity.Faction.Bandit]+=-score*penaltyScale;
							config.newScores[id][BaseCombatEntity.Faction.Scientist]+=score;
							
							break;
						case BaseCombatEntity.Faction.Player:
							config.newScores[id][BaseCombatEntity.Faction.Bandit]+=score*0.25f;
							config.newScores[id][BaseCombatEntity.Faction.Scientist]+=score*0.25f;						
							break;
					}
					return true;
				}catch(Exception e){return false;}
			}
			public BaseCombatEntity.Faction getNativeFaction(BasePlayer bp){		
				if(bp==null)return BaseCombatEntity.Faction.Player;
				if(bp.IsConnected != true) return BaseCombatEntity.Faction.Player;
				ulong id= bp.userID;
				if(config.newScores==null) config.newScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
				if(!config.newScores.ContainsKey(id)) config.newScores.Add(id, new Dictionary<BaseCombatEntity.Faction,float>());
				if(!config.newScores[id].ContainsKey(BaseCombatEntity.Faction.Bandit))config.newScores[id].Add(BaseCombatEntity.Faction.Bandit,0f);
				if(!config.newScores[id].ContainsKey(BaseCombatEntity.Faction.Scientist))config.newScores[id].Add(BaseCombatEntity.Faction.Scientist,0f);				
				if(config.newScores[id][BaseCombatEntity.Faction.Bandit]<0){
					if(config.newScores[id][BaseCombatEntity.Faction.Scientist]<0){
						return BaseCombatEntity.Faction.Player;
					}else{//
						return BaseCombatEntity.Faction.Scientist;						
					}					
				}else{					
					if(config.newScores[id][BaseCombatEntity.Faction.Scientist]<0){
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
			void initSpawnPoints(){
				foreach(BaseCombatEntity s in GameObject.FindObjectsOfType<BaseCombatEntity>()){
					if(s.transform.name.Contains("AIZ Spawner")){
						GameObject.Destroy(s.GetComponent<Oxide.Ext.RustEdit.NPC.NPCSpawner>());
						s.Kill();//
					}
				}
				/*
				if(!config.factionSpawns.ContainsKey(BaseCombatEntity.Faction.Bandit)) 
					config.factionSpawns.Add(BaseCombatEntity.Faction.Bandit,new List<Vector3>());
				if(!config.factionSpawns.ContainsKey(BaseCombatEntity.Faction.Scientist)) 
					config.factionSpawns.Add(BaseCombatEntity.Faction.Scientist,new List<Vector3>());
				foreach(Vector3 position in config.factionSpawns[BaseCombatEntity.Faction.Bandit]){
					spawnSpawnPoint(BaseCombatEntity.Faction.Bandit,position);
				}
				foreach(Vector3 position in config.factionSpawns[BaseCombatEntity.Faction.Scientist]){
					spawnSpawnPoint(BaseCombatEntity.Faction.Scientist,position);					
				}
				/*/
				if(!config.newFactionSpawns.ContainsKey(BaseCombatEntity.Faction.Bandit)) 
					config.newFactionSpawns.Add(BaseCombatEntity.Faction.Bandit,new Dictionary<string,Vector3>());
				if(!config.newFactionSpawns.ContainsKey(BaseCombatEntity.Faction.Scientist)) 
					config.newFactionSpawns.Add(BaseCombatEntity.Faction.Scientist,new Dictionary<string,Vector3>());
				foreach(BaseCombatEntity.Faction faction in config.newFactionSpawns.Keys){
					foreach(string key in config.newFactionSpawns[faction].Keys){
						spawnSpawnPoint(faction,config.newFactionSpawns[faction][key],null,key);
					}
				}

				//*/
			}
			void spawnSpawnPoint(BaseCombatEntity.Faction faction, Vector3 position,BaseCombatEntity entity = null, string id = ""){
					if(!SpawnPointBank.ContainsKey(faction))
						SpawnPointBank.Add(faction, new Dictionary<Oxide.Ext.RustEdit.NPC.NPCSpawner, BaseCombatEntity>());
					SerializedNPCSpawner serializedNPCSpawner = new SerializedNPCSpawner();
					serializedNPCSpawner.npcType=(int)Oxide.Ext.RustEdit.NPC.NPCType.Murderer;
					switch(faction){
						case BaseCombatEntity.Faction.Bandit:
							serializedNPCSpawner.npcType=(int)Oxide.Ext.RustEdit.NPC.NPCType.Bandit;
							break;
						case BaseCombatEntity.Faction.Scientist:
							serializedNPCSpawner.npcType=(int)Oxide.Ext.RustEdit.NPC.NPCType.JunkpileScientist;
							break;
					}
					if(entity==null){//
						entity = GameManager.server.CreateEntity("assets/prefabs/misc/halloween/skull spikes/skullspikes.deployed.prefab",
						 position, new Quaternion()) as BaseCombatEntity;
						entity.Spawn();
						entity.SendNetworkUpdateImmediate(true);
					}
					serializedNPCSpawner.respawnMin=3600;
					serializedNPCSpawner.respawnMax=6000;
					serializedNPCSpawner.position=entity.transform.position;
					serializedNPCSpawner.category="[AIZ Spawner]";
					
					entity.transform.name="[AIZ Spawner]:"+(id==""?DateTime.Now.Ticks.ToString():id);
					Oxide.Ext.RustEdit.NPC.NPCSpawner npcSpawner = entity.gameObject.AddComponent<Oxide.Ext.RustEdit.NPC.NPCSpawner>();
					npcSpawner.Initialize(serializedNPCSpawner);
					((BaseCombatEntity)entity).faction=faction;
					SpawnPointBank[faction].Add(npcSpawner, ((BaseCombatEntity)entity));//
			}
			
			void removeSpawnPoint(BaseCombatEntity.Faction faction, Vector3 position,BaseCombatEntity entity = null){
					if(!SpawnPointBank.ContainsKey(faction))
						SpawnPointBank.Add(faction, new Dictionary<Oxide.Ext.RustEdit.NPC.NPCSpawner, BaseCombatEntity>());
					Oxide.Ext.RustEdit.NPC.NPCSpawner npcSpawner = entity.gameObject.GetComponent<Oxide.Ext.RustEdit.NPC.NPCSpawner>();
					if(npcSpawner!=null)
						SpawnPointBank[faction].Remove(npcSpawner);//
			}
			
			BaseCombatEntity.Faction updateFactionZone(BaseCombatEntity entity){
				BaseCombatEntity.Faction faction = entity.faction;
				BaseCombatEntity.Faction oppositeFaction = (faction==BaseCombatEntity.Faction.Bandit?
					BaseCombatEntity.Faction.Scientist:BaseCombatEntity.Faction.Bandit);
				if(!SpawnPointBank.ContainsKey(oppositeFaction))
					SpawnPointBank.Add(oppositeFaction, new Dictionary<Oxide.Ext.RustEdit.NPC.NPCSpawner, BaseCombatEntity>());
				/*
				if(!config.factionSpawns.ContainsKey(oppositeFaction)) config.factionSpawns.Add(oppositeFaction,new List<Vector3>());
				/*/
				if(!config.newFactionSpawns.ContainsKey(oppositeFaction)) config.newFactionSpawns.Add(oppositeFaction,new Dictionary<string,Vector3>());

				//*/
				AIInformationZone aiz = findNearestAIZ(entity.transform.position);
				if(aiz==null) return BaseCombatEntity.Faction.Default;
				if(!(aiz.name.Split('{')[0].ToLower()==aiz.name.Split('{')[0])) return ((BaseCombatEntity.Faction) Enum.Parse(typeof(BaseCombatEntity.Faction),aiz.name.Split('{')[1].Split('}')[0]));
				bool isFree = true;
				foreach(Oxide.Ext.RustEdit.NPC.NPCSpawner ns in SpawnPointBank[oppositeFaction].Keys){
					if(aiz==findNearestAIZ(ns.transform.position)){
						isFree=false;
					}
				}
				if(isFree) {
					string oldName = aiz.transform.name;
					if(faction==BaseCombatEntity.Faction.Scientist){//////
						aiz.transform.name=aiz.transform.name.Replace("{Bandit}","{Scientist}");
					}
					else if(faction==BaseCombatEntity.Faction.Bandit){
						aiz.transform.name=aiz.transform.name.Replace("{Scientist}","{Bandit}");
					}
					string newName = aiz.transform.name;
					if(oldName!=newName){
						if(config.pointGroups[serverInfo.Map].ContainsKey(newName))
							config.pointGroups[serverInfo.Map][newName]=config.pointGroups[serverInfo.Map][oldName];
						else
							config.pointGroups[serverInfo.Map][newName]=config.pointGroups[serverInfo.Map][oldName];
						config.pointGroups[serverInfo.Map].Remove(oldName);
					}
				}
				MapMarkerGenericRadius mapmark = aiz.GetComponent<MapMarkerGenericRadius>();
				if(mapmark!=null){
					
					if(aiz.name.Contains("Bandit")){
						mapmark.color1=new UnityEngine.Color(0.2f,0,0);
						mapmark.color2=new UnityEngine.Color(1f,0,0);
					}
					if(aiz.name.Contains("Scientist")){
						mapmark.color1=new UnityEngine.Color(0,0,0.2f);
						mapmark.color2=new UnityEngine.Color(0,0,1f);
					}
					/*

If wearing the suit of the other side, NPCs won't 
auto-target, will treat you as Default as long as 
you're not hostile. Something is a little sussy baka 
ne? This also means wild could wander into these spaces
with their pack if they have a suit and let their pack
do the work. If you get spotted close enough to see
the player name, you're kinda screwed.

On equip, add to dictionary baseplayer,faction
on remove, remove
on checks for awareness, if faction is faction, ignore
rare crate drop in custom clothing crate with slow respawn

hazmatsuit_scientist
hazmatsuit_scientist_peacekeeper
attire.banditguard

object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
{
    Puts("CanMoveItem works!");
    return null;
}
					*/
					mapmark.SendUpdate();
				}
				return (isFree? faction:oppositeFaction);
				
			}
			#endregion
			#region Chat commands
			[Command("hz_save")] void surv_save(IPlayer player, string command, string[] args){		
				BasePlayer bp = (BasePlayer)player.Object;
				SaveConfig();
				SendChatMsg(bp,"Saving!");
			}
			[Command("hz_load")] void surv_load(IPlayer player, string command, string[] args){		
				BasePlayer bp = (BasePlayer)player.Object;
				LoadConfig();
				SendChatMsg(bp,"Saving!");
			}
			[Command("hz_stats")] void stats(IPlayer player, string command, string[] args){	
				BasePlayer bp = (BasePlayer)player.Object;
				Puts(SpawnPointBank[BaseCombatEntity.Faction.Bandit].Keys.Count().ToString());				
				Puts(SpawnPointBank[BaseCombatEntity.Faction.Scientist].Keys.Count().ToString());
				SendChatMsg(bp,"Current Score [Bandit]: "+(config.newScores.ContainsKey(bp.userID)?(config.newScores[bp.userID].ContainsKey(BaseCombatEntity.Faction.Bandit)?(config.newScores[bp.userID][BaseCombatEntity.Faction.Bandit].ToString()):"No Bandit Score"):"No Score"));
				SendChatMsg(bp,"Current Score [Scientist]: "+(config.newScores.ContainsKey(bp.userID)?(config.newScores[bp.userID].ContainsKey(BaseCombatEntity.Faction.Scientist)?(config.newScores[bp.userID][BaseCombatEntity.Faction.Scientist].ToString()):"No Scientist Score"):"No Score"));
			}
			[Command("hz_aizswap")] void swap(IPlayer player, string command, string[] args){
				float x = SwapWanderRate;
				float.TryParse(args[0],out x);
				if(!float.IsNaN(x) && x > 0){
					AIZSwapRate = x;
				}
			}
			[Command("hz_aizwander")] void wander(IPlayer player, string command, string[] args){
				float x = AIZSwapRate;
				float.TryParse(args[0],out x);
				if(!float.IsNaN(x) && x > 0){
					SwapWanderRate = x;
				}
			}
			#endregion chatcmds
			#region Faction Initializers
			void OnPlayerRespawned(BasePlayer player)=>initPlayer(player);
			void OnPlayerSleepEnded(BasePlayer player)=>initPlayer(player);
			object OnInitializeAI(BaseAIBrain<HumanNPC> player){swapSciRoamState(player.GetEntity() as HumanNPC); return null;}
			object OnInitializeAI(BaseAIBrain<BaseAnimalNPC> player){initAnimal(player.GetEntity() as BaseAnimalNPC); return null;}
			object OnBradleyApcInitialize(BradleyAPC apc){swapTank((apc));return null;}
			void OnEntitySpawned(BaseNetworkable entity){
				if(entity is ResourceEntity){
					if(entity.gameObject.GetComponent<NavMeshObstacle>()==null){
						NavMeshObstacle nmo = entity.gameObject.AddComponent<NavMeshObstacle>();
						nmo.carving=true;
					}
					
				}else if(entity is HumanNPC){
					if(entity.transform.name.Contains("peacekeeper"))entity.Kill();
				}else if(entity is BaseAnimalNPC){
					
				}else if(entity is NPCAutoTurret){
					if(entity.name.Contains("bandit")){
						(entity as NPCAutoTurret).faction = BaseCombatEntity.Faction.Bandit;
					}else if(entity.name.Contains("scientist")){
						(entity as NPCAutoTurret).faction = BaseCombatEntity.Faction.Scientist;
					}
				}else if(entity is AutoTurret){
					
				}
			}
			object OnPlayerDeath(BasePlayer player, HitInfo info){return null;}
			#endregion initializers
			#region Player Handlers
			object OnDoRespawn(Oxide.Ext.RustEdit.NPC.NPCSpawner npcSpawner){//*/){
				
				if(
				((SpawnPointBank.ContainsKey(BaseCombatEntity.Faction.Bandit)==true) &&
					SpawnPointBank[BaseCombatEntity.Faction.Bandit].Keys.ToList().Contains(npcSpawner))||
				((SpawnPointBank.ContainsKey(BaseCombatEntity.Faction.Scientist)==true) &&
					SpawnPointBank[BaseCombatEntity.Faction.Scientist].Keys.ToList().Contains(npcSpawner)))
				{
					//Puts("Is SelfSpawner. Blocking scheduled respawn");
					return false;
				}
				return null;
			}
			object CanDeployItem(BasePlayer player, Deployer deployer, uint entityId)
			{
				Puts("CanDeployItem works!");
				return null;
			}
			//void OnItemDeployed(Deployer deployer, BaseEntity entity, BaseEntity slotEntity)
			
			private object CanBuild(Planner deployer, Construction construction,Construction.Target target){
				BasePlayer bp = deployer.GetOwnerPlayer();
				if(bp==null) return null;
				if(bp.faction==null) return null;
				if(construction.fullName.Contains("skullspikes" )){
					if(config.factionBank[bp.faction]>spawnerCost){
						if(updateFactionZone(bp) ==bp.faction){
							config.factionBank[bp.faction]-=spawnerCost;
							return null;		
						}
					}
					return false;
				}
				return null;				
			}
			private object CanFactionBuild(Planner deployer, Construction construction,Construction.Target target){
				return CanBuild(deployer,construction,target);
			}
			void OnEntityBuilt(Planner deployer, GameObject entityGo)
			{
				//on place skull spike, add spike to dictionary of spikes and their aiz key "skull spikes/skullspikes"
				BaseCombatEntity entity = entityGo.GetComponent<BaseCombatEntity>();
				if(entity==null) return;
				if(deployer==null) return;
				BasePlayer bp = deployer.GetOwnerPlayer();
				if(bp==null) return;
				if(bp.faction==null) return;
				entity.faction=bp.faction;
				entity.creatorEntity = bp;
				if(entity.name.Contains("skullspikes" )){
					if((bp.faction == BaseCombatEntity.Faction.Scientist || bp.faction==BaseCombatEntity.Faction.Bandit)){
							spawnSpawnPoint(bp.faction, entity.transform.position, entity);
							/*
							if(!config.factionSpawns.ContainsKey(bp.faction))éé config.factionSpawns.Add(bp.faction,new List<Vector3>());
							config.factionSpawns[bp.faction].Add(entity.transform.position);
							/*/
							if(!config.newFactionSpawns.ContainsKey(bp.faction)) config.newFactionSpawns.Add(bp.faction,new Dictionary<string,Vector3>());
							config.newFactionSpawns[bp.faction].Add(entity.transform.name.Split(':')[1],entity.transform.position);
							//*/
							updateFactionZone(entity);
					}
				}
			}
			void OnEntityKill(BaseNetworkable entity)
			{
				if(entity.name.Contains("AIZ Spawner")){
					Oxide.Ext.RustEdit.NPC.NPCSpawner npcSpawner = entity.GetComponent<Oxide.Ext.RustEdit.NPC.NPCSpawner>();
					if(npcSpawner!=null){
						SpawnPointBank[((BaseCombatEntity)entity).faction].Remove(npcSpawner);//
						/*
						config.factionSpawns[((BaseCombatEntity)entity).faction].Remove(entity.transform.position);//
						/*/
						config.newFactionSpawns[((BaseCombatEntity)entity).faction].Remove(entity.transform.name.Split(':')[1]);
						
						//*/
						GameObject.Destroy(npcSpawner);
					}
				}
			}
			void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
			{
				if(newItem==null) return;
				if(newItem.ToString().Contains("torch")){
					GameObject go = findNearestAIZ(player.transform.position).gameObject;
					if(go!=null){
						if(go.name.Contains("[AIZ]"))
							SendChatMsg(player, go.name.Replace("[AIZ]",""));
						else
							SendChatMsg(player, "Unassociated Zone");
					}else{
						SendChatMsg(player, "No Zones set up");
					}
				}
				//SendChatMsg(player, newItem.ToString());
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
							hn.inventory.containerBelt.GetSlot(0).Drop(hn.inventory.containerBelt.playerOwner.GetDropPosition(), hn.inventory.containerBelt.playerOwner.GetDropVelocity(), default(Quaternion));
							if (!item2.MoveToContainer(hn.inventory.containerBelt, 0, true, false))
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
							wi.Kill();
							hn.EquipWeapon();
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
			object OnVendingTransaction(VendingMachine machine, BasePlayer bp, int sellOrderId, int numberOfTransactions)
				{
					AIInformationZone aiz = findNearestAIZ(machine.transform.position);
					string[] faction=aiz.name.Split('{');
					if(faction.Count()!=2) return null;
					faction=faction[1].Split('}');
					if(faction.Count()!=2) return null;
					if( machine.sellOrders.sellOrders[sellOrderId].currencyID == -932201673){
						int scrapCount = machine.sellOrders.sellOrders[sellOrderId].currencyAmountPerItem*numberOfTransactions;
						BaseCombatEntity.Faction oldFaction = bp.faction;
						switch(faction[0]){
							case "Bandit":
								if(!config.factionBank.ContainsKey(BaseCombatEntity.Faction.Bandit))
									config.factionBank.Add(BaseCombatEntity.Faction.Bandit,scrapCount);
								else
									config.factionBank[BaseCombatEntity.Faction.Bandit]+=scrapCount;
								changeScore(bp, BaseCombatEntity.Faction.Bandit, 0.001f*scrapCount,0);
								
								break;
							case "Scientist":
								if(!config.factionBank.ContainsKey(BaseCombatEntity.Faction.Scientist))
									config.factionBank.Add(BaseCombatEntity.Faction.Scientist,scrapCount);
								else
									config.factionBank[BaseCombatEntity.Faction.Scientist]+=scrapCount;
								changeScore(bp, BaseCombatEntity.Faction.Scientist, 0.001f*scrapCount,0);	
								break;
						}													
						bp.faction =getNativeFaction(bp);	
						if(oldFaction!=bp.faction) GenerateFactionLogo(bp);
					}
					if( machine.sellOrders.sellOrders[sellOrderId].itemToSellID == -932201673){
						int scrapCount = machine.sellOrders.sellOrders[sellOrderId].itemToSellAmount*numberOfTransactions;
						switch(faction[0]){
							case "Bandit":
								if(!config.factionBank.ContainsKey(BaseCombatEntity.Faction.Bandit))
									config.factionBank.Add(BaseCombatEntity.Faction.Bandit,0-scrapCount);
								else
									config.factionBank[BaseCombatEntity.Faction.Bandit]-=scrapCount;
									if(config.factionBank[BaseCombatEntity.Faction.Bandit]<0)									
										config.factionBank[BaseCombatEntity.Faction.Bandit]=0;
								break;
							case "Scientist":
								if(!config.factionBank.ContainsKey(BaseCombatEntity.Faction.Scientist))
									config.factionBank.Add(BaseCombatEntity.Faction.Scientist,0-scrapCount);
								else
									config.factionBank[BaseCombatEntity.Faction.Scientist]-=scrapCount;	
									if(config.factionBank[BaseCombatEntity.Faction.Scientist]<0)									
										config.factionBank[BaseCombatEntity.Faction.Scientist]=0;
								break;
						}													

					}
					return null;
				}
			#endregion
			#region Generic NPC Handlers
			object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info){
				if(info.HitEntity==null||info.Initiator==null) return null;
				if(info.HitEntity.gameObject==null||info.Initiator.gameObject==null) return null;
				bool? returnvar = null;
				try{
					returnvar = ((BaseCombatEntity)(info.Initiator)).faction!=((BaseCombatEntity)(info.HitEntity)).faction||((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Default||((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Player||(!(info.HitEntity as HumanNPC) && !(info.HitEntity as BaseNpc));
					
					float switcher = UnityEngine.Random.Range(0.0f,1.0f);
					((HumanNPC)info.HitEntity).Brain.Navigator.Resume();
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
				}catch(Exception e){}
				if (info.damageTypes.Has(Rust.DamageType.Heat) ){
					Puts("is firework?"+(info.HitEntity is BaseFirework).ToString());
					if(info.HitEntity is BaseFirework){
						CommandFirework(info.HitEntity as BaseFirework, info);
					}	
					if(info.HitEntity is SkyLantern){
						ZoneLantern(info.HitEntity as SkyLantern, info);
					}	
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
			#endregion
			#region Faction NPC Targeting
			object CanBradleyApcTarget(BradleyAPC apc, BaseCombatEntity bce){
				return validTarget(((BaseCombatEntity)apc),bce,true);
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
				BasePlayer bp = (bce as BasePlayer);
				if(bp!=null) if(at.IsAuthed(bp)) return false;
				if(!validTarget(((BaseCombatEntity)at),bce,true)) 		return false;
				return null;
			}
			object OnTurretCheckHostile(NPCAutoTurret turret, BaseCombatEntity entity)
			{
				if(entity==null)return null;
				if(((entity.faction != BaseCombatEntity.Faction.Scientist) && turret.gameObject.name.Contains("sentry.sci")) || ((entity.faction != BaseCombatEntity.Faction.Bandit) && turret.gameObject.name.Contains("sentry.ban"))){
					if(entity is BaseAnimalNPC || (entity is BasePlayer && (entity as BasePlayer).IsConnected==false)){
						return entity.unHostileTime> UnityEngine.Time.realtimeSinceStartup;
					}
				}
				return null;
			}
			bool? OnIsFriendly(HumanNPC hn, BaseEntity be){
					return null;
			}
			bool? OnIsTarget(HumanNPC hn, BaseEntity be){
				try{
					bool result = ((BaseCombatEntity)hn).faction!=((BaseCombatEntity)be).faction;//
					return (result==true?(bool?)true:(bool?)false);
				}catch(Exception e){return null;}
			}
			bool? OnIsThreat(HumanNPC hn, BaseEntity be){
				try{
					bool result = ((BaseCombatEntity)hn).faction!=((BaseCombatEntity)be).faction;//
					return (result==true?(bool?)true:(bool?)false);
				}catch(Exception e){return null;}
			}
			bool? OnCaresAbout(AIBrainSenses hn, BaseEntity be){
				BaseAnimalNPC bn = (be as BaseAnimalNPC);
				
				bool result = ((BaseCombatEntity)hn.owner).faction!=((BaseCombatEntity)be).faction && !(((BaseCombatEntity)be).faction ==BaseCombatEntity.Faction.Default);//
				
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
			#endregion
			#region Custom States
			class FactionCombatState : ScientistBrain.CombatState{
				private global::StateStatus status = global::StateStatus.Error;
				
				public override void StateEnter(){
					if(this.brain==null){return;}
					if(this.brain.Navigator==null){return;}
					if(this.brain.Navigator.BaseEntity==null){
						return;
					}		
					if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
						return;
					}		
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
				public override void StateEnter(){
					if(this.brain.Navigator.BaseEntity==null){
						base.StateEnter();					return;
					}		
					if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
						base.StateEnter();					return;
					}		
					HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
					if (hn.inventory == null || hn.inventory.containerBelt == null)
					{
						base.StateEnter();
					}
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
					base.StateEnter();
				}
			}
			class FactionAnimalIdleState : BaseAIBrain<BaseAnimalNPC>.BaseIdleState{
				private global::StateStatus status = global::StateStatus.Error;
				
				public override global::StateStatus StateThink(float delta)
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
			
				public override global::StateStatus StateThink(float delta)
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
				
				public override global::StateStatus StateThink(float delta)
				{
					if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
						if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
							if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 30f)
							{//
								RadialPoint(this.brain.Navigator, this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);	
								return global::StateStatus.Running;
							}else{
								return base.StateThink(delta);
							}
						}
					}
					return base.StateThink(delta);
				}
				
			}
			class FactionBaseFollowPathState : BaseAIBrain<HumanNPC>.BasicAIState{
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
				public override void StateEnter(){
					int i=0;
					this.currentWaitTime =0;
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
					
					if(UnityEngine.Random.Range(0.0f,1.0f)<AIZSwapRate){
						if(UnityEngine.Random.Range(0.0f,1.0f)>SwapWanderRate){
							UnityEngine.Debug.Log("Changing AIZ");
							try{
							Vector3 samplePos = ((this.brain.Navigator.BaseEntity as HumanNPC).eyes.transform.forward*100)+(this.brain.Navigator.BaseEntity as HumanNPC).transform.position;
							(this.brain.Navigator.BaseEntity as HumanNPC).VirtualInfoZone = findNearestAIZ(this.brain.Navigator.BaseEntity.transform.position,(this.brain.Navigator.BaseEntity as HumanNPC).VirtualInfoZone,this.brain.Navigator.BaseEntity.faction);
							this.brain.Navigator.Path = (this.brain.Navigator.BaseEntity as HumanNPC).VirtualInfoZone.paths[0];	
							}catch(Exception e){}
						}else{
							(this.brain.Navigator.BaseEntity as HumanNPC).VirtualInfoZone = null;
							this.brain.Navigator.Path = null;
							
						}
					}
					
					
					if(this.brain.Navigator.Path!=null ){
						this.path = this.brain.Navigator.Path;
						if(this.path.Points == null){	
						return ;}
						if(this.currentTargetPoint == null){
							this.currentNodeIndex=this.path.FindNearestPointIndex(this.GetEntity().ServerPosition);
							this.currentTargetPoint= this.path.GetPointAtIndex(this.currentNodeIndex);}
						this.currentNodeIndex=(((this.path.Points.Count()*6)+this.currentNodeIndex + (UnityEngine.Random.Range(0,1)  * (this.pathDirection == AIMovePointPath.PathDirection.Forwards? 1 : -1)))%this.path.Points.Count());
						this.currentTargetPoint=this.path.GetPointAtIndex(this.currentNodeIndex);
						if(this.currentTargetPoint.transform==null && this.brain.transform==null){
							UnityEngine.Debug.Log("Not Following Path");
							RadialPoint(this.brain.Navigator, this.brain.transform.position+(this.brain.transform.forward*5),this.brain.transform.position);	
							return;
						}else{
							RadialPoint(this.brain.Navigator, this.currentTargetPoint.transform.position,this.brain.transform.position);	
							return;
						}
					}else{
						RadialPoint(this.brain.Navigator, this.brain.transform.position+(this.brain.transform.forward*5),this.brain.transform.position);	
						return;
					}return;
				
				}
				public override global::StateStatus StateThink(float delta)
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
					if(this.brain.Navigator.Path!=null && this.brain.Navigator.Path?.Points!=null){
						if(this.path==null){return global::StateStatus.Finished;}
						if(this.path.Points == null){return global::StateStatus.Finished;}
						if(this.currentTargetPoint == null){return global::StateStatus.Finished;}
						if (this.currentWaitTime >= 5f && this.currentTargetPoint.HasLookAtPoints())
						{
							Transform randomLookAtPoint = this.currentTargetPoint.GetRandomLookAtPoint();
							if (randomLookAtPoint != null)
							{
								this.brain.Navigator.SetFacingDirectionOverride(Vector3Ex.Direction2D(randomLookAtPoint.transform.position, this.GetEntity().ServerPosition));
							}
						}
						if (this.currentTargetPoint.WaitTime > 60f || ( this.currentWaitTime >= 5f && Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<0.1f))
						{
							return global::StateStatus.Finished;
						}
						return global::StateStatus.Running;
					}else{
						if (this.currentWaitTime >= 15f|| ( this.currentWaitTime >= 5f && Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<0.5f))
						{//
							return global::StateStatus.Finished;
						}else{
							return global::StateStatus.Running;
						}
					}
				
					
				}
				
				public override void StateLeave()
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
				}
				
			}
			class FactionBaseRoamState : BaseAIBrain<HumanNPC>.BaseRoamState{
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
				public List<AIInformationZone> visitedZones = new List<AIInformationZone>();

				// Token: 0x040001E4 RID: 484
				public int currentNodeIndex;
				public FactionBaseRoamState() : base(){
					pathDirection = (UnityEngine.Random.Range(0,1)==1?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards);
				}
				public override void StateEnter(){
					int i=0;
					this.currentWaitTime =0;
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
					
					if(UnityEngine.Random.Range(0.0f,1.0f)<AIZSwapRate){
						if(UnityEngine.Random.Range(0.0f,1.0f)>SwapWanderRate){
							UnityEngine.Debug.Log("Changing AIZ");
							try{
							visitedZones.Add((this.brain.Navigator.BaseEntity as HumanNPC).VirtualInfoZone);
							if(visitedZones.Count()>3) visitedZones.RemoveAt(0);
							Vector3 samplePos = ((this.brain.Navigator.BaseEntity as HumanNPC).eyes.transform.forward*50)+(this.brain.Navigator.BaseEntity as HumanNPC).transform.position;
							(this.brain.Navigator.BaseEntity as HumanNPC).VirtualInfoZone = findNearestAIZ(this.brain.Navigator.BaseEntity.transform.position,visitedZones.ToArray(),this.brain.Navigator.BaseEntity.faction);
							this.brain.Navigator.Path = (this.brain.Navigator.BaseEntity as HumanNPC).VirtualInfoZone.paths[0];	
							}catch(Exception e){}
						}else{
							(this.brain.Navigator.BaseEntity as HumanNPC).VirtualInfoZone = null;
							this.brain.Navigator.Path = null;
							
						}
					}
					if(this.brain.Navigator.Agent.agentTypeID==0){
						this.brain.Navigator.Path=null;
					}
					
					if(this.brain.Navigator.Path!=null ){
						this.path = this.brain.Navigator.Path;
						if(this.path.Points == null){	
						return ;}
						if(this.currentTargetPoint == null){
							this.currentNodeIndex=this.path.FindNearestPointIndex(this.GetEntity().ServerPosition);
							this.currentTargetPoint= this.path.GetPointAtIndex(this.currentNodeIndex);}
						this.currentNodeIndex=(((this.path.Points.Count()*6)+this.currentNodeIndex + (UnityEngine.Random.Range(1,2)  * (this.pathDirection == AIMovePointPath.PathDirection.Forwards? 1 : -1)))%this.path.Points.Count());
						this.currentTargetPoint=this.path.GetPointAtIndex(this.currentNodeIndex);
						if(this.currentTargetPoint.transform==null && this.brain.transform==null){
							UnityEngine.Debug.Log("Not Following Path");
							RadialPoint(this.brain.Navigator, this.brain.transform.position+(this.brain.transform.forward*5),this.brain.transform.position);	
							return;
						}else{
							RadialPoint(this.brain.Navigator, this.currentTargetPoint.transform.position,this.brain.transform.position);	
							return;
						}
					}else{
						RadialPoint(this.brain.Navigator, this.brain.transform.position+(this.brain.transform.forward*5),this.brain.transform.position);	
						return;
					}return;
				
				}
				public override global::StateStatus StateThink(float delta)
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
					if(this.brain.Navigator.Path!=null && this.brain.Navigator.Path?.Points!=null){
						if(this.path==null){return global::StateStatus.Finished;}
						if(this.path.Points == null){return global::StateStatus.Finished;}
						if(this.currentTargetPoint == null){return global::StateStatus.Finished;}
						if (this.currentWaitTime >= 5f && this.currentTargetPoint.HasLookAtPoints())
						{
							Transform randomLookAtPoint = this.currentTargetPoint.GetRandomLookAtPoint();
							if (randomLookAtPoint != null)
							{
								this.brain.Navigator.SetFacingDirectionOverride(Vector3Ex.Direction2D(randomLookAtPoint.transform.position, this.GetEntity().ServerPosition));
							}
						}
						if (this.currentWaitTime > 30f || ( this.currentWaitTime >= 5f && Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<0.1f))
						{
							return global::StateStatus.Finished;
						}
						return global::StateStatus.Running;
					}else{
						if (this.currentWaitTime >= 15f|| ( this.currentWaitTime >= 5f && Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<0.5f))
						{//
					
							
							RadialPoint(this.brain.Navigator, this.brain.transform.position+((UnityEngine.Random.Range(0.0f,1.0f)>0.5f?this.brain.transform.right:new Vector3(0,0,0)-this.brain.transform.right)*5),this.brain.transform.position);	
							return global::StateStatus.Finished;
						}else{
							return global::StateStatus.Running;
						}
					}
				
					
				}
				
				public override void StateLeave()
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
				}
				
			}
			class FactionTakeCoverState : ScientistBrain.TakeCoverState{
				private global::StateStatus status = global::StateStatus.Error;
				private global::BaseEntity coverFromEntity;
				public override void StateEnter(){
					this.status = global::StateStatus.Running;
					if(!this.brain.Navigator.BaseEntity.creatorEntity==null){
						if(!this.brain.Navigator.BaseEntity.creatorEntity.transform==null){
							RadialPoint(this.brain.Navigator, this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);						
							return;
						}
					}
					global::HumanNPC entity = base.GetEntity();
					this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if (!this.StartMovingToCover())
					{
						if (this.coverFromEntity == null)
						{
							RadialPoint(this.brain.Navigator, this.brain.transform.position,this.brain.transform.position);
						}else{
							
							Vector3 AwayFrom = (this.coverFromEntity ? this.coverFromEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 30f))- entity.transform.position;
							AwayFrom = AwayFrom.normalized;
							RadialPoint(this.brain.Navigator, this.brain.transform.position + (AwayFrom * 5) + ((Quaternion.AngleAxis((UnityEngine.Random.Range(0.0f,1.0f)<0.5f?-90:90), Vector3.up) * AwayFrom)*10),this.brain.transform.position);
						}
						
					}
						base.StateEnter();					return;
				}
				public override void StateLeave()
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
					ClearCoverPointUsage();
						base.StateLeave();					return;
					
				}
				private bool StartMovingToCover()
				{
					if (this.coverFromEntity == null)
					{
						return false;
					}
					global::HumanNPC entity = base.GetEntity();
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
						aipoint.ClearIfUsedBy(base.GetEntity());
				
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
			
		
			class FactionChaseState : global::BaseAIBrain<global::HumanNPC>.BaseChaseState
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

				public override void StateLeave()
				{
					base.StateLeave();
							
					this.Stop();
				}

				public override void StateEnter()
				{
					base.StateEnter();
					this.status = global::StateStatus.Error;
					if (this.brain.PathFinder == null)
					{
						return;
					}
							
					if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
					{
						(this.brain.GetEntity() as BasePlayer).Hurt(10f);
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

				public override global::StateStatus StateThink(float delta)
				{
					global::BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if (baseEntity == null)
					{
						return global::StateStatus.Error;
					}
					global::HumanNPC entity = base.GetEntity();
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
									(this.brain.GetEntity() as BasePlayer).Hurt(10f);
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
			
		#endregion
	}
}