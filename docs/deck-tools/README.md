# Client deck tools

Builds `docs/FleetPro_Client_Overview.pptx` — a non-technical client presentation with real screenshots of the app.

## One-time setup

```powershell
cd docs/deck-tools
npm install
```
Google Chrome must be installed (path can be overridden with `CHROME_PATH`).

## Rebuild the deck

1. Start the app: `cd src/FleetPro.Web; dotnet run` (uses the local DB with sample data).
2. Set the logins used for screenshots (don't commit these):
   ```powershell
   $env:FP_TENANT_EMAIL = "sunil@parmartravels.in"; $env:FP_TENANT_PASSWORD = "<password>"
   $env:FP_ADMIN_EMAIL  = "vinod@fleetpro.in";      $env:FP_ADMIN_PASSWORD  = "<password>"
   ```
3. Run:
   ```powershell
   npm run all      # capture screenshots, then build the .pptx
   npm run build    # rebuild only (reuses existing shots/)
   ```

### Hindi version
```powershell
$env:FP_LANG = "hi";   node shoot.js     # Hindi screenshots → shots-hi/
$env:DECK_LANG = "hi"; node build.js     # → docs/FleetPro_Client_Overview_Hindi.pptx
```
Export to PDF from PowerPoint (File → Export → PDF), or with PowerShell:
```powershell
$app = New-Object -ComObject PowerPoint.Application
$p = $app.Presentations.Open("$PWD\..\FleetPro_Client_Overview_Hindi.pptx", $true, $false, $false)
$p.SaveAs("$PWD\..\FleetPro_Client_Overview_Hindi.pdf", 32); $p.Close(); $app.Quit()
```
The Hindi deck uses the Nirmala UI font (included with Windows/Office). All slide text has an English and Hindi version side by side in `build.js` — `t('English', 'हिंदी')`.

## Files

| File | Purpose |
|---|---|
| `shoot.js` | Logs in with headless Chrome and saves each screen to `shots/` (hides the login page's demo-credentials box, crops the Hindi dashboard) |
| `build.js` | Builds the deck: slide text, speaker notes, layout and theme. Edit wording here |
| `apply_theme.js` | Writes the deck's colour theme into the .pptx after it's generated |
| `grid.js` | Optional: contact sheet of slide images in `render/` for a quick visual check |

Screenshots are cropped to remove the app's left menu so the content reads larger on slides.
If you add a screen, capture it in `shoot.js` and add a `featureSlide(...)` call in `build.js`.
