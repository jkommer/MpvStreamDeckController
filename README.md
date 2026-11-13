# MpvStreamDeckController

A Stream Deck plugin to control [mpv](https://mpv.io) on Windows. It talks to mpv through mpv's JSON IPC interface, so
keys and dials act on the running player directly and dials show live state such as the playback position, volume and speed.

## Actions

| Action | Type | What it does |
|---|---|---|
| Play / Pause | Key | Toggles pause |
| Cycle Audio / Cycle Subtitles | Key | Switches to the next audio or subtitle track |
| Next / Previous Chapter | Key | Jumps between chapters, continuing into the next or previous file at the ends (optional) |
| Next / Previous Playlist Entry | Key | Moves through the playlist |
| Toggle Acompressor | Key | Toggles the audio compressor (needs `acompressor.lua`, see below); the key shows whether it's on |
| Toggle On Top | Key | Toggles whether the mpv window stays on top of other windows; the key shows whether it's on |
| Toggle Fullscreen | Key | Toggles fullscreen; the key shows whether it's on |
| Toggle Subtitle Visibility | Key | Shows or hides subtitles without changing the selected track; the key shows whether they're visible |
| Custom Command | Key | Sends any mpv command, written as in `input.conf` |
| Seek | Dial | Rotate to seek, press or tap to play / pause; shows the position |
| Volume | Dial | Rotate to change the volume, press or tap to mute; shows the volume |
| Playback Speed | Dial | Rotate to change the playback speed, press or tap to reset to 1.00x; shows the speed |

## Requirements

- Windows 10 or later
- Stream Deck 6.4 or later
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64)
- mpv

## Getting started

1. Download `com.jkommer.mpv.streamDeckPlugin` from the [latest release](https://github.com/jkommer/MpvStreamDeckController/releases/latest) and double-click it to install.
2. Enable mpv's IPC server by adding this line to your `mpv.conf` (usually `%APPDATA%\mpv\mpv.conf`):

   ```
   input-ipc-server=mpvsocket
   ```

3. Start mpv and drag the actions onto your Stream Deck.

The plugin connects automatically when mpv starts and reconnects if mpv is restarted.

## Settings

Most actions have an **OSD** setting that chooses whether mpv shows a message or bar on screen when the action is
used. By default nothing is shown.

- **Next / Previous Chapter**: *Playlist* (on by default) makes the key continue into the next file after the last
  chapter, and into the previous file before the first; files without chapters always move through the playlist.
  Untick it to only move between chapters.
- **Seek**: the seconds per dial tick and the seek type (exact, keyframes, or mpv's default).
- **Volume**: the percentage per dial tick.
- **Playback Speed**: the speed change per dial tick (0.1 by default).
- **Custom Command**: the command, e.g. `cycle pause`, `seek 30` or `add volume -5; show-text "${volume}"`. Several
  commands can be separated with `;`. Tick *Expand ${property}* to have mpv fill in property values.

For plugin settings use a full stop for decimals (`0.5`, not `0,5`).

## FAQ and notes

**Nothing happens when I press a key, or the key shows a warning triangle.**
mpv isn't running, its IPC server isn't enabled (see *Getting started*), or mpv rejected the command - for example
seeking when nothing is loaded. Rejected commands are also written to the plugin's log.

**I have several mpv windows open; only one responds.**
Only one mpv instance can own the `mpvsocket` pipe, normally the first one started. The others can't be controlled
by the plugin. Closing all mpv instances and opening a single one should ensure that it owns and opens the `mpvsocket` 
pipe

**Toggle Acompressor does nothing.**
It relies on mpv's [`acompressor.lua`](https://github.com/mpv-player/mpv/blob/master/TOOLS/lua/acompressor.lua)
script; copy it into your mpv `scripts` folder. The script also provides key bindings to adjust the compressor.

**Seeking jumps further than the configured interval.**
mpv seeks to keyframes by default for relative seeks. Set the seek type to *Exact*, or set `hr-seek=yes` in
`mpv.conf`.

**My custom command works in `input.conf` but shows nothing on screen here.**
Unlike `input.conf`, the plugin doesn't show an OSD unless one is selected in the action's settings, and
`${property}` is only expanded when *Expand* is ticked.

## Building

Requires the .NET 10 SDK (the version in `global.json` or a later 10.0 one) and [Node.js](https://nodejs.org). The build tools -
[GitVersion](https://gitversion.net) and the [Elgato CLI](https://docs.elgato.com/streamdeck/cli/intro) - are pinned
in the repository (`.config/dotnet-tools.json` and `package.json`) and installed by `dotnet tool restore` and `npm ci`.

`build-and-deploy.ps1` builds and tests the plugin, validates and packs it, and reinstalls it into Stream Deck.

### Releasing

Pushing a version tag builds that version and creates a draft GitHub release with the plugin attached:

```
git tag v1.2.0
git push origin v1.2.0
```

Review the draft on the repository's Releases page and publish it from there. A tag with a pre-release part, e.g.
`v1.2.0-beta.1`, is marked as a pre-release.

## License

[MIT](LICENSE)
