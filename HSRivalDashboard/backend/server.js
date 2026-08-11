import fs from 'fs';
import path from 'path';
import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import { decode } from 'deckstrings';

import { fileURLToPath } from 'url';

import db, { dbGet, dbAll, dbRun } from './database.js';
import { syncCards, getCardsByDbfIds } from './cardService.js';
import { scrapeLatestDecks } from './scraperService.js';
import { syncHdtDecks } from './hdtSyncService.js';

dotenv.config();

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = process.env.PORT || 5000;

app.use(cors());
app.use(express.json({ limit: '10mb' })); // support large collections

// In-memory cache for cards metadata to make calculations super fast
let globalCardMap = new Map();

async function loadCardsCache() {
  try {
    const cards = await dbAll('SELECT * FROM cards');
    globalCardMap.clear();
    cards.forEach(c => globalCardMap.set(c.dbf_id, c));
    console.log(`Loaded ${globalCardMap.size} cards metadata into memory cache.`);
  } catch (err) {
    console.error('Failed to load cards metadata cache:', err.message);
  }
}

const DUST_VALUES = {
  'LEGENDARY': 1600,
  'EPIC': 400,
  'RARE': 100,
  'COMMON': 40,
  'FREE': 0
};

// Helper: Calculate dust cost, missing count, owned count and attach cards for a list of decks (1-to-1 card matching pipeline)
function calculateDecksDust(decks, collectionMap) {
  // Pre-build name-based max owned map for aliases/reprints across card sets
  const nameToOwnedMap = new Map();
  globalCardMap.forEach((c, dbfId) => {
    let owned = collectionMap.get(dbfId) || collectionMap.get(Number(dbfId)) || 0;
    if (owned > 0 && c.name && c.type) {
      const normName = c.name.toLowerCase().trim();
      const key = `${normName}_${c.type}`;
      nameToOwnedMap.set(key, Math.max(nameToOwnedMap.get(key) || 0, owned));
    }
  });

  return decks.map(deck => {
    let decoded;
    try {
      decoded = decode(deck.deck_code);
    } catch (err) {
      return { ...deck, dustCost: 0, missingCount: 0, ownedCount: 30, totalCount: 30, cards: [] };
    }

    let dustCost = 0;
    let totalDust = 0;
    let missingCount = 0;
    let ownedCount = 0;
    let totalCount = 0;
    const cardsList = [];

    decoded.cards.forEach(pair => {
      const dbfId = Number(pair[0]);
      const required = Number(pair[1]);

      totalCount += required;

      const cardMeta = globalCardMap.get(dbfId);
      const isCollectible = cardMeta && (cardMeta.collectible === 1 || cardMeta.collectible === true || cardMeta.collectible === undefined);

      // Non-collectible tokens / modules (e.g. Broxigar parts, Zilliax modules, Garona tokens, ETC sideboards):
      // They are derived tokens included in deck string, so they add to totalCount and are auto-owned (0 dust, 0 missing).
      if (!isCollectible) {
        ownedCount += required;
        return;
      }

      let owned = collectionMap.get(dbfId) || collectionMap.get(Number(dbfId)) || 0;

      // Alias / Reprint check by normalized card name AND type
      if (cardMeta.name && cardMeta.type) {
        const normName = cardMeta.name.toLowerCase().trim();
        const key = `${normName}_${cardMeta.type}`;
        const byName = nameToOwnedMap.get(key) || 0;
        owned = Math.max(owned, byName);
      }

      const finalOwned = Math.min(required, owned);
      const missing = Math.max(0, required - finalOwned);
      
      ownedCount += finalOwned;
      missingCount += missing;

      const rarity = cardMeta.rarity || 'COMMON';
      const unitDust = DUST_VALUES[rarity] || 40;
      const cardDust = missing * unitDust;
      dustCost += cardDust;
      totalDust += required * unitDust;

      cardsList.push({
        id: cardMeta.id,
        dbf_id: dbfId,
        name: cardMeta.name_en || cardMeta.name,
        name_en: cardMeta.name_en || cardMeta.name,
        name_pl: cardMeta.name_pl || cardMeta.name,
        cost: cardMeta.cost,
        rarity: cardMeta.rarity,
        type: cardMeta.type,
        count: required,
        owned: finalOwned,
        missing: missing,
        isMissing: missing > 0,
        dustCost: cardDust
      });
    });

    // Sort cards by mana cost
    cardsList.sort((a, b) => a.cost - b.cost);

    return {
      ...deck,
      dustCost,
      totalDust,
      missingCount,
      ownedCount,
      totalCount,
      cards: cardsList
    };
  });
}

