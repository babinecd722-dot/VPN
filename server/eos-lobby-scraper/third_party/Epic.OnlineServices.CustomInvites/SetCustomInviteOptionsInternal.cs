using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct SetCustomInviteOptionsInternal : ISettable<SetCustomInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_Payload;

	public void Set(ref SetCustomInviteOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.Payload, ref m_Payload);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_Payload);
	}
}
