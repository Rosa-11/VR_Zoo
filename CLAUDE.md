# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Unity VR application** (VR Zoo) targeting **PICO VR headsets**. The project implements a slingshot/bow mechanic with physics-based trajectory prediction. It uses the Universal Render Pipeline (URP) and supports multiple XR loaders (OpenXR, PICO SDK).

## Build & Development

- **Open the project in Unity Editor** (2022.3 LTS recommended)
- **Build**: Use File > Build Settings (Ctrl+Shift+B)
- **No standalone tests/lint commands** — Unity is editor-driven
- **Development testing**: The `TrajectoryDriver` component provides keyboard controls (Q = aim, E = fire) for testing trajectory systems without VR hardware

## Architecture

### Core Systems (`Assets/Scripts/Core/`)

**Pool System** (`Core/Pool/`)
- `PoolManager` — Singleton manager using `UnityEngine.Pool.ObjectPool<T>`. Access via `PoolManager.I.Get(key)` / `PoolManager.I.Return(obj)`
- `PoolableObject` — Base component for poolable objects. Override `OnSpawnFromPool()` / `OnReturnToPool()` for custom logic
- `IPoolable` — Interface for poolable objects
- Prefabs must be registered in `PoolManager` inspector or via `RegisterPool()`

**Trajectory System** (`Core/Trajectory/`)
- `TrajectoryPredictor` — Physics simulation (gravity + wind). Pure calculation, no rendering. Call `Predict(startPos, velocity)` for raw results
- `TrajectoryRenderer` — LineRenderer visualization driven by `TrajectoryPredictor`. Handles dotted-line flow animation and force-based color gradient (green→yellow→red)
- `TrajectoryResult` — Data object holding path points, landing point, and normal
- **Separation of concerns**: Predictor handles physics; Renderer handles visuals. They communicate via `TrajectoryResult`

**Utils** (`Core/Utils/`)
- `Singleton<T>` — Generic MonoBehaviour singleton base. Access via `ClassName.I`. Subclasses override `OnAwake()` for init (not Awake)

### XR Configuration (`Assets/XR/`)

- **Loaders**: `PXR_PTLoader` (PICO specific), `OpenXRLoader` — switchable per build target
- **Settings**: PICO and OpenXR configuration assets
- Uses Unity XR Interaction Toolkit 2.5.4 and XR Hands 1.3.0

### Key Dependencies (from Packages/)

- `com.unity.xr.interaction.toolkit` — XR interaction framework
- `com.unity.xr.hands` — Hand tracking
- `com.unity.render-pipelines.universal` — URP rendering
- `PICO Unity Integration SDK` — PICO VR hardware support
- `Unity Live Preview Plugin` — Desktop VR preview

### Namespace Conventions

- Custom code uses `Core.*` namespace (e.g., `Core.Pool`, `Core.Trajectory`)
- Test/driver code uses `Testers.*` namespace

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # Framework systems (Pool, Trajectory, Utils)
│   └── Testers/        # Development testing components (TrajectoryDriver)
├── XR/                 # XR loader and settings assets
├── Scenes/            # Unity scenes (LanTest.unity for slingshot testing)
├── Settings/           # URP and project settings
└── Resources/          # Runtime-loaded assets
Packages/
├── PICO Unity Integration SDK-*  # PICO VR SDK (not auto-updated)
└── manifest.json       # Unity Package Manager dependencies
```

## Code Style

- Chinese comments throughout (project is Chinese-localized)
- XML doc comments on public APIs
- SerializeField private fields with `[Header]` grouping and `[Tooltip]`
