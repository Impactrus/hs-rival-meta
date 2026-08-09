import db, { dbAll } from '../database.js';

async function checkTracking() {
  const rows = await dbAll("SELECT dbf_id, name, name_en, name_pl FROM cards WHERE name_en LIKE '%Tracking%' OR name_pl LIKE '%Tropien%'");
  console.log('Tracking rows in DB:', rows);
}

checkTracking();
