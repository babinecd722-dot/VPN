using System.Runtime.InteropServices;
using Epic.OnlineServices.Platform;

namespace Epic.OnlineServices;

public static class AndroidBindings
{
	[DllImport("EOSSDK-Win64-Shipping.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "EOS_Initialize")]
	internal static extern Result EOS_Initialize_Android(ref AndroidInitializeOptionsInternal options);
}
