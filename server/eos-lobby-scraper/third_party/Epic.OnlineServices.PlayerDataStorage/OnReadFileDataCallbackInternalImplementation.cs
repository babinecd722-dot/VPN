namespace Epic.OnlineServices.PlayerDataStorage;

internal static class OnReadFileDataCallbackInternalImplementation
{
	private static OnReadFileDataCallbackInternal s_Delegate;

	public static OnReadFileDataCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnReadFileDataCallbackInternal))]
	public static ReadResult EntryPoint(ref ReadFileDataCallbackInfoInternal data)
	{
		if (Helper.TryGetStructCallback<ReadFileDataCallbackInfoInternal, OnReadFileDataCallback, ReadFileDataCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			return callback(ref callbackInfo);
		}
		return (ReadResult)0;
	}
}
