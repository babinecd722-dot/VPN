using System;

namespace Epic.OnlineServices.Reports;

public sealed class ReportsInterface : Handle
{
	public const int REPORTCONTEXT_MAX_LENGTH = 4096;

	public const int REPORTMESSAGE_MAX_LENGTH = 512;

	public const int SENDPLAYERBEHAVIORREPORT_API_LATEST = 2;

	public ReportsInterface()
	{
	}

	public ReportsInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public void SendPlayerBehaviorReport(ref SendPlayerBehaviorReportOptions options, object clientData, OnSendPlayerBehaviorReportCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SendPlayerBehaviorReportOptionsInternal options2 = default(SendPlayerBehaviorReportOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Reports_SendPlayerBehaviorReport(base.InnerHandle, ref options2, clientDataPointer, OnSendPlayerBehaviorReportCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
