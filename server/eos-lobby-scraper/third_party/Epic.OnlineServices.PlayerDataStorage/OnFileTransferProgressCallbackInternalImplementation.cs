namespace Epic.OnlineServices.PlayerDataStorage;

internal static class OnFileTransferProgressCallbackInternalImplementation
{
	private static OnFileTransferProgressCallbackInternal s_Delegate;

	public static OnFileTransferProgressCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnFileTransferProgressCallbackInternal))]
	public static void EntryPoint(ref FileTransferProgressCallbackInfoInternal data)
	{
		if (Helper.TryGetStructCallback<FileTransferProgressCallbackInfoInternal, OnFileTransferProgressCallback, FileTransferProgressCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
