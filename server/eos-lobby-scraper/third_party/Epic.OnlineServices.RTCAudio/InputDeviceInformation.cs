namespace Epic.OnlineServices.RTCAudio;

public struct InputDeviceInformation
{
	public bool DefaultDevice { get; set; }

	public Utf8String DeviceId { get; set; }

	public Utf8String DeviceName { get; set; }
}
