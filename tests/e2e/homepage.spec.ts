// @ts-check
import { test, expect } from '@playwright/test';

// Use an environment variable for the base URL, with a fallback to a local URL for development.
// For a CI/CD pipeline, the environment variable can be set to the deployed URL.
const BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'https://localhost:44300';

test.describe('Basic Website Smoke Tests', () => {

  // Test to ensure the main page loads and has the correct title
  test('should load the homepage and verify the title', async ({ page }) => {
    // Navigate to the base URL
    await page.goto(BASE_URL);

    // Expect the page to have the specific title "The Exchange"
    await expect(page).toHaveTitle(/The Exchange/);
  });

  // Test to check for the presence of a key element on the page, like the main hero heading
  test('should have a main hero section with the correct heading', async ({ page }) => {
    // Navigate to the base URL
    await page.goto(BASE_URL);

    // Expect the hero heading on the homepage to be visible with the text "The Exchange"
    const heroHeading = page.getByRole('heading', { name: 'The Exchange' });
    await expect(heroHeading).toBeVisible();
  });

  // Test to ensure the "About" link is visible and navigates correctly
  test('should navigate to the about page', async ({ page }) => {
    // Navigate to the base URL
    await page.goto(BASE_URL);

    // Find the 'About' link in the navigation menu
    const aboutLink = page.getByRole('link', { name: 'About' });

    // Click the "About" link
    await aboutLink.click();

    // Expect the URL to contain the /about/ path after clicking
    await expect(page).toHaveURL(/.*about/);
  });
});
