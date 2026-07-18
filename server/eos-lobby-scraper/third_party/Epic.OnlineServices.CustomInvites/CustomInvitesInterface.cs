using System;

namespace Epic.OnlineServices.CustomInvites;

public sealed class CustomInvitesInterface : Handle
{
	public const int ACCEPTREQUESTTOJOIN_API_LATEST = 1;

	public const int ADDNOTIFYCUSTOMINVITEACCEPTED_API_LATEST = 1;

	public const int ADDNOTIFYCUSTOMINVITERECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYCUSTOMINVITEREJECTED_API_LATEST = 1;

	public const int ADDNOTIFYREQUESTTOJOINACCEPTED_API_LATEST = 1;

	public const int ADDNOTIFYREQUESTTOJOINRECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYREQUESTTOJOINREJECTED_API_LATEST = 1;

	public const int ADDNOTIFYREQUESTTOJOINRESPONSERECEIVED_API_LATEST = 1;

	public const int ADDNOTIFYSENDCUSTOMNATIVEINVITEREQUESTED_API_LATEST = 1;

	public const int FINALIZEINVITE_API_LATEST = 1;

	public const int MAX_PAYLOAD_LENGTH = 500;

	public const int REJECTREQUESTTOJOIN_API_LATEST = 1;

	public const int SENDCUSTOMINVITE_API_LATEST = 1;

	public const int SENDREQUESTTOJOIN_API_LATEST = 1;

	public const int SETCUSTOMINVITE_API_LATEST = 1;

	public CustomInvitesInterface()
	{
	}

