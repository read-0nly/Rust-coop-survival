//[System.Windows.Forms.Form].GetMembers() | select-object name,membertype

using Convert = System.Convert;
using Network;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using UnityEngine; 
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
	[Info("SurvivalMods", "obsol", "0.0.1")]
	[Description("Various mods and tweaks to bring out the survival aspects")]

/*======================================================================================================================= 
* 
Block picking up deployables except:
	camp fire
	small chest
	sleeping bag
	lamps
	chair
	stashes
	water purifiers
	traps except barricade traps
	ladder

	
*=======================================================================================================================*/


	public class SurvivalMods : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) =>
            _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Analyze]</color>", 0, Array.Empty<object>());
			
		bool CanPickupEntity(BasePlayer player, BaseEntity entity)
		{
			return false;
		}
		object OnNpcTarget(BaseEntity npc, BaseEntity entity)
		{
			if((""+entity.name).Contains("scientist")){
				Puts(npc.name+"_:_" + entity.name);
					//HumanPlayer hn = npc.gameObject.GetComponent<HumanPlayer>();
					Component[] components = npc.gameObject.GetComponents(typeof(Component));
					foreach(Component component in components) {
						Puts(component.ToString());
					}
					//BasePlayer bp = (BasePlayer)npc;
					//NPCPlayer np = (NPCPlayer)bp;
					//ScientistNPC hn = np as ScientistNPC;
					
				    //Puts((hn==null?"IsNull":"All"));
				    //Puts(entity.gameObject.GetComponent<BaseCombatEntity>().gameObject.name);
					//np.currentTarget = null;
					return true;
				
			}
			return null;
		}
		object OnNpcPlayerTarget(NPCPlayerApex npcPlayer, BaseEntity entity)
		{
			Puts("OnNpcPlayerTarget works!");
			return null;
		}
		//this.currentTarget = (BaseCombatEntity) component;
		///this.currentTargetLOS = this.IsPlayerVisibleToUs(component);
		
    }
}