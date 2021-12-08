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
	[Info("CustomBrainz", "obsol", "0.0.1")]
	[Description("Makes all scientist brains move towards a specified point on the map")]
	public class CustomBrainz : CovalencePlugin{	
		#region Generic Vars
			private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
			private void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,	"<color=#00ff00>[CustomBrainz]</color>", 0, Array.Empty<object>());
			bool debugOnBoot=false;
			
		#endregion
		#region Configuration
			private Configuration config;
			
			private void Init()
			{
				permission.RegisterPermission("custombrainz.spawn", this);				
				Subscribe("IsThreatForHumanNPC");			
				Subscribe("IsTargetForHumanNPC");
			}
			class Configuration{
				//[JsonProperty("debugOnBoot", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				//public bool debugOnBoot = false;
				[JsonProperty("target", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Vector3 target = new Vector3(0,0,0);				
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
				//debugOnBoot=config.debugOnBoot;
				target=config.target;
			}
			
			protected override void SaveConfig(){
				LogWarning($"Configuration changes saved to {Name}.json");
				//config.debugOnBoot=debugOnBoot;
				config.target=target;
				Config.WriteObject(config, true);
			}
		#endregion Configuration
		#region ScientistBrain			
			public static Vector3 target;
			[Command("cb_spawn")]
			private void surv_hotzone(IPlayer player, string command, string[] args)
			{
				BasePlayer bp = (BasePlayer)player.Object;
				if(player.HasPermission("custombrainz.spawn")){
					HumanNPC cn = CustomNPC.SpawnInitial(
						bp.transform.position,
						bp.transform.rotation,
						"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab",
						BaseNpc.AiStatistics.FamilyEnum.Scientist
					);				
					SendChatMsg(bp,"Target spawned?" + target.ToString());
					SaveConfig();
				}
				else SendChatMsg(bp,"Missing permission!");
				
			}	
			[Command("cb_swap")]
			private void cb_swap(IPlayer player, string command, string[] args){
				BasePlayer bp = (BasePlayer)player.Object;
				if(player.HasPermission("custombrainz.spawn")){
					RaycastHit hit;
					if (Physics.Raycast(bp.eyes.HeadRay(), out hit))
					{
						var entity = hit.GetEntity();
						
						if (entity != null)
						{
							SendChatMsg(bp,"doi permission!");	
						}
					}
				}
				else SendChatMsg(bp,"Missing permission!");
			}
			public bool IsThreat(HumanNPC item, BaseEntity amountToUse){
				Puts("TargetAttempt!");
				return 0;//
			}
			public bool IsTarget(HumanNPC item, BaseEntity amountToUse){
				Puts("TargetAttempt!");
				return 0;//
			}
			
			public class CustomNPC : ScientistNPC{
				public BaseNpc.AiStatistics Stats;
				/*
					public void Load(ProtoBuf.AIDesign design, BaseEntity owner)
					{
					this.Scope = (AIDesignScope) design.scope;
					this.DefaultStateContainerID = design.defaultStateContainer;
					this.Description = design.description;
					this.InitStateContainers(design, owner);
					}
					
					
					public ProtoBuf.AIDesign ToProto(int currentStateID)
					{
					ProtoBuf.AIDesign aiDesign = new ProtoBuf.AIDesign();
					aiDesign.description = this.Description;
					aiDesign.scope = (int) this.Scope;
					aiDesign.defaultStateContainer = this.DefaultStateContainerID;
					aiDesign.availableStates = new List<int>();
					foreach (AIState availableState in this.AvailableStates)
					aiDesign.availableStates.Add((int) availableState);
					aiDesign.stateContainers = new List<ProtoBuf.AIStateContainer>();
					foreach (AIStateContainer aiStateContainer in this.stateContainers.Values)
					aiDesign.stateContainers.Add(aiStateContainer.ToProto());
					aiDesign.intialViewStateID = currentStateID;
					return aiDesign;
					}
				*/
				
				//Spawn scientist/bandit, then set family, capture state container id, swap brain and body interface then attach state container and wake
				public static HumanNPC SpawnInitial(Vector3 pos, Quaternion rot, string prefabPath, BaseNpc.AiStatistics.FamilyEnum fam){
					
					Debug.Log("Spawning");
					if (!string.IsNullOrEmpty(prefabPath))
					{
						Debug.Log("- Creating from prefab path");
						BaseEntity entity = GameManager.server.CreateEntity(prefabPath, pos, rot, false);
						Debug.Log("- Lobotomizing");
						int i = Lobotomize(entity);
						Debug.Log("- Inserting new brain");
						return InsertCustom(entity, fam,i);				
					}
					Debug.Log("Spawn failed - Provide a prefab path");
					return null;
					
				}
				public static CustomNPC SwapBrain(BaseEntity entity, BaseNpc.AiStatistics.FamilyEnum fam){
					try{
						
						Debug.Log("-- Fetching old brain");
						HumanNPC hn = entity.gameObject.GetComponent<HumanNPC>() as HumanNPC;
						Debug.Log(hn==null);
						Debug.Log("-- UpgradeNPC");
						UpgradeNPC(ref hn);
						Debug.Log(hn==null);
						Debug.Log("-- UpgradeNPC Done");
						Debug.Log("-- UpgradeBraine ");
						BaseAIBrain<HumanNPC> hb = entity.gameObject.GetComponent<BaseAIBrain<HumanNPC>>() as BaseAIBrain<HumanNPC>;
						Debug.Log(hb==null);
						UpgradeBrain(ref hb);
						Debug.Log("-- UpgradeBraine Done");
						/*hn  = (HumanNPC)((entity.gameObject.GetComponent<HumanNPC>() ) as CustomNPC);
							Debug.Log("-- Fetching old brain");
							BaseAIBrain<HumanNPC> hb = entity.gameObject.GetComponent<BaseAIBrain<HumanNPC>>();
							Debug.Log("-- Fetching old brain");
							hb  = (BaseAIBrain<HumanNPC>)((entity.gameObject.GetComponent<BaseAIBrain<HumanNPC>>()) as CustomBrain);
							Debug.Log("-- Fetching old brain");
						CustomBrain cb = (CustomBrain)hb;*/
						Debug.Log(hb==null);
						hb.SwitchToState(AIState.Roam, hb.currentStateContainerID);
						Debug.Log("-- Fetching");
						
					}catch(Exception e){Debug.Log(e.ToString());}
					return null;
				}
				public static void UpgradeNPC(ref HumanNPC hn){
					Debug.Log("-- Fetching old 1");
					hn.gameObject.AddComponent<CustomNPC>();
					Destroy(hn);
					CustomNPC cn = (hn as CustomNPC);
					
					Debug.Log("-- Fetching old 2");
				}
				public static void UpgradeBrain(ref BaseAIBrain<HumanNPC> hn){
					Debug.Log("-- Fetching old 1");					
					hn=(BaseAIBrain<HumanNPC>)(hn as CustomBrain);
					Debug.Log("-- Fetching old 2");
				}
				public static int Lobotomize(BaseEntity entity){ //returns stateContainerId
					Debug.Log("-- Fetching old brain");
					HumanNPC oldNPC = entity.gameObject.GetComponent<HumanNPC>();
					BaseAIBrain<HumanNPC> oldBrain = entity.gameObject.GetComponent<BaseAIBrain<HumanNPC>>();	
					Debug.Log("--- Get Container ID");
					int statecontainer = oldBrain.currentStateContainerID;
					Debug.Log("--- Meatgrinder");
					Destroy(oldNPC);
					Destroy(oldBrain);	
					Debug.Log("-- Ding!");
					return statecontainer;
				}
				public static HumanNPC InsertCustom(BaseEntity entity, BaseNpc.AiStatistics.FamilyEnum fam, int statecontainer){
					try{
						Debug.Log("-- Inserting new brain");
						CustomBrain cb = entity.gameObject.AddComponent<CustomBrain>() as CustomBrain;
						cb.currentStateContainerID = statecontainer;
						CustomNPC cn = entity.gameObject.AddComponent<CustomNPC>() as CustomNPC;
						//cn.Stats.Family = fam;
						if ((bool) (UnityEngine.Object) entity)
						{
							Debug.Log(cn==null);
							Debug.Log("State Switch");
							cb.SwitchToState(AIState.Combat, statecontainer);
							Debug.Log("Wake");
							cb.AddStates();
							((IAISleepable)cb).WakeAI();
							Debug.Log("Insert succeeded. Is AI working?");
							return cn;
						}
						Debug.Log("Insert failed, entity null");
						return null;
					}
					catch(Exception e){
						Debug.Log("Insert failed with exception");
						return null;
					}
				}
				
				
				//this.Stats.Family = FamilyEnum.[ Bear | Wolf | Deer | Boar | Chicken | Horse | Zombie | Scientist | Murderer | Player ]
				public bool IsThreat(BaseEntity entity)
				{
					BaseNpc baseNpc = entity as BaseNpc;
					if ((UnityEngine.Object) baseNpc != (UnityEngine.Object) null) return baseNpc.Stats.Family != this.Stats.Family && this.IsAfraidOf(baseNpc.Stats.Family);
					BasePlayer basePlayer = entity as BasePlayer;
					return (UnityEngine.Object) basePlayer != (UnityEngine.Object) null && this.IsAfraidOf(basePlayer.Family);
				}
				
				public bool IsAfraidOf(BaseNpc.AiStatistics.FamilyEnum family)
				{
					Debug.Log("-- roam old brain");
					foreach (BaseNpc.AiStatistics.FamilyEnum familyEnum in this.Stats.IsAfraidOf)
					{
						if (family == familyEnum)
						return true;
					}
					return false;
				}
				
				
				public bool IsTarget(BaseEntity entity)
				{
					BaseNpc baseNpc = entity as BaseNpc;
					return (!((UnityEngine.Object) baseNpc != (UnityEngine.Object) null) || baseNpc.Stats.Family != this.Stats.Family) && !this.IsThreat(entity);
				}
				
				public bool IsFriendly(BaseEntity entity) => (entity as BaseNpc).Stats.Family == this.Stats.Family ;
			}
			public class CustomBrain : ScientistBrain{
				/*
					Set up an array of vectors
					if position in range, increment counter to switch target
					modulus to loop, direction to pingpong, switch to combat otherwise
					step distance and randomization to not lock them in too long
					currentStateContainerID
					
				*/
				public static int Count;
				public override void AddStates()
				{
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.ChaseState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.CombatState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.TakeCoverState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.CoverState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.MountedState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.DismountedState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new BaseAIBrain<HumanNPC>.BaseFollowPathState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new BaseAIBrain<HumanNPC>.BaseNavigateHomeState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.CombatStationaryState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new BaseAIBrain<HumanNPC>.BaseMoveTorwardsState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.IdleState());
					this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.RoamState());
				}
				public class IdleState : ScientistBrain.IdleState
				{
					private StateStatus status = StateStatus.Error;
					private AIMovePoint roamPoint;
					
					public override void StateLeave()
					{
						base.StateLeave();
						this.Stop();
						this.ClearRoamPointUsage();
					}
					
					public override void StateEnter()
					{
						base.StateEnter();
						Debug.Log("-- roam old brain");
						this.status = StateStatus.Error;
						this.ClearRoamPointUsage();
						HumanNPC entity = this.GetEntity();
						if (this.brain.PathFinder == null)
						return;
						this.status = StateStatus.Error;
						this.roamPoint = this.brain.PathFinder.GetBestRoamPoint(entity.transform.position, entity.transform.position, entity.eyes.BodyForward(), this.brain.Navigator.MaxRoamDistanceFromHome, this.brain.Navigator.BestRoamPointMaxDistance);
						if (!(this.roamPoint != null))
						return;
						if (this.brain.Navigator.SetDestination(this.roamPoint.transform.position, BaseNavigator.NavigationSpeed.Slow))
						{
							this.roamPoint.SetUsedBy((BaseEntity) this.GetEntity());
							this.status = StateStatus.Running;
						}
						else
						this.roamPoint.SetUsedBy((BaseEntity) entity, 600f);
					}
					
					private void ClearRoamPointUsage()
					{
						if (!(this.roamPoint != null))
						return;
						this.roamPoint.ClearIfUsedBy((BaseEntity) this.GetEntity());
						this.roamPoint = (AIMovePoint) null;
					}
					
					private void Stop() => this.brain.Navigator.Stop();
					
					public override StateStatus StateThink(float delta)
					{
						if (this.status == StateStatus.Error)
						return this.status;
						return this.brain.Navigator.Moving ? StateStatus.Running : StateStatus.Finished;
					}
				}
				
				
				public class RoamState : ScientistBrain.RoamState
				{
					private StateStatus status = StateStatus.Error;
					private AIMovePoint roamPoint;
					
					public override void StateLeave()
					{
						base.StateLeave();
						this.Stop();
						this.ClearRoamPointUsage();
					}
					
					public override void StateEnter()
					{
						base.StateEnter();
						Debug.Log("-- roam old brain");
						this.status = StateStatus.Error;
						this.ClearRoamPointUsage();
						HumanNPC entity = this.GetEntity();
						if (this.brain.PathFinder == null)
						return;
						this.status = StateStatus.Error;
						this.roamPoint = this.brain.PathFinder.GetBestRoamPoint(this.GetRoamAnchorPosition(), entity.transform.position, entity.eyes.BodyForward(), this.brain.Navigator.MaxRoamDistanceFromHome, this.brain.Navigator.BestRoamPointMaxDistance);
						if (!(this.roamPoint != null))
						return;
						if (this.brain.Navigator.SetDestination(this.roamPoint.transform.position, BaseNavigator.NavigationSpeed.Slow))
						{
							this.roamPoint.SetUsedBy((BaseEntity) this.GetEntity());
							this.status = StateStatus.Running;
						}
						else
						this.roamPoint.SetUsedBy((BaseEntity) entity, 600f);
					}
					
					private void ClearRoamPointUsage()
					{
						if (!(this.roamPoint != null))
						return;
						this.roamPoint.ClearIfUsedBy((BaseEntity) this.GetEntity());
						this.roamPoint = (AIMovePoint) null;
					}
					
					private void Stop() => this.brain.Navigator.Stop();
					
					public override StateStatus StateThink(float delta)
					{
						if (this.status == StateStatus.Error)
						return this.status;
						return this.brain.Navigator.Moving ? StateStatus.Running : StateStatus.Finished;
					}
				}
			}
		#endregion ScientistBrain
	}
}



//NPC Spawner notes 
/*	
	
	
*/
