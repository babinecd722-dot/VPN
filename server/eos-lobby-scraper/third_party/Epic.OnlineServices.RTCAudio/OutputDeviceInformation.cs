namespace Epic.OnlineServices.RTCAudio;

public struct OutputDeviceInformation
{
	public bool DefaultDevice { get; set; }

	public Utf8String DeviceId { get; set; }

	public Utf8String DeviceName { get; set; }
}
