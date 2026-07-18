namespace Epic.OnlineServices.RTCAudio;

internal static class OnQueryInputDevicesInformationCallbackInternalImplementation
{
	private static OnQueryInputDevicesInformationCallbackInternal s_Delegate;

	public static OnQueryInputDevicesInformationCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryInputDevicesInformationCallbackInternal))]
	public static void EntryPoint(ref OnQueryInputDevicesInformationCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryInputDevicesInformationCallbackInfoInternal, OnQueryInputDevicesInformationCallback, OnQueryInputDevicesInformationCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
