import sqlite3 from 'sqlite3';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const dbPath = path.join(__dirname, 'hsdashboard.db');
const db = new sqlite3.Database(dbPath, (err) => {
  if (err) {
    console.error('Failed to connect to SQLite database:', err.message);
  } else {
    console.log('Connected to SQLite database at:', dbPath);
    initTables();
  }
});

// Helper wrappers for Promise-based queries
export function dbRun(sql, params = []) {
  return new Promise((resolve, reject) => {
    db.run(sql, params, function (err) {
      if (err) reject(err);
      else resolve({ id: this.lastID, changes: this.changes });
    });
  });
}

export function dbGet(sql, params = []) {
  return new Promise((resolve, reject) => {
    db.get(sql, params, (err, row) => {
      if (err) reject(err);
      else resolve(row);
    });
  });
}

export function dbAll(sql, params = []) {
  return new Promise((resolve, reject) => {
    db.all(sql, params, (err, rows) => {
      if (err) reject(err);
      else resolve(rows);
    });
  });
}

async function initTables() {
  try {
    // Cards table (metadata)
    await dbRun(`
      CREATE TABLE IF NOT EXISTS cards (
        id TEXT PRIMARY KEY,
        dbf_id INTEGER UNIQUE,
        name TEXT NOT NULL,
        name_en TEXT,
        name_pl TEXT,
        cost INTEGER NOT NULL,
        rarity TEXT,
        card_class TEXT,
        type TEXT,
        collectible INTEGER DEFAULT 1
      )
    `);

    // Decks table (scraped high-rank decks)
    await dbRun(`
      CREATE TABLE IF NOT EXISTS decks (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        title TEXT NOT NULL,
        deck_code TEXT UNIQUE NOT NULL,
        player_class TEXT NOT NULL,
        format TEXT DEFAULT 'Standard',
        rank_desc TEXT,
        date TEXT,
        source_url TEXT,
        winrate REAL DEFAULT 0,
        games INTEGER DEFAULT 0,
        duration REAL DEFAULT 0
      )
    `);

    // Safely add columns if they don't exist (for existing databases)
    try { await dbRun("ALTER TABLE decks ADD COLUMN winrate REAL DEFAULT 0"); } catch { }
    try { await dbRun("ALTER TABLE decks ADD COLUMN games INTEGER DEFAULT 0"); } catch { }
    try { await dbRun("ALTER TABLE decks ADD COLUMN duration REAL DEFAULT 0"); } catch { }

    // Matches table (locally uploaded matches with JSON card columns for mulligan/played stats)
    await dbRun(`
      CREATE TABLE IF NOT EXISTS matches (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        player_class TEXT NOT NULL,
        opponent_class TEXT NOT NULL,
        result TEXT NOT NULL,
        rank TEXT,
        date TEXT NOT NULL,
        deck_code TEXT,
        opponent_deck TEXT,
        starting_cards TEXT,
        kept_cards TEXT,
        played_cards TEXT
      )
    `);

    // Collection table (stores user's collection: dbf_id -> quantity)
    await dbRun(`
      CREATE TABLE IF NOT EXISTS collection (
        dbf_id INTEGER PRIMARY KEY,
        count INTEGER NOT NULL
      )
    `);

    // Settings table (stores key-value settings e.g. user_dust)
    await dbRun(`
      CREATE TABLE IF NOT EXISTS settings (
        key TEXT PRIMARY KEY,
        value TEXT
      )
    `);

    // Helper migration for existing databases: check if matches table has the new columns
    db.all("PRAGMA table_info(matches)", async (err, columns) => {
      if (err) {
        console.error('Error reading table info:', err);
        return;
      }
      
      const columnNames = columns.map(c => c.name);
      
      if (!columnNames.includes('starting_cards')) {
        console.log("Migrating database: adding starting_cards to matches...");
        await dbRun("ALTER TABLE matches ADD COLUMN starting_cards TEXT");
      }
      if (!columnNames.includes('kept_cards')) {
        console.log("Migrating database: adding kept_cards to matches...");
        await dbRun("ALTER TABLE matches ADD COLUMN kept_cards TEXT");
      }
      if (!columnNames.includes('played_cards')) {
        console.log("Migrating database: adding played_cards to matches...");
        await dbRun("ALTER TABLE matches ADD COLUMN played_cards TEXT");
      }
    });

    db.all("PRAGMA table_info(cards)", async (err, columns) => {
      if (err) return;
      const columnNames = columns.map(c => c.name);
      if (!columnNames.includes('collectible')) {
        console.log("Migrating database: adding collectible to cards...");
        await dbRun("ALTER TABLE cards ADD COLUMN collectible INTEGER DEFAULT 1");
      }
      if (!columnNames.includes('name_en')) {
        console.log("Migrating database: adding name_en to cards...");
        await dbRun("ALTER TABLE cards ADD COLUMN name_en TEXT");
      }
      if (!columnNames.includes('name_pl')) {
        console.log("Migrating database: adding name_pl to cards...");
        await dbRun("ALTER TABLE cards ADD COLUMN name_pl TEXT");
      }
    });

    console.log('Database tables initialized successfully.');
  } catch (err) {
    console.error('Error initializing database tables:', err);
  }
}

export default db;
