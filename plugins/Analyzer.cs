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
			
			List<GameObject> list = new List<GameObject>(Resources.FindObjectsOfTypeAll<GameObject>());
			if(list!=null){
				foreach(GameObject s in GameObject){
					if(s.transform.name.ToLower().Contains("roadcone")){
						target = s.transform;
					}
				}
			}
            timer.Every(10f, () => {
				List<Scientist> list = new List<Scientist>(Resources.FindObjectsOfTypeAll<Scientist>());
				if(list!=null){
					foreach(Scientist s in list){
						Puts(s.transform.name+":"+s.TimeLastMoved.ToString()+":"+(s.LookAtPoint==null).ToString());
						if(s.transform.name=="assets/prefabs/npc/scientist/scientist.prefab"){
							
						}
					}
				}//
			});
		}
	}
}
