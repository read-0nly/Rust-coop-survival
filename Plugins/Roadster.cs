// Requires: AIZLiberator
// Requires: Cordyceps
// Requires: VehicleHandler
// Requires: Omninasty
// Requires: DataTest
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
using System.Collections.Specialized;
using System.IO;
using System.ComponentModel;
using System.Globalization;
using Facepunch.Utility;
using static Oxide.Plugins.DataTest;
using UnityEngine.UIElements;
#endregion
namespace Oxide.Plugins{
	[Info("Roadster", "obsol", "0.2.1")]
	[Description("Takes over roaming logic to allow for herding")]
	public class Roadster : CovalencePlugin{
        public static int debugCnt = 0;
        public static bool debugging = false;
        [Command("send.npc")]
        void setOrderToPlayerPoss(IPlayer player, string command, string[] args)
        {
            debugging = !debugging;
        }
        Cordyceps cordy;
		VehicleHandler vehicleHandler;
        DataTest dt;
		public static Omninasty omninasty;
		
		
		[ConsoleCommand("Roadster.save")]
		private void surv_saveconfig(ConsoleSystem.Arg arg){
			SaveConfig();
			Puts("Boid Saved");
		}
		[ConsoleCommand("Roadster.load")]
		private void surv_loadconfig(ConsoleSystem.Arg arg){
			LoadConfig();
			Puts("Boid Loaded");
		}

        string[] targets = AIZLiberator.targets;
        public static int Count = 0;
		
		public static Dictionary<string,MonumentInfo> homes = new Dictionary<string,MonumentInfo>();
		
