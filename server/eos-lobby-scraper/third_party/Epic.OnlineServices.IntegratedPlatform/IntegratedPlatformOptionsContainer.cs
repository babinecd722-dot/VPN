using System;

namespace Epic.OnlineServices.IntegratedPlatform;

public sealed class IntegratedPlatformOptionsContainer : Handle
{
	public IntegratedPlatformOptionsContainer()
	{
	}

	public IntegratedPlatformOptionsContainer(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result Add(ref IntegratedPlatformOptionsContainerAddOptions inOptions)
	{
		IntegratedPlatformOptionsContainerAddOptionsInternal inOptions2 = default(IntegratedPlatformOptionsContainerAddOptionsInternal);
		inOptions2.Set(ref inOptions);
		Result result = Bindings.EOS_IntegratedPlatformOptionsContainer_Add(base.InnerHandle, ref inOptions2);
		Helper.Dispose(ref inOptions2);
		return result;
	}

	public void Release()
	{
		Bindings.EOS_IntegratedPlatformOptionsContainer_Release(base.InnerHandle);
	}

	public Result Add(ref WindowsSteamIntegratedPlatformOptionsContainerAddOptions inOptions)
	{
		WindowsSteamIntegratedPlatformOptionsContainerAddOptionsInternal inOptions2 = default(WindowsSteamIntegratedPlatformOptionsContainerAddOptionsInternal);
		inOptions2.Set(ref inOptions);
		Result result = WindowsBindings.EOS_IntegratedPlatformOptionsContainer_Add_WindowsSteam(base.InnerHandle, ref inOptions2);
		Helper.Dispose(ref inOptions2);
		return result;
	}
}
