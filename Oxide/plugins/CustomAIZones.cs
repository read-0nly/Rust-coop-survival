
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
using  UnityEditor; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
	using Oxide.Core.Plugins;
using System.Threading;
	using Oxide.Core;
	using Newtonsoft.Json;

namespace Oxide.Plugins
{
	[Info("CustomAIZones", "obsol", "0.0.1")]
	[Description("For breaking monuments")]
	public class CustomAIZones : CovalencePlugin
	{
		
		#region config
		public ConfigData config;
		public class ConfigData
		{
			[JsonProperty("version", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Oxide.Core.VersionNumber Version = default(VersionNumber);
		}
		protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    UpdateConfigValues();
                }
            }
            catch (Exception ex)
            {
                PrintError($"The configuration file is corrupted or missing. \n{ex}");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
			Puts("Version mismatch for config");
            config = new ConfigData();
            config.Version = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void UpdateConfigValues()
        {
            if (config.Version < Version)
            {
                if (config.Version <= default(VersionNumber))
                {
					Puts("Version mismatch for config");
                }
                config.Version = Version;
            }
        }

        private bool GetConfigValue<T>(out T value, params string[] path)
        {
            var configValue = Config.Get(path);
            if (configValue == null)
            {
                value = default(T);
                return false;
            }
            value = Config.ConvertValue<T>(configValue);
            return true;
        }

		#endregion
		
		List<AIPoint> customPoints = new List<AIPoint>();
		List<AIMovePoint> customMovePoints = new List<AIMovePoint>();
		List<AICoverPoint> customCoverPoints = new List<AICoverPoint>();
		List<MonumentInfo> customMonuments = new List<MonumentInfo>();
		List<AIMovePointPath> allPaths = new List<AIMovePointPath>();
		struct AgentProperties{
			public static float acceleration;
			public static int agentTypeID;
			public static float angularSpeed;
			public static int areaMask;
			public static bool autoBraking;
			public static bool autoRepath;
			public static bool autoTraverseOffMeshLink;
			public static int avoidancePriority;
			public static float baseOffset;
			public static float height;
			public static ObstacleAvoidanceType obstacleAvoidanceType;
			public static float radius;
			public static float speed;
			public static float stoppingDistance;
			public static bool updatePosition;
			public static bool updateRotation;
			public static bool updateUpAxis;
			public static int walkableMask;
			
			public static void Store(NavMeshAgent nma){
			 acceleration = nma.acceleration;
			 agentTypeID = nma.agentTypeID;
			 angularSpeed = nma.angularSpeed;
			 areaMask = nma.areaMask;
			 autoBraking = nma.autoBraking;
			 autoRepath = nma.autoRepath;
			 autoTraverseOffMeshLink = nma.autoTraverseOffMeshLink;
			 avoidancePriority = nma.avoidancePriority;
			 baseOffset = nma.baseOffset;
			 height = nma.height;
			 obstacleAvoidanceType = nma.obstacleAvoidanceType;
			 radius = nma.radius;
			 speed = nma.speed;
			 stoppingDistance = nma.stoppingDistance;
			 updatePosition = nma.updatePosition;
			 updateRotation = nma.updateRotation;
			 updateUpAxis = nma.updateUpAxis;
			 walkableMask = nma.walkableMask;
				
			}
			public static void Set(NavMeshAgent nma){
			 nma.acceleration = acceleration;
			 nma.agentTypeID = agentTypeID;
			 nma.angularSpeed = angularSpeed;
			 nma.areaMask = areaMask;
			 nma.autoBraking = autoBraking;
			 nma.autoRepath = autoRepath;
			 nma.autoTraverseOffMeshLink = autoTraverseOffMeshLink;
			 nma.avoidancePriority = avoidancePriority;
			 nma.baseOffset = baseOffset;
			 nma.height = height;
			 nma.obstacleAvoidanceType = obstacleAvoidanceType;
			 nma.radius = radius;
			 nma.speed = speed;
			 nma.stoppingDistance = stoppingDistance;
			 nma.updatePosition = updatePosition;
			 nma.updateRotation = updateRotation;
			 nma.updateUpAxis = updateUpAxis;
			 nma.walkableMask = walkableMask;
				
			}
		}
		private void OnServerInitialized()
        {
			Puts("Getting Points");
			GetCustomAIPoints();
			
			Puts("Getting markers");
			GetMonumentMarkers();//assets/bundled/prefabs/modding/volumes_and_triggers/monument_marker.prefab
			Puts("Getting safe Navmesh Agent params");
			
			BaseEntity baseEntity = GameManager.server.CreateEntity("assets/rust.ai/agents/wolf/wolf.prefab", Vector3.zero,Quaternion.LookRotation(Vector3.forward, Vector3.up), false);
			
			AgentProperties.Store(baseEntity.GetComponent<NavMeshAgent>());
			GameObject.Destroy(baseEntity.gameObject);
			
			
		}
		
		public class NavmeshAgentSwapFlag : BaseMonoBehaviour{
			public bool swapped = true;
			
		}
		private void OnEntitySpawned(HumanNPC hn){
			if(hn.GetComponent<NavmeshAgentSwapFlag>()){return;}
			BaseEntity baseEntity = GameManager.server.CreateEntity(hn.gameObject.name, hn.transform.position, hn.transform.rotation, false);
			AgentProperties.Set(baseEntity.GetComponent<NavMeshAgent>());
			baseEntity.gameObject.AddComponent<NavmeshAgentSwapFlag>();
			baseEntity.gameObject.AwakeFromInstantiate();
			baseEntity.Spawn();
			
			hn.Kill();
		}
		public void GetCustomAIPoints(){//
			GameObject[] all = UnityEngine.Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
			foreach(GameObject go in all){
				if(go.transform.name.Contains("assets/prefabs/npc/scientist/patrolpoint.prefab")){
					//Puts("Found point! " + go.transform.name);
					if(go.transform.localScale.y>0){

						AIMovePoint movepoint = go.GetComponent<AIMovePoint>();
						if(movepoint ==null){
							movepoint=go.AddComponent<AIMovePoint>();
							
							//Puts("Movepoint added!");
						}
						movepoint.WaitTime = (go.transform.localScale.y*1);
						Puts("Found MOVE point! " + movepoint.WaitTime + " " + go.transform.localScale.ToString());
						if(!customMovePoints.Contains(movepoint)){
							customMovePoints.Add(movepoint);
							//Puts("Added Movepoint to processing :");
						}
						customPoints.Add(movepoint);
					}
					else{
						
						AICoverPoint coverpoint = go.GetComponent<AICoverPoint>();
						Puts("Found COVER point! " + go.transform.localScale.ToString());
						if(coverpoint ==null){
							coverpoint=go.AddComponent<AICoverPoint>();
							//Puts("coverpoint added!");
						}
						if(!customCoverPoints.Contains(coverpoint)){
							customCoverPoints.Add(coverpoint);
							//Puts("Added coverpoint to processing");
						}
						customPoints.Add(coverpoint);
					}

				}
			}
			
		}
		
		class CustomAIZ : AIInformationZone{
			
		}
		
		//
		public void GetMonumentMarkers(){
			
			MonumentInfo[] Markers =  UnityEngine.Object.FindObjectsOfType(typeof(MonumentInfo)) as MonumentInfo[];
			AIInformationZone[] Zones = UnityEngine.Object.FindObjectsOfType(typeof(AIInformationZone)) as AIInformationZone[];
			Puts("Found "+Markers.Count()+" markers and "+Zones.Count()+" zones");
			foreach(MonumentInfo marker in Markers){	
			
				//Check if marker is already in a zone
				bool isInZone = false;
				CustomAIZ markerzone = marker.GetComponent<CustomAIZ>();
				foreach(AIInformationZone zone in Zones){
					if(markerzone!=null || (zone != markerzone && zone.bounds.Contains(marker.transform.position))){
						isInZone=true;
						Puts("Marker in zone");
						break;
					}
				}
				if(isInZone){continue;}
				Puts("Marker not in zone or self");
				
				//Ignore markers that aren't for AI otherwise add to custom monuments
				if(marker.transform.localScale == new Vector3(1,1,1)){continue;}
				if(!customMonuments.Contains(marker)){
					customMonuments.Add(marker);
					Puts("Marker added to custom monuments");//
				}
				
				//Vars we'll need
				Bounds monumentBounds = new Bounds(marker.transform.position,marker.transform.localScale);
				List<AIMovePoint> myMovePoints = new List<AIMovePoint>();
				
				//Parent points in bounds
				foreach(AIPoint point in customPoints){
					if(monumentBounds.Contains(point.transform.position)){
						Vector3 oldScale = point.transform.localScale;
						point.transform.SetParent(marker.transform);
						point.transform.localScale=oldScale;
						Puts("Point in bounds, parenting!:" + point.transform.localScale);
					}
					if(point is AIMovePoint){
						myMovePoints.Add(point as AIMovePoint);
					}
				}
				
				//Get or create AI Info zone, remove to readd on new start if existing 
				if(markerzone==null){	
					Puts("Adding new zone");
					markerzone = marker.gameObject.AddComponent<CustomAIZ>();
				}
				else{
					if(AIInformationZone.zones.Contains(markerzone)){
						AIInformationZone.zones.Remove(markerzone);
					}
				}
				//Make sure it has a grid
				AIInformationGrid newGrid = marker.gameObject.GetComponent<AIInformationGrid>();
				if(newGrid==null){
					newGrid = marker.gameObject.AddComponent<AIInformationGrid>();
				}
				markerzone.paths=getPaths(myMovePoints,marker);
				//Init zone
				markerzone.bounds=monumentBounds;
				markerzone.ShouldSleepAI=false;
				markerzone.Virtual=false;
				markerzone.Start();				
				
			}
		}
        [Command("ShowPoints")]
        void DemoMonumentSwap_cmd(IPlayer player, string command, string[] args)
        {
			foreach(AIPoint p in customPoints){
				global::ConsoleNetwork.BroadcastToAllClients("ddraw.sphere", new object[]
				{
					60,
					global::UnityEngine.Color.cyan,
					p.transform.position,
					1f
				});
			}
			foreach(AIMovePointPath path in allPaths){
				int i = 0;
				while(i<path.Points.Count()){
					int i2 = (i<path.Points.Count()-1?i+1:0);
					global::ConsoleNetwork.BroadcastToAllClients("ddraw.line", new object[]
					{
						60,
						global::UnityEngine.Color.green,
						path.Points[i].transform.position,
						path.Points[i2].transform.position
					});
					i++;
				}
			}
			
			
		}
		
		public List<AIMovePointPath> getPaths (List<AIMovePoint> allPoints, MonumentInfo marker){
			List<AIMovePoint> pointsToProcess = new List<AIMovePoint>(allPoints.ToArray());
			List<AIMovePoint> processedPoints = new List<AIMovePoint>();
			List<AIMovePointPath> result = new List<AIMovePointPath>();
			Dictionary<int,List<AIMovePoint>> Paths= new Dictionary<int,List<AIMovePoint>>();
			
			int maxSeek=allPoints.Count();
			
			foreach(AIMovePoint p in allPoints.ToArray()){
				int pPath = (int)Mathf.Round(p.transform.localScale.x*1000);
				if(!Paths.ContainsKey(pPath)){
					Paths.Add(pPath,new List<AIMovePoint>());
				}
				Paths[pPath].Add(p);
			}
			foreach(List<AIMovePoint> l in Paths.Values){
				if(l.Count()>0){
					Puts("Path"+l[0].transform.localScale.x+" has "+l.Count()+" points!");
					l.Sort(delegate(AIMovePoint x, AIMovePoint y)
						{
							return (x.transform.localScale.z>y.transform.localScale.z?
								1:-1);
						});
					
					AIMovePointPath path = l[0].gameObject.GetComponent<AIMovePointPath>();
					if(path==null){
						path = l[0].gameObject.AddComponent<AIMovePointPath>();
					}
					path.Points.Clear();
					path.Points.AddRange(l);
					result.Add(path);
				}
				
			}
			
			return result;
		}
		
	}
}
