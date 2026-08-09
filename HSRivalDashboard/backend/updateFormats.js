import { decode } from 'deckstrings';
import sqlite3 from 'sqlite3';

const db = new sqlite3.Database('hsdashboard.db');

db.all('SELECT id, deck_code FROM decks', (err, rows) => {
  if (err) return console.error(err);
  let updated = 0;
  console.log(`Processing ${rows.length} decks...`);
  
  rows.forEach(row => {
    try {
      const decoded = decode(row.deck_code);
      const formatStr = decoded.format === 1 ? 'Wild' : (decoded.format === 2 ? 'Standard' : 'Other');
      
      db.run('UPDATE decks SET format = ? WHERE id = ?', [formatStr, row.id], (uErr) => {
        if (uErr) console.error(uErr);
        updated++;
        if (updated === rows.length) console.log('Finished updating formats');
      });
    } catch(e) {
      updated++;
      if (updated === rows.length) console.log('Finished updating formats');
    }
  });
});
