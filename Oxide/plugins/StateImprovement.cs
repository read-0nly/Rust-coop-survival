// Requires: Cordyceps
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
	using Newtonsoft.Json;//

namespace Oxide.Plugins
{
	[Info("StateImprovement", "obsol", "0.0.1")]
	[Description("For breaking monuments")]
	public class StateImprovement : CovalencePlugin
	{
		
		#region config
		public ConfigData config;
		public static float findNextZone = 900;
		public class ConfigData
		{
			
			[JsonProperty("findNextZone", ObjectCreationHandling = ObjectCreationHandling.Replace)]			
			public float findNextZone = 900;
			
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
			StateImprovement.findNextZone=config.findNextZone;
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
		
		#region States
		public class CustomFollowPathState : BaseAIBrain.BaseFollowPathState
		{
			float TimeOnPath = 0f;
			public static float TimeLimit = 300;
			public static float TimeLimitMax = 600;
			HumanNPC self = null;
			public Vector3 lastPosition= new Vector3(0,0,0);
			public float timeAtPoint = 0;
			public static float stuckLimit = 5;
			private AIMovePointPath path;
			private StateStatus status;
			private AIMovePoint currentTargetPoint;
			private float currentWaitTime;
			private float currentLookTime = 0;
			private AIMovePointPath.PathDirection pathDirection;
			private int currentNodeIndex;
			
			public static bool debugMode = false;
			public static BaseAIBrain debugEntity = null;
			
			public void debugState(string text, BaseAIBrain entity){
				if(debugMode){
					if(debugEntity==null){
						debugEntity=entity;
					}
					if(debugEntity!=entity) return;
					System.Console.ForegroundColor = ConsoleColor.Red;
					System.Console.WriteLine(text);
					System.Console.ResetColor();
					
				}
			}
			
			public CustomFollowPathState(){
			}
			
			public void setNextPath(BaseAIBrain brain, BaseEntity entity){
				
				// Get zone
				AIInformationZone forPoint = self.GetInformationZone(entity.transform.position);
				AIMovePointPath best = brain.Navigator.Path;
				
				// If there's a zone and paths, check for best path
				if(forPoint!=null||forPoint.paths.Count()>=0){					
					
					
					// Find best path you can reach
					foreach (AIMovePointPath aizPath in forPoint.paths)
					{
						if(best== brain.Navigator.Path || Vector3.Distance(aizPath.transform.position,entity.transform.position) < Vector3.Distance(best.transform.position,entity.transform.position))
						{
							NavMeshPath navMeshPath = new NavMeshPath();
							if(NavMesh.CalculatePath(entity.transform.position, aizPath.transform.position, 25, navMeshPath)){
								if(navMeshPath.status == NavMeshPathStatus.PathComplete)
								{
									best=aizPath;
								}
							}
							else{
								brain.Navigator.Agent.Warp(entity.transform.position);
							}
						}
					}
				}
				// If same path just flip
				if(best==brain.Navigator.Path){
					this.pathDirection = (this.pathDirection==AIMovePointPath.PathDirection.Forwards?AIMovePointPath.PathDirection.Backwards:AIMovePointPath.PathDirection.Forwards);
					System.Console.ForegroundColor = ConsoleColor.Cyan;
					System.Console.WriteLine("Flipping path");
					System.Console.ResetColor();
				}else{
					System.Console.ForegroundColor = ConsoleColor.Cyan;
					System.Console.WriteLine("Changing path");
					System.Console.ResetColor();
				}
				
				brain.Navigator.Path = best;
				
				
			
			}
			
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{
				base.StateLeave(brain, entity);
				//If we've been on path too long, find best path
				if(TimeOnPath >  TimeLimit){
					TimeOnPath=0f;
					setNextPath(brain,entity);
				}
				if(currentTargetPoint!=null){
					currentTargetPoint.ClearIfUsedBy(entity);
				}
				pathDirection = UnityEngine.Random.Range(0f,1f)>0.5f?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards;
			}

