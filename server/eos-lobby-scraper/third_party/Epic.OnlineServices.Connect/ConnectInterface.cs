using System;

namespace Epic.OnlineServices.Connect;

public sealed class ConnectInterface : Handle
{
	public const int ADDNOTIFYAUTHEXPIRATION_API_LATEST = 1;

	public const int ADDNOTIFYLOGINSTATUSCHANGED_API_LATEST = 1;

	public const int COPYIDTOKEN_API_LATEST = 1;

	public const int COPYPRODUCTUSEREXTERNALACCOUNTBYACCOUNTID_API_LATEST = 1;

	public const int COPYPRODUCTUSEREXTERNALACCOUNTBYACCOUNTTYPE_API_LATEST = 1;

	public const int COPYPRODUCTUSEREXTERNALACCOUNTBYINDEX_API_LATEST = 1;

	public const int COPYPRODUCTUSERINFO_API_LATEST = 1;

	public const int CREATEDEVICEID_API_LATEST = 1;

	public const int CREATEDEVICEID_DEVICEMODEL_MAX_LENGTH = 64;

	public const int CREATEUSER_API_LATEST = 1;

	public const int CREDENTIALS_API_LATEST = 1;

	public const int DELETEDEVICEID_API_LATEST = 1;

	public const int EXTERNALACCOUNTINFO_API_LATEST = 1;

	public const int EXTERNAL_ACCOUNT_ID_MAX_LENGTH = 256;

	public const int GETEXTERNALACCOUNTMAPPINGS_API_LATEST = 1;

	public const int GETEXTERNALACCOUNTMAPPING_API_LATEST = 1;

	public const int GETPRODUCTUSEREXTERNALACCOUNTCOUNT_API_LATEST = 1;

	public const int GETPRODUCTUSERIDMAPPING_API_LATEST = 1;

	public const int IDTOKEN_API_LATEST = 1;

	public const int LINKACCOUNT_API_LATEST = 1;

	public const int LOGIN_API_LATEST = 2;

	public const int LOGOUT_API_LATEST = 1;

	public const int QUERYEXTERNALACCOUNTMAPPINGS_API_LATEST = 1;

	public const int QUERYEXTERNALACCOUNTMAPPINGS_MAX_ACCOUNT_IDS = 128;

	public const int QUERYPRODUCTUSERIDMAPPINGS_API_LATEST = 2;

	public const int TIME_UNDEFINED = -1;

	public const int TRANSFERDEVICEIDACCOUNT_API_LATEST = 1;

	public const int UNLINKACCOUNT_API_LATEST = 1;

	public const int USERLOGININFO_API_LATEST = 2;

	public const int USERLOGININFO_DISPLAYNAME_MAX_LENGTH = 32;

	public const int VERIFYIDTOKEN_API_LATEST = 1;

	public ConnectInterface()
	{
	}