	public CustomInvitesInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public void AcceptRequestToJoin(ref AcceptRequestToJoinOptions options, object clientData, OnAcceptRequestToJoinCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AcceptRequestToJoinOptionsInternal options2 = default(AcceptRequestToJoinOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_CustomInvites_AcceptRequestToJoin(base.InnerHandle, ref options2, clientDataPointer, OnAcceptRequestToJoinCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public ulong AddNotifyCustomInviteAccepted(ref AddNotifyCustomInviteAcceptedOptions options, object clientData, OnCustomInviteAcceptedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyCustomInviteAcceptedOptionsInternal options2 = default(AddNotifyCustomInviteAcceptedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_CustomInvites_AddNotifyCustomInviteAccepted(base.InnerHandle, ref options2, clientDataPointer, OnCustomInviteAcceptedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyCustomInviteReceived(ref AddNotifyCustomInviteReceivedOptions options, object clientData, OnCustomInviteReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyCustomInviteReceivedOptionsInternal options2 = default(AddNotifyCustomInviteReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_CustomInvites_AddNotifyCustomInviteReceived(base.InnerHandle, ref options2, clientDataPointer, OnCustomInviteReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyCustomInviteRejected(ref AddNotifyCustomInviteRejectedOptions options, object clientData, OnCustomInviteRejectedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyCustomInviteRejectedOptionsInternal options2 = default(AddNotifyCustomInviteRejectedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_CustomInvites_AddNotifyCustomInviteRejected(base.InnerHandle, ref options2, clientDataPointer, OnCustomInviteRejectedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyRequestToJoinAccepted(ref AddNotifyRequestToJoinAcceptedOptions options, object clientData, OnRequestToJoinAcceptedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyRequestToJoinAcceptedOptionsInternal options2 = default(AddNotifyRequestToJoinAcceptedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_CustomInvites_AddNotifyRequestToJoinAccepted(base.InnerHandle, ref options2, clientDataPointer, OnRequestToJoinAcceptedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyRequestToJoinReceived(ref AddNotifyRequestToJoinReceivedOptions options, object clientData, OnRequestToJoinReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyRequestToJoinReceivedOptionsInternal options2 = default(AddNotifyRequestToJoinReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_CustomInvites_AddNotifyRequestToJoinReceived(base.InnerHandle, ref options2, clientDataPointer, OnRequestToJoinReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyRequestToJoinRejected(ref AddNotifyRequestToJoinRejectedOptions options, object clientData, OnRequestToJoinRejectedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyRequestToJoinRejectedOptionsInternal options2 = default(AddNotifyRequestToJoinRejectedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_CustomInvites_AddNotifyRequestToJoinRejected(base.InnerHandle, ref options2, clientDataPointer, OnRequestToJoinRejectedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyRequestToJoinResponseReceived(ref AddNotifyRequestToJoinResponseReceivedOptions options, object clientData, OnRequestToJoinResponseReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyRequestToJoinResponseReceivedOptionsInternal options2 = default(AddNotifyRequestToJoinResponseReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_CustomInvites_AddNotifyRequestToJoinResponseReceived(base.InnerHandle, ref options2, clientDataPointer, OnRequestToJoinResponseReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifySendCustomNativeInviteRequested(ref AddNotifySendCustomNativeInviteRequestedOptions options, object clientData, OnSendCustomNativeInviteRequestedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifySendCustomNativeInviteRequestedOptionsInternal options2 = default(AddNotifySendCustomNativeInviteRequestedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_CustomInvites_AddNotifySendCustomNativeInviteRequested(base.InnerHandle, ref options2, clientDataPointer, OnSendCustomNativeInviteRequestedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result FinalizeInvite(ref FinalizeInviteOptions options)
	{
		FinalizeInviteOptionsInternal options2 = default(FinalizeInviteOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_CustomInvites_FinalizeInvite(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void RejectRequestToJoin(ref RejectRequestToJoinOptions options, object clientData, OnRejectRequestToJoinCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		RejectRequestToJoinOptionsInternal options2 = default(RejectRequestToJoinOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_CustomInvites_RejectRequestToJoin(base.InnerHandle, ref options2, clientDataPointer, OnRejectRequestToJoinCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyCustomInviteAccepted(ulong inId)
	{
		Bindings.EOS_CustomInvites_RemoveNotifyCustomInviteAccepted(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyCustomInviteReceived(ulong inId)
	{
		Bindings.EOS_CustomInvites_RemoveNotifyCustomInviteReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyCustomInviteRejected(ulong inId)
	{
		Bindings.EOS_CustomInvites_RemoveNotifyCustomInviteRejected(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyRequestToJoinAccepted(ulong inId)
	{
		Bindings.EOS_CustomInvites_RemoveNotifyRequestToJoinAccepted(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyRequestToJoinReceived(ulong inId)
	{
		Bindings.EOS_CustomInvites_RemoveNotifyRequestToJoinReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyRequestToJoinRejected(ulong inId)
	{
		Bindings.EOS_CustomInvites_RemoveNotifyRequestToJoinRejected(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyRequestToJoinResponseReceived(ulong inId)
	{
		Bindings.EOS_CustomInvites_RemoveNotifyRequestToJoinResponseReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifySendCustomNativeInviteRequested(ulong inId)
	{
		Bindings.EOS_CustomInvites_RemoveNotifySendCustomNativeInviteRequested(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void SendCustomInvite(ref SendCustomInviteOptions options, object clientData, OnSendCustomInviteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SendCustomInviteOptionsInternal options2 = default(SendCustomInviteOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_CustomInvites_SendCustomInvite(base.InnerHandle, ref options2, clientDataPointer, OnSendCustomInviteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void SendRequestToJoin(ref SendRequestToJoinOptions options, object clientData, OnSendRequestToJoinCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SendRequestToJoinOptionsInternal options2 = default(SendRequestToJoinOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_CustomInvites_SendRequestToJoin(base.InnerHandle, ref options2, clientDataPointer, OnSendRequestToJoinCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result SetCustomInvite(ref SetCustomInviteOptions options)
	{
		SetCustomInviteOptionsInternal options2 = default(SetCustomInviteOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_CustomInvites_SetCustomInvite(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}
}
