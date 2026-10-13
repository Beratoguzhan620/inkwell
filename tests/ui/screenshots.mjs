// Visual smoke test: registers a writer, verifies the e-mail with the development code,
// and captures the main screens on desktop and a 360px phone.
import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";

const baseUrl = process.env.BASE_URL ?? "http://localhost:5080";
const out = new URL("./output/", import.meta.url).pathname;

const viewports = {
  desktop: { viewport: { width: 1440, height: 900 } },
  mobile: { viewport: { width: 360, height: 780 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true },
};

await mkdir(out, { recursive: true });
const browser = await chromium.launch();
const problems = [];

async function shot(page, name) {
  await page.waitForLoadState("networkidle").catch(() => {});
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  if (overflow > 1) problems.push(`${name}: page is ${overflow}px wider than the viewport`);
  await page.screenshot({ path: `${out}${name}.png`, fullPage: true });
  console.log(`saved ${name}.png`);
}

for (const [device, options] of Object.entries(viewports)) {
  const context = await browser.newContext({ ...options, locale: "tr-TR" });
  const page = await context.newPage();
  const email = `${device}.yazar@example.com`;
  const password = "Demo123!";

  await page.goto(`${baseUrl}/`);
  await shot(page, `${device}-home`);

  await page.goto(`${baseUrl}/Login`);
  await page.click("form button[type=submit]");
  await shot(page, `${device}-login-validation`);

  await page.goto(`${baseUrl}/Register`);
  await shot(page, `${device}-register`);
  await page.fill("#FirstName", "Defne");
  await page.fill("#LastName", device === "desktop" ? "Aydın" : "Kara");
  await page.fill("#Email", email);
  await page.fill("#Password", password);
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
  await shot(page, `${device}-verify`);

  const code = (await page.textContent(".alert strong.font-mono")).trim();
  await page.fill("#Code", code);
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
  await shot(page, `${device}-verified`);

  await page.goto(`${baseUrl}/Login`);
  await page.fill("#Email", email);
  await page.fill("#Password", password);
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
  await shot(page, `${device}-home-signed-in`);

  await page.goto(`${baseUrl}/BlogPost/Create`);
  await shot(page, `${device}-create`);

  await page.goto(`${baseUrl}/does-not-exist`);
  await shot(page, `${device}-not-found`);

  await context.close();
}

await browser.close();

if (problems.length > 0) {
  console.error("Layout problems:\n" + problems.join("\n"));
  process.exit(1);
}
