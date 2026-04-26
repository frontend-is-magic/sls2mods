# In-Game Config Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a BaseLib-powered in-game mod menu for selecting the injected Ancient and editing normal/unknown room replacement chances.

**Architecture:** Introduce a BaseLib `SimpleModConfig` class with static properties for the menu, convert it into the existing runtime `VakuuInjectionConfig`, and update room injection to create the selected Ancient event. Keep the JSON config path as a fallback when BaseLib config is unavailable, but prefer the in-game menu after registration.

**Tech Stack:** C# 12, .NET 9, STS2 `v0.103.2`, BaseLib `SimpleModConfig`, Harmony, Godot .NET DLL-only packaging.

---

### Task 1: Add Test Coverage For Config Conversion

**Files:**
- Create: `tests/MapNodeChanger.Tests/MapNodeChanger.Tests.csproj`
- Create: `tests/MapNodeChanger.Tests/VakuuInjectionConfigTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests that expect default menu values to convert to `Vakuu`, `0.066`, and `0.66`, and verify percent values clamp into `0..1`.

- [ ] **Step 2: Run tests and confirm failure**

Run `dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj`. Expected: failure because menu config types do not exist yet.

### Task 2: Implement BaseLib Config Menu And Runtime Mapping

**Files:**
- Modify: `VakuuRoomInjection/MapNodeChanger.csproj`
- Create: `VakuuRoomInjection/Features/Vakuu/AncientTarget.cs`
- Create: `VakuuRoomInjection/Features/Vakuu/VakuuRoomInjectionConfigMenu.cs`
- Modify: `VakuuRoomInjection/Features/Vakuu/VakuuInjectionConfig.cs`
- Modify: `VakuuRoomInjection/MapNodeChanger.cs`

- [ ] **Step 1: Reference BaseLib**

Add a `BaseLib` reference to the mod project and the test project.

- [ ] **Step 2: Add menu enum and config class**

Create an enum containing `Darv`, `Neow`, `Nonupeipe`, `Orobas`, `Pael`, `Tanx`, `Tezcatara`, and `Vakuu`. Create `VakuuRoomInjectionConfigMenu : SimpleModConfig` with static menu properties and a `ToRuntimeConfig()` method.

- [ ] **Step 3: Register menu**

Register the menu with `ModConfigRegistry.Register("MapNodeChanger", new VakuuRoomInjectionConfigMenu())` during mod load, then use its runtime config.

- [ ] **Step 4: Run tests**

Run `dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj`. Expected: pass.

### Task 3: Support Selected Ancient Event Creation

**Files:**
- Modify: `VakuuRoomInjection/Features/Vakuu/VakuuInjectionRule.cs`
- Modify: `VakuuRoomInjection/README.md`
- Modify: `VakuuRoomInjection/MapNodeChangerConfig.json.example`

- [ ] **Step 1: Create selected Ancient**

Replace hard-coded `ModelDb.Event<Vakuu>()` with a switch on `AncientTarget`, returning `ModelDb.AncientEvent<T>()`.

- [ ] **Step 2: Update docs and config example**

Document the game menu and add `ancient_target` to JSON fallback config.

- [ ] **Step 3: Build and install**

Run `dotnet build .\VakuuRoomInjection\MapNodeChanger.csproj` and `powershell -ExecutionPolicy Bypass -File .\enable-vakuu-room-injection.ps1`. Expected: build succeeds and installed mod directory contains DLL and manifest only.

- [ ] **Step 4: Commit and push**

Commit with `feat: add in-game Vakuu config menu`, then push.
