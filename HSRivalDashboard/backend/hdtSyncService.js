import fs from 'fs';
import path from 'path';
import { encode } from 'deckstrings';
import db, { dbRun, dbAll } from './database.js';

const HERO_DBF_IDS = {
  'Warrior': 7,
  'Hunter': 31,
  'Mage': 637,
  'Paladin': 671,
  'Priest': 813,
  'Warlock': 893,
  'Rogue': 930,
  'Shaman': 1066,
  'Druid': 274,
  'Demon Hunter': 56550,
  'Death Knight': 78065
};

export async function syncHdtDecks() {
  console.log('Starting Hearthstone Deck Tracker PlayerDecks.xml sync...');
  
  const appData = process.env.APPDATA;
  if (!appData) {
    console.error('APPDATA environment variable not found. Cannot locate HDT folder.');
    return 0;
  }

  const xmlPath = path.join(appData, 'HearthstoneDeckTracker', 'PlayerDecks.xml');
  if (!fs.existsSync(xmlPath)) {
    console.log(`PlayerDecks.xml not found at: ${xmlPath}. Skipping local decks sync.`);
    return 0;
  }

  try {
    const xml = fs.readFileSync(xmlPath, 'utf8');
    const deckRegex = /<Deck>([\s\S]*?)<\/Deck>/g;
    let match;
    let importedCount = 0;

    // Load card mapping (id -> dbf_id) from cards database to avoid querying sqlite in loop
    const dbCards = await dbAll('SELECT id, dbf_id FROM cards');
    const cardMap = new Map();
    dbCards.forEach(c => cardMap.set(c.id, c.dbf_id));

    while ((match = deckRegex.exec(xml)) !== null) {
      const deckXml = match[1];
      
      const nameMatch = deckXml.match(/<Name>(.*?)<\/Name>/);
      const classMatch = deckXml.match(/<Class>(.*?)<\/Class>/);
      
      if (!nameMatch || !classMatch) continue;

      const name = nameMatch[1];
      const playerClass = classMatch[1];
      
      // Parse cards inside <Cards> ... </Cards>
      const cardsMatch = deckXml.match(/<Cards>([\s\S]*?)<\/Cards>/);
      if (!cardsMatch) continue;

      const cards = [];
      const cardRegex = /<Card>[\s\S]*?<Id>(.*?)<\/Id>[\s\S]*?<Count>(.*?)<\/Count>[\s\S]*?<\/Card>/g;
      let cardMatch;
      while ((cardMatch = cardRegex.exec(cardsMatch[1])) !== null) {
        const cardId = cardMatch[1];
        const count = parseInt(cardMatch[2], 10);
        const dbfId = cardMap.get(cardId);
        if (dbfId) {
          cards.push([dbfId, count]);
        }
      }

      if (cards.length === 0) continue;

      // Sort cards by DBF ID (deckstrings encoder requires sorted cards for consistency)
      cards.sort((a, b) => a[0] - b[0]);

      // Generate deckstring
      const heroDbf = HERO_DBF_IDS[playerClass] || 7; // fallback to Warrior hero
      const deckObject = {
        cards: cards,
        heroes: [heroDbf],
        format: 2 // 2 for Standard
      };

      let deckCode;
      try {
        deckCode = encode(deckObject);
      } catch (encodeErr) {
        console.error(`Failed to encode deckstring for local deck: ${name}`, encodeErr.message);
        continue;
      }

      const dateMatch = deckXml.match(/<LastEdited>(.*?)<\/LastEdited>/);
      const date = dateMatch ? dateMatch[1] : new Date().toISOString();
      const title = `${name} [Lokalna]`;

      try {
        // Insert or replace local deck in decks table
        const result = await dbRun(`
          INSERT INTO decks (title, deck_code, player_class, rank_desc, date, source_url)
          VALUES (?, ?, ?, ?, ?, 'local')
          ON CONFLICT(deck_code) DO UPDATE SET
            title = excluded.title,
            date = excluded.date
        `, [title, deckCode, playerClass, 'Mój Deck', date]);

        if (result.changes > 0) {
          importedCount++;
        }
      } catch (dbErr) {
        console.error('Error inserting local deck into DB:', dbErr.message);
      }
    }

    console.log(`Synced local HDT decks. Added/Updated ${importedCount} decks.`);
    return importedCount;
  } catch (err) {
    console.error('Error reading/parsing PlayerDecks.xml:', err.message);
    return 0;
  }
}
