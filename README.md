# LEGO Sorter v1

A Unity application that controls and simulates a custom LEGO brick sorting machine. It combines a live machine dashboard, a LEGO part database with search and inventory management, and a 3D simulation of the sorter (C-channels, carousel, cameras).

Built with Unity 6000.0.71f1 (Unity 6) and the new Input System. The UI scales with the screen size, so it also works when built for the browser (WebGL).

## Scenes

| Scene | Purpose |
| --- | --- |
| `Assets/Scenes/SampleScene.unity` | Empty starter scene (first in the build order, no UI). |
| `Assets/sorter v2/scenes/home screen.unity` | Main menu with buttons to open Run, Simulation and the inventory. |
| `Assets/sorter v2/scenes/run.unity` | The main app: live telemetry, analytics, management ("beheer"), the "pak blokjes" (take bricks) list and the inventory (Kant A / Kant B bins). |
| `Assets/sorter v2/scenes/simulation.unity` | 3D simulation of the sorting machine: three C-channels, a rotating carousel with bins, a brick spawner and switchable/following cameras. |
| `Assets/sorter v2/scenes/inventory.unity` | Legacy standalone inventory scene (not included in the build; the inventory now lives inside the Run scene). |

Build order: SampleScene, home screen, simulation, run.

## Features

### Machine dashboard (Run scene)

- Live telemetry panel that parses `machine_update` JSON from a TCP socket and shows:
  - current brick name, colour and target bin
  - bricks per hour and total sorted counter
  - hub 1 / hub 2 connection status with pulsing glow indicators (`HubStatusIndicator`)
- The detected colour is also reflected in the colour of the text.
- Analytics and management ("beheer") panels inside the Home tab, each with an automatic close (X) button that returns to Home.
- Navigation buttons between Home, Run, Simulation and back (works from every scene).

### Speed control

- One slider that controls four machine settings, switchable via the C1 / C2 / C3 / Time cards:
  - C1: input speed
  - C2: sorting speed
  - C3: vibration belt speed
  - Time: brick drop time
- Values are saved to PlayerPrefs, shown on the cards, and sent to the machine over TCP as a `control_update` JSON message.
- The same four settings are used by the simulation sliders.

### LEGO part database and search

- `LegoDataAsset` (ScriptableObject) holds the part database: part number, name, category, material, image URL/path, flags (modified, sticker, minifig) and per-colour stock.
- Smart search scoring (`ZoekHelper`): exact part number, prefix, word-prefix and substring matches are ranked; multiple search words must all match (AND). Exact matches get a star, prefixes a triangle, substring a dot.
- Search history (`ZoekGeschiedenis`): recent and popular search terms are stored in PlayerPrefs and shown as clickable chips under the search bar.
- Grid size filter: search can be narrowed by brick dimensions (e.g. 2x4), including special handling for bars/hinges/sticks.
- Optional filters on the "pak blokjes" page: Modified, Stickers and Minifig toggles.

### "Pak blokjes" (Take bricks) page

- Lazy-loaded result list: the first batch of rows is shown immediately, more rows load when scrolling to the bottom or via the "Toon meer" button.
- Each row shows the part image, part number and name, stock per colour, a colour picker and a "pak" (take) button.
- Results are sorted so parts already in the inventory come first, then best search match, then regular parts before modified/sticker/minifig.
- The confirmation panel (`BevestigPakPanel`) opens after pressing "pak": shows the part, colour swatch, available stock and a quantity stepper, then confirms and subtracts the amount from stock.

### Inventory (Kant A / Kant B)

- Two sides (Kant A and Kant B), each with a grid of 15 columns of bins sized to the part dimensions.
- Bins are clickable: select a search result and click a bin to assign that part to it. Assigned bins are saved in PlayerPrefs and restored on start.
- Grid cells zoom on hover (`VergrotenZoom`).
- Search results can be dropped into bins; the grid remembers which parts are already placed ("bezet").
- An images toggle controls whether part images are loaded (off by default for performance).
- `RectMask2D` clipping keeps all content (bins, colours, images, results) inside its own panel.

### Simulation (Sim scene)

