public class CustomNPC : ScientistNPC{
	//this.Stats.Family = FamilyEnum.[ Bear | Wolf | Deer | Boar | Chicken | Horse | Zombie | Scientist | Murderer | Player ]
	public bool IsThreat(BaseEntity entity)
	{
		BaseNpc baseNpc = entity as BaseNpc;
		if ((UnityEngine.Object) baseNpc != (UnityEngine.Object) null)
			return baseNpc.Stats.Family != this.Stats.Family && this.IsAfraidOf(baseNpc.Stats.Family);
		BasePlayer basePlayer = entity as BasePlayer;
		return (UnityEngine.Object) basePlayer != (UnityEngine.Object) null && this.IsAfraidOf(basePlayer.Family);
	}

	public bool IsTarget(BaseEntity entity)
	{
		BaseNpc baseNpc = entity as BaseNpc;
		return (!((UnityEngine.Object) baseNpc != (UnityEngine.Object) null) || baseNpc.Stats.Family != this.Stats.Family) && !this.IsThreat(entity);
	}

	public bool IsFriendly(BaseEntity entity) => !((UnityEngine.Object) entity == (UnityEngine.Object) null) && (int) entity.prefabID == (int) this.prefabID;
	
	public static void SpawnInitial(Vector3 pos, Quaternion rot, string prefabPath, Type npcComponent, type brainComponent){
		this.fillOnSpawn = this.shouldFillOnSpawn;
		if (this.WaitingForNavMesh())
			this.Invoke(new Action(this.LateSpawn), 10f);
		else
			if (prefab != null && !string.IsNullOrEmpty(prefab.guid))
			{
				if ((bool) (UnityEngine.Object) spawnPoint)
				{
					BaseEntity entity = GameManager.server.CreateEntity(prefabPath, pos, rot, false);
					
					entity.gameObject.RemoveComponent<npcComponent>();
					entity.gameObject.RemoveComponent<brainComponent>();					
					CustomBrain cb = entity.gameObject.AddComponent(new CustomBrain());
					CustomNPC cn = entity.gameObject.AddComponent(new CustomNPC());
					cn.Stats.Family
					if ((bool) (UnityEngine.Object) entity)
					{
						entity.gameObject.AwakeFromInstantiate();
						entity.Spawn();
					}
				}
			}
	}
}

public class CustomBrain : ScientistBrain
{
  public static int Count;

  public override void AddStates()
  {
    base.AddStates();
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new BaseAIBrain<HumanNPC>.BaseIdleState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.RoamState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.ChaseState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.CombatState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.TakeCoverState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.CoverState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.MountedState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.DismountedState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new BaseAIBrain<HumanNPC>.BaseFollowPathState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new BaseAIBrain<HumanNPC>.BaseNavigateHomeState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new ScientistBrain.CombatStationaryState());
    this.AddState((BaseAIBrain<HumanNPC>.BasicAIState) new BaseAIBrain<HumanNPC>.BaseMoveTorwardsState());
  }
  public class IdleState : Scientist.IdleState
  {
  }
  
  public class RoamState : Scientist.RoamState
  {
    private StateStatus status = StateStatus.Error;
    private AIMovePoint roamPoint;

    public override void StateLeave()
    {
      base.StateLeave();
      this.Stop();
      this.ClearRoamPointUsage();
    }

    public override void StateEnter()
    {
      base.StateEnter();
      this.status = StateStatus.Error;
      this.ClearRoamPointUsage();
      HumanNPC entity = this.GetEntity();
      if (this.brain.PathFinder == null)
        return;
      this.status = StateStatus.Error;
      this.roamPoint = this.brain.PathFinder.GetBestRoamPoint(this.GetRoamAnchorPosition(), entity.transform.position, entity.eyes.BodyForward(), this.brain.Navigator.MaxRoamDistanceFromHome, this.brain.Navigator.BestRoamPointMaxDistance);
      if (!((Object) this.roamPoint != (Object) null))
        return;
      if (this.brain.Navigator.SetDestination(this.roamPoint.transform.position, BaseNavigator.NavigationSpeed.Slow))
      {
        this.roamPoint.SetUsedBy((BaseEntity) this.GetEntity());
        this.status = StateStatus.Running;
      }
      else
        this.roamPoint.SetUsedBy((BaseEntity) entity, 600f);
    }

    private void ClearRoamPointUsage()
    {
      if (!((Object) this.roamPoint != (Object) null))
        return;
      this.roamPoint.ClearIfUsedBy((BaseEntity) this.GetEntity());
      this.roamPoint = (AIMovePoint) null;
    }

    private void Stop() => this.brain.Navigator.Stop();

    public override StateStatus StateThink(float delta)
    {
      if (this.status == StateStatus.Error)
        return this.status;
      return this.brain.Navigator.Moving ? StateStatus.Running : StateStatus.Finished;
    }
  }
}

		
//NPC Spawner notes
	/*
	
			
		
	
	
	
	might need to capture state container to reuse
	
	
    int stateContainerId  this.currentStateContainerID;
    AIStateContainer stateContainerById = this.AIDesign.GetStateContainerByID(newStateContainerID);