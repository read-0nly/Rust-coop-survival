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
	public class Analyzer : CovalencePlugin
	{
		private void OnServerInitialized()
        {
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
		*/
	}
}
