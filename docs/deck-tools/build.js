// FleetPro client overview deck (non-technical audience)
// English:  node build.js               → ../FleetPro_Client_Overview.pptx       (screens from shots/)
// Hindi:    DECK_LANG=hi node build.js  → ../FleetPro_Client_Overview_Hindi.pptx (screens from shots-hi/)
const pptxgen = require('pptxgenjs');
const React = require('react');
const ReactDOMServer = require('react-dom/server');
const sharp = require('sharp');
const fa = require('react-icons/fa');
const fs = require('fs');
const path = require('path');
const { applyTheme } = require('./apply_theme.js');

const LANG = process.env.DECK_LANG === 'hi' ? 'hi' : 'en';
const HI = LANG === 'hi';
const t = (en, hi) => (HI ? hi : en);
// Devanagari needs extra line spacing so wrapped lines don't touch
const LS = HI ? 1.2 : 1.0;

const SHOTS = path.join(__dirname, HI ? 'shots-hi' : 'shots');
const shotPath = n => path.join(SHOTS, n + '.png');
const OUT = path.join(__dirname, '..', HI ? 'FleetPro_Client_Overview_Hindi.pptx' : 'FleetPro_Client_Overview.pptx');

// Calibri has no Devanagari; Nirmala UI ships with Windows and Office and covers Hindi + Latin.
const FONT = HI ? 'Nirmala UI' : 'Calibri';
const THEME = {
  name: 'FleetPro Highway',
  headFontFace: FONT,
  bodyFontFace: FONT,
  colors: {
    dk1: '1B2238', lt1: 'FFFFFF', dk2: '1E2A4F', lt2: 'EEF2F8',
    accent1: 'F5A623', accent2: '1FB5B0', accent3: 'E5484D',
    accent4: '2E9E5B', accent5: '3B6FD8', accent6: '7A869A',
    hlink: '3B6FD8', folHlink: '7A869A',
  },
};
const HEX = THEME.colors;

const pres = new pptxgen();
pres.layout = 'LAYOUT_WIDE'; // 13.33 x 7.5
pres.title = t('FleetPro — Client Overview', 'FleetPro — परिचय');
pres.author = 'Vinod Parmar';
pres.company = 'FleetPro';
pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
const C = pres.SchemeColor;

// ── icons ─────────────────────────────────────────────
async function icon(Comp, color, size = 256) {
  const svg = ReactDOMServer.renderToStaticMarkup(React.createElement(Comp, { color: '#' + color, size: String(size) }));
  const buf = await sharp(Buffer.from(svg)).png().toBuffer();
  return 'image/png;base64,' + buf.toString('base64');
}

// ── layouts ───────────────────────────────────────────
const footer = (dark) => [
  { text: { text: 'FleetPro', options: { x: 0.6, y: 6.95, w: 3, h: 0.3, fontSize: 10, color: dark ? C.background2 : C.accent6, margin: 0 } } },
];
pres.defineSlideMaster({
  title: 'TITLE', background: { color: HEX.dk2 },
  objects: [
    { placeholder: { options: { name: 'title', type: 'title', align: 'left', x: 0.8, y: 2.3, w: 8.2, h: 1.5, fontSize: 54, bold: true, color: C.background1, valign: 'bottom', margin: 0 }, text: '' } },
    { placeholder: { options: { name: 'body', type: 'body', align: 'left', x: 0.8, y: 3.95, w: 8.2, h: 1.3, fontSize: 22, color: C.accent1, valign: 'top', margin: 0, lineSpacingMultiple: LS }, text: '' } },
  ],
});
pres.defineSlideMaster({
  title: 'SECTION', background: { color: HEX.dk2 },
  objects: [
    { placeholder: { options: { name: 'title', type: 'title', align: 'left', x: 0.8, y: 2.95, w: 11.7, h: 1.1, fontSize: 40, bold: true, color: C.background1, valign: 'middle', margin: 0 }, text: '' } },
    { placeholder: { options: { name: 'body', type: 'body', align: 'left', x: 0.8, y: 4.1, w: 11.7, h: 0.8, fontSize: 20, color: C.background2, valign: 'top', margin: 0 }, text: '' } },
    ...footer(true),
  ],
  slideNumber: { x: 12.2, y: 6.95, w: 0.6, h: 0.3, fontSize: 10, color: C.background2, align: 'right' },
});
pres.defineSlideMaster({
  title: 'CLOSING', background: { color: HEX.dk2 },
  objects: [
    { placeholder: { options: { name: 'title', type: 'title', align: 'left', x: 0.8, y: 0.7, w: 11.7, h: 0.95, fontSize: 40, bold: true, color: C.background1, valign: 'middle', margin: 0 }, text: '' } },
    ...footer(true),
  ],
  slideNumber: { x: 12.2, y: 6.95, w: 0.6, h: 0.3, fontSize: 10, color: C.background2, align: 'right' },
});
pres.defineSlideMaster({
  title: 'CONTENT', background: { color: HEX.lt1 },
  objects: [
    { placeholder: { options: { name: 'title', type: 'title', align: 'left', x: 0.6, y: 0.35, w: 12.1, h: 0.9, fontSize: 34, bold: true, color: C.text2, valign: 'middle', margin: 0 }, text: '' } },
    ...footer(false),
  ],
  slideNumber: { x: 12.2, y: 6.95, w: 0.6, h: 0.3, fontSize: 10, color: C.accent6, align: 'right' },
});

// ── helpers ───────────────────────────────────────────
let section = '';
function add(master, title, notes) {
  const s = pres.addSlide({ masterName: master, sectionTitle: section });
  s.addText(title, { placeholder: 'title' });
  if (notes) s.addNotes(notes);
  return s;
}
function newSection(title) { section = title; pres.addSection({ title }); }
function sectionSlide(title, sub) {
  const s = pres.addSlide({ masterName: 'SECTION', sectionTitle: section });
  s.addText(title, { placeholder: 'title' });
  s.addText(sub, { placeholder: 'body' });
}

