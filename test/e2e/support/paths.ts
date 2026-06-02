import path from 'node:path';
import { fileURLToPath } from 'node:url';

const thisFile = fileURLToPath(import.meta.url);
export const e2eRoot = path.resolve(path.dirname(thisFile), '..');
export const repoRoot = path.resolve(e2eRoot, '..', '..');
export const artifactsDir = path.join(e2eRoot, '.artifacts');
export const apphostPath = path.join(repoRoot, 'src', 'BarTabTracker.AppHost', 'BarTabTracker.AppHost.csproj');
export const baseUrlPath = path.join(artifactsDir, 'base-url.txt');
export const pidPath = path.join(artifactsDir, 'aspire.pid');
export const aspireLogPath = path.join(artifactsDir, 'aspire-run.log');
