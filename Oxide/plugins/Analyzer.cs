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
			
			
		bool keepLooping = true;
		void Loaded(){
			timer.Once(200f,()=>{
				looper();
				
			});
		}
		void OnStartServer(){
				scanAll();
			
		}
		void looper(){
			if(keepLooping){
			}
		}
		void scanAll(){
			
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(MonumentNavMesh));
			foreach(MonumentNavMesh go in GameobjectList){
				Puts("["+(go.transform.parent==null?go.transform.ToString():go.transform.ToString())+" : "+go.NavMeshAgentTypeIndex.ToString()+" : "+ NavMesh.GetSettingsByIndex(go.NavMeshAgentTypeIndex).agentTypeID.ToString()+"]");
				go.NavMeshAgentTypeIndex=0;
				go.gameObject.SetActive(false);
				go.gameObject.SetActive(true);
			}
			GameobjectList = Resources.FindObjectsOfTypeAll(typeof(DungeonNavmesh));
			foreach(DungeonNavmesh go in GameobjectList){
				Puts("["+(go.transform.parent==null?go.transform.ToString():go.transform.ToString())+" : "+go.NavMeshAgentTypeIndex.ToString()+" : "+ NavMesh.GetSettingsByIndex(go.NavMeshAgentTypeIndex).agentTypeID.ToString()+"]");
				go.NavMeshAgentTypeIndex=0;
				go.gameObject.SetActive(false);
				go.gameObject.SetActive(true);
			}
			GameobjectList = Resources.FindObjectsOfTypeAll(typeof(DynamicNavMesh));
			foreach(DynamicNavMesh go in GameobjectList){
				Puts("["+(go.transform.parent==null?go.transform.ToString():go.transform.ToString())+" : "+go.NavMeshAgentTypeIndex.ToString()+" : "+ NavMesh.GetSettingsByIndex(go.NavMeshAgentTypeIndex).agentTypeID.ToString()+"]");
				go.NavMeshAgentTypeIndex=0;
				go.gameObject.SetActive(false);
				go.gameObject.SetActive(true);
			}
			GameobjectList = Resources.FindObjectsOfTypeAll(typeof(NavMeshSurface));
			NavMeshSurface last;
			(GameobjectList[0] as NavMeshSurface).size+=(GameobjectList[0] as NavMeshSurface).size;
			foreach(NavMeshSurface go in GameobjectList){
				Puts("["+(go.transform.parent==null?go.transform.ToString():go.transform.parent.ToString())+" : "+go.agentTypeID.ToString()+" ]");
				last=go;
				go.agentTypeID=0;
				go.BuildNavMesh();
			}
		}
		private void OnServerInitialized()
        {
			keepLooping=false;
		}/*
		private void OnItemUse(Item i, int n)
        {
			if(i.ToString().Contains("cactus")){
				foreach(HumanNPC hn in GameObject.FindObjectsOfType<HumanNPC>()){
					
					if(hn.Brain!=null){
						Puts(hn.Brain.GetType().ToString()+":"+hn.Brain.CurrentState.ToString());
						if(hn.Brain.CurrentState.ToString().Contains("Idle") || hn.Brain.CurrentState.ToString().Contains("BaseFollowPathState")){							
							Vector3 AB = i.parent.playerOwner.transform.position - hn.transform.position;
							AB = Vector3.Normalize(AB);
							Vector3 position = hn.transform.position+(new Vector3(AB.x,0,AB.z)*50);
							hn.Brain.Navigator.SetDestination(position);		
						}
					}
				}
			}
		}
		*/void AddSeat(BaseNpc ent, Vector3 locPos) {
				BaseEntity seat = GameManager.server.CreateEntity("assets/prefabs/deployable/chair/chair.deployed.prefab", ent.transform.position, new Quaternion()) as BaseEntity;
				if (seat == null) return;
				seat.Spawn();
				seat.SetParent(ent);
				seat.transform.localPosition = locPos;
				seat.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ| RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX| RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ ;
				seat.SendNetworkUpdateImmediate(true);
				//Puts(seat.transform.parent.GetChildren()[1].name=="assets/prefabs/misc/xmas/sled/sled.deployed.prefab");//
			}
		[Command("hz_get")] void surv_info(IPlayer player, string command, string[] args){				
			BasePlayer bp = (BasePlayer)player.Object;
			SendChatMsg(bp,"Current oceanlevel:" + ConVar.Env.oceanlevel.ToString());
			SendChatMsg(bp,"Current Position:" + bp.transform.position.ToString());
			SendChatMsg(bp,"Current Faction:"+bp.faction.ToString());
			if(bp.GetHeldEntity().name.Contains("planner")){
				
				SendChatMsg(bp,"Current held:"+(bp.GetHeldEntity()).GetItem().info.shortname);
			}
			//
		}
		[Command("hz_scan")] void surv_scan(IPlayer player, string command, string[] args){				
			BasePlayer bp = (BasePlayer)player.Object;	
				SendChatMsg(bp, "<color=#FF0000>[scanning]</color>");
			try{
				Transform t = (getLookingAtRaw(bp)).transform;
				SendChatMsg(bp,t.name.ToString());
				SendChatMsg(bp,LayerMask.LayerToName(t.gameObject.layer) + ":" + t.gameObject.layer.ToString());
				t.gameObject.layer=23;
				SendChatMsg(bp,"-----");
				SendChatMsg(bp,((getLookingAt(bp) is HumanNPC)? getLookingAtRaw(bp).gameObject.GetComponent<ScientistBrain>().CurrentState.ToString():""));
				SendChatMsg(bp,((getLookingAt(bp) is HumanNPC)? getLookingAtRaw(bp).gameObject.GetComponent<ScientistBrain>().Navigator.Path.Points.Count().ToString():""));
			}catch(Exception f){}
			bool first = true;
			try{
				BaseCombatEntity fc = (BaseCombatEntity)getLookingAt(bp);
				if(Vector3.Distance(fc.gameObject.transform.position,bp.transform.position) < 10){
					SendChatMsg(bp, "<color=#FF0000>["+fc.faction.ToString()+":"+fc.name+"]</color>");
					BaseAIBrain<BaseCombatEntity> brain = fc.GetComponent<BaseAIBrain<BaseCombatEntity>>();
					if(brain==null){fc.GetComponent<BaseAIBrain<HumanNPC>>();}
					if(brain==null) return;
					SendChatMsg(bp, "<color=#00FFFF>["+brain.CurrentState.ToString()+"] ");
					SendChatMsg(bp,brain.Navigator.Path.transform.parent.name.ToString());
				}
			}catch(Exception e){
					//SendChatMsg(bp, e.ToString()+"</color>");
			}
		}
		object OnItemUse(Item i, int n)
        {
			if(i.ToString().Contains("cactus")){/*
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo
					{
						connection = i.parent.playerOwner.net.connection
					}, null, "AddUI", "[\n\t{\n\t\t\"name\": \"HotzoneFactionLogo\",\n\t\t\"parent\": \"Overlay\",\n\n\t\t\"components\":\n\t\t[\n\t\t\t{\n\t\t\t\t\"type\":\"UnityEngine.UI.RawImage\",\n\t\t\t\t\"imagetype\": \"Tiled\",\n\t\t\t\t\"color\": \"1.0 1.0 1.0 1.0\",\n\t\t\t\t\"url\": \"https://i.imgur.com/BnE1wVd.jpeg\",\n\t\t\t},\n\n\t\t\t{\n\t\t\t\t\"type\":\"RectTransform\",\n\t\t\t\t\"anchormin\": \"0.975 0.95\",\n\t\t\t\t\"anchormax\": \"1 1\"\n\t\t\t},\n\n\t\t\t{\n\t\t\t\t\"type\":\"NeedsCursor\"\n\t\t\t}\n\t\t]\n\t}\n]\n");
				
			}
			if(i.ToString().Contains("pumpkin")){
				CommunityEntity.ServerInstance.ClientRPCEx<string>(new SendInfo
				{
					connection = i.parent.playerOwner.net.connection
			}, null, "DestroyUI", "HotzoneFactionLogo");*/
			}
			return null;
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
