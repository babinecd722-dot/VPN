namespace Epic.OnlineServices.PlayerDataStorage;

internal static class OnWriteFileCompleteCallbackInternalImplementation
{
	private static OnWriteFileCompleteCallbackInternal s_Delegate;

	public static OnWriteFileCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnWriteFileCompleteCallbackInternal))]
	public static void EntryPoint(ref WriteFileCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<WriteFileCallbackInfoInternal, OnWriteFileCompleteCallback, WriteFileCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
