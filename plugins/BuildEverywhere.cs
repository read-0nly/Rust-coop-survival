
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
			
			
		}
		object CanBuild(Planner planner, Construction prefab, Construction.Target target)
{
			
            Puts("Canbuild called");
			return null;
}
        private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
        }return buildingPrivlidge;
		}
	}
}