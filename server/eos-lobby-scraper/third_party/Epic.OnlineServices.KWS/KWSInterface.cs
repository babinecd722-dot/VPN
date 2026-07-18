using System;

namespace Epic.OnlineServices.KWS;

public sealed class KWSInterface : Handle
{
	public const int ADDNOTIFYPERMISSIONSUPDATERECEIVED_API_LATEST = 1;

	public const int COPYPERMISSIONBYINDEX_API_LATEST = 1;

	public const int CREATEUSER_API_LATEST = 1;

	public const int GETPERMISSIONBYKEY_API_LATEST = 1;

	public const int GETPERMISSIONSCOUNT_API_LATEST = 1;

	public const int MAX_PERMISSIONS = 16;

	public const int MAX_PERMISSION_LENGTH = 32;

	public const int PERMISSIONSTATUS_API_LATEST = 1;

	public const int QUERYAGEGATE_API_LATEST = 1;

	public const int QUERYPERMISSIONS_API_LATEST = 1;

	public const int REQUESTPERMISSIONS_API_LATEST = 1;

	public const int UPDATEPARENTEMAIL_API_LATEST = 1;

	public KWSInterface()
	{
	}

	public KWSInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyPermissionsUpdateReceived(ref AddNotifyPermissionsUpdateReceivedOptions options, object clientData, OnPermissionsUpdateReceivedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyPermissionsUpdateReceivedOptionsInternal options2 = default(AddNotifyPermissionsUpdateReceivedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_KWS_AddNotifyPermissionsUpdateReceived(base.InnerHandle, ref options2, clientDataPointer, OnPermissionsUpdateReceivedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result CopyPermissionByIndex(ref CopyPermissionByIndexOptions options, out PermissionStatus? outPermission)
	{
		CopyPermissionByIndexOptionsInternal options2 = default(CopyPermissionByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outPermission2 = IntPtr.Zero;
		Result result = Bindings.EOS_KWS_CopyPermissionByIndex(base.InnerHandle, ref options2, out outPermission2);
		Helper.Dispose(ref options2);
		Helper.Get<PermissionStatusInternal, PermissionStatus>(outPermission2, out outPermission);
		if (outPermission2 != IntPtr.Zero)
		{
			Bindings.EOS_KWS_PermissionStatus_Release(outPermission2);
		}
		return result;
	}

	public void CreateUser(ref CreateUserOptions options, object clientData, OnCreateUserCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		CreateUserOptionsInternal options2 = default(CreateUserOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_KWS_CreateUser(base.InnerHandle, ref options2, clientDataPointer, OnCreateUserCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result GetPermissionByKey(ref GetPermissionByKeyOptions options, out KWSPermissionStatus outPermission)
	{
		GetPermissionByKeyOptionsInternal options2 = default(GetPermissionByKeyOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_KWS_GetPermissionByKey(base.InnerHandle, ref options2, out outPermission);
		Helper.Dispose(ref options2);
		return result;
	}

	public int GetPermissionsCount(ref GetPermissionsCountOptions options)
	{
		GetPermissionsCountOptionsInternal options2 = default(GetPermissionsCountOptionsInternal);
		options2.Set(ref options);
		int result = Bindings.EOS_KWS_GetPermissionsCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryAgeGate(ref QueryAgeGateOptions options, object clientData, OnQueryAgeGateCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryAgeGateOptionsInternal options2 = default(QueryAgeGateOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_KWS_QueryAgeGate(base.InnerHandle, ref options2, clientDataPointer, OnQueryAgeGateCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryPermissions(ref QueryPermissionsOptions options, object clientData, OnQueryPermissionsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryPermissionsOptionsInternal options2 = default(QueryPermissionsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_KWS_QueryPermissions(base.InnerHandle, ref options2, clientDataPointer, OnQueryPermissionsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyPermissionsUpdateReceived(ulong inId)
	{
		Bindings.EOS_KWS_RemoveNotifyPermissionsUpdateReceived(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RequestPermissions(ref RequestPermissionsOptions options, object clientData, OnRequestPermissionsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		RequestPermissionsOptionsInternal options2 = default(RequestPermissionsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_KWS_RequestPermissions(base.InnerHandle, ref options2, clientDataPointer, OnRequestPermissionsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateParentEmail(ref UpdateParentEmailOptions options, object clientData, OnUpdateParentEmailCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateParentEmailOptionsInternal options2 = default(UpdateParentEmailOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_KWS_UpdateParentEmail(base.InnerHandle, ref options2, clientDataPointer, OnUpdateParentEmailCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
