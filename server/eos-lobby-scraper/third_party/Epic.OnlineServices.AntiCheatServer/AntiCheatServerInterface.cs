using System;
using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatServer;

public sealed class AntiCheatServerInterface : Handle
{
	public const int ADDNOTIFYCLIENTACTIONREQUIRED_API_LATEST = 1;

	public const int ADDNOTIFYCLIENTAUTHSTATUSCHANGED_API_LATEST = 1;

	public const int ADDNOTIFYMESSAGETOCLIENT_API_LATEST = 1;

	public const int BEGINSESSION_API_LATEST = 3;

	public const int BEGINSESSION_MAX_REGISTERTIMEOUT = 120;

	public const int BEGINSESSION_MIN_REGISTERTIMEOUT = 10;

	public const int ENDSESSION_API_LATEST = 1;

	public const int GETPROTECTMESSAGEOUTPUTLENGTH_API_LATEST = 1;

	public const int ONMESSAGETOCLIENTCALLBACK_MAX_MESSAGE_SIZE = 512;

	public const int PROTECTMESSAGE_API_LATEST = 1;

	public const int RECEIVEMESSAGEFROMCLIENT_API_LATEST = 1;

	public const int REGISTERCLIENT_API_LATEST = 3;

	public const int SETCLIENTNETWORKSTATE_API_LATEST = 1;

	public const int UNPROTECTMESSAGE_API_LATEST = 1;

	public const int UNREGISTERCLIENT_API_LATEST = 1;

	public AntiCheatServerInterface()
	{
	}

