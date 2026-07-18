using System;

namespace Epic.OnlineServices.Sessions;

internal struct CreateSessionModificationOptionsInternal : ISettable<CreateSessionModificationOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	private IntPtr m_BucketId;

	private uint m_MaxPlayers;

	private IntPtr m_LocalUserId;

	private int m_PresenceEnabled;

	private IntPtr m_SessionId;

	private int m_SanctionsEnabled;

	private IntPtr m_AllowedPlatformIds;

	private uint m_AllowedPlatformIdsCount;

	public void Set(ref CreateSessionModificationOptions other)
	{
		Dispose();
		m_ApiVersion = 5;
		Helper.Set(other.SessionName, ref m_SessionName);
		Helper.Set(other.BucketId, ref m_BucketId);
		m_MaxPlayers = other.MaxPlayers;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.PresenceEnabled, ref m_PresenceEnabled);
		Helper.Set(other.SessionId, ref m_SessionId);
		Helper.Set(other.SanctionsEnabled, ref m_SanctionsEnabled);
		Helper.Set(other.AllowedPlatformIds, ref m_AllowedPlatformIds, out m_AllowedPlatformIdsCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
		Helper.Dispose(ref m_BucketId);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_SessionId);
		Helper.Dispose(ref m_AllowedPlatformIds);
	}
}
