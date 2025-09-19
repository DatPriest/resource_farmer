using System;
namespace ResourceFarmer.Resources;
public static class ResourceTypeExtensions
{
	/// <summary>
	/// Gets a user-friendly display name for the resource type
	/// </summary>
	public static string GetDisplayName(this ResourceType type)
	{
		return type switch
		{
			ResourceType.None => "None",
			ResourceType.Wood => "Wood",
			ResourceType.Stone => "Stone",
			ResourceType.Fiber => "Fiber",
			ResourceType.CopperOre => "Copper Ore",
			ResourceType.TinOre => "Tin Ore",
			ResourceType.IronOre => "Iron Ore",
			ResourceType.Coal => "Coal",
			ResourceType.SilverOre => "Silver Ore",
			ResourceType.GoldOre => "Gold Ore",
			ResourceType.MithrilOre => "Mithril Ore",
			ResourceType.AdamantiteOre => "Adamantite Ore",
			ResourceType.Quartz => "Quartz",
			ResourceType.RubyRough => "Ruby (Rough)",
			ResourceType.SapphireRough => "Sapphire (Rough)",
			ResourceType.EmeraldRough => "Emerald (Rough)",
			ResourceType.DiamondRough => "Diamond (Rough)",
			ResourceType.CrystalShard => "Crystal Shard",
			ResourceType.EssenceDust => "Essence Dust",
			ResourceType.DragonScale => "Dragon Scale",
			ResourceType.PhoenixFeather => "Phoenix Feather",
			_ => type.ToString()
		};
	}
	
	/// <summary>
	/// Determines if a specific tool is generally required to gather this resource.
	/// Basic resources might be gatherable by hand (slowly).
	/// </summary>
	public static bool IsHardResource(this ResourceType type)
	{
		switch (type)
		{
			// Resources gatherable by hand (potentially)
			case ResourceType.None:
			case ResourceType.Wood:
			case ResourceType.Stone: // Maybe requires a basic tool? Debatable. Let's say yes for now.
			case ResourceType.Fiber:
			case ResourceType.EssenceDust: // Maybe gatherable by hand?
				return false;

			// Resources requiring tools
			case ResourceType.CopperOre:
			case ResourceType.TinOre:
			case ResourceType.IronOre:
			case ResourceType.Coal:
			case ResourceType.SilverOre:
			case ResourceType.GoldOre:
			case ResourceType.MithrilOre:
			case ResourceType.AdamantiteOre:
			case ResourceType.Quartz:
			case ResourceType.RubyRough:
			case ResourceType.SapphireRough:
			case ResourceType.EmeraldRough:
			case ResourceType.DiamondRough:
			case ResourceType.CrystalShard:
			case ResourceType.DragonScale: // Definitely need something!
			case ResourceType.PhoenixFeather: // Maybe?
				return true;

			default:
				return false; // Default to not hard if not specified
		}
	}
}