// Screenshot in a soft white frame with shadow
function screenshot(s, name, x, y, w, h, alt) {
  s.addShape(pres.shapes.ROUNDED_RECTANGLE, {
    x: x - 0.08, y: y - 0.08, w: w + 0.16, h: h + 0.16, rectRadius: 0.08,
    fill: { color: C.background1 }, line: { color: 'D5DBE6', width: 0.75 },
    shadow: { type: 'outer', color: '1B2238', opacity: 0.18, blur: 10, offset: 3, angle: 90 },
    objectName: 'Frame ' + name,
  });
  s.addImage({ path: shotPath(name), x, y, w, h, altText: alt, objectName: 'Screenshot ' + name });
}

// Numbered amber circle + bold header + description (the deck's motif)
function point(s, n, x, y, w, head, desc) {
  s.addShape(pres.shapes.OVAL, { x, y, w: 0.5, h: 0.5, fill: { color: C.accent1 }, line: { type: 'none' }, objectName: 'Step ' + n });
  s.addText(String(n), { x, y, w: 0.5, h: 0.5, fontSize: 16, bold: true, color: C.text2, align: 'center', valign: 'middle', margin: 0, isTextBox: true });
  s.addText(head, { x: x + 0.68, y: y - 0.04, w: w - 0.68, h: 0.46, fontSize: 18, bold: true, color: C.text2, valign: 'top', margin: 0, isTextBox: true, objectName: 'Point head ' + n });
  s.addText(desc, { x: x + 0.68, y: y + 0.44, w: w - 0.68, h: 0.95, fontSize: 14, color: C.text1, valign: 'top', margin: 0, lineSpacingMultiple: LS, isTextBox: true, objectName: 'Point text ' + n });
}

// App screenshots are cropped to remove the left menu bar so the content is larger
const DIMS = {};
async function prepShots() {
  if (!fs.existsSync(SHOTS)) throw new Error(`No screenshots in ${SHOTS} — run shoot.js first${HI ? ' with FP_LANG=hi' : ''}`);
  for (const f of fs.readdirSync(SHOTS)) {
    const name = f.replace(/\.png$/, '');
    if (!/^\d\d-/.test(name) || name.endsWith('-full')) continue;
    const keepWhole = name === '01-login' || name === '19-dashboard-hindi-crop';
    const src = shotPath(name);
    if (keepWhole) { const m = await sharp(src).metadata(); DIMS[name] = { name, r: m.width / m.height }; continue; }
    // Pop-up dialogs sit near the top of the page, so keep the top bar for those
    const top = name === '12-expense-popup' ? 20 : 57, height = 810 - top;
    await sharp(src).extract({ left: 250, top, width: 1190, height }).toFile(shotPath('c-' + name));
    DIMS[name] = { name: 'c-' + name, r: 1190 / height };
  }
}

// Standard feature slide: 3 numbered points on the left, screenshot on the right
function featureSlide(title, shot, alt, points, notes) {
  const s = add('CONTENT', title, notes);
  const d = DIMS[shot];
  const maxW = 7.9, maxH = 5.15;
  let iw = maxW, ih = iw / d.r;
  if (ih > maxH) { ih = maxH; iw = ih * d.r; }
  screenshot(s, d.name, 4.85 + (maxW - iw), 1.55, iw, ih, alt);
  points.forEach((p, i) => point(s, i + 1, 0.6, 1.65 + i * 1.55, 3.9, p[0], p[1]));
  return s;
}

