using System;

namespace Epic.OnlineServices.UserInfo;

public sealed class UserInfoInterface : Handle
{
	public const int BESTDISPLAYNAME_API_LATEST = 1;

	public const int COPYBESTDISPLAYNAMEWITHPLATFORM_API_LATEST = 1;

	public const int COPYBESTDISPLAYNAME_API_LATEST = 1;

	public const int COPYEXTERNALUSERINFOBYACCOUNTID_API_LATEST = 1;

	public const int COPYEXTERNALUSERINFOBYACCOUNTTYPE_API_LATEST = 1;

	public const int COPYEXTERNALUSERINFOBYINDEX_API_LATEST = 1;

	public const int COPYUSERINFO_API_LATEST = 3;

	public const int EXTERNALUSERINFO_API_LATEST = 2;

	public const int GETEXTERNALUSERINFOCOUNT_API_LATEST = 1;

	public const int GETLOCALPLATFORMTYPE_API_LATEST = 1;

	public const int MAX_DISPLAYNAME_CHARACTERS = 16;

	public const int MAX_DISPLAYNAME_UTF8_LENGTH = 64;

	public const int QUERYUSERINFOBYDISPLAYNAME_API_LATEST = 1;

	public const int QUERYUSERINFOBYEXTERNALACCOUNT_API_LATEST = 1;

	public const int QUERYUSERINFO_API_LATEST = 1;

	public UserInfoInterface()
	{
	}

	public UserInfoInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyBestDisplayName(ref CopyBestDisplayNameOptions options, out BestDisplayName? outBestDisplayName)
	{
		CopyBestDisplayNameOptionsInternal options2 = default(CopyBestDisplayNameOptionsInternal);
		options2.Set(ref options);
		IntPtr outBestDisplayName2 = IntPtr.Zero;
		Result result = Bindings.EOS_UserInfo_CopyBestDisplayName(base.InnerHandle, ref options2, out outBestDisplayName2);
		Helper.Dispose(ref options2);
		Helper.Get<BestDisplayNameInternal, BestDisplayName>(outBestDisplayName2, out outBestDisplayName);
		if (outBestDisplayName2 != IntPtr.Zero)
		{
			Bindings.EOS_UserInfo_BestDisplayName_Release(outBestDisplayName2);
		}
		return result;
	}

	public Result CopyBestDisplayNameWithPlatform(ref CopyBestDisplayNameWithPlatformOptions options, out BestDisplayName? outBestDisplayName)
	{
		CopyBestDisplayNameWithPlatformOptionsInternal options2 = default(CopyBestDisplayNameWithPlatformOptionsInternal);
		options2.Set(ref options);
		IntPtr outBestDisplayName2 = IntPtr.Zero;
		Result result = Bindings.EOS_UserInfo_CopyBestDisplayNameWithPlatform(base.InnerHandle, ref options2, out outBestDisplayName2);
		Helper.Dispose(ref options2);
		Helper.Get<BestDisplayNameInternal, BestDisplayName>(outBestDisplayName2, out outBestDisplayName);
		if (outBestDisplayName2 != IntPtr.Zero)
		{
			Bindings.EOS_UserInfo_BestDisplayName_Release(outBestDisplayName2);
		}
		return result;
	}

	public Result CopyExternalUserInfoByAccountId(ref CopyExternalUserInfoByAccountIdOptions options, out ExternalUserInfo? outExternalUserInfo)
	{
		CopyExternalUserInfoByAccountIdOptionsInternal options2 = default(CopyExternalUserInfoByAccountIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outExternalUserInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_UserInfo_CopyExternalUserInfoByAccountId(base.InnerHandle, ref options2, out outExternalUserInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<ExternalUserInfoInternal, ExternalUserInfo>(outExternalUserInfo2, out outExternalUserInfo);
		if (outExternalUserInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_UserInfo_ExternalUserInfo_Release(outExternalUserInfo2);
		}
		return result;
	}

	public Result CopyExternalUserInfoByAccountType(ref CopyExternalUserInfoByAccountTypeOptions options, out ExternalUserInfo? outExternalUserInfo)
	{
		CopyExternalUserInfoByAccountTypeOptionsInternal options2 = default(CopyExternalUserInfoByAccountTypeOptionsInternal);
		options2.Set(ref options);
		IntPtr outExternalUserInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_UserInfo_CopyExternalUserInfoByAccountType(base.InnerHandle, ref options2, out outExternalUserInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<ExternalUserInfoInternal, ExternalUserInfo>(outExternalUserInfo2, out outExternalUserInfo);
		if (outExternalUserInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_UserInfo_ExternalUserInfo_Release(outExternalUserInfo2);
		}
		return result;
	}

	public Result CopyExternalUserInfoByIndex(ref CopyExternalUserInfoByIndexOptions options, out ExternalUserInfo? outExternalUserInfo)
	{
		CopyExternalUserInfoByIndexOptionsInternal options2 = default(CopyExternalUserInfoByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outExternalUserInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_UserInfo_CopyExternalUserInfoByIndex(base.InnerHandle, ref options2, out outExternalUserInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<ExternalUserInfoInternal, ExternalUserInfo>(outExternalUserInfo2, out outExternalUserInfo);
		if (outExternalUserInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_UserInfo_ExternalUserInfo_Release(outExternalUserInfo2);
		}
		return result;
	}

	public Result CopyUserInfo(ref CopyUserInfoOptions options, out UserInfoData? outUserInfo)
	{
		CopyUserInfoOptionsInternal options2 = default(CopyUserInfoOptionsInternal);
		options2.Set(ref options);
		IntPtr outUserInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_UserInfo_CopyUserInfo(base.InnerHandle, ref options2, out outUserInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<UserInfoDataInternal, UserInfoData>(outUserInfo2, out outUserInfo);
		if (outUserInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_UserInfo_Release(outUserInfo2);
		}
		return result;
	}

	public uint GetExternalUserInfoCount(ref GetExternalUserInfoCountOptions options)
	{
		GetExternalUserInfoCountOptionsInternal options2 = default(GetExternalUserInfoCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_UserInfo_GetExternalUserInfoCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetLocalPlatformType(ref GetLocalPlatformTypeOptions options)
	{
		GetLocalPlatformTypeOptionsInternal options2 = default(GetLocalPlatformTypeOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_UserInfo_GetLocalPlatformType(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryUserInfo(ref QueryUserInfoOptions options, object clientData, OnQueryUserInfoCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryUserInfoOptionsInternal options2 = default(QueryUserInfoOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_UserInfo_QueryUserInfo(base.InnerHandle, ref options2, clientDataPointer, OnQueryUserInfoCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryUserInfoByDisplayName(ref QueryUserInfoByDisplayNameOptions options, object clientData, OnQueryUserInfoByDisplayNameCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryUserInfoByDisplayNameOptionsInternal options2 = default(QueryUserInfoByDisplayNameOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_UserInfo_QueryUserInfoByDisplayName(base.InnerHandle, ref options2, clientDataPointer, OnQueryUserInfoByDisplayNameCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryUserInfoByExternalAccount(ref QueryUserInfoByExternalAccountOptions options, object clientData, OnQueryUserInfoByExternalAccountCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryUserInfoByExternalAccountOptionsInternal options2 = default(QueryUserInfoByExternalAccountOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_UserInfo_QueryUserInfoByExternalAccount(base.InnerHandle, ref options2, clientDataPointer, OnQueryUserInfoByExternalAccountCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
