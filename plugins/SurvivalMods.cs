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
using Oxide.Plugins;

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
	    //		Armed = false
		private bool envUpdateArmed=false;
		private float newOcean = 0.0f;
			
		bool CanPickupEntity(BasePlayer player, BaseEntity entity)
		{
			return false;
		}
			
		private void OnServerInitialized()
        {
			
			/*
			timer 5{
				if getTime > 23
					show "Kick incoming at 0: 24-gettime"
					if(not Armed){Armed=true;}
				if getTime < 1 && Armed{
					refreshAll;
					Armed=false;
				}
			}
			*/
            timer.Every(10f, () => {
				Puts(ConVar.Env.time.ToString());
				if(ConVar.Env.time > 23 && envUpdateArmed){
					foreach (global::BasePlayer basePlayer in global::BasePlayer.activePlayerList.ToArray())
					{
						if(basePlayer == null){return;}
						if(basePlayer.IsConnected == false){return;}
						WaterSystem.OceanLevel=0f;
						basePlayer.Kick("Night Cycle");
					}
					envUpdateArmed=false;
				}
			});
			//	origSplat = getSplatMap();
			//	data.add("OriginalSplat", origSplat);
			//	data.add("CurrentSplat", origSplat);
			//	data.add("NextSplat", origSplat);
			//	origTopo = getTopo();
			//	data.add("OriginalTopo",origTopo);
			//	data.add("CurrentTopo",origTopo);
			//	data.add("NextTopo",origTopo);
		}
			
		void OnItemUse(Item item, int amountToUse)
        {
			string ItemToEat = item.info.shortname.ToString();
			if(ItemToEat.ToLower().Contains("cactus")){
				envUpdateArmed = true;
			}
		}
		/*

		onTreeCut{	
				data["NextTopo"][x,y] = data["OriginalTopo"][x,y]
				data["NextTopo"][TerrainTopology.FOREST].SetTopo(x,y,0);
				data["NextTopo"][TerrainTopology.SAND].SetTopo(x,y,1);
				data["NextSplat"].setsplat(x,y, Splat.Sand);
		}
		*/
		/*
		onPlant{
				data["NextTopo"][x,y] = data["OriginalTopo"][x,y]
				data["NextTopo"][TerrainTopology.FOREST].SetTopo(x,y,1);
				data["NextTopo"][TerrainTopology.SAND].SetTopo(x,y,0);
				data["NextSplat"].setsplat(x,y, Splat.FOREST);
		}
		*/
		/*
		riseOcean(float level){
			OceanLevel+=level
			for(int x =0 < NextSplat.Width ++){
				for(y = 0 < NextSplat.Height ++){
					if(HeightMap.getHeight(x,y) < level-1){
						data["NextTopo"].setTopo(x,y,Topo.OCEAN,1);
						
					}
					else if(HeightMap.getHeight(x,y) <= level+1){
						data["NextTopo"].setTopo(x,y,Topo.BEACH,1);
					}
					else if(HeightMap.getHeight(x,y) > level+1){
						data["NextTopo"].setTopo(x,y,Topo.BEACH,0);
					}
				}
			}
		}
		*/
		/*
		refreshAll(){
			data[currenttopo]=data[newtopo];
			data[currentsplat]=data[newsplat];
			riseOcean(float level)
			foreach basePlayer{basePlayer.kick};
		}
		*/
			













		object OnNpcTarget(BaseEntity npc, BaseEntity entity)
		{
			if((""+entity.name).Contains("NPC")){
					Puts(npc.name+"_:_" + entity.name);
					HumanNPC.HumanLocomotion hn = entity.GetComponent<HumanLocomotion>();
					if(hn==null){return null;}
					if(hn.locomotion.attackEntity.name == npc.name){return null;}
					hn.StartAttackingEntity(npc);
					foreach(Component c in entity.GetComponents(typeof(Component))){
						Puts(c.ToString());
					}
					return true;
				
			}
			return null;
		}*/
		
		
		
		
		
		
		
					
					//BaseNpc hn = entity as BaseNpc;
					//BasePlayer bp = (BasePlayer)npc;
					//NPCPlayer np = (NPCPlayer)bp;
					//ScientistNPC hn = np as ScientistNPC;
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
		object OnNpcPlayerTarget(NPCPlayerApex npcPlayer, BaseEntity entity)
		{
			Puts("OnNpcPlayerTarget works!");
			return null;
		}
		//this.currentTarget = (BaseCombatEntity) component;
		///this.currentTargetLOS = this.IsPlayerVisibleToUs(component);
		
    }
}