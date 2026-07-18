using System;

namespace Epic.OnlineServices.Reports;

internal struct SendPlayerBehaviorReportOptionsInternal : ISettable<SendPlayerBehaviorReportOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ReporterUserId;

	private IntPtr m_ReportedUserId;

	private PlayerReportsCategory m_Category;

	private IntPtr m_Message;

	private IntPtr m_Context;

	public void Set(ref SendPlayerBehaviorReportOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.ReporterUserId, ref m_ReporterUserId);
		Helper.Set((Handle)other.ReportedUserId, ref m_ReportedUserId);
		m_Category = other.Category;
		Helper.Set(other.Message, ref m_Message);
		Helper.Set(other.Context, ref m_Context);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ReporterUserId);
		Helper.Dispose(ref m_ReportedUserId);
		Helper.Dispose(ref m_Message);
		Helper.Dispose(ref m_Context);
	}
}
