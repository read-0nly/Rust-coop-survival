
using Convert = System.Convert;
using Network;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Rust.Ai;
using System;
using System.IO;
using UnityEngine; 
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("Cordyceps", "obsol", "0.0.1")]
	[Description("A baseline for swapping out custom AI state handlers without lobotomies.")]
	public class Cordyceps : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) => _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Cordyceps]</color>", 0, Array.Empty<object>());	
		public static Dictionary<string,Dictionary<AIState,BaseAIBrain<HumanNPC>.BasicAIState>> StateAssignments = 
			new Dictionary<string,Dictionary<AIState,BaseAIBrain<HumanNPC>.BasicAIState>>();
		public static Dictionary<string,Dictionary<AIState,BaseAIBrain<BaseAnimalNPC>.BasicAIState>> AnimalStateAssignments = 
			new Dictionary<string,Dictionary<AIState,BaseAIBrain<BaseAnimalNPC>.BasicAIState>>();
		public Configuration config;
		public bool WalkableOnly = false;
		
		public class Configuration
		{
			[JsonProperty("Name", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string name = "";
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
			
		}
		protected override void LoadDefaultConfig() => config = new Configuration();//
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					LogWarning("Configuration appears to be outdated; updating and saving");SaveConfig();}
			}
			catch(Exception e){LogWarning($"Configuration file {Name}.json is invalid; using defaults");LogWarning(e.ToString());LoadDefaultConfig();}
		}
		protected override void SaveConfig(){
			LogWarning($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}		
		private void OnServerInitialized()
        {
			LoadConfig();
		}
		
		// Requires custom injection : BaseAIBrain.InitializeAI() injection index 0, continue, just this//
		void OnInitializeAI(BaseAIBrain<HumanNPC> brain)
		{
			ApplyDesign(brain.GetEntity());
		}
		void OnInitializeAI(AnimalBrain brain)
		{
			ApplyDesign(brain.GetEntity());
		}
		
		public bool ApplyDesign(BaseEntity entity){
			HumanNPC hn = (entity as HumanNPC);
			BaseAnimalNPC bn = (entity as BaseAnimalNPC);
			if(hn==null){
				if(bn!=null){
					return SwapAnimalState(bn.brain);
				}
				return false;
			}else{		
				hn.GetComponent<BaseNavigator>().PlaceOnNavMesh();
				return SwapHumanState(hn.Brain);
			}
		}
		public bool AssignHumanState(string prefabname, AIState stateType, BaseAIBrain<HumanNPC>.BasicAIState state){
			if(!StateAssignments.ContainsKey(prefabname)){
				StateAssignments.Add(prefabname,new Dictionary<AIState,BaseAIBrain<HumanNPC>.BasicAIState>());
			}
			if(!StateAssignments[prefabname].ContainsKey(stateType)){
				StateAssignments[prefabname].Add(stateType,state);
			}else{
				StateAssignments[prefabname][stateType]=state;
			}
			return true;
		}
		public bool AssignAnimalState(string prefabname, AIState stateType, BaseAIBrain<BaseAnimalNPC>.BasicAIState state){
			if(!AnimalStateAssignments.ContainsKey(prefabname)){
				AnimalStateAssignments.Add(prefabname,new Dictionary<AIState,BaseAIBrain<BaseAnimalNPC>.BasicAIState>());
			}
			if(!AnimalStateAssignments[prefabname].ContainsKey(stateType)){
				AnimalStateAssignments[prefabname].Add(stateType,state);
			}else{
				AnimalStateAssignments[prefabname][stateType]=state;
			}
			return true;
		}
		public bool SwapHumanState(BaseAIBrain<HumanNPC> brain,AIState stateType,BaseAIBrain<HumanNPC>.BasicAIState state){
			try{
				if(brain.Navigator==null){return false;}
				if(WalkableOnly){
					if(brain.Navigator.DefaultArea!="Walkable") return true;
				}
				
				brain.Navigator.defaultAreaMask = UnityEngine.AI.NavMesh.AllAreas;
				brain.Navigator.navMeshQueryFilter.areaMask= UnityEngine.AI.NavMesh.AllAreas;
				if (brain.states.ContainsKey(stateType)){
					if(brain.CurrentState!=null)
						if(brain.CurrentState.StateType==stateType)
							brain.states[stateType].StateLeave();
				}
				BaseAIBrain<HumanNPC>.BasicAIState state2 = (BaseAIBrain<HumanNPC>.BasicAIState)System.Activator.CreateInstance(state.GetType());
				state2.brain = brain;
				brain.states[stateType]=state2;
				return true;
			}catch(Exception e){
				return false;
			}
		}
		public bool SwapAnimalState(BaseAIBrain<BaseAnimalNPC> brain,AIState stateType,BaseAIBrain<BaseAnimalNPC>.BasicAIState state){
			try{
				if (brain.states.ContainsKey(stateType)){
					if(brain.CurrentState!=null)
						if(brain.CurrentState.StateType==stateType)
							brain.states[stateType].StateLeave();
				}
				
				Puts("Applying Animal States" + stateType.ToString());
				BaseAIBrain<BaseAnimalNPC>.BasicAIState state2 = (BaseAIBrain<BaseAnimalNPC>.BasicAIState)System.Activator.CreateInstance(state.GetType());
				state2.brain = brain;
				brain.states[stateType]=state2;
				return true;
			}catch(Exception e){
				return false;
			}
		}
		public bool SwapAnimalState(BaseAIBrain<BaseAnimalNPC> brain){
			bool result = false;
			if(AnimalStateAssignments.ContainsKey(brain.gameObject.transform.name)){
				foreach(AIState state in AnimalStateAssignments[brain.gameObject.transform.name].Keys){
					bool stateResult = (SwapAnimalState(brain,state,AnimalStateAssignments[brain.gameObject.transform.name][state]));
					result = result || stateResult;
				}
				return result;	
			}
			return false;
		}

		public bool SwapHumanState(BaseAIBrain<HumanNPC> brain){
			bool result = false;
			if(StateAssignments.ContainsKey(brain.gameObject.transform.name)){
				foreach(AIState state in StateAssignments[brain.gameObject.transform.name].Keys){
					bool stateResult = (SwapHumanState(brain,state,StateAssignments[brain.gameObject.transform.name][state]));
					result = result || stateResult;
				}
				return result;	
			}
			return false;
		}

	}
}

