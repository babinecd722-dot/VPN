using System;

namespace Epic.OnlineServices.UI;

internal struct AcknowledgeEventIdOptionsInternal : ISettable<AcknowledgeEventIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private ulong m_UiEventId;

	private Result m_Result;

	public void Set(ref AcknowledgeEventIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_UiEventId = other.UiEventId;
		m_Result = other.Result;
	}

	public void Dispose()
	{
	}
}
