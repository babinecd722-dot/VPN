using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct FinalizeInviteOptionsInternal : ISettable<FinalizeInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private IntPtr m_LocalUserId;

	private IntPtr m_CustomInviteId;

	private Result m_ProcessingResult;

	public void Set(ref FinalizeInviteOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.CustomInviteId, ref m_CustomInviteId);
		m_ProcessingResult = other.ProcessingResult;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_CustomInviteId);
	}
}
