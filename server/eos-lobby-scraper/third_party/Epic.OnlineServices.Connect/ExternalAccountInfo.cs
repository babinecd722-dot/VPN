using System;

namespace Epic.OnlineServices.Connect;

public struct ExternalAccountInfo
{
	public ProductUserId ProductUserId { get; set; }

	public Utf8String DisplayName { get; set; }

	public Utf8String AccountId { get; set; }

	public ExternalAccountType AccountIdType { get; set; }

	public DateTimeOffset? LastLoginTime { get; set; }
}
