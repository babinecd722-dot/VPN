using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionDetailsSettingsInternal : IGettable<SessionDetailsSettings>
{
	private int m_ApiVersion;

	private IntPtr m_BucketId;

	private uint m_NumPublicConnections;

	private int m_AllowJoinInProgress;

	private OnlineSessionPermissionLevel m_PermissionLevel;

	private int m_InvitesAllowed;

	private int m_SanctionsEnabled;

	private IntPtr m_AllowedPlatformIds;

	private uint m_AllowedPlatformIdsCount;

	public void Get(out SessionDetailsSettings other)
	{
		other = default(SessionDetailsSettings);
		Helper.Get(m_BucketId, out Utf8String to);
		other.BucketId = to;
		other.NumPublicConnections = m_NumPublicConnections;
		Helper.Get(m_AllowJoinInProgress, out bool to2);
		other.AllowJoinInProgress = to2;
		other.PermissionLevel = m_PermissionLevel;
		Helper.Get(m_InvitesAllowed, out bool to3);
		other.InvitesAllowed = to3;
		Helper.Get(m_SanctionsEnabled, out bool to4);
		other.SanctionsEnabled = to4;
		Helper.Get(m_AllowedPlatformIds, out uint[] to5, m_AllowedPlatformIdsCount, isArrayItemAllocated: false);
		other.AllowedPlatformIds = to5;
	}
}
