namespace Epic.OnlineServices.Connect;

internal static class OnQueryProductUserIdMappingsCallbackInternalImplementation
{
	private static OnQueryProductUserIdMappingsCallbackInternal s_Delegate;

	public static OnQueryProductUserIdMappingsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryProductUserIdMappingsCallbackInternal))]
	public static void EntryPoint(ref QueryProductUserIdMappingsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryProductUserIdMappingsCallbackInfoInternal, OnQueryProductUserIdMappingsCallback, QueryProductUserIdMappingsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
