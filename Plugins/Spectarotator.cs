
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
using CompanionServer.Handlers;
using ConVar;
#endregion
namespace Oxide.Plugins
{
    [Info("Spectarotator", "obsol", "0.2.1")]
    [Description("Makes NPCs and Animals attack any NPC/animal that doesn't share their prefab name")]
    public class Spectarotator : CovalencePlugin
    {
        static BasePlayer Spectator = null;

        [Command("spectarotate")]
        void spectarotate(IPlayer player, string command, string[] args)
        {
            Puts("Setting spectator " + player.Id);
            BasePlayer Spectator = (player.Object as BasePlayer);
            Spectator.SpectateOffset++;
            Spectator.UpdateSpectateTarget(Spectator.spectateFilter);
            Puts("Set spectator " + Spectator.userID);
            timer.Every(060f, () =>
            {
                
                if (Spectator == null)
                {
                    return;
                }
                
                if (Spectator.spectateFilter == null || Spectator.spectateFilter == "") return;
                
                if (Spectator.parentEntity.Get(true) == null)
                {

                    Spectator.SpectateOffset++;
                    Spectator.UpdateSpectateTarget(Spectator.spectateFilter);
                    return;
                }

                BasePlayer bp = Spectator.parentEntity.Get(true) as BasePlayer;
                if (bp == null) {

                    Spectator.SpectateOffset++;
                    Spectator.UpdateSpectateTarget(Spectator.spectateFilter);
                    return; 
                }
                if (bp.IsDead() || (bp.lastAttackedTime < UnityEngine.Time.time - 30 && (bp.GetComponent<BaseAIBrain>()!=null && bp.GetComponent<BaseAIBrain>().CurrentState.AgrresiveState==false)))
                {
                    Spectator.SpectateOffset++;
                    Spectator.UpdateSpectateTarget(Spectator.spectateFilter);
                }
            });
        }

        private void OnServerInitialized()
        {
        }
    }
}
