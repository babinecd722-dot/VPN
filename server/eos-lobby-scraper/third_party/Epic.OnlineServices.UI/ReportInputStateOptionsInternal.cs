using System;

namespace Epic.OnlineServices.UI;

internal struct ReportInputStateOptionsInternal : ISettable<ReportInputStateOptions>, IDisposable
{
	private int m_ApiVersion;

	private InputStateButtonFlags m_ButtonDownFlags;

	private int m_AcceptIsFaceButtonRight;

	private int m_MouseButtonDown;

	private uint m_MousePosX;

	private uint m_MousePosY;

	private uint m_GamepadIndex;

	private float m_LeftStickX;

	private float m_LeftStickY;

	private float m_RightStickX;

	private float m_RightStickY;

	private float m_LeftTrigger;

	private float m_RightTrigger;

	public void Set(ref ReportInputStateOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		m_ButtonDownFlags = other.ButtonDownFlags;
		Helper.Set(other.AcceptIsFaceButtonRight, ref m_AcceptIsFaceButtonRight);
		Helper.Set(other.MouseButtonDown, ref m_MouseButtonDown);
		m_MousePosX = other.MousePosX;
		m_MousePosY = other.MousePosY;
		m_GamepadIndex = other.GamepadIndex;
		m_LeftStickX = other.LeftStickX;
		m_LeftStickY = other.LeftStickY;
		m_RightStickX = other.RightStickX;
		m_RightStickY = other.RightStickY;
		m_LeftTrigger = other.LeftTrigger;
		m_RightTrigger = other.RightTrigger;
	}

	public void Dispose()
	{
	}
}
