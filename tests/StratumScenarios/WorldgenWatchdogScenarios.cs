using System.Reflection;
using Atlas.Api;
using Atlas.XUnit;
using Xunit;

namespace StratumScenarios;

/// <summary>
/// The blocking-load watchdog counts finished stages for the requested
/// rectangle plus the neighbour ring (PreDone - TerrainLate, 4 chunks).
/// </summary>
public class WorldgenWatchdogScenarios : AtlasScenarioBase
{
	[AtlasScenario(TimeoutMs = 60_000)]
	public async Task RectangleProgress_Should_IgnoreColumnsOutsideTheRequest()
	{
		await World.Ticks(1);
		Type requestType = Type.GetType("Vintagestory.Server.ChunkColumnLoadRequest, VintagestoryLib")
			?? throw new InvalidOperationException("ChunkColumnLoadRequest was not loaded");
		Type supplyType = Type.GetType("Vintagestory.Server.ServerSystemSupplyChunks, VintagestoryLib")
			?? throw new InvalidOperationException("ServerSystemSupplyChunks was not loaded");
		MethodInfo progress = supplyType.GetMethod("StratumRectangleStageProgress", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("StratumRectangleStageProgress was not found");
		FieldInfo finished = requestType.GetField("stratumStagesFinished", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("stratumStagesFinished was not found");

		object inside = Activator.CreateInstance(requestType, 1L, 10, 12, 0, 7, World.Api.World)!;
		object ring = Activator.CreateInstance(requestType, 2L, 24, 12, 0, 7, World.Api.World)!;
		object beyond = Activator.CreateInstance(requestType, 3L, 25, 12, 0, 7, World.Api.World)!;
		Array columns = Array.CreateInstance(requestType, 3);
		columns.SetValue(inside, 0);
		columns.SetValue(ring, 1);
		columns.SetValue(beyond, 2);
		Assert.Equal(0, (int)progress.Invoke(null, new object[] { columns, 0, 0, 20, 20 })!);

		finished.SetValue(inside, 2);
		finished.SetValue(ring, 5);
		finished.SetValue(beyond, 9);
		Assert.Equal(7, (int)progress.Invoke(null, new object[] { columns, 0, 0, 20, 20 })!);

		finished.SetValue(inside, 3);
		Assert.Equal(8, (int)progress.Invoke(null, new object[] { columns, 0, 0, 20, 20 })!);
	}
}
