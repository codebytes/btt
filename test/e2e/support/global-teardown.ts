import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import { apphostPath, pidPath, repoRoot } from './paths';

export default async function globalTeardown() {
  if (!fs.existsSync(pidPath)) {
    return;
  }

  try {
    execFileSync('aspire', ['stop', '--apphost', apphostPath, '--non-interactive', '--nologo'], {
      cwd: repoRoot,
      stdio: 'ignore',
    });
  } catch {
    const pid = Number(fs.readFileSync(pidPath, 'utf8'));
    if (Number.isInteger(pid) && pid > 0) {
      try {
        process.kill(-pid, 'SIGTERM');
      } catch {
        // Process already stopped.
      }
    }
  } finally {
    fs.rmSync(pidPath, { force: true });
  }
}
