# Reverie

**Multichannel audio routing for Windows** — map 2.0 / 5.1 / 7.1 / 7.2.1 and more across multiple physical playback devices with a visual room layout.

Reverie takes a single audio source (virtual cable or system loopback), expands or maps channels, and routes each logical speaker to a real output device (HDMI, Bluetooth, analog, USB) with per-channel gain, delay, and orientation controls.

## Features

- **Visual room layout** — click to select speakers, optional free-move drag
- **11 built-in layouts** — 2.0, 2.1, 3.1, 4.0, 5.1, 5.1.2, 5.1.4, 7.1, 7.1.2, 7.1.4, 7.2.1
- **Role-based device assignment** — Front / Surround / Sub / Height, with auto channel mapping
- **Per-channel control** — gain (dB), delay (ms), mute, L/R swap per role
- **Stereo upmix** — matrix upmix to the selected layout; multichannel sources bypass upmix automatically
- **WASAPI engine** — capture or loopback source → multi-device render, live parameter updates
- **Presets** — save / load JSON layouts and routes

## Signal flow

```
App / player ──► virtual cable (e.g. VB-CABLE) ──► Reverie
                                                      │
                         matrix upmix (stereo only)   │  or direct map (N-ch)
                                                      ▼
                              per-channel: gain / delay / mute / L-R swap
                                                      ▼
                    ┌─────────────────────────────────┴─────────────────┐
                    ▼                                                   ▼
              front device                                      surround / sub / height
```

## Requirements

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build)
- Optional: [VB-CABLE](https://vb-audio.com/Cable/) (or similar) for system-wide routing

## Build & run

```powershell
dotnet build Reverie.sln -c Release
.\src\Reverie.Gui\bin\Release\net8.0-windows\Reverie.exe
```

## Quick start

1. Set system default output to **CABLE Input** (apps play into the virtual cable).
2. Open Reverie, pick the capture source (**CABLE Output**).
3. Choose a layout (e.g. 5.1), assign **front** and **surround** devices.
4. Click **Start**.

Multichannel (≥ 3 ch) sources are mapped directly and never upmixed. Stereo sources use the upmix matrix when enabled (Advanced).

## Presets

JSON files (see `presets/*.json`) store speaker positions and route parameters:

```json
{
  "name": "5.1",
  "layoutTag": "5.1",
  "speakers": [{ "id": "FL", "x": 0.3, "y": 0.18, "heightTier": 0 }],
  "routes":  [{ "channel": "FL", "deviceChannel": 1, "gainDb": 0, "delayMs": 0, "polarity": true }]
}
```

Device IDs are machine-specific; re-select devices after loading on a new PC.

## Project layout

```
src/Reverie.Core/   Models · Devices · Dsp · Layouts · Engine · IO
src/Reverie.Gui/    WPF UI (room canvas, inspector, upmix panel)
presets/            Example layout JSON files
```

## Notes & limitations

- Multidevice clock drift is compensated manually via per-channel delay (Bluetooth may need tens of ms).
- Dolby / Atmos are trademarks of Dolby Laboratories. This project does not implement certified Atmos rendering; object-based workflows can use open standards (ADM, Ambisonics).
- Not affiliated with VB-AUDIO or any device vendor.

## License

[MIT](LICENSE)
