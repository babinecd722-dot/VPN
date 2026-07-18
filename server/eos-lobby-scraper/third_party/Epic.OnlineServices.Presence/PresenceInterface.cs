using System;

namespace Epic.OnlineServices.Presence;

public sealed class PresenceInterface : Handle
{
	public const int ADDNOTIFYJOINGAMEACCEPTED_API_LATEST = 2;

	public const int ADDNOTIFYONPRESENCECHANGED_API_LATEST = 1;

	public const int COPYPRESENCE_API_LATEST = 3;

	public const int CREATEPRESENCEMODIFICATION_API_LATEST = 1;

	public const int DATARECORD_API_LATEST = 1;

	public const int DATA_MAX_KEYS = 32;

	public const int DATA_MAX_KEY_LENGTH = 64;

	public const int DATA_MAX_VALUE_LENGTH = 255;

	public const int DELETEDATA_API_LATEST = 1;

	public const int GETJOININFO_API_LATEST = 1;

	public const int HASPRESENCE_API_LATEST = 1;

	public const int INFO_API_LATEST = 3;

	public static readonly Utf8String KEY_PLATFORM_PRESENCE = "EOS_PlatformPresence";

	public const int PRESENCEMODIFICATION_DATARECORDID_API_LATEST = 1;

	public const int PRESENCEMODIFICATION_DELETEDATA_API_LATEST = 1;

	public const int PRESENCEMODIFICATION_JOININFO_MAX_LENGTH = 255;

	public const int PRESENCEMODIFICATION_SETDATA_API_LATEST = 1;

	public const int PRESENCEMODIFICATION_SETJOININFO_API_LATEST = 1;

	public const int PRESENCEMODIFICATION_SETRAWRICHTEXT_API_LATEST = 1;

	public const int PRESENCEMODIFICATION_SETSTATUS_API_LATEST = 1;

	public const int PRESENCEMODIFICATION_SETTEMPLATEDATA_API_LATEST = 1;

	public const int PRESENCEMODIFICATION_SETTEMPLATEID_API_LATEST = 1;

	public const int QUERYPRESENCE_API_LATEST = 1;

	public const int RICH_TEXT_MAX_VALUE_LENGTH = 255;

	public const int SETDATA_API_LATEST = 1;

	public const int SETPRESENCE_API_LATEST = 1;

	public const int SETRAWRICHTEXT_API_LATEST = 1;

	public const int SETSTATUS_API_LATEST = 1;

	public PresenceInterface()
	{
	}

	public PresenceInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyJoinGameAccepted(ref AddNotifyJoinGameAcceptedOptions options, object clientData, OnJoinGameAcceptedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyJoinGameAcceptedOptionsInternal options2 = default(AddNotifyJoinGameAcceptedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_Presence_AddNotifyJoinGameAccepted(base.InnerHandle, ref options2, clientDataPointer, OnJoinGameAcceptedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyOnPresenceChanged(ref AddNotifyOnPresenceChangedOptions options, object clientData, OnPresenceChangedCallback notificationHandler)
	{
		if (notificationHandler == null)
		{
			throw new ArgumentNullException("notificationHandler");
		}
		AddNotifyOnPresenceChangedOptionsInternal options2 = default(AddNotifyOnPresenceChangedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationHandler);
		ulong num = Bindings.EOS_Presence_AddNotifyOnPresenceChanged(base.InnerHandle, ref options2, clientDataPointer, OnPresenceChangedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result CopyPresence(ref CopyPresenceOptions options, out Info? outPresence)
	{
		CopyPresenceOptionsInternal options2 = default(CopyPresenceOptionsInternal);
		options2.Set(ref options);
		IntPtr outPresence2 = IntPtr.Zero;
		Result result = Bindings.EOS_Presence_CopyPresence(base.InnerHandle, ref options2, out outPresence2);
		Helper.Dispose(ref options2);
		Helper.Get<InfoInternal, Info>(outPresence2, out outPresence);
		if (outPresence2 != IntPtr.Zero)
		{
			Bindings.EOS_Presence_Info_Release(outPresence2);
		}
		return result;
	}

	public Result CreatePresenceModification(ref CreatePresenceModificationOptions options, out PresenceModification outPresenceModificationHandle)
	{
		CreatePresenceModificationOptionsInternal options2 = default(CreatePresenceModificationOptionsInternal);
		options2.Set(ref options);
		IntPtr outPresenceModificationHandle2 = IntPtr.Zero;
		Result result = Bindings.EOS_Presence_CreatePresenceModification(base.InnerHandle, ref options2, out outPresenceModificationHandle2);
		Helper.Dispose(ref options2);
		Helper.Get(outPresenceModificationHandle2, out outPresenceModificationHandle);
		return result;
	}

	public Result GetJoinInfo(ref GetJoinInfoOptions options, out Utf8String outBuffer)
	{
		GetJoinInfoOptionsInternal options2 = default(GetJoinInfoOptionsInternal);
		options2.Set(ref options);
		int inOutBufferLength = 256;
		IntPtr value = Helper.AddAllocation(inOutBufferLength);
		Result result = Bindings.EOS_Presence_GetJoinInfo(base.InnerHandle, ref options2, value, ref inOutBufferLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outBuffer);
		Helper.Dispose(ref value);
		return result;
	}

	public bool HasPresence(ref HasPresenceOptions options)
	{
		HasPresenceOptionsInternal options2 = default(HasPresenceOptionsInternal);
		options2.Set(ref options);
		int num = Bindings.EOS_Presence_HasPresence(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(num, out bool to);
		return to;
	}

	public void QueryPresence(ref QueryPresenceOptions options, object clientData, OnQueryPresenceCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryPresenceOptionsInternal options2 = default(QueryPresenceOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Presence_QueryPresence(base.InnerHandle, ref options2, clientDataPointer, OnQueryPresenceCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyJoinGameAccepted(ulong inId)
	{
		Bindings.EOS_Presence_RemoveNotifyJoinGameAccepted(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyOnPresenceChanged(ulong notificationId)
	{
		Bindings.EOS_Presence_RemoveNotifyOnPresenceChanged(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void SetPresence(ref SetPresenceOptions options, object clientData, SetPresenceCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SetPresenceOptionsInternal options2 = default(SetPresenceOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Presence_SetPresence(base.InnerHandle, ref options2, clientDataPointer, SetPresenceCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
