namespace DieMob;

public class Config
{
	public int UpdateInterval = 1000;
	public float RepelPowerModifier = 1.0f;
	/// <summary>
	/// A "stretch" that goes outside the area of the TShock region, to already kill mobs in that outer stretch.
	/// </summary>
	public int RegionStretch = 10;
}
