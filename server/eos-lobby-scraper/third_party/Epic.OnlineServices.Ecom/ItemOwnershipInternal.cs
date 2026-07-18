using System;

namespace Epic.OnlineServices.Ecom;

internal struct ItemOwnershipInternal : IGettable<ItemOwnership>
{
	private int m_ApiVersion;

	private IntPtr m_Id;

	private OwnershipStatus m_OwnershipStatus;

	public void Get(out ItemOwnership other)
	{
		other = default(ItemOwnership);
		Helper.Get(m_Id, out Utf8String to);
		other.Id = to;
		other.OwnershipStatus = m_OwnershipStatus;
	}
}
