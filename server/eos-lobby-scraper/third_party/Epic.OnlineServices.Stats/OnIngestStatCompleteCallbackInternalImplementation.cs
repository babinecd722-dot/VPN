namespace Epic.OnlineServices.Stats;

internal static class OnIngestStatCompleteCallbackInternalImplementation
{
	private static OnIngestStatCompleteCallbackInternal s_Delegate;

	public static OnIngestStatCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnIngestStatCompleteCallbackInternal))]
	public static void EntryPoint(ref IngestStatCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<IngestStatCompleteCallbackInfoInternal, OnIngestStatCompleteCallback, IngestStatCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
