using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceModificationSetTemplateIdOptionsInternal : ISettable<PresenceModificationSetTemplateIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TemplateId;

	public void Set(ref PresenceModificationSetTemplateIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.TemplateId, ref m_TemplateId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TemplateId);
	}
}
