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
	[Info("Navmesher", "obsol", "0.0.1")]
	[Description("Re-bakes everything for Agent 0 and spawns everything Agent 0")]
	public class Navmesher : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,  "<color=#00ff00>[Analyzer]</color>", 0, Array.Empty<object>());
			
			
		
		void scan1(){
			
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(MonumentNavMesh));
			foreach(MonumentNavMesh go in GameobjectList){
				Puts("["+(go.transform.parent==null?go.transform.ToString():go.transform.ToString())+" : "+go.NavMeshAgentTypeIndex.ToString()+" : "+ NavMesh.GetSettingsByIndex(go.NavMeshAgentTypeIndex).agentTypeID.ToString()+"]");
				go.NavMeshAgentTypeIndex=0;
				go.gameObject.SetActive(false);
				go.gameObject.SetActive(true);
			}
			
		}
		void scan(GameObject go){
			MonumentNavMesh mnm = go.GetComponentInChildren<MonumentNavMesh>();
			DungeonNavmesh dun = go.GetComponentInChildren<DungeonNavmesh>();
			DynamicNavMesh dyn = go.GetComponentInChildren<DynamicNavMesh>();
			NavMeshSurface nms = go.GetComponentInChildren<NavMeshSurface>();
			if(mnm!=null){
				mnm.NavMeshAgentTypeIndex=0;
				Puts("MonumentNavmesh updated");
			}
			if(dun!=null){
				dun.NavMeshAgentTypeIndex=0;
				Puts("DungeonNavmesh updated");
			}
			if(dyn!=null){
				dyn.NavMeshAgentTypeIndex=0;
				Puts("DynamicNavMesh updated");
			}
			if(nms!=null){
				nms.agentTypeID=0;
				nms.BuildNavMesh();
				Puts("NavMeshSurface updated");
			}
			go.SetActive(false);
			go.SetActive(true);
		}
		
		void scan2(){
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(DungeonNavmesh));
			foreach(DungeonNavmesh go in GameobjectList){
				Puts("["+(go.transform.parent==null?go.transform.ToString():go.transform.ToString())+" : "+go.NavMeshAgentTypeIndex.ToString()+" : "+ NavMesh.GetSettingsByIndex(go.NavMeshAgentTypeIndex).agentTypeID.ToString()+"]");
				go.NavMeshAgentTypeIndex=0;
				go.gameObject.SetActive(false);
				go.gameObject.SetActive(true);
			}
			
		}
		void scan3(){
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(DynamicNavMesh));
			foreach(DynamicNavMesh go in GameobjectList){
				Puts("["+(go.transform.parent==null?go.transform.ToString():go.transform.ToString())+" : "+go.NavMeshAgentTypeIndex.ToString()+" : "+ NavMesh.GetSettingsByIndex(go.NavMeshAgentTypeIndex).agentTypeID.ToString()+"]");
				go.NavMeshAgentTypeIndex=0;
				go.gameObject.SetActive(false);
				go.gameObject.SetActive(true);
			}
			
		}
		void scan4(){
			
			UnityEngine.Object[] GameobjectList= Resources.FindObjectsOfTypeAll(typeof(NavMeshSurface));
			NavMeshSurface last;
			(GameobjectList[0] as NavMeshSurface).size+=(GameobjectList[0] as NavMeshSurface).size;
			foreach(NavMeshSurface go in GameobjectList){
				Puts("["+(go.transform.parent==null?go.transform.ToString():go.transform.parent.ToString())+" : "+go.agentTypeID.ToString()+" ]");
				last=go;
				go.agentTypeID=0;
				go.BuildNavMesh();
			}
			
		}
		void scanAll(){
			scan1();
			scan2();
			scan3();
			scan4();
		}
		void OnEntitySpawned(BaseNpc bn){
			if(bn.GetNavAgent!=null)
			bn.GetNavAgent.agentTypeID=0;
		}
		void OnTerrainInitialized()
		{
			scanAll();
		}
		void OnWorldPrefabSpawned(GameObject gameObject, string category)
		{
			scan(gameObject);
		}
		void OnEntitySpawned(CargoShip bn){
			scan(bn.gameObject);
		}
		void OnEntitySpawned(HumanNPC bn){
			if(!bn.transform.name.ToLower().Contains("cargo") ){
				int oldID = bn.GetComponent<BaseNavigator>().Agent.agentTypeID;
				bn.GetComponent<BaseNavigator>().defaultAreaMask = UnityEngine.AI.NavMesh.AllAreas;
				bn.GetComponent<BaseNavigator>().navMeshQueryFilter.areaMask= UnityEngine.AI.NavMesh.AllAreas;
				bn.GetComponent<BaseNavigator>().navMeshQueryFilter.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
				bn.GetComponent<BaseNavigator>().Agent.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
				
				Vector3 position = 	bn.transform.position;		
			
				if(!bn.GetComponent<BaseNavigator>().Warp(position)){						
					bn.GetComponent<BaseNavigator>().Agent.agentTypeID=oldID;
				}
				if(!bn.GetComponent<BaseNavigator>().Warp(position)){	
					Puts(((char)27)+"[94m"+"Killing "+bn.transform.name);//
					bn.Kill();
				}
			}
		}
		private void OnServerInitialized()
        {
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
