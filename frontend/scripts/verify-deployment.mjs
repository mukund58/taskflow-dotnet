#!/usr/bin/env node

const baseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(/\/+$/, "");
const apiPathPrefix = normalizeApiPathPrefix(process.env.NEXT_PUBLIC_API_PATH_PREFIX ?? "/api/v1");
const verifyOrigin = process.env.VERIFY_ORIGIN ?? "https://example.com";

function normalizeApiPathPrefix(pathPrefix) {
  const trimmed = pathPrefix.trim();
  if (!trimmed) {
    return "/api/v1";
  }

  const withLeadingSlash = trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
  return withLeadingSlash.replace(/\/+$/, "");
}

if (!baseUrl) {
  console.error("[verify:deployment] NEXT_PUBLIC_API_BASE_URL is required.");
  process.exit(1);
}

const checks = [];

async function runCheck(name, fn) {
  try {
    await fn();
    checks.push({ name, ok: true });
  } catch (error) {
    checks.push({ name, ok: false, message: error instanceof Error ? error.message : String(error) });
  }
}

await runCheck("Swagger is reachable", async () => {
  const response = await fetch(`${baseUrl}/swagger/index.html`, {
    method: "GET",
    headers: {
      Accept: "text/html,application/json;q=0.9,*/*;q=0.8",
    },
  });

  if (response.status >= 500) {
    throw new Error(`Swagger returned ${response.status}`);
  }
});

await runCheck("Tasks API is reachable", async () => {
  const response = await fetch(`${baseUrl}${apiPathPrefix}/tasks?page=1&pageSize=1`, {
    method: "GET",
    headers: {
      Accept: "application/json",
    },
  });

  if (![200, 401, 403].includes(response.status)) {
    throw new Error(`Unexpected status ${response.status} from GET ${apiPathPrefix}/tasks`);
  }
});

await runCheck("CORS preflight responds", async () => {
  const response = await fetch(`${baseUrl}${apiPathPrefix}/auth/login`, {
    method: "OPTIONS",
    headers: {
      Origin: verifyOrigin,
      "Access-Control-Request-Method": "POST",
      "Access-Control-Request-Headers": "authorization,content-type",
    },
  });

  const allowOrigin = response.headers.get("access-control-allow-origin");
  const allowMethods = response.headers.get("access-control-allow-methods");

  if (!response.ok) {
    throw new Error(`Preflight returned ${response.status}`);
  }

  if (!allowOrigin) {
    throw new Error("Missing access-control-allow-origin header");
  }

  const originAllowed = allowOrigin === "*" || allowOrigin === verifyOrigin;
  if (!originAllowed) {
    throw new Error(`Origin not allowed. access-control-allow-origin=${allowOrigin}`);
  }

  if (allowMethods && !allowMethods.toUpperCase().includes("POST")) {
    throw new Error(`POST not allowed. access-control-allow-methods=${allowMethods}`);
  }
});

const hasFailures = checks.some((check) => !check.ok);

for (const check of checks) {
  if (check.ok) {
    console.log(`[PASS] ${check.name}`);
  } else {
    console.error(`[FAIL] ${check.name}: ${check.message}`);
  }
}

if (hasFailures) {
  process.exit(1);
}

console.log("[verify:deployment] Deployment API and CORS checks passed.");
