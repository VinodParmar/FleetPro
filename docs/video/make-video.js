// FleetPro Hindi walkthrough video builder.
// Records the live app (in Hindi) scene by scene with a visible cursor and spotlight,
// narrates each beat with Microsoft's neural Hindi voice (edge-tts), burns in Hindi
// subtitles and joins everything into one MP4.
//
// Usage (app must be running; see README.md):
//   node make-video.js                 # build all scenes that are missing, then join
//   node make-video.js --force         # rebuild every scene
//   node make-video.js --only trips    # rebuild scenes whose id contains "trips"
//   node make-video.js --check         # walk all scenes quickly, report missing elements (no audio/video)
const path = require('path');
module.paths.unshift(path.join(__dirname, '..', 'deck-tools', 'node_modules'));
const puppeteer = require('puppeteer-core');
const fs = require('fs');
const crypto = require('crypto');
const { execFileSync } = require('child_process');

const BASE = process.env.FP_BASE_URL || 'https://localhost:58789';
const CHROME = process.env.CHROME_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const FFMPEG = process.env.FFMPEG_PATH || execFileSync('python', ['-c', 'import imageio_ffmpeg as f; print(f.get_ffmpeg_exe())']).toString().trim();
const VOICE = process.env.FP_VOICE || 'hi-IN-SwaraNeural';
const RATE = process.env.FP_VOICE_RATE || '-4%';
const W = 1440, H = 810;               // recorded viewport (upscaled to 1920x1080)
const WORK = path.join(__dirname, 'work');
const OUT_DIR = path.join(__dirname, 'out');
const FINAL = path.join(__dirname, '..', 'FleetPro_Hindi_Walkthrough.mp4');
fs.mkdirSync(WORK, { recursive: true }); fs.mkdirSync(OUT_DIR, { recursive: true });

const args = process.argv.slice(2);
const FORCE = args.includes('--force');
const CHECK = args.includes('--check');
const ONLY = args.includes('--only') ? args[args.indexOf('--only') + 1] : null;

function need(name) {
  const v = process.env[name];
  if (!v) { console.error(`Missing environment variable ${name}`); process.exit(1); }
  return v;
}
const CREDS = {
  tenant: { email: need('FP_TENANT_EMAIL'), password: need('FP_TENANT_PASSWORD') },
  admin: { email: need('FP_ADMIN_EMAIL'), password: need('FP_ADMIN_PASSWORD') },
};

const sleep = ms => new Promise(r => setTimeout(r, ms));
const ff = (a) => execFileSync(FFMPEG, ['-hide_banner', '-loglevel', 'error', '-y', ...a], { stdio: ['ignore', 'ignore', 'inherit'] });
function mediaDuration(file) {
  let out = '';
  try { execFileSync(FFMPEG, ['-hide_banner', '-i', file], { stdio: ['ignore', 'ignore', 'pipe'] }); }
  catch (e) { out = e.stderr.toString(); }
  const m = out.match(/Duration: (\d+):(\d+):([\d.]+)/);
  if (!m) throw new Error('Cannot read duration of ' + file);
  return (+m[1] * 3600 + +m[2] * 60 + +m[3]) * 1000;
}

// ── Text to speech (cached by text hash) ─────────────────────────────
function tts(text) {
  const key = crypto.createHash('md5').update(VOICE + RATE + text).digest('hex').slice(0, 12);
  const mp3 = path.join(WORK, `tts-${key}.mp3`), srt = path.join(WORK, `tts-${key}.srt`);
  if (!fs.existsSync(mp3)) {
    execFileSync('edge-tts', ['--voice', VOICE, `--rate=${RATE}`, '--text', text, '--write-media', mp3, '--write-subtitles', srt], { stdio: 'ignore' });
  }
  return { mp3, srt, ms: mediaDuration(mp3) };
}
const srtTime = s => { const [h, m, rest] = s.split(':'); const [sec, ms] = rest.split(','); return ((+h * 60 + +m) * 60 + +sec) * 1000 + +ms; };
function readSrt(file) {
  return fs.readFileSync(file, 'utf8').trim().split(/\r?\n\r?\n/).map(block => {
    const lines = block.split(/\r?\n/);
    const [a, b] = lines[1].split(' --> ');
    return { start: srtTime(a), end: srtTime(b), text: lines.slice(2).join(' ') };
  });
}

