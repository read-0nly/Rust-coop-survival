// Requires: AIZLiberator
// Requires: Cordyceps
// Requires: VehicleHandler
// Requires: Omninasty
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
using ConVar;
using static BaseEntity.RPC_Server;
#endregion
namespace Oxide.Plugins{
	[Info("Boids", "obsol", "0.2.1")]
	[Description("Takes over roaming logic to allow for herding")]
	public class Boids : RustPlugin{
		public class BoidInfo : MonoBehaviour{
			public Vector3 heading = new Vector3 (0,0,0);
		}
		Cordyceps cordy;
		VehicleHandler vehicleHandler;
		public static Omninasty omninasty;
		public static Dictionary<string,List<BoidPoint>> points = new Dictionary<string,List<BoidPoint>>();
		
		
		[ConsoleCommand("boid.save")]
		private void surv_saveconfig(ConsoleSystem.Arg arg){
			SaveConfig();
			Puts("Boid Saved");
		}
		[ConsoleCommand("boid.load")]
		private void surv_loadconfig(ConsoleSystem.Arg arg){
			LoadConfig();
			Puts("Boid Loaded");
		}

        string[] targets = new string[]{"assets/rust.ai/agents/npcplayer/humannpc/banditguard/npc_bandit_guard.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_mp5.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_pistol.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_shotgun.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_peacekeeper.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab"};
        public static int Count = 0;
		
		public static Dictionary<string,MonumentInfo> homes = new Dictionary<string,MonumentInfo>();
		
