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
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using UnityEngine.AI;

namespace Oxide.Plugins
{
	[Info("Analyzer", "obsol", "0.0.1")]
	[Description("Server debugging tool for modders")]
/*======================================================================================================================= 
* 
//One Entity Enter - if nobuild zone, leave

/*
void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
{ 
    Puts("OnEntityEnter works!");
}
*=======================================================================================================================*/


	public class Analyzer : CovalencePlugin
	{
		public Transform target;
		private void OnServerInitialized()
        {
			//
            timer.Every(10f, () => {
				Puts("-----------------------------------------");
				List<Scientist> list = new List<Scientist>(Resources.FindObjectsOfTypeAll<Scientist>());
				if(list!=null && target !=null){
					foreach(Scientist s in list){
						string logStr=s.transform.name+":"+s.TimeLastMoved.ToString()+":";
						logStr+=(s.LookAtPoint==null?"[::]":"["+s.LookAtPoint.transform.position.x.ToString()+":"+s.LookAtPoint.transform.position.y.ToString()+":"+s.LookAtPoint.transform.position.z.ToString()+"]");
						logStr+="["+s.transform.position.x.ToString()+":"+s.transform.position.y.ToString()+":"+s.transform.position.z.ToString()+"]";
						Puts(logStr);
						if(s.transform.name=="assets/prefabs/npc/scientist/scientist.prefab"){
							Component[] components = s.gameObject.GetComponents(typeof(Component));
							foreach(Component component in components) {
								//Debug.Log(component.ToString());
							}
							NavMeshAgent na = s.gameObject.GetComponent<NavMeshAgent>();
							Puts(target.ToString());
							//s.UpdateDestination(s.spawnPos); <<This gets them to return to their spawn
							s.UpdateDestination(target); 
							s.SetTargetPathStatus();
							Puts(na.isOnNavMesh.ToString());
							if(target!=null){
								//s.gameObject.GetComponent<HumanNPCNew>().SetAimDirection(target.position);
							}
						}
					}
				}
				else{
					List<MonumentMarker> list2 = new List<MonumentMarker>(Resources.FindObjectsOfTypeAll<MonumentMarker>());
					if(list2!=null){
						foreach(GameObject s in list2){//
							if(s.text.text.ToLower().Contains("Hotzone")){
								target = s.transform;
							}
						}
					}						
				}//
				Puts("-----------------------------------------");
			});
		}
	}
}
