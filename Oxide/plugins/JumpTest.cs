
/*
    [ServerVar(Help = "Show user info for players on server.")]
    public static void users(ConsoleSystem.Arg arg)
    {
*/

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
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using System.Threading;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("JumpTest", "obsol", "0.0.1")]
	[Description("Jumping from navmesh to navmesh using offmesh links")]
	public class JumpTest : CovalencePlugin
	{
	
	
		
		void OnEntitySpawned(NPCPlayer bn){
			BaseNavigator bnav = bn.GetComponent<BaseNavigator>();
			if(bnav==null)return;
			bnav.Agent.autoTraverseOffMeshLink = false;
			bn.gameObject.AddComponent<AgentLinkMover>();
			
		}
	
		public enum OffMeshLinkMoveMethod
		{
			Teleport,
			NormalSpeed,
			Parabola
		}

		[RequireComponent(typeof(NavMeshAgent))]
		public class AgentLinkMover : MonoBehaviour
		{
			public OffMeshLinkMoveMethod method = OffMeshLinkMoveMethod.Parabola;
			IEnumerator Start()
			{
				NavMeshAgent agent = GetComponent<NavMeshAgent>();
				BasePlayer player = GetComponent<BasePlayer>();
				agent.autoTraverseOffMeshLink = false;
				while (true)
				{
					if (agent.isOnOffMeshLink)
					{
						player.modelState.onLadder = true;
						player.SendModelState(true);
						if (method == OffMeshLinkMoveMethod.NormalSpeed)
							yield return StartCoroutine(NormalSpeed(agent));
						else if (method == OffMeshLinkMoveMethod.Parabola)
							yield return StartCoroutine(Parabola(agent, 4.0f, 2f));
						
						player.modelState.onLadder = false;
						player.SendModelState(true);
						agent.CompleteOffMeshLink();
					}
					yield return null;
				}
			}

			IEnumerator NormalSpeed(NavMeshAgent agent)
			{
				OffMeshLinkData data = agent.currentOffMeshLinkData;
				Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
				while (agent.transform.position != endPos)
				{
					agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
					yield return null;
				}
			}

			IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
			{
				OffMeshLinkData data = agent.currentOffMeshLinkData;
				Vector3 startPos = agent.transform.position;
				Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
				float normalizedTime = 0.0f;
				while (normalizedTime < 1.0f)
				{
					float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
					agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
					normalizedTime += Time.deltaTime / duration;
					yield return null;
				}
			}
		}
	}
	
}
