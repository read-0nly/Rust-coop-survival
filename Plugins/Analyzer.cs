/*
    [ServerVar(Help = "Show user info for players on server.")]
    public static void users(ConsoleSystem.Arg arg)
    {
*/

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
using UnityEngine; 
using UnityEngine.AI; 
using  UnityEditor; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
	using Oxide.Core.Plugins;
using System.Threading;
	using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("Analyzer", "obsol", "0.0.1")]
	[Description("Server debugging tool for modders")]
	public class Analyzer : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,  "<color=#00ff00>[Analyzer]</color>", 0, Array.Empty<object>());
		string[] lights = {
				"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_3p_cool.prefab",
				"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_3p_warm.prefab",
				"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_cool.prefab",
				"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_warm.prefab",
				"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_red.prefab",
				"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_green.prefab"
			};
			string shield = "assets/prefabs/deployable/reactive target/reactivetarget_deployed.prefab";
		BaseEntity spawnLight(string s, BasePlayer bp, Vector3 offset){
				BaseEntity be = GameManager.server.CreateEntity(s, bp.transform.position+new Vector3(0,2f,0), Quaternion.LookRotation(new Vector3(1,0,0), Vector3.up), true);//
				be.transform.parent=bp.transform;
				be.transform.localPosition+=offset;
				Puts("Spawned Light");
				be.syncPosition=true;
				be.Spawn();
				be.syncPosition=true;
				be.SetFlag(BaseEntity.Flags.Reserved1,true);
				be.SetParent(bp,0, true, true);		
				return be;
		}
		BaseEntity spawnShield(string s, BasePlayer bp, Vector3 offset, Vector3 rot){
				BaseEntity be = GameManager.server.CreateEntity("assets/prefabs/deployable/search light/searchlight.deployed.prefab", bp.transform.position+new Vector3(0,0f,0), Quaternion.LookRotation(rot, Vector3.up), true);//
				be.Spawn();
				be.syncPosition=true;
				be.SetFlag(BaseEntity.Flags.Reserved1, true, false, true);
				return be;
		}
		object OnItemUse(Item i, int n)
        {
			if(i.ToString().Contains("cactus")){/*
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo
					{
						connection = i.parent.playerOwner.net.connection
					}, null, "AddUI", "[\n\t{\n\t\t\"name\": \"HotzoneFactionLogo\",\n\t\t\"parent\": \"Overlay\",\n\n\t\t\"components\":\n\t\t[\n\t\t\t{\n\t\t\t\t\"type\":\"UnityEngine.UI.RawImage\",\n\t\t\t\t\"imagetype\": \"Tiled\",\n\t\t\t\t\"color\": \"1.0 1.0 1.0 1.0\",\n\t\t\t\t\"url\": \"https://i.imgur.com/BnE1wVd.jpeg\",\n\t\t\t},\n\n\t\t\t{\n\t\t\t\t\"type\":\"RectTransform\",\n\t\t\t\t\"anchormin\": \"0.975 0.95\",\n\t\t\t\t\"anchormax\": \"1 1\"\n\t\t\t},\n\n\t\t\t{\n\t\t\t\t\"type\":\"NeedsCursor\"\n\t\t\t}\n\t\t]\n\t}\n]\n");
			*/
				if(i.GetOwnerPlayer()!=null)
					Puts(i.GetOwnerPlayer().transform.name);
				spawnShield(shield,i.GetOwnerPlayer(),new Vector3(0,0,3),new Vector3(0,0,0));
				
				
			}
			/*
			if(i.ToString().Contains("pumpkin")){
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo
				{
					connection = i.parent.playerOwner.net.connection
			}, null, "DestroyUI", "HotzoneFactionLogo");*ddf*/
			
			
			return null;
		}
		
		void OnServerInitialized()
		{
			 ReactiveTarget[] components = GameObject.FindObjectsOfType<ReactiveTarget>();
			 foreach(ReactiveTarget bp in components.ToArray()){
				 bp.Kill();
			 }
			
		}
		public Transform getLookingAtRaw(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.transform;
				return entity;
			}
			return null;
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