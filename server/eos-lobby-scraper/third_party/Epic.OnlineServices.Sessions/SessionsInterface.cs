using System;

namespace Epic.OnlineServices.Sessions;

public sealed class SessionsInterface : Handle
{
	public const int ACTIVESESSION_COPYINFO_API_LATEST = 1;

	public const int ACTIVESESSION_GETREGISTEREDPLAYERBYINDEX_API_LATEST = 1;

	public const int ACTIVESESSION_GETREGISTEREDPLAYERCOUNT_API_LATEST = 1;

	public const int ACTIVESESSION_INFO_API_LATEST = 1;

	public const int ADDNOTIFYJOINSESSIONACCEPTED_API_LATEST = 1;

	public const int ADDNOTIFYLEAVESESSIONREQUESTED_API_LATEST = 1;

	public const int ADDNOTIFYSENDSESSIONNATIVEINVITEREQUESTED_API_LATEST = 1;

	public const int ADDNOTIFYSESSIONINVITEACCEPTED_API_LATEST = 1;

	public const int ADDNOTIFYSESSIONINVITERECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYSESSIONINVITEREJECTED_API_LATEST = 1;

	public const int ATTRIBUTEDATA_API_LATEST = 1;

	public const int COPYACTIVESESSIONHANDLE_API_LATEST = 1;

	public const int COPYSESSIONHANDLEBYINVITEID_API_LATEST = 1;

	public const int COPYSESSIONHANDLEBYUIEVENTID_API_LATEST = 1;

	public const int COPYSESSIONHANDLEFORPRESENCE_API_LATEST = 1;

	public const int CREATESESSIONMODIFICATION_API_LATEST = 5;

	public const int CREATESESSIONSEARCH_API_LATEST = 1;

	public const int DESTROYSESSION_API_LATEST = 1;

	public const int DUMPSESSIONSTATE_API_LATEST = 1;

	public const int ENDSESSION_API_LATEST = 1;

	public const int GETINVITECOUNT_API_LATEST = 1;

	public const int GETINVITEIDBYINDEX_API_LATEST = 1;

	public const int INVITEID_MAX_LENGTH = 64;

	public const int ISUSERINSESSION_API_LATEST = 1;

	public const int JOINSESSION_API_LATEST = 2;

	public const int MAXREGISTEREDPLAYERS = 1000;

	public const int MAX_SEARCH_RESULTS = 200;

	public const int QUERYINVITES_API_LATEST = 1;

	public const int REGISTERPLAYERS_API_LATEST = 3;

	public const int REJECTINVITE_API_LATEST = 1;

	public static readonly Utf8String SEARCH_BUCKET_ID = "bucket";

	public static readonly Utf8String SEARCH_EMPTY_SERVERS_ONLY = "emptyonly";

	public static readonly Utf8String SEARCH_MINSLOTSAVAILABLE = "minslotsavailable";

	public static readonly Utf8String SEARCH_NONEMPTY_SERVERS_ONLY = "nonemptyonly";

	public const int SENDINVITE_API_LATEST = 1;

	public const int SESSIONATTRIBUTEDATA_API_LATEST = 1;

	public const int SESSIONATTRIBUTE_API_LATEST = 1;

	public const int SESSIONDETAILS_ATTRIBUTE_API_LATEST = 1;

	public const int SESSIONDETAILS_COPYINFO_API_LATEST = 1;

	public const int SESSIONDETAILS_COPYSESSIONATTRIBUTEBYINDEX_API_LATEST = 1;

	public const int SESSIONDETAILS_COPYSESSIONATTRIBUTEBYKEY_API_LATEST = 1;

	public const int SESSIONDETAILS_GETSESSIONATTRIBUTECOUNT_API_LATEST = 1;

	public const int SESSIONDETAILS_INFO_API_LATEST = 2;

	public const int SESSIONDETAILS_SETTINGS_API_LATEST = 4;

	public const int SESSIONMODIFICATION_ADDATTRIBUTE_API_LATEST = 2;

	public const int SESSIONMODIFICATION_MAX_SESSIONIDOVERRIDE_LENGTH = 64;

