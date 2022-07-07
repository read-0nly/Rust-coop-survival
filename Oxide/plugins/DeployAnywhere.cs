
using Oxide.Plugins;

using UnityEngine; 


namespace Oxide.Plugins
{
	[Info("DeployAnywhere", "obsol", "0.0.1")]
	[Description("Allows any deployable to be deployed using middle-click regardless of triggers/zones")]
	public class DeployAnywhere : CovalencePlugin
	{
		
		private void OnServerInitialized()
        {
			TriggerSafeZone[] szArr = TriggerSafeZone.allSafeZones.ToArray();
			foreach(TriggerSafeZone sz in szArr){
				sz.gameObject.SetActive(false);
			}
		}
		BaseEntity OnPlayerInput(BasePlayer player, InputState input){
			if(!input.WasJustPressed(BUTTON.FIRE_THIRD)) return null;
			Deployer deployer = player.GetHeldEntity() as Deployer;
			if(deployer){Puts("Deployer");return null;}
			Planner planner = player.GetHeldEntity() as Planner;
			uint prefabID = 0;
			if(planner){
				Puts("Planner " + planner.GetOwnerItemDefinition().isUsable.ToString());
				prefabID = (uint)planner.GetOwnerItemDefinition().gameObject.GetComponentInChildren<ItemModDeployable>()?.entityPrefab.resourceID;
			}
			if(planner==null){
				Puts(player.GetHeldEntity().ToString());
				if(!(player.GetHeldEntity().ToString().Contains("hammer.entity"))) return null;
				Puts("Rotating?");
				BaseEntity be = getLookingAt(player);
				Puts(be.name);
				be.gameObject.transform.Rotate(new Vector3(0,30f,0));
				
				be.InvalidateNetworkCache();
				if (!be.isCallingUpdateNetworkGroup)
				{
					be.Invoke(new System.Action(be.UpdateNetworkGroup), 5f);
					be.isCallingUpdateNetworkGroup = true;
				}
				be.SendNetworkUpdate_Position();
				be.OnPositionalNetworkUpdate();
				Puts(be.gameObject.transform.eulerAngles.ToString());
			}else{
				Deployable deploy = planner.GetDeployable();
				if(deploy==null)
				Puts("No  Deployable");
				RaycastHit raycastHit;
				if (!Physics.Raycast( player.eyes.HeadRay(), out raycastHit, 5f))
				{
					return null;
				}
				Puts("Raycast");
				Vector3 lhs;
				switch (planner.GetItem().info.shortname.ToLower()){
					case "ladder.wooden.wall":
						lhs= Quaternion.LookRotation(raycastHit.normal,  raycastHit.point) * Quaternion.Euler(0f, 0f, 0f) * Vector3.up;
						break;
					case "sign.wooden.small":
						lhs= Quaternion.LookRotation(raycastHit.normal, raycastHit.point) * Quaternion.Euler(0f, 0f, 0f) * Vector3.up;
						break;
					default:
						lhs= Quaternion.LookRotation(raycastHit.normal,  raycastHit.point) * Quaternion.Euler(90f, 0f, 0f) * Vector3.up;
						if (!(Mathf.Acos(Vector3.Dot(lhs, Vector3.up)) < 0.61086524f))
						{
							return null;
						}
						break;
				}
				Construction.Target target = new Construction.Target();
				target.ray = player.eyes.HeadRay();
				target.onTerrain = true;
				target.valid = true;
				target.inBuildingPrivilege = false;
				target.position = raycastHit.point+ new Vector3(0,0f,0);
				target.normal = raycastHit.normal;
				target.rotation = lhs;
				target.player = player;
				Puts("Target");
				Construction construction = (deploy?PrefabAttribute.server.Find<Construction>(deploy.prefabID):PrefabAttribute.server.Find<Construction>(prefabID));
				if(construction==null) return null;
				Puts("Construction");
				BaseEntity baseEntity =(construction.CreateConstruction(target,false));
				switch (planner.GetItem().info.shortname.ToLower()){
					case "ladder.wooden.wall":
						break;
					case "sign.wooden.small":
						break;
					default:
						baseEntity.gameObject.transform.Rotate(new Vector3(0,0f,0));
						break;
				}
						
				float num = 1f;
				global::Item ownerItem = player.inventory.FindItemUID(planner.ownerItemUID);
				if (ownerItem != null)
				{
					baseEntity.skinID = ownerItem.skin;
					if (ownerItem.hasCondition)
					{
						num = ownerItem.conditionNormalized;
					}
				}
				baseEntity.gameObject.AwakeFromInstantiate();
				global::BuildingBlock buildingBlock = baseEntity as global::BuildingBlock;
				if (buildingBlock)
				{
					buildingBlock.blockDefinition = global::PrefabAttribute.server.Find<global::Construction>(buildingBlock.prefabID);
					if (!buildingBlock.blockDefinition)
					{
						Debug.LogError("Placing a building block that has no block definition!");
						return null;
					}
					buildingBlock.SetGrade(buildingBlock.blockDefinition.defaultGrade.gradeBase.type);
					float num2 = buildingBlock.currentGrade.maxHealth;
				}
				global::BaseCombatEntity baseCombatEntity = baseEntity as global::BaseCombatEntity;
				if (baseCombatEntity)
				{
					float num2 = (buildingBlock != null) ? buildingBlock.currentGrade.maxHealth : baseCombatEntity.startHealth;
					baseCombatEntity.ResetLifeStateOnSpawn = false;
					baseCombatEntity.InitializeHealth(num2 * num, num2);
				}
				baseEntity.gameObject.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
				baseEntity.OwnerID = player.userID;
				baseEntity.Spawn();
				if (buildingBlock)
				{
					global::Effect.server.Run("assets/bundled/prefabs/fx/build/frame_place.prefab", baseEntity, 0U, Vector3.zero, Vector3.zero, null, false);
				}
				global::StabilityEntity stabilityEntity = baseEntity as global::StabilityEntity;
				if (stabilityEntity)
				{
					stabilityEntity.UpdateSurroundingEntities();
				}
				GameObject gameObject = baseEntity.gameObject;
				if (gameObject != null)
				{
					Puts("Gameobject");
					global::Deployable deployable = planner.GetDeployable();
					if (deployable != null)
					{
						if (deployable.setSocketParent && target.entity != null && target.entity.SupportsChildDeployables() && baseEntity)
						{
							Puts("SetParent?");
							baseEntity.SetParent(target.entity, true, false);
						}
						if (deployable.wantsInstanceData && ownerItem.instanceData != null)
						{
							Puts("Instance?");
							(baseEntity as global::IInstanceDataReceiver).ReceiveInstanceData(ownerItem.instanceData);
						}
						if (deployable.copyInventoryFromItem)
						{
							global::StorageContainer component2 = baseEntity.GetComponent<global::StorageContainer>();
							if (component2)
							{
								component2.ReceiveInventoryFromItem(ownerItem);
							}
						}
						baseEntity.OnDeployed(baseEntity.GetParentEntity(), player, ownerItem);
						if (deployable.placeEffect.isValid)
						{
							Puts("placeffect");
							if (target.entity && target.socket != null)
							{
								global::Effect.server.Run(deployable.placeEffect.resourcePath, target.entity.transform.TransformPoint(target.socket.worldPosition), target.entity.transform.up, null, false);
							}
							else
							{
								global::Effect.server.Run(deployable.placeEffect.resourcePath, target.position, target.normal, null, false);
							}
						}
					}
					Puts("Pay");
					planner.PayForPlacement(player, construction);
				}
			}
			return null;
		}
		public Transform getLookingAtRaw(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.transform;
				return entity;
			}
			return null;
		}
		public BaseEntity getLookingAt(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
			}
			return null;
		}
	}
}
			