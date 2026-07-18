using System;

namespace Epic.OnlineServices.Auth;

public sealed class AuthInterface : Handle
{
	public const int ACCOUNTFEATURERESTRICTEDINFO_API_LATEST = 1;

	public const int ADDNOTIFYLOGINSTATUSCHANGED_API_LATEST = 1;

	public const int COPYIDTOKEN_API_LATEST = 1;

	public const int COPYUSERAUTHTOKEN_API_LATEST = 1;

	public const int CREDENTIALS_API_LATEST = 4;

	public const int DELETEPERSISTENTAUTH_API_LATEST = 2;

	public const int IDTOKEN_API_LATEST = 1;

	public const int LINKACCOUNT_API_LATEST = 1;

	public const int LOGIN_API_LATEST = 3;

	public const int LOGOUT_API_LATEST = 1;

	public const int PINGRANTINFO_API_LATEST = 2;

	public const int QUERYIDTOKEN_API_LATEST = 1;

	public const int TOKEN_API_LATEST = 2;

	public const int VERIFYIDTOKEN_API_LATEST = 1;

	public const int VERIFYUSERAUTH_API_LATEST = 1;

	public AuthInterface()
	{
	}

	public AuthInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
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
		ulong num = Bindings.EOS_Auth_AddNotifyLoginStatusChanged(base.InnerHandle, ref options2, clientDataPointer, OnLoginStatusChangedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result CopyIdToken(ref CopyIdTokenOptions options, out IdToken? outIdToken)
	{
		CopyIdTokenOptionsInternal options2 = default(CopyIdTokenOptionsInternal);
		options2.Set(ref options);
		IntPtr outIdToken2 = IntPtr.Zero;
		Result result = Bindings.EOS_Auth_CopyIdToken(base.InnerHandle, ref options2, out outIdToken2);
		Helper.Dispose(ref options2);
		Helper.Get<IdTokenInternal, IdToken>(outIdToken2, out outIdToken);
		if (outIdToken2 != IntPtr.Zero)
		{
			Bindings.EOS_Auth_IdToken_Release(outIdToken2);
		}
		return result;
	}

	public Result CopyUserAuthToken(ref CopyUserAuthTokenOptions options, EpicAccountId localUserId, out Token? outUserAuthToken)
	{
		CopyUserAuthTokenOptionsInternal options2 = default(CopyUserAuthTokenOptionsInternal);
		options2.Set(ref options);
		IntPtr outUserAuthToken2 = IntPtr.Zero;
		Result result = Bindings.EOS_Auth_CopyUserAuthToken(base.InnerHandle, ref options2, localUserId.InnerHandle, out outUserAuthToken2);
		Helper.Dispose(ref options2);
		Helper.Get<TokenInternal, Token>(outUserAuthToken2, out outUserAuthToken);
		if (outUserAuthToken2 != IntPtr.Zero)
		{
			Bindings.EOS_Auth_Token_Release(outUserAuthToken2);
		}
		return result;
	}

	public void DeletePersistentAuth(ref DeletePersistentAuthOptions options, object clientData, OnDeletePersistentAuthCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		DeletePersistentAuthOptionsInternal options2 = default(DeletePersistentAuthOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Auth_DeletePersistentAuth(base.InnerHandle, ref options2, clientDataPointer, OnDeletePersistentAuthCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public EpicAccountId GetLoggedInAccountByIndex(int index)
	{
		Helper.Get(Bindings.EOS_Auth_GetLoggedInAccountByIndex(base.InnerHandle, index), out EpicAccountId to);
		return to;
	}

	public int GetLoggedInAccountsCount()
	{
		return Bindings.EOS_Auth_GetLoggedInAccountsCount(base.InnerHandle);
	}

	public LoginStatus GetLoginStatus(EpicAccountId localUserId)
	{
		return Bindings.EOS_Auth_GetLoginStatus(base.InnerHandle, localUserId.InnerHandle);
	}

	public EpicAccountId GetMergedAccountByIndex(EpicAccountId localUserId, uint index)
	{
		Helper.Get(Bindings.EOS_Auth_GetMergedAccountByIndex(base.InnerHandle, localUserId.InnerHandle, index), out EpicAccountId to);
		return to;
	}

	public uint GetMergedAccountsCount(EpicAccountId localUserId)
	{
		return Bindings.EOS_Auth_GetMergedAccountsCount(base.InnerHandle, localUserId.InnerHandle);
	}

	public Result GetSelectedAccountId(EpicAccountId localUserId, out EpicAccountId outSelectedAccountId)
	{
		IntPtr outSelectedAccountId2 = IntPtr.Zero;
		Result result = Bindings.EOS_Auth_GetSelectedAccountId(base.InnerHandle, localUserId.InnerHandle, out outSelectedAccountId2);
		Helper.Get(outSelectedAccountId2, out outSelectedAccountId);
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
		Bindings.EOS_Auth_LinkAccount(base.InnerHandle, ref options2, clientDataPointer, OnLinkAccountCallbackInternalImplementation.Delegate);
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
		Bindings.EOS_Auth_Login(base.InnerHandle, ref options2, clientDataPointer, OnLoginCallbackInternalImplementation.Delegate);
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
		Bindings.EOS_Auth_Logout(base.InnerHandle, ref options2, clientDataPointer, OnLogoutCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryIdToken(ref QueryIdTokenOptions options, object clientData, OnQueryIdTokenCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryIdTokenOptionsInternal options2 = default(QueryIdTokenOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Auth_QueryIdToken(base.InnerHandle, ref options2, clientDataPointer, OnQueryIdTokenCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyLoginStatusChanged(ulong inId)
	{
		Bindings.EOS_Auth_RemoveNotifyLoginStatusChanged(base.InnerHandle, inId);
		Helper.RemoveCallbackByNotificationId(inId);
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
		Bindings.EOS_Auth_VerifyIdToken(base.InnerHandle, ref options2, clientDataPointer, OnVerifyIdTokenCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void VerifyUserAuth(ref VerifyUserAuthOptions options, object clientData, OnVerifyUserAuthCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		VerifyUserAuthOptionsInternal options2 = default(VerifyUserAuthOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Auth_VerifyUserAuth(base.InnerHandle, ref options2, clientDataPointer, OnVerifyUserAuthCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
