import { execFileSync, spawn } from 'node:child_process';
import fs from 'node:fs';
import { artifactsDir, apphostPath, aspireLogPath, baseUrlPath, pidPath, repoRoot } from './paths';

type AspireDescribe = {
  resources?: Array<{
    displayName?: string;
    urls?: Array<{ name?: string; url?: string; isInternal?: boolean }>;
  }>;
};

function runAspire(args: string[]) {
  return execFileSync('aspire', [...args, '--apphost', apphostPath, '--non-interactive', '--nologo'], {
    cwd: repoRoot,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
}

function runAspireGlobal(args: string[]) {
  return execFileSync('aspire', [...args, '--non-interactive', '--nologo'], {
    cwd: repoRoot,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
}

async function waitForAppHostRegistration() {
  const deadline = Date.now() + 60_000;

  while (Date.now() < deadline) {
    try {
      const running = JSON.parse(runAspireGlobal(['ps', '--format', 'Json'])) as Array<{ appHostPath?: string; status?: string }>;
      if (running.some((app) => app.appHostPath === apphostPath && app.status === 'running')) {
        return;
      }
    } catch {
      // Aspire may not have registered the just-spawned AppHost yet.
    }

    await new Promise((resolve) => setTimeout(resolve, 1_000));
  }

  throw new Error('Timed out waiting for Aspire AppHost registration.');
}

function discoverWebFrontendUrl() {
  const raw = runAspire(['describe', '--format', 'Json']);
  const description = JSON.parse(raw) as AspireDescribe;
  const webfrontend = description.resources?.find((resource) => resource.displayName === 'webfrontend');
  const localhost = webfrontend?.urls?.find((url) => url.url?.startsWith('http://localhost'))?.url;
  const external = webfrontend?.urls?.find((url) => url.name === 'http' && url.url?.startsWith('http'))?.url;
  const baseUrl = localhost ?? external;

  if (!baseUrl) {
    throw new Error('Could not find a webfrontend HTTP URL in Aspire resource description.');
  }

  return baseUrl;
}

function tailAspireLog() {
  if (!fs.existsSync(aspireLogPath)) {
    return '';
  }

  return fs.readFileSync(aspireLogPath, 'utf8').split('\n').slice(-80).join('\n');
}

export default async function globalSetup() {
  fs.mkdirSync(artifactsDir, { recursive: true });
  fs.rmSync(baseUrlPath, { force: true });
  fs.rmSync(pidPath, { force: true });

  if (process.env.E2E_BASE_URL) {
    fs.writeFileSync(baseUrlPath, process.env.E2E_BASE_URL);
    return;
  }

  try {
    runAspire(['stop']);
  } catch {
    // No existing AppHost to stop.
  }

  const log = fs.openSync(aspireLogPath, 'w');
  const child = spawn('aspire', ['run', '--apphost', apphostPath, '--non-interactive', '--nologo'], {
    cwd: repoRoot,
    detached: true,
    stdio: ['ignore', log, log],
  });

  fs.writeFileSync(pidPath, String(child.pid));
  child.unref();

  try {
    await waitForAppHostRegistration();
    runAspire(['wait', 'server', '--status', 'healthy', '--timeout', '180']);
    runAspire(['wait', 'webfrontend', '--status', 'up', '--timeout', '180']);
    const baseUrl = discoverWebFrontendUrl();
    fs.writeFileSync(baseUrlPath, baseUrl);
  } catch (error) {
    throw new Error(`Aspire E2E startup failed: ${String(error)}\n\nAspire log tail:\n${tailAspireLog()}`);
  }
}
