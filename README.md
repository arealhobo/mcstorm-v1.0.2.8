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

The viewer launches directly into a 3D window with an ImGui world browser overlay. Select any `.lvl` world from the browser panel to load and explore it.

World files are bundled in `MCStormViewer/levels/`. You can also drop additional `.lvl` files into that directory — the viewer will detect new and changed files automatically via hot-reload.

<img width="1281" height="747" alt="image" src="https://github.com/user-attachments/assets/6dc195a4-9eff-4ba0-9d1d-56cc972f89e1" />
<img width="1284" height="751" alt="image" src="https://github.com/user-attachments/assets/5e852b8d-9b79-452f-84ec-549146309dd5" />




### Controls

| Key | Action |
|---|---|
| W/A/S/D | Move |
| Mouse | Look |
| Space | Up |
| Shift | Down |
| Scroll | Adjust speed |
| F | Toggle fog |
| Tab | Toggle world browser |
| Esc | Release mouse / Exit |

### Hot-Reload

If you modify or replace a `.lvl` file while it is loaded, the viewer will automatically reload it within about one second.
