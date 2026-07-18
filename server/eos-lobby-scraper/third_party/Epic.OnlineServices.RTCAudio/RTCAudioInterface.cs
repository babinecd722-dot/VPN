using System;

namespace Epic.OnlineServices.RTCAudio;

public sealed class RTCAudioInterface : Handle
{
	public const int ADDNOTIFYAUDIOBEFORERENDER_API_LATEST = 1;

	public const int ADDNOTIFYAUDIOBEFORESEND_API_LATEST = 1;

	public const int ADDNOTIFYAUDIODEVICESCHANGED_API_LATEST = 1;

	public const int ADDNOTIFYAUDIOINPUTSTATE_API_LATEST = 1;

	public const int ADDNOTIFYAUDIOOUTPUTSTATE_API_LATEST = 1;

	public const int ADDNOTIFYPARTICIPANTUPDATED_API_LATEST = 1;

	public const int AUDIOBUFFER_API_LATEST = 1;

	public const int AUDIOINPUTDEVICEINFO_API_LATEST = 1;

	public const int AUDIOOUTPUTDEVICEINFO_API_LATEST = 1;

	public const int COPYINPUTDEVICEINFORMATIONBYINDEX_API_LATEST = 1;

	public const int COPYOUTPUTDEVICEINFORMATIONBYINDEX_API_LATEST = 1;

	public const int GETAUDIOINPUTDEVICEBYINDEX_API_LATEST = 1;

	public const int GETAUDIOINPUTDEVICESCOUNT_API_LATEST = 1;

	public const int GETAUDIOOUTPUTDEVICEBYINDEX_API_LATEST = 1;

	public const int GETAUDIOOUTPUTDEVICESCOUNT_API_LATEST = 1;

	public const int GETINPUTDEVICESCOUNT_API_LATEST = 1;

	public const int GETOUTPUTDEVICESCOUNT_API_LATEST = 1;

	public const int INPUTDEVICEINFORMATION_API_LATEST = 1;

	public const int OUTPUTDEVICEINFORMATION_API_LATEST = 1;

	public const int QUERYINPUTDEVICESINFORMATION_API_LATEST = 1;

	public const int QUERYOUTPUTDEVICESINFORMATION_API_LATEST = 1;

	public const int REGISTERPLATFORMAUDIOUSER_API_LATEST = 1;

	public const int REGISTERPLATFORMUSER_API_LATEST = 1;

	public const int SENDAUDIO_API_LATEST = 1;

	public const int SETAUDIOINPUTSETTINGS_API_LATEST = 1;

	public const int SETAUDIOOUTPUTSETTINGS_API_LATEST = 1;

	public const int SETINPUTDEVICESETTINGS_API_LATEST = 1;

	public const int SETOUTPUTDEVICESETTINGS_API_LATEST = 1;

	public const int UNREGISTERPLATFORMAUDIOUSER_API_LATEST = 1;

	public const int UNREGISTERPLATFORMUSER_API_LATEST = 1;

	public const int UPDATEPARTICIPANTVOLUME_API_LATEST = 1;

	public const int UPDATERECEIVINGVOLUME_API_LATEST = 1;

	public const int UPDATERECEIVING_API_LATEST = 1;

	public const int UPDATESENDINGVOLUME_API_LATEST = 1;

	public const int UPDATESENDING_API_LATEST = 1;

	public RTCAudioInterface()
	{
	}

