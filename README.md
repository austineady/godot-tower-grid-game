# Grid Tower Game

![Godot Engine Screenshot 2025 05 24 - 10 21 51 20](https://github.com/user-attachments/assets/d576c346-4b0e-44ee-918b-79951d028b75)


<a href="https://godotengine.org/" target="_blank"><img src="https://img.shields.io/badge/Made_With-Godot-blue?logo=godotengine&logoColor=white&style=for-the-badge"></a>
<a href="https://dotnet.microsoft.com/en-us/download" target="_blank"><img src="https://img.shields.io/badge/Written_In-C%23-rgb(151 128 229)?logo=dotnet&logoColor=white&style=for-the-badge"></a>
<a href="https://firebelley.com" target="_blank"><img src="https://img.shields.io/badge/Special_Thanks-FireBelly-rgb(103 30 165)?style=for-the-badge"></a>

## Project Details

### Game Assets
- [TinySwords by Pixel Frog](https://pixelfrog-assets.itch.io/tiny-swords)
- [Font Source](https://nimblebeastscollective.itch.io/nb-pixel-font-bundle)

### Purpose
This is an intermediate-level self-development & self-learning project not intended for production use. This game has not been published and is only playable locally.

### Learning Topics
- Godot sprites, tiles, tilemaps, tilesets, animated tiles, auto-tiling setup that allows easy level creation, and attaching custom data to individual tiles
- Complications of Y-sorting, sprite dimensions/positions/offsets, tile order, and sprite sizing to make the perspective look appropriate
- Using custom resources to centralize logic across many core game elements. Level management, building management, etc.
- Alternative ways to get node references across a larger project: exported fields, unique name references, `GetTree()` depth-first search implementations, and concepts like Node grouping.
- Event scripting using `Signals`, deferred `Callable`s, and orchestrating asynchronous operations across multiple node states (waiting for another node's `QueueFree()` completion, for example)
- Creating a flexible user interface that is programmatically hydrated
- Godot theming capabilities, creating global themes and applying theme variations
- Just how useful System.Linq is as a standard lib and how these methods can really simplify grid operations, such as merging and/or excluding many overlapping grid matrices
- The flexible utility of static method extensions within Godot's framework

## Game Details

### Objective
Place a tower so that each level's gold mine is within the tower's radius.

### Features
- Extremely simple puzzle game containing 3 levels. The 3rd level has a WIP genemy camp feature.
- Y-sorting problem solving
- Grid highlighting
- Overlapping grid management with HashSet merging and exclusion
- Building placement and destruction
- Centralized building resources
- Level resources and level-change management
- User interface with programmatic hydration
- Main menu with simple level selection
- Level creation scenes to help with elevation
- Animations for building placement and destruction





