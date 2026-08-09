import db from './database.js';

export async function syncCards() {
  console.log('Fetching card definitions from HearthstoneJSON (enUS & plPL)...');
  try {
    const [resEn, resPl] = await Promise.all([
      fetch('https://api.hearthstonejson.com/v1/latest/enUS/cards.json'),
      fetch('https://api.hearthstonejson.com/v1/latest/plPL/cards.json')
    ]);

    if (!resEn.ok || !resPl.ok) {
      throw new Error(`Failed to fetch HearthstoneJSON definitions.`);
    }

    const cardsEn = await resEn.json();
    const cardsPl = await resPl.json();

    const plMap = new Map();
    cardsPl.forEach(c => {
      if (c.dbfId) plMap.set(c.dbfId, c.name);
    });

    console.log(`Successfully fetched ${cardsEn.length} cards. Syncing to database...`);

    return new Promise((resolve, reject) => {
      db.serialize(() => {
        db.run('BEGIN TRANSACTION');

        const stmt = db.prepare(`
          INSERT OR REPLACE INTO cards (id, dbf_id, name, name_en, name_pl, cost, rarity, card_class, type, collectible)
          VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        `);

        let count = 0;
        for (const card of cardsEn) {
          const dbfId = card.dbfId;
          const cardClass = card.cardClass || 'NEUTRAL';
          const isCollectible = card.collectible ? 1 : 0;
          const nameEn = card.name || '';
          const namePl = plMap.get(dbfId) || nameEn;

          if (dbfId) {
            stmt.run(
              card.id,
              dbfId,
              nameEn, // default name is English
              nameEn,
              namePl,
              card.cost || 0,
              card.rarity || 'FREE',
              cardClass,
              card.type || 'UNKNOWN',
              isCollectible
            );
            count++;
          }
        }

        stmt.finalize((err) => {
          if (err) {
            db.run('ROLLBACK');
            console.error('Error during statement finalization:', err);
            reject(err);
            return;
          }

          db.run('COMMIT', (commitErr) => {
            if (commitErr) {
              console.error('Error committing transaction:', commitErr);
              reject(commitErr);
            } else {
              console.log(`Successfully synced ${count} cards to database.`);
              resolve(count);
            }
          });
        });
      });
    });
  } catch (error) {
    console.error('Error syncing cards:', error);
    throw error;
  }
}

// Helper to look up cards by their DBF IDs (from deckstrings)
export function getCardsByDbfIds(dbfIds) {
  if (!dbfIds || dbfIds.length === 0) return Promise.resolve([]);
  
  const placeholders = dbfIds.map(() => '?').join(',');
  const sql = `SELECT * FROM cards WHERE dbf_id IN (${placeholders})`;
  
  return new Promise((resolve, reject) => {
    db.all(sql, dbfIds, (err, rows) => {
      if (err) reject(err);
      else resolve(rows);
    });
  });
}
