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
#endregion
namespace Oxide.Plugins{
	[Info("AIPlus", "obsol", "0.1.1")]
	[Description("AI Rewrite - bandit, scientist, animal, and unaffiliated faction, squad and faction control, animal squadding, in-game zone definition")]
	public class AIPlus : CovalencePlugin{	
		#region Generic Vars
			private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
			private void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,  "<color=#00ff00>[AIPlus]</color>", 0, Array.Empty<object>());
			private Dictionary<string,AIInformationZone> ActiveAIZ = new Dictionary<string,AIInformationZone>();
			private Dictionary<BaseAIBrain<BaseAnimalNPC>, float> waitTimes = new Dictionary<BaseAIBrain<BaseAnimalNPC>, float>();
			Dictionary<NPCPlayer, AIInformationZone> AssignedAgents = new Dictionary<NPCPlayer, AIInformationZone>();
			Dictionary<NPCSpawner, BaseCombatEntity> SpawnPointBank = new Dictionary<NPCSpawner, BaseCombatEntity>();
			Dictionary<BasePlayer, HashSet<BaseCombatEntity>> AIZSquads = new Dictionary<BasePlayer, HashSet<BaseCombatEntity>>();
		#endregion
		#region Configuration
			private Configuration config;
			void Init(){
				permission.RegisterPermission("aiplus.squad", this);
				permission.RegisterPermission("aiplus.command", this);
				permission.RegisterPermission("aiplus.zoning", this);
				LoadConfig();
			}
			void Unload(){
				SaveConfig();
			}
			class Configuration{
				[JsonProperty("newScores", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>> newScores=new Dictionary<ulong, Dictionary<BaseCombatEntity.Faction,float>>();
				[JsonProperty("pointGroups", ObjectCreationHandling = ObjectCreationHandling.Replace)]		
				public Dictionary<string,List<Vector3>> pointGroups = new Dictionary<string,List<Vector3>>();
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
						LogWarning("Configuration appears to be outdated; updating and saving");SaveConfig();}
				}
				catch{LogWarning($"Configuration file {Name}.json is invalid; using defaults");LoadDefaultConfig();}
			}
			protected override void SaveConfig(){
				LogWarning($"Configuration changes saved to {Name}.json");
				Config.WriteObject(config, true);
			}
		#endregion Configuration
		#region Faction logic		
			#region Utility
			/*
				//NPCSpawner.NPCType
				//Scientist,
				//Peacekeeper,
				//HeavyScientist,
				//JunkpileScientist,
				//Bandit,
				//Murderer,
				//Scarecrow

				SerializedNPCSpawner serializedNPCSpawner = new SerializedNPCSpawner();
				serializedNPCSpawner.npcType=(int)NPCSpawner.NPCType.Bandit;
				serializedNPCSpawner.respawnMin=3600;
				serializedNPCSpawner.respawnMax=6000;
				serializedNPCSpawner.position=bp.transform.position;
				serializedNPCSpawner.category="[AIZ Spawner]";
				
				GameObject go = new GameObject("[AIZ Spawner]");
				NPCSpawner npcSpawner = go.AddComponent<NPCSpawner>();
				npcSpawner.Initialize(serializedNPCSpawner);

				//Foreach vending machine, find closest AIZ, on vend, if scrap, add to scrap bank for faction
				//on timer, remove cost from faction bank and spawn faction member from spawnpoint bank if available;
				//bank of spawnpoints created by skull spike
				//on place skull spike, add spike to dictionary of spikes and their aiz key
				//on remove/destroy remove from dictionary
				//if only spikes at AIZ, flip AIZ faction
				//more dictionaries, use structs to keep things clean
				//on deploy apply faction to item - on attack, score shift - on destroy, score shift
				//bigger scoreshift on death
				//if target is owner, chill
				//when holding food, animals won't add you as a target if they have no owner
				//giving food makes you ownerplayer

			*/
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
							if(hn.Brain.CurrentState is BaseAIBrain<HumanNPC>.BaseFollowPathState){
								((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
								((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
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
								if(hn.Brain.CurrentState is BaseAIBrain<HumanNPC>.BaseFollowPathState){
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
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
								if(hn.Brain.CurrentState is BaseAIBrain<HumanNPC>.BaseFollowPathState){
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).path = null;
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=0;
								}
								hn.Brain.OwningPlayer=null;
							}else{
								hn.VirtualInfoZone = aiz;
								hn.Brain.Navigator.Path = hn.VirtualInfoZone.paths[0];
								if(hn.Brain.CurrentState is BaseAIBrain<HumanNPC>.BaseFollowPathState){
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
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
								if(hn.Brain.CurrentState is BaseAIBrain<HumanNPC>.BaseFollowPathState){
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).path = null;
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=0;
								}
							}else{
								hn.VirtualInfoZone = aiz;
								hn.Brain.Navigator.Path = hn.VirtualInfoZone.paths[0];
								if(hn.Brain.CurrentState is BaseAIBrain<HumanNPC>.BaseFollowPathState){
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
									((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
								}
							}
						}
					}
				}
			}
			void AddSeat(BaseNpc ent, Vector3 locPos, bool chair=false) {
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
			bool RadialPoint(BaseNavigator nav, Vector3 target, Vector3 self){
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
				if (distance<16f){
					 destRes=nav.SetDestination(newPosition, global::BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
					return true;
					}
				else{
					destRes=nav.SetDestination(newPosition, global::BaseNavigator.NavigationSpeed.Fast, 0f, 0f);							
					return true;
					}		
			}
			void initAnimal(BaseNpc animal){
				if(animal.IsNpc)Puts(((char)27)+"[96m"+"IsNpc! Did you fix NPCPlayer with dnSpy?");
				if(animal.transform.name.Contains("bear") ||animal.transform.name.Contains("boar") ||animal.transform.name.Contains("wolf") ||animal.transform.name.Contains("deer")|| animal.transform.name.Contains("shark")){
					animal.faction = BaseCombatEntity.Faction.Player;	
				}
			}
			bool swapSciRoamState(BasePlayer s){
				if(s.IsNpc)Puts(((char)27)+"[96m"+"IsNpc! Did you fix NPCPlayer with dnSpy?");
				if(s.transform.name.ToLower().Contains("scientist") || s.transform.name.ToLower().Contains("underwater")||s.transform.name.ToLower().Contains("apc")||s.transform.name.ToLower().Contains("bradley")) { 
					s.faction = BaseCombatEntity.Faction.Scientist;
				}
				if(s.transform.name.ToLower().Contains("bandit")||s.transform.name.ToLower().Contains("dweller")) {
					s.faction = BaseCombatEntity.Faction.Bandit;
				}
				try{
					HumanNPC hn = ((HumanNPC)s);
					if(hn.Brain==null) return false;
					hn.Brain.Senses.senseTypes = (EntityType)67;
					hn.Brain.Senses.hostileTargetsOnly = false;
					hn.Brain.Senses.checkVision=true;
					hn.Brain.Senses.checkLOS=true;
					hn.Brain.Senses.maxRange=35f;
					hn.Brain.Navigator.StoppingDistance=1f;
					float terraingDiff = hn.transform.position.y - Terrain.activeTerrain.SampleHeight(hn.transform.position);
					if(terraingDiff > -0.1f && terraingDiff < 2f){
						hn.VirtualInfoZone = findNearestAIZ(hn.spawnPos, hn.faction);
						hn.Brain.Navigator.Path = hn.VirtualInfoZone.paths[0];
						if(hn.Brain.CurrentState is BaseAIBrain<HumanNPC>.BaseFollowPathState){
							((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).path = hn.Brain.Navigator.Path;
							((BaseAIBrain<HumanNPC>.BaseFollowPathState)hn.Brain.CurrentState).currentNodeIndex=hn.Brain.Navigator.Path.FindNearestPointIndex(hn.transform.position)+UnityEngine.Random.Range(-1,2);
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
				changeScore(player, BaseCombatEntity.Faction.Scientist, 0.01f);
				changeScore(player, BaseCombatEntity.Faction.Bandit, 0.01f);
				player.faction=getNativeFaction(player);
				GenerateFactionLogo(player);
				}
			}
			void initAIZ(){
				Dictionary<string,List<Vector3>>.KeyCollection keys = config.pointGroups.Keys;  
				foreach (string key in keys)  
				{  
					initSingleAIZ(key);
				}  
			}
			void initSingleAIZ(string key){	
				if(!config.pointGroups.ContainsKey(key))	return;		
				GameObject aiz = new GameObject(key);	
				aiz.transform.position = config.pointGroups[key][0];
				if(ActiveAIZ.ContainsKey(key)){
					GameObject.Destroy(ActiveAIZ[key]);
					ActiveAIZ[key] = aiz.AddComponent<AIInformationZone>();
				}else{
					ActiveAIZ.Add(key, aiz.AddComponent<AIInformationZone>());
				}
				ActiveAIZ[key].bounds =new Bounds(aiz.transform.position,new Vector3(100,20,100));
				aiz.AddComponent<AIInformationGrid>();
				GameObject aizmppgo  = new GameObject();
				aizmppgo.transform.position = config.pointGroups[key][0];				
				AIMovePointPath AIZMPP = aizmppgo.AddComponent<AIMovePointPath>();	
				aizmppgo.transform.parent = ActiveAIZ[key].transform;
				AIZMPP.LoopMode = AIMovePointPath.Mode.Loop;
				foreach(Vector3 v3 in config.pointGroups[key]){
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
				Puts("AIZ "+key+" Loaded");
			}
			AIInformationZone findNearestAIZ(Vector3 position){
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
			AIInformationZone findNearestAIZ(Vector3 position, BaseCombatEntity.Faction faction){
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
			void CommandFirework(BaseFirework bf, HitInfo info){
				if(info.HitEntity==null||info.Initiator==null) return;
				if(info.HitEntity.gameObject==null||info.Initiator.gameObject==null) return;
				if(! (info.Initiator is BasePlayer)) return;
				BasePlayer bp = ((BasePlayer) info.Initiator);
				if (info.damageTypes.Has(Rust.DamageType.Heat))
				{
					Puts(bf.transform.name);///
					if(bf.transform.name.Contains("blue")){
						if(ActiveAIZ.ContainsKey(bp.faction.ToString())){
							if(bf.transform.name.Contains("mortar")){							
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
			}
			void ZoneLantern(SkyLantern sl, HitInfo hitInfo){
				string s = "";
				if(sl.transform.name.Contains(".deployed.")){
					s+="Create Point";
					Item note = sl.inventory.FindItemsByItemName("note");
					if(!(note!=null && (hitInfo.Initiator is BasePlayer) && note.text !=null)){
						return;
					}
					string[] lines = note.text.Split('\n');
					Dictionary<string,string> AIZSettings = new Dictionary<string,string>();
					if(lines!=null&&lines.Count()>1){
						foreach(string line in lines){
							string[] entries = line.Split(':');
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
						string[] entries = note.text.Split(':');
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
					if(AIZSettings.ContainsKey("zone")&&AIZSettings.ContainsKey("point")){
						addPointToGroup(AIZSettings["zone"]+" {"+
						((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
						"} [AIZ]", hitInfo.Initiator.transform.position);
						s+= "Point Added to "+AIZSettings["zone"]+" {"+
							((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
							"} [AIZ]\n";
					}
					if(AIZSettings.ContainsKey("zone")&&AIZSettings.ContainsKey("load")){
						initSingleAIZ(AIZSettings["zone"]+" {"+
						((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
						"} [AIZ]");
						s+= "Reloaded "+AIZSettings["zone"]+" {"+
							((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
							"} [AIZ]\n";
							SaveConfig();
					}
					if(AIZSettings.ContainsKey("zone")&&AIZSettings.ContainsKey("delete")){
						if(ActiveAIZ.ContainsKey(AIZSettings["zone"]+" {"+
						((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
						"} [AIZ]")){
							GameObject.Destroy(ActiveAIZ[AIZSettings["zone"]+" {"+
								((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
								"} [AIZ]"]);
							ActiveAIZ.Remove(AIZSettings["zone"]+" {"+
							((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
							"} [AIZ]"
							);

						}
						if(config.pointGroups.ContainsKey(AIZSettings["zone"]+" {"+
						((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
						"} [AIZ]")){
							config.pointGroups.Remove(AIZSettings["zone"]+" {"+
							((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
							"} [AIZ]"
							);
						}
						s+= "Deleted "+AIZSettings["zone"]+" {"+
							((BaseCombatEntity)hitInfo.Initiator).faction.ToString()+
							"} [AIZ]\n";
							SaveConfig();
					}
				}
				else if(sl.transform.name.Contains(".green.")){
					s+="Create Move Point";
				}
				else if(sl.transform.name.Contains(".orange.")){
					s+="Create Cover Point";

				}
				else if(sl.transform.name.Contains(".purple.")){
					s+="Finalized Zone";

				}
				else if(sl.transform.name.Contains(".red.")){
					s+="Delete";

				}
				if(hitInfo.Initiator is BasePlayer) SendChatMsg((hitInfo.Initiator as BasePlayer),s);
			}
			void addPointToGroup(string key, Vector3 position){
				
				if(!config.pointGroups.ContainsKey(key))config.pointGroups.Add(key,new List<Vector3>());
				if(config.pointGroups[key]==null) config.pointGroups[key] = new List<Vector3>();
				config.pointGroups[key].Add(position);
			}
			public bool changeScore(BasePlayer bp, BaseCombatEntity.Faction ft, float score){
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
							config.newScores[id][BaseCombatEntity.Faction.Scientist]+=-score*2;
							break;
						case BaseCombatEntity.Faction.Scientist:
							config.newScores[id][BaseCombatEntity.Faction.Bandit]+=-score*2;
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
			
			#endregion chatcmds
			#region Faction Initializers
			void OnPlayerRespawned(BasePlayer player)=>initPlayer(player);
			void OnPlayerSleepEnded(BasePlayer player)=>initPlayer(player);
			object OnNPCAIInitialized(BaseAIBrain<HumanNPC> player){swapSciRoamState(player.GetComponent<HumanNPC>()); return null;}
			object OnNPCAIInitialized(BaseAIBrain<BaseAnimalNPC> player){initAnimal(player.GetComponent<BaseAnimalNPC>()); return null;}
			object OnBradleyApcInitialize(BradleyAPC apc){swapTank((apc));return null;}
			object OnPlayerDeath(BasePlayer player, HitInfo info){return null;}
			void OnItemDropped(Item item, BaseEntity entity){
				try{
					if(!item.parent.playerOwner.IsConnected) return;
					BasePlayer bp = item.parent.playerOwner;
					BaseCombatEntity bce = (getLookingAt(bp) as BaseCombatEntity);
					if(bce==null) return;
					Puts(bce.transform.name);
					if(bce.faction==bp.faction){
						WorldItem wi = entity as WorldItem;
						if(wi==null) return;
						Puts("worlditem");
						if((bce is HumanNPC)){
							Puts("HumanNPC");
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
						}else if ((bce is BaseNpc)){	
							if(entity.transform.name.Contains("sled")){
								if((bce as BaseNpc).transform.name.Contains("bear") ||(bce as BaseNpc).transform.name.Contains("stag"))
									AddSeat((bce as BaseNpc), new Vector3(0.0f,-0.1f,-2f));
								else if((bce as BaseNpc).transform.name.Contains("wolf") ||(bce as BaseNpc).transform.name.Contains("boar")){
									AddSeat((bce as BaseNpc), new Vector3(0.0f,0.2f,-1.5f));
								}			
								wi.RemoveItem();	
								wi.Kill();							
								bce.SendNetworkUpdateImmediate(true);
							}else if(entity.transform.name.Contains("chair")){
								Puts("Chair");
								if((bce as BaseNpc).transform.name.Contains("bear") ||(bce as BaseNpc).transform.name.Contains("stag")){
									Puts("bearchair");
									AddSeat((bce as BaseNpc), new Vector3(0.0f,0.6f,0f),true);
									Puts("bearchaird");
								}
								else if((bce as BaseNpc).transform.name.Contains("wolf") ||(bce as BaseNpc).transform.name.Contains("boar")){
									AddSeat((bce as BaseNpc), new Vector3(0.0f,0.2f,0f),true);
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
			void OnServerInitialized(){
				foreach(Sled s in GameObject.FindObjectsOfType<Sled>()){
					s.Kill();
				}
				foreach(AIInformationZone fc in GameObject.FindObjectsOfType<AIInformationZone>()){
					if(fc.transform.name.Contains("[AIZ]")){
						if(ActiveAIZ.ContainsKey(fc.transform.name.Replace(" [AIZ]",""))){
							ActiveAIZ.Remove((fc.transform.name.Replace(" [AIZ]","")));
						}
						GameObject.Destroy(fc.gameObject);
					}
				}
				LoadConfig();
				initAIZ();
			}
			#endregion initializers
			#region Faction NPC Handlers
			object OnBasePlayerAttacked(BasePlayer victimbp, HitInfo info){
				if(victimbp==null)return null;	
				var result = ((BaseCombatEntity)(info.Initiator)).faction!=victimbp.faction||((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Default||(((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Player && !info.damageTypes.Has(Rust.DamageType.Heat));
				if(result==false) return victimbp;
				return null;
			}
			object CanEntityBeHostile(BasePlayer entity){
				if(entity.IsConnected==false){
					return entity.unHostileTime> UnityEngine.Time.realtimeSinceStartup;
				}
				return null;
			}
			object OnAttackedAIEvent(AttackedAIEvent aie, AIMemory memory, AIBrainSenses senses, StateStatus stateStatus){
				aie.combatEntity = null;
				return null;
			}
			void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
			{
				if(newItem.ToString().Contains("torch")){
					GameObject go = findNearestAIZ(player.transform.position).gameObject;
					if(go.name.Contains("[AIZ]"))
						SendChatMsg(player, go.name.Replace("[AIZ]",""));
					else
						SendChatMsg(player, "Unassociated Zone");
				}
				//SendChatMsg(player, newItem.ToString());
			}
			object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info){
				if(info.HitEntity==null||info.Initiator==null) return null;
				if(info.HitEntity.gameObject==null||info.Initiator.gameObject==null) return null;
				bool? returnvar = null;
				try{
					returnvar = ((BaseCombatEntity)(info.Initiator)).faction!=((BaseCombatEntity)(info.HitEntity)).faction||((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Default||((BaseCombatEntity)(info.Initiator)).faction==BaseCombatEntity.Faction.Player;
					
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
						if(!((HumanNPC)info.HitEntity).Brain.Senses.Memory.Targets.Contains((BaseCombatEntity)info.Initiator))((HumanNPC)info.HitEntity).Brain.Senses.Memory.Targets.Add((BaseCombatEntity)info.Initiator);
				}catch(Exception e){}
				if (info.damageTypes.Has(Rust.DamageType.Heat) ){
					
					if(info.HitEntity is BaseFirework){
						CommandFirework(info.HitEntity as BaseFirework, info);
					}	
					if(info.HitEntity is SkyLantern){
						ZoneLantern(info.HitEntity as SkyLantern, info);
					}	
					try{
						if( ((BaseCombatEntity)(info.Initiator)).faction==((BaseCombatEntity)(info.HitEntity)).faction)
						{
							if(((HumanNPC)info.HitEntity).Brain.OwningPlayer==null){
								((HumanNPC)info.HitEntity).Brain.OwningPlayer=((BasePlayer)info.Initiator);
							}else{						
								((HumanNPC)info.HitEntity).Brain.OwningPlayer=null;
							}
							return false;
						}	
					}catch(Exception e){
					}//			
				}
				try{///
					BasePlayer bp = (BasePlayer)info.Initiator;	
					if(bp.IsConnected){		
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
				if(returnvar==null || returnvar == false ){
					return false;
				}else{
					return null;
				}
			}
			object OnTurretTarget(AutoTurret at, BaseCombatEntity bce){
				if(!validTarget(((BaseCombatEntity)at),bce,true)) return false;
				return null;
			}
			object OnTurretAuthorize(AutoTurret at, BasePlayer bp){
				at.faction=bp.faction;
				return null;
			}
			void OnEntitySpawned(BaseNetworkable entity){
				if(entity is ResourceEntity){
					if(entity.gameObject.GetComponent<NavMeshObstacle>()==null){
						NavMeshObstacle nmo = entity.gameObject.AddComponent<NavMeshObstacle>();
						nmo.carving=true;
					}
					
				}
			}
			object CanDismountEntity(BasePlayer player, BaseMountable entity){
				Puts("DismountTest");
				/*
				entity._mounted.DismountObject();
				entity._mounted.MovePosition(entity.transform.position);
				entity._mounted.SendNetworkUpdateImmediate(false);
				entity._mounted.SendModelState(true);
				entity._mounted = null;*/
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
					return (result==true?(bool?)null:(bool?)false);
				}catch(Exception e){return null;}
			}
			bool? OnCaresAbout(AIBrainSenses hn, BaseEntity be){
				try{
					BaseNpc bn = (hn.owner as BaseNpc);
					bool result = ((BaseCombatEntity)hn.owner).faction!=((BaseCombatEntity)be).faction && !(((BaseCombatEntity)be).faction ==BaseCombatEntity.Faction.Default &&bn==null);//
		
					return (result==true?(bool?)null:(bool?)false);
				}catch(Exception e){return null;}
			}
			#endregion
			#region Faction State Managers
			object OnStateEnter(ScientistBrain.CombatStationaryState state){
				if(state.brain.Navigator.BaseEntity==null){
					return null;
				}		
				if(!(state.brain.Navigator.BaseEntity as HumanNPC)==null){
					return null;
				}		
				HumanNPC hn = (state.brain.Navigator.BaseEntity as HumanNPC);
				if (hn.inventory == null || hn.inventory.containerBelt == null)
				{
					return null;
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
							return null;
			}
			object OnStateEnter(ScientistBrain.CombatState state){
				if(state.brain.Navigator.BaseEntity==null){
					return null;
				}		
				if(!(state.brain.Navigator.BaseEntity as HumanNPC)==null){
					return null;
				}		
				HumanNPC hn = (state.brain.Navigator.BaseEntity as HumanNPC);
				if (hn.inventory == null || hn.inventory.containerBelt == null)
				{
					return null;
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
							return null;
			}
			object OnStateThink(AnimalBrain.IdleState state,float delta){
				//Dictionary<BaseAIBrain<BaseCombatEntity>, float> waitTimes
				if(!waitTimes.ContainsKey(state.brain)) waitTimes.Add(state.brain,0f);
				waitTimes[state.brain]+=delta;
				if(waitTimes[state.brain]<0.1f){return global::StateStatus.Running;}
				waitTimes[state.brain]=0;
				if(!(state.brain.OwningPlayer==null)){
					if(!(state.brain.OwningPlayer.transform==null)){
						if(!state.brain.OwningPlayer.mounted.IsSet()){
							if (Vector3.Distance(state.brain.OwningPlayer.transform.position,state.brain.transform.position) > 10f)
							{//
								state.brain.Navigator.ClearFacingDirectionOverride();
								Vector3 forwardpos = state.brain.OwningPlayer.transform.position + (state.brain.OwningPlayer.eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
								forwardpos.y-=0.25f;
								RadialPoint(state.brain.Navigator, forwardpos,state.brain.transform.position);	
								return global::StateStatus.Running;
							}else{
								return null;																											
							}
						}else{
							BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Normal;
							if(state.brain.OwningPlayer.serverInput.IsDown(BUTTON.SPRINT))speed=BaseNavigator.NavigationSpeed.Fast;
							if(state.brain.OwningPlayer.serverInput.IsDown(BUTTON.DUCK))speed=BaseNavigator.NavigationSpeed.Slowest;
							if(state.brain.OwningPlayer.mounted.Get(true).transform.name.Contains("/chair.deployed")){
								state.brain.OwningPlayer.transform.position=state.brain.OwningPlayer.mounted.Get(true).transform.parent.position+state.brain.OwningPlayer.mounted.Get(true).transform.localPosition;
								state.brain.OwningPlayer.mounted.Get(true).syncPosition=true;
								state.brain.OwningPlayer.SendNetworkUpdateImmediate(true);
								state.brain.OwningPlayer.mounted.Get(true).SendNetworkUpdateImmediate(true);
							}
							if(state.brain.OwningPlayer.serverInput.IsDown(BUTTON.FIRE_SECONDARY)){
								if (Vector3.Distance(state.brain.OwningPlayer.transform.position,state.brain.transform.position) > 30f)
								{
									state.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = state.brain.OwningPlayer.transform.position + (state.brain.OwningPlayer.eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
									forwardpos.y-=0.25f;
									RadialPoint(state.brain.Navigator, forwardpos,state.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									state.brain.Navigator.ClearFacingDirectionOverride();
									//Vector3 forwardpos = state.brain.transform.position + (state.brain.OwningPlayer.transform.forward*5);
									Vector3 forwardpos = state.brain.transform.position + (state.brain.transform.forward*5);
									state.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																													
								}
							}
							else{
								if (Vector3.Distance(state.brain.OwningPlayer.transform.position,state.brain.transform.position) > 30f)
								{
									state.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = state.brain.OwningPlayer.transform.position + (state.brain.OwningPlayer.eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
									forwardpos.y-=0.25f;
									RadialPoint(state.brain.Navigator, forwardpos,state.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									state.brain.Navigator.ClearFacingDirectionOverride();
									//Vector3 forwardpos = state.brain.transform.position + (state.brain.OwningPlayer.transform.forward*5);
									Vector3 forwardpos = state.brain.transform.position + (state.brain.OwningPlayer.eyes.HeadForward()*5);
									state.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																										
								}
							}
						}
						
					}
				}
				return null;
			}
			object OnStateThink(AnimalBrain.RoamState state,float delta){
				if(!waitTimes.ContainsKey(state.brain)) waitTimes.Add(state.brain,0f);
				waitTimes[state.brain]+=delta;
				if(waitTimes[state.brain]<0.1f){return global::StateStatus.Running;}
				waitTimes[state.brain]=0;
				if(!(state.brain.OwningPlayer==null)){
					if(!(state.brain.OwningPlayer.transform==null)){
						if(!state.brain.OwningPlayer.mounted.IsSet()){
							if (Vector3.Distance(state.brain.OwningPlayer.transform.position,state.brain.transform.position) > 10f)
							{//
								state.brain.Navigator.ClearFacingDirectionOverride();
								Vector3 forwardpos = state.brain.OwningPlayer.transform.position + (state.brain.OwningPlayer.transform.forward*UnityEngine.Random.Range(1f,3f));
								forwardpos.y-=0.25f;
								RadialPoint(state.brain.Navigator, forwardpos,state.brain.transform.position);	
								return global::StateStatus.Running;
							}else{
								return null;
							}
						}else{
							BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Normal;
							if(state.brain.OwningPlayer.serverInput.IsDown(BUTTON.SPRINT))speed=BaseNavigator.NavigationSpeed.Fast;
							if(state.brain.OwningPlayer.serverInput.IsDown(BUTTON.DUCK))speed=BaseNavigator.NavigationSpeed.Slowest;
							if(state.brain.OwningPlayer.mounted.Get(true).transform.name.Contains("/chair.deployed")){
								state.brain.OwningPlayer.transform.position=state.brain.OwningPlayer.mounted.Get(true).transform.parent.position+state.brain.OwningPlayer.mounted.Get(true).transform.localPosition;
								state.brain.OwningPlayer.mounted.Get(true).syncPosition=true;
								state.brain.OwningPlayer.SendNetworkUpdateImmediate(true);
								state.brain.OwningPlayer.mounted.Get(true).SendNetworkUpdateImmediate(true);
							}
							if(state.brain.OwningPlayer.serverInput.IsDown(BUTTON.FIRE_SECONDARY)){
								if (Vector3.Distance(state.brain.OwningPlayer.transform.position,state.brain.transform.position) > 30f)
								{
									state.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = state.brain.OwningPlayer.transform.position + (state.brain.OwningPlayer.eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
									forwardpos.y-=0.25f;
									RadialPoint(state.brain.Navigator, forwardpos,state.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									state.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = state.brain.transform.position + (state.brain.transform.forward*5);
									state.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																						
								}
							}
							else{
								if (Vector3.Distance(state.brain.OwningPlayer.transform.position,state.brain.transform.position) > 30f)
								{
									state.brain.Navigator.ClearFacingDirectionOverride();
									Vector3 forwardpos = state.brain.OwningPlayer.transform.position + (state.brain.OwningPlayer.eyes.transform.forward*UnityEngine.Random.Range(1f,3f));
									forwardpos.y-=0.25f;
									RadialPoint(state.brain.Navigator, forwardpos,state.brain.transform.position);	
									return global::StateStatus.Running;
								}else{
									state.brain.Navigator.ClearFacingDirectionOverride();
									//Vector3 forwardpos = state.brain.transform.position + (state.brain.OwningPlayer.transform.forward*5);
									Vector3 forwardpos = state.brain.transform.position + (state.brain.OwningPlayer.eyes.HeadForward()*5);
									state.brain.Navigator.SetDestination(forwardpos, speed, 0f, 0f);																										
								}
							}
						}
						
					}
				}
				return null;
			}
			object OnStateThink(AnimalBrain.ChaseState state,float delta){
				if(!(state.brain.OwningPlayer==null)){
					if(!(state.brain.OwningPlayer.transform==null)){
						if (Vector3.Distance(state.brain.OwningPlayer.transform.position,state.brain.transform.position) > 30f)
						{//
							RadialPoint(state.brain.Navigator, state.brain.OwningPlayer.transform.position,state.brain.transform.position);	
							return global::StateStatus.Running;
						}else{
							return null;
						}
					}
				}
							return null;
			}
			object OnStateThink(BaseAIBrain<HumanNPC>.BaseFollowPathState state,float delta){
				state.currentWaitTime += delta;
				if(!(state.brain.OwningPlayer==null)){
					if(!(state.brain.OwningPlayer.transform==null)){
						if (Vector3.Distance(state.brain.OwningPlayer.transform.position,state.brain.transform.position) > 10f || state.currentWaitTime >= 60f)
						{//
							RadialPoint(state.brain.Navigator, state.brain.OwningPlayer.transform.position,state.brain.transform.position);	
							state.currentWaitTime=UnityEngine.Random.Range(0,50);
							return global::StateStatus.Running;
						}else{
							return global::StateStatus.Running;
						}
					}
				}
				if(state.brain.Navigator.Path!=null && state.brain.Navigator.Path?.Points!=null){
					if(state.path==null){
						state.path = state.brain.Navigator.Path;}
					if(state.path.Points == null){
						return global::StateStatus.Running;}
					if(state.currentTargetPoint == null){
						state.currentNodeIndex=state.path.FindNearestPointIndex(state.GetEntity().ServerPosition);
						state.currentTargetPoint= state.path.GetPointAtIndex(state.currentNodeIndex);}
					if (state.currentWaitTime <= 0f && state.currentTargetPoint.HasLookAtPoints())
					{
						Transform randomLookAtPoint = state.currentTargetPoint.GetRandomLookAtPoint();
						if (randomLookAtPoint != null)
						{
							state.brain.Navigator.SetFacingDirectionOverride(Vector3Ex.Direction2D(randomLookAtPoint.transform.position, state.GetEntity().ServerPosition));
						}
					}
					if (state.currentTargetPoint.WaitTime <= 0f || state.currentWaitTime >= state.currentTargetPoint.WaitTime)
					{
						state.brain.Navigator.ClearFacingDirectionOverride();
						state.currentWaitTime=UnityEngine.Random.Range(0,5);
						int num = state.currentNodeIndex;
						int useCount = 0;
						bool destRes = false;
						state.currentNodeIndex=(((state.path.Points.Count()*6)+state.currentNodeIndex + (UnityEngine.Random.Range(0,3)  * (state.pathDirection == AIMovePointPath.PathDirection.Forwards? 1 : -1)))%state.path.Points.Count());
						state.currentTargetPoint=state.path.GetPointAtIndex(state.currentNodeIndex);
						state.currentWaitTime=0;RadialPoint(state.brain.Navigator, state.currentTargetPoint.transform.position,state.brain.transform.position);	
					}
					return global::StateStatus.Running;
				}else{
					if (state.currentWaitTime >= 15f)
					{//
						state.currentWaitTime=0;RadialPoint(state.brain.Navigator, state.brain.transform.position,state.brain.transform.position);	
						return global::StateStatus.Running;
					}else{
						return global::StateStatus.Running;
					}
				}
			}
			object OnStateEnter(BaseAIBrain<HumanNPC>.BaseFollowPathState state){		
				int i=0;;
				if(state.brain==null){
					return false;
				}		
				if(state.brain.Navigator==null){return false;}
				if(state.brain.Navigator.BaseEntity==null){
					return false;
				}		
				if(!state.brain.OwningPlayer==null){
					if(!state.brain.OwningPlayer.transform==null){
						RadialPoint(state.brain.Navigator, state.brain.OwningPlayer.transform.position,state.brain.transform.position);						
						return global::StateStatus.Running;
					}
				}
				if(state.brain.Navigator.Path!=null ){
					state.path = state.brain.Navigator.Path;
					if(state.path.Points == null){						
					return global::StateStatus.Running;}
					if(state.currentTargetPoint == null){
						state.currentNodeIndex=state.path.FindNearestPointIndex(state.GetEntity().ServerPosition);
						state.currentTargetPoint= state.path.GetPointAtIndex(state.currentNodeIndex);}
					state.currentNodeIndex=(((state.path.Points.Count()*6)+state.currentNodeIndex + (UnityEngine.Random.Range(0,3)  * (state.pathDirection == AIMovePointPath.PathDirection.Forwards? 1 : -1)))%state.path.Points.Count());
					state.currentTargetPoint=state.path.GetPointAtIndex(state.currentNodeIndex);
					if(state.currentTargetPoint.transform==null && state.brain.transform==null){
						RadialPoint(state.brain.Navigator, state.brain.transform.position,state.brain.transform.position);	
						return global::StateStatus.Running;
					}else{
						RadialPoint(state.brain.Navigator, state.currentTargetPoint.transform.position,state.brain.transform.position);	
						return global::StateStatus.Running;
					}
				}else{
					RadialPoint(state.brain.Navigator, state.brain.transform.position,state.brain.transform.position);	
					return global::StateStatus.Running;
				}return global::StateStatus.Running;
			}
			object OnStateLeave(BaseAIBrain<HumanNPC>.BaseFollowPathState state){
				RadialPoint(state.brain.Navigator, state.brain.transform.position,state.brain.transform.position);	
				return null;
			}
			object OnStateEnter(ScientistBrain.TakeCoverState state){
				state.status = global::StateStatus.Running;
				if(!state.brain.OwningPlayer==null){
					if(!state.brain.OwningPlayer.transform==null){
						RadialPoint(state.brain.Navigator, state.brain.OwningPlayer.transform.position,state.brain.transform.position);						
						return global::StateStatus.Running;
					}
				}
				if (!state.StartMovingToCover())
				{
					RadialPoint(state.brain.Navigator, state.brain.transform.position,state.brain.transform.position);	
					return true;
				}
				return true;
			}
			object OnStateLeave(ScientistBrain.TakeCoverState state){
				state.brain.Navigator.ClearFacingDirectionOverride();
				state.ClearCoverPointUsage();
				return true;
			}			
			#endregion
		#endregion
	}
}