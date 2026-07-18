using System;

namespace Epic.OnlineServices.Sanctions;

internal struct CreatePlayerSanctionAppealOptionsInternal : ISettable<CreatePlayerSanctionAppealOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private SanctionAppealReason m_Reason;

	private IntPtr m_ReferenceId;

	public void Set(ref CreatePlayerSanctionAppealOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_Reason = other.Reason;
		Helper.Set(other.ReferenceId, ref m_ReferenceId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ReferenceId);
	}
}
