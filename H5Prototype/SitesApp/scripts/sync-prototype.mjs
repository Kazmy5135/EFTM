import { copyFile, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const scriptRoot = dirname(fileURLToPath(import.meta.url));
const siteRoot = resolve(scriptRoot, '..');
const prototypeRoot = resolve(siteRoot, '..');
const publicRoot = resolve(siteRoot, 'public');

await mkdir(publicRoot, { recursive: true });
await Promise.all([
  copyFile(resolve(prototypeRoot, 'peek-app.js'), resolve(publicRoot, 'peek-app.js')),
  copyFile(resolve(prototypeRoot, 'styles.css'), resolve(publicRoot, 'peek-styles.css')),
  copyFile(resolve(prototypeRoot, 'inventory-app.js'), resolve(publicRoot, 'inventory-app.js')),
  copyFile(resolve(prototypeRoot, 'inventory-styles.css'), resolve(publicRoot, 'inventory-styles.css')),
]);

process.stdout.write('Synced combat and inventory H5 prototypes into the Sites deployment project.\n');
