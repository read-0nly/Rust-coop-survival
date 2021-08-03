//[System.Windows.Forms.Form].GetMembers() | select-object name,membertype

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
	[Info("Analyze", "obsol", "0.0.1")]
	[Description("Type Explorer for Rust - bit of a wonky syntax")]

/*======================================================================================================================= 
*
    /anal self|world|spectator|entity:property>property[:lp|lm|lc|le]
	
	lp = list properties
	lm = list methods
	lc = list constructors
	le = list events
	
	Get the properties of the current player
	/anal self:list>properties
	/anal self:metabolism>health:lp
	
*=======================================================================================================================*/


	public class Analyze : CovalencePlugin
	{
		
	
	   public class MemberInfoComparer : IComparer
	   {
		  // Call CaseInsensitiveComparer.Compare with the parameters reversed.
		  int IComparer.Compare(System.Object x, System.Object y)
		  {
			  return ((new CaseInsensitiveComparer()).Compare(((System.Reflection.MemberInfo)x).Name, ((System.Reflection.MemberInfo)y).Name));
		  }
	   }
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) =>
            _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Analyze]</color>", 0, Array.Empty<object>());
        [Command("anal")]
        private void AnalyzeCommand(IPlayer player, string command, string[] args)
        {
			string[] parseString = args[0].Split(':');
					
			string[] paths = new string[0];
			if(parseString.Count()>1){
				if(parseString[1].Contains('>')){
					paths=parseString[1].Split('>');
				}
				else{
					if(parseString[1]!=""){
						paths= new string[1];
						paths[0] =parseString[1];
					}
				}
			}
			switch (parseString[0].ToLower()){
				case "self":
					if(parseString.Count()>2){
						switch(parseString[2]){
							case "lm":
								recurseMemberObj(player,(BasePlayer)player.Object,"Method",paths);	
								break;
							case "lp":
								recurseMemberObj(player,(BasePlayer)player.Object,"Property",paths);	
								break;
							case "le":
								recurseMemberObj(player,(BasePlayer)player.Object,"Event",paths);	
								break;
							case "lc":
								recurseMemberObj(player,(BasePlayer)player.Object,"Constructor",paths);	
								break;
						}
						
					}
					else{
						SendChatMsg((BasePlayer)player.Object,recursePropertyValue(player,(BasePlayer)player.Object,((BasePlayer)player.Object).GetType().GetMembers(),paths));
					}
					break;
				case "assemblies":
					List<Type> list = new List<Type>();
					foreach (System.Reflection.Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
					{
						foreach (Type t in ass.GetExportedTypes())
						{
							if (t.IsEnum)
							{
								list.Add(t);
							}
						}
					}
					foreach (Type t in list){
						Puts(t.FullName);
					}
					break;
				case "blueprint":
					var bp = ItemManager.bpList[0];
					recurseMemberObj(player,((Translate.Phrase)bp.targetItem.displayName),"Constructor",paths);
					break;
				case "scene":					
					GameObject[] gos = (UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects());
					GameObject compound = null;
					for(int i = 0; i < gos.Count(); i++){
						if(gos[i].name == "Decor"){
							Puts(gos[i].name);
							Puts(gos[i].transform.childCount.ToString());
							compound=gos[i];
							SendChatMsg((BasePlayer)player.Object,gos[i].name);
							SendChatMsg((BasePlayer)player.Object,gos[i].transform.childCount.ToString());
						}
					}
					for(int i = 0; i < compound.transform.childCount;i++){
							String name = compound.transform.GetChild(i).gameObject.name;
							if(name.StartsWith("assets/bundled/prefabs/modding")){
								Puts(compound.transform.GetChild(i).gameObject.name);
								SendChatMsg((BasePlayer)player.Object,compound.transform.GetChild(i).gameObject.name);
							}
					}
					break;

				default:
					break;
			}
		}
		
		private System.Object getPropertyValue(System.Object o, string p){
			System.Reflection.PropertyInfo property= o.GetType().GetProperty(p);
			return property.GetValue(o, null);
		}
		
		private string recursePropertyValue(IPlayer player, System.Object t, System.Reflection.MemberInfo[] mis, string[] remainingChain){
			IComparer miComparer = new MemberInfoComparer();
			Array.Sort(mis, miComparer);
			if(remainingChain.Count()==1){
				foreach(System.Reflection.MemberInfo mix in mis){
					if(mix.Name == remainingChain[0]){
						return getPropertyValue(t,remainingChain[0]).ToString();
					}
				}
			}
			else{
				foreach(System.Reflection.MemberInfo mix in mis){
					if(mix.Name == remainingChain[0]){
						return recursePropertyValue(player,getPropertyValue(t,remainingChain[0]),mix.DeclaringType.GetMembers(),remainingChain.Skip(1).ToArray());
					}
				}
			}
			return "GetVal Failed!"; //this should work
		}
		
		private void recurseMemberObj(IPlayer player, System.Object t, string memberType, string[] remainingChain){
			System.Reflection.MemberInfo[] mis = t.GetType().GetMembers();
			IComparer miComparer = new MemberInfoComparer();
			Array.Sort(mis, miComparer);
			if(remainingChain.Count()==0){
				string finalString = "";
				if (mis.Count()>0){
					foreach(System.Reflection.MemberInfo mi in mis){
						if(mi.MemberType.ToString() == memberType){
							finalString+= (" ["+mi.Name + ":" + mi.MemberType.ToString()+":"+(memberType=="Property"?((System.Reflection.PropertyInfo)mi).GetValue(t):"")+"] <br> ");
						}
					} 
				}
				SendChatMsg((BasePlayer)player.Object,finalString);
			}
			else{
				foreach(System.Reflection.MemberInfo mi in mis){
					if(mi.Name == remainingChain[0]){
						recurseMember(player,mi,memberType,remainingChain);
					}
				}
			}
		}
		
		private void recurseMember(IPlayer player, System.Reflection.MemberInfo t, string memberType, string[] remainingChain){
			
			if(remainingChain.Count()==0){
				System.Reflection.MemberInfo[] mis = t.DeclaringType.GetMembers();
				SendChatMsg((BasePlayer)player.Object, t.ReflectedType.Name);
				string finalString = "";
				foreach(System.Reflection.MemberInfo mi in mis){
					if(mi.MemberType.ToString() == memberType){
						finalString+= (" ["+mi.Name + ":" + mi.MemberType.ToString()+"] ");
					}
				}
				SendChatMsg((BasePlayer)player.Object,finalString);
			}
			else{
				System.Reflection.MemberInfo[] mis = t.DeclaringType.GetMembers();
				SendChatMsg((BasePlayer)player.Object, "Recurse: "+remainingChain[0]);
				SendChatMsg((BasePlayer)player.Object, t.ReflectedType.Name);
				foreach(System.Reflection.MemberInfo mi in mis){
					if(mi.Name == remainingChain[0]){
						SendChatMsg((BasePlayer)player.Object, mi.Name);
						if(remainingChain.Count()>2){
							recurseMember(player,mi,memberType,remainingChain.Skip(1).ToArray());
						}else{
							recurseMember(player,mi,memberType,new string[0]);
						}
					}
				}
			}
		}
    }
}