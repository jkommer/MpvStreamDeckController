# MpvStreamDeckController

A Stream Deck plugin to control [mpv](https://mpv.io) on Windows. It talks to mpv through mpv's JSON IPC interface, so
keys and dials act on the running player directly and show live state such as the playback position, volume, playback speed and toggle state.

## Actions

| Action | Type | What it does |
|---|---|---|
| Play / Pause | Key | Toggles pause; the key shows whether mpv is playing or paused |
| Stop | Key | Stops playback and clears the playlist; mpv then closes unless it is set to stay open |
| Quit | Key | Closes mpv, optionally remembering the playback position |
| Cycle Audio / Cycle Subtitles | Key | Switches to the next audio or subtitle track |
| Next / Previous Chapter | Key | Jumps between chapters, continuing into the next or previous file at the ends (optional) |
| Next / Previous Playlist Entry | Key | Moves through the playlist |
| Shuffle Playlist | Key | Puts the playlist in a random order; the current file keeps playing |
| Toggle Loop File / Toggle Loop Playlist | Key | Repeats the current file, or the whole playlist, until turned off; the key shows whether it's on |
| A-B Loop | Key | First press sets the start of a loop, the second its end, the third clears it; the key shows which step it's at |
| Toggle Acompressor | Key | Toggles the audio compressor (needs [`acompressor.lua`](https://github.com/mpv-player/mpv/blob/master/TOOLS/lua/acompressor.lua)); the key shows whether it's on |
| Toggle On Top | Key | Toggles whether the mpv window stays on top of other windows; the key shows whether it's on |
| Toggle Fullscreen | Key | Toggles fullscreen; the key shows whether it's on |
| Toggle Subtitle Visibility | Key | Shows or hides subtitles without changing the selected track; the key shows whether they're visible |
| Toggle Deband | Key | Toggles mpv's debanding filter, which smooths visible colour steps in gradients; the key shows whether it's on |
| Toggle Interpolation | Key | Toggles mpv's frame interpolation, which blends frames for smoother motion; the key shows whether it's on |
| Toggle Mute | Key | Mutes or unmutes; the key shows whether it's muted |
| Volume Up / Volume Down | Key | Changes the volume by a configurable step |
| Seek Back / Seek Forward | Key | Seeks backwards or forwards by a configurable number of seconds |
| Custom Command | Key | Sends any mpv command, written as in `input.conf` |
| Volume | Dial | Rotate to change the volume, press or tap to mute; shows the volume |
| Seek | Dial | Rotate to seek, press or tap to play / pause; shows the position |
| Frame Step | Dial | Rotate to step forwards or backwards one frame per tick, press or tap to play / pause; shows the position, or the frame number while paused |
| Playback Speed | Dial | Rotate to change the playback speed, press or tap to reset to 1.00x; shows the speed |
| Brightness | Dial | Rotate to make the picture brighter or darker, press or tap to reset to 0; shows the brightness |
| Subtitle Delay / Audio Delay | Dial | Rotate to make the subtitles or audio come later or earlier, press or tap to reset to 0; shows the delay in milliseconds |
| Custom Dial | Dial | Sends your own mpv commands: one per tick clockwise, one per tick anticlockwise, and one on press or tap |

## Requirements

- Windows 10 or later
- Stream Deck 7.6 or later
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

- **Next / Previous Chapter**: *Playlist* (on by default) makes the key continue into the next file after the last chapter, and into the previous file before the first; files without chapters always move through the playlist. Untick it to only move between chapters.
- **Seek / Seek Back / Seek Forward**: the seconds per dial tick / key press and the seek type (exact, keyframes, or mpv's default).
- **Volume / Volume Up / Volume Down**: the percentage per dial tick / key press.
- **Playback Speed**: the speed change per dial tick.
- **Brightness**: the brightness change per dial tick.
- **Subtitle Delay / Audio Delay**: the milliseconds per dial tick.
- **Custom Command**: the command, e.g. `cycle pause`, `seek 30` or `add volume -5; show-text "${volume}"`. Several commands can be separated with `;`. Tick *Expand ${property}* to have mpv fill in property values.
- **Custom Dial**: a command each for clockwise, anticlockwise and press, written like a Custom Command; an empty one does nothing. The dial shows no value itself, so pick an OSD mode to see the result in mpv.
- **Quit**: whether mpv remembers the playback position, so the file resumes there next time (off by default). This has no effect if `no-resume-playback` (`resume-playback=no`) is set in `mpv.conf`: mpv still saves the position but doesn't restore it.

For plugin settings use a full stop for decimals (`0.5`, not `0,5`).

## FAQ and notes

**Nothing happens when I press a key, or the key shows a warning triangle.**
mpv isn't running, its IPC server isn't enabled (see *Getting started*), or mpv rejected the command - for example seeking when nothing is loaded. Rejected commands are also written to the plugin's log.

**I have several mpv windows open; only one responds.**
Only one mpv instance can own the `mpvsocket` pipe, normally the first one started. The others can't be controlled by the plugin. Closing all mpv instances and opening a single one should ensure that it owns and opens the `mpvsocket` pipe

**Toggle Acompressor does nothing.**
It relies on mpv's [`acompressor.lua`](https://github.com/mpv-player/mpv/blob/master/TOOLS/lua/acompressor.lua) script; copy it into your mpv `scripts` folder. The script also provides key bindings to adjust the compressor.

**Seeking jumps further than the configured interval.**
mpv seeks to keyframes by default for relative seeks. Set the seek type to *Exact*, or set `hr-seek=yes` in `mpv.conf`.

**Toggle Interpolation turns on but the picture looks the same.**
mpv only interpolates when `video-sync` is set to one of the `display-...` modes, e.g. `video-sync=display-resample` in `mpv.conf`, and only with the `gpu` or `gpu-next` video output. The key shows the `interpolation` option, not whether mpv is actually interpolating.

**The picture stutters when I turn Audio Delay, or the delay seems to do nothing.**
That is how mpv applies a change to `audio-delay`; mpv doesn't move the audio, it retimes the video against it, so the picture briefly freezes or skips frames to catch up. With `video-sync=display-resample` it instead corrects by playing the audio very slightly faster or slower (0.125% by default, see `video-sync-max-audio-change`), so a change of 100 ms can take over a minute to take full effect. Seeking, even by a second, makes mpv resynchronise with the new delay at once.

**My custom command works in `input.conf` but shows nothing on screen here.**
Unlike `input.conf`, the plugin doesn't show an OSD unless one is selected in the action's settings, and `${property}` is only expanded when *Expand* is ticked.

## Building

Requires the .NET 10 SDK (the version in `global.json` or a later 10.0 one) and [Node.js](https://nodejs.org). The build tools - [GitVersion](https://gitversion.net) and the [Elgato CLI](https://docs.elgato.com/streamdeck/cli/intro) - are pinned in the repository (`.config/dotnet-tools.json` and `package.json`) and installed by `dotnet tool restore` and `npm ci`.

`build-and-deploy.ps1` builds and tests the plugin, validates and packs it, and reinstalls it into Stream Deck.

Released packages are self-contained: the GitHub build includes the .NET runtime, so users don't need to install it. A local build is not, and runs on the .NET installed with the SDK.

## License

[MIT](LICENSE)