(async () => {
  await prepShots();
  const I = {
    truck: await icon(fa.FaTruck, HEX.lt1), driver: await icon(fa.FaIdCard, HEX.lt1),
    route: await icon(fa.FaRoute, HEX.lt1), rupee: await icon(fa.FaRupeeSign, HEX.lt1),
    bell: await icon(fa.FaBell, HEX.lt1), chart: await icon(fa.FaChartLine, HEX.lt1),
    book: await icon(fa.FaBook, HEX.accent3), phone: await icon(fa.FaMobileAlt, HEX.accent3),
    calc: await icon(fa.FaCalculator, HEX.accent3), forget: await icon(fa.FaRegCalendarTimes, HEX.accent3),
    check: await icon(fa.FaCheckCircle, HEX.accent4),
    arrow: await icon(fa.FaChevronRight, HEX.accent6), truckBig: await icon(fa.FaTruckMoving, HEX.accent1, 512),
  };

  // ═════ 1. Title ═════
  newSection(t('Introduction', 'परिचय'));
  {
    const s = pres.addSlide({ masterName: 'TITLE', sectionTitle: section });
    s.addText('FleetPro', { placeholder: 'title' });
    s.addText(t('Your whole transport business — trucks, drivers, trips and money — in one simple app',
                'आपका पूरा ट्रांसपोर्ट कारोबार — ट्रक, ड्राइवर, ट्रिप और पैसा — एक आसान ऐप में'), { placeholder: 'body' });
    s.addImage({ data: I.truckBig, x: 9.6, y: 2.4, w: 2.9, h: 2.9, altText: t('Truck illustration', 'ट्रक का चित्र'), objectName: 'Hero truck' });
    s.addText(t('Client overview', 'ग्राहक के लिए परिचय'), { x: 0.8, y: 6.6, w: 6, h: 0.4, fontSize: 14, color: C.background2, margin: 0, isTextBox: true });
    s.addNotes(t('Welcome. In the next few minutes we will walk through FleetPro screen by screen. No technical knowledge is needed — if you run trucks, you already understand everything in this app.',
                 'नमस्ते। अगले कुछ मिनटों में हम FleetPro की हर स्क्रीन देखेंगे। कोई तकनीकी जानकारी की ज़रूरत नहीं है — अगर आप ट्रक चलाते हैं, तो इस ऐप की हर चीज़ आप पहले से समझते हैं।'));
  }

  // ═════ 2. Problem → solution ═════
  {
    const s = add('CONTENT', t('Why FleetPro?', 'FleetPro क्यों?'),
      t('Most transporters today keep trip details in a register, payments in WhatsApp chats and expenses in a notebook or Excel. Papers expire without anyone noticing. FleetPro brings all of it into one place and does the maths for you.',
        'आज ज़्यादातर ट्रांसपोर्टर ट्रिप का हिसाब रजिस्टर में, पेमेंट WhatsApp पर और खर्च कॉपी या Excel में रखते हैं। गाड़ी के कागज़ कब खत्म हो गए, पता ही नहीं चलता। FleetPro यह सब एक जगह रखता है और हिसाब खुद लगाता है।'));
    const colW = 5.85, y0 = 1.6;
    const cols = [
      { x: 0.6, head: t('Today, without FleetPro', 'आज — FleetPro के बिना'), fill: 'FDECEC', items: [
        [I.book, t('Trip details written in registers', 'ट्रिप का हिसाब रजिस्टर में')],
        [I.phone, t('Payments tracked on phone and WhatsApp', 'पेमेंट फ़ोन और WhatsApp पर')],
        [I.calc, t('Profit worked out by hand at month end', 'महीने के आख़िर में हाथ से मुनाफ़ा जोड़ना')],
        [I.forget, t('Insurance or licence expiry noticed too late', 'बीमा या लाइसेंस खत्म होने का देर से पता')],
      ]},
      { x: 6.88, head: t('With FleetPro', 'FleetPro के साथ'), fill: 'E8F6EE', items: [
        [I.check, t('Every trip saved in one place, searchable', 'हर ट्रिप एक जगह, तुरंत खोजें')],
        [I.check, t('Money received and pending shown per trip', 'हर ट्रिप का मिला और बाकी पैसा सामने')],
        [I.check, t('Profit calculated automatically, any time', 'मुनाफ़ा अपने-आप, कभी भी देखें')],
        [I.check, t('Automatic reminders before papers expire', 'कागज़ खत्म होने से पहले याद दिलाना')],
      ]},
    ];
    cols.forEach((c, ci) => {
      s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x: c.x, y: y0, w: colW, h: 5.0, rectRadius: 0.12, fill: { color: c.fill }, line: { type: 'none' }, objectName: 'Column ' + ci });
      s.addText(c.head, { x: c.x + 0.4, y: y0 + 0.3, w: colW - 0.8, h: 0.55, fontSize: 22, bold: true, color: C.text2, margin: 0, isTextBox: true });
      c.items.forEach((it, i) => {
        const yy = y0 + 1.1 + i * 0.92;
        s.addImage({ data: it[0], x: c.x + 0.4, y: yy + 0.06, w: 0.42, h: 0.42, altText: '', objectName: `Icon ${ci}-${i}` });
        s.addText(it[1], { x: c.x + 1.0, y: yy, w: colW - 1.4, h: 0.55, fontSize: 17, color: C.text1, valign: 'middle', margin: 0, isTextBox: true });
      });
    });
  }

  // ═════ 3. At a glance ═════
  {
    const s = add('CONTENT', t('Everything a transporter needs, in six parts', 'ट्रांसपोर्टर की हर ज़रूरत — छह हिस्सों में'),
      t('These are the six main parts of FleetPro. We will look at each one with a real screen from the app.',
        'ये FleetPro के छह मुख्य हिस्से हैं। आगे हम हर हिस्से को ऐप की असली स्क्रीन के साथ देखेंगे।'));
    const cards = [
      [I.truck, t('Trucks', 'ट्रक'), t('Every vehicle and its papers', 'हर गाड़ी और उसके कागज़')],
      [I.driver, t('Drivers & Agents', 'ड्राइवर और एजेंट'), t('Your drivers and the parties who give loads', 'आपके ड्राइवर और माल देने वाली पार्टियाँ')],
      [I.route, t('Trips', 'ट्रिप'), t('Going and return loads, km, rate, deal', 'जाने और वापसी का माल, किमी, रेट, सौदा')],
      [I.rupee, t('Money', 'पैसा'), t('Payments received, expenses, pending', 'मिला पेमेंट, खर्च और बाकी रकम')],
      [I.bell, t('Reminders', 'रिमाइंडर'), t('Alerts before any paper expires', 'कागज़ खत्म होने से पहले सूचना')],
      [I.chart, t('Reports', 'रिपोर्ट'), t('Profit & loss, Excel export, print', 'लाभ-हानि, Excel और प्रिंट')],
    ];
    const cw = 3.75, ch = 2.25, gx = 0.425, gy = 0.35;
    const colors = [C.accent5, C.accent2, C.accent1, C.accent4, C.accent3, C.text2];
    cards.forEach((c, i) => {
      const x = 0.6 + (i % 3) * (cw + gx), y = 1.6 + Math.floor(i / 3) * (ch + gy);
      s.addShape(pres.shapes.ROUNDED_RECTANGLE, { x, y, w: cw, h: ch, rectRadius: 0.12, fill: { color: C.background2 }, line: { type: 'none' }, objectName: 'Card ' + i });
      s.addShape(pres.shapes.OVAL, { x: x + 0.35, y: y + 0.35, w: 0.8, h: 0.8, fill: { color: colors[i] }, line: { type: 'none' }, objectName: 'Icon circle ' + i });
      s.addImage({ data: c[0], x: x + 0.55, y: y + 0.55, w: 0.4, h: 0.4, altText: '', objectName: 'Icon ' + i });
      s.addText(c[1], { x: x + 1.35, y: y + 0.4, w: cw - 1.55, h: 0.7, fontSize: 20, bold: true, color: C.text2, valign: 'middle', margin: 0, isTextBox: true });
      s.addText(c[2], { x: x + 0.35, y: y + 1.35, w: cw - 0.7, h: 0.75, fontSize: 15, color: C.text1, valign: 'top', margin: 0, lineSpacingMultiple: LS, isTextBox: true });
    });
  }

  // ═════ 4. How it works ═════
  {
    const s = add('CONTENT', t('How it works, in five simple steps', 'काम कैसे होता है — पाँच आसान कदम'),
      t('This is the normal daily flow. Step 1 is done only once per truck or driver. Steps 2 to 4 happen for every trip. Step 5 — the profit — is calculated by FleetPro automatically.',
        'यह रोज़ का सामान्य तरीका है। कदम 1 हर ट्रक या ड्राइवर के लिए सिर्फ़ एक बार होता है। कदम 2 से 4 हर ट्रिप के लिए होते हैं। कदम 5 — मुनाफ़ा — FleetPro खुद निकालता है।'));
    const steps = [
      [t('Add trucks & drivers', 'ट्रक और ड्राइवर जोड़ें'), t('Once, at the start', 'शुरुआत में, सिर्फ़ एक बार')],
      [t('Create a trip', 'ट्रिप बनाएं'), t('Pick truck, driver, route and load', 'ट्रक, ड्राइवर, रूट और माल चुनें')],
      [t('Record expenses', 'खर्च लिखें'), t('Diesel, toll, repairs — with bill photo', 'डीज़ल, टोल, मरम्मत — बिल की फ़ोटो के साथ')],
      [t('Record payments', 'पेमेंट लिखें'), t('Advance and balance from the party', 'पार्टी से एडवांस और बाकी रकम')],
      [t('See your profit', 'मुनाफ़ा देखें'), t('Calculated automatically', 'अपने-आप हिसाब')],
    ];
    const w = 2.2, gap = 0.275, y = 2.6;
    steps.forEach((st, i) => {
      const x = 0.6 + i * (w + gap);
      s.addShape(pres.shapes.OVAL, { x: x + (w - 1.1) / 2, y, w: 1.1, h: 1.1, fill: { color: i === 4 ? C.accent4 : C.accent1 }, line: { type: 'none' }, objectName: 'Flow step ' + (i + 1) });
      s.addText(String(i + 1), { x: x + (w - 1.1) / 2, y, w: 1.1, h: 1.1, fontSize: 36, bold: true, color: C.text2, align: 'center', valign: 'middle', margin: 0, isTextBox: true });
      s.addText(st[0], { x, y: y + 1.35, w, h: 0.75, fontSize: 18, bold: true, color: C.text2, align: 'center', valign: 'top', margin: 0, isTextBox: true });
      s.addText(st[1], { x, y: y + 2.15, w, h: 0.85, fontSize: 14, color: C.text1, align: 'center', valign: 'top', margin: 0, lineSpacingMultiple: LS, isTextBox: true });
      if (i < 4) s.addImage({ data: I.arrow, x: x + w + gap / 2 - 0.14, y: y + 0.41, w: 0.28, h: 0.28, altText: '', objectName: 'Arrow ' + (i + 1) });
    });
  }

  // ═════ Section: Getting started ═════
  newSection(t('Getting started', 'शुरुआत'));
  sectionSlide(t('Getting started', 'शुरुआत'), t('Logging in and your home screen', 'लॉगिन और आपकी मुख्य स्क्रीन'));

  featureSlide(t('Safe and private login', 'सुरक्षित और निजी लॉगिन'), '01-login', t('FleetPro login screen', 'FleetPro लॉगिन स्क्रीन'), [
    [t('Your own login', 'हर किसी का अपना लॉगिन'), t('Every staff member gets their own email and password.', 'हर स्टाफ़ को अपना ईमेल और पासवर्ड मिलता है।')],
    [t('Auto screen lock', 'स्क्रीन अपने-आप लॉक'), t('If the screen is left idle, it locks itself until the password is entered.', 'कुछ देर काम न हो तो स्क्रीन लॉक हो जाती है, पासवर्ड डालने पर खुलती है।')],
    [t('Your data stays yours', 'आपका डेटा सिर्फ़ आपका'), t('Each company sees only its own trucks, trips and money.', 'हर कंपनी सिर्फ़ अपने ट्रक, ट्रिप और पैसा देखती है।')],
  ], t('Every person signs in with their own email and password. If someone walks away from the computer, the screen locks itself. Most importantly: your data is private. Another transport company using FleetPro can never see your trucks, trips or money.',
       'हर व्यक्ति अपने ईमेल और पासवर्ड से लॉगिन करता है। अगर कोई कंप्यूटर छोड़कर चला जाए, तो स्क्रीन अपने-आप लॉक हो जाती है। सबसे ज़रूरी बात: आपका डेटा निजी है। FleetPro चलाने वाली दूसरी कोई कंपनी आपके ट्रक, ट्रिप या पैसे कभी नहीं देख सकती।'));

  featureSlide(t('Dashboard — your business at a glance', 'डैशबोर्ड — पूरा कारोबार एक नज़र में'), '02-dashboard', t('FleetPro dashboard with trucks, trips, revenue and charts', 'ट्रक, ट्रिप, आय और चार्ट वाला डैशबोर्ड'), [
    [t('Today’s picture', 'आज की स्थिति'), t('How many trucks are active, trips running and drivers on the team.', 'कितने ट्रक चालू हैं, कितनी ट्रिप चल रही हैं, कितने ड्राइवर हैं।')],
    [t('This month’s money', 'इस महीने का पैसा'), t('Revenue, expenses and net profit — updated by itself.', 'आय, खर्च और शुद्ध लाभ — अपने-आप अपडेट।')],
    [t('Charts and urgent alerts', 'चार्ट और ज़रूरी सूचनाएं'), t('Six-month trend, trip status and papers that need attention.', 'छह महीने का हाल, ट्रिप की स्थिति और ध्यान देने वाले कागज़।')],
  ], t('This is the first screen after login. At the top: trucks, active trips and drivers. Below that: this month\'s revenue, expenses and profit. The charts show the last six months and the red box lists urgent reminders. You don\'t enter anything here — it fills itself from your trips.',
       'लॉगिन के बाद यही पहली स्क्रीन है। ऊपर: ट्रक, चालू ट्रिप और ड्राइवर। उसके नीचे: इस महीने की आय, खर्च और मुनाफ़ा। चार्ट पिछले छह महीने दिखाते हैं और लाल बॉक्स में ज़रूरी सूचनाएं होती हैं। यहाँ कुछ भरना नहीं पड़ता — सब आपकी ट्रिप से अपने-आप भरता है।'));

  // ═════ Section: Your fleet ═════
  newSection(t('Your fleet', 'आपका वाहन बेड़ा'));
  sectionSlide(t('Your fleet', 'आपका वाहन बेड़ा'), t('Trucks, drivers and agents', 'ट्रक, ड्राइवर और एजेंट'));

  featureSlide(t('Trucks — every vehicle and its papers', 'ट्रक — हर गाड़ी और उसके कागज़'), '03-trucks', t('List of trucks with fitness and insurance expiry dates', 'फ़िटनेस और बीमा की तारीख़ों के साथ ट्रकों की सूची'), [
    [t('All trucks in one list', 'सभी ट्रक एक सूची में'), t('Number plate, model, year and status — search in seconds.', 'नंबर प्लेट, मॉडल, साल और स्थिति — सेकंडों में खोजें।')],
    [t('Paper expiry dates', 'कागज़ों की आख़िरी तारीख़'), t('Fitness, insurance, tax, permit, PUC and authorization.', 'फ़िटनेस, बीमा, टैक्स, परमिट, PUC और ऑथराइज़ेशन।')],
    [t('Colour tells the story', 'रंग से तुरंत पता'), t('Green means fine. Red means expired — act now.', 'हरा मतलब ठीक है। लाल मतलब खत्म — तुरंत ध्यान दें।')],
  ], t('Here you see all your trucks. The most useful part is the expiry columns: green means the paper is valid, red means it has expired. The top boxes show totals, and how many trucks need attention.',
       'यहाँ आपके सारे ट्रक दिखते हैं। सबसे काम की चीज़ है कागज़ों की तारीख़: हरा मतलब कागज़ चालू है, लाल मतलब खत्म हो चुका है। ऊपर के बॉक्स में कुल ट्रक और ध्यान देने वाले ट्रकों की गिनती है।'));

  featureSlide(t('Adding a truck takes one simple form', 'ट्रक जोड़ना — बस एक आसान फ़ॉर्म'), '04-truck-form', t('Form to add a new truck', 'नया ट्रक जोड़ने का फ़ॉर्म'), [
    [t('Fill it once', 'एक बार भरें'), t('Number plate, model, make and load capacity.', 'नंबर प्लेट, मॉडल, कंपनी और भार क्षमता।')],
    [t('Enter paper dates', 'कागज़ों की तारीख़ डालें'), t('FleetPro will remind you before each one expires.', 'हर कागज़ खत्म होने से पहले FleetPro याद दिलाएगा।')],
    [t('Change any time', 'कभी भी बदलें'), t('Edit details whenever a paper is renewed.', 'कागज़ नया बनने पर जानकारी बदल दें।')],
  ], t('Adding a truck is one form. Enter the number plate and model, and the dates of each paper. That\'s all — from now on FleetPro watches those dates for you.',
       'ट्रक जोड़ने के लिए एक ही फ़ॉर्म है। नंबर प्लेट, मॉडल और हर कागज़ की तारीख़ डालें। बस — अब से उन तारीख़ों पर FleetPro नज़र रखेगा।'));

  {
    const s = add('CONTENT', t('Drivers and agents', 'ड्राइवर और एजेंट'),
      t('Drivers: keep licence number and expiry, phone, salary and bank details in one place. Agents are the brokers or parties who give you loads — keep their contact details here so you can pick them when creating a trip.',
        'ड्राइवर: लाइसेंस नंबर और उसकी तारीख़, फ़ोन, तनख़्वाह और बैंक की जानकारी एक जगह रखें। एजेंट वे ब्रोकर या पार्टियाँ हैं जो आपको माल देती हैं — उनकी जानकारी यहाँ रखें ताकि ट्रिप बनाते समय चुन सकें।'));
    const w = 5.85, h = w / DIMS['05-drivers'].r, y = 1.55;
    screenshot(s, DIMS['05-drivers'].name, 0.6, y, w, h, t('List of drivers', 'ड्राइवरों की सूची'));
    screenshot(s, DIMS['06-agents'].name, 6.88, y, w, h, t('List of agents', 'एजेंटों की सूची'));
    const cap = [
      [0.6, t('Drivers', 'ड्राइवर'), t('Licence and expiry, phone, monthly salary and bank details.', 'लाइसेंस और उसकी तारीख़, फ़ोन, मासिक तनख़्वाह और बैंक जानकारी।')],
      [6.88, t('Agents (brokers / parties)', 'एजेंट (ब्रोकर / पार्टी)'), t('Who gives you loads — pick them when creating a trip.', 'जो आपको माल देते हैं — ट्रिप बनाते समय इन्हें चुनें।')],
    ];
    cap.forEach(([x, head, txt]) => {
      s.addText(head, { x, y: y + h + 0.25, w, h: 0.45, fontSize: 18, bold: true, color: C.text2, margin: 0, isTextBox: true });
      s.addText(txt, { x, y: y + h + 0.72, w, h: 0.6, fontSize: 14, color: C.text1, valign: 'top', margin: 0, lineSpacingMultiple: LS, isTextBox: true });
    });
  }

  // ═════ Section: Trips & money ═════
  newSection(t('Trips and money', 'ट्रिप और पैसा'));
  sectionSlide(t('Trips and money', 'ट्रिप और पैसा'), t('From loading to final payment', 'माल लदने से आख़िरी पेमेंट तक'));

  featureSlide(t('Trips — every journey in one list', 'ट्रिप — हर सफ़र एक सूची में'), '07-trips', t('List of trips with route, deal amount, expenses and profit', 'रूट, सौदा, खर्च और मुनाफ़े के साथ ट्रिप की सूची'), [
    [t('Full picture per trip', 'हर ट्रिप का पूरा हाल'), t('Route, driver, truck, km, deal amount, expenses and profit.', 'रूट, ड्राइवर, ट्रक, किमी, सौदे की रकम, खर्च और मुनाफ़ा।')],
    [t('Find anything fast', 'तुरंत खोजें'), t('Filter by date or status, or search by trip number or city.', 'तारीख़ या स्थिति से छाँटें, या ट्रिप नंबर / शहर से खोजें।')],
    [t('Export to Excel', 'Excel में निकालें'), t('One click gives you the list as an Excel file.', 'एक क्लिक में पूरी सूची Excel फ़ाइल में।')],
  ], t('Every trip is one row. You can see at a glance how much each trip earned, how much was spent and what profit it made. The totals row at the bottom adds everything up.',
       'हर ट्रिप एक लाइन में है। एक नज़र में दिखता है कि हर ट्रिप से कितना कमाया, कितना खर्च हुआ और कितना मुनाफ़ा हुआ। नीचे की लाइन में सबका जोड़ है।'));

  featureSlide(t('Going and return load — both in one trip', 'जाने और वापसी का माल — एक ही ट्रिप में'), '08-trip-edit', t('Trip screen showing up phase, deal amount and summary', 'अप चरण, सौदे की रकम और सारांश वाली ट्रिप स्क्रीन'), [
    [t('Up and Down journey', 'अप और डाउन सफ़र'), t('“Up” is the going load, “Down” is the return load.', '“अप” मतलब जाने का माल, “डाउन” मतलब वापसी का माल।')],
    [t('Maths done for you', 'हिसाब अपने-आप'), t('Weight × rate gives the deal amount. Meter readings give the km.', 'वज़न × रेट से सौदे की रकम। मीटर रीडिंग से किलोमीटर।')],
    [t('Live summary', 'तुरंत सारांश'), t('Left box shows deal amount, received, pending and profit.', 'बाएं बॉक्स में सौदा, मिला पैसा, बाकी और मुनाफ़ा।')],
  ], t('A trip has two parts: the going load (Up) and the return load (Down). Enter the weight and rate, and FleetPro calculates the deal amount. Enter start and end meter readings and it calculates the kilometres. The summary on the left updates by itself.',
       'एक ट्रिप के दो हिस्से होते हैं: जाने का माल (अप) और वापसी का माल (डाउन)। वज़न और रेट डालें, FleetPro सौदे की रकम निकाल देता है। शुरू और आख़िरी मीटर रीडिंग डालें, किलोमीटर अपने-आप निकल आते हैं। बाईं तरफ़ का सारांश खुद अपडेट होता है।'));

  featureSlide(t('Payments — know exactly what is pending', 'पेमेंट — बाकी रकम का पक्का हिसाब'), '09-trip-payments', t('Trip payments tab listing received and paid amounts', 'मिली और दी गई रकम वाला पेमेंट टैब'), [
    [t('Money in', 'आया पैसा'), t('Record advance and balance received from the party.', 'पार्टी से मिला एडवांस और बाकी पैसा लिखें।')],
    [t('Money out', 'गया पैसा'), t('Record amounts paid, for example to the driver.', 'दी गई रकम लिखें, जैसे ड्राइवर को दिया पैसा।')],
    [t('Pending amount', 'बाकी रकम'), t('FleetPro shows how much the party still owes on this trip.', 'इस ट्रिप पर पार्टी से कितना लेना बाकी है, FleetPro बताता है।')],
  ], t('Every payment for the trip is recorded here — cash, UPI or bank transfer. Money received is green, money paid is red. The summary shows how much is still pending from the party, so nothing is forgotten.',
       'ट्रिप का हर पेमेंट यहाँ लिखा जाता है — नकद, UPI या बैंक ट्रांसफ़र। मिला पैसा हरे रंग में, दिया पैसा लाल रंग में। सारांश बताता है कि पार्टी से कितना लेना बाकी है, ताकि कुछ भूलें नहीं।'));

  featureSlide(t('Expenses — with a photo of the bill', 'खर्च — बिल की फ़ोटो के साथ'), '12-expense-popup', t('Add expense pop-up form', 'खर्च जोड़ने का पॉप-अप फ़ॉर्म'), [
    [t('Quick pop-up form', 'छोटा पॉप-अप फ़ॉर्म'), t('Pick the trip and the type: diesel, toll, repair and more.', 'ट्रिप और खर्च का प्रकार चुनें: डीज़ल, टोल, मरम्मत आदि।')],
    [t('Attach the bill', 'बिल लगाएं'), t('Upload a photo or PDF of the receipt as proof.', 'सबूत के लिए रसीद की फ़ोटो या PDF लगाएं।')],
    [t('Profit updates itself', 'मुनाफ़ा अपने-आप बदलता है'), t('The expense is added to the trip and profit is recalculated.', 'खर्च ट्रिप में जुड़ता है और मुनाफ़ा दोबारा निकलता है।')],
  ], t('Expenses are added from a small pop-up. Choose the trip and the expense type, enter the amount, and attach a photo of the bill. The trip\'s profit is updated straight away.',
       'खर्च एक छोटे पॉप-अप से जोड़ा जाता है। ट्रिप और खर्च का प्रकार चुनें, रकम डालें और बिल की फ़ोटो लगाएं। ट्रिप का मुनाफ़ा तुरंत अपडेट हो जाता है।'));

  // ═════ Section: Stay in control ═════
  newSection(t('Stay in control', 'पूरा नियंत्रण आपके हाथ'));
  sectionSlide(t('Stay in control', 'पूरा नियंत्रण आपके हाथ'), t('Reminders, reports, language and team', 'रिमाइंडर, रिपोर्ट, भाषा और टीम'));

  featureSlide(t('Reminders before papers expire', 'कागज़ खत्म होने से पहले रिमाइंडर'), '13-alerts', t('Alerts screen listing expired and expiring documents', 'खत्म और खत्म होने वाले कागज़ों की सूचना स्क्रीन'), [
    [t('Automatic alerts', 'अपने-आप सूचना'), t('For truck insurance, fitness and driver licences.', 'ट्रक के बीमा, फ़िटनेस और ड्राइवर के लाइसेंस के लिए।')],
    [t('Easy colours', 'आसान रंग'), t('Red: 0–7 days. Yellow: 8–30 days. Blue: 31–60 days.', 'लाल: 0–7 दिन। पीला: 8–30 दिन। नीला: 31–60 दिन।')],
    [t('Bell on every screen', 'हर स्क्रीन पर घंटी'), t('The red number on the bell shows unread alerts.', 'घंटी पर लाल नंबर बताता है कितनी सूचनाएं नहीं देखीं।')],
  ], t('FleetPro checks every date for you. Anything expiring within 7 days — or already expired — is red. 8 to 30 days is yellow, and 31 to 60 days is blue. No more fines because a paper expired unnoticed.',
       'FleetPro हर तारीख़ खुद जाँचता है। जो कागज़ 7 दिन में खत्म होने वाला है — या खत्म हो चुका है — वह लाल दिखता है। 8 से 30 दिन वाला पीला, और 31 से 60 दिन वाला नीला। अब कागज़ खत्म होने पर चालान का डर नहीं।'));

  featureSlide(t('Profit & loss report in one click', 'लाभ-हानि रिपोर्ट — एक क्लिक में'), '15-pl-report', t('Profit and loss report with revenue, expenses and trip breakdown', 'आय, खर्च और ट्रिप-वार हिसाब वाली लाभ-हानि रिपोर्ट'), [
    [t('Pick any dates', 'कोई भी तारीख़ चुनें'), t('This month, last quarter or the whole year.', 'यह महीना, पिछले तीन महीने या पूरा साल।')],
    [t('Where money went', 'पैसा कहाँ गया'), t('Revenue against fuel, toll, maintenance, wages and others.', 'आय के सामने डीज़ल, टोल, मरम्मत, मज़दूरी और बाकी खर्च।')],
    [t('Print or Excel', 'प्रिंट या Excel'), t('Share it with your accountant or partner.', 'अपने अकाउंटेंट या पार्टनर को भेजें।')],
  ], t('Choose a date range and FleetPro builds the profit and loss statement: total revenue, expenses by type, and the net profit with margin. The table below shows profit trip by trip. Print it or download it as Excel.',
       'तारीख़ चुनें और FleetPro लाभ-हानि का पूरा हिसाब बना देता है: कुल आय, हर प्रकार का खर्च और मार्जिन के साथ शुद्ध लाभ। नीचे की टेबल में हर ट्रिप का मुनाफ़ा है। इसे प्रिंट करें या Excel में डाउनलोड करें।'));

  {
    const s = add('CONTENT', t('Works in Hindi and English', 'हिंदी और अंग्रेज़ी — दोनों में'),
      t('Every user can switch between Hindi and English with one click at the top of the screen. Menus, labels and buttons change language. The choice is remembered for next time.',
        'हर व्यक्ति स्क्रीन के ऊपर एक क्लिक से हिंदी या अंग्रेज़ी चुन सकता है। मेन्यू, नाम और बटन उसी भाषा में दिखते हैं। अगली बार भी वही भाषा याद रहती है।'));
    const w = 12.1, h = w * 385 / 1440;
    screenshot(s, '19-dashboard-hindi-crop', 0.6, 1.55, w, h, t('Dashboard shown in Hindi', 'हिंदी में डैशबोर्ड'));
    const items = [
      [t('One click to switch', 'एक क्लिक में बदलें'), t('Use the हि / En buttons at the top right.', 'ऊपर दाईं ओर हि / En बटन दबाएं।')],
      [t('Whole app changes', 'पूरा ऐप बदलता है'), t('Menus, labels and buttons appear in Hindi.', 'मेन्यू, नाम और बटन हिंदी में दिखते हैं।')],
      [t('Remembered', 'याद रहता है'), t('Each person’s choice is saved for next time.', 'हर व्यक्ति की पसंद अगली बार के लिए सेव रहती है।')],
    ];
    const cw = 3.75, gx = 0.425, y = 1.55 + h + 0.45;
    items.forEach((it, i) => point(s, i + 1, 0.6 + i * (cw + gx), y, cw, it[0], it[1]));
  }

  featureSlide(t('Your team, with the right access', 'आपकी टीम — सही अधिकार के साथ'), '17-user-permissions', t('User permissions screen with grant, role and deny options', 'अनुमति देने और रोकने वाली स्क्रीन'), [
    [t('Add your staff', 'अपना स्टाफ़ जोड़ें'), t('Each person gets a login under your company.', 'हर व्यक्ति को आपकी कंपनी में अपना लॉगिन मिलता है।')],
    [t('Ready-made roles', 'तैयार भूमिकाएं'), t('Admin: everything. Data entry: add and edit, no delete. Viewer: only see.', 'एडमिन: सब कुछ। डेटा एंट्री: जोड़ना-बदलना, हटाना नहीं। व्यूअर: सिर्फ़ देखना।')],
    [t('Fine-tune per person', 'हर व्यक्ति के लिए अलग'), t('Allow or block any single action with Grant / Deny.', 'Grant / Deny से कोई भी एक काम चालू या बंद करें।')],
  ], t('You decide who can do what. Choose a role — Admin, Data Entry or Viewer — and, if needed, allow or block individual actions for one person. For example, a data-entry person can add expenses but not delete trips.',
       'कौन क्या कर सकता है, यह आप तय करते हैं। भूमिका चुनें — एडमिन, डेटा एंट्री या व्यूअर — और ज़रूरत हो तो किसी एक व्यक्ति के लिए कोई काम चालू या बंद करें। जैसे, डेटा एंट्री वाला खर्च जोड़ सकता है पर ट्रिप हटा नहीं सकता।'));

  featureSlide(t('A full history of every change', 'हर बदलाव का पूरा रिकॉर्ड'), '18-audit', t('Audit log listing who changed what and when', 'किसने क्या और कब बदला — इसका रिकॉर्ड'), [
    [t('Who, what, when', 'किसने, क्या, कब'), t('Every add, edit and delete is recorded automatically.', 'हर जोड़ना, बदलना और हटाना अपने-आप दर्ज होता है।')],
    [t('Filter and search', 'छाँटें और खोजें'), t('By module and date range.', 'हिस्से और तारीख़ के हिसाब से।')],
    [t('Peace of mind', 'बेफ़िक्री'), t('Nothing changes silently — you can always check.', 'चुपचाप कुछ नहीं बदलता — आप कभी भी जाँच सकते हैं।')],
  ], t('The audit log records every change: who did it, what they changed and when. It is useful when two people remember something differently, or when you want to check what a staff member did.',
       'यह रिकॉर्ड हर बदलाव दर्ज करता है: किसने किया, क्या बदला और कब। जब दो लोगों को अलग-अलग याद हो, या आप जानना चाहें कि किसी स्टाफ़ ने क्या किया — तब यह बहुत काम आता है।'));

  // ═════ Section: For the platform owner ═════
  newSection(t('Many companies, one platform', 'कई कंपनियाँ, एक प्लेटफ़ॉर्म'));
  featureSlide(t('One platform for many transport companies', 'कई ट्रांसपोर्ट कंपनियों के लिए एक प्लेटफ़ॉर्म'), '21-companies', t('Companies list managed by the platform owner', 'प्लेटफ़ॉर्म मालिक की कंपनियों वाली सूची'), [
    [t('Separate company accounts', 'हर कंपनी का अलग खाता'), t('Each transporter has its own private space and logins.', 'हर ट्रांसपोर्टर की अपनी निजी जगह और लॉगिन।')],
    [t('Plans and limits', 'प्लान और सीमा'), t('Each company has a plan with a truck and user limit.', 'हर कंपनी के प्लान में ट्रक और यूज़र की सीमा।')],
    [t('Central control', 'एक जगह से नियंत्रण'), t('The owner manages companies, roles and expense types.', 'मालिक कंपनियाँ, भूमिकाएं और खर्च के प्रकार संभालता है।')],
  ], t('FleetPro is built so many transport companies can use it at the same time, each with complete privacy. The platform owner can add new companies, set their plan and limits, and manage common settings such as expense types.',
       'FleetPro ऐसा बना है कि कई ट्रांसपोर्ट कंपनियाँ एक साथ इसे चला सकें, और हर एक का डेटा पूरी तरह निजी रहे। प्लेटफ़ॉर्म का मालिक नई कंपनियाँ जोड़ सकता है, उनका प्लान और सीमा तय कर सकता है, और खर्च के प्रकार जैसी आम सेटिंग संभाल सकता है।'));

  // ═════ Closing ═════
  newSection(t('Summary', 'सारांश'));
  {
    const s = pres.addSlide({ masterName: 'CLOSING', sectionTitle: section });
    s.addText(t('What FleetPro gives you', 'FleetPro से आपको क्या मिलता है'), { placeholder: 'title' });
    const items = [
      [I.route, t('Every trip, payment and expense in one place', 'हर ट्रिप, पेमेंट और खर्च एक जगह')],
      [I.rupee, t('Profit and pending money — calculated automatically', 'मुनाफ़ा और बाकी पैसा — अपने-आप हिसाब')],
      [I.bell, t('Reminders before any paper expires', 'कोई भी कागज़ खत्म होने से पहले रिमाइंडर')],
      [I.chart, t('Ready reports to print or send as Excel', 'प्रिंट या Excel में भेजने के लिए तैयार रिपोर्ट')],
    ];
    items.forEach((it, i) => {
      const y = 2.0 + i * 1.0;
      s.addShape(pres.shapes.OVAL, { x: 0.8, y, w: 0.7, h: 0.7, fill: { color: C.accent1 }, line: { type: 'none' }, objectName: 'Benefit circle ' + i });
      s.addImage({ data: it[0], x: 0.97, y: y + 0.17, w: 0.36, h: 0.36, altText: '', objectName: 'Benefit icon ' + i });
      s.addText(it[1], { x: 1.75, y, w: 10.5, h: 0.7, fontSize: 22, color: C.background1, valign: 'middle', margin: 0, isTextBox: true });
    });
    s.addText(t('Thank you — questions welcome', 'धन्यवाद — आपके सवालों का स्वागत है'), { x: 0.8, y: 6.1, w: 11.7, h: 0.55, fontSize: 20, bold: true, color: C.accent1, margin: 0, isTextBox: true });
    s.addNotes(t('To sum up: one place for trips, money and papers; profit worked out for you; reminders before anything expires; and reports ready to share. Happy to answer any questions or give a live demo.',
                 'संक्षेप में: ट्रिप, पैसा और कागज़ — सब एक जगह; मुनाफ़े का हिसाब अपने-आप; कुछ भी खत्म होने से पहले रिमाइंडर; और भेजने के लिए तैयार रिपोर्ट। कोई भी सवाल हो तो पूछिए, या हम ऐप चलाकर भी दिखा सकते हैं।'));
  }

  await pres.writeFile({ fileName: OUT });
  await applyTheme(OUT, THEME);
  console.log('written', OUT);
})().catch(e => { console.error(e); process.exit(1); });
