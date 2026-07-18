using System;

namespace Epic.OnlineServices.UI;

public sealed class UIInterface : Handle
{
	public const int ACKNOWLEDGECORRELATIONID_API_LATEST = 1;

	public const int ACKNOWLEDGEEVENTID_API_LATEST = 1;

	public const int ADDNOTIFYDISPLAYSETTINGSUPDATED_API_LATEST = 1;

	public const int ADDNOTIFYMEMORYMONITOROPTIONS_API_LATEST = 1;

	public const int ADDNOTIFYMEMORYMONITOR_API_LATEST = 1;

	public const int ADDNOTIFYONSCREENKEYBOARDREQUESTED_API_LATEST = 1;

	public const int CONFIGUREONSCREENKEYBOARD_API_LATEST = 1;

	public const int EVENTID_INVALID = 0;

	public const int GETFRIENDSEXCLUSIVEINPUT_API_LATEST = 1;

	public const int GETFRIENDSVISIBLE_API_LATEST = 1;

	public const int GETTOGGLEFRIENDSBUTTON_API_LATEST = 1;

	public const int GETTOGGLEFRIENDSKEY_API_LATEST = 1;

	public const int HIDEFRIENDS_API_LATEST = 1;

	public const int ISSOCIALOVERLAYPAUSED_API_LATEST = 1;

	public const int PAUSESOCIALOVERLAY_API_LATEST = 1;

	public const int PREPRESENT_API_LATEST = 1;

	public const int RECT_API_LATEST = 1;

	public const int REPORTINPUTSTATE_API_LATEST = 2;

	public const int SETDISPLAYPREFERENCE_API_LATEST = 1;

	public const int SETTOGGLEFRIENDSBUTTON_API_LATEST = 1;

	public const int SETTOGGLEFRIENDSKEY_API_LATEST = 1;

	public const int SHOWBLOCKPLAYER_API_LATEST = 1;

	public const int SHOWFRIENDS_API_LATEST = 1;

	public const int SHOWNATIVEPROFILE_API_LATEST = 1;

	public const int SHOWREPORTPLAYER_API_LATEST = 1;

	public UIInterface()
	{
	}

