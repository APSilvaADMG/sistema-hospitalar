import { chromium } from 'playwright';
import { pathToFileURL } from 'url';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const htmlPath = path.join(__dirname, '..', '..', 'Diversos', 'linkedin-banner-render.html');
const outPath = path.join(__dirname, '..', '..', 'Diversos', 'linkedin-banner-anderson-1584x396.png');

const browser = await chromium.launch();
const page = await browser.newPage({
  viewport: { width: 1584, height: 396 },
  deviceScaleFactor: 1,
});
await page.goto(pathToFileURL(htmlPath).href, { waitUntil: 'networkidle' });
await page.screenshot({
  path: outPath,
  type: 'png',
  clip: { x: 0, y: 0, width: 1584, height: 396 },
});
await browser.close();
console.log(`Saved ${outPath}`);
