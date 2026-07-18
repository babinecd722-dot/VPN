using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogEventOptionsInternal : ISettable<LogEventOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	private uint m_EventId;

	private uint m_ParamsCount;

	private IntPtr m_Params;

	public void Set(ref LogEventOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_ClientHandle = other.ClientHandle;
		m_EventId = other.EventId;
		Helper.Set<LogEventParamPair, LogEventParamPairInternal>(other.Params, ref m_Params, out m_ParamsCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Params);
	}
}
