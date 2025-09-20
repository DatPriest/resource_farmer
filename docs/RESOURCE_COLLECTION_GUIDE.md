# Resource Collection System Guide

This guide explains how the basic resource collection system works in Resource Farmer.

## Overview

The resource collection system allows players to gather resources (wood, stone, ores, etc.) from resource nodes in the world by clicking on them. The system is built using S&box's modern Component architecture with proper server-side validation and client-side effects.

## System Components

### 1. ResourceType (Code/Resources/ResourceType.cs)

Defines all available resource types in the game:

- Basic resources: Wood, Stone, Fiber
- Ores: CopperOre, TinOre, IronOre, Coal, etc.
- Gems: Quartz, Ruby, Sapphire, etc.
- Magical: EssenceDust, CrystalShard, etc.

### 2. ResourceNode (Code/Resources/ResourceNode.cs)

Represents a gatherable resource in the world:

- **Properties**: ResourceType, Amount, Difficulty, Required Tool/Level
- **Features**: Visual highlighting, tool requirement checking, depletion
- **Networking**: Server-side validation, client-side effects

### 3. Player Components

#### PlayerInteractionComponent (Code/Player/PlayerInteractionComponent.cs)

- Handles input (Attack1/Attack2) and performs raycasting
- Server-side validation via RPC calls
- Uses camera or player eye position for raycast origin

#### PlayerGatheringComponent (Code/Player/PlayerGatheringComponent.cs)

- Processes gathering logic and tool bonuses
- Calculates final gather amount based on tools and player level
- Handles both tool-based and hand gathering

### 4. Player (Code/Player/Player.cs)

- Contains the inventory system (Dictionary<ResourceType, float>)
- GatherResource method adds resources and grants experience
- Updates UI through ResourceManager

## How It Works

### Basic Flow

1. Player presses **left mouse button** (Attack1)
2. PlayerInteractionComponent performs raycast from camera/eye position
3. If a ResourceNode is hit, PlayerGatheringComponent.ProcessHit() is called
4. System checks tool requirements and calculates gather amount
5. ResourceNode.Gather() reduces node amount and calls Player.GatherResource()
6. Player.GatherResource() updates inventory and UI
7. ResourceNode plays hit effects and may be destroyed when depleted

### Tool Requirements

- **None**: Can be gathered by hand (reduced efficiency)
- **Specific Tool**: Requires matching tool type and minimum level
- **Hand Gathering**: Allowed for basic resources like wood/fiber

### Amount Calculation

```csharp
finalAmount = BaseGatherAmountPerHit * toolMultiplier * playerLevelBonus * critMultiplier
```

## Usage Instructions

### For Players

1. **Basic Gathering**: Left-click on resource nodes to gather
2. **Tool Usage**: Equip appropriate tools for better efficiency
3. **Visual Cues**: Resource nodes have colored outlines:
   - Green: Easy to gather
   - Yellow: Moderate difficulty
   - Orange: Hard to gather
   - Red: Wrong tool or too difficult

### For Developers

#### Creating Resource Nodes

1. Create a GameObject with ResourceNode component
2. Set ResourceType, Amount range, and tool requirements
3. Add colliders and visual effects as needed
4. ResourceSpawner can automatically place nodes in the world

#### Adding New Resources

1. Add new entry to ResourceType enum
2. Update ResourceSpawner tool requirements if needed
3. Add sell values in Player.SellResources() method
4. Update UI to display new resource type

#### Testing the System

Use the ResourceCollectionTest component (Code/Test/ResourceCollectionTest.cs):

1. Attach to a test object in the scene
2. Assign TestPlayer and TestResourceNode references
3. Check "RunTest" to verify system functionality

## Configuration

### Input Controls

- **Attack1**: Left Mouse Button (mouse1) - Primary gather action
- **Attack2**: Right Mouse Button (mouse2) - Secondary interaction

### Key Properties

- **BaseGatherAmountPerHit**: Amount gathered per successful hit (default: 1.0)
- **InteractionDistance**: Maximum raycast distance for gathering (default: 100)
- **Player.BaseAttackRate**: Cooldown between gather attempts (default: 1 second)

## Networking

The system uses modern S&box networking patterns:

- **Server Authority**: All gathering logic runs on server
- **Client Prediction**: Visual/audio effects play immediately
- **RPC Communication**: `[Rpc.Broadcast]` for server-to-client calls
- **State Sync**: `[Sync]` properties for inventory and resource amounts

## UI Integration

The HUD (Code/UI/HUD.razor) automatically displays:

- Current inventory with resource counts
- Player stats (money, level, experience)
- Equipped tool information

UI updates are handled through:

- ResourceManager.UpdateInventory() calls
- Event-driven updates when inventory changes
- Real-time synchronization across clients

## Extension Points

### Adding New Gathering Mechanics

- Implement custom IGatherable interfaces
- Create specialized components for different resource types
- Add new tool bonus types through ToolBonusRegistry

### Custom Resource Behaviors

- Override ResourceNode.Gather() for special effects
- Add resource regeneration systems
- Implement season-dependent resource availability

## Troubleshooting

### Common Issues

1. **No gathering happens**: Check tool requirements and player level
2. **Resources not visible**: Verify ResourceSpawner configuration
3. **UI not updating**: Ensure ResourceManager.Instance exists in scene
4. **Networking issues**: Verify server-side execution of gathering logic

### Debug Tools

- Enable debug logging in PlayerGatheringComponent
- Use ResourceCollectionTest for systematic testing
- Check console for RPC call confirmations
