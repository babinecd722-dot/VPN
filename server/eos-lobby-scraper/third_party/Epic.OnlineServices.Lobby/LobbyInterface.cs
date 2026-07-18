using System;

namespace Epic.OnlineServices.Lobby;

public sealed class LobbyInterface : Handle
{
	public const int ADDNOTIFYJOINLOBBYACCEPTED_API_LATEST = 1;

	public const int ADDNOTIFYLEAVELOBBYREQUESTED_API_LATEST = 1;

	public const int ADDNOTIFYLOBBYINVITEACCEPTED_API_LATEST = 1;

	public const int ADDNOTIFYLOBBYINVITERECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYLOBBYINVITEREJECTED_API_LATEST = 1;

	public const int ADDNOTIFYLOBBYMEMBERSTATUSRECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYLOBBYMEMBERUPDATERECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYLOBBYUPDATERECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYRTCROOMCONNECTIONCHANGED_API_LATEST = 2;

	public const int ADDNOTIFYSENDLOBBYNATIVEINVITEREQUESTED_API_LATEST = 1;

	public const int ATTRIBUTEDATA_API_LATEST = 1;

	public const int ATTRIBUTE_API_LATEST = 1;

	public const int COPYLOBBYDETAILSHANDLEBYINVITEID_API_LATEST = 1;

	public const int COPYLOBBYDETAILSHANDLEBYUIEVENTID_API_LATEST = 1;

	public const int COPYLOBBYDETAILSHANDLE_API_LATEST = 1;

	public const int CREATELOBBYSEARCH_API_LATEST = 1;

	public const int CREATELOBBY_API_LATEST = 10;

	public const int DESTROYLOBBY_API_LATEST = 1;

	public const int GETCONNECTSTRING_API_LATEST = 1;

	public const int GETCONNECTSTRING_BUFFER_SIZE = 256;

	public const int GETINVITECOUNT_API_LATEST = 1;

	public const int GETINVITEIDBYINDEX_API_LATEST = 1;

	public const int GETRTCROOMNAME_API_LATEST = 1;

	public const int HARDMUTEMEMBER_API_LATEST = 1;

	public const int INVITEID_MAX_LENGTH = 64;

	public const int ISRTCROOMCONNECTED_API_LATEST = 1;

	public const int JOINLOBBYBYID_API_LATEST = 3;

	public const int JOINLOBBY_API_LATEST = 5;

	public const int JOINRTCROOM_API_LATEST = 1;

	public const int KICKMEMBER_API_LATEST = 1;

	public const int LEAVELOBBY_API_LATEST = 1;

	public const int LEAVERTCROOM_API_LATEST = 1;

	public const int LOBBYDETAILS_COPYATTRIBUTEBYINDEX_API_LATEST = 1;

	public const int LOBBYDETAILS_COPYATTRIBUTEBYKEY_API_LATEST = 1;

	public const int LOBBYDETAILS_COPYINFO_API_LATEST = 1;

	public const int LOBBYDETAILS_COPYMEMBERATTRIBUTEBYINDEX_API_LATEST = 1;

	public const int LOBBYDETAILS_COPYMEMBERATTRIBUTEBYKEY_API_LATEST = 1;

	public const int LOBBYDETAILS_COPYMEMBERINFO_API_LATEST = 1;

	public const int LOBBYDETAILS_GETATTRIBUTECOUNT_API_LATEST = 1;

	public const int LOBBYDETAILS_GETLOBBYOWNER_API_LATEST = 1;

	public const int LOBBYDETAILS_GETMEMBERATTRIBUTECOUNT_API_LATEST = 1;

	public const int LOBBYDETAILS_GETMEMBERBYINDEX_API_LATEST = 1;

	public const int LOBBYDETAILS_GETMEMBERCOUNT_API_LATEST = 1;

	public const int LOBBYDETAILS_INFO_API_LATEST = 3;

	public const int LOBBYDETAILS_MEMBERINFO_API_LATEST = 1;

	public const int LOBBYMODIFICATION_ADDATTRIBUTE_API_LATEST = 2;

	public const int LOBBYMODIFICATION_ADDMEMBERATTRIBUTE_API_LATEST = 2;

	public const int LOBBYMODIFICATION_MAX_ATTRIBUTES = 64;

