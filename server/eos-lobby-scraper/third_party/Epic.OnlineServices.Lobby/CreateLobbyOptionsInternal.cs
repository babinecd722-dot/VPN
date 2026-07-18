using System;

namespace Epic.OnlineServices.Lobby;

internal struct CreateLobbyOptionsInternal : ISettable<CreateLobbyOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_MaxLobbyMembers;

	private LobbyPermissionLevel m_PermissionLevel;

	private int m_PresenceEnabled;

	private int m_AllowInvites;

	private IntPtr m_BucketId;

	private int m_DisableHostMigration;

	private int m_EnableRTCRoom;

	private IntPtr m_LocalRTCOptions;

	private IntPtr m_LobbyId;

	private int m_EnableJoinById;

	private int m_RejoinAfterKickRequiresInvite;

	private IntPtr m_AllowedPlatformIds;

	private uint m_AllowedPlatformIdsCount;

	private int m_CrossplayOptOut;

	private LobbyRTCRoomJoinActionType m_RTCRoomJoinActionType;

	public void Set(ref CreateLobbyOptions other)
	{
		Dispose();
		m_ApiVersion = 10;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_MaxLobbyMembers = other.MaxLobbyMembers;
		m_PermissionLevel = other.PermissionLevel;
		Helper.Set(other.PresenceEnabled, ref m_PresenceEnabled);
		Helper.Set(other.AllowInvites, ref m_AllowInvites);
		Helper.Set(other.BucketId, ref m_BucketId);
		Helper.Set(other.DisableHostMigration, ref m_DisableHostMigration);
		Helper.Set(other.EnableRTCRoom, ref m_EnableRTCRoom);
		Helper.Set<LocalRTCOptions, LocalRTCOptionsInternal>(other.LocalRTCOptions, ref m_LocalRTCOptions);
		Helper.Set(other.LobbyId, ref m_LobbyId);
		Helper.Set(other.EnableJoinById, ref m_EnableJoinById);
		Helper.Set(other.RejoinAfterKickRequiresInvite, ref m_RejoinAfterKickRequiresInvite);
		Helper.Set(other.AllowedPlatformIds, ref m_AllowedPlatformIds, out m_AllowedPlatformIdsCount, isArrayItemAllocated: false);
		Helper.Set(other.CrossplayOptOut, ref m_CrossplayOptOut);
		m_RTCRoomJoinActionType = other.RTCRoomJoinActionType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_BucketId);
		Helper.Dispose(ref m_LocalRTCOptions);
		Helper.Dispose(ref m_LobbyId);
		Helper.Dispose(ref m_AllowedPlatformIds);
	}
}
