
using Convert = System.Convert;
using Network;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using UnityEngine; 
using UnityEngine.SceneManagement;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Libraries.Covalence;
using ProtoBuf;

namespace Oxide.Plugins
{
	[Info("Nuild Everywhere", "obsol", "0.0.1")]
	[Description("Build everywhere!")]

/*======================================================================================================================= 
* 
//One Entity Enter - if nobuild zone, leave

/*
void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
{
    Puts("OnEntityEnter works!");
}
*=======================================================================================================================*/


	public class BuildEverywhere : CovalencePlugin
	{
		void OnItemUse(Item item, int amountToUse)
        {
			
			 // get root objects in scene
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
		}
		
		void removePreventBuilds(GameObject g){
			 List<GameObject> rootObjects = new List<GameObject>();
			
			 for (int i = 0; i < rootObjects.Count; ++i)
			 {
				 GameObject gameObject = rootObjects[ i ];
				 //gameObject.DoSomething();
				 if(gameObject.name == "prevent_building"){
					 
				 }
			 }				
		}
		object CanBuild(Planner planner, Construction prefab, Construction.Target target)
		{/*
            Puts("Canbuild called");*/
		}
        private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
			return;
		}
	}
}