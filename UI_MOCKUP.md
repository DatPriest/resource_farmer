# Inventory System Visual Mockup

## Main HUD with Inventory Button

```
┌─────────────────────────────────────────────────────────────────┐
│ Stats Panel                Resources Panel               Tool    │
│ ┌─────────────┐          ╭─────────────────────────────╮ Panel   │
│ │ Money: $150 │          │ Wood: 45.0  Stone: 23.0    │ ┌─────┐ │
│ │ Level: 5    │          │ Iron: 12.0  Coal: 8.0  📦  │ │Wood │ │
│ │ EXP: 340/500│          ╰─────────────────────────────╯ │Axe  │ │
│ │ Prestige: 0 │                                          │Lv.3 │ │
│ └─────────────┘                                          └─────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Inventory Panel UI (opened with 'I' key)

```
                    ╭─────────────────────────────────────╮
                    │ 📦 Inventory    [▓▓▓▓░░░] 756/1000  ×│
                    ├─────────────────────────────────────┤
                    │ 🔍 Search... [Name▼] [💰 Sell All]    │
                    ├─────────────────────────────────────┤
                    │ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ │
                    │ │🪵 │ │🪨 │ │⚫│ │🟤│ │⚪│ │🟡│ │
                    │ │Wood│ │Stone│ │Iron│ │Copper│ │Tin│ │Gold│ │
                    │ │45.0│ │23.0│ │12.0│ │18.5│ │6.0│ │2.0│ │
                    │ │[U][S]│ │[U][S]│ │[U][S]│ │[U][S]│ │[U][S]│ │[U][S]│ │
                    │ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ │
                    │                                     │
                    │ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐      │
                    │ │💎 │ │🔴│ │🔵│ │✨ │ │🔸│      │
                    │ │Mithril│ │Ruby│ │Sapphire│ │Essence│ │Crystal│      │
                    │ │1.0│ │3.0│ │2.0│ │5.0│ │10.0│      │
                    │ │[U][S]│ │[U][S]│ │[U][S]│ │[U][S]│ │[U][S]│      │
                    │ └───┘ └───┘ └───┘ └───┘ └───┘      │
                    ╰─────────────────────────────────────╯
```

## Resource Rarity Color System

- **Common (Gray)**: Wood, Stone, Fiber
- **Uncommon (Green)**: Copper Ore, Tin Ore, Coal  
- **Rare (Blue)**: Iron Ore, Silver Ore
- **Epic (Purple)**: Gold Ore, Quartz
- **Legendary (Orange)**: Mithril Ore, Adamantite Ore
- **Magical (Pink)**: Essence Dust, Crystal Shard
- **Mythic (Red/Gold)**: Dragon Scale, Phoenix Feather

## Interactive Elements

### Search and Sort
- Search bar filters resources by name
- Sort dropdown: Name, Amount, Category
- Real-time filtering and sorting

### Resource Actions
- **[U] Use Button**: Consume 1 unit of resource
- **[S] Sell Button**: Sell all of that resource type
- **Sell All Button**: Sell entire inventory

### Capacity Management
- Visual capacity bar showing current usage
- Color changes: Green → Yellow → Orange → Red as it fills
- Automatic overflow prevention when gathering

### Keyboard Controls
- **'I' Key**: Toggle inventory open/close
- **ESC Key**: Close inventory panel
- **Mouse Hover**: Show detailed tooltips
- **Click**: Select resource for details

## Visual Design Features

### Modern Glass Morphism
- Semi-transparent backgrounds with blur effects
- Subtle gradients and smooth animations
- Hover effects with color transitions
- Rounded corners and soft shadows

### Responsive Grid Layout
- Automatically adjusts to different screen sizes
- Maintains aspect ratios for resource icons
- Smooth transitions when filtering/sorting

### Accessibility
- High contrast text and backgrounds
- Clear visual hierarchy with typography
- Keyboard navigation support
- Tooltip information for all interactive elements