		public static Configuration config;
		public class Configuration
        {
            [JsonProperty("monumentDelay", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float monumentDelay = 20;
			[JsonProperty("roadDelay", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float roadDelay = 3;
            [JsonProperty("monumentLoopMin", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int monumentLoopMin = 3;
            [JsonProperty("monumentLoopMax", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int monumentLoopMax = 15;
            [JsonProperty("monumentRoadDistance", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float monumentRoadDistance = 50;
            [JsonProperty("roadRoadDistance", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float roadRoadDistance = 30;
            [JsonProperty("stepSize", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int stepSize = 10;
            [JsonProperty("nodeStuck", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int nodeStuck = 20;
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
		public static CustomBaseRoamState debugTgt = null;
        void OnServerInitialized()
        {

        }
		void Unload(){
			SaveConfig();
		}
		void Loaded(){
			
			cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
			omninasty = (Omninasty)Manager.GetPlugin("Omninasty");
            vehicleHandler = (VehicleHandler)Manager.GetPlugin("VehicleHandler");
            dt = (DataTest)Manager.GetPlugin("DataTest");
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

        public static Vector3 findHome(string faction)
        {
            float max = 0;
            Node selection = null;
            Vector3 result = Vector3.down;
            foreach (Node n in Node.nodePool)
            {
                if (n is NodeMonument)
                {
                    if (!n.seats.ContainsKey(faction))
                    {
                        n.seats.Add(faction, new Dictionary<long, float>());
                    }
                    int count = n.seats[faction].Count();
                    if (count > n.seatMin) {
                        if (count > max)
                        {
                            max = count;
                            selection = n;
                        }
                    }   
                }
            }
            if (selection != null)
                result = selection.GetPoint();
            return result;
            return result;
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
			public int currentNodeIndex=0;
			public bool homeSearch=true;
			public bool preference=true;
			public long homeTimeout=0;
            public int homeTime = 300;
            public int homeDelay = 3300;
            public int homeVariance = 60;
            public string faction="";
            public int stuckAtNode = 0;
			public bool isAnchored = false;
			public List<string> alliances = new List<string>();
			List<DataTest.Connection> steps;
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
            public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
            {
                waitTime = defaultWaitTime;
                if (this.brain.Navigator.BaseEntity.creatorEntity != null)
                {
                    if (!isAnchored)
                    {
                        ColorWrite("Anchoring agent going to " + steps[0].parent.name, ConsoleColor.Cyan);

                    }
                    isAnchored = true;
                    if (this.brain.Navigator.BaseEntity.creatorEntity.transform)
                    {
                        waitTime += 10;
                        RadialPoint(this.brain.Navigator, this.brain.Navigator.BaseEntity.creatorEntity.transform.position, this.brain.transform.position);
                        return;
                    }

                }
                else
                {
                    if (isAnchored)
                    {
                        steps = new List<DataTest.Connection>();
                        NodeInterest.findNearest(steps, entity.transform.position);
                        ColorWrite("Released agent going to " + steps[0].parent.name, ConsoleColor.Red);

                    }
                    isAnchored = false;
                }

                base.StateEnter(brain, entity);

                if (entity.transform.position.y > 0 && (debugTgt == null || debugTgt.brain == null || debugTgt.brain.GetBrainBaseEntity() == null || (debugTgt.brain.GetBrainBaseEntity() is BasePlayer && (debugTgt.brain.GetBrainBaseEntity() as BasePlayer).IsDead())))
                {
                    debugTgt = this;
                    debugCnt++;
                    //UnityEngine.Debug.Log("Switching focus");
                }


                this.brain.Navigator.ClearFacingDirectionOverride();
                int i = 0;
                this.currentWaitTime = 0;
                bool flag = false;
                //ColorWrite("Checking water depth", ConsoleColor.Cyan);
                if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.05f)
                {
                    if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.75f)
                    {
                        (entity as BasePlayer).Hurt(10f);
                    }
                    flag = true;
                }
                else
                {
                    (entity as BasePlayer).Heal(0.5f);
                }
                if (this.brain == null)
                {
                    return;
                }
                if (this.brain.Navigator == null)
                {
                    return;
                }
                if (this.brain.Navigator.BaseEntity == null)
                {
                    return;
                }
                if (!flag)
                    lastLocation = this.brain.Navigator.BaseEntity.gameObject.transform.position;
				

                if (steps == null || steps.Count() == 0)
                {
                    steps = new List<DataTest.Connection>();
                    NodeInterest.findNearest(steps, entity.transform.position);
                    //if (steps.Count() > 0) 
                    //ColorWrite(entity.transform.position.ToString()+ " Sent to " +steps[0].parent.name,ConsoleColor.Magenta);
                }
                Vector3 target = this.brain.transform.position;

                Vector3 targetPin = new Vector3(0, 0, 0);
                if (steps.Count() > 0)
                {
                    //ColorWrite("Getting Wait Time", ConsoleColor.Cyan);
                    if (steps[0].parent is NodeInterest)
                    {
                        waitTime += config.monumentDelay;
                    }
                    else { waitTime += config.roadDelay; }
                    //ColorWrite("Getting NodeIndex", ConsoleColor.Cyan);
                    string log="";
                    string StepLog = "";
                    string stepname = steps[0].parent.name + " connecting to " + steps[0].finish.name + " who links back? " + steps[0].finish.links.ContainsKey(steps[0].parent).ToString() ;
                    if (steps[0].parent is NodeInterest || Vector3.Distance(entity.transform.position, ((steps[0].parent as NodeTransit)).GetPoint(steps,currentNodeIndex,entity,out log )) < config.roadRoadDistance * 2)
                    {
                        currentNodeIndex = steps[0].parent.Step(steps, currentNodeIndex,entity, out StepLog);
                        stuckAtNode = 0;
                    }
                    else {
                        stuckAtNode++;
                    }

                    if (steps == null || steps.Count() == 0)
                    {
                        steps = new List<DataTest.Connection>();
                        NodeInterest.findNearest(steps, entity.transform.position);
                        ColorWrite("Resetting  steps -  previous step "+stepname+"\n"+ log+"\n"+StepLog, ConsoleColor.Red);
                        //if (steps.Count() > 0) 
                        //ColorWrite(entity.transform.position.ToString()+ " Sent to " +steps[0].parent.name,ConsoleColor.Magenta);
                    }
                    if ((steps[0].parent is NodeTransit) && currentNodeIndex >= ((steps[0].parent as NodeTransit)).points.Count()) { currentNodeIndex = ((steps[0].parent as NodeTransit)).points.Count() - 1; }
                    if (currentNodeIndex < 0) { currentNodeIndex = 0; }

                    //ColorWrite("Getting Point", ConsoleColor.Cyan);
                    if (steps[0].parent is DataTest.NodeInterest)
                    {
                        string s = "";
                        target = ((DataTest.NodeInterest)steps[0].parent).GetPoint(steps, currentNodeIndex, entity, out s);
						targetPin = target;
						float maxX = ((DataTest.NodeInterest)steps[0].parent).radius;
						float maxZ = ((DataTest.NodeInterest)steps[0].parent).radius;
						target.x += UnityEngine.Mathf.Sqrt(UnityEngine.Random.Range(0, maxX)) * ((UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1));
						target.z += UnityEngine.Mathf.Sqrt(UnityEngine.Random.Range(0, maxZ)) * ((UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1));
					}
					else if (steps[0].parent is NodeTransit && currentNodeIndex > -1 && currentNodeIndex < (((NodeTransit)steps[0].parent).points.Count()))
                    {
						string s = "";
						if (steps[0].parent is NodeWarp)
						{
							string logstr = "";
							//ColorWrite("GettingWarpTarget", ConsoleColor.Cyan);
							Vector3 nextPoint =steps[0].finish.GetPoint(steps, steps[0].finish.links[steps[0].parent], entity, out logstr);
							bool aboveGround = steps[0].finish.points[0].y > 0;
							Vector3 targetPosition = (aboveGround ? (steps[0].parent as NodeWarp).A.transform.position : (steps[0].parent as NodeWarp).B.transform.position);
                            Vector3 lastPos = this.brain.transform.position;
                            Vector3 position;
                            Vector3 positionSecondary;
                            RadialPoint(out position, targetPosition, this.brain.transform.position, (aboveGround?15:1), (aboveGround ? 20 : 2));
                            RadialPoint(out positionSecondary, targetPosition, this.brain.transform.position, 2, 5);
                            bool remove = true;
                            if (!this.brain.Navigator.Warp(position))
                            {
								if (!this.brain.Navigator.Warp(targetPosition))
								{
									remove = false;
									if (!this.brain.Navigator.Warp(lastPos))
									{
										entity.Kill();
									}
								}
                            }
                            //ColorWrite("Warping from " + steps[0].start.name + " to " + steps[0].finish.name + " for  " + entity.GetType().ToString(), ConsoleColor.Magenta);
							if (remove)
                            {
                                stuckCount = 0;
                                stuckAtNode = 0;
                                string sdsfdsgsdfg = "";
								steps[0].finish.Extend(steps, steps[0].finish.links[steps[0].parent], entity, out sdsfdsgsdfg);
								steps.RemoveAt(0);
							}
							return;
                        }
						else
						{
							target = ((NodeTransit)steps[0].parent).GetPoint(steps, currentNodeIndex, entity, out s);
                            targetPin = target;
                        }

                    }

                    if (this == debugTgt)
                    {
                        switch (debugCnt % 3)
                        {
                            case 0:
                                break;
                            case 1:
                                break;
                            case 2:
                                break;
                            default: break;
                        }
                        //if(debugging)
                            //ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[] { config.monumentDelay, Color.cyan, target, "X" });

                    }
                }
                bool isAggro = false;
                this.brain.Navigator.Path = null;

                //ColorWrite("Generating debug info", ConsoleColor.Cyan);
                Color textCol = Color.red;
                Color oldtextCol = Color.Lerp(Color.red, Color.clear, 0.8f);
                string repchar = "[#]";
                if (entity is BanditGuard) {
                    textCol = Color.red;
                    oldtextCol = Color.Lerp(Color.red, Color.clear, 0.1f);
                }
                else if (entity is ScientistNPC)
                {
                    textCol = Color.cyan;
                    oldtextCol = Color.Lerp(Color.cyan, Color.clear, 0.1f);
                }
                else if (entity is TunnelDweller)
                {
                    textCol = Color.yellow;
                    oldtextCol = Color.Lerp(Color.yellow, Color.clear, 0.1f);
                }
                else if (entity is UnderwaterDweller)
                {
                    textCol = Color.yellow;
                    oldtextCol = Color.Lerp(Color.yellow, Color.clear, 0.1f);
                }
                if (debugTgt == this && debugging)
                {
                    textCol = Color.green;
					if (steps.Count() > 0)
					{
						ColorWrite("Next connection: " + (steps[0].start != null ? steps[0].start.name : "nullval") + " > " + (steps[0].parent != null ? steps[0].parent.name : "nullval") + " > " + (steps[0].finish != null ? steps[0].finish.name : "nullval"), ConsoleColor.Yellow);
						if (steps.Count() > 1)
						{
							for (int z = 1; z < steps.Count(); z++)
							{
								ConsoleColor conscol = (steps[z].start.name.Contains("Bandit") || steps[z].start.name.StartsWith("Out") ? ConsoleColor.Magenta : ConsoleColor.Cyan);
								conscol = (steps[z].start.name.Contains("Rail") ? ConsoleColor.Red : conscol);
								//if(conscol== ConsoleColor.Cyan) { continue; }
								ColorWrite("Future connection: " + (steps[z].start != null ? steps[z].start.name : "nullval") + " > " + (steps[z].parent != null ? steps[z].parent.name : "nullval") + " > " + (steps[z].finish != null ? steps[z].finish.name : "nullval"), conscol);
							}
						}
					}
					else
					{
                        ColorWrite("Tracked agent has no steps", ConsoleColor.Red);
                    }
                }
				if (debugging)
				{
					//ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[] { 120, oldtextCol, entity.transform.position, repchar });
					Vector3 typeNudge = new Vector3(0,0,0);
					if(entity is ScientistNPC)
					{
						typeNudge = new Vector3(10, 0, 0);

                    }
                    else if(entity is TunnelDweller)
                    {
                        typeNudge = new Vector3(-10, 0, 0);

                    }
                    else if (entity is UnderwaterDweller)
                    {
                        typeNudge = new Vector3(0, 0, 10);

                    }
                    else if (entity is BanditGuard)
                    {
                        typeNudge = new Vector3(0, 0, -10);

                    }
                    if (steps.Count()>0 && steps[0].parent is NodeMonument)
                    {
                        ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[] { 15, oldtextCol, (targetPin==new Vector3(0,0,0)?target:targetPin) + typeNudge, repchar });
						//if(textCol==Color.green)
							//ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[] { waitTime, textCol, entity.transform.position, repchar });
						//else
                            //ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[] { waitTime, textCol, entity.transform.position, repchar });
                    }
                }
                Vector3 resultVector = new Vector3(0, 0, 0);
                if (target!=new Vector3(0, 0, 0))
				{
                    resultVector = target - this.brain.transform.position;
                }
                //ColorWrite("Moving", ConsoleColor.Cyan);
                if (stuckCount>3 || stuckAtNode > config.nodeStuck)
                {
					Vector3 position;
					Vector3 lastPos = this.brain.transform.position;
					RadialPoint(out position, this.brain.transform.position+(resultVector.normalized*5),this.brain.transform.position,1,2);
					stuckCount=0;
                    stuckAtNode = 0;
                    if (!this.brain.Navigator.Warp(position)){
						if(!this.brain.Navigator.Warp(lastPos)){
							entity.Kill();
						}
					}
				}
				else
                {
					Vector3 selectedPoint= this.brain.transform.position;
                    RadialPoint(out selectedPoint, this.brain.transform.position + (resultVector), this.brain.transform.position, 3, config.roadRoadDistance);

                    if (WaterLevel.GetOverallWaterDepth(selectedPoint) > 0.2f)
					{
                        selectedPoint = this.brain.transform.position;
					}
					if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.5f)
                    {
                        RadialPoint(this.brain.Navigator, lastLocation, this.brain.transform.position, 5, 15);
                    }
					else
                    {
                        RadialPoint(this.brain.Navigator, selectedPoint, this.brain.transform.position, 3, config.roadRoadDistance);
                    }
				}
					
				waitTime+=(!isAggro? 0:0);//monumentDelay
                status = StateStatus.Running;

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

                        }
                        else{
							return StateStatus.Running;
						}
					}

				}
				else
				{

                }
				if (this.currentWaitTime >= waitTime)
				{///
					if(Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<0.3f){
						stuckCount++;
					};
					this.currentWaitTime = 0;
					HumanNPC humanNPC = entity as HumanNPC;
					if (humanNPC != null)
					{
						if (humanNPC.healthFraction <= brain.HealBelowHealthFraction && UnityEngine.Random.Range(0f, 1f) <= brain.HealChance)
						{
							global::Item item = humanNPC.FindHealingItem();
							if (item != null)
							{
								global::BaseEntity baseEntity = brain.Events.Memory.Entity.Get(brain.Events.CurrentInputMemorySlot);
								if (baseEntity == null || (!brain.Senses.Memory.IsLOS(baseEntity) && Vector3.Distance(entity.transform.position, baseEntity.transform.position) >= 5f))
								{
									humanNPC.UseHealingItem(item);
								}
							}
						}
					}
                    return StateStatus.Finished;
				}else{
					return StateStatus.Running;
				}
				
			}
			
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{ 
				waitTime=defaultWaitTime;
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
			Vector3 newPosition = target;
            bool destRes = false;
            RadialPoint(out newPosition,target,self,minDist,maxDist);

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
		static bool RadialPoint(out Vector3 outvect, Vector3 target, Vector3 self, int areamask, float minDist = 5, float maxDist = 8)
		{

            float dist = UnityEngine.Random.Range(minDist, maxDist);
            float angle = UnityEngine.Random.Range(-360f, 360f);
            float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
            Vector3 newPosition = target;
            newPosition.x += x;
            newPosition.z += y;
            newPosition.y = target.y + (UnityEngine.Random.Range(-5, 6));
            UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
            NavMesh.SamplePosition(newPosition, out nmh, 20, areamask);
            newPosition = nmh.position;
            outvect = newPosition;
            return true;
        }
        static bool RadialPoint(out Vector3 outvect, Vector3 target, Vector3 self,float minDist = 5,float maxDist=8){
			return RadialPoint(out outvect, target, self, 25, minDist, maxDist);
			
		}
	}
}
