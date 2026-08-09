import Database from 'better-sqlite3';
import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const srcPath = path.join(__dirname, 'hsdashboard.db');
const seedPath = path.join(__dirname, 'hsdashboard.seed.db');

if (fs.existsSync(seedPath)) fs.unlinkSync(seedPath);

const src = new Database(srcPath, { readonly: true });
const seed = new Database(seedPath);

// Copy only non-personal tables: decks, cards (NO collection, NO user data)
const tablesToCopy = ['decks', 'cards'];

for (const t of tablesToCopy) {
  try {
    const schema = src.prepare(`SELECT sql FROM sqlite_master WHERE type='table' AND name='${t}'`).get();
    if (schema && schema.sql) {
      seed.exec(schema.sql);
      const rows = src.prepare(`SELECT * FROM ${t}`).all();
      if (rows.length > 0) {
        const cols = Object.keys(rows[0]);
        const placeholders = cols.map(() => '?').join(', ');
        const ins = seed.prepare(`INSERT OR IGNORE INTO ${t} (${cols.join(', ')}) VALUES (${placeholders})`);
        const insMany = seed.transaction((rows) => {
          for (const r of rows) ins.run(Object.values(r));
        });
        insMany(rows);
        console.log(`${t}: ${rows.length} rows copied`);
      }
    }
  } catch (e) {
    console.log(`Skipped ${t}: ${e.message}`);
  }
}

src.close();
seed.close();
console.log('Seed DB created:', seedPath);
