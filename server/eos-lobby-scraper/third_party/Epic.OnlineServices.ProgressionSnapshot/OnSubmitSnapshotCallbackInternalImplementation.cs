namespace Epic.OnlineServices.ProgressionSnapshot;

internal static class OnSubmitSnapshotCallbackInternalImplementation
{
	private static OnSubmitSnapshotCallbackInternal s_Delegate;

	public static OnSubmitSnapshotCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSubmitSnapshotCallbackInternal))]
	public static void EntryPoint(ref SubmitSnapshotCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<SubmitSnapshotCallbackInfoInternal, OnSubmitSnapshotCallback, SubmitSnapshotCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
