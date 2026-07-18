using System;

namespace Epic.OnlineServices.Sanctions;

internal struct PlayerSanctionInternal : IGettable<PlayerSanction>
{
	private int m_ApiVersion;

	private long m_TimePlaced;

	private IntPtr m_Action;

	private long m_TimeExpires;

	private IntPtr m_ReferenceId;

	public void Get(out PlayerSanction other)
	{
		other = default(PlayerSanction);
		other.TimePlaced = m_TimePlaced;
		Helper.Get(m_Action, out Utf8String to);
		other.Action = to;
		other.TimeExpires = m_TimeExpires;
		Helper.Get(m_ReferenceId, out Utf8String to2);
		other.ReferenceId = to2;
	}
}
