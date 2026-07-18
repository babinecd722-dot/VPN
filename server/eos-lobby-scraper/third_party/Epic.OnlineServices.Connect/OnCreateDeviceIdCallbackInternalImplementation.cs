namespace Epic.OnlineServices.Connect;

internal static class OnCreateDeviceIdCallbackInternalImplementation
{
	private static OnCreateDeviceIdCallbackInternal s_Delegate;

	public static OnCreateDeviceIdCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnCreateDeviceIdCallbackInternal))]
	public static void EntryPoint(ref CreateDeviceIdCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<CreateDeviceIdCallbackInfoInternal, OnCreateDeviceIdCallback, CreateDeviceIdCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
