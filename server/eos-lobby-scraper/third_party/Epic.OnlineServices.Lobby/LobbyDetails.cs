using System;

namespace Epic.OnlineServices.Lobby;

public sealed class LobbyDetails : Handle
{
	public LobbyDetails()
	{
	}

	public LobbyDetails(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyAttributeByIndex(ref LobbyDetailsCopyAttributeByIndexOptions options, out Attribute? outAttribute)
	{
		LobbyDetailsCopyAttributeByIndexOptionsInternal options2 = default(LobbyDetailsCopyAttributeByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outAttribute2 = IntPtr.Zero;
		Result result = Bindings.EOS_LobbyDetails_CopyAttributeByIndex(base.InnerHandle, ref options2, out outAttribute2);
		Helper.Dispose(ref options2);
		Helper.Get<AttributeInternal, Attribute>(outAttribute2, out outAttribute);
		if (outAttribute2 != IntPtr.Zero)
		{
			Bindings.EOS_Lobby_Attribute_Release(outAttribute2);
		}
		return result;
	}

	public Result CopyAttributeByKey(ref LobbyDetailsCopyAttributeByKeyOptions options, out Attribute? outAttribute)
	{
		LobbyDetailsCopyAttributeByKeyOptionsInternal options2 = default(LobbyDetailsCopyAttributeByKeyOptionsInternal);
		options2.Set(ref options);
		IntPtr outAttribute2 = IntPtr.Zero;
		Result result = Bindings.EOS_LobbyDetails_CopyAttributeByKey(base.InnerHandle, ref options2, out outAttribute2);
		Helper.Dispose(ref options2);
		Helper.Get<AttributeInternal, Attribute>(outAttribute2, out outAttribute);
		if (outAttribute2 != IntPtr.Zero)
		{
			Bindings.EOS_Lobby_Attribute_Release(outAttribute2);
		}
		return result;
	}

	public Result CopyInfo(ref LobbyDetailsCopyInfoOptions options, out LobbyDetailsInfo? outLobbyDetailsInfo)
	{
		LobbyDetailsCopyInfoOptionsInternal options2 = default(LobbyDetailsCopyInfoOptionsInternal);
		options2.Set(ref options);
		IntPtr outLobbyDetailsInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_LobbyDetails_CopyInfo(base.InnerHandle, ref options2, out outLobbyDetailsInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<LobbyDetailsInfoInternal, LobbyDetailsInfo>(outLobbyDetailsInfo2, out outLobbyDetailsInfo);
		if (outLobbyDetailsInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_LobbyDetails_Info_Release(outLobbyDetailsInfo2);
		}
		return result;
	}

	public Result CopyMemberAttributeByIndex(ref LobbyDetailsCopyMemberAttributeByIndexOptions options, out Attribute? outAttribute)
	{
		LobbyDetailsCopyMemberAttributeByIndexOptionsInternal options2 = default(LobbyDetailsCopyMemberAttributeByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outAttribute2 = IntPtr.Zero;
		Result result = Bindings.EOS_LobbyDetails_CopyMemberAttributeByIndex(base.InnerHandle, ref options2, out outAttribute2);
		Helper.Dispose(ref options2);
		Helper.Get<AttributeInternal, Attribute>(outAttribute2, out outAttribute);
		if (outAttribute2 != IntPtr.Zero)
		{
			Bindings.EOS_Lobby_Attribute_Release(outAttribute2);
		}
		return result;
	}

	public Result CopyMemberAttributeByKey(ref LobbyDetailsCopyMemberAttributeByKeyOptions options, out Attribute? outAttribute)
	{
		LobbyDetailsCopyMemberAttributeByKeyOptionsInternal options2 = default(LobbyDetailsCopyMemberAttributeByKeyOptionsInternal);
		options2.Set(ref options);
		IntPtr outAttribute2 = IntPtr.Zero;
		Result result = Bindings.EOS_LobbyDetails_CopyMemberAttributeByKey(base.InnerHandle, ref options2, out outAttribute2);
		Helper.Dispose(ref options2);
		Helper.Get<AttributeInternal, Attribute>(outAttribute2, out outAttribute);
		if (outAttribute2 != IntPtr.Zero)
		{
			Bindings.EOS_Lobby_Attribute_Release(outAttribute2);
		}
		return result;
	}

	public Result CopyMemberInfo(ref LobbyDetailsCopyMemberInfoOptions options, out LobbyDetailsMemberInfo? outLobbyDetailsMemberInfo)
	{
		LobbyDetailsCopyMemberInfoOptionsInternal options2 = default(LobbyDetailsCopyMemberInfoOptionsInternal);
		options2.Set(ref options);
		IntPtr outLobbyDetailsMemberInfo2 = IntPtr.Zero;
		Result result = Bindings.EOS_LobbyDetails_CopyMemberInfo(base.InnerHandle, ref options2, out outLobbyDetailsMemberInfo2);
		Helper.Dispose(ref options2);
		Helper.Get<LobbyDetailsMemberInfoInternal, LobbyDetailsMemberInfo>(outLobbyDetailsMemberInfo2, out outLobbyDetailsMemberInfo);
		if (outLobbyDetailsMemberInfo2 != IntPtr.Zero)
		{
			Bindings.EOS_LobbyDetails_MemberInfo_Release(outLobbyDetailsMemberInfo2);
		}
		return result;
	}

	public uint GetAttributeCount(ref LobbyDetailsGetAttributeCountOptions options)
	{
		LobbyDetailsGetAttributeCountOptionsInternal options2 = default(LobbyDetailsGetAttributeCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_LobbyDetails_GetAttributeCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public ProductUserId GetLobbyOwner(ref LobbyDetailsGetLobbyOwnerOptions options)
	{
		LobbyDetailsGetLobbyOwnerOptionsInternal options2 = default(LobbyDetailsGetLobbyOwnerOptionsInternal);
		options2.Set(ref options);
		IntPtr intPtr = Bindings.EOS_LobbyDetails_GetLobbyOwner(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(intPtr, out ProductUserId to);
		return to;
	}

	public uint GetMemberAttributeCount(ref LobbyDetailsGetMemberAttributeCountOptions options)
	{
		LobbyDetailsGetMemberAttributeCountOptionsInternal options2 = default(LobbyDetailsGetMemberAttributeCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_LobbyDetails_GetMemberAttributeCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public ProductUserId GetMemberByIndex(ref LobbyDetailsGetMemberByIndexOptions options)
	{
		LobbyDetailsGetMemberByIndexOptionsInternal options2 = default(LobbyDetailsGetMemberByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr intPtr = Bindings.EOS_LobbyDetails_GetMemberByIndex(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(intPtr, out ProductUserId to);
		return to;
	}

	public uint GetMemberCount(ref LobbyDetailsGetMemberCountOptions options)
	{
		LobbyDetailsGetMemberCountOptionsInternal options2 = default(LobbyDetailsGetMemberCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_LobbyDetails_GetMemberCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void Release()
	{
		Bindings.EOS_LobbyDetails_Release(base.InnerHandle);
	}
}