	public const int LOBBYMODIFICATION_MAX_ATTRIBUTE_LENGTH = 64;

	public const int LOBBYMODIFICATION_REMOVEATTRIBUTE_API_LATEST = 1;

	public const int LOBBYMODIFICATION_REMOVEMEMBERATTRIBUTE_API_LATEST = 1;

	public const int LOBBYMODIFICATION_SETALLOWEDPLATFORMIDS_API_LATEST = 1;

	public const int LOBBYMODIFICATION_SETBUCKETID_API_LATEST = 1;

	public const int LOBBYMODIFICATION_SETINVITESALLOWED_API_LATEST = 1;

	public const int LOBBYMODIFICATION_SETMAXMEMBERS_API_LATEST = 1;

	public const int LOBBYMODIFICATION_SETPERMISSIONLEVEL_API_LATEST = 1;

	public const int LOBBYSEARCH_COPYSEARCHRESULTBYINDEX_API_LATEST = 1;

	public const int LOBBYSEARCH_FIND_API_LATEST = 1;

	public const int LOBBYSEARCH_GETSEARCHRESULTCOUNT_API_LATEST = 1;

	public const int LOBBYSEARCH_REMOVEPARAMETER_API_LATEST = 1;

	public const int LOBBYSEARCH_SETLOBBYID_API_LATEST = 1;

	public const int LOBBYSEARCH_SETMAXRESULTS_API_LATEST = 1;

	public const int LOBBYSEARCH_SETPARAMETER_API_LATEST = 1;

	public const int LOBBYSEARCH_SETTARGETUSERID_API_LATEST = 1;

	public const int LOCALRTCOPTIONS_API_LATEST = 2;

	public const int MAX_LOBBIES = 16;

	public const int MAX_LOBBYIDOVERRIDE_LENGTH = 60;

	public const int MAX_LOBBY_MEMBERS = 64;

	public const int MAX_SEARCH_RESULTS = 200;

	public const int MIN_LOBBYIDOVERRIDE_LENGTH = 4;

	public const int PARSECONNECTSTRING_API_LATEST = 1;

	public const int PARSECONNECTSTRING_BUFFER_SIZE = 256;

	public const int PROMOTEMEMBER_API_LATEST = 1;

	public const int QUERYINVITES_API_LATEST = 1;

	public const int REJECTINVITE_API_LATEST = 1;

	public static readonly Utf8String SEARCH_BUCKET_ID = "bucket";

	public static readonly Utf8String SEARCH_MINCURRENTMEMBERS = "mincurrentmembers";

	public static readonly Utf8String SEARCH_MINSLOTSAVAILABLE = "minslotsavailable";

	public const int SENDINVITE_API_LATEST = 1;

	public const int UPDATELOBBYMODIFICATION_API_LATEST = 1;

	public const int UPDATELOBBY_API_LATEST = 1;

	public LobbyInterface()
	{
	}

