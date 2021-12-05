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
	[Info("Hotzone", "obsol", "0.0.1")]
	[Description("Makes all scientist brains move towards a specified point on the map")]
	public class Hotzone : CovalencePlugin{	
		#region Generic Vars
			private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
			private void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,  "<color=#00ff00>[Hotzone]</color>", 0, Array.Empty<object>());
			bool debugOnBoot=false;
			
		#endregion
		#region Configuration
			private Configuration config;
			
			private void Init()
			{
				permission.RegisterPermission("hotzone.set", this);
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
			[Command("hz_set")]
			private void surv_hotzone(IPlayer player, string command, string[] args)
			{
				BasePlayer bp = (BasePlayer)player.Object;
				if(player.HasPermission("hotzone.set")){
					target=bp.transform.position;				
					SendChatMsg(bp,"Target set!" + target.ToString());
					SaveConfig();
				}
				else SendChatMsg(bp,"Missing permission!");
					
			}	
			private class RoamState : ScientistBrain.RoamState{
				private StateStatus status = StateStatus.Error;
				private AIMovePoint roamPoint;
				public Vector3 target;			
				public override void StateEnter(){
					this.Reset();
					ScientistNPC entity = (ScientistNPC)this.GetEntity();
					this.brain.Navigator.Warp(entity.transform.position);
					this.status = StateStatus.Error;
					if(roamPoint!=null){
						this.roamPoint.ClearIfUsedBy((BaseEntity) this.GetEntity());
						this.roamPoint = (AIMovePoint) null;
					}
					if (this.brain.PathFinder == null) return;
					if(this.target == new Vector3(0,0,0)){
						this.roamPoint = this.brain.PathFinder.GetBestRoamPoint(
							this.GetRoamAnchorPosition(), 
							entity.transform.position, 
							entity.eyes.BodyForward(), 
							-1, 
							this.brain.Navigator.BestRoamPointMaxDistance
						);
					}
					else{
						this.roamPoint = this.brain.PathFinder.GetBestRoamPoint(
							this.target, 
							entity.transform.position, 
							entity.eyes.BodyForward(), 
							-1, 
							10
						);					
					}
					if ((Vector3.Distance(target, entity.transform.position)>5f && target != new Vector3(0,0,0))){
						if (Vector3.Distance(target, entity.transform.position)>10f){
							this.brain.Navigator.SetDestination((target) ,BaseNavigator.NavigationSpeed.Fast);
							this.status = StateStatus.Running;
						}
						else{
							this.brain.Navigator.SetDestination((target) ,BaseNavigator.NavigationSpeed.Slowest);
							this.status = StateStatus.Running;
							
						}
					}
					else if((this.roamPoint !=	null)){
						if (this.brain.Navigator.SetDestination(this.roamPoint.transform.position, BaseNavigator.NavigationSpeed.Slow)){
							this.roamPoint.SetUsedBy((BaseEntity) this.GetEntity());//
							this.status = StateStatus.Running;
						}
					}//
				}			
				private void ClearRoamPointUsage(){
					if (!(this.roamPoint != null)) return;
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
			private void swapSciRoamState(ScientistNPC s){
				if(s.IsDormant||s.Brain==null) return;
				NavMeshAgent na = s.gameObject.GetComponent<NavMeshAgent>();
				if(na == null) return; 
				RoamState rs=new RoamState();
				rs.brain=s.Brain;
				rs.target=target;
				if(s.Brain.states[rs.StateType].GetType() != rs.GetType() || ((RoamState)s.Brain.states[rs.StateType])?.target != target){
					s.Brain.states.Remove(rs.StateType);
					s.Brain.AddState(rs);
				}		
				if((Vector3.Distance(target, s.transform.position)<5f )){
					s.Brain.Events.RemoveAll();
					s.Brain.SwitchToState(AIState.Combat, s.Brain.currentStateContainerID);
					((IAISleepable)s.Brain).WakeAI();
				}else{
					((IAISleepable)s.Brain).WakeAI();
				}
			}
		#endregion
		private void OnServerInitialized(){			
			LoadConfig();		
			List<ScientistNPC> list2 = Resources.FindObjectsOfTypeAll<ScientistNPC>().ToList();
			if(list2!=null && target !=null && target != new Vector3(0,0,0)){
				foreach(ScientistNPC s in list2){swapSciRoamState(s);Puts(s.transform.name);}
			}
            timer.Every(47f, () => {
				List<ScientistNPC> list = Resources.FindObjectsOfTypeAll<ScientistNPC>().ToList();
				if(list!=null && target != new Vector3(0,0,0)){
					foreach(ScientistNPC s in list){
						swapSciRoamState(s);
					}
				}
			});
		}
	}
}