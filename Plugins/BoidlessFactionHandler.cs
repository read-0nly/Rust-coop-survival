/*

  "FactionScores": {
    "10645866045":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Default":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Player-Aligned":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Player":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Scientist":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Dwellers":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Bandits":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Carnivores":{
		"Default":0,
		"Player-Aligned":0,
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Carnivores":0,
		"Herbivores":0
	},
    "Herbivores":{
		"Player":0,
		"Scientist":0,
		"Dwellers":0,
		"Bandits":0,
		"Animals":0,
		"Herbivores":0
	}
  },
  
When damage
	personal score - damage (if player)
	enemy score + damage/10 (if player)
	ally score - damage/10 (if player)
	interfaction score - damage/100
	if enemy on player
		enemy faction + damage/1000
On sale
	if NPC shop, scrap to faction bank
		scrap /100 to faction score
		scrap /1000 to interfaction score
	
	If not, get HumanNPC in 50 meters, if no enemies, split proportional to alliance counts
	
On timer
	if populations < minimums, spawn on road
	if populations < maximum, if budget, spawn on road
	


	
*/

// Requires: Omninasty
// Requires: Roadster
//// Requires: Boids
#region using
	using Convert = System.Convert;
	using Network;
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.Collections;
	using System.Linq;
	using System.Text;
	using Oxide.Core.Libraries.Covalence;
	using Oxide.Plugins;
	using Oxide.Core.Plugins;
	using Oxide.Core;
	using UnityEngine; 
	using UnityEngine.SceneManagement;
	using UnityEngine.AI;
	using Rust.Ai;
	using Oxide.Ext.RustEdit;
	using Oxide.Ext.RustEdit.NPC;
using Rust;

#endregion

namespace Oxide.Plugins{
	[Info("BoidlessFactionHandler", "obsol", "0.2.1")]
	[Description("Sticks a shop on every human npc")]
	public class BoidlessFactionHandler : RustPlugin{

		public static Omninasty omninasty;
        //public static Boids boids;
        public static Roadster boids;
        void Loaded(){
			omninasty = (Omninasty)Manager.GetPlugin("Omninasty");
            boids = (Roadster)Manager.GetPlugin("Roadster");
            LoadConfig();
			foreach(string key1 in config.FactionScores.Keys.ToArray()){
				foreach(string key2 in config.FactionScores[key1].Keys.ToArray()){
					config.FactionScores[key1][key2]=((int)(config.FactionScores[key1][key2]*100))/100.0f;
					config.FactionScores[key1][key2]=config.FactionScores[key1][key2]*0.9f;
				}
			}
		}
		void Unload(){
			SaveConfig();
		}
		//
		
