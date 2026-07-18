using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct Vec3fInternal : ISettable<Vec3f>, IDisposable
{
	private float m_x;

	private float m_y;

	private float m_z;

	public void Set(ref Vec3f other)
	{
		Dispose();
		m_x = other.x;
		m_y = other.y;
		m_z = other.z;
	}

	public void Dispose()
	{
	}
}
