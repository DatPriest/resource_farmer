# AI Assistant Instructions Summary - Resource Farmer

This directory contains specialized instruction files for different AI coding assistants to help with Resource Farmer (S&box game) development.

## Available Instruction Files

### `.github/copilot-instructions.md`

- **For**: GitHub Copilot (VS Code extension)
- **Focus**: Comprehensive S&box patterns, Resource Farmer architecture
- **Best for**: Code completion and suggestions within VS Code

### `.cursorrules`

- **For**: Cursor AI editor
- **Focus**: Concise S&box development rules and anti-patterns
- **Best for**: Code generation and refactoring in Cursor

### `CLAUDE.md`

- **For**: Claude (Anthropic) - all interfaces
- **Focus**: Deep architectural understanding and detailed patterns
- **Best for**: Complex analysis, architecture discussions, detailed explanations

### `.windsurfrules`

- **For**: Windsurf AI assistant
- **Focus**: Quick reference patterns and multi-file workflows
- **Best for**: Project-wide refactoring and component development

### `.clinerules`

- **For**: Cline AI assistant
- **Focus**: Terminal operations, file management, and PowerShell commands
- **Best for**: File operations, project setup, and build tasks

## Key S&box Patterns Covered

All instruction files include guidance on:

- **Component System**: Inheriting from `Component`, using `[Property]` and `[Sync]`
- **Resource Management**: ResourceType enum, ResourceNode, ResourceSpawner
- **Crafting System**: .recipe files, RecipeManager, ToolBase
- **Networking**: Server/client separation with IsProxy checks
- **UI Development**: Razor components with proper lifecycle management

## Usage

These files are automatically detected by their respective AI assistants when working in this directory. No additional setup required - just start coding and the AI will have context about your S&box game architecture.

## Quick Reference

**For code completion**: Use GitHub Copilot in VS Code  
**For architecture questions**: Ask Claude about complex system design  
**For file operations**: Use Cline for PowerShell/terminal tasks  
**For refactoring**: Use Cursor or Windsurf for multi-file changes

All assistants understand Resource Farmer's component-based architecture, resource/crafting systems, and S&box-specific patterns.