	public LobbyInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyJoinLobbyAccepted(ref AddNotifyJoinLobbyAcceptedOptions options, object clientData, OnJoinLobbyAcceptedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyJoinLobbyAcceptedOptionsInternal options2 = default(AddNotifyJoinLobbyAcceptedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyJoinLobbyAccepted(base.InnerHandle, ref options2, clientDataPointer, OnJoinLobbyAcceptedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLeaveLobbyRequested(ref AddNotifyLeaveLobbyRequestedOptions options, object clientData, OnLeaveLobbyRequestedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyLeaveLobbyRequestedOptionsInternal options2 = default(AddNotifyLeaveLobbyRequestedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyLeaveLobbyRequested(base.InnerHandle, ref options2, clientDataPointer, OnLeaveLobbyRequestedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLobbyInviteAccepted(ref AddNotifyLobbyInviteAcceptedOptions options, object clientData, OnLobbyInviteAcceptedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyLobbyInviteAcceptedOptionsInternal options2 = default(AddNotifyLobbyInviteAcceptedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyLobbyInviteAccepted(base.InnerHandle, ref options2, clientDataPointer, OnLobbyInviteAcceptedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLobbyInviteReceived(ref AddNotifyLobbyInviteReceivedOptions options, object clientData, OnLobbyInviteReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyLobbyInviteReceivedOptionsInternal options2 = default(AddNotifyLobbyInviteReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyLobbyInviteReceived(base.InnerHandle, ref options2, clientDataPointer, OnLobbyInviteReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLobbyInviteRejected(ref AddNotifyLobbyInviteRejectedOptions options, object clientData, OnLobbyInviteRejectedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyLobbyInviteRejectedOptionsInternal options2 = default(AddNotifyLobbyInviteRejectedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyLobbyInviteRejected(base.InnerHandle, ref options2, clientDataPointer, OnLobbyInviteRejectedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLobbyMemberStatusReceived(ref AddNotifyLobbyMemberStatusReceivedOptions options, object clientData, OnLobbyMemberStatusReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyLobbyMemberStatusReceivedOptionsInternal options2 = default(AddNotifyLobbyMemberStatusReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyLobbyMemberStatusReceived(base.InnerHandle, ref options2, clientDataPointer, OnLobbyMemberStatusReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLobbyMemberUpdateReceived(ref AddNotifyLobbyMemberUpdateReceivedOptions options, object clientData, OnLobbyMemberUpdateReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyLobbyMemberUpdateReceivedOptionsInternal options2 = default(AddNotifyLobbyMemberUpdateReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyLobbyMemberUpdateReceived(base.InnerHandle, ref options2, clientDataPointer, OnLobbyMemberUpdateReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLobbyUpdateReceived(ref AddNotifyLobbyUpdateReceivedOptions options, object clientData, OnLobbyUpdateReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyLobbyUpdateReceivedOptionsInternal options2 = default(AddNotifyLobbyUpdateReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyLobbyUpdateReceived(base.InnerHandle, ref options2, clientDataPointer, OnLobbyUpdateReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyRTCRoomConnectionChanged(ref AddNotifyRTCRoomConnectionChangedOptions options, object clientData, OnRTCRoomConnectionChangedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyRTCRoomConnectionChangedOptionsInternal options2 = default(AddNotifyRTCRoomConnectionChangedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifyRTCRoomConnectionChanged(base.InnerHandle, ref options2, clientDataPointer, OnRTCRoomConnectionChangedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifySendLobbyNativeInviteRequested(ref AddNotifySendLobbyNativeInviteRequestedOptions options, object clientData, OnSendLobbyNativeInviteRequestedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifySendLobbyNativeInviteRequestedOptionsInternal options2 = default(AddNotifySendLobbyNativeInviteRequestedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Lobby_AddNotifySendLobbyNativeInviteRequested(base.InnerHandle, ref options2, clientDataPointer, OnSendLobbyNativeInviteRequestedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result CopyLobbyDetailsHandle(ref CopyLobbyDetailsHandleOptions options, out LobbyDetails outLobbyDetailsHandle)
	{
		CopyLobbyDetailsHandleOptionsInternal options2 = default(CopyLobbyDetailsHandleOptionsInternal);
		options2.Set(ref options);
		IntPtr outLobbyDetailsHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Lobby_CopyLobbyDetailsHandle(base.InnerHandle, ref options2, out outLobbyDetailsHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outLobbyDetailsHandle2, out outLobbyDetailsHandle);
		return result;
	}

	public Result CopyLobbyDetailsHandleByInviteId(ref CopyLobbyDetailsHandleByInviteIdOptions options, out LobbyDetails outLobbyDetailsHandle)
	{
		CopyLobbyDetailsHandleByInviteIdOptionsInternal options2 = default(CopyLobbyDetailsHandleByInviteIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outLobbyDetailsHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Lobby_CopyLobbyDetailsHandleByInviteId(base.InnerHandle, ref options2, out outLobbyDetailsHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outLobbyDetailsHandle2, out outLobbyDetailsHandle);
		return result;
	}

	public Result CopyLobbyDetailsHandleByUiEventId(ref CopyLobbyDetailsHandleByUiEventIdOptions options, out LobbyDetails outLobbyDetailsHandle)
	{
		CopyLobbyDetailsHandleByUiEventIdOptionsInternal options2 = default(CopyLobbyDetailsHandleByUiEventIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outLobbyDetailsHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Lobby_CopyLobbyDetailsHandleByUiEventId(base.InnerHandle, ref options2, out outLobbyDetailsHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outLobbyDetailsHandle2, out outLobbyDetailsHandle);
		return result;
	}

	public void CreateLobby(ref CreateLobbyOptions options, object clientData, OnCreateLobbyCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		CreateLobbyOptionsInternal options2 = default(CreateLobbyOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_CreateLobby(base.InnerHandle, ref options2, clientDataPointer, OnCreateLobbyCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result CreateLobbySearch(ref CreateLobbySearchOptions options, out LobbySearch outLobbySearchHandle)
	{
		CreateLobbySearchOptionsInternal options2 = default(CreateLobbySearchOptionsInternal);
		options2.Set(ref options);
		IntPtr outLobbySearchHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Lobby_CreateLobbySearch(base.InnerHandle, ref options2, out outLobbySearchHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outLobbySearchHandle2, out outLobbySearchHandle);
		return result;
	}

	public void DestroyLobby(ref DestroyLobbyOptions options, object clientData, OnDestroyLobbyCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		DestroyLobbyOptionsInternal options2 = default(DestroyLobbyOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_DestroyLobby(base.InnerHandle, ref options2, clientDataPointer, OnDestroyLobbyCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result GetConnectString(ref GetConnectStringOptions options, out Utf8String outBuffer)
	{
		GetConnectStringOptionsInternal options2 = default(GetConnectStringOptionsInternal);
		options2.Set(ref options);
		uint inOutBufferLength = 256u;
		IntPtr value = Helper.AddAllocation(inOutBufferLength);
		Result result = Bindings.EOS_Lobby_GetConnectString(base.InnerHandle, ref options2, value, ref inOutBufferLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outBuffer);
		Helper.Dispose(ref value);
		return result;
	}

	public uint GetInviteCount(ref GetInviteCountOptions options)
	{
		GetInviteCountOptionsInternal options2 = default(GetInviteCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Lobby_GetInviteCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result GetInviteIdByIndex(ref GetInviteIdByIndexOptions options, out Utf8String outBuffer)
	{
		GetInviteIdByIndexOptionsInternal options2 = default(GetInviteIdByIndexOptionsInternal);
		options2.Set(ref options);
		int inOutBufferLength = 65;
		IntPtr value = Helper.AddAllocation(inOutBufferLength);
		Result result = Bindings.EOS_Lobby_GetInviteIdByIndex(base.InnerHandle, ref options2, value, ref inOutBufferLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outBuffer);
		Helper.Dispose(ref value);
		return result;
	}

	public Result GetRTCRoomName(ref GetRTCRoomNameOptions options, out Utf8String outBuffer)
	{
		GetRTCRoomNameOptionsInternal options2 = default(GetRTCRoomNameOptionsInternal);
		options2.Set(ref options);
		uint inOutBufferLength = 256u;
		IntPtr value = Helper.AddAllocation(inOutBufferLength);
		Result result = Bindings.EOS_Lobby_GetRTCRoomName(base.InnerHandle, ref options2, value, ref inOutBufferLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outBuffer);
		Helper.Dispose(ref value);
		return result;
	}

	public void HardMuteMember(ref HardMuteMemberOptions options, object clientData, OnHardMuteMemberCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		HardMuteMemberOptionsInternal options2 = default(HardMuteMemberOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_HardMuteMember(base.InnerHandle, ref options2, clientDataPointer, OnHardMuteMemberCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result IsRTCRoomConnected(ref IsRTCRoomConnectedOptions options, out bool outIsConnected)
	{
		IsRTCRoomConnectedOptionsInternal options2 = default(IsRTCRoomConnectedOptionsInternal);
		options2.Set(ref options);
		int outIsConnected2 = 0;
		Result result = Bindings.EOS_Lobby_IsRTCRoomConnected(base.InnerHandle, ref options2, out outIsConnected2);
		Helper.Dispose(ref options2);
		Helper.Get(outIsConnected2, out outIsConnected);
		return result;
	}

	public void JoinLobby(ref JoinLobbyOptions options, object clientData, OnJoinLobbyCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		JoinLobbyOptionsInternal options2 = default(JoinLobbyOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_JoinLobby(base.InnerHandle, ref options2, clientDataPointer, OnJoinLobbyCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void JoinLobbyById(ref JoinLobbyByIdOptions options, object clientData, OnJoinLobbyByIdCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		JoinLobbyByIdOptionsInternal options2 = default(JoinLobbyByIdOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_JoinLobbyById(base.InnerHandle, ref options2, clientDataPointer, OnJoinLobbyByIdCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void JoinRTCRoom(ref JoinRTCRoomOptions options, object clientData, OnJoinRTCRoomCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		JoinRTCRoomOptionsInternal options2 = default(JoinRTCRoomOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_JoinRTCRoom(base.InnerHandle, ref options2, clientDataPointer, OnJoinRTCRoomCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void KickMember(ref KickMemberOptions options, object clientData, OnKickMemberCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		KickMemberOptionsInternal options2 = default(KickMemberOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_KickMember(base.InnerHandle, ref options2, clientDataPointer, OnKickMemberCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void LeaveLobby(ref LeaveLobbyOptions options, object clientData, OnLeaveLobbyCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		LeaveLobbyOptionsInternal options2 = default(LeaveLobbyOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_LeaveLobby(base.InnerHandle, ref options2, clientDataPointer, OnLeaveLobbyCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void LeaveRTCRoom(ref LeaveRTCRoomOptions options, object clientData, OnLeaveRTCRoomCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		LeaveRTCRoomOptionsInternal options2 = default(LeaveRTCRoomOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_LeaveRTCRoom(base.InnerHandle, ref options2, clientDataPointer, OnLeaveRTCRoomCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result ParseConnectString(ref ParseConnectStringOptions options, out Utf8String outBuffer)
	{
		ParseConnectStringOptionsInternal options2 = default(ParseConnectStringOptionsInternal);
		options2.Set(ref options);
		uint inOutBufferLength = 256u;
		IntPtr value = Helper.AddAllocation(inOutBufferLength);
		Result result = Bindings.EOS_Lobby_ParseConnectString(base.InnerHandle, ref options2, value, ref inOutBufferLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outBuffer);
		Helper.Dispose(ref value);
		return result;
	}

	public void PromoteMember(ref PromoteMemberOptions options, object clientData, OnPromoteMemberCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		PromoteMemberOptionsInternal options2 = default(PromoteMemberOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_PromoteMember(base.InnerHandle, ref options2, clientDataPointer, OnPromoteMemberCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryInvites(ref QueryInvitesOptions options, object clientData, OnQueryInvitesCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryInvitesOptionsInternal options2 = default(QueryInvitesOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_QueryInvites(base.InnerHandle, ref options2, clientDataPointer, OnQueryInvitesCallbackInternalImplementation.Delegate);
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
		Bindings.EOS_Lobby_RejectInvite(base.InnerHandle, ref options2, clientDataPointer, OnRejectInviteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyJoinLobbyAccepted(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyJoinLobbyAccepted(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLeaveLobbyRequested(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyLeaveLobbyRequested(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLobbyInviteAccepted(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyLobbyInviteAccepted(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLobbyInviteReceived(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyLobbyInviteReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLobbyInviteRejected(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyLobbyInviteRejected(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLobbyMemberStatusReceived(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyLobbyMemberStatusReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLobbyMemberUpdateReceived(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyLobbyMemberUpdateReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLobbyUpdateReceived(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyLobbyUpdateReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyRTCRoomConnectionChanged(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifyRTCRoomConnectionChanged(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifySendLobbyNativeInviteRequested(ulong inId)
	{
		Bindings.EOS_Lobby_RemoveNotifySendLobbyNativeInviteRequested(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
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
		Bindings.EOS_Lobby_SendInvite(base.InnerHandle, ref options2, clientDataPointer, OnSendInviteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateLobby(ref UpdateLobbyOptions options, object clientData, OnUpdateLobbyCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateLobbyOptionsInternal options2 = default(UpdateLobbyOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Lobby_UpdateLobby(base.InnerHandle, ref options2, clientDataPointer, OnUpdateLobbyCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result UpdateLobbyModification(ref UpdateLobbyModificationOptions options, out LobbyModification outLobbyModificationHandle)
	{
		UpdateLobbyModificationOptionsInternal options2 = default(UpdateLobbyModificationOptionsInternal);
		options2.Set(ref options);
		IntPtr outLobbyModificationHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Lobby_UpdateLobbyModification(base.InnerHandle, ref options2, out outLobbyModificationHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outLobbyModificationHandle2, out outLobbyModificationHandle);
		return result;
	}
}