	public AntiCheatServerInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyClientActionRequired(ref AddNotifyClientActionRequiredOptions options, object clientData, OnClientActionRequiredCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyClientActionRequiredOptionsInternal options2 = default(AddNotifyClientActionRequiredOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_AntiCheatServer_AddNotifyClientActionRequired(base.InnerHandle, ref options2, clientDataPointer, OnClientActionRequiredCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyClientAuthStatusChanged(ref AddNotifyClientAuthStatusChangedOptions options, object clientData, OnClientAuthStatusChangedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyClientAuthStatusChangedOptionsInternal options2 = default(AddNotifyClientAuthStatusChangedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_AntiCheatServer_AddNotifyClientAuthStatusChanged(base.InnerHandle, ref options2, clientDataPointer, OnClientAuthStatusChangedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyMessageToClient(ref AddNotifyMessageToClientOptions options, object clientData, OnMessageToClientCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyMessageToClientOptionsInternal options2 = default(AddNotifyMessageToClientOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_AntiCheatServer_AddNotifyMessageToClient(base.InnerHandle, ref options2, clientDataPointer, OnMessageToClientCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result BeginSession(ref BeginSessionOptions options)
	{
		BeginSessionOptionsInternal options2 = default(BeginSessionOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_BeginSession(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result EndSession(ref EndSessionOptions options)
	{
		EndSessionOptionsInternal options2 = default(EndSessionOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_EndSession(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result GetProtectMessageOutputLength(ref GetProtectMessageOutputLengthOptions options, out uint outBufferSizeBytes)
	{
		GetProtectMessageOutputLengthOptionsInternal options2 = default(GetProtectMessageOutputLengthOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_GetProtectMessageOutputLength(base.InnerHandle, ref options2, out outBufferSizeBytes);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogEvent(ref LogEventOptions options)
	{
		LogEventOptionsInternal options2 = default(LogEventOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogEvent(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogGameRoundEnd(ref LogGameRoundEndOptions options)
	{
		LogGameRoundEndOptionsInternal options2 = default(LogGameRoundEndOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogGameRoundEnd(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogGameRoundStart(ref LogGameRoundStartOptions options)
	{
		LogGameRoundStartOptionsInternal options2 = default(LogGameRoundStartOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogGameRoundStart(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogPlayerDespawn(ref LogPlayerDespawnOptions options)
	{
		LogPlayerDespawnOptionsInternal options2 = default(LogPlayerDespawnOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogPlayerDespawn(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogPlayerRevive(ref LogPlayerReviveOptions options)
	{
		LogPlayerReviveOptionsInternal options2 = default(LogPlayerReviveOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogPlayerRevive(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogPlayerSpawn(ref LogPlayerSpawnOptions options)
	{
		LogPlayerSpawnOptionsInternal options2 = default(LogPlayerSpawnOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogPlayerSpawn(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogPlayerTakeDamage(ref LogPlayerTakeDamageOptions options)
	{
		LogPlayerTakeDamageOptionsInternal options2 = default(LogPlayerTakeDamageOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogPlayerTakeDamage(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogPlayerTick(ref LogPlayerTickOptions options)
	{
		LogPlayerTickOptionsInternal options2 = default(LogPlayerTickOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogPlayerTick(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogPlayerUseAbility(ref LogPlayerUseAbilityOptions options)
	{
		LogPlayerUseAbilityOptionsInternal options2 = default(LogPlayerUseAbilityOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogPlayerUseAbility(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result LogPlayerUseWeapon(ref LogPlayerUseWeaponOptions options)
	{
		LogPlayerUseWeaponOptionsInternal options2 = default(LogPlayerUseWeaponOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_LogPlayerUseWeapon(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result ProtectMessage(ref ProtectMessageOptions options, ArraySegment<byte> outBuffer, out uint outBytesWritten)
	{
		ProtectMessageOptionsInternal options2 = default(ProtectMessageOptionsInternal);
		options2.Set(ref options);
		IntPtr value = Helper.AddPinnedBuffer(outBuffer);
		Result result = Bindings.EOS_AntiCheatServer_ProtectMessage(base.InnerHandle, ref options2, value, out outBytesWritten);
		Helper.Dispose(ref options2);
		Helper.Dispose(ref value);
		return result;
	}

	public Result ReceiveMessageFromClient(ref ReceiveMessageFromClientOptions options)
	{
		ReceiveMessageFromClientOptionsInternal options2 = default(ReceiveMessageFromClientOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_ReceiveMessageFromClient(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result RegisterClient(ref RegisterClientOptions options)
	{
		RegisterClientOptionsInternal options2 = default(RegisterClientOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_RegisterClient(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result RegisterEvent(ref RegisterEventOptions options)
	{
		RegisterEventOptionsInternal options2 = default(RegisterEventOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_RegisterEvent(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void RemoveNotifyClientActionRequired(ulong notificationId)
	{
		Bindings.EOS_AntiCheatServer_RemoveNotifyClientActionRequired(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyClientAuthStatusChanged(ulong notificationId)
	{
		Bindings.EOS_AntiCheatServer_RemoveNotifyClientAuthStatusChanged(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyMessageToClient(ulong notificationId)
	{
		Bindings.EOS_AntiCheatServer_RemoveNotifyMessageToClient(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public Result SetClientDetails(ref SetClientDetailsOptions options)
	{
		SetClientDetailsOptionsInternal options2 = default(SetClientDetailsOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_SetClientDetails(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetClientNetworkState(ref SetClientNetworkStateOptions options)
	{
		SetClientNetworkStateOptionsInternal options2 = default(SetClientNetworkStateOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_SetClientNetworkState(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetGameSessionId(ref SetGameSessionIdOptions options)
	{
		SetGameSessionIdOptionsInternal options2 = default(SetGameSessionIdOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_SetGameSessionId(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result UnprotectMessage(ref UnprotectMessageOptions options, ArraySegment<byte> outBuffer, out uint outBytesWritten)
	{
		UnprotectMessageOptionsInternal options2 = default(UnprotectMessageOptionsInternal);
		options2.Set(ref options);
		IntPtr value = Helper.AddPinnedBuffer(outBuffer);
		Result result = Bindings.EOS_AntiCheatServer_UnprotectMessage(base.InnerHandle, ref options2, value, out outBytesWritten);
		Helper.Dispose(ref options2);
		Helper.Dispose(ref value);
		return result;
	}

	public Result UnregisterClient(ref UnregisterClientOptions options)
	{
		UnregisterClientOptionsInternal options2 = default(UnregisterClientOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_AntiCheatServer_UnregisterClient(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}
}
