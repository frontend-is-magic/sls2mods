# Random Ancient Target Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `Random` option to the in-game Ancient target menu.

**Architecture:** Extend the existing `AncientTarget` enum with `Random`, resolve that value inside `VakuuInjectionRule` at replacement time, and keep default behavior as `Vakuu`. Tests cover that the menu preserves `Random` in runtime config and the rule resolves it to one of the concrete Ancient options.

**Tech Stack:** C# 12, .NET 9, xUnit, BaseLib config UI, STS2 mod DLL packaging.

---

### Task 1: Add Red Tests

- [ ] Add tests for `AncientTarget.Random` conversion and concrete random target resolution.
- [ ] Run `dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj` and confirm failure.

### Task 2: Implement Random Target

- [ ] Add `Random` to `AncientTarget`.
- [ ] Add a rule helper that resolves `Random` to one concrete Ancient using the rule RNG.
- [ ] Use the resolved target for event creation and reroll seed material.

### Task 3: Verify And Publish

- [ ] Run tests and mod build.
- [ ] Run the install script.
- [ ] Commit and push.
