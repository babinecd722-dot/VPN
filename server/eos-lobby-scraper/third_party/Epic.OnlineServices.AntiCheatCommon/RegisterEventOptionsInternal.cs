using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct RegisterEventOptionsInternal : ISettable<RegisterEventOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_EventId;

	private IntPtr m_EventName;

	private AntiCheatCommonEventType m_EventType;

	private uint m_ParamDefsCount;

	private IntPtr m_ParamDefs;

	public void Set(ref RegisterEventOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_EventId = other.EventId;
		Helper.Set(other.EventName, ref m_EventName);
		m_EventType = other.EventType;
		Helper.Set<RegisterEventParamDef, RegisterEventParamDefInternal>(other.ParamDefs, ref m_ParamDefs, out m_ParamDefsCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_EventName);
		Helper.Dispose(ref m_ParamDefs);
	}
}
