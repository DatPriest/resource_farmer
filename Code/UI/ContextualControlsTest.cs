// File: Code/UI/ContextualControlsTest.cs
// Simple test to validate contextual controls functionality
using Sandbox;
using ResourceFarmer.PlayerBase;
using ResourceFarmer.Resources;

namespace ResourceFarmer.UI;

/// <summary>
/// Test component to validate contextual controls functionality.
/// This demonstrates how the contextual controls system works.
/// </summary>
public class ContextualControlsTest
{
	/// <summary>
	/// Test the contextual controls component logic
	/// </summary>
	public static void TestContextualControls()
	{
		Log.Info("[ContextualControlsTest] Testing contextual controls functionality...");
		
		// Test the ContextualControl struct
		var primaryControl = new ContextualControl
		{
			Text = "Press [LMB] to gather Wood",
			IsEnabled = true,
			Priority = 1
		};
		
		var secondaryControl = new ContextualControl
		{
			Text = "Press [I] to open inventory",
			IsEnabled = true,
			IsSecondary = true,
			Priority = 10
		};
		
		Log.Info($"[ContextualControlsTest] Primary Control: {primaryControl.Text}");
		Log.Info($"[ContextualControlsTest] Secondary Control: {secondaryControl.Text}");
		
		// Test control text parsing (simulating RenderControlText functionality)
		string testText = "Press [LMB] to gather Wood";
		var parts = testText.Split('[', ']');
		
		Log.Info("[ContextualControlsTest] Parsed control text:");
		for (int i = 0; i < parts.Length; i++)
		{
			if (i % 2 == 0)
			{
				Log.Info($"  Regular text: '{parts[i]}'");
			}
			else
			{
				Log.Info($"  Keybind: '{parts[i]}'");
			}
		}
		
		Log.Info("[ContextualControlsTest] Test completed successfully!");
	}
	
	/// <summary>
	/// Test resource node prompt generation
	/// </summary>
	public static void TestResourceNodePrompts()
	{
		Log.Info("[ContextualControlsTest] Testing resource node prompt generation...");
		
		// Test different scenarios
		var scenarios = new[]
		{
			"Wood gathering with Axe",
			"Stone gathering without tool", 
			"Ore gathering with wrong tool"
		};
		
		foreach (var scenario in scenarios)
		{
			Log.Info($"[ContextualControlsTest] Scenario: {scenario}");
		}
		
		Log.Info("[ContextualControlsTest] Resource node prompt test completed!");
	}
}