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
				target=config.target;
			}
			protected override void SaveConfig(){
				LogWarning($"Configuration changes saved to {Name}.json");
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
			private void swapSciRoamState(HumanNPC s){
			
				if(s.Brain==null) return;
				NavMeshAgent na = s.gameObject.GetComponent<NavMeshAgent>();
				if(na == null) return; 
				((IAISleepable)s.Brain).SleepAI();
				s.Brain.Senses.senseTypes = (EntityType)67;
				s.Brain.Senses.senseFriendlies = true;
				s.Brain.Senses.hostileTargetsOnly = false;
				if((Vector3.Distance(target, s.transform.position)<5f )){
					s.Brain.Events.RemoveAll();
					s.Brain.SwitchToState(AIState.Roam, s.Brain.currentStateContainerID);
					((IAISleepable)s.Brain).WakeAI();
				}else{
					s.Brain.SwitchToState(AIState.Roam, s.Brain.currentStateContainerID);
					((IAISleepable)s.Brain).WakeAI();
				}
			}	/*
			bool? OnIsThreat(HumanNPC hn, BaseEntity be){
				string[] sl1 = hn.transform.name.Split('/');
				string[] sl2 = (be?.transform.name).Split('/');
				string s1 = sl1[sl1.Length-1];
				string s2 = sl2[sl2.Length-1];				
				if(s1.Contains("scientist") && !s2.Contains("scientist")){
				}

				return false;
			}
			bool? OnIsTarget(HumanNPC hn, BaseEntity be){
				string[] sl1 = hn.transform.name.Split('/');
				string[] sl2 = (be?.transform.name).Split('/');
				string s1 = sl1[sl1.Length-1];
				string s2 = sl2[sl2.Length-1];				
				if(s1.Contains("scientist") && !s2.Contains("scientist")){
				}
				return false;
			}
			bool? OnIsFriendly(HumanNPC hn, BaseEntity be){
				if(hn.Brain.Senses.owner.transform.name==be.transform.name) return true;
				return false;
			}*/
			bool? OnCaresAbout(AIBrainSenses hn, BaseEntity be){
				if(
					(!(be.GetComponent<BasePlayer>()==null))
					&& (!(be.GetComponent<BaseNpc>()==null))
					) return false;
				if(be.GetComponent<BasePlayer>()!=null) if (be.GetComponent<BasePlayer>().IsConnected) return true;
				if(
					((hn.owner.GetComponent<BasePlayer>()==null)
					!= (be.GetComponent<BasePlayer>()==null)
					)) return false;
				if(hn.owner.transform.name==be.transform.name) return false;				
				if(hn.owner.transform.name.Contains("scientist") && be.transform.name.Contains("scientist")) return false;
				return true;
			}
			Vector3? OnSetDestination(Vector3 pos, BaseNavigator bs){
					
				if(bs!=null){
					BaseAIBrain<HumanNPC> brain = bs.GetComponent<BaseAIBrain<HumanNPC>>();
					if(brain!=null){
						BaseAIBrain<HumanNPC>.BasicAIState cs = brain.CurrentState;
						if(cs!=null){
							if(cs.ToString().ToLower().Contains("roam")){
								if(target!= new Vector3(0,0,0)){
									return target;			
									
								}
							}
						}
					}
				}
				return null;
			}
		#endregion
			object OnNPCAIInitialized(BaseAIBrain<HumanNPC> player)
			{
				swapSciRoamState(player.GetComponent<HumanNPC>());
				return null;
			}
	}
}	