		[ConsoleCommand("fact.save")]
		private void surv_saveconfig(ConsoleSystem.Arg arg){
			SaveConfig();
			Puts("Faction Saved");
		}
		[ConsoleCommand("fact.load")]
		private void surv_loadconfig(ConsoleSystem.Arg arg){
			LoadConfig();
			Puts("Faction Loaded");
		}
		public int scrap = -932201673;
		public Dictionary<string,int> spawnCounts = new Dictionary<string,int>();
		public static Configuration config;
		public class Configuration{
			[JsonProperty("FactionScores", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,Dictionary<string,float>> FactionScores = new Dictionary<string,Dictionary<string,float>>();
			[JsonProperty("FactionBanks", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,float> FactionBanks = new Dictionary<string,float>();
			[JsonProperty("FactionSpawnEntities",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,List<string>> FactionSpawnEntities = new Dictionary<string,List<string>>();
			[JsonProperty("FactionSpawnLimits",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,int> FactionSpawnLimits = new Dictionary<string,int>();
			[JsonProperty("FactionSpawnMinimums",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,int> FactionSpawnMinimums = new Dictionary<string,int>();
			[JsonProperty("FactionSpawnCost",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public int FactionSpawnCost = 500;
			
			
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		protected override void LoadDefaultConfig() => config = new Configuration();
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					Puts("Configuration appears to be outdated; updating and saving");
					SaveConfig();
					}
			}
			catch{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
				
			}
		}
		object OnTurretTarget(AutoTurret at, BaseCombatEntity bce)
		{
			if (bce is BasePlayer)

                if (at.transform.name.Contains("bandit"))
                {
                    if (omninasty.getAlliances("Bandits").Contains(omninasty.getFaction(bce)))
                    {
                        return bce;
                    }
                    else if (omninasty.getAlliances("Bandits").Contains((bce as BasePlayer).userID.ToString()))
                    {
                        return bce;

                    }
                }
				else if (at.transform.name.Contains("scientist"))
				{
					if (omninasty.getAlliances("Scientist").Contains(omninasty.getFaction(bce)))
					{
						return bce;
                    }
                    else if (omninasty.getAlliances("Scientist").Contains((bce as BasePlayer).userID.ToString()))
                    {
                        return bce;
                    }

                }
				else
				{
					if(omninasty.isFriend(at.OwnerID.ToString(), omninasty.getFaction(bce))||omninasty.isFriend(at.OwnerID.ToString(), (bce as BasePlayer).userID.ToString()))
						return bce;
					else
						return null;
				}
			return null;
        }
        protected override void SaveConfig(){
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		
		
		private void OnServerInitialized()//
        {

            timer.Every(600f, () => {
                foreach (string faction in config.FactionScores.Keys)
                {
                    spawnForFaction(faction);
                }
            });
            timer.Once(60f, () => {
                foreach (string faction in config.FactionScores.Keys)
                {
                    spawnForFaction(faction);
                }
            });
            UnityEngine.Object[] GameobjectList = Resources.FindObjectsOfTypeAll(typeof(NPCAutoTurret));
			foreach (NPCAutoTurret go in GameobjectList)
			
			{
                /*
				 * 
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("["+go.Health().ToString("0000")+":"+ go.MaxHealth().ToString("0000") + "]\t"+go.baseProtection.comments);
                Console.ResetColor();
				 */
                int i = 0;
                go.SetHealth(1000);
                go.SetMaxHealth(1000);

                foreach (float f in go.baseProtection.amounts.ToArray())
                {
					try
					{
						switch ((Rust.DamageType)i)
						{
							case DamageType.Bullet://.9
							case DamageType.Arrow://0.9
								go.baseProtection.amounts[i] = 0.9f;
								break;
							case DamageType.Heat://.8
								go.baseProtection.amounts[i] = 0.8f;
								break;
							case DamageType.Slash://.5
							case DamageType.Blunt://.5
							case DamageType.Stab://.5
								go.baseProtection.amounts[i] = 0.5f;
								break;
							case DamageType.Bite://0
							case DamageType.Explosion://0
							case DamageType.AntiVehicle://0
							case DamageType.Fun_Water://0
								go.baseProtection.amounts[i] = 0f;
								break;
							default:
								break;
						}
					}
					catch(Exception e)
					{

					}
                    i++;
                }
				go.baseProtection.density = 100;
                go.SetPeacekeepermode(false);
				go.Kill();
            }
                

        }
		
		
		public bool spawnForFaction(string faction){
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnMinimums.ContainsKey(faction))
				config.FactionSpawnMinimums.Add(faction,10);
			if(!config.FactionBanks.ContainsKey(faction))
				config.FactionBanks.Add(faction,0);
			if(!config.FactionSpawnLimits.ContainsKey(faction))
				config.FactionSpawnLimits.Add(faction,50);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			if(config.FactionSpawnEntities[faction].Count>0)
            {
				int spawnAmount = (spawnCounts[faction] > 0?(int)(config.FactionSpawnMinimums[faction]/(spawnCounts[faction])): config.FactionSpawnMinimums[faction]);

				Vector3 result = Roadster.findHome(faction);
				
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.ResetColor();
                for (int z=0;z<spawnAmount;z++)
                {
                    List<PathList> roads = TerrainMeta.Path.Roads;
                    int j = UnityEngine.Random.Range(0, roads.Count);
                    PathList road = roads[j];
                    Vector3[] points = road.Path.Points;
                    int k = UnityEngine.Random.Range(0, points.Length);
                    Vector3 position = ((result != Vector3.down) ? result : points[k]);
                    int i = UnityEngine.Random.Range(0,config.FactionSpawnEntities[faction].Count);
					RadialPoint(out position, position, position, (position.y>0?20:2),(position.y>0?30:3));
                    string prefab = config.FactionSpawnEntities[faction][i];
					if(spawnCounts[faction]<config.FactionSpawnMinimums[faction]){					
						ConVar.Entity.svspawn(prefab,position,new Vector3(10,0,0));	
					}else if (config.FactionBanks[faction] > config.FactionSpawnCost && spawnCounts[faction]<config.FactionSpawnLimits[faction]){
						ConVar.Entity.svspawn(prefab,position,new Vector3(10,0,0));
					}
				}
			}
			return false;
		}
		
		object OnTurretCheckHostile(NPCAutoTurret nat, BaseCombatEntity ent)
		{
			if (nat == null) return null;
			if (ent == null) return null;
            nat.nextShotTime = UnityEngine.Time.time + 1f;
			return null;
        }


        object OnEntityDeath(BaseNpc entity, HitInfo info){
			string faction = omninasty.getFaction(entity);
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnEntities[faction].Contains(entity.ShortPrefabName))
				config.FactionSpawnEntities[faction].Add(entity.ShortPrefabName);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			spawnCounts[faction]= Math.Max(spawnCounts[faction]-1,0);
			return null;
		}
		object OnEntityDeath(HumanNPC entity, HitInfo info){
			string faction = omninasty.getFaction(entity);
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnEntities[faction].Contains(entity.ShortPrefabName))
				config.FactionSpawnEntities[faction].Add(entity.ShortPrefabName);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			spawnCounts[faction]= Math.Max(spawnCounts[faction]-1,0);
			
			return null;
		}
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info){
			if(info.HitEntity==null||info.Initiator==null) return null;
			if(info.HitEntity.gameObject==null||info.Initiator.gameObject==null) return null;
			bool? returnvar = true;
			#region squadding
				if(omninasty.isFriend(info.Initiator, info.HitEntity) )
				{
					if (info.damageTypes.Has(Rust.DamageType.Heat) ){
					
						if(info.HitEntity is HumanNPC){
							if(((HumanNPC)info.HitEntity).creatorEntity==null){
								((HumanNPC)info.HitEntity).creatorEntity=((BasePlayer)info.Initiator);
							}else{						
								((HumanNPC)info.HitEntity).creatorEntity=null;
							}
						}
						if(info.HitEntity is BaseAnimalNPC){
							if(((BaseAnimalNPC)info.HitEntity).creatorEntity==null){
								((BaseAnimalNPC)info.HitEntity).creatorEntity=((BasePlayer)info.Initiator);
							}else{						
								((BaseAnimalNPC)info.HitEntity).creatorEntity=null;
							}
						}
						return entity;
					}	
				}
				#endregion squadding
			
			#region factionScores
				//If valid for faction variation
				if(isFaction(info.Initiator)&&isFaction(info.HitEntity)){
					//Get details to apply score
					string faction1 = omninasty.getFaction(info.Initiator);
					string faction2 = omninasty.getFaction(info.HitEntity);
					float damage = info.damageTypes.Total();
					if(faction1==null||faction2==null){return null;}
					//If player, added personal score logic
					if(isPlayer(info.Initiator)){
						//Player Info
						BasePlayer bp = info.Initiator as BasePlayer;
						string id = bp.userID.ToString();
						
						//Validate safe
						if(!config.FactionScores.ContainsKey(id))
							config.FactionScores.Add(id,new Dictionary<string,float>());
						if(!config.FactionScores.ContainsKey(faction2))
							config.FactionScores.Add(faction2,new Dictionary<string,float>());
						if(!config.FactionScores[id].ContainsKey(faction2))
							config.FactionScores[id].Add(faction2,0);
						
						//Apply direct score
						config.FactionScores[id][faction2]-=damage;
						
						//Apply relational score 
						foreach(string s in config.FactionScores[faction2].Keys){
							if(config.FactionScores[faction2][s]<0){
								if(!config.FactionScores[id].ContainsKey(s))
									config.FactionScores[id].Add(s,0);
								config.FactionScores[id][s]+=damage/10;								
							}else{
								if(!config.FactionScores[id].ContainsKey(s))
									config.FactionScores[id].Add(s,0);
								config.FactionScores[id][s]-=damage/5;										
							}
						}
						//Regen
						regenAlliances(bp);
					}
					//Ignore friendly fire
					if(faction1!=faction2){
						//Validate safe for interfaction
						if(!config.FactionScores.ContainsKey(faction1))
							config.FactionScores.Add(faction1,new Dictionary<string,float>());
						if(!config.FactionScores.ContainsKey(faction2))
							config.FactionScores.Add(faction2,new Dictionary<string,float>());
						if(!config.FactionScores[faction2].ContainsKey(faction1))
							config.FactionScores[faction2].Add(faction1,0f);
						if(!config.FactionScores[faction1].ContainsKey(faction2))
							config.FactionScores[faction1].Add(faction2,0f);					
						//Apply relational score 
						foreach(string s in config.FactionScores[faction2].Keys){
							if(s==faction1)continue;

							if(config.FactionScores[faction2][s]< 0){
								if(!config.FactionScores[faction1].ContainsKey(s))
									config.FactionScores[faction1].Add(s,0);
								config.FactionScores[faction1][s]+=damage/20;								
							}else{
								if(!config.FactionScores[faction1].ContainsKey(s))
									config.FactionScores[faction1].Add(s,0);
								config.FactionScores[faction1][s]-=damage/10;										
							}
						}
						//Apply and regen for interfaction
						config.FactionScores[faction1][faction2]-=damage/5;	
						config.FactionScores[faction2][faction1]+=damage/10;	
						regenAlliances(faction1);
						regenAlliances(faction2);
					}
					
				}
				#endregion
			
			
			//set hostile
			float b = UnityEngine.Time.realtimeSinceStartup + 60f;
			if(returnvar==true)((BaseCombatEntity)info.Initiator).unHostileTime = Math.Max(((BaseCombatEntity)info.Initiator).unHostileTime , b);


			if ((returnvar == null || returnvar == false) && (info.HitEntity is HumanNPC || info.HitEntity is BaseAnimalNPC || info.HitEntity.name.Contains("cactus") || info.HitEntity.transform.name.Contains("cactus"))){
				return entity;
			}else{
				return null;
			}
		}
		void OnEntitySpawned(AutoTurret be) {
			be.SetPeacekeepermode(false);

        }
		void OnEntitySpawned(HumanNPC be){
			string faction = omninasty.getFaction(be);
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnEntities[faction].Contains(be.ShortPrefabName))
				config.FactionSpawnEntities[faction].Add(be.ShortPrefabName);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			spawnCounts[faction]++;
		}
		void OnEntitySpawned(BaseNpc be){
			string faction = omninasty.getFaction(be);
			if(!config.FactionSpawnEntities.ContainsKey(faction))
				config.FactionSpawnEntities.Add(faction,new List<string>());
			if(!config.FactionSpawnEntities[faction].Contains(be.ShortPrefabName))
				config.FactionSpawnEntities[faction].Add(be.ShortPrefabName);
			if(!spawnCounts.ContainsKey(faction))
				spawnCounts.Add(faction,0);
			spawnCounts[faction]++;
		}
		
		
		void OnVendingShopOpened(VendingMachine machine, BasePlayer player){
			if(machine is InvisibleVendingMachine){
				if(machine.transform.parent==null){return;}
				BaseAIBrain hn = machine.transform.parent.gameObject.GetComponent<BaseAIBrain>();
				if(hn.states!=null){
					if(hn.states[AIState.Roam]!=null){
                        Roadster.CustomBaseRoamState fbrs = hn.states[AIState.Roam] as Roadster.CustomBaseRoamState;
						if(fbrs != null){
							fbrs.waitTime=180;
							hn.Navigator.SetDestination(hn.Navigator.BaseEntity.transform.position);
						}
					}					
				}			
			}
		}
		void OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId, int numberOfTransactions)
		{
			string buyerFaction = omninasty.getFaction(buyer);
			
			string storeFaction="";
			
			string id = buyer.userID.ToString();
			int amount = 0;
			ProtoBuf.VendingMachine.SellOrder so = machine.sellOrders.sellOrders[sellOrderId];
			amount = (so.currencyID==scrap?so.currencyAmountPerItem:so.itemToSellAmount);
			
			if(machine is InvisibleVendingMachine &&machine.transform.parent!=null){
				storeFaction = omninasty.getFaction(machine.transform.parent.GetComponent<BaseEntity>());
			}else{
				BaseEntity[] results = new BaseEntity[10];
				BaseEntity.Query.Server.GetInSphere(machine.transform.position, 50, results,new Func<BaseEntity, bool>(isFaction));
				Dictionary<string,int> scores = new Dictionary<string,int>();
				foreach(BaseEntity be in results){
					string npcFaction = omninasty.getFaction(be);
					if(scores.ContainsKey(npcFaction)){
						scores[npcFaction]++;
					}else{
						scores[npcFaction]=1;
					}
				}
				
				foreach(string s in scores.Keys.ToArray()){
					List<string> alliances = omninasty.getAlliances(s);
					bool safe=true;
					foreach(string s2 in scores.Keys.ToArray()){
						if(s!=s2){
							safe=safe&&alliances.Contains(s2);
						}
					}
					if(safe && (storeFaction==""||scores[s]>scores[storeFaction]))
						storeFaction=s;
				}
			}
			Puts(buyerFaction+" is buying from "+storeFaction);
			if(storeFaction!="" && buyerFaction!=""){
				if(!config.FactionBanks.ContainsKey(storeFaction))
					config.FactionBanks.Add(storeFaction,0);
				
				if(!config.FactionScores.ContainsKey(id)){
					config.FactionScores.Add(id,new Dictionary<string,float>());
				}
				if(!config.FactionScores.ContainsKey(buyerFaction)){
					config.FactionScores.Add(buyerFaction,new Dictionary<string,float>());
				}
				if(!config.FactionScores.ContainsKey(storeFaction)){
					config.FactionScores.Add(storeFaction,new Dictionary<string,float>());
				}
				
				if(!config.FactionScores[id].ContainsKey(storeFaction)){
					config.FactionScores[id].Add(storeFaction,0);
				}
				if(!config.FactionScores[buyerFaction].ContainsKey(storeFaction)){
					config.FactionScores[buyerFaction].Add(storeFaction,0);
				}
				if(!config.FactionScores[storeFaction].ContainsKey(buyerFaction)){
					config.FactionScores[storeFaction].Add(buyerFaction,0);
				}
				
				config.FactionBanks[storeFaction]+=amount;
				config.FactionScores[id][storeFaction]+=amount/100f;
				config.FactionScores[buyerFaction][storeFaction]+=amount/1000f;
				config.FactionScores[storeFaction][buyerFaction]+=amount/1000f;
				regenAlliances(buyer);
				regenAlliances(buyerFaction);
				regenAlliances(storeFaction);
			}
			return;
		}
		bool isFaction(BaseEntity be){
			return (be is BasePlayer)||(be is BaseNpc);
		}
		bool isPlayer(BaseEntity be){
			BasePlayer basePlayer = be as BasePlayer;
			return basePlayer!=null && basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Connected);
		}
		string regenAlliances(BasePlayer basePlayer){			
			if(isPlayer(basePlayer)){
				string id = basePlayer.userID.ToString();
				string max = regenAlliances(id);
				omninasty.setFaction(basePlayer,max);
				return max;
			}else{
				return regenAlliances(omninasty.getFaction(basePlayer));
			}
		}
		string regenAlliances(string faction){
			if(!config.FactionScores.ContainsKey(faction)){
				config.FactionScores.Add(faction,new Dictionary<string,float>());
			}
			string max = "Default";
			foreach(string s in config.FactionScores[faction].Keys){
				if(config.FactionScores[faction][s]>0){
					if(max=="Default" || config.FactionScores[faction][s] > config.FactionScores[faction][max])
						max=s;
					omninasty.addAlliance(s,faction);
				}
				else{
					omninasty.deleteAlliance(s,faction);
				}
			}
			return max;
        }
        static bool RadialPoint(BaseNavigator nav, Vector3 target, Vector3 self, float minDist = 5, float maxDist = 8)
        {
            bool destRes = false;
            Vector3 newPosition = target;
            RadialPoint(out newPosition,target,self,minDist,maxDist);
            //newPosition.y = Terrain.activeTerrain.SampleHeight(newPosition);
            float distance = Vector3.Distance(newPosition, self);
            if (distance < 2f)
            {
                destRes = nav.SetDestination(newPosition, BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
                return destRes;
            }
            else if (distance < 4f)
            {
                destRes = nav.SetDestination(newPosition, BaseNavigator.NavigationSpeed.Normal, 0f, 0f);
                return destRes;
            }
            else
            {
                destRes = nav.SetDestination(newPosition, BaseNavigator.NavigationSpeed.Fast, 0f, 0f);
                return destRes;
            }
            return destRes;
        }

        static bool RadialPoint(out Vector3 outvect, Vector3 target, Vector3 self, float minDist = 5, float maxDist = 8)
        {
            bool destRes = false;
            float dist = UnityEngine.Random.Range(minDist, maxDist);
            float angle = UnityEngine.Random.Range(-360f, 360f);
            float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
            Vector3 newPosition = target;
            newPosition.x += x;
            newPosition.z += y;
            newPosition.y = (target.y>0? target.y+10:target.y);
            UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
            NavMesh.SamplePosition(newPosition, out nmh, 20, (int)(0xffffff));
            newPosition = nmh.position;
            outvect = newPosition;
            return true;

        }
    }
}