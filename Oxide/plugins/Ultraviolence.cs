using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System;
namespace Oxide.Plugins{
	[Info("Ultraviolence", "obsol", "0.1.1")]
	[Description("Turns off IsNpc on HumanNPC and BaseAnimalNPC")]
	public class Ultraviolence : CovalencePlugin{	
		void Loaded(){
			Puts("Typing");
			Type t = typeof(NPCPlayer);
			Puts("Typed");
			PropertyInfo IsNpc = t.GetProperty("IsNpc");
			Puts("Getting IsNpc");
			MethodInfo Getter = IsNpc.GetGetMethod();
			Puts("Getting Getter");
			MethodInfo replacementInfo = this.GetType().GetMethod("IsNpc");
			Puts("Geting replacement");
			MethodBody replacement = replacementInfo.GetMethodBody();
			
			Puts("Getting IL");
			byte[] methodBytes = replacement.GetILAsByteArray();
			Puts("Getting Token");
			int tokenOld;
			Puts("Getting Tokend");
			tokenOld = Getter.MetadataToken;
			Puts("Donr");

			// Get the pointer to the method body.
			GCHandle hmem = GCHandle.Alloc((Object) methodBytes, GCHandleType.Pinned);
			IntPtr addr = hmem.AddrOfPinnedObject();
			int cbSize = methodBytes.Length;
			
			MethodRental.SwapMethodBody(
				t,
				tokenOld,
				addr,
				cbSize,
				MethodRental.JitImmediate);

		}
	    public bool IsNpc(){
			return true;
		}
	}
}