// Capture FleetPro screenshots for the client deck.
// Requires the app to be running (dotnet run) and these environment variables:
//   FP_TENANT_EMAIL / FP_TENANT_PASSWORD   – a company admin with sample data
//   FP_ADMIN_EMAIL  / FP_ADMIN_PASSWORD    – the SuperAdmin (for Companies/Roles screens)
// Optional: FP_BASE_URL (default https://localhost:58789), CHROME_PATH,
//           FP_LANG=hi to capture the Hindi UI into shots-hi/ (default en → shots/).
const puppeteer = require('puppeteer-core');
const sharp = require('sharp');
const fs = require('fs');
const path = require('path');

const BASE = process.env.FP_BASE_URL || 'https://localhost:58789';
const CHROME = process.env.CHROME_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const LANG = process.env.FP_LANG === 'hi' ? 'hi' : 'en';
const OUT = path.join(__dirname, LANG === 'hi' ? 'shots-hi' : 'shots');
fs.mkdirSync(OUT, { recursive: true });

function need(name) {
  const v = process.env[name];
  if (!v) { console.error(`Missing environment variable ${name}`); process.exit(1); }
  return v;
}
const TENANT = { email: need('FP_TENANT_EMAIL'), password: need('FP_TENANT_PASSWORD') };
const ADMIN = { email: need('FP_ADMIN_EMAIL'), password: need('FP_ADMIN_PASSWORD') };

const HIDE_LOCK = '#lockScreen{display:none!important}';
const sleep = ms => new Promise(r => setTimeout(r, ms));

async function shot(page, name, url, opts = {}) {
  if (url) await page.goto(BASE + url, { waitUntil: 'networkidle2', timeout: 60000 });
  await page.addStyleTag({ content: HIDE_LOCK }).catch(() => {});
  if (opts.before) await opts.before(page);
  await sleep(opts.wait || 1200);
  await page.screenshot({ path: path.join(OUT, name + '.png') });
  console.log('saved', name);
}

async function login(browser, { email, password }, lang = LANG) {
  const ctx = await browser.createBrowserContext();
  const page = await ctx.newPage();
  await page.setViewport({ width: 1440, height: 810 });
  await page.setCookie({ name: 'FleetPro_Lang', value: lang, url: BASE });
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle2' });
  await page.type('#Email', email);
  await page.type('#Password', password);
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type=submit]')]);
  if (page.url().includes('/Account/Login')) throw new Error('Login failed for ' + email);
  return page;
}

async function hrefOf(page, selector) {
  return page.$$eval(selector, as => as.map(a => a.getAttribute('href'))).catch(() => []);
}

(async () => {
  const browser = await puppeteer.launch({ executablePath: CHROME, headless: true, acceptInsecureCerts: true });
  try {
    // ── Login page: hide any demo-credential box, zoom on the sign-in card ──
    const anon = await browser.newPage();
    await anon.setViewport({ width: 1440, height: 810, deviceScaleFactor: 2 });
    await anon.setCookie({ name: 'FleetPro_Lang', value: LANG, url: BASE });
    await anon.goto(BASE + '/Account/Login', { waitUntil: 'networkidle2' });
    await anon.evaluate(() => [...document.querySelectorAll('div')]
      .filter(d => d.textContent.includes('Demo Credentials') && d.querySelectorAll('div').length === 0)
      .forEach(d => d.remove()));
    if (await anon.evaluate(() => /password\s*[:\/]|@20\d\d!/i.test(document.body.innerText)))
      throw new Error('Login page still shows credentials — not capturing it');
    await anon.screenshot({ path: path.join(OUT, '01-login-full.png') });
    await sharp(path.join(OUT, '01-login-full.png'))
      .extract({ left: 480, top: 260, width: 1920, height: 1080 }).toFile(path.join(OUT, '01-login.png'));
    console.log('saved 01-login');

    // ── Company admin screens ──
    const p = await login(browser, TENANT);
    await shot(p, '02-dashboard', '/Dashboard', { wait: 2500 });
    await shot(p, '03-trucks', '/Truck');
    await shot(p, '04-truck-form', '/Truck/Create');
    await shot(p, '05-drivers', '/Driver');
    await shot(p, '06-agents', '/Agent');
    await shot(p, '07-trips', '/Trip');
    const edits = await hrefOf(p, 'a[href*="/Trip/Edit"]');
    const details = await hrefOf(p, 'a[href*="/Trip/Details"]');
    if (edits.length) {
      await shot(p, '08-trip-edit', edits[0], { wait: 1500 });
      // Oldest trip usually has payments recorded
      await shot(p, '09-trip-payments', edits[edits.length - 1], {
        before: pg => pg.evaluate(() => {
          const a = [...document.querySelectorAll('a.nav-link')].find(x => x.textContent.includes('Payments'));
          if (a) a.click();
        }),
      });
    }
    if (details.length) await shot(p, '10-trip-details', details[0]);
    await shot(p, '11-expenses', '/Expense');
    await shot(p, '12-expense-popup', '/Expense', {
      before: pg => pg.evaluate(() => typeof openAddExpenseModal === 'function' && openAddExpenseModal()),
      wait: 1500,
    });
    await shot(p, '13-alerts', '/Alert');
    await shot(p, '14-reports', '/Report');
    // P&L is a POST from the report form — submit it for the whole current year
    await p.goto(BASE + '/Report', { waitUntil: 'networkidle2' });
    const year = new Date().getFullYear();
    await p.$eval('form[action*="PL"] input[name=FromDate]', (e, y) => { e.value = `${y}-01-01`; }, year);
    await p.$eval('form[action*="PL"] input[name=ToDate]', (e, y) => { e.value = `${y}-12-31`; }, year);
    await Promise.all([p.waitForNavigation({ waitUntil: 'networkidle2' }), p.$eval('form[action*="PL"]', f => f.submit())]);
    await shot(p, '15-pl-report', null, { wait: 2000 });
    await shot(p, '16-users', '/User');
    const perms = await hrefOf(p, 'a[href*="/User/Permissions"]');
    if (perms.length) await shot(p, '17-user-permissions', perms[0]);
    await shot(p, '18-audit', '/Audit');

    // ── Hindi sample for the English deck (top part only — cards and menu) ──
    const h = await login(browser, TENANT, 'hi');
    await shot(h, '19-dashboard-hindi', '/Dashboard', { wait: 2500 });
    await sharp(path.join(OUT, '19-dashboard-hindi.png'))
      .extract({ left: 0, top: 0, width: 1440, height: 385 }).toFile(path.join(OUT, '19-dashboard-hindi-crop.png'));

    // ── Platform owner (SuperAdmin) ──
    const s = await login(browser, ADMIN);
    await shot(s, '20-super-dashboard', '/Dashboard', { wait: 2500 });
    await shot(s, '21-companies', '/Tenant');
    await shot(s, '22-roles', '/Role');
    await shot(s, '23-expense-categories', '/ExpenseCategory');
  } finally {
    await browser.close();
  }
})().catch(e => { console.error(e); process.exit(1); });
