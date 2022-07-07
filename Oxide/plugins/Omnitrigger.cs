
using Oxide.Plugins;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
	[Info("Omnitrigger", "obsol", "0.0.1")]
	[Description("Allows trap triggers to react to animals and NPCs")]
	public class Omnitrigger : CovalencePlugin
	{
				
				
		public LayerMask NPCLayers = 0; //Debug.Log(LayerMask.LayerToName(1));
		void OnEntitySpawned(BaseNetworkable entity)
		{
			BaseEntity be = (entity as BaseEntity);
			BaseAnimalNPC bn = (entity as BaseAnimalNPC);
			HumanNPC hn = (entity as HumanNPC);
			if(be==null)return;
			if(be is GunTrap){
					GunTrap gt = be as GunTrap;
					gt.trigger.interestLayers = gt.trigger.interestLayers|NPCLayers;
			}else if(be is AutoTurret){
					AutoTurret gt = be as AutoTurret;//
					gt.targetTrigger.interestLayers = gt.targetTrigger.interestLayers|NPCLayers;
			}else if(be is FlameTurret){
					FlameTurret gt = be as FlameTurret;
					gt.trigger.interestLayers = gt.trigger.interestLayers|NPCLayers;
			}else if(be is TeslaCoil){
					TeslaCoil gt = be as TeslaCoil;
					gt.targetTrigger.interestLayers = gt.targetTrigger.interestLayers|NPCLayers;
			}else if((be is BaseTrap)){
				TriggerBase btt = be.GetComponentInChildren<TriggerBase>();
				if(btt==null){
					btt = be.GetComponentInChildren<BaseTrapTrigger>();
				}
				if(btt==null){
					btt = be.GetComponentInChildren<BearTrapTrigger>();
				}
				if(btt!=null){
					btt.interestLayers = btt.interestLayers|NPCLayers;
				}
			} else if((entity is BaseAnimalNPC) && bn.gameObject!=null){
				try{
					Rigidbody rb = bn.gameObject.GetComponent<Rigidbody>();
					if(rb==null){rb = bn.gameObject.GetComponentInChildren<Rigidbody>();}
					if(rb==null){rb=bn.gameObject.AddComponent<Rigidbody>();}
					BoxCollider bc = (bn.gameObject.GetComponent<BoxCollider>()? bn.gameObject.GetComponent<BoxCollider>():bn.gameObject.AddComponent<BoxCollider>());
					if(bc!=null){
						bc.size = new Vector3(1f,1f,1f);
						bc.center = new Vector3(0,0,0);
						bc.enabled = true;
					}
					if(rb!=null){
						rb.detectCollisions = true;
						rb.useGravity = false;	
						bn.ForceUpdateTriggers();	
					}					
					NPCLayers = NPCLayers | (1 << bn.gameObject.layer);
				}catch (Exception e){
					Puts(e.ToString());
				}
			
			} else if((entity is HumanNPC) && hn.gameObject!=null){
				try{
					Rigidbody rb = hn.gameObject.GetComponent<Rigidbody>();
					if(rb==null){rb = hn.gameObject.GetComponentInChildren<Rigidbody>();}
					if(rb==null){rb = hn.gameObject.AddComponent<Rigidbody>();}
					BoxCollider bc = hn.gameObject.AddComponent<BoxCollider>();
					bc.size = new Vector3(1f,1f,1f);
					bc.center = new Vector3(0,0,0);
					bc.enabled = true;
					rb.detectCollisions = true;
					rb.isKinematic = true;		
					rb.useGravity = false;	
					NPCLayers = NPCLayers | (1 << hn.gameObject.layer);
					hn.ForceUpdateTriggers();					
				}catch ( Exception e){
					Puts(e.ToString());
				}
			
			}
		}
	}
}