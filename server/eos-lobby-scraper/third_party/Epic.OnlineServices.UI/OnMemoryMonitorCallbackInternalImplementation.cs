namespace Epic.OnlineServices.UI;

internal static class OnMemoryMonitorCallbackInternalImplementation
{
	private static OnMemoryMonitorCallbackInternal s_Delegate;

	public static OnMemoryMonitorCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnMemoryMonitorCallbackInternal))]
	public static void EntryPoint(ref MemoryMonitorCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<MemoryMonitorCallbackInfoInternal, OnMemoryMonitorCallback, MemoryMonitorCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
