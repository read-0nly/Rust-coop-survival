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
	[Info("Hotzone", "obsol", "0.0.1")]
	[Description("Makes all scientist brains move towards a specified point on the map")]
	public class Hotzone : CovalencePlugin{	
		#region Generic Vars
			private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
			private void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,  "<color=#00ff00>[Hotzone]</color>", 0, Array.Empty<object>());
			bool debugOnBoot=false;//
			List<Oxide.Ext.RustEdit.NPC.NPCSpawner> Spawners;
		#endregion
		#region Configuration
		private Configuration config;
		private void Init(){
			permission.RegisterPermission("hotzone.set", this);
			Spawners = new List<Oxide.Ext.RustEdit.NPC.NPCSpawner>(Resources.FindObjectsOfTypeAll<Oxide.Ext.RustEdit.NPC.NPCSpawner>());
		}		
		class Configuration{
			[JsonProperty("target", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Vector3 target = new Vector3(0,0,0);	
			[JsonProperty("factionLib", ObjectCreationHandling = ObjectCreationHandling.Replace)]			
			public Dictionary<ulong, Dictionary<FactionController.FactionType,float>> factionLib=new Dictionary<ulong, Dictionary<FactionController.FactionType,float>>();			
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
			target=config.target;
			FactionController.factionScores = config.factionLib;
		}
		protected override void SaveConfig(){
			LogWarning($"Configuration changes saved to {Name}.json");
			config.target=target;
			config.factionLib=FactionController.factionScores;
			Config.WriteObject(config, true);
		}
		#endregion Configuration
		#region Faction logic		
			#region Utility
			public static Vector3 target;
			public BaseEntity getLookingAt(BasePlayer player){			
				RaycastHit hit;
				if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
					var entity = hit.GetEntity();
					if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
				}
				return null;
			}
			#endregion
			#region Chat commands
			[Command("hz_set")] private void surv_hotzone(IPlayer player, string command, string[] args){
				BasePlayer bp = (BasePlayer)player.Object;
				if(player.HasPermission("hotzone.set")){
					target=bp.transform.position;				
					SendChatMsg(bp,"Target set!" + target.ToString());
					SaveConfig();
				}
				else SendChatMsg(bp,"Missing permission!");
			
			}
			[Command("hz_get")] private void surv_info(IPlayer player, string command, string[] args){				
				BasePlayer bp = (BasePlayer)player.Object;
				FactionController fc = bp.GetComponent<FactionController>();
				SendChatMsg(bp,"Current Position:" + bp.transform.position.ToString());
				SendChatMsg(bp,"Current Target:" + target.ToString());
				SendChatMsg(bp,"Current Distance:" + Vector3.Distance(target,bp.transform.position).ToString());
				if(fc==null) return;
				SendChatMsg(bp,"Current Faction:"+bp.GetComponent<FactionController>().faction.ToString());
				foreach(KeyValuePair<FactionController.FactionType,float> ff in FactionController.factionScores[bp.userID]){
					SendChatMsg(bp,ff.Key.ToString() + ":" + ff.Value.ToString());
				}	
				
			}
			[Command("hz_save")] private void surv_save(IPlayer player, string command, string[] args){		
				BasePlayer bp = (BasePlayer)player.Object;
				SaveConfig();
				SendChatMsg(bp,"Saving!");
								
			}
			[Command("hz_reset")] private void surv_reset(IPlayer player, string command, string[] args){	
				foreach(HumanNPC hn in GameObject.FindObjectsOfType<HumanNPC>()){
					if(hn.GetComponent<FactionController>()!=null){
						GameObject.Destroy(hn.GetComponent<FactionController>());
					}
					swapSciRoamState(hn);		
				}					
			}
			[Command("hz_create")] private void surv_create(IPlayer player, string command, string[] args){	
				BasePlayer bp = (BasePlayer)player.Object;
				if(args[0]==null){
					SendChatMsg(bp,"Create what? [store]");
					return;
				}
				switch(args[0]){
					case "path":
						if(args[1]==null){
							SendChatMsg(bp,"Create what path?");
							return;
						}
						if(!config.pointGroups.ContainsKey(args[1]))config.pointGroups.Add(args[1],new List<Vector3>());
						config.pointGroups[args[1]].Add(bp.transform.position);
						break;
					case "point":
						if(args[1]==null){
							SendChatMsg(bp,"Create point for what group?");
							return;
						}
						if(!config.pointGroups.ContainsKey(args[1]))config.pointGroups.Add(args[1],new List<Vector3>());
						config.pointGroups[args[1]].Add(bp.transform.position);
						break;
				}
			}
			[Command("hz_assign")] private void surv_assign(IPlayer player, string command, string[] args){	
				BasePlayer bp = (BasePlayer)player.Object;
				if(args[0]==null){
					SendChatMsg(bp,"Assign what? [store]");
					return;
				}
				switch(args[0]){
					case "store":
						if(args[1]==null){
							SendChatMsg(bp,"Assign store to what faction?");
							return;
						}
						BaseEntity storeent = getLookingAt(bp);
						VendingMachine store = storeent.GetComponent<VendingMachine>();
						if(store==null){
							SendChatMsg(bp,"Not a vending machine");
							return;
						}
						string basicName = store.shopName.Replace("[Bandit]","").Replace("[Scientist]","").Replace("[Pacifist]","");
						switch(args[1].ToLower()){
							case "bandit":
								store.shopName = "[Bandit]"+basicName;
								break;
							case "scientist":
								store.shopName = "[Scientist]"+basicName;
								break;
							case "pacifist":
								store.shopName = "[Pacifist]"+basicName;
								break;
							default:
								SendChatMsg(bp,"Please enter a valid faction [bandit/scientist/pacifist].");								
								return;
						}
						store.UpdateMapMarker();
						return;
					case "npc":
						if(args[1]==null){
							SendChatMsg(bp,"Assign npc to what path?");
							return;
						}
						BaseEntity ent = getLookingAt(bp);
						if(ent.GetComponent<FactionController>()==null){
							SendChatMsg(bp,"Not a faction npc");
							return;
						}
						if(!config.pointGroups.ContainsKey(args[1])){config.pointGroups.Add(args[1],new List<Vector3>());}
						ent.GetComponent<FactionController>().ActivePointGroup = config.pointGroups[args[1]];
						return;
					case "path":
						if(args[1]==null){
							SendChatMsg(bp,"Assign path to what?");
							return;
						}
						if(args[2]==null){
							SendChatMsg(bp,"Assign what path?");
							return;
						}
						switch(args[1].ToLower()){
							case "point":
								if(!config.pointGroups.ContainsKey(args[2])) config.pointGroups.Add(args[2],new List<Vector3>());
								config.pointGroups[args[2]].Add(bp.transform.position);	
								return;
							case "npc":
								BaseEntity ent2 = getLookingAt(bp);
								if(ent2.GetComponent<FactionController>()==null){
									SendChatMsg(bp,"Not a faction npc");
									return;
								}
								if(!config.pointGroups.ContainsKey(args[2]))config.pointGroups.Add(args[2],new List<Vector3>());
								ent2.GetComponent<FactionController>().ActivePointGroup = config.pointGroups[args[2]];								
								return;
							default:
								SendChatMsg(bp,"Invalid parameter. Valid path arguments: NPC/Point");
								return;
						}
						return;
					case "point":
						if(args[1]==null){
							SendChatMsg(bp,"Assign "+bp.transform.position.ToString()+" to what group?");
							return;
						}
						if(!config.pointGroups.ContainsKey(args[1]))config.pointGroups.Add(args[1],new List<Vector3>());
						config.pointGroups[args[1]].Add(bp.transform.position);
						return;
				}				
				return;
			}		
			public void addPointToGroup(){}
			#endregion chatcmds
			//Oxide.Ext.RustEdit.NPC.NPCSpawner
			#region Faction Initializers
			void OnPlayerRespawned(BasePlayer player)=>initPlayer(player);
			void OnPlayerSleepEnded(BasePlayer player)=>initPlayer(player);
			void OnPlayerDisconnected(BasePlayer player, string reason)=>SaveConfig();
			object OnNPCAIInitialized(BaseAIBrain<HumanNPC> player){swapSciRoamState(player.GetComponent<HumanNPC>()); return null;}
			object OnPlayerDeath(BasePlayer player, HitInfo info){SaveConfig(); return null;}
			private void swapSciRoamState(HumanNPC s){
				if(s.IsNpc)Puts(((char)27)+"[96m"+"IsNpc! Did you fix NPCPlayer with dnSpy?");
				//Puts(s.spawnPos.ToString());
				if(s.Brain==null) return;
				((IAISleepable)s.Brain).SleepAI();
				s.Brain.Senses.senseTypes = (EntityType)67;
				s.Brain.Senses.senseFriendlies = true;
				s.Brain.Senses.hostileTargetsOnly = false;
				FactionController fc = s.gameObject.AddComponent<FactionController>();
				fc.faction=FactionController.FactionType.Bandit;
				fc.self=s;
				if(s.transform.name.ToLower().Contains("scientist")) 
					fc.faction=FactionController.FactionType.Scientist;
				s.Brain.states[AIState.Idle]=s.Brain.states[AIState.Roam];
				s.Brain.SwitchToState(AIState.Roam, s.Brain.currentStateContainerID);
				((IAISleepable)s.Brain).WakeAI();
				BaseNavigator bn = s.gameObject.GetComponent<BaseNavigator>();
				if(bn!=null){
					bn.Resume();
				}
				s.Brain.SwitchToState(AIState.Idle, s.Brain.currentStateContainerID);
			}
			void initPlayer(BasePlayer player){
				FactionController fc = player.gameObject.AddComponent<FactionController>();
				FactionController.changeScore(player, FactionController.FactionType.Both, 0.5f);
				FactionController.changeScore(player, FactionController.FactionType.Scientist, 0f);
				FactionController.changeScore(player, FactionController.FactionType.Bandit, 0f);
				SaveConfig();
			}
			#endregion initializers
			#region Faction NPC Handlers
			object OnAttackedAIEvent(AttackedAIEvent aievent, BasePlayer bp){	
				//Puts("OnAttackedAIEvent");
				if(bp==null)return null;
				FactionController shooter = bp.gameObject.GetComponent<FactionController>();
				FactionController victim = aievent.combatEntity.gameObject.GetComponent<FactionController>();
				//Puts("OnAttackedAIEvent2");
				if(shooter==null || victim==null) return null;
				//Puts(shooter.faction.ToString() + " : " + victim.faction.ToString());
				if(shooter.self==null) return null;
				//Puts("Shooter is NPC");
				BaseNavigator bn = aievent.combatEntity.gameObject.GetComponent<BaseNavigator>();
				if(bn!=null){
					bn.Stop();
					bn.SetDestination(bn.transform.position);
				}
				if(FactionController.validTarget(victim.GetComponent<BasePlayer>(),bp)) return null;
				aievent.combatEntity.lastAttacker=null;
				//Puts("Same Team");
				return victim;
			}
			object OnBasePlayerAttacked(BasePlayer victimbp, HitInfo info){
				//Puts("OnBasePlayerAttacked");
				if(victimbp==null)return null;
				FactionController victim = victimbp.gameObject.GetComponent<FactionController>();
				FactionController shooter = info.Initiator.gameObject.GetComponent<FactionController>();
				//Puts("OnBasePlayerAttacked");
				if(shooter==null || victim==null) return null;
				//Puts(shooter.faction.ToString() + " : " + victim.faction.ToString());
				BaseNavigator bn = victimbp.GetComponent<BaseNavigator>();
				if(bn!=null){
					bn.Stop();
				}
				if(shooter.self==null) return null;
				//Puts("Shooter is NPC");
				if(FactionController.validTarget(shooter.GetComponent<BasePlayer>(),victimbp)) return null;
				//Puts("Same Team");
				return victim;
			}
			object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info){
				if(info.HitEntity==null||info.Initiator==null) return null;
				if(info.HitEntity.gameObject==null||info.Initiator.gameObject==null) return null;
				FactionController shooter = info.Initiator.GetComponent<FactionController>();
				FactionController victim = info.HitEntity.gameObject.GetComponent<FactionController>();
				if(shooter!=null&&victim!=null){//
					if(shooter.self==null){
						BasePlayer bp = shooter.GetComponent<BasePlayer>();
						FactionController.changeScore(bp, victim.faction, -0.01f);
						FactionController.changeScore(bp, FactionController.FactionType.Both, -0.1f);
						if(victim.faction==FactionController.FactionType.Scientist)
							FactionController.changeScore(bp, FactionController.FactionType.Bandit, +0.005f);
						if(victim.faction==FactionController.FactionType.Bandit)
							FactionController.changeScore(bp, FactionController.FactionType.Scientist, +0.005f);
					}						
										
					if(shooter.faction==victim.faction){
						if(shooter.self==null){
							//Puts("Same team player, halving!");	
							info.damageTypes.ScaleAll(0.5f);
							return new object();
						}
						else{
							//Puts("Same team NPCs, ignoring!");
							return new object();
						}
						
					}
				}
				return null;
			}
			bool? OnIsThreat(HumanNPC hn, BaseEntity be){
				BasePlayer bp = be.GetComponent<BasePlayer>();
				if(bp == null){return false;}
				return (FactionController.validTarget((BasePlayer)hn,bp));
			}
			bool? OnIsTarget(HumanNPC hn, BaseEntity be){
				BasePlayer bp = be.GetComponent<BasePlayer>();
				if(bp == null){return false;}
				return (FactionController.validTarget((BasePlayer)hn,bp));
			}
			bool? OnIsFriendly(HumanNPC hn, BaseEntity be){/*
			if(hn.Brain.Senses.owner.transform.name==be.transform.name) return true;*/
				BasePlayer bp = be.GetComponent<BasePlayer>();
				if(bp == null){return true;}
				return !(FactionController.validTarget((BasePlayer)hn,bp));
			}
			bool? OnCaresAbout(AIBrainSenses hn, BaseEntity be){
				//*
				if(
					((be.GetComponent<BasePlayer>()==null))
					&& ((be.GetComponent<BaseNpc>()==null))
					) return false;
				if(be.GetComponent<BasePlayer>()!=null && hn.owner.GetComponent<BaseNpc>()!=null){
					if (be.GetComponent<BasePlayer>().IsConnected) return true;
					//else return false;
				}
				//*/
				return FactionController.validTarget(hn.owner, be);
			}
			/*
			Assign points by faction
			Walk towards closest point
			if in range of point, next point. If stuck, next list
			Follow point chain indefinitely
			if all lists exhausted, fall into random roam
			*/
			#endregion
			#region vending machine handlers
			object OnShopCompleteTrade(ShopFront entity){Puts("OnShopCompleteTrade works!"); return null;}
			object OnBuyVendingItem(VendingMachine machine, BasePlayer player, int sellOrderId, int numberOfTransactions){
				FactionController fc = player.GetComponent<FactionController>();
				if(fc==null)return fc;
				if(machine.shopName.Contains("Scientist")) FactionController.changeScore(player, FactionController.FactionType.Scientist, 0.001f*numberOfTransactions);
				else if(machine.shopName.Contains("Bandit")) FactionController.changeScore(player, FactionController.FactionType.Bandit, 0.001f*numberOfTransactions);
				else if(machine.shopName.Contains("Pacifist")) FactionController.changeScore(player, FactionController.FactionType.Both, 0.001f*numberOfTransactions);
				
				return null;
			}
			#endregion
			#region NPC destination handlers
			Vector3? OnGetRoamAnchorPosition(BaseAIBrain<HumanNPC> bn){
				BaseAIBrain<HumanNPC>.BasicAIState cs = bn.CurrentState;
				if(cs!=null){
						if(target!= new Vector3(0,0,0)){
							if(Vector3.Distance(target,bn.transform.position)<70f){
								//Puts("Destination set " +pos.ToString()+":"+ target.ToString());
								return bn.transform.position;
							}
							return target;							
						}
				}
				return null;
			}	
			Vector3? OnSetDestination(Vector3 pos, BaseNavigator bs){
					
				if(bs!=null){
					BaseAIBrain<HumanNPC> brain = bs.GetComponent<BaseAIBrain<HumanNPC>>();
					if(brain!=null){
						BaseAIBrain<HumanNPC>.BasicAIState cs = brain.CurrentState;
						if(cs!=null){
							if(!cs.ToString().ToLower().Contains("combat") && 
								!cs.ToString().ToLower().Contains("attack") &&
								!cs.ToString().ToLower().Contains("flee") && 
								!cs.ToString().ToLower().Contains("chase")
							)
							{
								if(target!= new Vector3(0,0,0)){
									Vector3 result;
									float dist = Vector3.Distance(target,brain.transform.position);
									if(dist>70)
										result=target;//
									else if(dist<70)
										result=brain.transform.position+brain.transform.forward + new Vector3(UnityEngine.Random.Range(-0.1f,0.1f),0,UnityEngine.Random.Range(-0.1f,0.1f));
									else if(dist<30	)
										result=brain.transform.forward+((target+brain.transform.position)/2);
									else
										result=(brain.transform.forward*5) + repulsePoint(brain.transform.position,target)+ new Vector3(UnityEngine.Random.Range(-0.1f,0.1f),0,UnityEngine.Random.Range(-0.1f,0.1f));								
									brain.transform.LookAt(result);
									return result;
								}
							}else if (cs.ToString().ToLower().Contains("cover")){
									Vector3 result;
								result=brain.transform.position+new Vector3(UnityEngine.Random.Range(-5f,5f),0,UnityEngine.Random.Range(-5f,5f));					
								brain.transform.LookAt(result);
								return result;
							}
						}
					}
				}
				return null;
			}
			#endregion
		#endregion
		private Vector3 repulsePoint(Vector3 t1, Vector3 t2){
			float factor=750f;//This is a guess, viva desmos
			float Distance = Vector3.Distance(t1,t2);
			return t1-((t2-t1)/(Distance/factor));
		}
		public class FactionController : MonoBehaviour{
			public enum FactionType{
				None,
				Scientist,
				Bandit,
				Both
			}
			public FactionType faction;
			public static Dictionary<ulong, Dictionary<FactionType,float>> factionScores = 
				new Dictionary<ulong, Dictionary<FactionType,float>>();//
			public List<Vector3> ActivePointGroup = new List<Vector3>();
			public int PointIndex;
			public HumanNPC self;
			
			public static bool changeScore(BasePlayer bp, FactionType ft, float score){
				try{
					if(bp==null)return false;
					if(ft==null)return false;
					FactionController fc = bp.GetComponent<FactionController>();
					if(fc==null)fc=bp.gameObject.AddComponent<FactionController>();
					ulong id= bp.userID;
					if(factionScores==null) return false;
					Dictionary<FactionType,float> selfFactions;
						factionScores.TryGetValue(id,out selfFactions);
					if(selfFactions==null) factionScores.Add(id, new Dictionary<FactionType,float>());
					float oldScore;//
					if(!(selfFactions.TryGetValue(ft,out oldScore))){
						oldScore=0.0f;
						selfFactions.Add(ft,oldScore);
					}
					oldScore+=score;
					factionScores[id][ft]=oldScore;
					if(factionScores[id][ft]>1)factionScores[id][ft]=1;
					if(factionScores[id][ft]<-1)factionScores[id][ft]=-1;
					float maxScore =-2f;
					List<FactionType> activeFactions = new List<FactionType>();
					foreach(KeyValuePair<FactionType,float> f in factionScores[id]){
						if(f.Value>0 && f.Key!=FactionType.None){
							maxScore=f.Value;
							activeFactions.Add(f.Key);
						}
					}
					switch(activeFactions.Count()){
						case 0 :
							fc.faction=FactionType.None;
							break;
						case 1 :
							fc.faction=activeFactions[0];
							break;
						default :
							fc.faction=FactionType.Both;
							break;
					}
					return true;
				}catch(Exception e){return false;}
			}
			public static bool validTarget(BaseEntity self, BaseEntity target){
			
				FactionController selfFC = self.GetComponent<FactionController>();
				FactionController targetFC = target.GetComponent<FactionController>();
				if(selfFC!=null&&targetFC!=null){
					bool result =  (!(targetFC.faction==FactionType.Both) && (
						targetFC.faction==FactionType.None ||
						selfFC.faction != targetFC.faction));
					return result;
				}else{}
				return false;
			}
		}
	}
}	//*/