	public const int SESSIONMODIFICATION_MAX_SESSION_ATTRIBUTES = 64;

	public const int SESSIONMODIFICATION_MAX_SESSION_ATTRIBUTE_LENGTH = 64;

	public const int SESSIONMODIFICATION_MIN_SESSIONIDOVERRIDE_LENGTH = 16;

	public const int SESSIONMODIFICATION_REMOVEATTRIBUTE_API_LATEST = 1;

	public const int SESSIONMODIFICATION_SETALLOWEDPLATFORMIDS_API_LATEST = 1;

	public const int SESSIONMODIFICATION_SETBUCKETID_API_LATEST = 1;

	public const int SESSIONMODIFICATION_SETHOSTADDRESS_API_LATEST = 1;

	public const int SESSIONMODIFICATION_SETINVITESALLOWED_API_LATEST = 1;

	public const int SESSIONMODIFICATION_SETJOININPROGRESSALLOWED_API_LATEST = 1;

	public const int SESSIONMODIFICATION_SETMAXPLAYERS_API_LATEST = 1;

	public const int SESSIONMODIFICATION_SETPERMISSIONLEVEL_API_LATEST = 1;

	public const int SESSIONSEARCH_COPYSEARCHRESULTBYINDEX_API_LATEST = 1;

	public const int SESSIONSEARCH_FIND_API_LATEST = 2;

	public const int SESSIONSEARCH_GETSEARCHRESULTCOUNT_API_LATEST = 1;

	public const int SESSIONSEARCH_REMOVEPARAMETER_API_LATEST = 1;

	public const int SESSIONSEARCH_SETMAXSEARCHRESULTS_API_LATEST = 1;

	public const int SESSIONSEARCH_SETPARAMETER_API_LATEST = 1;

	public const int SESSIONSEARCH_SETSESSIONID_API_LATEST = 1;

	public const int SESSIONSEARCH_SETTARGETUSERID_API_LATEST = 1;

	public const int STARTSESSION_API_LATEST = 1;

	public const int UNREGISTERPLAYERS_API_LATEST = 2;

	public const int UPDATESESSIONMODIFICATION_API_LATEST = 1;

	public const int UPDATESESSION_API_LATEST = 1;

	public SessionsInterface()
	{
	}

