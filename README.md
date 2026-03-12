# MCStorm v1.0.2.8

A 3D world viewer for Classic Minecraft `.lvl` files.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- OpenGL 3.3+ compatible GPU

## Quick Start

```
cd MCStormViewer
dotnet run
```

To preload all worlds into memory at startup for faster switching:

```
dotnet run -- --preload
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
| PgDn / N | Load next world |
| Esc | Release mouse / Exit |

### Hot-Reload

If you modify or replace a `.lvl` file while it is loaded, the viewer will automatically reload it within about one second.

## Contributors

Players who built worlds on the MCStorm server:

30jon, 499285006, 617lobos, ahardy14, areublem, biel_mcg7, bobeevee, Bob_the_Builderr, CallumBoy265, camac11, chris_penn78, codmod123, cole3000, coulombevin, creepersarehere, cripto, danieldv77, Den8, denny1124, dillon_bo21c, dji152, doomphx, Doomsun50, Evil_30jon, hampaboii, hedgie98, Hordmann, ibelooney, II_LOST_ODST, imanazi, Ininjaninja, Invincible1000, itiscoming78, jason274, jhmoreira, jjj3499, jugiss69, kalon5, KansasJayhawkz25, killer2591, kingedfoolface, klopolo, kristjanaus1, lalo562, lizard725, luckycharms01, mabp, Maister77, maksim106, marattremolo, Markdood88, mathys31, mryeans, nederlandisbest, noah695, noname52, PackedInsanity, pancakelicker, parktatkrap, patrickjin6, pepe822, poooopy232, rezin123, rocsmasher, rodo_le_sodo, roflwaffle117, rridge, sandroarminana, santorini, santy123456789, sasasa1, Shadowmech415, shane0161, shotgem300, sididi, simpergirl, Sk1llsT3R, Skyline_7i, spoonoz, storryashmore1, sugarmelons, themsvej, Toby951, touchofsoul, tristanqq, txboy45, unseensniper, valdeham06, VIP_Skiill, xboxgamer969, \_sebe\_
