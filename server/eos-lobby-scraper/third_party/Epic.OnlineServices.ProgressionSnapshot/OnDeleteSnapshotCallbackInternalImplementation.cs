namespace Epic.OnlineServices.ProgressionSnapshot;

internal static class OnDeleteSnapshotCallbackInternalImplementation
{
	private static OnDeleteSnapshotCallbackInternal s_Delegate;

	public static OnDeleteSnapshotCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDeleteSnapshotCallbackInternal))]
	public static void EntryPoint(ref DeleteSnapshotCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<DeleteSnapshotCallbackInfoInternal, OnDeleteSnapshotCallback, DeleteSnapshotCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
