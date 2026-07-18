using System;
using Epic.OnlineServices.RTCAudio;
using Epic.OnlineServices.RTCData;

namespace Epic.OnlineServices.RTC;

public sealed class RTCInterface : Handle
{
	public const int ADDNOTIFYDISCONNECTED_API_LATEST = 1;

	public const int ADDNOTIFYPARTICIPANTSTATUSCHANGED_API_LATEST = 1;

	public const int ADDNOTIFYROOMBEFOREJOIN_API_LATEST = 1;

	public const int ADDNOTIFYROOMSTATISTICSUPDATED_API_LATEST = 1;

	public const int BLOCKPARTICIPANT_API_LATEST = 1;

	public const int JOINROOM_API_LATEST = 1;

	public const int LEAVEROOM_API_LATEST = 1;

	public const int OPTION_API_LATEST = 1;

	public const int OPTION_KEY_MAXCHARCOUNT = 256;

	public const int OPTION_VALUE_MAXCHARCOUNT = 256;

	public const int PARTICIPANTMETADATA_API_LATEST = 1;

	public const int PARTICIPANTMETADATA_KEY_MAXCHARCOUNT = 256;

	public const int PARTICIPANTMETADATA_VALUE_MAXCHARCOUNT = 256;

	public const int SETROOMSETTING_API_LATEST = 1;

	public const int SETSETTING_API_LATEST = 1;

	public RTCInterface()
	{
	}

	public RTCInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public ulong AddNotifyDisconnected(ref AddNotifyDisconnectedOptions options, object clientData, OnDisconnectedCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyDisconnectedOptionsInternal options2 = default(AddNotifyDisconnectedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTC_AddNotifyDisconnected(base.InnerHandle, ref options2, clientDataPointer, OnDisconnectedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyParticipantStatusChanged(ref AddNotifyParticipantStatusChangedOptions options, object clientData, OnParticipantStatusChangedCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyParticipantStatusChangedOptionsInternal options2 = default(AddNotifyParticipantStatusChangedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTC_AddNotifyParticipantStatusChanged(base.InnerHandle, ref options2, clientDataPointer, OnParticipantStatusChangedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyRoomBeforeJoin(ref AddNotifyRoomBeforeJoinOptions options, object clientData, OnRoomBeforeJoinCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyRoomBeforeJoinOptionsInternal options2 = default(AddNotifyRoomBeforeJoinOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTC_AddNotifyRoomBeforeJoin(base.InnerHandle, ref options2, clientDataPointer, OnRoomBeforeJoinCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyRoomStatisticsUpdated(ref AddNotifyRoomStatisticsUpdatedOptions options, object clientData, OnRoomStatisticsUpdatedCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		AddNotifyRoomStatisticsUpdatedOptionsInternal options2 = default(AddNotifyRoomStatisticsUpdatedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		ulong num = Bindings.EOS_RTC_AddNotifyRoomStatisticsUpdated(base.InnerHandle, ref options2, clientDataPointer, OnRoomStatisticsUpdatedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public void BlockParticipant(ref BlockParticipantOptions options, object clientData, OnBlockParticipantCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		BlockParticipantOptionsInternal options2 = default(BlockParticipantOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTC_BlockParticipant(base.InnerHandle, ref options2, clientDataPointer, OnBlockParticipantCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public RTCAudioInterface GetAudioInterface()
	{
		Helper.Get(Bindings.EOS_RTC_GetAudioInterface(base.InnerHandle), out RTCAudioInterface to);
		return to;
	}

	public RTCDataInterface GetDataInterface()
	{
		Helper.Get(Bindings.EOS_RTC_GetDataInterface(base.InnerHandle), out RTCDataInterface to);
		return to;
	}

	public void JoinRoom(ref JoinRoomOptions options, object clientData, OnJoinRoomCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		JoinRoomOptionsInternal options2 = default(JoinRoomOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTC_JoinRoom(base.InnerHandle, ref options2, clientDataPointer, OnJoinRoomCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void LeaveRoom(ref LeaveRoomOptions options, object clientData, OnLeaveRoomCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		LeaveRoomOptionsInternal options2 = default(LeaveRoomOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_RTC_LeaveRoom(base.InnerHandle, ref options2, clientDataPointer, OnLeaveRoomCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void RemoveNotifyDisconnected(ulong notificationId)
	{
		Bindings.EOS_RTC_RemoveNotifyDisconnected(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyParticipantStatusChanged(ulong notificationId)
	{
		Bindings.EOS_RTC_RemoveNotifyParticipantStatusChanged(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyRoomBeforeJoin(ulong notificationId)
	{
		Bindings.EOS_RTC_RemoveNotifyRoomBeforeJoin(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public void RemoveNotifyRoomStatisticsUpdated(ulong notificationId)
	{
		Bindings.EOS_RTC_RemoveNotifyRoomStatisticsUpdated(base.InnerHandle, notificationId);
		Helper.RemoveCallbackByNotificationId(notificationId);
	}

	public Result SetRoomSetting(ref SetRoomSettingOptions options)
	{
		SetRoomSettingOptionsInternal options2 = default(SetRoomSettingOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_RTC_SetRoomSetting(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetSetting(ref SetSettingOptions options)
	{
		SetSettingOptionsInternal options2 = default(SetSettingOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_RTC_SetSetting(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}
}
