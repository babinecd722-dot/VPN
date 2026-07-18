namespace Epic.OnlineServices.RTCAudio;

internal static class OnUpdateParticipantVolumeCallbackInternalImplementation
{
	private static OnUpdateParticipantVolumeCallbackInternal s_Delegate;

	public static OnUpdateParticipantVolumeCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateParticipantVolumeCallbackInternal))]
	public static void EntryPoint(ref UpdateParticipantVolumeCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateParticipantVolumeCallbackInfoInternal, OnUpdateParticipantVolumeCallback, UpdateParticipantVolumeCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
