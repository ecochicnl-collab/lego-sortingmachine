
  - Follow mode: press F or the "volg" button and the active camera automatically follows the newest falling brick until it lands.
- Manual brick scanner: press Space to take left/right snapshots and run the Python detection script.

## AI-assisted development

This project was partly built with AI assistance, mainly in the final stages:

- The UI was optimized with AI help near the end of development (consistent scaling across scenes, filling panels properly, styling that matches the existing look).
- Some of the more complex parts of the search system were AI-assisted, such as the smart search scoring (`ZoekHelper`) and the search history chips.
- AI was mostly used to merge and optimize systems that already existed, for example combining the inventory into the Run scene, reusing the same search logic across the "pak blokjes" page and the inventory, linking the existing speed sliders, and unifying camera controls in the simulation.

## Networking

The app communicates over TCP on `127.0.0.1:5005`:
