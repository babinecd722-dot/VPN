namespace Epic.OnlineServices.Reports;

internal static class OnSendPlayerBehaviorReportCompleteCallbackInternalImplementation
{
	private static OnSendPlayerBehaviorReportCompleteCallbackInternal s_Delegate;

	public static OnSendPlayerBehaviorReportCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSendPlayerBehaviorReportCompleteCallbackInternal))]
	public static void EntryPoint(ref SendPlayerBehaviorReportCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<SendPlayerBehaviorReportCompleteCallbackInfoInternal, OnSendPlayerBehaviorReportCompleteCallback, SendPlayerBehaviorReportCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