		public static Configuration config;
		public class Configuration{
			[JsonProperty("Attract",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float Attract = 90;
			[JsonProperty("Align",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float Align = 50;
			[JsonProperty("Repulse",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float Repulse = 10;
			[JsonProperty("PointAttract",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float PointAttract = 200;
			[JsonProperty("PointFactor",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float PointFactor = 3;
			[JsonProperty("SelfFactor",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float SelfFactor = 1;
			[JsonProperty("AttractionFactor",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float AttractionFactor = 2;
			[JsonProperty("DeflectionFactor",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float DeflectionFactor = 10;
			[JsonProperty("CohesionFactor",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float CohesionFactor = 2.5f;
			[JsonProperty("AttractOptimal",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float AttractOptimal = 70;
			[JsonProperty("AttractHome",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float AttractHome = 60;
			[JsonProperty("HomeOptimal",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public float HomeOptimal = 40;
			[JsonProperty("HomeList",ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public Dictionary<string,List<string>> HomeList = new Dictionary<string,List<string>>();
			
			
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
		protected override void SaveConfig(){
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		
		
		public class BoidPoint{
			public string faction = "";
			public Vector3Int position;
			public Vector3 direction;
			public float attraction;
			public float redirect;
			public float timeout;
		}
		
		public void addPoint(string faction, Vector3 position, Vector3 direction, float attraction = 1, float redirect = 0, float timeout = 600){
			BoidPoint bp = new BoidPoint();
			bp.faction=faction;
			bp.position=new Vector3Int((int)position.x,(int)position.y,(int)position.z);
			bp.direction=direction;
			bp.attraction=attraction;
			bp.redirect=redirect;
			bp.timeout=timeout+ UnityEngine.Time.time;
		}
		void OnServerInitialized(){
			
			foreach (MonumentInfo mi in TerrainMeta.Path.Monuments.ToArray()){
				string key = mi.displayPhrase.translated.Replace("\r","").Replace("\n","").Replace("\t","").Replace(" ","");
				if(!homes.ContainsKey(key) && mi.transform.position!=(new Vector3(0,0,0)) && !key.Contains("ishing") && !key.Contains("ighthouse"))
                {
					homes.Add(key,mi);
					Puts("Map has:[" + key+"]");
				}
			}
		}
		void Unload(){
			SaveConfig();
		}
		void Loaded(){
			
			cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
			omninasty = (Omninasty)Manager.GetPlugin("Omninasty");
			vehicleHandler = (VehicleHandler)Manager.GetPlugin("VehicleHandler");
			cordy.WalkableOnly = false;
			VehicleHandler.AIZRoam = false;
			loadStates();
			LoadConfig();
			
		}
		void loadStates(){
			foreach(string s in targets){
				CustomBaseRoamState cbrs= new CustomBaseRoamState();
				cordy.AssignHumanState(s, cbrs.StateType, cbrs);
			}			
		}
		public class CustomBaseRoamState : VehicleHandler.CustomBaseRoamState{
			public StateStatus status = StateStatus.Error;
			public AIMovePointPath path;
			public AIMovePoint currentTargetPoint;
			public float currentWaitTime;
			public Vector3 lastLocation = new Vector3(0,0,0);
			public float defaultWaitTime=UnityEngine.Random.Range(2f,3f);
			public float waitTime = 0;
			public int stuckCount = 0;
			public int currentNodeIndex;
			public bool homeSearch=true;
			public bool preference=true;
			public long homeTimeout=0;
            public int homeTime = 300;
            public int homeDelay = 3300;
            public int homeVariance = 60;
            public string faction="";
			public List<string> alliances = new List<string>();
			MonumentInfo home=null;
			public CustomBaseRoamState() : base(){
				waitTime = defaultWaitTime;
				preference=UnityEngine.Random.Range(0,1)==0;
				
			}
			public float bumpCurve(float val, float width, float center){
				return Mathf.Max(0,(-Mathf.Pow(((val-center)/width),2)+1));
			}
			public float sCurve(float val, float slope, float center){
				return Mathf.Max(0,(1/(1+Mathf.Exp((val-center)/slope))));
			}
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity){
				base.StateEnter(brain,entity);
				waitTime = defaultWaitTime;
				
				BoidInfo bime = entity.GetComponent<BoidInfo>();
				if(bime==null){
					bime=entity.gameObject.AddComponent<BoidInfo>();
					bime.heading = entity.transform.forward.normalized;
				}
				Vector3 resultVector =entity.transform.forward.normalized * config.SelfFactor;
				if(home==null&&homeSearch){
					homeSearch=false;
					if(config.HomeList.ContainsKey(entity.GetType().ToString())){
						foreach(string s in (config.HomeList[entity.GetType().ToString()].ToArray())){
							if(homes.ContainsKey(s)){
								home = homes[s];
                                if(Vector3.Distance(entity.transform.position, home.transform.position)<100)
									homeTimeout = DateTime.Now.Ticks + (TimeSpan.TicksPerSecond * homeTime);
                                break;
							}
						}
							
					}else{
						config.HomeList.Add(entity.GetType().ToString(), new List<string>());
					}
				}
				if (homeTimeout < (DateTime.Now.Ticks - (homeDelay * TimeSpan.TicksPerSecond)))
				{
					homeTimeout = DateTime.Now.Ticks + (homeTime * TimeSpan.TicksPerSecond) + (UnityEngine.Random.Range(0, homeVariance) * 
						TimeSpan.TicksPerSecond);
                }
				bool homeSpace = false;
				float distanceToHome =-1;
				if((home!=null)){
					distanceToHome = Vector3.Distance(home.transform.position, entity.transform.position);
					homeSpace=distanceToHome<config.HomeOptimal /2;
					resultVector +=( ((home.transform.position - entity.transform.position).normalized + 
						(distanceToHome < config.HomeOptimal / 1.5f ? (
						preference ?
                        Quaternion.AngleAxis(90, Vector3.up)*(home.transform.position - entity.transform.position).normalized*2 :
                        Quaternion.AngleAxis(-90, Vector3.up)*(home.transform.position - entity.transform.position).normalized*2
						) : new Vector3(0, 0, 0))
						) ) *
					 (config.AttractHome) * sCurve(distanceToHome, 500, config.HomeOptimal)*10;
                    ;
				}
				foreach (MonumentInfo mi in homes.Values){
					if(mi==home)continue;
					float distanceToMon = Vector3.Distance(mi.transform.position, entity.transform.position);
					float scale = 1;
					if(config.HomeList.ContainsKey(entity.GetType().ToString())&&!config.HomeList[entity.GetType().ToString()].Contains(mi.displayPhrase.translated.Replace("\r","").Replace("\n","").Replace("\t","").Replace(" ",""))){
						scale=0.3f;
					}
					//waitTime+=(distanceToHome<config.HomeOptimal?3:0);
					resultVector +=(( (distanceToMon<config.HomeOptimal/2?0.5f:1f)*((mi.transform.position - entity.transform.position).normalized +
                        (distanceToHome < config.HomeOptimal / 1.5f ? (
                        preference ?
                        Quaternion.AngleAxis(90, Vector3.up) * (mi.transform.position - entity.transform.position).normalized * 2 :
                        Quaternion.AngleAxis(-90, Vector3.up) * (mi.transform.position - entity.transform.position).normalized * 2
                        ) : new Vector3(0, 0, 0))) * 
					(config.AttractHome)*sCurve(distanceToMon, 500, config.HomeOptimal)))*1;
				}
				if(entity!=null && faction=="")
					faction = omninasty.getFaction(entity);
				alliances = omninasty.getAlliances(faction);
				
				List<BoidPoint> validPoints = new List<BoidPoint>();
				if(!points.ContainsKey(faction))
					points.Add(faction, new List<BoidPoint>());				
				foreach(BoidPoint bp in points[faction].ToArray()){
					if(bp.timeout< UnityEngine.Time.time)
                    {
						points[faction].Remove(bp);
						continue;
					}
					if(Vector3.Distance(entity.transform.position, bp.position)<config.PointAttract){
						validPoints.Add(bp);
					}
					
				}	
				
				foreach(string alliance in alliances){
					if(!points.ContainsKey(alliance))
						points.Add(alliance, new List<BoidPoint>());		
					foreach(BoidPoint bp in points[alliance]){
						if(bp.timeout< UnityEngine.Time.time){
							points[alliance].Remove(bp);
							continue;
						}
						if(Vector3.Distance(entity.transform.position, bp.position)<config.PointAttract){
							validPoints.Add(bp);
						}
					}
					
				}
				
				
				foreach(BoidPoint bp in validPoints){
					float distance = Vector3.Distance(entity.transform.position, bp.position);
					Vector3 direction = (bp.position - entity.transform.position).normalized * config.PointFactor * bp.attraction * (config.PointAttract/distance);
					Vector3 alignment = bp.direction.normalized * config.PointFactor * bp.redirect * (config.PointAttract/distance);
					resultVector+=direction+alignment;
				}
				
				
				
				this.brain.Navigator.ClearFacingDirectionOverride();
				int i=0;
				this.currentWaitTime =0;
				bool flag = false;
				if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.2f)
                {
					if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.99f)
					{
						(entity as BasePlayer).Hurt(10f);
					}
					flag=true;
				}else{
					(entity as BasePlayer).Heal(0.5f);					
				}
				if(this.brain==null){
					return;
				}		
				if(this.brain.Navigator==null){
					return;}
				if(this.brain.Navigator.BaseEntity==null){
					return;
				}		
				if(!flag)
					lastLocation = this.brain.Navigator.BaseEntity.gameObject.transform.position;
				
				if(this.brain.Navigator.BaseEntity.creatorEntity){
					if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
						RadialPoint(this.brain.Navigator,this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);						
						return ;
					}
				}
				bool isAggro = false;
				this.brain.Navigator.Path=null;
				//if (homeTimeout > DateTime.Now.Ticks )
				//{
					foreach (BaseCombatEntity bce in brain.Senses.Memory.Friendlies)
					{
						if (bce is BaseNpc) continue;
						if (bce == null || bce.transform == null) { continue; }
                        BoidInfo bi = bce.GetComponent<BoidInfo>();
                        BaseAIBrain br = bce.GetComponent<BaseAIBrain>();
						if (br == null) continue;
                        if (bi == null)
						{
							bi = bce.gameObject.AddComponent<BoidInfo>();
							bi.heading = bce.transform.forward.normalized;
						}
						isAggro |= (br.Senses.TimeInAgressiveState > 0);

                        float distance = Vector3.Distance(entity.transform.position, bce.transform.position);
						float AttractDistance = bumpCurve(distance, 45 * (br.Senses.TimeInAgressiveState>0?2:1), config.AttractOptimal);
                        //40/Mathf.Max(40,(Mathf.Sqrt(Mathf.Abs(distance-config.AttractOptimal)))) ; homeSpace
                        //Mathf.Abs(distance-config.AttractOptimal)/config.AttractOptimal;
                        float RepelDistance = sCurve(distance, (homeSpace ? 5 : 1), config.Repulse * (homeSpace ? 5 : 1));
						//0.5f/Mathf.Max(0.5f,(Mathf.Sqrt(Mathf.Abs(distance-config.Repulse))));
						if (distance < config.Attract)
						{
							Vector3 newVector = new Vector3(0, 0, 0);
                            newVector += (bime.heading.normalized) * config.SelfFactor;
                            newVector += (bi.heading.normalized) * config.CohesionFactor * bumpCurve(distance, 10, config.Align);
                            newVector += (entity.transform.position - bce.transform.position).normalized * RepelDistance * config.DeflectionFactor;
                            newVector += (bce.transform.position - entity.transform.position).normalized * AttractDistance * config.AttractionFactor *
								(br.Senses.TimeInAgressiveState+1);
							//newVector = newVector * (homeSpace ? 0.5f : 1f);
                            resultVector += newVector;


						}
					}
				//}
				resultVector.y=0;
				
				bime.heading = (flag ?
                            ((lastLocation - entity.transform.position) +
                            (Quaternion.AngleAxis(90, Vector3.up) * (lastLocation - entity.transform.position).normalized * 2)).normalized
                            : resultVector.normalized);
				resultVector= bime.heading*(UnityEngine.Random.Range(7f,15f)*(!homeSpace?(flag?2f:1f):3f));
				
				if(waitTime==defaultWaitTime){
					if(stuckCount>3){
						Vector3 position;
						Vector3 lastPos = this.brain.transform.position;
						RadialPoint(out position, this.brain.transform.position+(resultVector.normalized*5),this.brain.transform.position,1,2);
						stuckCount=0;
						if(!this.brain.Navigator.Warp(position)){
							if(!this.brain.Navigator.Warp(lastPos)){
								entity.Kill();
							}
						}
					}
					else{
						RadialPoint(this.brain.Navigator, this.brain.transform.position+(resultVector),this.brain.transform.position,1,3);
					}
					
				}
				waitTime=defaultWaitTime+(homeSpace&&!isAggro? 45:0);
				status= StateStatus.Running;
				return;
			
			}
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
			{
				this.currentWaitTime += delta;
				if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
					if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
						if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 10f || this.currentWaitTime >= waitTime)
						{//
							return StateStatus.Finished;
						}else{
							return StateStatus.Running;
						}
					}
				}
				if (this.currentWaitTime >= waitTime)
				{///
					if(Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<2){
						stuckCount++;
					};
					this.currentWaitTime = 0;

                    return StateStatus.Finished;
				}else{
					return StateStatus.Running;
				}
				
			}
			
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{ 
				waitTime=defaultWaitTime;
				BoidInfo bime = entity.GetComponent<BoidInfo>();
				if(bime==null){
					bime=entity.gameObject.AddComponent<BoidInfo>();
				}
				bime.heading = entity.transform.forward.normalized;
				base.StateLeave(brain,entity);
			}
			
		}
		public static System.Random rng = new System.Random(); 
		public static UnityEngine.Object[] Shuffle(UnityEngine.Object[] list,int seed)  
		{  
			int n = list.Length;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				UnityEngine.Object v = list[k];  
				list[k] = list[n];  
				list[n] = v;  
			}  
			return list;
		}
		public static System.Object[] Shuffle(System.Object[] list)  
		{  
			int n = list.Length;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				System.Object v = list[k];  
				list[k] = list[n];  
				list[n] = v;  
			}  
			return list;
		}
		

		
		static bool RadialPoint(BaseNavigator nav, Vector3 target, Vector3 self,float minDist = 5,float maxDist=8){
				bool destRes = false;
				float dist = UnityEngine.Random.Range(minDist,maxDist);
				float angle = (180+nav.transform.eulerAngles.y) + UnityEngine.Random.Range(-60f,60f);
				float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
				float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
				Vector3 newPosition = target;
				newPosition.x += x;
				newPosition.z += y;
				newPosition.y = TerrainMeta.HeightMap.GetHeight(newPosition)+10;
				UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
				NavMesh.SamplePosition(newPosition, out nmh, 20, (int)(0xffffff));
				newPosition = nmh.position;
            //newPosition.y = Terrain.activeTerrain.SampleHeight(newPosition);
            float distance = Vector3.Distance(newPosition,self);
				if (distance<2f){
					 destRes=nav.SetDestination(newPosition, BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
					return destRes;
					}
				else if (distance<4f){
					 destRes=nav.SetDestination(newPosition, BaseNavigator.NavigationSpeed.Normal, 0f, 0f);
					return destRes;
					}
				else{
					destRes=nav.SetDestination(newPosition, BaseNavigator.NavigationSpeed.Fast, 0f, 0f);							
					return destRes;
					}							
				return destRes;	
			}

		static bool RadialPoint(out Vector3 outvect, Vector3 target, Vector3 self,float minDist = 5,float maxDist=8){
				bool destRes = false;
				float dist = UnityEngine.Random.Range(minDist,maxDist);
				float angle =UnityEngine.Random.Range(-360f,360f);
				float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
				float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
				Vector3 newPosition = target;
				newPosition.x += x;
				newPosition.z += y;
				newPosition.y = TerrainMeta.HeightMap.GetHeight(newPosition) + 10;
				UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
				NavMesh.SamplePosition(newPosition, out nmh, 20, (int)(0xffffff));
				newPosition = nmh.position;
				outvect = newPosition;
				return true;
			
		}
	}
}