- Start/stop button that starts or stops the spawner and all channel rotations.
- Sliders for C1, C2, C3 speeds and the brick drop time (linked to the same settings as the Run scene).
- A spawner drops LEGO bricks at a fixed interval; each C-channel rotates according to its channel speed (`ronddraaien`).
- The carousel rotates in 90-degree steps and its bins catch bricks (`CarouselBakje`); caught bricks are parented to the carousel and rotated through up to three positions.
- Detection flow: trigger zones in the carousel (`script voor collider carousel`) take snapshots with the left/right cameras and run `brickverwerken.py` (Python) to identify the part. After three positions the brick is removed.
- Throughput estimate: bricks per hour is calculated every 30 seconds and the best result is kept in PlayerPrefs.
- Camera controls:
  - Orbit and zoom with the right mouse button and mouse wheel (`simulation camera script`).
  - Switch between the three cameras (Main Camera, "blokje boven", "blokje onder") with the C key or the "camera" button (`SimCameraWisselaar`).
  - Follow mode: press F or the "volg" button and the active camera automatically follows the newest falling brick until it lands.
- Manual brick scanner: press Space to take left/right snapshots and run the Python detection script.

## AI-assisted development

This project was partly built with AI assistance, mainly in the final stages:

- The UI was optimized with AI help near the end of development (consistent scaling across scenes, filling panels properly, styling that matches the existing look).
- Some of the more complex parts of the search system were AI-assisted, such as the smart search scoring (`ZoekHelper`) and the search history chips.
- AI was mostly used to merge and optimize systems that already existed, for example combining the inventory into the Run scene, reusing the same search logic across the "pak blokjes" page and the inventory, linking the existing speed sliders, and unifying camera controls in the simulation.

## Networking

The app communicates over TCP on `127.0.0.1:5005`:

- Outgoing: `control_update` JSON with the changed setting and its value (sent when a slider changes).
- Incoming: `machine_update` JSON with `huidig_blokje`, `huidige_kleur`, `naar_bakje`, `blokjes_per_uur`, `totaal_gesorteerd` and hub statuses.

If the connection is lost, the app logs a warning and keeps trying to reconnect.

## Persistence

Settings, stock, inventory bin assignments, search history and the best simulation result are stored with PlayerPrefs:
`InvoerSnelheid`, `SorteerSnelheid`, `TrilbandSnelheid`, `ValTijd`, `ZoekGeschiedenis`, `GridSlot_<w>_<h>`, `bestetijd`, and the inventory data used by `InventarisOpslag`.

## Python integration

`Assets/sorter v2/scripts/brickverwerken.py` is invoked from Unity to identify LEGO parts from camera snapshots. It receives the paths of the captured images as command-line arguments and prints the detected part number, which Unity parses from the output.

## Folder structure (main scripts)

```
Assets/
  appmanager.cs                  Main app logic: panels, slider, ESP32 TCP, scene navigation
  RunPageManager / TelemetryPanelManager / HubStatusIndicator   Live machine telemetry
  PakBlokjesPanelManager.cs      Take-bricks list with lazy loading, filters, search chips
  BlokjeRijPrefab.cs             Row prefab logic (list rows + inventory rows)
  BevestigPakPanel.cs            Confirmation dialog for taking bricks
  InventarisManager.cs           Inventory: Kant A/B grids, bins, search-to-bin, clipping
  InventarisData.cs              Inventory bin data, config, sorting and storage
  LegoDataAsset.cs               Part database data classes
  LegoZoekHelper.cs              Smart search scoring
  ZoekGeschiedenis.cs            Search history + chip UI helper
  search.cs / BrickManager.cs    Legacy search and grid-size filter
  GridSlot.cs / LegoGridUi.cs    Legacy grid slots and 8x8 grid filter UI
  scenemanager.cs                Scene loading helper
  UImanagersim.cs                Simulation UI: sliders and start/stop
  spawner.cs                     Brick spawner
  ronddraaien.cs                 C-channel rotation per channel speed
  carouselmovingsecond.cs        Carousel 90-degree step rotation + bin tilt
  CarouselBakje.cs               Carousel bin trigger (catches bricks)
  script voor collider carousel.cs  Detection triggers, snapshots and BlokjeTracker
  sorter v2/scripts/SimCameraWisselaar.cs       Camera switching + follow mode
  sorter v2/scripts/simulation camera script.cs Orbit/zoom camera control
  sorter v2/scripts/brick scanner.cs            Manual snapshot scanner (Space)
  sorter v2/scripts/brickverwerken.py           Python brick detection
```

## Requirements

- Unity 6000.0.71f1 (Unity 6)
- Python installed and on PATH for brick detection (`brickverwerken.py`)
- A machine/ESP32 (or local test server) listening on TCP 127.0.0.1:5005 for live data
