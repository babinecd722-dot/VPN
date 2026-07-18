using System;

namespace Epic.OnlineServices.Friends;

public sealed class FriendsInterface : Handle
{
	public const int ACCEPTINVITE_API_LATEST = 1;

	public const int ADDNOTIFYBLOCKEDUSERSUPDATE_API_LATEST = 1;

	public const int ADDNOTIFYFRIENDSUPDATE_API_LATEST = 1;

	public const int GETBLOCKEDUSERATINDEX_API_LATEST = 1;

	public const int GETBLOCKEDUSERSCOUNT_API_LATEST = 1;

	public const int GETFRIENDATINDEX_API_LATEST = 1;

	public const int GETFRIENDSCOUNT_API_LATEST = 1;

	public const int GETSTATUS_API_LATEST = 1;

	public const int QUERYFRIENDS_API_LATEST = 1;

	public const int REJECTINVITE_API_LATEST = 1;

	public const int SENDINVITE_API_LATEST = 1;

	public FriendsInterface()
	{
	}

	public FriendsInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public void AcceptInvite(ref AcceptInviteOptions options, object clientData, OnAcceptInviteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AcceptInviteOptionsInternal options2 = default(AcceptInviteOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Friends_AcceptInvite(base.InnerHandle, ref options2, clientDataPointer, OnAcceptInviteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public ulong AddNotifyBlockedUsersUpdate(ref AddNotifyBlockedUsersUpdateOptions options, object clientData, OnBlockedUsersUpdateCallback blockedUsersUpdateHandler)
	{
		if (blockedUsersUpdateHandler == null)
		{
			throw new ArgumentNullException("blockedUsersUpdateHandler");
		}
		AddNotifyBlockedUsersUpdateOptionsInternal options2 = default(AddNotifyBlockedUsersUpdateOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, blockedUsersUpdateHandler);
		ulong num = Bindings.EOS_Friends_AddNotifyBlockedUsersUpdate(base.InnerHandle, ref options2, clientDataPointer, OnBlockedUsersUpdateCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyFriendsUpdate(ref AddNotifyFriendsUpdateOptions options, object clientData, OnFriendsUpdateCallback friendsUpdateHandler)
	{
		if (friendsUpdateHandler == null)
		{
			throw new ArgumentNullException("friendsUpdateHandler");
		}
		AddNotifyFriendsUpdateOptionsInternal options2 = default(AddNotifyFriendsUpdateOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, friendsUpdateHandler);
		ulong num = Bindings.EOS_Friends_AddNotifyFriendsUpdate(base.InnerHandle, ref options2, clientDataPointer, OnFriendsUpdateCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public EpicAccountId GetBlockedUserAtIndex(ref GetBlockedUserAtIndexOptions options)
	{
		GetBlockedUserAtIndexOptionsInternal options2 = default(GetBlockedUserAtIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr intPtr = Bindings.EOS_Friends_GetBlockedUserAtIndex(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(intPtr, out EpicAccountId to);
		return to;
	}

	public int GetBlockedUsersCount(ref GetBlockedUsersCountOptions options)
	{
		GetBlockedUsersCountOptionsInternal options2 = default(GetBlockedUsersCountOptionsInternal);
		options2.Set(ref options);
		int result = Bindings.EOS_Friends_GetBlockedUsersCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public EpicAccountId GetFriendAtIndex(ref GetFriendAtIndexOptions options)
	{
		GetFriendAtIndexOptionsInternal options2 = default(GetFriendAtIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr intPtr = Bindings.EOS_Friends_GetFriendAtIndex(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(intPtr, out EpicAccountId to);
		return to;
	}

	public int GetFriendsCount(ref GetFriendsCountOptions options)
	{
		GetFriendsCountOptionsInternal options2 = default(GetFriendsCountOptionsInternal);
		options2.Set(ref options);
		int result = Bindings.EOS_Friends_GetFriendsCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public FriendsStatus GetStatus(ref GetStatusOptions options)
	{
		GetStatusOptionsInternal options2 = default(GetStatusOptionsInternal);
		options2.Set(ref options);
		FriendsStatus result = Bindings.EOS_Friends_GetStatus(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryFriends(ref QueryFriendsOptions options, object clientData, OnQueryFriendsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryFriendsOptionsInternal options2 = default(QueryFriendsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Friends_QueryFriends(base.InnerHandle, ref options2, clientDataPointer, OnQueryFriendsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RejectInvite(ref RejectInviteOptions options, object clientData, OnRejectInviteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		RejectInviteOptionsInternal options2 = default(RejectInviteOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Friends_RejectInvite(base.InnerHandle, ref options2, clientDataPointer, OnRejectInviteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyBlockedUsersUpdate(ulong notificationId)
	{
		Bindings.EOS_Friends_RemoveNotifyBlockedUsersUpdate(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyFriendsUpdate(ulong notificationId)
	{
		Bindings.EOS_Friends_RemoveNotifyFriendsUpdate(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void SendInvite(ref SendInviteOptions options, object clientData, OnSendInviteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SendInviteOptionsInternal options2 = default(SendInviteOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Friends_SendInvite(base.InnerHandle, ref options2, clientDataPointer, OnSendInviteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
