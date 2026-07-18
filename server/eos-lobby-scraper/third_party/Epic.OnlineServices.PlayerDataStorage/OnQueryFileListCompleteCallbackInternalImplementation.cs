namespace Epic.OnlineServices.PlayerDataStorage;

internal static class OnQueryFileListCompleteCallbackInternalImplementation
{
	private static OnQueryFileListCompleteCallbackInternal s_Delegate;

	public static OnQueryFileListCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryFileListCompleteCallbackInternal))]
	public static void EntryPoint(ref QueryFileListCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryFileListCallbackInfoInternal, OnQueryFileListCompleteCallback, QueryFileListCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
