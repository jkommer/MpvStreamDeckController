# MpvStreamDeckController

A Stream Deck plugin to control [mpv](https://mpv.io) on Windows. It talks to mpv through mpv's JSON IPC interface, so
keys and dials act on the running player directly and dials show live state such as the playback position and volume.

## Actions

| Action | Type | What it does |
|---|---|---|
| Play / Pause | Key | Toggles pause |
| Cycle Audio / Cycle Subtitles | Key | Switches to the next audio or subtitle track |
| Next / Previous Chapter | Key | Jumps between chapters |
| Next / Previous Playlist Entry | Key | Moves through the playlist |
| Toggle Acompressor | Key | Toggles the audio compressor (needs `acompressor.lua`, see below); the key shows whether it's on |
| Custom Command | Key | Sends any mpv command, written as in `input.conf` |
| Seek | Dial | Rotate to seek, press or tap to play / pause; shows the position |
| Volume | Dial | Rotate to change the volume, press or tap to mute; shows the volume |

## Requirements

- Windows 10 or later
- Stream Deck 6.4 or later
- mpv

## Getting started

1. Install the plugin.
2. Enable mpv's IPC server by adding this line to your `mpv.conf` (usually `%APPDATA%\mpv\mpv.conf`):

   ```
   input-ipc-server=mpvsocket
   ```

3. Start mpv and drag the actions onto your Stream Deck.

The plugin connects automatically when mpv starts and reconnects if mpv is restarted.

## Settings

Most actions have an **OSD** setting that chooses whether mpv shows a message or bar on screen when the action is
used. By default nothing is shown.

- **Seek**: the seconds per dial tick and the seek type (exact, keyframes, or mpv's default).
- **Volume**: the percentage per dial tick.
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

Requires the .NET 10 SDK, [GitVersion](https://gitversion.net) (`dotnet tool install --global GitVersion.Tool`) and
the [Elgato CLI](https://docs.elgato.com/streamdeck/cli/intro) (`npm install -g @elgato/cli`).

`build-and-deploy.ps1` builds and tests the plugin, validates and packs it, and reinstalls it into Stream Deck.

## License

[MIT](LICENSE)