using System;
using System.Runtime.InteropServices;
using Epic.OnlineServices.IntegratedPlatform;
using Epic.OnlineServices.Platform;

namespace Epic.OnlineServices;

public static class WindowsBindings
{
	[DllImport("EOSSDK-Win64-Shipping.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "EOS_IntegratedPlatformOptionsContainer_Add")]
	internal static extern Result EOS_IntegratedPlatformOptionsContainer_Add_WindowsSteam(IntPtr handle, ref WindowsSteamIntegratedPlatformOptionsContainerAddOptionsInternal inOptions);

	[DllImport("EOSSDK-Win64-Shipping.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "EOS_Platform_Create")]
	internal static extern IntPtr EOS_Platform_Create_Windows(ref WindowsOptionsInternal options);
}