			public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
			{
				debugState("Start Follow Path",brain);
				if(self==null) 
					self= entity as HumanNPC;
					
				timeAtPoint=0;
				lastPosition = entity.transform.position;
				
				this.status = StateStatus.Error;
				brain.Navigator.SetBrakingEnabled(false);
				
				//Set the path
				this.path = brain.Navigator.Path;
				if (this.path == null)
				{
					AIInformationZone forPoint = AIInformationZone.GetForPoint(entity.ServerPosition, true);
					if (forPoint == null)
					{
						return;
					}
					this.path = brain.Navigator.Path = forPoint.GetNearestPath(entity.ServerPosition);
					if (this.path == null)
					{
						return;
					}
				}
				//Find a point
				this.currentNodeIndex = this.path.FindNearestPointIndex(entity.ServerPosition);
				this.currentTargetPoint = this.path.FindNearestPoint(entity.ServerPosition);
				if (this.currentTargetPoint == null)
				{
					return;
				}
				//Get next index
				currentTargetPoint.ClearIfUsedBy(entity);
				
				int i = 0;
				//While next point is busy, pick next next point (up to path length)
				do{
					//Get next index
					
					this.currentNodeIndex = this.path.GetNextPointIndex(this.currentNodeIndex, ref this.pathDirection);
					this.currentTargetPoint = this.path.GetPointAtIndex(this.currentNodeIndex);
					i++;
				}while(i<this.path.Points.Count() && this.currentTargetPoint.InUse());
				
				currentTargetPoint.SetUsedBy(entity,20f);
				this.status = StateStatus.Running;
				
				// figure out necessary speed
				this.currentWaitTime = 0f;
				float distance = Vector3.Distance(this.currentTargetPoint.transform.position,entity.ServerPosition);
				BaseNavigator.NavigationSpeed speed = BaseNavigator.NavigationSpeed.Slowest;
				if(distance < 25){
					speed = BaseNavigator.NavigationSpeed.Slow;
				}
				else if(distance < 50){
					speed = BaseNavigator.NavigationSpeed.Normal;
				}
				else{
					speed = BaseNavigator.NavigationSpeed.Fast;
				}
				
				// Lock for self and head to point
				currentTargetPoint.SetUsedBy(entity,20f);
				brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, speed, 0f, 0f);
			}
			
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
			{
				/*
				debugState("Thinking",brain);				
				if (this.status == global::StateStatus.Error)
				{
					debugState("  Error return",brain);
					return this.status;
				}
				if (!brain.Navigator.Moving)
				{
					debugState("  Stuck or moving",brain);
					if (this.currentWaitTime <= 0f && this.currentTargetPoint.HasLookAtPoints())
					{
						debugState("    Look At Point",brain);
						global::UnityEngine.Transform randomLookAtPoint = this.currentTargetPoint.GetRandomLookAtPoint();
						if (randomLookAtPoint != null)
						{
							brain.Navigator.SetFacingDirectionOverride(global::UnityEngine.Vector3Ex.Direction2D(randomLookAtPoint.transform.position, entity.ServerPosition));
						}
					}
					if (this.currentTargetPoint.WaitTime > 0f)
					{
						this.currentWaitTime += delta;
					}
					if (this.currentTargetPoint.WaitTime <= 0f || this.currentWaitTime >= this.currentTargetPoint.WaitTime)
					{
						debugState("    Done waiting at point",brain);
						brain.Navigator.ClearFacingDirectionOverride();
						this.currentWaitTime = 0f;
						int num = this.currentNodeIndex;
						this.currentNodeIndex = this.path.GetNextPointIndex(this.currentNodeIndex, ref this.pathDirection);
						this.currentTargetPoint = this.path.GetPointAtIndex(this.currentNodeIndex);
						if ((!(this.currentTargetPoint != null) || this.currentNodeIndex != num) && (this.currentTargetPoint == null || !brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, global::BaseNavigator.NavigationSpeed.Slow, 0f, 0f)))
						{
							debugState("      Error pathing in think - Target point: "+(this.currentTargetPoint == null).ToString()+" \n      Target Index and old : "+this.currentNodeIndex.ToString()+":"+num.ToString(), brain);
							return global::StateStatus.Error;
						}
					}
				}
				else if (this.currentTargetPoint != null)
				{
					debugState("  Goto point",brain);
					brain.Navigator.SetDestination(this.currentTargetPoint.transform.position, global::BaseNavigator.NavigationSpeed.Slow, 1f, 0f);
				}
				return global::StateStatus.Running;
				/*/
				
				debugState("Thinking",brain);
				TimeOnPath+=delta;
				currentLookTime+=delta;
				
				// If position changed, reset stuck time, otherwise increment
				if(entity.transform.position!=lastPosition){
					timeAtPoint=0;
				}
				else{
					timeAtPoint+=delta;
				}
				if (this.status == StateStatus.Error)
				{
					this.currentWaitTime = 0f;
					return this.status;
				}
				
				// If we're over the stuck limit we're stuck
				bool isStuck=timeAtPoint>stuckLimit;
				
				// If we're done moving or we're stuck
				if (!brain.Navigator.Moving||isStuck)
				{
					debugState("Stuck or not moving",brain);
					//If we're stuck flip direction
					if(isStuck){
						debugState("We're stuck",brain);
						pathDirection = UnityEngine.Random.Range(0f,1f)>0.5f?AIMovePointPath.PathDirection.Forwards:AIMovePointPath.PathDirection.Backwards;
						//Reset wait time
						this.currentWaitTime = 0f;
						return StateStatus.Error;
					}
					
					// If the point has lookat points, look at them
					if (this.currentWaitTime <= 0f && this.currentTargetPoint.HasLookAtPoints())
					{
						debugState("Look At Point",brain);
						UnityEngine.Transform randomLookAtPoint = this.currentTargetPoint.GetRandomLookAtPoint();
						if (randomLookAtPoint != null)
						{
							brain.Navigator.SetFacingDirectionOverride(UnityEngine.Vector3Ex.Direction2D(randomLookAtPoint.transform.position, entity.ServerPosition));
						}
					} 
					
					// If not, look at a sorta forward location
					else if(currentLookTime>10){
						
						debugState("Random look",brain);
						currentLookTime=0;
						float x=UnityEngine.Random.Range(-1.5f,1.5f);
						float y=UnityEngine.Random.Range(-1.5f,1.5f);
						brain.Navigator.SetFacingDirectionOverride(UnityEngine.Vector3Ex.Direction2D(entity.transform.position+entity.transform.forward+new Vector3(x,0,y), entity.ServerPosition));
					}
					
					//  Increment wait time
					this.currentWaitTime += delta;
					
					// If it`s time to change path, return finished
					if(TimeOnPath >  TimeLimit){
						debugState("Done with path, finishing",brain);
						brain.Navigator.ClearFacingDirectionOverride();
						return StateStatus.Finished;
					}
					
					// If point has no wait time or we're over the wait time
					if (this.currentTargetPoint.WaitTime <= 0f || this.currentWaitTime >= this.currentTargetPoint.WaitTime)
					{
						debugState("Done waiting at point",brain);
						//Allow freelook
						brain.Navigator.ClearFacingDirectionOverride();
						
						//Reset wait time
						this.currentWaitTime = 0f;
						
						//Take note of current index
						int num = this.currentNodeIndex;
						
						currentTargetPoint.ClearIfUsedBy(entity);
						
						return StateStatus.Finished;
					}else{
						currentTargetPoint.SetUsedBy(entity,20f);						
					}
				}
				// If still moving and there's a point, move to point
				else if (this.currentTargetPoint != null)
				{
					currentTargetPoint.SetUsedBy(entity,20f);
					debugState("Continuing Path",brain);
				}
				// Note last positino and keep running
				
				debugState("Still running at "+entity.transform.position.ToString(),brain);
				lastPosition=entity.transform.position;
				return StateStatus.Running;
				//*/
			}
				


		}

