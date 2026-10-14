// Visual smoke test: reads the demo posts, signs in as a demo author, writes a post with the
// Markdown editor and comments on it. Captures desktop and 360px-wide screenshots.
import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";

const baseUrl = process.env.BASE_URL ?? "http://localhost:5080";
const out = new URL("./output/", import.meta.url).pathname;
const password = "Demo123!";

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

async function login(page, email) {
  await page.goto(`${baseUrl}/Login`);
  await page.fill("#Email", email);
  await page.fill("#Password", password);
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
}

for (const [device, options] of Object.entries(viewports)) {
  const context = await browser.newContext({ ...options, locale: "tr-TR" });
  const page = await context.newPage();

  await page.goto(`${baseUrl}/`);
  await shot(page, `${device}-home`);

  await page.goto(`${baseUrl}/?q=sorgu`);
  await shot(page, `${device}-search`);

  await page.goto(`${baseUrl}/posts/kucuk-pull-requestler-neden-daha-hizli-birlesir`);
  await shot(page, `${device}-post`);

  await page.goto(`${baseUrl}/posts/yavas-sorguyu-bulmak-ef-core-ile-bir-haftalik-iz-surme`);
  await shot(page, `${device}-post-code`);

  await page.goto(`${baseUrl}/Register`);
  await shot(page, `${device}-register`);

  await login(page, device === "desktop" ? "elif@example.com" : "kerem@example.com");
  await page.goto(`${baseUrl}/my-posts`);
  await shot(page, `${device}-my-posts`);

  await page.goto(`${baseUrl}/write`);
  await page.waitForTimeout(500);
  await shot(page, `${device}-editor`);

  if (device === "desktop") {
    await page.click("button[value=publish]");
    await page.waitForLoadState("networkidle").catch(() => {});
    await shot(page, `${device}-editor-validation`);

    await page.fill("#Title", "Kod incelemesinde sorulacak beş soru");
    await page.selectOption("#CategoryId", { label: "Yazılım" });
    const body = [
      "İyi bir inceleme hataları bulmaktan çok, ekibin aynı şeyi anladığından emin olmaktır.",
      "",
      "## Sorular",
      "",
      "1. Bu değişiklik **neden** gerekli?",
      "2. Testler davranışı mı, uygulamayı mı doğruluyor?",
      "3. Hata durumunda kullanıcı ne görüyor?",
      "4. Altı ay sonra bu kodu okuyan biri neyi anlamaz?",
      "5. Geri almak kolay mı?",
      "",
      "> Soru sormak, onay vermekten daha değerlidir.",
    ].join("\n");
    await page.evaluate((text) => {
      const cm = document.querySelector(".CodeMirror");
      if (cm && cm.CodeMirror) cm.CodeMirror.setValue(text);
      document.getElementById("content").value = text;
    }, body);
    await shot(page, `${device}-editor-filled`);
    await Promise.all([page.waitForNavigation(), page.click("button[value=publish]")]);
    await shot(page, `${device}-published`);

    await page.fill("#commentContent", "Beşinci soru bizde hep unutuluyor, listeye ekledim.");
    await Promise.all([page.waitForNavigation(), page.click("#comments form button[type=submit]")]);
    await shot(page, `${device}-commented`);
  }

  await page.goto(`${baseUrl}/posts/does-not-exist`);
  await shot(page, `${device}-not-found`);

  await context.close();
}

// Accounts: password reset, the reader role and the admin panel.
const adminUrl = process.env.ADMIN_URL ?? "http://localhost:5081";
{
  const context = await browser.newContext({ ...viewports.desktop, locale: "tr-TR" });
  const page = await context.newPage();

  await page.goto(`${baseUrl}/forgot-password`);
  await page.fill("#Email", "zeynep@example.com");
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
  await shot(page, "desktop-forgot-password-sent");
  const resetLink = await page.getAttribute(".alert a[href*='reset-password']", "href");
  await page.goto(resetLink);
  await page.fill("#Password", "Yeni123!");
  await page.fill("#ConfirmPassword", "Yeni123!");
  await shot(page, "desktop-reset-password");
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
  await shot(page, "desktop-reset-done");

  await page.fill("#Email", "okur@example.com");
  await page.fill("#Password", password);
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
  await shot(page, "desktop-reader-home");
  await page.goto(`${baseUrl}/write`);
  await shot(page, "desktop-reader-cannot-write");
  await context.close();
}

for (const [device, options] of Object.entries(viewports)) {
  const context = await browser.newContext({ ...options, locale: "tr-TR" });
  const page = await context.newPage();

  await page.goto(`${adminUrl}/login`);
  await shot(page, `${device}-admin-login`);
  await page.fill("#Email", "admin@example.com");
  await page.fill("#Password", password);
  await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
  await shot(page, `${device}-admin-dashboard`);

  await page.goto(`${adminUrl}/posts`);
  await shot(page, `${device}-admin-posts`);
  await page.goto(`${adminUrl}/users`);
  await shot(page, `${device}-admin-users`);
  await page.goto(`${adminUrl}/comments`);
  await shot(page, `${device}-admin-comments`);
  await page.goto(`${adminUrl}/categories`);
  await shot(page, `${device}-admin-categories`);

  if (device === "desktop") {
    await page.goto(`${adminUrl}/posts?search=gökyüzünde`);
    await page.click("form[action$='/hide'] button");
    await page.waitForTimeout(400);
    await shot(page, `${device}-admin-confirm`);
    await Promise.all([page.waitForNavigation(), page.click("[data-confirm-accept]")]);
    await shot(page, `${device}-admin-post-hidden`);

    await page.goto(`${adminUrl}/users?search=okur`);
    await page.selectOption("select[name=roleId]", "2");
    await Promise.all([page.waitForNavigation(), page.click("form[action$='/role'] button")]);
    await shot(page, `${device}-admin-role-changed`);
  }

  await context.close();
}

await browser.close();

if (problems.length > 0) {
  console.error("Layout problems:\n" + problems.join("\n"));
  process.exit(1);
}
