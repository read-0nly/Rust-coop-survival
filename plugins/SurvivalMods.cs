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
using UnityEngine.SceneManagement;

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
            _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Survival]</color>", 0, Array.Empty<object>());
	    //		Armed = false
		private bool envUpdateArmed=false;
		int defaultHealth = 50;
		int maxHealth = 150;
		int defaultCals = 75;
		int maxCals = 400;
		int defaultWater = 100;
		int maxWater = 500;
		float waterIncrease = 0f;
		bool debugOnBoot=false;
        [PluginReference]
        private Plugin ImageLibrary;
			
		bool CanPickupEntity(BasePlayer player, BaseEntity entity)
		{
			return false;
		}
			
		private void OnServerInitialized()
        {
            timer.Every(10f, () => {
				if(ConVar.Env.time > 23 && envUpdateArmed){
					WaterSystem.OceanLevel+=waterIncrease;
					waterIncrease=0;
					
					BaseBoat[] components = GameObject.FindObjectsOfType<BaseBoat>();
					foreach (BaseBoat boat in components)
					{
						boat.WakeUp();
						if(boat.gameObject.GetComponent<MotorRowboat>()==null){break;}
						MotorRowboat mrb = boat.gameObject.GetComponent<MotorRowboat>();//
						if (mrb.IsFlipped())
						{
						  //mrb.rigidBody.AddRelativeTorque(Vector3.forward * 200.25f, ForceMode.VelocityChange);
						  mrb.transform.Rotate(180f,0,0,Space.Self);
						}
					}
					
					foreach (global::BasePlayer basePlayer in global::BasePlayer.activePlayerList.ToArray())
					{
						if(basePlayer == null){return;}
						if(basePlayer.IsConnected == false){return;}
						basePlayer.Kick("Night Cycle - Reconnect to wake up!");
					}
					envUpdateArmed=false;
					ConVar.Env.time = 5;
					envUpdateArmed=true;
				}
	
				foreach (global::BasePlayer basePlayer in global::BasePlayer.activePlayerList.ToArray())
				{					
					if(basePlayer.metabolism.calories.value < 10 && basePlayer.metabolism.calories.max > 20){
						basePlayer.metabolism.calories.max+= -1.5f;									
					}else if (basePlayer.metabolism.calories.value<20 && basePlayer.metabolism.calories.max > 30){
						basePlayer.metabolism.calories.max+= -0.5f;
					}else if (basePlayer.metabolism.calories.max-basePlayer.metabolism.calories.value<10){
						basePlayer.metabolism.calories.max+= (basePlayer.metabolism.calories.max < maxCals?0.5f:0);				
					}				
					basePlayer.metabolism.hydration.max+= (basePlayer.metabolism.hydration.max < maxWater?0.1f:0);
					basePlayer._maxHealth+= (basePlayer._maxHealth < maxHealth?((0.005f*(maxHealth-basePlayer._maxHealth))*(0.005f*(maxHealth-basePlayer._maxHealth))):0);
					
				}
				//Puts(basePlayer._maxHealth.ToString());
				List<GrowableEntity> list = new List<GrowableEntity>(Resources.FindObjectsOfTypeAll<GrowableEntity>());
				foreach (GrowableEntity growableEntity in list)
				{
				  //if (growableEntity.isServer)////
					//growableEntity.ChangeState(growableEntity.currentStage.nextState, false);
					
						//Puts(growableEntity.transform.name);
					if(growableEntity.transform.name == "assets/prefabs/plants/hemp/hemp.entity.prefab"){
						
						if(growableEntity.State.ToString() == "Dying"){
							 string tree = "assets/bundled/prefabs/autospawn/resource/v3_temp_field/birch_tiny_temp.prefab";
							 PlantTree(growableEntity,tree);
						}
					}
				}
				
				if(debugOnBoot){
					 /*
			 GameObject[] rootObjects;
			 Scene scene = SceneManager.GetActiveScene();
			 rootObjects=Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
			 
			 Puts("Try Destroy");
			 // iterate root objects and do something
			 for (int i = 0; i < rootObjects.Count(); ++i)
			 {
					 if(rootObjects[ i ].name.Contains("prevent_building")){
						 MonoBehaviour.Destroy(rootObjects[ i ]);
						 Puts("Destroyed");
					 }
			 }	
*/	
				 GameObject[] rootObjects;
				 Scene scene = SceneManager.GetActiveScene();
				 rootObjects=Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
				 for (int i = 0; i < rootObjects.Count(); ++i)
				 {
						 if(rootObjects[ i ].name.Contains("prevent_building")){
							 Puts("[Target:"+rootObjects[ i ].name+"]|[I:"+i+"]");
						 }
				 }	
				}
			
			});
			
			for(int x = 0; x<1024; x++){
				for(int y = 0; y<1024; y++){
					try{
							TerrainMeta.TopologyMap.RemoveTopology(x,y,32); //32
							TerrainMeta.TopologyMap.AddTopology(x,y,2); //262144
							TerrainMeta.TopologyMap.AddTopology(x,y,262144); //262144
							
						
					}
					catch{};
				}
			}
			
		}
		void PlantTree(GrowableEntity plant, string prefabName)
        {
            BaseEntity entity = GameManager.server.CreateEntity(prefabName, plant.transform.position, Quaternion.identity);
            if (entity == null)
            {
                return;
            }

            entity.Spawn();

            plant?.Kill();

        }
		private void OnPlayerRespawned(BasePlayer player)
        {			
          setDefaults(player);
        }
		
		void setDefaults(BasePlayer player){
			player._maxHealth = defaultHealth;						
			player.metabolism.hydration.max = defaultWater;
			player.metabolism.calories.max = defaultCals;
			
		}
		void OnItemUse(Item item, int amountToUse)
        {
			string ItemToEat = item.info.shortname.ToString();
            if (ItemToEat == null){return;}
            ItemContainer Container = item.GetRootContainer();
            if (Container == null){return;}/*
			if(ItemToEat.ToLower().Contains("cactus")){
				envUpdateArmed = true;
				waterIncrease=-10;
				BasePlayer Eater = Container.GetOwnerPlayer();
				if (Eater == null){return;}
			}
			*/
		}
		