// ── In-page overlay: fake cursor, click ripple, spotlight, lock-screen hider ──
const OVERLAY = `(() => {
  const init = () => {
    if (document.getElementById('__cur')) return;
    const st = document.createElement('style');
    st.textContent = '#lockScreen{display:none!important}'
      + '#__cur{position:fixed;left:0;top:0;width:28px;height:28px;z-index:2147483647;pointer-events:none;filter:drop-shadow(0 2px 3px rgba(0,0,0,.45))}'
      + '.__spot{position:fixed;z-index:2147483640;pointer-events:none;border:4px solid #F5A623;border-radius:12px;box-shadow:0 0 0 9999px rgba(12,18,38,.45);opacity:0;transition:opacity .35s ease, left .45s ease, top .45s ease, width .45s ease, height .45s ease}'
      + '.__click{position:fixed;width:40px;height:40px;margin:-20px 0 0 -20px;border-radius:50%;background:rgba(245,166,35,.6);z-index:2147483646;pointer-events:none;animation:__pulse .55s ease-out forwards}'
      + '@keyframes __pulse{from{transform:scale(.3);opacity:1}to{transform:scale(1.7);opacity:0}}';
    document.head.appendChild(st);
    const c = document.createElement('div'); c.id = '__cur';
    c.innerHTML = '<svg viewBox="0 0 24 24" width="28" height="28"><path d="M4 2l15 11-6.5 1.2L16 21l-2.6 1.2-3.5-6.8L5 20z" fill="#fff" stroke="#111" stroke-width="1.3" stroke-linejoin="round"/></svg>';
    let p = [${W / 2}, ${H / 2}];
    try { p = JSON.parse(sessionStorage.getItem('__curpos')) || p; } catch (e) {}
    c.style.transform = 'translate(' + p[0] + 'px,' + p[1] + 'px)';
    document.body.appendChild(c);
    document.addEventListener('mousemove', e => {
      c.style.transform = 'translate(' + e.clientX + 'px,' + e.clientY + 'px)';
      try { sessionStorage.setItem('__curpos', JSON.stringify([e.clientX, e.clientY])); } catch (err) {}
    }, true);
    document.addEventListener('mousedown', e => {
      const r = document.createElement('div'); r.className = '__click';
      r.style.left = e.clientX + 'px'; r.style.top = e.clientY + 'px';
      document.body.appendChild(r); setTimeout(() => r.remove(), 700);
    }, true);
    window.__spot = (rect) => {
      let s = document.querySelector('.__spot');
      if (!s) { s = document.createElement('div'); s.className = '__spot'; document.body.appendChild(s); }
      const pad = 8;
      Object.assign(s.style, { left: (rect.x - pad) + 'px', top: (rect.y - pad) + 'px', width: (rect.w + pad * 2) + 'px', height: (rect.h + pad * 2) + 'px' });
      requestAnimationFrame(() => { s.style.opacity = '1'; });
    };
    window.__unspot = () => { const s = document.querySelector('.__spot'); if (s) s.style.opacity = '0'; };
  };
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();`;

