namespace Epic.OnlineServices.PlayerDataStorage;

internal static class OnDeleteFileCompleteCallbackInternalImplementation
{
	private static OnDeleteFileCompleteCallbackInternal s_Delegate;

	public static OnDeleteFileCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDeleteFileCompleteCallbackInternal))]
	public static void EntryPoint(ref DeleteFileCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<DeleteFileCallbackInfoInternal, OnDeleteFileCompleteCallback, DeleteFileCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
