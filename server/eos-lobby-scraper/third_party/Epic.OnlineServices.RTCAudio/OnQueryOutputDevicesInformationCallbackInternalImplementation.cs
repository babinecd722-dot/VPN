namespace Epic.OnlineServices.RTCAudio;

internal static class OnQueryOutputDevicesInformationCallbackInternalImplementation
{
	private static OnQueryOutputDevicesInformationCallbackInternal s_Delegate;

	public static OnQueryOutputDevicesInformationCallbackInternal Delegate
	{
		get
		{
			if (s_Delegate == null)
			{
				s_Delegate = EntryPoint;
			}
			return s_Delegate;
		}
	}

	[MonoPInvokeCallback(typeof(OnQueryOutputDevicesInformationCallbackInternal))]
	public static void EntryPoint(ref OnQueryOutputDevicesInformationCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryOutputDevicesInformationCallbackInfoInternal, OnQueryOutputDevicesInformationCallback, OnQueryOutputDevicesInformationCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
