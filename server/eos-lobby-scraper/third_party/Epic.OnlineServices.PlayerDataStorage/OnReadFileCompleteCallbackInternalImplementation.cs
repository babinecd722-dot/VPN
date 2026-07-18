namespace Epic.OnlineServices.PlayerDataStorage;

internal static class OnReadFileCompleteCallbackInternalImplementation
{
	private static OnReadFileCompleteCallbackInternal s_Delegate;

	public static OnReadFileCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnReadFileCompleteCallbackInternal))]
	public static void EntryPoint(ref ReadFileCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<ReadFileCallbackInfoInternal, OnReadFileCompleteCallback, ReadFileCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
