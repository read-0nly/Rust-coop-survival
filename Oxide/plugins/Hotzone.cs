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
				Puts(bp.GetComponent<FactionController>().faction.ToString());
				if(player.HasPermission("hotzone.set")){
					target=bp.transform.position;				
					SendChatMsg(bp,"Target set!" + target.ToString());
					SaveConfig();
				}
				else SendChatMsg(bp,"Missing permission!");
					
			}
			[Command("hz_get")]
			private void surv_info(IPlayer player, string command, string[] args)
			{
				BasePlayer bp = (BasePlayer)player.Object;
				Puts(bp.GetComponent<FactionController>().faction.ToString());
				SendChatMsg(bp,bp.GetComponent<FactionController>().faction.ToString());
					
			}
			[Command("hz_reset")]
			private void surv_reset(IPlayer player, string command, string[] args)
			{	foreach(HumanNPC hn in GameObject.FindObjectsOfType<HumanNPC>()){
					if(hn.GetComponent<FactionController>()!=null){
						GameObject.Destroy(hn.GetComponent<FactionController>());
					}
					swapSciRoamState(hn);		
				}					
			}
			private void swapSciRoamState(HumanNPC s){
			
				if(s.Brain==null) return;
				NavMeshAgent na = s.gameObject.GetComponent<NavMeshAgent>();
				if(na == null) return; 
				s.Brain.Senses.senseTypes = (EntityType)67;
				s.Brain.Senses.senseFriendlies = true;
				s.Brain.Senses.hostileTargetsOnly = false;
				FactionController fc = s.gameObject.AddComponent<FactionController>();
				fc.changeFactionScore(FactionController.FactionType.Bandit,0.5f);
				if(s.transform.name.ToLower().Contains("scientist")) fc.changeFactionScore(FactionController.FactionType.Scientist,0.7f);
				if((Vector3.Distance(target, s.transform.position)<5f )){
					s.Brain.Events.RemoveAll();
					s.Brain.SwitchToState(AIState.Roam, s.Brain.currentStateContainerID);
					((IAISleepable)s.Brain).WakeAI();
				}else{
					s.Brain.SwitchToState(AIState.Roam, s.Brain.currentStateContainerID);
					((IAISleepable)s.Brain).WakeAI();
				}
			}
			void OnPlayerRespawned(BasePlayer player)
			{
				Puts("OnPlayerRespawned works!");
				FactionController fc = player.gameObject.AddComponent<FactionController>();
				fc.changeFactionScore(FactionController.FactionType.None,0.1f);		
				Puts(fc.faction.ToString());
			}
			void OnPlayerSleepEnded(BasePlayer player)
			{
				Puts("OnPlayerSleepEnded works!");
				FactionController fc = player.gameObject.AddComponent<FactionController>();
				fc.changeFactionScore(FactionController.FactionType.None,0.1f);		
				Puts(fc.faction.ToString());
			}
			void OnPlayerConnected(BasePlayer player)
			{
				Puts("Spawn and set");
				FactionController fc = player.gameObject.AddComponent<FactionController>();
				fc.changeFactionScore(FactionController.FactionType.None,0.1f);		
				Puts(fc.faction.ToString());
			}
			bool? OnIsThreat(HumanNPC hn, BaseEntity be){
				return false;

			}
			bool? OnIsTarget(HumanNPC hn, BaseEntity be){
				return false;
			}
			bool? OnIsFriendly(HumanNPC hn, BaseEntity be){/*
			if(hn.Brain.Senses.owner.transform.name==be.transform.name) return true;*/
				return true;
			}
			bool? OnCaresAbout(AIBrainSenses hn, BaseEntity be){
				//*
				if(
					((be.GetComponent<BasePlayer>()==null))
					&& ((be.GetComponent<BaseNpc>()==null))
					) return false;
				if(be.GetComponent<BasePlayer>()!=null && hn.owner.GetComponent<BaseNpc>()!=null){
					if (be.GetComponent<BasePlayer>().IsConnected) return true;
					else return false;
				}
				//*/
				return FactionController.validTarget(hn.owner, be);
			}
			Vector3? OnSetDestination(Vector3 pos, BaseNavigator bs){
					
				if(bs!=null){
					BaseAIBrain<HumanNPC> brain = bs.GetComponent<BaseAIBrain<HumanNPC>>();
					if(brain!=null){
						BaseAIBrain<HumanNPC>.BasicAIState cs = brain.CurrentState;
						if(cs!=null){
							if(cs.ToString().ToLower().Contains("roam")){
								if(target!= new Vector3(0,0,0)){
									if(Vector3.Distance(target,brain.transform.position)<5f){
										//Puts("Destination set " +pos.ToString()+":"+ target.ToString());
										return null;
									}
									return target + new Vector3(UnityEngine.Random.Range(-5.0f,5.0f),0,UnityEngine.Random.Range(-5.0f,5.0f));
									
								}
								else{return null;}
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
			Puts("IsInit");
			return null;
		}
		public class FactionController : MonoBehaviour{
			public enum FactionType{
				None,
				Scientist,
				Bandit,
				Pacifist
			}
			public FactionType faction;
			public Dictionary<FactionType,float> factionScores = new Dictionary<FactionType,float>();
			public void changeFactionScore(FactionType fc, float score){
				if(!factionScores.ContainsKey(fc)) factionScores.Add(fc,0);
				factionScores[fc]+=score;
				if(factionScores[fc]>1){
					factionScores[fc]=1;//
				}
				if(factionScores[fc]<-1){
					factionScores[fc]=-1;
				}
				float max = 0;
				faction = fc;
				Debug.Log("first:" +fc.ToString());
				foreach(KeyValuePair<FactionType, float> pair in factionScores){
					if(factionScores[pair.Key]>max){
						max = factionScores[pair.Key];
						faction=pair.Key;
					}
				}
				Debug.Log("finally:" +faction.ToString());
			}
			public static bool validTarget(BaseEntity self, BaseEntity target){
			
				FactionController selfFC = self.GetComponent<FactionController>();
				FactionController targetFC = target.GetComponent<FactionController>();
				if(selfFC!=null&&targetFC!=null){
					return (!(targetFC.faction==FactionType.Pacifist) && (
						targetFC.faction==FactionType.None ||
					selfFC.faction != targetFC.faction));
					}
				return false;
			}
		}
	}
}	