		public class CustomTakeCoverState : ScientistBrain.TakeCoverState{
			
			public Vector3 lastPosition= new Vector3(0,0,0);
			public float timeAtPoint = 0;
			public static float stuckLimit = 5;
			
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
			{
				lastPosition=entity.transform.position;
				timeAtPoint = 0;	
				this.status = StateStatus.Running;
				if (!this.StartMovingToCover(entity as HumanNPC))
				{
					this.status = StateStatus.Error;
				}
				
			}
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
			{
				if(lastPosition==entity.transform.position){
					timeAtPoint+=delta;
					if(timeAtPoint>stuckLimit){
						return StateStatus.Finished;
					}
				}
				lastPosition=entity.transform.position;
				timeAtPoint = 0;
				
				if (brain.Navigator.Moving)
				{
					FaceTarget();
					return StateStatus.Running;
				}
				return StateStatus.Finished;
			}
			private void FaceCoverFromEntity()
			{
				this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (this.coverFromEntity == null)
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
					return;
				}
				this.brain.Navigator.SetFacingDirectionEntity(this.coverFromEntity);
			}
			private void FaceTarget()
			{
				BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (baseEntity == null)
				{
					FaceCoverFromEntity();
					return;
				}
				this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
			}
			private bool StartMovingToCover(HumanNPC entity)
			{
				this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (this.coverFromEntity == null)
				{
					return false;
				}
				UnityEngine.Vector3 vector = (this.coverFromEntity ? this.coverFromEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 30f));
				AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
				if (informationZone == null)
				{
					return false;
				}
				float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
				float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
				AICoverPoint bestCoverPoint = informationZone.GetBestCoverPoint(entity.transform.position, vector, num, bestCoverPointMaxDistance, entity, true);
				if (bestCoverPoint == null)
				{
					return false;
				}
				UnityEngine.Vector3 position = bestCoverPoint.transform.position;
				if (!this.brain.Navigator.SetDestination(position, BaseNavigator.NavigationSpeed.Normal, 0f, 0f))
				{
					return false;
				}
				this.FaceCoverFromEntity();
				this.brain.Events.Memory.AIPoint.Set(bestCoverPoint, 4);
				bestCoverPoint.SetUsedBy(entity);
				return true;
			}
			private StateStatus status = StateStatus.Error;
			private BaseEntity coverFromEntity;
	
		}	
		#endregion
		
		#region Load
		
		Cordyceps cordy;

		public static string[] targets= new string[]{"assets/rust.ai/agents/npcplayer/humannpc/banditguard/npc_bandit_guard.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_mp5.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_pistol.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_shotgun.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_peacekeeper.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_excavator.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roamtethered.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_patrol.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/tunneldweller/npc_tunneldweller.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/underwaterdweller/npc_underwaterdweller.prefab"};

        void Loaded(){
			
			cordy = (Cordyceps)Manager.GetPlugin("Cordyceps");
			loadStates();
			
		}
		void loadStates(){
			foreach(string s in targets){
				CustomTakeCoverState tcs= new CustomTakeCoverState();
				cordy.AssignHumanState(s, tcs.StateType, tcs);
				CustomFollowPathState fps= new CustomFollowPathState();
				cordy.AssignHumanState(s, fps.StateType, fps);
			}			
		}
		
		#endregion
		
	}
}