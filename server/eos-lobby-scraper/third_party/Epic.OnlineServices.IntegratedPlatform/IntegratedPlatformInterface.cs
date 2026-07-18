using System;

namespace Epic.OnlineServices.IntegratedPlatform;

public sealed class IntegratedPlatformInterface : Handle
{
	public const int ADDNOTIFYUSERLOGINSTATUSCHANGED_API_LATEST = 1;

	public const int CLEARUSERPRELOGOUTCALLBACK_API_LATEST = 1;

	public const int CREATEINTEGRATEDPLATFORMOPTIONSCONTAINER_API_LATEST = 1;

	public const int FINALIZEDEFERREDUSERLOGOUT_API_LATEST = 1;

	public const int INTEGRATEDPLATFORMOPTIONSCONTAINER_ADD_API_LATEST = 1;

	public const int OPTIONS_API_LATEST = 1;

	public const int SETUSERLOGINSTATUS_API_LATEST = 1;

	public const int SETUSERPRELOGOUTCALLBACK_API_LATEST = 1;

	public static readonly Utf8String WINDOWS_STEAM_IPT = "STEAM";

	public const int WINDOWS_STEAM_MAX_STEAMAPIINTERFACEVERSIONSARRAY_SIZE = 4096;

	public const int WINDOWS_STEAM_OPTIONS_API_LATEST = 3;

	public IntegratedPlatformInterface()
	{
	}

	public IntegratedPlatformInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyUserLoginStatusChanged(ref AddNotifyUserLoginStatusChangedOptions options, object clientData, OnUserLoginStatusChangedCallback callbackFunction)
	{
		if (callbackFunction == null)
		{
			throw new ArgumentNullException("callbackFunction");
		}
		AddNotifyUserLoginStatusChangedOptionsInternal options2 = default(AddNotifyUserLoginStatusChangedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, callbackFunction);
		ulong num = Bindings.EOS_IntegratedPlatform_AddNotifyUserLoginStatusChanged(base.InnerHandle, ref options2, clientDataPointer, OnUserLoginStatusChangedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public void ClearUserPreLogoutCallback(ref ClearUserPreLogoutCallbackOptions options)
	{
		ClearUserPreLogoutCallbackOptionsInternal options2 = default(ClearUserPreLogoutCallbackOptionsInternal);
		options2.Set(ref options);
		Bindings.EOS_IntegratedPlatform_ClearUserPreLogoutCallback(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
	}

	public static Result CreateIntegratedPlatformOptionsContainer(ref CreateIntegratedPlatformOptionsContainerOptions options, out IntegratedPlatformOptionsContainer outIntegratedPlatformOptionsContainerHandle)
	{
		CreateIntegratedPlatformOptionsContainerOptionsInternal options2 = default(CreateIntegratedPlatformOptionsContainerOptionsInternal);
		options2.Set(ref options);
		IntPtr outIntegratedPlatformOptionsContainerHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_IntegratedPlatform_CreateIntegratedPlatformOptionsContainer(ref options2, out outIntegratedPlatformOptionsContainerHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outIntegratedPlatformOptionsContainerHandle2, out outIntegratedPlatformOptionsContainerHandle);
		return result;
	}

	public Result FinalizeDeferredUserLogout(ref FinalizeDeferredUserLogoutOptions options)
	{
		FinalizeDeferredUserLogoutOptionsInternal options2 = default(FinalizeDeferredUserLogoutOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_IntegratedPlatform_FinalizeDeferredUserLogout(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void RemoveNotifyUserLoginStatusChanged(ulong notificationId)
	{
		Bindings.EOS_IntegratedPlatform_RemoveNotifyUserLoginStatusChanged(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public Result SetUserLoginStatus(ref SetUserLoginStatusOptions options)
	{
		SetUserLoginStatusOptionsInternal options2 = default(SetUserLoginStatusOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_IntegratedPlatform_SetUserLoginStatus(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetUserPreLogoutCallback(ref SetUserPreLogoutCallbackOptions options, object clientData, OnUserPreLogoutCallback callbackFunction)
	{
		if (callbackFunction == null)
		{
			throw new ArgumentNullException("callbackFunction");
		}
		SetUserPreLogoutCallbackOptionsInternal options2 = default(SetUserPreLogoutCallbackOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, callbackFunction);
		Result result = Bindings.EOS_IntegratedPlatform_SetUserPreLogoutCallback(base.InnerHandle, ref options2, clientDataPointer, OnUserPreLogoutCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		return result;
	}
}
