using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceModificationSetTemplateDataOptionsInternal : ISettable<PresenceModificationSetTemplateDataOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Key;

	private PresenceModificationSetTemplateDataOptionsValueInternal m_Value;

	private PresenceModificationTemplateType m_ValueType;

	public void Set(ref PresenceModificationSetTemplateDataOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Key, ref m_Key);
		Helper.Set<PresenceModificationSetTemplateDataOptionsValue, PresenceModificationSetTemplateDataOptionsValueInternal>(other.Value, ref m_Value);
		m_ValueType = other.Value.ValueType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Key);
		Helper.Dispose(ref m_Value);
	}
}
