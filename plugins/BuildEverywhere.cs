
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

namespace Oxide.Plugins
{
	[Info("Trigger Zapper", "obsol", "0.0.1")]
	[Description("Build everywhere! Never be warm! Who knows!")]

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
        private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            if (!(trigger is TriggerSafeZone) && !(entity is BasePlayer)) return;
            var safeZone = trigger as TriggerSafeZone;
            if (safeZone == null) return;

            safeZone.enabled = false;
        }

    }
}