	public UIInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result AcknowledgeEventId(ref AcknowledgeEventIdOptions options)
	{
		AcknowledgeEventIdOptionsInternal options2 = default(AcknowledgeEventIdOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_UI_AcknowledgeEventId(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public ulong AddNotifyDisplaySettingsUpdated(ref AddNotifyDisplaySettingsUpdatedOptions options, object clientData, OnDisplaySettingsUpdatedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyDisplaySettingsUpdatedOptionsInternal options2 = default(AddNotifyDisplaySettingsUpdatedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_UI_AddNotifyDisplaySettingsUpdated(base.InnerHandle, ref options2, clientDataPointer, OnDisplaySettingsUpdatedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyMemoryMonitor(ref AddNotifyMemoryMonitorOptions options, object clientData, OnMemoryMonitorCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyMemoryMonitorOptionsInternal options2 = default(AddNotifyMemoryMonitorOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_UI_AddNotifyMemoryMonitor(base.InnerHandle, ref options2, clientDataPointer, OnMemoryMonitorCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public ulong AddNotifyOnScreenKeyboardRequested(ref AddNotifyOnScreenKeyboardRequestedOptions options, object clientData, OnScreenKeyboardRequestedCallback notificationFn)
	{
		if (notificationFn == null)
		{
			throw new ArgumentNullException("notificationFn");
		}
		AddNotifyOnScreenKeyboardRequestedOptionsInternal options2 = default(AddNotifyOnScreenKeyboardRequestedOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, notificationFn);
		ulong num = Bindings.EOS_UI_AddNotifyOnScreenKeyboardRequested(base.InnerHandle, ref options2, clientDataPointer, OnScreenKeyboardRequestedCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		Helper.AssignNotificationIdToCallback(clientDataPointer, num);
		return num;
	}

	public Result ConfigureOnScreenKeyboard(ref ConfigureOnScreenKeyboardOptions options)
	{
		ConfigureOnScreenKeyboardOptionsInternal options2 = default(ConfigureOnScreenKeyboardOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_UI_ConfigureOnScreenKeyboard(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public bool GetFriendsExclusiveInput(ref GetFriendsExclusiveInputOptions options)
	{
		GetFriendsExclusiveInputOptionsInternal options2 = default(GetFriendsExclusiveInputOptionsInternal);
		options2.Set(ref options);
		int num = Bindings.EOS_UI_GetFriendsExclusiveInput(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(num, out bool to);
		return to;
	}

	public bool GetFriendsVisible(ref GetFriendsVisibleOptions options)
	{
		GetFriendsVisibleOptionsInternal options2 = default(GetFriendsVisibleOptionsInternal);
		options2.Set(ref options);
		int num = Bindings.EOS_UI_GetFriendsVisible(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(num, out bool to);
		return to;
	}

	public NotificationLocation GetNotificationLocationPreference()
	{
		return Bindings.EOS_UI_GetNotificationLocationPreference(base.InnerHandle);
	}

	public InputStateButtonFlags GetToggleFriendsButton(ref GetToggleFriendsButtonOptions options)
	{
		GetToggleFriendsButtonOptionsInternal options2 = default(GetToggleFriendsButtonOptionsInternal);
		options2.Set(ref options);
		InputStateButtonFlags result = Bindings.EOS_UI_GetToggleFriendsButton(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public KeyCombination GetToggleFriendsKey(ref GetToggleFriendsKeyOptions options)
	{
		GetToggleFriendsKeyOptionsInternal options2 = default(GetToggleFriendsKeyOptionsInternal);
		options2.Set(ref options);
		KeyCombination result = Bindings.EOS_UI_GetToggleFriendsKey(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void HideFriends(ref HideFriendsOptions options, object clientData, OnHideFriendsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		HideFriendsOptionsInternal options2 = default(HideFriendsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_UI_HideFriends(base.InnerHandle, ref options2, clientDataPointer, OnHideFriendsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public bool IsSocialOverlayPaused(ref IsSocialOverlayPausedOptions options)
	{
		IsSocialOverlayPausedOptionsInternal options2 = default(IsSocialOverlayPausedOptionsInternal);
		options2.Set(ref options);
		int num = Bindings.EOS_UI_IsSocialOverlayPaused(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		Helper.Get(num, out bool to);
		return to;
	}

	public bool IsValidButtonCombination(InputStateButtonFlags buttonCombination)
	{
		Helper.Get(Bindings.EOS_UI_IsValidButtonCombination(base.InnerHandle, buttonCombination), out bool to);
		return to;
	}

	public bool IsValidKeyCombination(KeyCombination keyCombination)
	{
		Helper.Get(Bindings.EOS_UI_IsValidKeyCombination(base.InnerHandle, keyCombination), out bool to);
		return to;
	}

	public Result PauseSocialOverlay(ref PauseSocialOverlayOptions options)
	{
		PauseSocialOverlayOptionsInternal options2 = default(PauseSocialOverlayOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_UI_PauseSocialOverlay(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result PrePresent(ref PrePresentOptions options)
	{
		PrePresentOptionsInternal options2 = default(PrePresentOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_UI_PrePresent(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void RemoveNotifyDisplaySettingsUpdated(ulong id)
	{
		Bindings.EOS_UI_RemoveNotifyDisplaySettingsUpdated(base.InnerHandle, id);
		Helper.RemoveCallbackByNotificationId(id);
	}

	public void RemoveNotifyMemoryMonitor(ulong id)
	{
		Bindings.EOS_UI_RemoveNotifyMemoryMonitor(base.InnerHandle, id);
		Helper.RemoveCallbackByNotificationId(id);
	}

	public void RemoveNotifyOnScreenKeyboardRequested(ulong id)
	{
		Bindings.EOS_UI_RemoveNotifyOnScreenKeyboardRequested(base.InnerHandle, id);
		Helper.RemoveCallbackByNotificationId(id);
	}

	public Result ReportInputState(ref ReportInputStateOptions options)
	{
		ReportInputStateOptionsInternal options2 = default(ReportInputStateOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_UI_ReportInputState(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetDisplayPreference(ref SetDisplayPreferenceOptions options)
	{
		SetDisplayPreferenceOptionsInternal options2 = default(SetDisplayPreferenceOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_UI_SetDisplayPreference(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetToggleFriendsButton(ref SetToggleFriendsButtonOptions options)
	{
		SetToggleFriendsButtonOptionsInternal options2 = default(SetToggleFriendsButtonOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_UI_SetToggleFriendsButton(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public Result SetToggleFriendsKey(ref SetToggleFriendsKeyOptions options)
	{
		SetToggleFriendsKeyOptionsInternal options2 = default(SetToggleFriendsKeyOptionsInternal);
		options2.Set(ref options);
		Result result = Bindings.EOS_UI_SetToggleFriendsKey(base.InnerHandle, ref options2);
		Helper.Dispose(ref options2);
		return result;
	}

	public void ShowBlockPlayer(ref ShowBlockPlayerOptions options, object clientData, OnShowBlockPlayerCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		ShowBlockPlayerOptionsInternal options2 = default(ShowBlockPlayerOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_UI_ShowBlockPlayer(base.InnerHandle, ref options2, clientDataPointer, OnShowBlockPlayerCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void ShowFriends(ref ShowFriendsOptions options, object clientData, OnShowFriendsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		ShowFriendsOptionsInternal options2 = default(ShowFriendsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_UI_ShowFriends(base.InnerHandle, ref options2, clientDataPointer, OnShowFriendsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void ShowNativeProfile(ref ShowNativeProfileOptions options, object clientData, OnShowNativeProfileCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		ShowNativeProfileOptionsInternal options2 = default(ShowNativeProfileOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_UI_ShowNativeProfile(base.InnerHandle, ref options2, clientDataPointer, OnShowNativeProfileCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void ShowReportPlayer(ref ShowReportPlayerOptions options, object clientData, OnShowReportPlayerCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		ShowReportPlayerOptionsInternal options2 = default(ShowReportPlayerOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_UI_ShowReportPlayer(base.InnerHandle, ref options2, clientDataPointer, OnShowReportPlayerCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
