using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsInfoInternal : IGettable<LobbyDetailsInfo>
{
	private int m_ApiVersion;

	private IntPtr m_LobbyId;

	private IntPtr m_LobbyOwnerUserId;

	private LobbyPermissionLevel m_PermissionLevel;

	private uint m_AvailableSlots;

	private uint m_MaxMembers;

	private int m_AllowInvites;

	private IntPtr m_BucketId;

	private int m_AllowHostMigration;

	private int m_RTCRoomEnabled;

	private int m_AllowJoinById;

	private int m_RejoinAfterKickRequiresInvite;

	private int m_PresenceEnabled;

	private IntPtr m_AllowedPlatformIds;

	private uint m_AllowedPlatformIdsCount;

	public void Get(out LobbyDetailsInfo other)
	{
		other = default(LobbyDetailsInfo);
		Helper.Get(m_LobbyId, out Utf8String to);
		other.LobbyId = to;
		Helper.Get(m_LobbyOwnerUserId, out ProductUserId to2);
		other.LobbyOwnerUserId = to2;
		other.PermissionLevel = m_PermissionLevel;
		other.AvailableSlots = m_AvailableSlots;
		other.MaxMembers = m_MaxMembers;
		Helper.Get(m_AllowInvites, out bool to3);
		other.AllowInvites = to3;
		Helper.Get(m_BucketId, out Utf8String to4);
		other.BucketId = to4;
		Helper.Get(m_AllowHostMigration, out bool to5);
		other.AllowHostMigration = to5;
		Helper.Get(m_RTCRoomEnabled, out bool to6);
		other.RTCRoomEnabled = to6;
		Helper.Get(m_AllowJoinById, out bool to7);
		other.AllowJoinById = to7;
		Helper.Get(m_RejoinAfterKickRequiresInvite, out bool to8);
		other.RejoinAfterKickRequiresInvite = to8;
		Helper.Get(m_PresenceEnabled, out bool to9);
		other.PresenceEnabled = to9;
		Helper.Get(m_AllowedPlatformIds, out uint[] to10, m_AllowedPlatformIdsCount, isArrayItemAllocated: false);
		other.AllowedPlatformIds = to10;
	}
}
