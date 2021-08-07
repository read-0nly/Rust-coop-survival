//[System.Windows.Forms.Form].GetMembers() | select-object name,membertype

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
			if((""+entity.name).Contains("bandit")){
					Puts(npc.name+"_:_" + entity.name);
					HumanNPC hn = entity/GetComponent<HumanNPC>();
					if(hn==null){return null;}
					if(gn.locomotion.attackEntity.name == npc.name){return null;}
					gn.StartAttackingEntity(npc);
					//BaseNpc hn = entity as BaseNpc;
					//BasePlayer bp = (BasePlayer)npc;
					//NPCPlayer np = (NPCPlayer)bp;
					//ScientistNPC hn = np as ScientistNPC;
					/*foreach(Component c in entity.GetComponents(typeof(Component))){
						Puts(c.ToString());
					}*/
					
				    //Puts(((entity as BasePlayer)==null?"IsNull":"All"));
				    //Puts(((entity as HumanNPCNew).Brain==null?"IsNull":"All"));
				   // Puts(((entity as HumanNPCNew).Brain.Senses==null?"IsNull":"All"));
				   // Puts(((entity as HumanNPCNew).Brain.Senses.Memory==null?"IsNull":"All"));
				    //Puts(((entity as HumanNPCNew).Brain.Senses.Memory.Threats==null?"IsNull":"All"));
				    //Puts((entity as HumanNPCNew).Brain.HostileTargetsOnly.ToString());
				    //(npc as HumanNPCNew).Brain.GetEntity().currentTarget = npc;
					//(entity as HumanNPCNew).Brain.SenseTypes = (EntityType)67;
					//(entity as HumanNPCNew).currentTarget = npc;
					//(entity as HumanNPCNew).Brain.Senses.Init();
					
				    //Puts((entity as Scientist).ToString());//
					//(entity as HumanNPCNew).Brain.Senses.Memory.Threats.Add(npc);
				    //Puts(((npc as BasePlayer)==null?"IsNull":"All"));
				    //Puts(((entity.GetComponent<ScientistBrain>() as ScientistBrain)==null?"IsNull":"All"));
					//Puts(hn.AttackTarget.name+"=");
					//((BaseNpc)hn).AiContext.EnemyNpc = null;
					
				    //Puts(entity.gameObject.GetComponent<BaseCombatEntity>().gameObject.name);
					
					//Puts(hn.AttackTarget.name);
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