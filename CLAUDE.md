# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This project is a technical demonstration of a MOBA (Multiplayer Online Battle Arena) combat core, built in Unity (2022.3 LTS). Its primary focus is on the underlying deterministic engine, not on being a complete game. Key technologies include a custom lockstep framework, a fixed-point math library, and a custom physics engine, all written in C#.

## Architecture

The codebase follows a strict three-layer architecture to ensure complete separation between game logic and presentation.

### 1. Core Layer (`Assets/Scripts/Core`)
- **Purpose**: Provides the foundational, deterministic framework for the game.
- **Key Characteristic**: **Has no dependency on `UnityEngine`**. All code in this layer is pure C#.
- **Sub-modules**:
    - **`Math`**: Contains the `Fixed64` fixed-point math library, which is crucial for deterministic calculations across different platforms.
    - **`Lockstep`**: The heart of the synchronization model. `LockstepManager.cs` drives the simulation at a fixed tick rate.
    - **`Physics`**: A custom, deterministic 2D collision system (`CollisionDetector.cs`, `SpatialHash.cs`) that replaces Unity's built-in physics.

### 2. Gameplay Layer (`Assets/Scripts/Gameplay`)
- **Purpose**: Implements all game-specific rules and logic.
- **Key Characteristic**: **Also has no dependency on `UnityEngine`**. It builds on the Core layer.
- **Sub-modules**:
    - **`Entity`**: Defines logical game objects (`BaseEntity.cs`, `HeroEntity.cs`). These are pure data classes.
    - **`Skill`**: A data-driven skill system.
        - `SkillData.cs` (`ScriptableObject`) defines skill properties.
        - `ISkillLogic.cs` provides a strategy pattern for different skill effects.
        - `SkillStateMachine.cs` manages the lifecycle of a skill (cast, execution, cooldown).
    - **`Combat`**: Handles damage calculation and combat events.

### 3. Presentation Layer (`Assets/Scripts/Presentation`)
- **Purpose**: Renders the state of the Gameplay layer and handles user input.
- **Key Characteristic**: This is the **only layer allowed to use `UnityEngine`**.
- **Key Classes**:
    - **`EntityView.cs`**: A `MonoBehaviour` that visually represents a corresponding `BaseEntity`. It uses interpolation (`Lerp`) to create smooth movement between logic frames.
    - **`InputCollector.cs`**: Captures player input and sends it to the `LockstepManager`.

## Common Development Tasks

### Building the Project
This is a standard Unity project. To build, use the Unity Editor's main menu: `File > Build Settings...`.

### Running Tests
The project uses a custom test runner within the Unity Editor.
1.  Open the `UnitTest.unity` scene located in `Assets/_Project/00_Scenes/`.
2.  In the Hierarchy window, select the `_TEST_RUNNER_` GameObject.
3.  In the Inspector window, you will see the `TestRunner` script component.
4.  You can run all tests by right-clicking the component's header and selecting **"运行所有测试"** (Run All Tests) from the context menu.
5.  Alternatively, you can run specific test suites using the other options in the same context menu (e.g., "运行 Day 1 测试", "运行 Fixed64 测试").
6.  Test results are logged to the Unity Console.