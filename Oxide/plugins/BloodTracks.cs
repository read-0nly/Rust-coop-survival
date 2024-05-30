

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
using Oxide.Game.Rust.Cui;
using CompanionServer.Handlers;
#endregion
namespace Oxide.Plugins{
	[Info("BloodTracks", "obsol", "0.2.1")]
	[Description("Sends target positions to players for some time after hit")]
	public class BloodTracks: CovalencePlugin{
 /*
Planned:	Permission system
		Configuration file (control overlay, spheres, rays, as well as which types of entities get followed)

 */
		Timer myTimer;
		public class LineSegment{
			public static bool overlay=false;
			public static bool sphere=true;
			
			public static Queue<LineSegment> queue = new Queue<LineSegment>();
			public static Dictionary<BaseCombatEntity,List<LineSegment>> static_targets = new Dictionary<BaseCombatEntity,List<LineSegment>>();
			
			public Vector3 from_vector;
			public BaseCombatEntity target;
			public BasePlayer rx;
			
			public double timeout=0;
			public float size = 0.1f;
			public double bleed_multiplier=1f;
			public double damage_threshold = 10f;
			public float probability=0.7f;
			public double remove_time = 0f;
			
			public LineSegment(BaseCombatEntity bce, BasePlayer player){
				target=bce;
				rx=player;
				timeout=5f;
				remove_time=15f;
				queue.Enqueue(this);
				static_targets.Add(bce,new List<LineSegment>(){this});
			}
			
			public static bool remove(BaseCombatEntity target_for_removal=null){
				int rotate_amount = queue.Count;
				for(int i = 0; i < rotate_amount; i++){
					LineSegment current = queue.Dequeue();
					if(current.target==target_for_removal) continue;					
					queue.Enqueue(current);
				}
				if(static_targets.ContainsKey(target_for_removal)){
					static_targets.Remove(target_for_removal);
				}
				return true;
			}
			
			public bool update_target(){
				if(target==null||target.transform==null) return false;
				if(overlay){
					ConsoleNetwork.SendClientCommand(rx.Connection,"ddraw.text", new object[]
					{
						timeout,
						global::UnityEngine.Color.red,
						target.transform.position,
						"∙"
					});
				}
				if(sphere){
					ConsoleNetwork.SendClientCommand(rx.Connection,"ddraw.sphere", new object[]
					{
						timeout,
						global::UnityEngine.Color.red,
						target.transform.position + new Vector3(0,0.5f,0),
						size
					});
				}
				from_vector = target.transform.position;
				return true;
			}
			
			public static void process_queue(int process_amount){
				int rotate_amount = queue.Count;
				for(int i = 0; i < rotate_amount && i<process_amount; i++){					
					LineSegment current = queue.Dequeue();					
					if(current.update_target() && current.remove_time>Network.TimeEx.currentTimestamp)
						queue.Enqueue(current);
				}
				
			}
			
			public bool add_damage(double damage){
				damage=damage-damage_threshold;
				if(damage<0) return false;
				timeout+=timeout>60?0:bleed_multiplier*damage;
				remove_time =(remove_time > Network.TimeEx.currentTimestamp? remove_time-Network.TimeEx.currentTimestamp: 0) +Network.TimeEx.currentTimestamp + (bleed_multiplier*damage);
				return true;
			}
		}
		void OnServerInitialized(){
			myTimer=timer.Every(1f, () =>
			{
				LineSegment.process_queue(3);
			});

		}
		
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			BasePlayer hitter = info.Initiator as BasePlayer;
			if(hitter==null || hitter.Connection==null)
				return null;
			if(entity as BaseNpc==null && entity as BasePlayer==null)
				return null;
			LineSegment segment = null;
			if(LineSegment.static_targets.ContainsKey(entity))
			{
				foreach(LineSegment line in LineSegment.static_targets[entity])
				{
					if(line.rx==hitter)
					{
						segment=line;
						break;
					}
				}				
			}
			if(segment==null)
			{
				segment=new LineSegment(entity, hitter);
				
			}
			segment.add_damage(info.damageTypes.Total()+(entity.startHealth-entity.health));
			return null;
			
			
		}
		void Unloaded(){
			
			myTimer.Destroy();
		}
		
	}
}
