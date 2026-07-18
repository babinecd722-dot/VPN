using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct SendCustomInviteOptionsInternal : ISettable<SendCustomInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserIds;

	private uint m_TargetUserIdsCount;

	public void Set(ref SendCustomInviteOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.TargetUserIds, ref m_TargetUserIds, out m_TargetUserIdsCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserIds);
	}
}
