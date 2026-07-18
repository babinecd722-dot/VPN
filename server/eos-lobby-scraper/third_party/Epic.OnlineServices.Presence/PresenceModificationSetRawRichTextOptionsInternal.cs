using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceModificationSetRawRichTextOptionsInternal : ISettable<PresenceModificationSetRawRichTextOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_RichText;

	public void Set(ref PresenceModificationSetRawRichTextOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.RichText, ref m_RichText);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_RichText);
	}
}