	public SessionsInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyJoinSessionAccepted(ref AddNotifyJoinSessionAcceptedOptions options, object clientData, OnJoinSessionAcceptedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyJoinSessionAcceptedOptionsInternal options2 = default(AddNotifyJoinSessionAcceptedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Sessions_AddNotifyJoinSessionAccepted(base.InnerHandle, ref options2, clientDataPointer, OnJoinSessionAcceptedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLeaveSessionRequested(ref AddNotifyLeaveSessionRequestedOptions options, object clientData, OnLeaveSessionRequestedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyLeaveSessionRequestedOptionsInternal options2 = default(AddNotifyLeaveSessionRequestedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Sessions_AddNotifyLeaveSessionRequested(base.InnerHandle, ref options2, clientDataPointer, OnLeaveSessionRequestedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifySendSessionNativeInviteRequested(ref AddNotifySendSessionNativeInviteRequestedOptions options, object clientData, OnSendSessionNativeInviteRequestedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifySendSessionNativeInviteRequestedOptionsInternal options2 = default(AddNotifySendSessionNativeInviteRequestedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Sessions_AddNotifySendSessionNativeInviteRequested(base.InnerHandle, ref options2, clientDataPointer, OnSendSessionNativeInviteRequestedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifySessionInviteAccepted(ref AddNotifySessionInviteAcceptedOptions options, object clientData, OnSessionInviteAcceptedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifySessionInviteAcceptedOptionsInternal options2 = default(AddNotifySessionInviteAcceptedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Sessions_AddNotifySessionInviteAccepted(base.InnerHandle, ref options2, clientDataPointer, OnSessionInviteAcceptedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifySessionInviteReceived(ref AddNotifySessionInviteReceivedOptions options, object clientData, OnSessionInviteReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifySessionInviteReceivedOptionsInternal options2 = default(AddNotifySessionInviteReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Sessions_AddNotifySessionInviteReceived(base.InnerHandle, ref options2, clientDataPointer, OnSessionInviteReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifySessionInviteRejected(ref AddNotifySessionInviteRejectedOptions options, object clientData, OnSessionInviteRejectedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifySessionInviteRejectedOptionsInternal options2 = default(AddNotifySessionInviteRejectedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Sessions_AddNotifySessionInviteRejected(base.InnerHandle, ref options2, clientDataPointer, OnSessionInviteRejectedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result CopyActiveSessionHandle(ref CopyActiveSessionHandleOptions options, out ActiveSession outSessionHandle)
	{
		CopyActiveSessionHandleOptionsInternal options2 = default(CopyActiveSessionHandleOptionsInternal);
		options2.Set(ref options);
		IntPtr outSessionHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Sessions_CopyActiveSessionHandle(base.InnerHandle, ref options2, out outSessionHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outSessionHandle2, out outSessionHandle);
		return result;
	}

	public Result CopySessionHandleByInviteId(ref CopySessionHandleByInviteIdOptions options, out SessionDetails outSessionHandle)
	{
		CopySessionHandleByInviteIdOptionsInternal options2 = default(CopySessionHandleByInviteIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outSessionHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Sessions_CopySessionHandleByInviteId(base.InnerHandle, ref options2, out outSessionHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outSessionHandle2, out outSessionHandle);
		return result;
	}

	public Result CopySessionHandleByUiEventId(ref CopySessionHandleByUiEventIdOptions options, out SessionDetails outSessionHandle)
	{
		CopySessionHandleByUiEventIdOptionsInternal options2 = default(CopySessionHandleByUiEventIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outSessionHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Sessions_CopySessionHandleByUiEventId(base.InnerHandle, ref options2, out outSessionHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outSessionHandle2, out outSessionHandle);
		return result;
	}

	public Result CopySessionHandleForPresence(ref CopySessionHandleForPresenceOptions options, out SessionDetails outSessionHandle)
	{
		CopySessionHandleForPresenceOptionsInternal options2 = default(CopySessionHandleForPresenceOptionsInternal);
		options2.Set(ref options);
		IntPtr outSessionHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Sessions_CopySessionHandleForPresence(base.InnerHandle, ref options2, out outSessionHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outSessionHandle2, out outSessionHandle);
		return result;
	}

	public Result CreateSessionModification(ref CreateSessionModificationOptions options, out SessionModification outSessionModificationHandle)
	{
		CreateSessionModificationOptionsInternal options2 = default(CreateSessionModificationOptionsInternal);
		options2.Set(ref options);
		IntPtr outSessionModificationHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Sessions_CreateSessionModification(base.InnerHandle, ref options2, out outSessionModificationHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outSessionModificationHandle2, out outSessionModificationHandle);
		return result;
	}

	public Result CreateSessionSearch(ref CreateSessionSearchOptions options, out SessionSearch outSessionSearchHandle)
	{
		CreateSessionSearchOptionsInternal options2 = default(CreateSessionSearchOptionsInternal);
		options2.Set(ref options);
		IntPtr outSessionSearchHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Sessions_CreateSessionSearch(base.InnerHandle, ref options2, out outSessionSearchHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outSessionSearchHandle2, out outSessionSearchHandle);
		return result;
	}

	public void DestroySession(ref DestroySessionOptions options, object clientData, OnDestroySessionCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		DestroySessionOptionsInternal options2 = default(DestroySessionOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sessions_DestroySession(base.InnerHandle, ref options2, clientDataPointer, OnDestroySessionCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result DumpSessionState(ref DumpSessionStateOptions options)
	{
		DumpSessionStateOptionsInternal options2 = default(DumpSessionStateOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_Sessions_DumpSessionState(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void EndSession(ref EndSessionOptions options, object clientData, OnEndSessionCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		EndSessionOptionsInternal options2 = default(EndSessionOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sessions_EndSession(base.InnerHandle, ref options2, clientDataPointer, OnEndSessionCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public uint GetInviteCount(ref GetInviteCountOptions options)
	{
		GetInviteCountOptionsInternal options2 = default(GetInviteCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Sessions_GetInviteCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result GetInviteIdByIndex(ref GetInviteIdByIndexOptions options, out Utf8String outBuffer)
	{
		GetInviteIdByIndexOptionsInternal options2 = default(GetInviteIdByIndexOptionsInternal);
		options2.Set(ref options);
		int inOutBufferLength = 65;
		IntPtr value = Helper.AddAllocation(inOutBufferLength);
		Result result = Bindings.EOS_Sessions_GetInviteIdByIndex(base.InnerHandle, ref options2, value, ref inOutBufferLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outBuffer);
		Helper.Dispose(ref value);
		return result;
	}

	public Result IsUserInSession(ref IsUserInSessionOptions options)
	{
		IsUserInSessionOptionsInternal options2 = default(IsUserInSessionOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_Sessions_IsUserInSession(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void JoinSession(ref JoinSessionOptions options, object clientData, OnJoinSessionCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		JoinSessionOptionsInternal options2 = default(JoinSessionOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sessions_JoinSession(base.InnerHandle, ref options2, clientDataPointer, OnJoinSessionCallbackInternalImplementation.Delegate);
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
		Bindings.EOS_Sessions_QueryInvites(base.InnerHandle, ref options2, clientDataPointer, OnQueryInvitesCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RegisterPlayers(ref RegisterPlayersOptions options, object clientData, OnRegisterPlayersCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		RegisterPlayersOptionsInternal options2 = default(RegisterPlayersOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sessions_RegisterPlayers(base.InnerHandle, ref options2, clientDataPointer, OnRegisterPlayersCallbackInternalImplementation.Delegate);
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
		Bindings.EOS_Sessions_RejectInvite(base.InnerHandle, ref options2, clientDataPointer, OnRejectInviteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyJoinSessionAccepted(ulong inId)
	{
		Bindings.EOS_Sessions_RemoveNotifyJoinSessionAccepted(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLeaveSessionRequested(ulong inId)
	{
		Bindings.EOS_Sessions_RemoveNotifyLeaveSessionRequested(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifySendSessionNativeInviteRequested(ulong inId)
	{
		Bindings.EOS_Sessions_RemoveNotifySendSessionNativeInviteRequested(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifySessionInviteAccepted(ulong inId)
	{
		Bindings.EOS_Sessions_RemoveNotifySessionInviteAccepted(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifySessionInviteReceived(ulong inId)
	{
		Bindings.EOS_Sessions_RemoveNotifySessionInviteReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifySessionInviteRejected(ulong inId)
	{
		Bindings.EOS_Sessions_RemoveNotifySessionInviteRejected(base.InnerHandle, inId);
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
		Bindings.EOS_Sessions_SendInvite(base.InnerHandle, ref options2, clientDataPointer, OnSendInviteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void StartSession(ref StartSessionOptions options, object clientData, OnStartSessionCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		StartSessionOptionsInternal options2 = default(StartSessionOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sessions_StartSession(base.InnerHandle, ref options2, clientDataPointer, OnStartSessionCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UnregisterPlayers(ref UnregisterPlayersOptions options, object clientData, OnUnregisterPlayersCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UnregisterPlayersOptionsInternal options2 = default(UnregisterPlayersOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sessions_UnregisterPlayers(base.InnerHandle, ref options2, clientDataPointer, OnUnregisterPlayersCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateSession(ref UpdateSessionOptions options, object clientData, OnUpdateSessionCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateSessionOptionsInternal options2 = default(UpdateSessionOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Sessions_UpdateSession(base.InnerHandle, ref options2, clientDataPointer, OnUpdateSessionCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result UpdateSessionModification(ref UpdateSessionModificationOptions options, out SessionModification outSessionModificationHandle)
	{
		UpdateSessionModificationOptionsInternal options2 = default(UpdateSessionModificationOptionsInternal);
		options2.Set(ref options);
		IntPtr outSessionModificationHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Sessions_UpdateSessionModification(base.InnerHandle, ref options2, out outSessionModificationHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outSessionModificationHandle2, out outSessionModificationHandle);
		return result;
	}
}
