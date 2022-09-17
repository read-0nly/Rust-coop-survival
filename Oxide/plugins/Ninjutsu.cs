
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
	[Info("Ninjutsu", "obsol", "0.0.1")]
	[Description("Server debugging tool for modders")]
	public class Ninjutsu : CovalencePlugin
	{
		
		public Vector3 getLookingAtVect(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit,1000)){
				return hit.point;
			}
			return new Vector3(0,0,0);
		}
		
		public BasePlayer getLookingAtPlayer(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BasePlayer>()!=null) return entity.GetComponent<BasePlayer>();}
			}
			return null;
		}
		bool? CanUseGesture(BasePlayer player, GestureConfig gesture)
		{
			
			switch(gesture.gestureCommand)
			{
				case "clap":
					Puts(gesture.gestureCommand);
					if(ConVar.Env.time<21){
					}else{					
					}
					break;					
				case "friendly":
					server.Command("weather.load","Storm");					
					break;
				case "hurry":
					server.Command("weather.load","Fog");					
					break;
				case "ok":
					server.Command("weather.load","Clear");					
					break;
				case "point":
					
					server.Command("spawn","grenade.molotov.deployed",(getLookingAtVect(player)+new Vector3(0,200,0)).ToString());	
					 
					break;
				case "shrug":
					
					break;
				case "thumbsdown":
					server.Command("env.time","22");
				
					break;
				case "thumbsup":
					server.Command("env.time","8.5");	
					
					break;
				case "victory":
					BasePlayer bp = getLookingAtPlayer(player);	
					Vector3 oldPos = player.transform.position;
					player.Teleport(bp.transform.position);
					bp.Teleport(oldPos);
					if(bp is HumanNPC){
						(bp as HumanNPC).Brain.Navigator.Warp(oldPos);
					}
					break;
				case "wave":
					
					break;
				case "hiphopdance":
					
					break;
				case "sillydance":
					
					break;
				case "sillydance2":
					
					break;
				default:
					// code block
					break;
			}
			
			return null;
		}
	}
}