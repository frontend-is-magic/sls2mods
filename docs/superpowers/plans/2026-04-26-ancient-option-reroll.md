# Ancient Option Reroll Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure every injected Vakuu room generates a fresh Ancient 3-choice option set, while keeping the reroll mechanism reusable for future Ancient room injection mods.

**Architecture:** Add a reusable `utils/AncientOptions` layer that can mark a specific `AncientEventModel` instance for deterministic RNG reseeding before its initial options are generated. The Vakuu injection rule creates an `EventRoom` with an `OnStart` callback that marks each mutable event instance using the room injection key as seed material.

**Tech Stack:** C# 12, .NET 9, STS2 `v0.103.2`, Harmony, Godot .NET mod manifest with DLL-only packaging.

---

### Task 1: Add Reusable Ancient Option Reroll Utils

**Files:**
- Create: `utils/AncientOptions/AncientOptionRerollService.cs`
- Create: `utils/AncientOptions/AncientOptionRerollInstaller.cs`

- [ ] **Step 1: Create the service**

Create `AncientOptionRerollService` with a `ConditionalWeakTable<AncientEventModel, RerollRequest>`, so requests attach to mutable event instances without leaking after the event ends.

- [ ] **Step 2: Create the Harmony installer**

Patch `AncientEventModel.GenerateInitialOptionsWrapper()` with a prefix/postfix. Prefix consumes the service request, stores the original `EventModel.Rng`, then writes a new `Rng` instance into the private setter-backed property. Postfix restores the original RNG after vanilla option generation finishes.

- [ ] **Step 3: Build**

Run `dotnet build .\VakuuRoomInjection\MapNodeChanger.csproj`. Expected: 0 errors.

### Task 2: Wire Reroll Requests Into Vakuu Injection

**Files:**
- Modify: `utils/RoomInjection/RoomInjectionContext.cs`
- Modify: `utils/RoomInjection/RoomInjectionService.cs`
- Modify: `VakuuRoomInjection/Features/Vakuu/VakuuInjectionRule.cs`
- Modify: `VakuuRoomInjection/MapNodeChanger.cs`

- [ ] **Step 1: Expose a reusable room injection key**

Move key construction into `RoomInjectionContext.RoomKey`, so feature rules can use the same deterministic key that the cache uses.

- [ ] **Step 2: Register Ancient reroll utils**

Instantiate `AncientOptionRerollService` in `MapNodeChanger`, install `AncientOptionRerollInstaller`, and pass the service into `VakuuInjectionRule`.

- [ ] **Step 3: Mark injected Vakuu events**

When `VakuuInjectionRule` creates the `EventRoom`, set `OnStart` to call `AncientOptionRerollService.RequestReroll()` for the mutable `Vakuu` event, using the room key and rule name as seed material.

- [ ] **Step 4: Build**

Run `dotnet build .\VakuuRoomInjection\MapNodeChanger.csproj`. Expected: 0 errors.

### Task 3: Install And Smoke-Ready Package

**Files:**
- Modify if needed: `VakuuRoomInjection/README.md`

- [ ] **Step 1: Run enable script**

Run `powershell -ExecutionPolicy Bypass -File .\enable-vakuu-room-injection.ps1`. Expected: build succeeds and game mod directory contains only `MapNodeChanger.dll` and `MapNodeChanger.json`.

- [ ] **Step 2: Document smoke behavior**

Update README to say injected Ancient options are rerolled per injected room.

- [ ] **Step 3: Commit and push**

Commit with `feat: reroll injected ancient options`, then push the current branch.
