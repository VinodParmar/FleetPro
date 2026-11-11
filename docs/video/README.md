# Hindi walkthrough video

Builds `docs/FleetPro_Hindi_Walkthrough.mp4` — a narrated (Hindi) screen-by-screen tour of the live app,
with an on-screen cursor, amber spotlight on the part being explained, and burned-in Hindi subtitles.

## How it works

1. `scenes.js` lists each scene: Hindi narration split into short "beats", and the on-screen actions for each beat.
2. Each beat is voiced with Microsoft's neural Hindi voice (`hi-IN-SwaraNeural`) through **edge-tts**
   (needs internet; the narration text is sent to Microsoft's speech service). Audio is cached in `work/`.
3. The app is driven in headless Chrome **in Hindi** and recorded; every action is paced to its narration.
4. ffmpeg mixes the narration at the recorded offsets, burns in subtitles (Nirmala UI) and joins all scenes.

Recording never presses Save / Add / Delete — forms are filled for the camera and then cancelled,
so **no data is changed**. Logging in as the sample users is the only thing that writes (audit log entries).

## Setup (once)

```powershell
pip install edge-tts imageio-ffmpeg       # voice + bundled ffmpeg
cd docs/deck-tools; npm install           # puppeteer etc. (shared with the deck tools)
```
Google Chrome must be installed.

## Build

1. Run the app with sample data (`cd src/FleetPro.Web; dotnet run`).
2. Set the logins (don't commit these):
   ```powershell
   $env:FP_TENANT_EMAIL = "sunil@parmartravels.in"; $env:FP_TENANT_PASSWORD = "<password>"
   $env:FP_ADMIN_EMAIL  = "vinod@fleetpro.in";      $env:FP_ADMIN_PASSWORD  = "<password>"
   ```
3. Run from `docs/video`:
   ```powershell
   node make-video.js --check        # fast dry run: reports any button/field it can't find
   node make-video.js                # builds missing scenes and joins → docs/FleetPro_Hindi_Walkthrough.mp4
   node make-video.js --only trucks  # re-record one scene (e.g. after changing its text), then re-join
   node make-video.js --force        # re-record everything
   ```
   `chapters.txt` gets the start time of each chapter (handy for a YouTube description).

Options: `FP_BASE_URL` (default `https://localhost:58789`), `FP_VOICE` (`hi-IN-MadhurNeural` for a male voice),
`FP_VOICE_RATE` (default `-4%`), `CHROME_PATH`, `FFMPEG_PATH`.

## Editing the narration

Change the `say:` text in `scenes.js` and rebuild that scene with `--only <id>`.
Keep beats short (1–2 sentences) — each beat's actions run while that sentence is spoken.
Intro/outro images in `assets/` are exported from the Hindi deck (slides 1, 4 and 24).