	public RTCAudioInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyAudioBeforeRender(ref AddNotifyAudioBeforeRenderOptions options, object clientData, OnAudioBeforeRenderCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyAudioBeforeRenderOptionsInternal options2 = default(AddNotifyAudioBeforeRenderOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTCAudio_AddNotifyAudioBeforeRender(base.InnerHandle, ref options2, clientDataPointer, OnAudioBeforeRenderCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyAudioBeforeSend(ref AddNotifyAudioBeforeSendOptions options, object clientData, OnAudioBeforeSendCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyAudioBeforeSendOptionsInternal options2 = default(AddNotifyAudioBeforeSendOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTCAudio_AddNotifyAudioBeforeSend(base.InnerHandle, ref options2, clientDataPointer, OnAudioBeforeSendCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyAudioDevicesChanged(ref AddNotifyAudioDevicesChangedOptions options, object clientData, OnAudioDevicesChangedCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyAudioDevicesChangedOptionsInternal options2 = default(AddNotifyAudioDevicesChangedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTCAudio_AddNotifyAudioDevicesChanged(base.InnerHandle, ref options2, clientDataPointer, OnAudioDevicesChangedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyAudioInputState(ref AddNotifyAudioInputStateOptions options, object clientData, OnAudioInputStateCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyAudioInputStateOptionsInternal options2 = default(AddNotifyAudioInputStateOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTCAudio_AddNotifyAudioInputState(base.InnerHandle, ref options2, clientDataPointer, OnAudioInputStateCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyAudioOutputState(ref AddNotifyAudioOutputStateOptions options, object clientData, OnAudioOutputStateCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyAudioOutputStateOptionsInternal options2 = default(AddNotifyAudioOutputStateOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTCAudio_AddNotifyAudioOutputState(base.InnerHandle, ref options2, clientDataPointer, OnAudioOutputStateCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyParticipantUpdated(ref AddNotifyParticipantUpdatedOptions options, object clientData, OnParticipantUpdatedCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyParticipantUpdatedOptionsInternal options2 = default(AddNotifyParticipantUpdatedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTCAudio_AddNotifyParticipantUpdated(base.InnerHandle, ref options2, clientDataPointer, OnParticipantUpdatedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result CopyInputDeviceInformationByIndex(ref CopyInputDeviceInformationByIndexOptions options, out InputDeviceInformation? outInputDeviceInformation)
	{
		CopyInputDeviceInformationByIndexOptionsInternal options2 = default(CopyInputDeviceInformationByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outInputDeviceInformation2 = IntPtr.Zero;
		Result result = Bindings.EOS_RTCAudio_CopyInputDeviceInformationByIndex(base.InnerHandle, ref options2, out outInputDeviceInformation2);
		Helper.Dispose(ref options2);
		Helper.Get<InputDeviceInformationInternal, InputDeviceInformation>(outInputDeviceInformation2, out outInputDeviceInformation);
		if (outInputDeviceInformation2 != IntPtr.Zero)
		{
			Bindings.EOS_RTCAudio_InputDeviceInformation_Release(outInputDeviceInformation2);
		}
		return result;
	}

	public Result CopyOutputDeviceInformationByIndex(ref CopyOutputDeviceInformationByIndexOptions options, out OutputDeviceInformation? outOutputDeviceInformation)
	{
		CopyOutputDeviceInformationByIndexOptionsInternal options2 = default(CopyOutputDeviceInformationByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr outOutputDeviceInformation2 = IntPtr.Zero;
		Result result = Bindings.EOS_RTCAudio_CopyOutputDeviceInformationByIndex(base.InnerHandle, ref options2, out outOutputDeviceInformation2);
		Helper.Dispose(ref options2);
		Helper.Get<OutputDeviceInformationInternal, OutputDeviceInformation>(outOutputDeviceInformation2, out outOutputDeviceInformation);
		if (outOutputDeviceInformation2 != IntPtr.Zero)
		{
			Bindings.EOS_RTCAudio_OutputDeviceInformation_Release(outOutputDeviceInformation2);
		}
		return result;
	}

	public AudioInputDeviceInfo? GetAudioInputDeviceByIndex(ref GetAudioInputDeviceByIndexOptions options)
	{
		GetAudioInputDeviceByIndexOptionsInternal options2 = default(GetAudioInputDeviceByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr intPtr = Bindings.EOS_RTCAudio_GetAudioInputDeviceByIndex(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get<AudioInputDeviceInfoInternal, AudioInputDeviceInfo>(intPtr, out AudioInputDeviceInfo? to);
		return to;
	}

	public uint GetAudioInputDevicesCount(ref GetAudioInputDevicesCountOptions options)
	{
		GetAudioInputDevicesCountOptionsInternal options2 = default(GetAudioInputDevicesCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_RTCAudio_GetAudioInputDevicesCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public AudioOutputDeviceInfo? GetAudioOutputDeviceByIndex(ref GetAudioOutputDeviceByIndexOptions options)
	{
		GetAudioOutputDeviceByIndexOptionsInternal options2 = default(GetAudioOutputDeviceByIndexOptionsInternal);
		options2.Set(ref options);
		IntPtr intPtr = Bindings.EOS_RTCAudio_GetAudioOutputDeviceByIndex(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get<AudioOutputDeviceInfoInternal, AudioOutputDeviceInfo>(intPtr, out AudioOutputDeviceInfo? to);
		return to;
	}

	public uint GetAudioOutputDevicesCount(ref GetAudioOutputDevicesCountOptions options)
	{
		GetAudioOutputDevicesCountOptionsInternal options2 = default(GetAudioOutputDevicesCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_RTCAudio_GetAudioOutputDevicesCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetInputDevicesCount(ref GetInputDevicesCountOptions options)
	{
		GetInputDevicesCountOptionsInternal options2 = default(GetInputDevicesCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_RTCAudio_GetInputDevicesCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public uint GetOutputDevicesCount(ref GetOutputDevicesCountOptions options)
	{
		GetOutputDevicesCountOptionsInternal options2 = default(GetOutputDevicesCountOptionsInternal);
		options2.Set(ref options);
		uint result = Bindings.EOS_RTCAudio_GetOutputDevicesCount(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void QueryInputDevicesInformation(ref QueryInputDevicesInformationOptions options, object clientData, OnQueryInputDevicesInformationCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryInputDevicesInformationOptionsInternal options2 = default(QueryInputDevicesInformationOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_QueryInputDevicesInformation(base.InnerHandle, ref options2, clientDataPointer, OnQueryInputDevicesInformationCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void QueryOutputDevicesInformation(ref QueryOutputDevicesInformationOptions options, object clientData, OnQueryOutputDevicesInformationCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		QueryOutputDevicesInformationOptionsInternal options2 = default(QueryOutputDevicesInformationOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_QueryOutputDevicesInformation(base.InnerHandle, ref options2, clientDataPointer, OnQueryOutputDevicesInformationCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result RegisterPlatformAudioUser(ref RegisterPlatformAudioUserOptions options)
	{
		RegisterPlatformAudioUserOptionsInternal options2 = default(RegisterPlatformAudioUserOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_RTCAudio_RegisterPlatformAudioUser(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void RegisterPlatformUser(ref RegisterPlatformUserOptions options, object clientData, OnRegisterPlatformUserCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		RegisterPlatformUserOptionsInternal options2 = default(RegisterPlatformUserOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_RegisterPlatformUser(base.InnerHandle, ref options2, clientDataPointer, OnRegisterPlatformUserCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyAudioBeforeRender(ulong notificationId)
	{
		Bindings.EOS_RTCAudio_RemoveNotifyAudioBeforeRender(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyAudioBeforeSend(ulong notificationId)
	{
		Bindings.EOS_RTCAudio_RemoveNotifyAudioBeforeSend(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyAudioDevicesChanged(ulong notificationId)
	{
		Bindings.EOS_RTCAudio_RemoveNotifyAudioDevicesChanged(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyAudioInputState(ulong notificationId)
	{
		Bindings.EOS_RTCAudio_RemoveNotifyAudioInputState(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyAudioOutputState(ulong notificationId)
	{
		Bindings.EOS_RTCAudio_RemoveNotifyAudioOutputState(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyParticipantUpdated(ulong notificationId)
	{
		Bindings.EOS_RTCAudio_RemoveNotifyParticipantUpdated(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public Result SendAudio(ref SendAudioOptions options)
	{
		SendAudioOptionsInternal options2 = default(SendAudioOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_RTCAudio_SendAudio(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetAudioInputSettings(ref SetAudioInputSettingsOptions options)
	{
		SetAudioInputSettingsOptionsInternal options2 = default(SetAudioInputSettingsOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_RTCAudio_SetAudioInputSettings(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetAudioOutputSettings(ref SetAudioOutputSettingsOptions options)
	{
		SetAudioOutputSettingsOptionsInternal options2 = default(SetAudioOutputSettingsOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_RTCAudio_SetAudioOutputSettings(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void SetInputDeviceSettings(ref SetInputDeviceSettingsOptions options, object clientData, OnSetInputDeviceSettingsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SetInputDeviceSettingsOptionsInternal options2 = default(SetInputDeviceSettingsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_SetInputDeviceSettings(base.InnerHandle, ref options2, clientDataPointer, OnSetInputDeviceSettingsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void SetOutputDeviceSettings(ref SetOutputDeviceSettingsOptions options, object clientData, OnSetOutputDeviceSettingsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		SetOutputDeviceSettingsOptionsInternal options2 = default(SetOutputDeviceSettingsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_SetOutputDeviceSettings(base.InnerHandle, ref options2, clientDataPointer, OnSetOutputDeviceSettingsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public Result UnregisterPlatformAudioUser(ref UnregisterPlatformAudioUserOptions options)
	{
		UnregisterPlatformAudioUserOptionsInternal options2 = default(UnregisterPlatformAudioUserOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_RTCAudio_UnregisterPlatformAudioUser(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void UnregisterPlatformUser(ref UnregisterPlatformUserOptions options, object clientData, OnUnregisterPlatformUserCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UnregisterPlatformUserOptionsInternal options2 = default(UnregisterPlatformUserOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_UnregisterPlatformUser(base.InnerHandle, ref options2, clientDataPointer, OnUnregisterPlatformUserCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateParticipantVolume(ref UpdateParticipantVolumeOptions options, object clientData, OnUpdateParticipantVolumeCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateParticipantVolumeOptionsInternal options2 = default(UpdateParticipantVolumeOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_UpdateParticipantVolume(base.InnerHandle, ref options2, clientDataPointer, OnUpdateParticipantVolumeCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateReceiving(ref UpdateReceivingOptions options, object clientData, OnUpdateReceivingCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateReceivingOptionsInternal options2 = default(UpdateReceivingOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_UpdateReceiving(base.InnerHandle, ref options2, clientDataPointer, OnUpdateReceivingCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateReceivingVolume(ref UpdateReceivingVolumeOptions options, object clientData, OnUpdateReceivingVolumeCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateReceivingVolumeOptionsInternal options2 = default(UpdateReceivingVolumeOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_UpdateReceivingVolume(base.InnerHandle, ref options2, clientDataPointer, OnUpdateReceivingVolumeCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateSending(ref UpdateSendingOptions options, object clientData, OnUpdateSendingCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateSendingOptionsInternal options2 = default(UpdateSendingOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_UpdateSending(base.InnerHandle, ref options2, clientDataPointer, OnUpdateSendingCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateSendingVolume(ref UpdateSendingVolumeOptions options, object clientData, OnUpdateSendingVolumeCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateSendingVolumeOptionsInternal options2 = default(UpdateSendingVolumeOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTCAudio_UpdateSendingVolume(base.InnerHandle, ref options2, clientDataPointer, OnUpdateSendingVolumeCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