/*
				
				//ConVar.Env.time = 23;
				/*
				List<string> names = new List<string>();
				foreach(GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()){
					/*if(go.name.ToLower().Contains("foundation")){
						//Puts(go.name);
					go.transform.Translate(0f,100f,0f,Space.World);
					go.transform.Translate(0f,100f,0f,Space.Self);
						
					}*//*/
					
				}
				
				HashSet<string> unique_items = new HashSet<string>(names);
				
				foreach (string s in unique_items){
					Puts(s);
				}*/
				/*
				BuildingBlock[] components = GameObject.FindObjectsOfType<BuildingBlock>();
			
				foreach (BuildingBlock block in components)
				{
					block.transform.Translate(0f,100f,0f,Space.World);
					block.transform.Translate(0f,100f,0f,Space.Self);
				}*/
			
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
/*				//TerrainMeta.SplatMap
				//TerrainMeta.TopologyMap
				//this.src
				/*
				final = "";
				for(int x = 0; x<1024; x++){
					string row = "";
					for(int y = 0; y<1024; y++){
						try{
							//float z1 = TerrainMeta.SplatMap.GetSplatMaxIndex(x,y,-1);
							//TerrainMeta.TopologyMap.AddTopology(x,y,2); 
							//TerrainMeta.TopologyMap.RemoveTopology(x,y,32); //32
							bool z1 = TerrainMeta.TopologyMap.GetTopology(x,y,2);  //2048 = road//
							int z2 =  Convert.ToInt32(z1);//.ToInt();
							//TerrainMeta.SplatMap.AddSplat(x,y,2,255,255,0);
							//if(z1<5){row+=" ";break;}
							row+=z2.ToString();
							
							//Puts(x.ToString()+":"+y.ToString()+"["+z.ToString()+"]");//
						}
						catch{};
					}
					final+=row+"\n";
				}
				//TerrainMeta.SplatMap.Setup();
				//TerrainMeta.SplatMap.ApplyTextures();
							
			    Puts(final);
				*/
/*				//TerrainMeta.SplatMap
				//TerrainMeta.TopologyMap
			
			//float z1 = TerrainMeta.SplatMap.GetSplatMaxIndex(x,y,-1);
						
						//if((x<50||x>(512+50))||(y<50||y>(512+50))){
						//}
						//else{
						//	TerrainMeta.TopologyMap.RemoveTopology(x,y,2); //32
						//	TerrainMeta.TopologyMap.AddTopology(x,y,32); //32
						//}
						
						//TerrainMeta.TopologyMap.RemoveTopology(x,y,32); //32
						//bool z1 = TerrainMeta.TopologyMap.GetTopology(x,y,2);  //2048 = road//
						//int z2 =  Convert.ToInt32(z1);//.ToInt();
						//TerrainMeta.SplatMap.AddSplat(x,y,2,255,255,0);
						//if(z1<5){row+=" ";break;}
						//row+=z2.ToString();
						
						//Puts(x.ToString()+":"+y.ToString()+"["+z.ToString()+"]");//
			//	origSplat = getSplatMap();
			//	data.add("OriginalSplat", origSplat);
			//	data.add("CurrentSplat", origSplat);
			//	data.add("NextSplat", origSplat);
			//	origTopo = getTopo();
			//	data.add("OriginalTopo",origTopo);
			//	data.add("CurrentTopo",origTopo);
			//	data.add("NextTopo",origTopo);
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
			
/*

		object OnNpcTarget(BaseEntity npc, BaseEntity entity)
		{
			if((""+entity.name).Contains("NPC")){
					Puts(npc.name+"_:_" + entity.name);
					HumanNPC.HumanLocomotion hn = entity.GetComponent<HumanLocomotion>();
					//if(hn==null){return null;}
					//if(hn.locomotion.attackEntity.name == npc.name){return null;}
					//hn.StartAttackingEntity(npc);
					foreach(Component c in entity.GetComponents(typeof(Component))){
						Puts(c.ToString());
					}
					return true;
				
			}
			return null;
		}
		*/
/*		//
		
		
		
		
		
					
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
/* 		object OnNpcPlayerTarget(NPCPlayerApex npcPlayer, BaseEntity entity)
		{
			Puts("OnNpcPlayerTarget works!");
			return null;
		} */
/*		//this.currentTarget = (BaseCombatEntity) component;
		///this.currentTargetLOS = this.IsPlayerVisibleToUs(component);*/
		
		
    }
}