// 0. Track page visit (called once per session from frontend)
app.post('/api/stats/visit', async (req, res) => {
  try {
    // Ensure table exists
    await dbRun(`CREATE TABLE IF NOT EXISTS visits (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      visited_at TEXT NOT NULL DEFAULT (datetime('now')),
      ip TEXT
    )`);
    const ip = req.headers['x-forwarded-for']?.split(',')[0]?.trim() || req.socket.remoteAddress || 'unknown';
    await dbRun(`INSERT INTO visits (ip) VALUES (?)`, [ip]);
    res.json({ ok: true });
  } catch (err) {
    res.json({ ok: false });
  }
});

// 0b. Get visitor stats
app.get('/api/stats/visits', async (req, res) => {
  try {
    await dbRun(`CREATE TABLE IF NOT EXISTS visits (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      visited_at TEXT NOT NULL DEFAULT (datetime('now')),
      ip TEXT
    )`);
    const total = await dbGet(`SELECT COUNT(*) as count FROM visits`);
    const today = await dbGet(`SELECT COUNT(*) as count FROM visits WHERE date(visited_at) = date('now')`);
    res.json({ total: total.count, today: today.count });
  } catch (err) {
    res.json({ total: 0, today: 0 });
  }
});

// 1. Get all matches (uploaded by tracker)
app.get('/api/matches', async (req, res) => {
  try {
    const matches = await dbAll('SELECT * FROM matches ORDER BY date DESC');
    res.json(matches);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// 2. Post a new match (called by deck tracker)
app.post('/api/matches', async (req, res) => {
  const { 
    playerClass, opponentClass, result, rank, deckCode, 
    opponentDeck, startingCards, keptCards, playedCards 
  } = req.body;
  
  if (!playerClass || !opponentClass || !result) {
    return res.status(400).json({ error: 'playerClass, opponentClass, and result are required' });
  }

  const date = new Date().toISOString();

  // Convert card arrays to JSON strings
  const startingJson = startingCards ? JSON.stringify(startingCards) : null;
  const keptJson = keptCards ? JSON.stringify(keptCards) : null;
  const playedJson = playedCards ? JSON.stringify(playedCards) : null;

  try {
    const resultDb = await dbRun(`
      INSERT INTO matches (
        player_class, opponent_class, result, rank, date, deck_code, 
        opponent_deck, starting_cards, kept_cards, played_cards
      )
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `, [
      playerClass, opponentClass, result, rank || 'Bronze', date, 
      deckCode || null, opponentDeck || null, startingJson, keptJson, playedJson
    ]);

    res.status(201).json({ id: resultDb.id, message: 'Match recorded successfully' });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// 3. Get all high-rank decks (with computed dust cost based on collection)
app.get('/api/decks', async (req, res) => {
  const { playerClass, search, gameMode, ownedOnly, cardsInDecks, maxDust, craftableOnly } = req.query;
  const limit = Math.min(parseInt(req.query.limit, 10) || 20, 100);
  const offset = parseInt(req.query.offset, 10) || 0;

  // Normalize class name: handle "DemonHunter" -> "Demon Hunter", "DeathKnight" -> "Death Knight"
  const normalizeClass = (cls) => {
    if (!cls) return cls;
    const aliases = {
      'DemonHunter': 'Demon Hunter',
      'DeathKnight': 'Death Knight',
      'demonhunter': 'Demon Hunter',
      'deathknight': 'Death Knight',
    };
    return aliases[cls] || cls;
  };
  
  let sql = 'SELECT * FROM decks WHERE 1=1';
  const params = [];

  if (playerClass && playerClass !== 'All') {
    const normalizedClass = normalizeClass(playerClass);
    const altClass = normalizedClass.replace(/\s+/g, '');
    sql += ' AND (player_class = ? OR player_class = ? OR REPLACE(player_class, " ", "") = ?)';
    params.push(normalizedClass, altClass, altClass);
  }

  if (gameMode && gameMode !== 'All') {
    const targetFormat = (gameMode === 'Dziki' || gameMode === 'Wild' || gameMode?.toLowerCase() === 'wild' || gameMode?.toLowerCase() === 'dziki') ? 'Wild' : 'Standard';
    sql += ' AND format = ?';
    params.push(targetFormat);
  }

  if (search) {
    sql += ' AND title LIKE ?';
    params.push(`%${search}%`);
  }

  sql += ' ORDER BY date DESC';

  const parsedMaxDust = maxDust !== undefined && maxDust !== '' && !isNaN(parseInt(maxDust, 10)) ? parseInt(maxDust, 10) : null;
  const isFilteringOwned = ownedOnly === 'true' || cardsInDecks === 'Owned' || cardsInDecks === 'Craftable' || parsedMaxDust !== null || craftableOnly === 'true';

  try {
    // Load player collection (needed for dust calculations)
    const token = req.headers['x-user-token'] || req.query.token;
    let collectionRows = [];
    if (token) {
      try {
        collectionRows = await dbAll('SELECT dbf_id, count FROM user_collections WHERE token = ?', [token]);
      } catch { collectionRows = []; }
    }
    if (!collectionRows.length) {
      collectionRows = await dbAll('SELECT dbf_id, count FROM collection');
    }
    const collectionMap = new Map();
    collectionRows.forEach(r => collectionMap.set(r.dbf_id, r.count));

    if (isFilteringOwned) {
      // Need all decks for collection-based filtering — fetch all, filter, then paginate
      const decks = await dbAll(sql + ' LIMIT 1000', params);
      let decksWithDust = calculateDecksDust(decks, collectionMap);
      decksWithDust = decksWithDust.filter(d => d.totalCount >= 30);

      if (ownedOnly === 'true' || cardsInDecks === 'Owned') {
        decksWithDust = decksWithDust.filter(d => d.missingCount === 0);
      } else if (cardsInDecks === 'Craftable') {
        decksWithDust = decksWithDust.filter(d => d.missingCount > 0);
      }
      if (parsedMaxDust !== null) {
        decksWithDust = decksWithDust.filter(d => d.dustCost <= parsedMaxDust);
      }

      const total = decksWithDust.length;
      const page = decksWithDust.slice(offset, offset + limit);
      res.setHeader('X-Total-Count', total);
      return res.json(page);
    }

    // No collection filtering — efficient SQL pagination
    const countSql = sql.replace('SELECT *', 'SELECT COUNT(*) as total');
    const totalRow = await dbGet(countSql, params);
    const total = totalRow?.total || 0;

    const decks = await dbAll(sql + ` LIMIT ${limit} OFFSET ${offset}`, params);
    let decksWithDust = calculateDecksDust(decks, collectionMap);
    decksWithDust = decksWithDust.filter(d => d.totalCount >= 30);

    res.setHeader('X-Total-Count', total);
    res.json(decksWithDust);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});


// 4. Get specific deck details (decodes deck code + adds collection ownership info)
app.get('/api/decks/:id', async (req, res) => {
  const { id } = req.params;

  try {
    const deck = await dbGet('SELECT * FROM decks WHERE id = ?', [id]);
    if (!deck) {
      return res.status(404).json({ error: 'Deck not found' });
    }

    // Decode deck code to get list of card DBF IDs
    let decoded;
    try {
      decoded = decode(deck.deck_code);
    } catch (err) {
      return res.status(400).json({ error: 'Failed to decode deck string: ' + err.message });
    }

    // Load player collection
    const collectionRows = await dbAll('SELECT dbf_id, count FROM collection');
    const collectionMap = new Map();
    collectionRows.forEach(r => collectionMap.set(r.dbf_id, r.count));

    const dbfIds = decoded.cards.map(pair => pair[0]);
    const dbCards = await getCardsByDbfIds(dbfIds);

    const cardsList = decoded.cards.map(pair => {
      const dbfId = pair[0];
      const count = pair[1];
      const cardInfo = dbCards.find(c => c.dbf_id === dbfId) || {
        name: `Nieznana Karta (DBF: ${dbfId})`,
        cost: 0,
        rarity: 'UNKNOWN',
        card_class: 'NEUTRAL',
        type: 'UNKNOWN'
      };
      
      return {
        ...cardInfo,
        count,
        owned: collectionMap.get(dbfId) || 0
      };
    });

    // Sort cards: first by mana cost, then alphabetically by name
    cardsList.sort((a, b) => a.cost - b.cost || a.name.localeCompare(b.name));

    res.json({
      ...deck,
      format: decoded.format === 2 ? 'Standard' : 'Wild',
      cards: cardsList
    });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// 4.5. Automatically scan player collection from Hearthstone Deck Tracker PlayerDecks.xml + Core Set cards
app.post('/api/collection/scan', async (req, res) => {
  const appData = process.env.APPDATA;
  try {
    const dbCards = await dbAll('SELECT id, dbf_id, rarity FROM cards');
    const cardMap = new Map();
    const rarityMap = new Map();
    dbCards.forEach(c => {
      cardMap.set(c.id, c.dbf_id);
      rarityMap.set(c.dbf_id, c.rarity);
    });

    const collectionMap = new Map();

    // Scan PlayerDecks.xml if available in HDT
    if (appData) {
      const xmlPath = path.join(appData, 'HearthstoneDeckTracker', 'PlayerDecks.xml');
      if (fs.existsSync(xmlPath)) {
        const xml = fs.readFileSync(xmlPath, 'utf8');
        const cardRegex = /<Card>[\s\S]*?<Id>(.*?)<\/Id>[\s\S]*?<Count>(.*?)<\/Count>[\s\S]*?<\/Card>/g;
        let match;
        while ((match = cardRegex.exec(xml)) !== null) {
          const cardId = match[1];
          const count = parseInt(match[2], 10);
          const dbfId = cardMap.get(cardId);
          if (dbfId) {
            const rarity = rarityMap.get(dbfId);
            const maxOwned = rarity === 'LEGENDARY' ? 1 : 2;
            const current = collectionMap.get(dbfId) || 0;
            collectionMap.set(dbfId, Math.max(current, Math.min(count, maxOwned)));
          }
        }
      }
    }

    await dbRun('BEGIN TRANSACTION');
    await dbRun('DELETE FROM collection');
    const stmt = db.prepare('INSERT OR REPLACE INTO collection (dbf_id, count) VALUES (?, ?)');
    for (const [dbfId, qty] of collectionMap.entries()) {
      stmt.run(dbfId, qty);
    }
    stmt.finalize();
    await dbRun('COMMIT');

    res.json({
      success: true,
      count: collectionMap.size,
      message: `Pomyślnie skompletowano kolekcję (${collectionMap.size} kart) na podstawie HDT.`
    });
  } catch (err) {
    await dbRun('ROLLBACK');
    res.status(500).json({ error: err.message });
  }
});

// 5. Get player collection and dust (token-based for public users)
app.get('/api/collection', async (req, res) => {
  const token = req.headers['x-user-token'] || req.query.token;
  try {
    // Ensure per-user collection table exists
    await dbRun(`CREATE TABLE IF NOT EXISTS user_collections (
      token TEXT NOT NULL,
      dbf_id INTEGER NOT NULL,
      count INTEGER NOT NULL DEFAULT 1,
      PRIMARY KEY (token, dbf_id)
    )`);

    // Ensure user_settings table exists
    await dbRun(`CREATE TABLE IF NOT EXISTS user_settings (
      token TEXT NOT NULL,
      key TEXT NOT NULL,
      value TEXT NOT NULL,
      PRIMARY KEY (token, key)
    )`);

    const includeCards = req.query.cards === 'true' || req.query.includeCards === 'true';

    const enrichCollection = (rows) => {
      const collection = {};
      const cards = includeCards ? [] : null;
      rows.forEach(r => {
        collection[r.dbf_id] = r.count;
        if (includeCards) {
          const meta = globalCardMap.get(r.dbf_id) || {};
          cards.push({
            dbf_id: r.dbf_id,
            count: r.count,
            owned: r.count,
            id: meta.id || '',
            name: meta.name || meta.name_en || `Card ${r.dbf_id}`,
            name_en: meta.name_en || meta.name || `Card ${r.dbf_id}`,
            name_pl: meta.name_pl || meta.name_en || meta.name || `Karta ${r.dbf_id}`,
            cost: meta.cost !== undefined ? meta.cost : 0,
            rarity: meta.rarity || 'COMMON',
            card_class: meta.card_class || 'NEUTRAL',
            type: meta.type || 'MINION'
          });
        }
      });
      return { collection, cards };
    };

    if (token) {
      // Public user — return their token-scoped collection (with fallback to global collection)
      let rows = await dbAll('SELECT dbf_id, count FROM user_collections WHERE token = ?', [token]);
      if (!rows || rows.length === 0) {
        rows = await dbAll('SELECT dbf_id, count FROM collection');
      }
      const { collection, cards } = enrichCollection(rows);
      let dustRow = await dbGet("SELECT value FROM user_settings WHERE token = ? AND key = 'user_dust'", [token]);
      if (!dustRow) {
        dustRow = await dbGet("SELECT value FROM settings WHERE key = 'user_dust'");
      }
      const dust = dustRow ? parseInt(dustRow.value, 10) : 0;
      return res.json({ collection, cards, dust });
    }

    // Local use (no token) — return DB collection
    const rows = await dbAll('SELECT dbf_id, count FROM collection');
    const { collection, cards } = enrichCollection(rows);
    const dustRow = await dbGet("SELECT value FROM settings WHERE key = 'user_dust'");
    const dust = dustRow ? parseInt(dustRow.value, 10) : 0;
    res.json({ collection, cards, dust });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Update dust endpoint
app.post('/api/collection/dust', async (req, res) => {
  const { dust } = req.body;
  if (dust === undefined || isNaN(parseInt(dust, 10))) {
    return res.status(400).json({ error: 'Valid dust number is required' });
  }
  try {
    const val = parseInt(dust, 10);
    await dbRun("INSERT INTO settings (key, value) VALUES ('user_dust', ?) ON CONFLICT(key) DO UPDATE SET value = excluded.value", [val.toString()]);
    res.json({ success: true, dust: val });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// In-memory active plugin session tracking (token -> lastSeen timestamp)
const activePlugins = new Map();

app.post('/api/heartbeat', (req, res) => {
  const token = req.headers['x-user-token'] || req.body.token;
  if (token) {
    activePlugins.set(token, Date.now());
  }
  res.json({ success: true, timestamp: Date.now() });
});

app.get('/api/plugin-status', (req, res) => {
  const token = req.headers['x-user-token'] || req.query.token;

  // Check if token has active plugin OR if any plugin has reported in last 35 sec
  let connected = false;
  let lastSeen = null;
  if (token && activePlugins.has(token)) {
    lastSeen = activePlugins.get(token);
    connected = Date.now() - lastSeen < 35000;
  }
  if (!connected) {
    for (const [t, ts] of activePlugins.entries()) {
      if (Date.now() - ts < 35000) {
        connected = true;
        lastSeen = ts;
        break;
      }
    }
  }
  res.json({ connected, lastSeen });
});

// 6. Save player collection (called by manual upload or by C# tracker)
app.post('/api/collection', async (req, res) => {
  const token = req.headers['x-user-token'] || req.body.token;
  if (token) {
    activePlugins.set(token, Date.now());
  }
  const { collection, dust, isFullSync } = req.body;

  if (!collection) {
    return res.status(400).json({ error: 'Collection object is required' });
  }

  try {
    if (token) {
      // Store in user_collections scoped by token using pure additive accumulation (never delete existing cards on auto-sync)
      await dbRun(`CREATE TABLE IF NOT EXISTS user_collections (
        token TEXT NOT NULL,
        dbf_id INTEGER NOT NULL,
        count INTEGER NOT NULL DEFAULT 1,
        PRIMARY KEY (token, dbf_id)
      )`);
      if (req.body.forceReset === true) {
        await dbRun('DELETE FROM user_collections WHERE token = ?', [token]);
        await dbRun('DELETE FROM collection');
      }
      await dbRun('BEGIN TRANSACTION');
      const stmt = db.prepare(
        'INSERT INTO user_collections (token, dbf_id, count) VALUES (?, ?, ?) ON CONFLICT(token, dbf_id) DO UPDATE SET count = MAX(count, excluded.count)'
      );
      const stmtGlobal = db.prepare(
        'INSERT INTO collection (dbf_id, count) VALUES (?, ?) ON CONFLICT(dbf_id) DO UPDATE SET count = MAX(count, excluded.count)'
      );
      let count = 0;
      for (const [dbfIdStr, qty] of Object.entries(collection)) {
        const dbfId = parseInt(dbfIdStr, 10);
        const quantity = parseInt(qty, 10);
        if (!isNaN(dbfId) && !isNaN(quantity) && quantity > 0) {
          stmt.run(token, dbfId, quantity);
          stmtGlobal.run(dbfId, quantity);
          count++;
        }
      }
      stmt.finalize();
      stmtGlobal.finalize();

      if (dust !== undefined && !isNaN(parseInt(dust, 10)) && parseInt(dust, 10) > 0) {
        const val = parseInt(dust, 10);
        await dbRun(`CREATE TABLE IF NOT EXISTS user_settings (
          token TEXT NOT NULL,
          key TEXT NOT NULL,
          value TEXT NOT NULL,
          PRIMARY KEY (token, key)
        )`);
        await dbRun("INSERT INTO user_settings (token, key, value) VALUES (?, 'user_dust', ?) ON CONFLICT(token, key) DO UPDATE SET value = excluded.value", [token, val.toString()]);
        await dbRun("INSERT INTO settings (key, value) VALUES ('user_dust', ?) ON CONFLICT(key) DO UPDATE SET value = excluded.value", [val.toString()]);
      }
      await dbRun('COMMIT');
      return res.json({ success: true, message: `Zaktualizowano ${count} kart.` });
    }

    // Local use (no token) — save to shared collection table (additive accumulation)
    await dbRun('BEGIN TRANSACTION');
    if (req.body.forceReset === true) {
      await dbRun('DELETE FROM collection');
    }
    const stmt2 = db.prepare('INSERT INTO collection (dbf_id, count) VALUES (?, ?) ON CONFLICT(dbf_id) DO UPDATE SET count = MAX(count, excluded.count)');
    let count2 = 0;
    for (const [dbfIdStr, qty] of Object.entries(collection)) {
      const dbfId = parseInt(dbfIdStr, 10);
      const quantity = parseInt(qty, 10);
      if (!isNaN(dbfId) && !isNaN(quantity) && quantity > 0) {
        stmt2.run(dbfId, quantity);
        count2++;
      }
    }
    stmt2.finalize();
    if (dust !== undefined && !isNaN(parseInt(dust, 10))) {
      const val = parseInt(dust, 10);
      await dbRun("INSERT INTO settings (key, value) VALUES ('user_dust', ?) ON CONFLICT(key) DO UPDATE SET value = excluded.value", [val.toString()]);
    }
    await dbRun('COMMIT');
    const totalRows = await dbAll('SELECT count(*) as c FROM collection');
    res.json({ success: true, message: `Zaktualizowano ${count2} kart. Łącznie: ${totalRows[0].c} kart.` });
  } catch (error) {
    try { await dbRun('ROLLBACK'); } catch (e) {}
    res.status(500).json({ error: error.message });
  }
});

// 6b. Full collection reset (called only when user explicitly wants to start fresh)
app.post('/api/collection/reset', async (req, res) => {
  try {
    await dbRun('DELETE FROM collection');
    res.json({ success: true, message: 'Kolekcja wyczyszczona.' });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// 7. Get matchup statistics matrix
app.get('/api/stats/matchups', async (req, res) => {
  try {
    const matches = await dbAll('SELECT player_class, opponent_class, result FROM matches');
    
    // Matrix format: { playerClass: { opponentClass: { wins, losses, total } } }
    const matrix = {};
    
    matches.forEach(m => {
      const pClass = m.player_class;
      const oClass = m.opponent_class;
      const won = m.result?.toLowerCase() === 'won' || m.result?.toLowerCase() === 'wygrana';

      if (!matrix[pClass]) matrix[pClass] = {};
      if (!matrix[pClass][oClass]) matrix[pClass][oClass] = { wins: 0, losses: 0, total: 0 };

      matrix[pClass][oClass].total += 1;
      if (won) {
        matrix[pClass][oClass].wins += 1;
      } else {
        matrix[pClass][oClass].losses += 1;
      }
    });

    res.json(matrix);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// 8. Get mulligan & card statistics for a specific deck
app.get('/api/stats/mulligan/:deckCode', async (req, res) => {
  const { deckCode } = req.params;

  try {
    // Fetch all matches for this specific deck
    const matches = await dbAll(
      'SELECT result, starting_cards, kept_cards, played_cards FROM matches WHERE deck_code = ?', 
      [deckCode]
    );

    if (matches.length === 0) {
      return res.json({ matchesCount: 0, cardStats: {} });
    }

    // Structure to count stats per DBF ID: { dbfId: { mulliganCount, keptCount, keptWins, playedCount, playedWins } }
    const cardStats = {};

    matches.forEach(m => {
      const won = m.result?.toLowerCase() === 'won' || m.result?.toLowerCase() === 'wygrana';
      
      const starting = m.starting_cards ? JSON.parse(m.starting_cards) : [];
      const kept = m.kept_cards ? JSON.parse(m.kept_cards) : [];
      const played = m.played_cards ? JSON.parse(m.played_cards) : [];

      const initCard = (id) => {
        if (!cardStats[id]) {
          cardStats[id] = {
            mulliganCount: 0,
            keptCount: 0,
            keptWins: 0,
            playedCount: 0,
            playedWins: 0
          };
        }
      };

      // 1. Mulligan & kept stats
      starting.forEach(id => {
        initCard(id);
        cardStats[id].mulliganCount += 1;
      });

      kept.forEach(id => {
        initCard(id);
        cardStats[id].keptCount += 1;
        if (won) cardStats[id].keptWins += 1;
      });

      // 2. Played stats
      played.forEach(id => {
        initCard(id);
        cardStats[id].playedCount += 1;
        if (won) cardStats[id].playedWins += 1;
      });
    });

    res.json({
      matchesCount: matches.length,
      cardStats
    });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// 9. Force sync decks, cards and local HDT decks
app.post('/api/decks/sync', async (req, res) => {
  try {
    console.log('Manual sync triggered...');
    const cardCount = await syncCards();
    await loadCardsCache(); // refresh card cache in memory
    const localDecks = await syncHdtDecks();
    const newDecks = await scrapeLatestDecks();
    res.json({ 
      success: true, 
      message: `Zaktualizowano ${cardCount} kart, zsynchronizowano ${localDecks} lokalnych talii oraz pobrano ${newDecks} nowych talii z sieci.` 
    });
  } catch (error) {
    res.status(500).json({ success: false, error: error.message });
  }
});

// Route for downloading HDT Rival Tracker application
app.get('/api/download/tracker', (req, res) => {
  const zipPath = path.join(__dirname, 'public', 'downloads', 'HearthstoneDeckTracker.zip');
  if (fs.existsSync(zipPath)) {
    res.download(zipPath, 'HearthstoneDeckTracker.zip');
  } else {
    res.status(404).json({ error: 'Tracker package not found on server.' });
  }
});

// Route for downloading HDT Plugin (HSRivalPlugin.zip)
app.get('/api/download/plugin', (req, res) => {
  const pluginZipPath = path.join(__dirname, 'public', 'downloads', 'HSRivalPlugin.zip');
  if (fs.existsSync(pluginZipPath)) {
    res.download(pluginZipPath, 'HSRivalPlugin.zip');
  } else {
    res.status(404).json({ error: 'Plugin package not found on server.' });
  }
});

// Route for downloading 1-click HDT Plugin Installer executable (Install_HSRival_Plugin.exe)
app.get('/api/download/installer', (req, res) => {
  const installerPath = path.join(__dirname, 'public', 'downloads', 'Install_HSRival_Plugin.exe');
  if (fs.existsSync(installerPath)) {
    res.download(installerPath, 'Install_HSRival_Plugin.exe');
  } else {
    res.status(404).json({ error: 'Installer package not found on server.' });
  }
});

// HSReplay real meta stats sync endpoint
app.post('/api/meta/hsreplay-sync', express.json({ limit: '10mb' }), async (req, res) => {
  try {
    const data = req.body;
    console.log('Received HSReplay sync payload!');
    
    // Attempt to parse standard HSReplay response structure
    if (data?.series?.data) {
      const classesData = data.series.data;
      for (const [playerClass, decks] of Object.entries(classesData)) {
        if (Array.isArray(decks)) {
          for (let i = 0; i < Math.min(decks.length, 10); i++) {
            const d = decks[i];
            if (d.win_rate && d.total_games) {
              const wr = parseFloat(d.win_rate).toFixed(1);
              const games = parseInt(d.total_games);
              const duration = d.duration ? (d.duration / 60).toFixed(1) : 7.0;
              
              // Find a deck matching this class to update, or just insert a new mock record if we can't build a deckstring
              // In this MVP, we will just update existing decks for that class to distribute the real stats
              const existingDecks = await dbAll('SELECT id FROM decks WHERE player_class LIKE ? ORDER BY RANDOM() LIMIT 1', [`%${playerClass}%`]);
              if (existingDecks.length > 0) {
                await dbRun('UPDATE decks SET winrate = ?, games = ?, duration = ? WHERE id = ?', [wr, games, duration, existingDecks[0].id]);
              }
            }
          }
        }
      }
      console.log('Successfully updated decks with real HSReplay stats!');
    }
    res.json({ success: true });
  } catch (err) {
    console.error('Error parsing HSReplay payload:', err);
    res.status(500).json({ error: err.message });
  }
});

// Google Search Console verification endpoints
app.get('/googleYuR8TPJD6dTV0k4qd1GlbDy88YgReUxKMADK8DXQMjE.html', (req, res) => {
  res.send('google-site-verification: googleYuR8TPJD6dTV0k4qd1GlbDy88YgReUxKMADK8DXQMjE.html');
});

const frontendDistPath = path.join(__dirname, '..', 'frontend', 'dist');

// Serve index.html with guaranteed google-site-verification tag
app.get(['/', '/index.html'], (req, res) => {
  const htmlPath = path.join(frontendDistPath, 'index.html');
  if (fs.existsSync(htmlPath)) {
    let content = fs.readFileSync(htmlPath, 'utf8');
    if (!content.includes('google-site-verification')) {
      content = content.replace('<head>', '<head>\n    <meta name="google-site-verification" content="YuR8TPJD6dTV0k4qd1GlbDy88YgReUxKMADK8DXQMjE" />');
    }
    return res.send(content);
  }
  res.status(404).send('Frontend build not found');
});

// Serve frontend static build assets in production mode
if (fs.existsSync(frontendDistPath)) {
  app.use(express.static(frontendDistPath));
  app.get('*', (req, res, next) => {
    if (req.path.startsWith('/api')) {
      return next();
    }
    const htmlPath = path.join(frontendDistPath, 'index.html');
    if (fs.existsSync(htmlPath)) {
      let content = fs.readFileSync(htmlPath, 'utf8');
      if (!content.includes('google-site-verification')) {
        content = content.replace('<head>', '<head>\n    <meta name="google-site-verification" content="YuR8TPJD6dTV0k4qd1GlbDy88YgReUxKMADK8DXQMjE" />');
      }
      return res.send(content);
    }
    res.sendFile(htmlPath);
  });
}

// Start server
app.listen(PORT, async () => {
  console.log(`Server running on http://localhost:${PORT}`);
  
  // Initial sync & cache load
  setTimeout(async () => {
    try {
      const cardCount = await dbGet('SELECT COUNT(*) as count FROM cards');
      if (cardCount.count === 0) {
        console.log('Cards database is empty. Performing initial card sync...');
        await syncCards();
      }
      
      // Load card metadata cache into memory
      await loadCardsCache();

      // Parse and sync local decks from Hearthstone Deck Tracker's PlayerDecks.xml
      await syncHdtDecks();

      const deckCount = await dbGet('SELECT COUNT(*) as count FROM decks');
      if (deckCount.count === 0) {
        console.log('Decks database is empty. Scraping latest high-rank decks...');
        await scrapeLatestDecks();
      } else {
        console.log(`Found ${deckCount.count} decks in database.`);
      }
    } catch (err) {
      console.error('Error during startup sync:', err.message);
    }
  }, 1000);
});
