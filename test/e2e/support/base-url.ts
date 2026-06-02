import fs from 'node:fs';
import { baseUrlPath } from './paths';

export function getBaseUrl() {
  return fs.readFileSync(baseUrlPath, 'utf8').trim().replace(/\/$/, '');
}
