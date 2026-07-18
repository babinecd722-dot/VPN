using System;

namespace Epic.OnlineServices.UI;

internal struct RectInternal : ISettable<Rect>, IDisposable
{
	private int m_ApiVersion;

	private int m_X;

	private int m_Y;

	private uint m_Width;

	private uint m_Height;

	public void Set(ref Rect other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_X = other.X;
		m_Y = other.Y;
		m_Width = other.Width;
		m_Height = other.Height;
	}

	public void Dispose()
	{
	}
}
