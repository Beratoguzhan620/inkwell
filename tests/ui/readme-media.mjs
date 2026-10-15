// Regenerates the README images (docs/screenshots) from the demo content.
// Needs the site and the admin panel running with Seed:DemoContent=true. The walkthrough video is
// saved as output/media/walkthrough.webm; CI turns it into docs/screenshots/demo.gif with ffmpeg.
import { chromium } from "playwright";
import { mkdir, readdir, rename } from "node:fs/promises";

const baseUrl = process.env.BASE_URL ?? "http://localhost:5080";
const adminUrl = process.env.ADMIN_URL ?? "http://localhost:5081";
const out = new URL("./output/media/", import.meta.url).pathname;
const password = "Demo123!";
const postSlug = "kucuk-pull-requestler-neden-daha-hizli-birlesir";
const pause = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

await mkdir(out, { recursive: true });
const browser = await chromium.launch();

async function login(page, url, email) {
  await page.goto(url);
  await page.fill("#Email", email);
  await page.fill("#Password", password);
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
}

async function shot(page, name) {
  await page.waitForLoadState("networkidle").catch(() => {});
  await page.screenshot({ path: `${out}${name}.png` });
  console.log(`saved ${name}.png`);
}

// Still images, desktop.
{
  const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "tr-TR" });
  const page = await context.newPage();

  await page.goto(`${baseUrl}/`);
  await shot(page, "home");
  await page.goto(`${baseUrl}/posts/${postSlug}`);
  await shot(page, "post");

  await login(page, `${baseUrl}/Login`, "elif@example.com");
  await page.goto(`${baseUrl}/my-posts`);
  await shot(page, "my-posts");
  await page.goto(`${baseUrl}/write`);
  await page.waitForTimeout(500);
  await page.fill("#Title", "Kod incelemesinde sorulacak beş soru");
  await page.evaluate(() => {
    const text = "İyi bir inceleme hataları bulmaktan çok, ekibin aynı şeyi anladığından emin olmaktır.\n\n## Sorular\n\n1. Bu değişiklik **neden** gerekli?\n2. Hata durumunda kullanıcı ne görüyor?\n3. Geri almak kolay mı?";
    const cm = document.querySelector(".CodeMirror");
    if (cm && cm.CodeMirror) cm.CodeMirror.setValue(text);
  });
  await shot(page, "editor");

  await login(page, `${adminUrl}/login`, "admin@example.com");
  await shot(page, "admin-dashboard");
  await page.goto(`${adminUrl}/posts`);
  await shot(page, "admin-posts");

  await context.close();
}

// Still image, phone.
{
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true, locale: "tr-TR" });
  const page = await context.newPage();
  await page.goto(`${baseUrl}/posts/${postSlug}`);
  await shot(page, "mobile-post");
  await context.close();
}

// Walkthrough video for the README GIF.
{
  const context = await browser.newContext({
    viewport: { width: 1280, height: 800 },
    locale: "tr-TR",
    recordVideo: { dir: out, size: { width: 1280, height: 800 } },
  });
  const page = await context.newPage();

  await page.goto(`${baseUrl}/`);
  await pause(1600);
  await page.mouse.wheel(0, 600);
  await pause(1000);
  await page.goto(`${baseUrl}/posts/${postSlug}`);
  await pause(1600);
  await page.mouse.wheel(0, 700);
  await pause(1400);
  await page.mouse.wheel(0, 700);
  await pause(1200);

  await page.goto(`${baseUrl}/Login`);
  await page.type("#Email", "elif@example.com", { delay: 25 });
  await page.type("#Password", password, { delay: 25 });
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
  await page.goto(`${baseUrl}/write`);
  await pause(800);
  await page.type("#Title", "Kod incelemesinde sorulacak beş soru", { delay: 30 });
  await page.click(".CodeMirror");
  await page.keyboard.type("İyi bir inceleme, ekibin aynı şeyi anladığından emin olmaktır.", { delay: 15 });
  await pause(1400);

  await context.close();

  const video = (await readdir(out)).find((name) => name.endsWith(".webm"));
  if (video) await rename(`${out}${video}`, `${out}walkthrough.webm`);
}

await browser.close();
