# MCStorm v1.0.2.8

Classic Minecraft server with a built-in 3D world viewer.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- OpenGL 3.3+ compatible GPU

## Quick Start

### Server

```
MCStorm.exe
```

### World Viewer

```
cd MCStormViewer
dotnet run
```

Select a world from the list. The viewer loads `.lvl` files from the `levels/` directory.

### Controls

| Key | Action |
|---|---|
| W/A/S/D | Move |
| Mouse | Look |
| Space | Up |
| Shift | Down |
| Scroll | Adjust speed |
| F | Toggle fog |
| Tab | Back to world list |
| Esc | Release mouse / Exit |
