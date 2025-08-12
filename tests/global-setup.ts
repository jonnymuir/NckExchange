// global-setup.ts
import { chromium, type FullConfig } from '@playwright/test';

// tests/global-setup.ts
// ...
import playwrightConfig from '../playwright.config';
const baseURL = playwrightConfig.use?.baseURL;

async function globalSetup(config: FullConfig) {
  console.log('Starting global setup...');
  
  // Set a longer timeout for this specific process
  const timeoutInMilliseconds = 300000; // 5 minues to more than allow for the application to start

  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  // Navigate to the base URL and wait for the page to be ready
  await page.goto(baseURL!, { timeout: timeoutInMilliseconds });

  await page.waitForSelector('body', { timeout: timeoutInMilliseconds });

  await browser.close();
  console.log('Global setup complete.');
}

export default globalSetup;