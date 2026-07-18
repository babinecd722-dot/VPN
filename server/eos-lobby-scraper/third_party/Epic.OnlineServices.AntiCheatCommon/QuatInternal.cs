using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct QuatInternal : ISettable<Quat>, IDisposable
{
	private float m_w;

	private float m_x;

	private float m_y;

	private float m_z;

	public void Set(ref Quat other)
	{
		Dispose();
		m_w = other.w;
		m_x = other.x;
		m_y = other.y;
		m_z = other.z;
	}

	public void Dispose()
	{
	}
}
