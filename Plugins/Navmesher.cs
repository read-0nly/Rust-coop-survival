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
			
		void Loaded(){
			//ConVar.AI.npc_enable=false;
		}
		void OnTerrainInitialized(){
			
			foreach (MonumentInfo monumentInfo in TerrainMeta.Path.Monuments)
			{
				monumentInfo.HasNavmesh=false;
            }
			scanHurtTrig();
            NextFrame(scan1);
			//NextFrame(scan2);
			NextFrame(scan3);
			//NextFrame(scan4);
		}
		void scan1(){
			
			Puts("-----------------SCAN 1-----------------");
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(MonumentNavMesh));
			MonumentNavMesh.use_baked_terrain_mesh=true;
			foreach(MonumentNavMesh go in GameobjectList){
				if (!go.transform.ToString().ToLower().Contains("oilrig") && !go.transform.ToString().ToLower().Contains("underwater"))
				{
					go.NavMeshAgentTypeIndex = 0;
					go.agentTypeId = 0;
					UnityEngine.Object.Destroy(go);
				}
				else
                {
                    go.NavMeshAgentTypeIndex = 0;
                    go.agentTypeId = 0;
                    go.gameObject.SetActive(false);
                    go.gameObject.SetActive(true);
					go.UpdateNavMeshAndWait();

                }
			}
			
		}
		void scanHurtTrig(){
			
			Puts("-----------------SCAN Hurt-----------------");
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(TriggerHurtEx));
			foreach(TriggerHurtEx go in GameobjectList)
            {
				if (go.transform == null) continue;
				Transform parent = go.transform.parent;
                if (parent == null || !(
					parent.transform.ToString().Contains("barbed") ||
					parent.transform.ToString().Contains("barbed")
					)) continue;
                Collider col = parent.GetComponentInChildren<Collider>();

                Bounds b = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 5, 10));
                if (col != null) b = col.bounds;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Adding Barbed NMO for " + parent.transform.name.Split("/\\".ToCharArray()).Last());
                Console.ResetColor();
                float min = (b.extents.x > b.extents.z ? b.extents.x : b.extents.z);
				try
				{
					NavMeshObstacle nmo = parent.gameObject.GetComponent<NavMeshObstacle>();
					nmo=(nmo==null?parent.gameObject.AddComponent<NavMeshObstacle>():nmo);
					nmo.shape = NavMeshObstacleShape.Box;
					nmo.center = new Vector3(0, 0, 0);
					nmo.size = (parent.transform.name.ToString().Contains("corridor_train_tunnel_entrance") ? new Vector3(15, 10, 15) : new Vector3(10, 5, 10));
					nmo.carving = true;
					nmo.enabled = true;
                    Console.Write(" - ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Success");
                    Console.ResetColor();
                }
				catch(Exception ex)
                {
                    Console.Write(" - ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failure");
                    Console.ResetColor();
                }

            }
			
		}
		void scan(GameObject go){
			MonumentNavMesh mnm = go.GetComponentInChildren<MonumentNavMesh>();
			DungeonNavmesh dun = go.GetComponentInChildren<DungeonNavmesh>();
			DynamicNavMesh dyn = go.GetComponentInChildren<DynamicNavMesh>();
			NavMeshSurface nms = go.GetComponentInChildren<NavMeshSurface>();
			if(mnm!=null){
				mnm.NavMeshAgentTypeIndex=0;
				mnm.gameObject.SetActive(false);
				mnm.gameObject.SetActive(true);
				mnm.HasBuildOperationStarted=false;
				mnm.UpdateNavMeshAndWait();
			}
			if(dun!=null){
				dun.NavMeshAgentTypeIndex=0;
				dun.gameObject.SetActive(false);
				dun.gameObject.SetActive(true);
				dun.HasBuildOperationStarted=false;
				dun.UpdateNavMeshAndWait();
			}
			if(dyn!=null){
				dyn.NavMeshAgentTypeIndex=0;
				dyn.gameObject.SetActive(false);
				dyn.gameObject.SetActive(true);
				dyn.HasBuildOperationStarted=false;
				dyn.UpdateNavMeshAndWait();
			}
			if(nms!=null){
				nms.agentTypeID=0;
				nms.BuildNavMesh();
			}
		}
		
		void scan2(){
			Puts("-----------------SCAN 2-----------------");
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(DungeonNavmesh));
			foreach(DungeonNavmesh go in GameobjectList){
				go.NavMeshAgentTypeIndex=0;
				go.agentTypeId=0;
				go.gameObject.SetActive(false);
				go.gameObject.SetActive(true);
				go.HasBuildOperationStarted=false;
				go.UpdateNavMeshAndWait();
			}
			
		}
		void scan3(){
			Puts("-----------------SCAN 3-----------------");
			UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(DynamicNavMesh));
			foreach(DynamicNavMesh go in GameobjectList){
				go.NavMeshAgentTypeIndex=0;
				go.agentTypeId=0;
				go.gameObject.SetActive(false);
                go.HasBuildOperationStarted = false;
				//go.Bounds.extents = new Vector3(10000, 10000, 10000);
                Puts(go.Bounds.ToString() + " : " + go.NavMeshCollectGeometry.ToString());
                go.gameObject.SetActive(true);
				//go.NavMeshCollectGeometry = NavMeshCollectGeometry.RenderMeshes;
                go.UpdateNavMeshAndWait();
			}
			
		}
		void scan4(){
			Puts("-----------------SCAN 4-----------------");
			
			UnityEngine.Object[] GameobjectList= Resources.FindObjectsOfTypeAll(typeof(NavMeshSurface));
			NavMeshSurface last;
			(GameobjectList[0] as NavMeshSurface).size+=(GameobjectList[0] as NavMeshSurface).size;
			foreach(NavMeshSurface go in GameobjectList){
				last=go;
				go.agentTypeID=0;
				Puts(go.size.ToString() + " - " + go.center.ToString() + " : " + go.useGeometry.ToString());
				go.BuildNavMesh();
			}
			
		}
		void scanAll(){
			NextFrame(scan4);
		}
		void OnEntitySpawned(BaseNpc bn){
			int oldID=0;
			if(bn.GetNavAgent!=null)
				bn.GetNavAgent.agentTypeID=0;
			if(bn.GetComponent<BaseNavigator>() ==null || bn.transform.name.ToLower().Contains("cargo")) return;
			oldID = bn.GetComponent<BaseNavigator>().navMeshQueryFilter.agentTypeID;
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
		object OnSaveLoad(Dictionary<BaseEntity, ProtoBuf.Entity> entities)
		{
			//scanAll();
			return null;
		}
		void OnEntitySpawned(CargoShip bn){
			if(bn==null && bn.gameObject==null) return;
			scan(bn.gameObject);
		}
		void OnEntitySpawned(JunkPile bn){
			if(bn==null && bn.gameObject==null) return;
			scan(bn.gameObject);
		}
		void OnEntitySpawned(NPCPlayer bn){
				BaseNavigator bnav = bn.GetComponent<BaseNavigator>();
				if(bnav==null)return;
				int oldID = bnav.Agent.agentTypeID;
				bnav.defaultAreaMask = 25;
				bnav.navMeshQueryFilter.areaMask= 25;
				bnav.navMeshQueryFilter.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
				bnav.Agent.agentTypeID= NavMesh.GetSettingsByIndex(0).agentTypeID;
				
				Vector3 position = 	bn.transform.position;		
			
				if(!bnav.Warp(position)){						
					bnav.Agent.agentTypeID=oldID;
				}
				if(!bnav.Warp(position)){	
					Puts(((char)27)+"[94m"+"Killing "+bn.transform.name);//
					bn.Kill();
				}
				
		}
		private void OnServerInitialized()
        {
			ConVar.AI.npc_enable=true;
            //NextFrame(flushAll);
        }
		void flushAll(){
			UnityEngine.Object[] humans= Resources.FindObjectsOfTypeAll(typeof(NPCPlayer));
			UnityEngine.Object[] animals= Resources.FindObjectsOfTypeAll(typeof(BaseNpc));
			foreach(NPCPlayer hn in humans){
				try{hn.Kill();}catch(Exception e){}
				
			}
			foreach(BaseNpc bn in animals){
				try{bn.Kill();}catch(Exception e){}
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