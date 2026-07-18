using System;

namespace Epic.OnlineServices.RTCAdmin;

public sealed class RTCAdminInterface : Handle
{
	public const int COPYUSERTOKENBYINDEX_API_LATEST = 2;

	public const int COPYUSERTOKENBYUSERID_API_LATEST = 2;

	public const int KICK_API_LATEST = 1;

	public const int QUERYJOINROOMTOKEN_API_LATEST = 2;

	public const int SETPARTICIPANTHARDMUTE_API_LATEST = 1;

	public const int USERTOKEN_API_LATEST = 1;

	public RTCAdminInterface()
	{
	}

	public RTCAdminInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyUserTokenByIndex(ref CopyUserTokenByIndexOptions options, out UserToken? outUserToken)
	{
		CopyUserTokenByIndexOptionsInternal options2 = default(CopyUserTokenByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outUserToken2 = IntPtr.Zero;
		Result result = Bindings.EOS_RTCAdmin_CopyUserTokenByIndex(base.InnerHandle, ref options2, out outUserToken2);
		Helper.Dispose(ref options2);
		Helper.Get<UserTokenInternal, UserToken>(outUserToken2, out outUserToken);
		if (outUserToken2 != IntPtr.Zero)
		{
			Bindings.EOS_RTCAdmin_UserToken_Release(outUserToken2);
		}
		return result;
	}

	public Result CopyUserTokenByUserId(ref CopyUserTokenByUserIdOptions options, out UserToken? outUserToken)
	{
		CopyUserTokenByUserIdOptionsInternal options2 = default(CopyUserTokenByUserIdOptionsInternal);
		options2.Set(ref options);
		IntPtr outUserToken2 = IntPtr.Zero;
		Result result = Bindings.EOS_RTCAdmin_CopyUserTokenByUserId(base.InnerHandle, ref options2, out outUserToken2);
		Helper.Dispose(ref options2);
		Helper.Get<UserTokenInternal, UserToken>(outUserToken2, out outUserToken);
		if (outUserToken2 != IntPtr.Zero)
		{
			Bindings.EOS_RTCAdmin_UserToken_Release(outUserToken2);
		}
		return result;
	}

	public void Kick(ref KickOptions options, object clientData, OnKickCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		KickOptionsInternal options2 = default(KickOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAdmin_Kick(base.InnerHandle, ref options2, clientDataPointer, OnKickCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryJoinRoomToken(ref QueryJoinRoomTokenOptions options, object clientData, OnQueryJoinRoomTokenCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryJoinRoomTokenOptionsInternal options2 = default(QueryJoinRoomTokenOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAdmin_QueryJoinRoomToken(base.InnerHandle, ref options2, clientDataPointer, OnQueryJoinRoomTokenCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void SetParticipantHardMute(ref SetParticipantHardMuteOptions options, object clientData, OnSetParticipantHardMuteCompleteCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SetParticipantHardMuteOptionsInternal options2 = default(SetParticipantHardMuteOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAdmin_SetParticipantHardMute(base.InnerHandle, ref options2, clientDataPointer, OnSetParticipantHardMuteCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