// ── Helpers handed to each scene ──────────────────────────────────────
const missing = [];
function helpers(page, sceneId) {
  const h = { page };
  const warn = (what) => { missing.push(`${sceneId}: ${what}`); console.warn(`   ! ${sceneId}: not found → ${what}`); };

  // Find elements: CSS selector, optionally filtered by visible text
  h.find = async (sel, text) => {
    const handles = await page.$$(sel);
    for (const el of handles) {
      const ok = await el.evaluate((e, t) => {
        const r = e.getBoundingClientRect(); const style = getComputedStyle(e);
        if (r.width === 0 || r.height === 0 || style.visibility === 'hidden') return false;
        return !t || e.innerText.includes(t);
      }, text || null);
      if (ok) return el;
    }
    return null;
  };
  const resolve = async (target) => (typeof target === 'string' ? h.find(target) : Array.isArray(target) ? h.find(target[0], target[1]) : target);

  h.wait = (ms) => sleep(ms);
  h.ready = async () => {
    await page.evaluate(OVERLAY).catch(() => {});
  };
  h.go = async (url) => {
    await page.goto(BASE + url, { waitUntil: 'networkidle2', timeout: 60000 });
    await h.ready(); await sleep(400);
  };
  h.scrollInto = async (el) => {
    const moved = await el.evaluate(e => {
      const r = e.getBoundingClientRect();
      if (r.top >= 70 && r.bottom <= innerHeight - 20) return false;
      e.scrollIntoView({ behavior: 'smooth', block: r.height > innerHeight - 120 ? 'start' : 'center' });
      return true;
    });
    if (moved) await sleep(700);
  };
  h.scrollBy = async (y) => { await page.evaluate(dy => window.scrollBy({ top: dy, behavior: 'smooth' }), y); await sleep(800); };
  h.scrollTop = async () => { await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'smooth' })); await sleep(700); };

  h.move = async (target, { pause = 250 } = {}) => {
    const el = await resolve(target);
    if (!el) { warn(JSON.stringify(target)); return null; }
    await h.scrollInto(el);
    const b = await el.boundingBox();
    if (!b) { warn('no box ' + JSON.stringify(target)); return null; }
    await page.mouse.move(b.x + b.width / 2, b.y + Math.min(b.height / 2, 20), { steps: 28 });
    await sleep(pause);
    return el;
  };
  h.click = async (target, { nav = false } = {}) => {
    const el = await h.move(target);
    if (!el) return false;
    await page.mouse.down(); await sleep(90);
    if (nav) {
      await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2', timeout: 60000 }).catch(() => {}), page.mouse.up()]);
      await h.ready(); await sleep(400);
    } else { await page.mouse.up(); await sleep(400); }
    return true;
  };
  h.type = async (target, text, { clear = true, delay = 75 } = {}) => {
    const el = await h.move(target);
    if (!el) return;
    await page.mouse.down(); await page.mouse.up();
    if (clear) await el.evaluate(e => { e.value = ''; e.dispatchEvent(new Event('input', { bubbles: true })); });
    await page.keyboard.type(text, { delay });
    await el.evaluate(e => { e.dispatchEvent(new Event('change', { bubbles: true })); });
    await sleep(250);
  };
  // Native dropdowns don't render in recordings, so pick the option directly and flash the field
  h.choose = async (target, optionIndex = 1) => {
    const el = await h.move(target);
    if (!el) return;
    await page.mouse.down(); await page.mouse.up();
    await el.evaluate((e, i) => {
      const opts = [...e.options].filter(o => o.value !== '');
      const o = opts[Math.min(i - 1, opts.length - 1)];
      if (o) { e.value = o.value; e.dispatchEvent(new Event('change', { bubbles: true })); }
      e.style.transition = 'box-shadow .3s'; e.style.boxShadow = '0 0 0 4px rgba(245,166,35,.6)';
      setTimeout(() => { e.style.boxShadow = ''; }, 900);
    }, optionIndex);
    await sleep(500);
  };
  h.setValue = async (target, value) => {
    const el = await h.move(target);
    if (!el) return;
    await page.mouse.down(); await page.mouse.up();
    await el.evaluate((e, v) => { e.value = v; e.dispatchEvent(new Event('input', { bubbles: true })); e.dispatchEvent(new Event('change', { bubbles: true })); }, value);
    await sleep(400);
  };
  // Spotlight one target ('css', ['css','text'] or element), every match of a selector ({all:true}),
  // or the union of a list of targets ({many:true})
  h.spot = async (targets, { all = false, many = false, move = true, max = 12 } = {}) => {
    const list = [];
    if (all) {
      for (const el of (await page.$$(targets)).slice(0, max)) {
        const vis = await el.evaluate(e => e.getBoundingClientRect().width > 0);
        if (vis) list.push(el);
      }
    } else {
      for (const t of (many ? targets : [targets])) { const el = await resolve(t); if (el) list.push(el); }
    }
    if (!list.length) { warn('spot ' + JSON.stringify(targets)); return; }
    await h.scrollInto(list[0]);
    let x1 = Infinity, y1 = Infinity, x2 = -Infinity, y2 = -Infinity;
    for (const el of list) {
      const b = await el.boundingBox(); if (!b) continue;
      x1 = Math.min(x1, b.x); y1 = Math.min(y1, b.y); x2 = Math.max(x2, b.x + b.width); y2 = Math.max(y2, b.y + b.height);
    }
    y1 = Math.max(y1, 4); y2 = Math.min(y2, H - 4);
    await page.evaluate(r => window.__spot && window.__spot(r), { x: x1, y: y1, w: x2 - x1, h: y2 - y1 });
    if (move) await page.mouse.move(Math.min(x2 - 30, x1 + (x2 - x1) * 0.6), Math.min(y2 - 12, y1 + 28), { steps: 24 });
    await sleep(450);
  };
  h.unspot = async () => { await page.evaluate(() => window.__unspot && window.__unspot()); await sleep(350); };

  // Open a page from the left menu like a user would (expands the parent group first)
  h.nav = async (href) => {
    await h.unspot();
    const link = await page.$(`.nav-sidebar a[href="${href}"]`);
    if (!link) { warn('menu ' + href); return h.go(href); }
    const visible = await link.evaluate(e => e.getBoundingClientRect().height > 0 && e.offsetParent !== null);
    if (!visible) {
      const parent = await link.evaluateHandle(e => e.closest('ul.nav-treeview').closest('li.nav-item').querySelector(':scope > a'));
      await h.click(parent.asElement());
      await sleep(500);
    }
    await h.click(link, { nav: true });
  };
  return h;
}

