/*

    [ServerVar(Help = "Show user info for players on server.")]
    public static void users(ConsoleSystem.Arg arg)
    {
*/

using Network;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core;
using Rust.Ai;
using UnityEngine; 
using System;
using System.Collections.Generic;
using System.Collections;

namespace Oxide.Plugins
{
	[Info("Analyzer", "your-name-here", "0.0.1")]
	[Description("Server debugging tool for modders")]
	public class Analyzer : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) =>
		_rustPlayer.Message(pl, msg,  "<color=#00ff00>[Analyzer]</color>", 0, Array.Empty<object>());
		
		
		class Configuration{
			
			[JsonProperty("SomeValue", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public int SomeValue=0
			
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
		
		private void OnServerInitialized()
        {
			//Set yourself up
		}
		private void OnItemUse(Item i, int n)
        {
			if(i.ToString().Contains("cactus")){
				//Do thing triggered by item use
			}
		}
		[Command("a?")] void analyze(IPlayer player, string command, string[] args){	
			BasePlayer bp = (BasePlayer)player.Object;
			SendChatMsg(bp, "<color=#00FFFF>Analyzing</color>");			
			
		}
		private BaseEntity getLookingAt(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
			}
			return null;
		}
	}
}
