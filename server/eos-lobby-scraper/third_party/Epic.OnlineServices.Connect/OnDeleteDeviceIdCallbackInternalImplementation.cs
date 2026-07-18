namespace Epic.OnlineServices.Connect;

internal static class OnDeleteDeviceIdCallbackInternalImplementation
{
	private static OnDeleteDeviceIdCallbackInternal s_Delegate;

	public static OnDeleteDeviceIdCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDeleteDeviceIdCallbackInternal))]
	public static void EntryPoint(ref DeleteDeviceIdCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<DeleteDeviceIdCallbackInfoInternal, OnDeleteDeviceIdCallback, DeleteDeviceIdCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
