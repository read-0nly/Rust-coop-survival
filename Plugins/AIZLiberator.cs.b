// Requires: Cordyceps
// Requires: Navmesher
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
#endregion
namespace Oxide.Plugins{
	[Info("AIZLiberator", "obsol", "0.2.1")]
	[Description("Replaces states to both make sure they don't run out of ammo (topup between states) and to allow NPCs to roam free of AIZ control")]
	public class AIZLiberator : RustPlugin{
		
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
			cordy.WalkableOnly = false;
			loadStates();
			
		}
		void loadStates(){
			foreach(string s in targets){
				CustomCombatState fcs= new CustomCombatState();
				CustomCombatStationaryState fcss= new CustomCombatStationaryState();
				CustomTakeCoverState fbfps= new CustomTakeCoverState();
				CustomChaseState fbrs= new CustomChaseState();
				CustomFollowPathState cfps= new CustomFollowPathState();
				CustomBaseRoamState cbrs= new CustomBaseRoamState();
				cordy.AssignHumanState(s, fcs.StateType, fcs);
				cordy.AssignHumanState(s, fcss.StateType, fcss);
				cordy.AssignHumanState(s, fbfps.StateType, fbfps);
				cordy.AssignHumanState(s, fbrs.StateType, fbrs);
				cordy.AssignHumanState(s, cfps.StateType, cfps);
				cordy.AssignHumanState(s, cbrs.StateType, cbrs);
			}			
		}
		
		public class CustomCombatState : ScientistBrain.CombatState{
			private StateStatus status = StateStatus.Error;
			
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
            {
                FaceTarget(entity);
                if (this.brain==null){return;}
				if(this.brain.Navigator==null){return;}
				if(this.brain.Navigator.BaseEntity==null){
					return;
				}		
				if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
				{
					(this.brain.GetBrainBaseEntity() as BasePlayer).Hurt(10f);
				}
				if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
					return;
				}	
				this.status = StateStatus.Running;	
				
				HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
				if (!(hn.inventory == null || hn.inventory.containerBelt == null))
				{
					Item slot = hn.inventory.containerBelt.GetSlot(0);
					if (slot != null)
					{
						hn.UpdateActiveItem(hn.inventory.containerBelt.GetSlot(0).uid);
						BaseEntity heldEntity = slot.GetHeldEntity();
						if (heldEntity != null)
						{
							AttackEntity component = heldEntity.GetComponent<AttackEntity>();
							if (component != null)
							{
								component.TopUpAmmo();
							}
						}
					}
				}
				this.combatStartPosition = entity.transform.position;
				base.StateEnter(brain,entity);
				
			}
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                base.StateThink(delta, brain, entity);
                HumanNPC humanNPC = entity as HumanNPC;
				if (Time.time > this.nextActionTime)
                {
                    if (UnityEngine.Random.Range(0, 3) == 1)
                    {
                        this.nextActionTime = Time.time + UnityEngine.Random.Range(1f, 2f);
                        humanNPC.SetDucked(true);
                    }
                    else
                    {
                        this.nextActionTime = Time.time + UnityEngine.Random.Range(2f, 3f);
                        humanNPC.SetDucked(false);
                    }
                    if (UnityEngine.Random.Range(0, 5) >0)
                    {
                        brain.Navigator.SetDestination(brain.PathFinder.GetRandomPositionAround(entity.transform.position, 2f, 3f), BaseNavigator.NavigationSpeed.Fast, 0f, 0f);
                    }
                    else
                    {
                        brain.Navigator.SetDestination(brain.PathFinder.GetRandomPositionAround(this.combatStartPosition, 1f, 2f), BaseNavigator.NavigationSpeed.Normal, 0f, 0f);
                    }
                }
                FaceTarget(entity);
                return StateStatus.Running;
			}
			// Token: 0x04001529 RID: 5417
			private float nextActionTime;

			// Token: 0x0400152A RID: 5418
			private Vector3 combatStartPosition;
			private void FaceTarget(BaseEntity me)
			{
                BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				
				if (baseEntity == null || baseEntity.ShortPrefabName==me.ShortPrefabName)
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
					this.brain.Events.Memory.Entity.Set(null,this.brain.Events.CurrentInputMemorySlot);
					return;
				}
				this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
			}
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity){
				
				(entity as HumanNPC).SetDucked(false);
				brain.Navigator.ClearFacingDirectionOverride();
				base.StateLeave(brain,entity);
			}
		}
		public class CustomCombatStationaryState : ScientistBrain.CombatStationaryState{
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity){
				if(this.brain.Navigator.BaseEntity==null){
					base.StateEnter(brain,entity);					return;
				}		
				if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
					base.StateEnter(brain,entity);					return;
				}		
				HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
				if (hn.inventory == null || hn.inventory.containerBelt == null)
				{
					base.StateEnter(brain,entity);
				}
				
				Item slot = hn.inventory.containerBelt.GetSlot(0);
				
				if (slot != null)
				{
					hn.UpdateActiveItem(hn.inventory.containerBelt.GetSlot(0).uid);
					BaseEntity heldEntity = slot.GetHeldEntity();
					if (heldEntity != null)
					{
						AttackEntity component = heldEntity.GetComponent<AttackEntity>();
						if (component != null)
						{
							component.TopUpAmmo();
						}
					}
				}
				base.StateEnter(brain,entity);
			}
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity){
				
				//this.brain.Navigator.ClearFacingDirectionOverride();
				base.StateLeave(brain,entity);
			}
		}
		public class CustomTakeCoverState : ScientistBrain.TakeCoverState{
			private StateStatus status = StateStatus.Error;
			private BaseEntity coverFromEntity;
			private BaseEntity secondEntity;
			private float timeInState = 0.0f;
			private bool preference = UnityEngine.Random.Range(0, 2) == 0;
            private List<Vector3Int> safePositions = new List<Vector3Int>();
			private void FaceTarget()
			{
                secondEntity = coverFromEntity;
                coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (coverFromEntity == null)
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
					return;
				}
				this.brain.Navigator.SetFacingDirectionEntity(coverFromEntity);
			}
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
            {
                preference = UnityEngine.Random.Range(0, 2) == 0;
                if (this.brain==null){return;}
				if(this.brain.Navigator==null){return;}
				
				if(this.brain.Navigator.BaseEntity==null){
					return;
				}		
				if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
					return;
				}
               
                HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
				if (!(hn.inventory == null || hn.inventory.containerBelt == null))
				{
					Item slot = hn.inventory.containerBelt.GetSlot(0);
					if (slot != null)
					{
						hn.UpdateActiveItem(hn.inventory.containerBelt.GetSlot(0).uid);
						BaseEntity heldEntity = slot.GetHeldEntity();
						if (heldEntity != null)
						{
							AttackEntity component = heldEntity.GetComponent<AttackEntity>();
							if (component != null)
							{
								component.TopUpAmmo();
							}
						}
					}
				}
				this.status = StateStatus.Running;
				if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
					if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
						RadialPoint(this.brain.Navigator, this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);
                        FaceTarget();
                        return;
					}
				}
				Vector3 secondDir =  (secondEntity==null || secondEntity!=coverFromEntity?new Vector3(0,0,0): (secondEntity.transform.position-this.brain.transform.position).normalized);
				Vector3 coverDir = new Vector3(0, 0, 0);
                Vector3 safePosition = new Vector3(0, 0, 0);
                float safeDistance = -1;

                foreach (Vector3 v  in safePositions)
				{
					float dist = Vector3.Distance(v, hn.transform.position);

					if (dist < 1 || dist > 10) continue;
					if (safeDistance == -1 || dist < safeDistance)
					{
						coverDir += (v - hn.transform.position).normalized / dist;
                        safeDistance = dist;
						safePosition = v;
					}
                }
                Vector3 finalDir = new Vector3(0, 0, 0);
                Vector3 attackDir = (hn.LastAttackedDir.normalized * -4)+(secondDir.normalized*-3)+new Vector3(UnityEngine.Random.Range(-2.0f, 2f),0, UnityEngine.Random.Range(-2.0f, 2f));
                Vector3 sideDir = new Vector3(0, 0, 0);
                if (safeDistance>-1)
				{
					coverDir = coverDir.normalized;
				}
				else
                {
                    coverDir=new Vector3(0,0,0);
                    sideDir += (preference ? hn.eyes.transform.right * UnityEngine.Random.Range(1.0f,3.0f) : hn.eyes.transform.right * -1 * UnityEngine.Random.Range(1.0f, 3.0f));
                }

				if (hn.lastAttacker==null||(hn.lastDamage!=Rust.DamageType.Generic))
					if (safeDistance == -1) {
						RadialPoint(this.brain.Navigator, this.brain.transform.position + (attackDir.normalized * Mathf.Min(0.5f/(hn.healthFraction),10))+(sideDir* Mathf.Min(hn.healthFraction*5,1))+(coverDir), this.brain.transform.position, 2f, 2); 
					}
					else
                    {
                        RadialPoint(this.brain.Navigator, safePosition, this.brain.transform.position, 1f, 2);
                    }
				else
						RadialPoint(this.brain.Navigator, this.brain.transform.position + ((-1* attackDir.normalized) * 2), this.brain.transform.position);

                FaceTarget();
                return;
			}
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                FaceTarget();
                timeInState +=delta;
				Vector3 v1 = brain.Navigator.Destination;
				Vector3 v2 = entity.transform.position;
				v1.y=0;v2.y=0;
				if(Vector3.Distance(v1,v2)>1 || timeInState<5){
					return StateStatus.Running;
				}else{
					timeInState=0;
					return StateStatus.Finished;					
				}
			}
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{
				this.brain.Navigator.ClearFacingDirectionOverride();
				ClearCoverPointUsage();
				BasePlayer bp = entity as BasePlayer;
				if (bp == null) return;
				if (coverFromEntity == null) return;
                Vector3Int positionInt = new Vector3Int((int)this.brain.transform.position.x, (int)this.brain.transform.position.y, (int)this.brain.transform.position.z);
                BaseEntity secondEntity = this.brain.Events.Memory.Entity.Get(0);
                Vector3 secondDir = (secondEntity == null && secondEntity != coverFromEntity ? new Vector3(0, 0, 0) : (secondEntity.transform.position - this.brain.transform.position).normalized);
                if ((secondEntity == null||!CanSeeTarget(bp,coverFromEntity)) && (secondEntity == null || !CanSeeTarget(bp, secondEntity)))
                {
                    safePositions.Insert(0, positionInt);
                    while (safePositions.Count() > 10)
                    {
                        safePositions.RemoveAt(10);
                    }
                }
                else
                {
                    if (safePositions.Contains(positionInt))
                    {
                        safePositions.Remove(positionInt);
                    }
                }
                return;
				
			}
			private void ClearCoverPointUsage()
			{
                AIPoint aipoint = this.brain.Events.Memory.AIPoint.Get(4);
				if (aipoint != null)
				{
					aipoint.ClearIfUsedBy(brain.GetBrainBaseEntity());
			
				}
			}
			private void FaceCoverFromEntity()
			{
				this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (this.coverFromEntity == null)
				{
					return;
				}
				this.brain.Navigator.SetFacingDirectionEntity(this.coverFromEntity);
			}
			
		}		
		public class CustomChaseState : BaseAIBrain.BaseChaseState{
			Vector3 LastVector = new Vector3(0,-501,0);
			Vector3 Target = new Vector3(0,0,0);
			Vector3 Root = new Vector3(0,0,0);
			Vector3 Direction = new Vector3(0,0,0);
			float LastWet = 0f;
			float startTime = 0;
			bool TurnDirection = false;
			bool hasWarped = false;
			BaseNavigator.NavigationSpeed Speed = BaseNavigator.NavigationSpeed.Normal;
			public CustomChaseState() : base()
			{
				base.AgrresiveState = true;
				TurnDirection = (Oxide.Core.Random.Range(0, 2) == 1);
				startTime = Time.realtimeSinceStartup;
			}

			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{
				base.StateLeave(brain,entity);
				this.brain.Navigator.ClearFacingDirectionOverride();
				this.Stop();
			}

			public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
			{
				base.StateEnter(brain,entity);
                FaceTarget(entity);
                if (this.brain==null){return;}
				if(this.brain.Navigator==null){return;}
				if(this.brain.transform==null){return;}
				if(this.brain.Navigator.BaseEntity==null){
					return;
				}		
				if(!(this.brain.Navigator.BaseEntity as HumanNPC)==null){
					return;
				}		
				
				
				HumanNPC hn = (this.brain.Navigator.BaseEntity as HumanNPC);
				if (!(hn.inventory == null || hn.inventory.containerBelt == null))
				{
					Item slot = hn.inventory.containerBelt.GetSlot(0);
					if (slot != null)
					{
						hn.UpdateActiveItem(hn.inventory.containerBelt.GetSlot(0).uid);
						BaseEntity heldEntity = slot.GetHeldEntity();
						if (heldEntity != null)
						{
							AttackEntity component = heldEntity.GetComponent<AttackEntity>();
							if (component != null)
							{
								component.TopUpAmmo();
							}
						}
					}
				}
				
			
				this.status = StateStatus.Error;
				if (this.brain.PathFinder == null)
				{
					return;
				}
						
				if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
				{
					(this.brain.GetBrainBaseEntity() as BasePlayer).Hurt(10f);
				}
				this.status = StateStatus.Running;
				this.nextPositionUpdateTime = 0f;
                BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				Vector3 target = this.brain.transform.position;
				//
				if (baseEntity != null)
				{
					this.brain.Navigator.SetDestination(this.brain.PathFinder.GetRandomPositionAround(baseEntity.transform.position,5f, 4f), BaseNavigator.NavigationSpeed.Fast, 1f, 0f);
				}
			}

            private void FaceTarget(BaseEntity me)
            {
                BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);

                if (baseEntity == null || baseEntity.ShortPrefabName == me.ShortPrefabName)
                {
                    this.brain.Navigator.ClearFacingDirectionOverride();
                    this.brain.Events.Memory.Entity.Set(null, this.brain.Events.CurrentInputMemorySlot);
                    return;
                }
                this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
            }
            private void Stop()
			{
				try{
				this.brain.Navigator.Stop();
				this.brain.Navigator.ClearFacingDirectionOverride();
				}catch(Exception E){
					UnityEngine.Debug.Log(this.brain.Navigator.CurrentNavigationType.ToString());
				}
			}

			public override StateStatus StateThink(float delta,BaseAIBrain brain, BaseEntity entity)
            {
                FaceTarget(entity);
                BaseEntity baseEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
				if (baseEntity == null)
				{
					return StateStatus.Error;//
				}
				float num = Vector3.Distance(entity.transform.position,baseEntity.transform.position);
				if (this.brain.Senses.Memory.IsLOS(baseEntity) || num <= 10f)
				{
					this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
				}
				else
				{
					this.brain.Navigator.ClearFacingDirectionOverride();
				}
				if (num <= 10f)
				{
					this.brain.Navigator.SetCurrentSpeed(BaseNavigator.NavigationSpeed.Normal);
				}
				else//
				{
					this.brain.Navigator.SetCurrentSpeed(BaseNavigator.NavigationSpeed.Fast);
				}
				if (Time.time > this.nextPositionUpdateTime)
				{
					this.nextPositionUpdateTime = Time.time + UnityEngine.Random.Range(3f, 5f);			
					
					this.status = StateStatus.Running;
					float currentWet = WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false);
					if(currentWet > 0f)	{
						if (currentWet > 0.8f){
								(this.brain.GetBrainBaseEntity() as BasePlayer).Hurt(10f);
						}
						Root = LastVector;
						Direction = (TurnDirection?this.brain.transform.right:(-1*this.brain.transform.right));
						Target = this.brain.PathFinder.GetRandomPositionAround(Root-(this.brain.transform.forward*3)+(Direction*3),7f, 10f);
						if(Vector3.Distance(Target,this.brain.transform.position)<5)
							Speed = BaseNavigator.NavigationSpeed.Slow;
						if(Vector3.Distance(Target,this.brain.transform.position)>15)
							Speed = BaseNavigator.NavigationSpeed.Fast;
						
						if (this.brain.Navigator.SetDestination(Target, Speed, 0f, 0f))
						{
							this.status = StateStatus.Running;
							return this.status;
						}
						else{
							status = StateStatus.Error;
							if(!hasWarped){
								this.brain.Navigator.Warp(this.brain.transform.position);
								hasWarped=true;
							}
						}
					
					}else{
						LastWet=currentWet;
						LastVector=this.brain.transform.position;				
					}
				}
				if (this.brain.Navigator.Moving && base.TimeInState <= 10f)
				{
					return StateStatus.Running;
				}
				//this.brain.Navigator.ClearFacingDirectionOverride();
				return StateStatus.Finished;
				
				
			}

			private StateStatus status = StateStatus.Error;

			private float nextPositionUpdateTime;
		}
		public class CustomFollowPathState : BaseAIBrain.BasicAIState{
			public StateStatus status = StateStatus.Error;
			// Token: 0x040001DF RID: 479
			public AIMovePointPath path;
			// Token: 0x040001E1 RID: 481
			public AIMovePoint currentTargetPoint;
			// Token: 0x040001E2 RID: 482
			public float currentWaitTime;
			public Vector3 lastLocation = new Vector3(0,0,0);
			public float waitTime = 15;
			public float defaultWaitTime=15;
			public int stuckCount = 0;
			// Token: 0x040001E3 RID: 483
			public AIMovePointPath.PathDirection pathDirection;
			public int currentNodeIndex;
			public CustomFollowPathState() : base(AIState.FollowPath){
				
			}
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity){
				this.brain.Navigator.ClearFacingDirectionOverride();
				int i=0;
				this.currentWaitTime =0;
				
				if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
				{
					(entity as BasePlayer).Hurt(10f);
				}
				if(this.brain==null){
					return;
				}		
				if(this.brain.Navigator==null){
					return;}
				if(this.brain.Navigator.BaseEntity==null){
					return;
				}		
				lastLocation = this.brain.Navigator.BaseEntity.gameObject.transform.position;
				
				if(this.brain.Navigator.BaseEntity.creatorEntity){
					if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
						RadialPoint(this.brain.Navigator,this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);						
						return ;
					}
				}
				
				this.brain.Navigator.Path=null;
				
				if(waitTime==defaultWaitTime){RadialPoint(this.brain.Navigator, this.brain.transform.position+(this.brain.transform.forward*10),this.brain.transform.position,5,7);}
				status= StateStatus.Running;
				return;
			}
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
			{
				this.currentWaitTime += delta;
				if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
					if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
						if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 10f || this.currentWaitTime >= 60f)
						{//
							return StateStatus.Finished;
						}else{
							return StateStatus.Running;
						}
					}
				}
				if (this.currentWaitTime >= waitTime)
				{//
					if(Vector3.Distance(lastLocation,this.brain.Navigator.BaseEntity.gameObject.transform.position)<2){
						stuckCount++;
					};
					this.currentWaitTime = 0;

                    RadialPoint(this.brain.Navigator, this.brain.transform.position+((UnityEngine.Random.Range(0.0f,1.0f)>0.5f?this.brain.transform.right:new Vector3(0,0,0)-this.brain.transform.right)*5),this.brain.transform.position);
					return StateStatus.Finished;
				}else{
					return StateStatus.Running;
				}
				
			}
			
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{
				waitTime=defaultWaitTime;
			}
			
		}
		public class CustomBaseRoamState : BaseAIBrain.BaseRoamState{
			public StateStatus status = StateStatus.Error;
			public AIMovePointPath path;
			public AIMovePoint currentTargetPoint;
			public float currentWaitTime;
			public Vector3 lastLocation = new Vector3(0,0,0);
			public float waitTime = 5;
			public float defaultWaitTime=5;
			public int stuckCount = 0;
			public int currentNodeIndex;
			public CustomBaseRoamState() : base(){
			}
			public override void StateEnter(BaseAIBrain brain, BaseEntity entity){
				this.brain.Navigator.ClearFacingDirectionOverride();
				int i=0;
				this.currentWaitTime =0;
				
				if (WaterLevel.GetOverallWaterDepth(this.brain.transform.position, true, null, false) > 0.8f)
				{
					(entity as BasePlayer).Hurt(10f);
				}
				if(this.brain==null){
					return;
				}		
				if(this.brain.Navigator==null){
					return;}
				if(this.brain.Navigator.BaseEntity==null){
					return;
				}		
				lastLocation = this.brain.Navigator.BaseEntity.gameObject.transform.position;
				
				if(this.brain.Navigator.BaseEntity.creatorEntity){
					if(this.brain.Navigator.BaseEntity.creatorEntity.transform){
						RadialPoint(this.brain.Navigator,this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position);						
						return ;
					}
				}
				
				this.brain.Navigator.Path=null;
				
				if(waitTime==defaultWaitTime){RadialPoint(this.brain.Navigator, this.brain.transform.position+(this.brain.transform.forward*10),this.brain.transform.position,5,7);}
				status= StateStatus.Running;
				return;
			
			}
			public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
			{
				this.currentWaitTime += delta;
				if(!(this.brain.Navigator.BaseEntity.creatorEntity==null)){
					if(!(this.brain.Navigator.BaseEntity.creatorEntity.transform==null)){
						if (Vector3.Distance(this.brain.Navigator.BaseEntity.creatorEntity.transform.position,this.brain.transform.position) > 10f || this.currentWaitTime >= 60f)
						{//
							return StateStatus.Finished;
						}else{
							return StateStatus.Running;
						}
					}
				}
				if (this.currentWaitTime >= waitTime)
				{//
					return StateStatus.Finished;
				}else{
					return StateStatus.Running;
				}
				
			}
			
			public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
			{ 
				waitTime=defaultWaitTime;
			}
			
		}

        static bool RadialPoint(BaseNavigator nav, Vector3 target, Vector3 self, float minDist = 5, float maxDist = 8)
        {
            bool destRes = false;
            float dist = UnityEngine.Random.Range(minDist, maxDist);
            float angle = (180 + nav.transform.eulerAngles.y) + UnityEngine.Random.Range(-60f, 60f);
            float x = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
            Vector3 newPosition = target;
            newPosition.x += x;
            newPosition.z += y;
            newPosition.y = target.y;
            UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
            NavMesh.SamplePosition(newPosition, out nmh, 20, (int)(0xffffff));
            newPosition = nmh.position;
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
            newPosition.y = target.y;
            UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
            NavMesh.SamplePosition(newPosition, out nmh, 20, (int)(0xffffff));
            newPosition = nmh.position;
            outvect = newPosition;
            return true;

        }
        object OnStuck(BaseNavigator bn){
					Vector3 warpPoint = bn.BaseEntity.transform.position;
					RadialPoint(out warpPoint, warpPoint, warpPoint, 5, 5);

                    if (!(bn.Warp(warpPoint)))
						bn.BaseEntity.Kill();
					return warpPoint;
		}
		public static bool CanSeeTarget(BasePlayer observer,BaseEntity entity)
		{
			return !(entity == null) && entity.IsVisible(observer.eyes.position, entity.CenterPoint(), 70);
		}
	}
}
		//