// ── Browser sessions ──────────────────────────────────────────────────
async function newPage(browser, role) {
  const ctx = await browser.createBrowserContext();
  const page = await ctx.newPage();
  await page.setViewport({ width: W, height: H });
  await page.evaluateOnNewDocument(OVERLAY);
  await page.setCookie({ name: 'FleetPro_Lang', value: 'hi', url: BASE });
  if (role && role !== 'anon') {
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#Email', CREDS[role].email);
    await page.type('#Password', CREDS[role].password);
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type=submit]')]);
    if (page.url().includes('/Account/Login')) throw new Error('Login failed for ' + role);
  }
  return page;
}

// ── ASS subtitles (libass shapes Devanagari correctly) ────────────────
function assTime(ms) {
  const cs = Math.max(0, Math.round(ms / 10));
  const h = Math.floor(cs / 360000), m = Math.floor(cs / 6000) % 60, s = Math.floor(cs / 100) % 60, c = cs % 100;
  return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}.${String(c).padStart(2, '0')}`;
}
function writeAss(file, title, cues, totalMs) {
  const esc = t => t.replace(/[{}]/g, '').replace(/\n/g, '\\N');
  const lines = [
    '[Script Info]', 'ScriptType: v4.00+', 'PlayResX: 1920', 'PlayResY: 1080', 'WrapStyle: 0', '',
    '[V4+ Styles]',
    'Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding',
    'Style: Sub,Nirmala UI,46,&H00FFFFFF,&H00FFFFFF,&H00000000,&H9A1E120C,1,0,0,0,100,100,0,0,3,14,0,2,160,160,48,1',
    'Style: Title,Nirmala UI,44,&H00FFFFFF,&H00FFFFFF,&H004F2A1E,&H004F2A1E,1,0,0,0,100,100,0,0,3,18,0,7,40,40,36,1',
    '', '[Events]', 'Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text',
  ];
  if (title) lines.push(`Dialogue: 1,${assTime(0)},${assTime(Math.min(4500, totalMs))},Title,,0,0,0,,{\\fad(300,400)}${esc(title)}`);
  for (const c of cues) lines.push(`Dialogue: 0,${assTime(c.start)},${assTime(c.end)},Sub,,0,0,0,,${esc(c.text)}`);
  fs.writeFileSync(file, '\ufeff' + lines.join('\n') + '\n', 'utf8');
}
const ffPath = p => p.replace(/\\/g, '/').replace(/^([A-Za-z]):/, '$1\\:');

// Mix beat audio clips at their recorded offsets into one track
function mixAudio(beats, totalMs, outFile) {
  const inputs = [], filters = [];
  beats.forEach((b, i) => { inputs.push('-i', b.mp3); filters.push(`[${i}:a]adelay=${Math.round(b.offset)}|${Math.round(b.offset)}[a${i}]`); });
  filters.push(`${beats.map((_, i) => `[a${i}]`).join('')}amix=inputs=${beats.length}:normalize=0,apad,atrim=0:${(totalMs / 1000).toFixed(2)}[out]`);
  ff([...inputs, '-filter_complex', filters.join(';'), '-map', '[out]', '-ar', '48000', '-ac', '2', outFile]);
}

// ── Build one scene ───────────────────────────────────────────────────
async function buildScene(scene, sessions, idx) {
  const out = path.join(OUT_DIR, `${String(idx).padStart(2, '0')}-${scene.id}.mp4`);
  const wanted = ONLY ? scene.id.includes(ONLY) : FORCE;
  if (!CHECK && fs.existsSync(out) && !wanted) { console.log(`✓ ${scene.id} (cached)`); return out; }
  console.log(`● ${scene.id}`);

  const beats = scene.beats.map(b => ({ ...b, ...(CHECK ? { ms: 300 } : tts(b.say)) }));
  const GAP = 450;

  if (scene.image) {
    // Still image scene (intro/outro): beats play back to back
    let t = 600; for (const b of beats) { b.offset = t; t += b.ms + GAP; }
    const total = t + 600;
    if (CHECK) return out;
    const wav = path.join(WORK, `${scene.id}.wav`), ass = path.join(WORK, `${scene.id}.ass`);
    mixAudio(beats, total, wav);
    const cues = beats.flatMap(b => readSrt(b.srt).map(c => ({ start: c.start + b.offset, end: c.end + b.offset, text: c.text })));
    writeAss(ass, scene.title, cues, total);
    ff(['-loop', '1', '-framerate', '30', '-i', scene.image, '-i', wav, '-t', (total / 1000).toFixed(2),
      '-vf', `scale=1920:1080,format=yuv420p,fade=t=in:st=0:d=0.6,subtitles='${ffPath(ass)}'`,
      '-c:v', 'libx264', '-preset', 'medium', '-crf', '20', '-c:a', 'aac', '-b:a', '160k', '-r', '30', out]);
    return out;
  }

  // Live scene
  sessions[scene.role] = sessions[scene.role] || await newPage(sessions.browser, scene.role);
  const page = sessions[scene.role];
  const h = helpers(page, scene.id);
  if (scene.setup) await scene.setup(h);
  await h.ready();

  const webm = path.join(WORK, `${scene.id}.webm`);
  const rec = CHECK ? null : await page.screencast({ path: webm, ffmpegPath: FFMPEG, fps: 30, quality: 18 });
  const t0 = Date.now();
  await sleep(500);
  for (const b of beats) {
    b.offset = Date.now() - t0;
    await Promise.all([b.do ? b.do(h).catch(e => { missing.push(`${scene.id}: ${e.message}`); console.warn('   ! ' + e.message); }) : null, sleep(b.ms)]);
    await sleep(GAP);
  }
  await h.unspot();
  await sleep(500);
  const total = Date.now() - t0;
  if (CHECK) return out;
  await rec.stop();

  const wav = path.join(WORK, `${scene.id}.wav`), ass = path.join(WORK, `${scene.id}.ass`);
  mixAudio(beats, total, wav);
  const cues = beats.flatMap(b => readSrt(b.srt).map(c => ({ start: c.start + b.offset, end: Math.min(c.end + b.offset, total), text: c.text })));
  writeAss(ass, scene.title, cues, total);
  encodeLive(scene.id, out);
  return out;
}

// Length of a video by decoding it (the recorder's webm has no duration header)
function decodedDuration(file) {
  const r = require('child_process').spawnSync(FFMPEG, ['-hide_banner', '-i', file, '-f', 'null', '-'], { encoding: 'utf8', maxBuffer: 64 << 20 });
  const all = [...(r.stderr || '').matchAll(/time=(\d+):(\d+):([\d.]+)/g)];
  if (!all.length) throw new Error('Cannot measure ' + file);
  const m = all[all.length - 1];
  return (+m[1] * 3600 + +m[2] * 60 + +m[3]) * 1000;
}

// Combine recorded video + narration + subtitles. The recorder captures in real time but
// stamps frames at a different rate, so the video is retimed to the real (wall-clock) length.
function encodeLive(id, out) {
  const webm = path.join(WORK, `${id}.webm`), wav = path.join(WORK, `${id}.wav`), ass = path.join(WORK, `${id}.ass`);
  const total = mediaDuration(wav), vdur = decodedDuration(webm);
  const factor = (total / vdur).toFixed(5);
  console.log(`   retime ${id}: video ${(vdur / 1000).toFixed(1)}s → ${(total / 1000).toFixed(1)}s`);
  ff(['-i', webm, '-i', wav,
    '-vf', `setpts=PTS*${factor},scale=1920:1080:flags=lanczos,fps=30,format=yuv420p,subtitles='${ffPath(ass)}'`,
    '-c:v', 'libx264', '-preset', 'medium', '-crf', '20', '-c:a', 'aac', '-b:a', '160k', '-shortest', out]);
}

function joinScenes(scenes, outputs) {
  const list = path.join(WORK, 'concat.txt');
  fs.writeFileSync(list, outputs.map(o => `file '${o.replace(/\\/g, '/')}'`).join('\n'));
  ff(['-f', 'concat', '-safe', '0', '-i', list, '-c', 'copy', '-movflags', '+faststart', FINAL]);
  let t = 0; const chapters = [];
  outputs.forEach((o, i) => {
    const s = Math.round(t / 1000); chapters.push(`${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')} ${scenes[i].title || scenes[i].id}`);
    t += mediaDuration(o);
  });
  fs.writeFileSync(path.join(__dirname, 'chapters.txt'), chapters.join('\n') + '\n', 'utf8');
  console.log(`\nDone: ${FINAL} (${(t / 60000).toFixed(1)} min)`);
  console.log(chapters.join('\n'));
}

(async () => {
  const scenes = require('./scenes.js');
  // --remux: re-encode live scenes from the saved recordings in work/ (no browser, no re-recording)
  if (args.includes('--remux')) {
    const outputs = scenes.map((s, i) => {
      const out = path.join(OUT_DIR, `${String(i).padStart(2, '0')}-${s.id}.mp4`);
      if (!s.image && (!ONLY || s.id.includes(ONLY))) encodeLive(s.id, out);
      return out;
    });
    return joinScenes(scenes, outputs);
  }
  const browser = await puppeteer.launch({ executablePath: CHROME, headless: true, acceptInsecureCerts: true, args: [`--window-size=${W},${H}`] });
  const sessions = { browser };
  const outputs = [];
  try {
    for (let i = 0; i < scenes.length; i++) outputs.push(await buildScene(scenes[i], sessions, i));
  } finally {
    await browser.close();
  }
  if (missing.length) { console.log('\nElements not found:'); missing.forEach(m => console.log('  - ' + m)); }
  if (CHECK) return;

  joinScenes(scenes, outputs);
})().catch(e => { console.error(e); process.exit(1); });