	public ConnectInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyAuthExpiration(ref AddNotifyAuthExpirationOptions options, object clientData, OnAuthExpirationCallback notification)
	{
		if (notification == null)
		{
			throw new ArgumentNullException("notification");
		}
		AddNotifyAuthExpirationOptionsInternal options2 = default(AddNotifyAuthExpirationOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notification);
		ulong num = Bindings.EOS_Connect_AddNotifyAuthExpiration(base.InnerHandle, ref options2, clientDataPointer, OnAuthExpirationCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyLoginStatusChanged(ref AddNotifyLoginStatusChangedOptions options, object clientData, OnLoginStatusChangedCallback notification)
	{
		if (notification == null)
		{
			throw new ArgumentNullException("notification");
		}
		AddNotifyLoginStatusChangedOptionsInternal options2 = default(AddNotifyLoginStatusChangedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notification);
		ulong num = Bindings.EOS_Connect_AddNotifyLoginStatusChanged(base.InnerHandle, ref options2, clientDataPointer, OnLoginStatusChangedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result CopyIdToken(ref CopyIdTokenOptions options, out IdToken? outIdToken)
	{
		CopyIdTokenOptionsInternal options2 = default(CopyIdTokenOptionsInternal);
		options2.Set(ref options);
		IntPtr outIdToken2 = IntPtr.Zero;
		Result result = Bindings.EOS_Connect_CopyIdToken(base.InnerHandle, ref options2, out outIdToken2);
		Helper.Dispose(ref options2);
		Helper.Get<IdTokenInternal, IdToken>(outIdToken2, out outIdToken);
		if (outIdToken2 != IntPtr.Zero)
		{
			Bindings.EOS_Connect_IdToken_Release(outIdToken2);
		}
		return result;
	}

	public Result CopyProductUserExternalAccountByAccountId(ref CopyProductUserExternalAccountByAccountIdOptions options, out ExternalAccountInfo? outExternalAccountInfo)
	{
		CopyProductUserExternalAccountByAccountIdOptionsInternal options2 = default(CopyProductUserExternalAccountByAccountIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outExternalAccountInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_Connect_CopyProductUserExternalAccountByAccountId(base.InnerHandle, ref options2, out outExternalAccountInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<ExternalAccountInfoInternal, ExternalAccountInfo>(outExternalAccountInfo2, out outExternalAccountInfo);
		if (outExternalAccountInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_Connect_ExternalAccountInfo_Release(outExternalAccountInfo2);
		}
		return result;
	}

	public Result CopyProductUserExternalAccountByAccountType(ref CopyProductUserExternalAccountByAccountTypeOptions options, out ExternalAccountInfo? outExternalAccountInfo)
	{
		CopyProductUserExternalAccountByAccountTypeOptionsInternal options2 = default(CopyProductUserExternalAccountByAccountTypeOptionsInternal);
		options2.Set(ref options);
		IntPtr outExternalAccountInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_Connect_CopyProductUserExternalAccountByAccountType(base.InnerHandle, ref options2, out outExternalAccountInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<ExternalAccountInfoInternal, ExternalAccountInfo>(outExternalAccountInfo2, out outExternalAccountInfo);
		if (outExternalAccountInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_Connect_ExternalAccountInfo_Release(outExternalAccountInfo2);
		}
		return result;
	}

	public Result CopyProductUserExternalAccountByIndex(ref CopyProductUserExternalAccountByIndexOptions options, out ExternalAccountInfo? outExternalAccountInfo)
	{
		CopyProductUserExternalAccountByIndexOptionsInternal options2 = default(CopyProductUserExternalAccountByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outExternalAccountInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_Connect_CopyProductUserExternalAccountByIndex(base.InnerHandle, ref options2, out outExternalAccountInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<ExternalAccountInfoInternal, ExternalAccountInfo>(outExternalAccountInfo2, out outExternalAccountInfo);
		if (outExternalAccountInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_Connect_ExternalAccountInfo_Release(outExternalAccountInfo2);
		}
		return result;
	}

	public Result CopyProductUserInfo(ref CopyProductUserInfoOptions options, out ExternalAccountInfo? outExternalAccountInfo)
	{
		CopyProductUserInfoOptionsInternal options2 = default(CopyProductUserInfoOptionsInternal);
		options2.Set(ref options);
		IntPtr outExternalAccountInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_Connect_CopyProductUserInfo(base.InnerHandle, ref options2, out outExternalAccountInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<ExternalAccountInfoInternal, ExternalAccountInfo>(outExternalAccountInfo2, out outExternalAccountInfo);
		if (outExternalAccountInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_Connect_ExternalAccountInfo_Release(outExternalAccountInfo2);
		}
		return result;
	}

	public void CreateDeviceId(ref CreateDeviceIdOptions options, object clientData, OnCreateDeviceIdCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		CreateDeviceIdOptionsInternal options2 = default(CreateDeviceIdOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_CreateDeviceId(base.InnerHandle, ref options2, clientDataPointer, OnCreateDeviceIdCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
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
		Bindings.EOS_Connect_CreateUser(base.InnerHandle, ref options2, clientDataPointer, OnCreateUserCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void DeleteDeviceId(ref DeleteDeviceIdOptions options, object clientData, OnDeleteDeviceIdCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		DeleteDeviceIdOptionsInternal options2 = default(DeleteDeviceIdOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_DeleteDeviceId(base.InnerHandle, ref options2, clientDataPointer, OnDeleteDeviceIdCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public ProductUserId GetExternalAccountMapping(ref GetExternalAccountMappingsOptions options)
	{
		GetExternalAccountMappingsOptionsInternal options2 = default(GetExternalAccountMappingsOptionsInternal);
		options2.Set(ref options);
		IntPtr intPtr = Bindings.EOS_Connect_GetExternalAccountMapping(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(intPtr, out ProductUserId to);
		return to;
	}

	public ProductUserId GetLoggedInUserByIndex(int index)
	{
		Helper.Get(Bindings.EOS_Connect_GetLoggedInUserByIndex(base.InnerHandle, index), out ProductUserId to);
		return to;
	}

	public int GetLoggedInUsersCount()
	{
		return Bindings.EOS_Connect_GetLoggedInUsersCount(base.InnerHandle);
	}

	public LoginStatus GetLoginStatus(ProductUserId localUserId)
	{
		return Bindings.EOS_Connect_GetLoginStatus(base.InnerHandle, localUserId.InnerHandle);
	}

	public uint GetProductUserExternalAccountCount(ref GetProductUserExternalAccountCountOptions options)
	{
		GetProductUserExternalAccountCountOptionsInternal options2 = default(GetProductUserExternalAccountCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_Connect_GetProductUserExternalAccountCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result GetProductUserIdMapping(ref GetProductUserIdMappingOptions options, out Utf8String outBuffer)
	{
		GetProductUserIdMappingOptionsInternal options2 = default(GetProductUserIdMappingOptionsInternal);
		options2.Set(ref options);
		int inOutBufferLength = 257;
		IntPtr value = Helper.AddAllocation(inOutBufferLength);
		Result result = Bindings.EOS_Connect_GetProductUserIdMapping(base.InnerHandle, ref options2, value, ref inOutBufferLength);
		Helper.Dispose(ref options2);
		Helper.Get(value, out outBuffer);
		Helper.Dispose(ref value);
		return result;
	}

	public void LinkAccount(ref LinkAccountOptions options, object clientData, OnLinkAccountCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		LinkAccountOptionsInternal options2 = default(LinkAccountOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_LinkAccount(base.InnerHandle, ref options2, clientDataPointer, OnLinkAccountCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void Login(ref LoginOptions options, object clientData, OnLoginCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		LoginOptionsInternal options2 = default(LoginOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_Login(base.InnerHandle, ref options2, clientDataPointer, OnLoginCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void Logout(ref LogoutOptions options, object clientData, OnLogoutCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		LogoutOptionsInternal options2 = default(LogoutOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_Logout(base.InnerHandle, ref options2, clientDataPointer, OnLogoutCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryExternalAccountMappings(ref QueryExternalAccountMappingsOptions options, object clientData, OnQueryExternalAccountMappingsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryExternalAccountMappingsOptionsInternal options2 = default(QueryExternalAccountMappingsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_QueryExternalAccountMappings(base.InnerHandle, ref options2, clientDataPointer, OnQueryExternalAccountMappingsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryProductUserIdMappings(ref QueryProductUserIdMappingsOptions options, object clientData, OnQueryProductUserIdMappingsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryProductUserIdMappingsOptionsInternal options2 = default(QueryProductUserIdMappingsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_QueryProductUserIdMappings(base.InnerHandle, ref options2, clientDataPointer, OnQueryProductUserIdMappingsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyAuthExpiration(ulong inId)
	{
		Bindings.EOS_Connect_RemoveNotifyAuthExpiration(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void RemoveNotifyLoginStatusChanged(ulong inId)
	{
		Bindings.EOS_Connect_RemoveNotifyLoginStatusChanged(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
	}

	public void TransferDeviceIdAccount(ref TransferDeviceIdAccountOptions options, object clientData, OnTransferDeviceIdAccountCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		TransferDeviceIdAccountOptionsInternal options2 = default(TransferDeviceIdAccountOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_TransferDeviceIdAccount(base.InnerHandle, ref options2, clientDataPointer, OnTransferDeviceIdAccountCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UnlinkAccount(ref UnlinkAccountOptions options, object clientData, OnUnlinkAccountCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UnlinkAccountOptionsInternal options2 = default(UnlinkAccountOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_UnlinkAccount(base.InnerHandle, ref options2, clientDataPointer, OnUnlinkAccountCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void VerifyIdToken(ref VerifyIdTokenOptions options, object clientData, OnVerifyIdTokenCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		VerifyIdTokenOptionsInternal options2 = default(VerifyIdTokenOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Connect_VerifyIdToken(base.InnerHandle, ref options2, clientDataPointer, OnVerifyIdTokenCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
