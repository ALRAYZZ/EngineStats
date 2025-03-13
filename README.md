# Engine Stats App

A lightweight, topmost overlay to monitor CPU, GPU, and RAM usage in real-time—perfect for gaming or keeping an eye on system resources.

## Known Issues
- Full Screen mode on apps is not working properly by DirectX/OpenGL taking full control of screen. 
- Try with borderless or windowed modes.

## Features
- Shows CPU (averaged), GPU, and RAM usage (updated every second).
- Draggable—move it anywhere, saves position between runs.
- Tray icon control: "Show/Hide" to toggle visibility, "Exit" to close.
- Minimal design—stays out of the taskbar, runs silently in the background.

## Requirements
- Windows 10/11 (tested on .NET 8.0).
- Admin rights (run as administrator for full GPU/RAM data).

## How to Use
1. Download the latest release
2. Extract the `.zip` 
3. Right-click `EngineStatsClient.exe` > "Run as administrator".
4. Drag the overlay to your preferred spot.
5. Right-click the tray icon (your custom icon) to "Hide" or "Exit".

## Notes
- GPU shows "N/A" if not detected (e.g., no driver support).
- Must run as admin for full functionality—UAC will prompt on launch.

## Building from Source
- Requires: Visual Studio 2022, .NET 8.0 SDK, `LibreHardwareMonitorLib` (NuGet).
- Clone this repo, open `.sln`, build in Release mode.

## Credits
- Built with [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).
- Icon: [source if applicable, e.g., "Flaticon"].

## License
MIT License (or your choice—see LICENSE file).
