import assert from "node:assert/strict";
import { createRequire } from "node:module";
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || "playwright");
if (!process.env.WEB_URL)
  throw new Error(
    "Set WEB_URL to the running web app using its configured CORS origin.",
  );
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
const name = `Frontend verification ${Date.now()}`;
const button = (name) => page.getByRole("button", { name, exact: true });
let id;
try {
  await page.goto(new URL("/riders/accounts", process.env.WEB_URL).href);
  await button("Makumbura operations").click();
  await button("Add passenger").click();
  await page.getByLabel("Full name", { exact: true }).fill(name);
  await page
    .getByLabel("Phone number", { exact: true })
    .fill(`TEST-${Date.now()}`);
  const created = page.waitForResponse(
    (r) =>
      r.url().endsWith("/api/v1/passengers") && r.request().method() === "POST",
  );
  await button("Save passenger").click();
  const response = await created;
  assert.equal(response.status(), 201);
  id = (await response.json()).id;
  await page.getByText("Passenger created.", { exact: true }).waitFor();
  await page.getByLabel("Top-up amount (LKR)").fill("1000");
  await button("Top up wallet").click();
  await page
    .getByText("Top-up recorded. Balance and transactions refreshed.", {
      exact: true,
    })
    .waitFor();
  await page.getByRole("cell", { name: "Top-up", exact: true }).waitFor();
  await button("Edit profile").click();
  await page
    .getByLabel("Category", { exact: true })
    .last()
    .selectOption("Student");
  await button("Save passenger").click();
  await page.getByText("Passenger updated.", { exact: true }).waitFor();
  // Reload requests the persisted detail. Demo login must be selected again.
  await page.reload();
  await button("Makumbura operations").click();
  await page.getByRole("heading", { name, exact: true }).waitFor();
  await page.getByRole("cell", { name: "Top-up", exact: true }).waitFor();
  await button("Deactivate account").click();
  await button("Confirm deactivation").click();
  await page.getByText("Passenger deactivated.", { exact: true }).waitFor();
  assert.equal(await button("Deactivate account").count(), 0);
  console.log(
    `PASS live API: create, profile, edit, wallet top-up/history, refresh persistence, deactivate. Retained inactive test passenger ${id}.`,
  );
} finally {
  await browser.close();
}
