import fs from 'fs';
import path from 'path';
import { execSync } from 'child_process';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const publishDir = path.resolve(__dirname, '..', '..', 'HearthstoneDeckTracker', 'publish');
const downloadsDir = path.join(__dirname, 'public', 'downloads');
const zipPath = path.join(downloadsDir, 'HearthstoneDeckTracker.zip');
const tempStage = path.join(__dirname, 'temp_stage');

if (!fs.existsSync(downloadsDir)) {
  fs.mkdirSync(downloadsDir, { recursive: true });
}

if (!fs.existsSync(publishDir)) {
  console.log('Publish dir not found (running on remote server). Using committed tracker zip.');
  process.exit(0);
}

try {
  if (fs.existsSync(tempStage)) {
    fs.rmSync(tempStage, { recursive: true, force: true });
  }
  fs.mkdirSync(tempStage, { recursive: true });

  const files = fs.readdirSync(publishDir);
  for (const file of files) {
    if (file === 'cache') continue;
    const src = path.join(publishDir, file);
    const dest = path.join(tempStage, file);
    if (fs.statSync(src).isFile()) {
      fs.copyFileSync(src, dest);
    }
  }

  if (fs.existsSync(zipPath)) {
    fs.unlinkSync(zipPath);
  }

  const isWindows = process.platform === 'win32';
  if (isWindows) {
    const psCmd = `powershell -Command "Compress-Archive -Path '${tempStage}\\*' -DestinationPath '${zipPath}' -Force"`;
    execSync(psCmd, { stdio: 'inherit' });
  } else {
    const zipCmd = `zip -j "${zipPath}" "${tempStage}"/*`;
    execSync(zipCmd, { stdio: 'inherit' });
  }

  fs.rmSync(tempStage, { recursive: true, force: true });
  console.log(`Successfully packaged tracker to: ${zipPath}`);
} catch (err) {
  console.warn('Could not package tracker on this environment:', err.message);
}
