# LEGO Sorter App - Unity & Computer Vision Interface

Welcome to the repository for the LEGO Sorter App, a Unity-based interface designed to power an automated physical LEGO sorting machine.

This app acts as the primary control center and dashboard for the sorter, bridging local computer vision systems, inventory management, and ESP32 microcontroller communication into a seamless user experience.

Features
Real-Time Inventory & Search System: Complete database navigation and filtering for cached LEGO parts, powered by automated visual previews and image fetching.

Optimized Asynchronous Data Pipeline: Built-in batch downloading and caching mechanics to handle large brick catalogs without UI freezing or performance drops.

ESP32 & Hardware Integration: Communicates directly with machine microcontrollers and Python edge-processing scripts to orchestrate physical sorting routines.

"Pak een Blokje" (Pick a Brick): Target specific parts in your inventory and trigger the hardware to fetch or route the exact bin location.

Built With
Engine: Unity

Language: C#

Hardware Interfacing: Python & ESP32

Data & Media: Async image download manager, local JSON / database storage


installation

clone github repository

Open Unity Hub.

Click Add -> Add project from disk and select the cloned repository folder.

Launch the project using the appropriate Unity Editor version.
