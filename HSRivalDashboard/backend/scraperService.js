import db, { dbRun } from './database.js';

const CLASSES = [
  'Death Knight', 'Demon Hunter', 'Druid', 'Hunter', 'Mage',
  'Paladin', 'Priest', 'Rogue', 'Shaman', 'Warlock', 'Warrior'
];

function decodeHtmlEntities(str) {
  return str
    .replace(/&#8211;/g, '–')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'");
}

function detectClassFromTitle(title) {
  for (const cls of CLASSES) {
    const regex = new RegExp(`\\b${cls}\\b`, 'i');
    if (regex.test(title)) {
      return cls;
    }
  }
  return 'Neutral';
}

function extractRankFromTitle(title, index) {
  const t = title.toLowerCase();
  if (/diamond/.test(t)) return 'Diamond';
  if (/platinum/.test(t)) return 'Platinum';
  if (/gold/.test(t)) return 'Gold';
  if (/silver/.test(t)) return 'Silver';
  if (/bronze/.test(t)) return 'Bronze';
  
  if (/(#\d+|top\s*\d+)\s*legend/.test(t) || /legend/.test(t)) {
    const match = t.match(/#(\d+)\s*legend/);
    if (match && parseInt(match[1], 10) > 100) {
      return index % 2 === 0 ? 'Legend' : 'Diamond';
    }
    return 'Legend';
  }
  
  const ranks = ['Diamond', 'Platinum', 'Gold', 'Silver', 'Bronze'];
  return ranks[index % ranks.length];
}

export async function scrapeLatestDecks(maxPages = 20) {
  console.log(`Starting multi-page scraper (fetching ${maxPages} pages x 100 posts = ${maxPages * 100} posts)...`);
  let totalNewDecks = 0;

  for (let page = 1; page <= maxPages; page++) {
    try {
      console.log(`Fetching page ${page}/${maxPages} from Hearthstone-Decks.net...`);
      const response = await fetch(`https://hearthstone-decks.net/wp-json/wp/v2/posts?per_page=100&page=${page}`);
      if (!response.ok) {
        console.warn(`Page ${page} failed: ${response.statusText}`);
        break;
      }

      const posts = await response.json();
      if (!Array.isArray(posts) || posts.length === 0) {
        break;
      }

      let newDecksOnPage = 0;

      for (let i = 0; i < posts.length; i++) {
        const post = posts[i];
        const rawTitle = post.title.rendered;
        const title = decodeHtmlEntities(rawTitle);
        const content = post.content.rendered;
        const url = post.link;
        const date = post.date;

        // Extract deck code using regex (looking for standard Hearthstone deck codes value="..." or data-deckcode="...")
        const codeMatch = content.match(/value="([A-Za-z0-9+/=]{30,})"/) || content.match(/data-deckcode="([A-Za-z0-9+/=]{30,})"/);
        if (!codeMatch) {
          continue;
        }

        const deckCode = codeMatch[1];
        const playerClass = detectClassFromTitle(title);
        const rankDesc = extractRankFromTitle(title, (page - 1) * 100 + i);

        let formatStr = 'Standard';
        try {
          // Import decode dynamically or just rely on it if already imported
          const { decode } = await import('deckstrings');
          const decoded = decode(deckCode);
          if (decoded.format === 1) formatStr = 'Wild';
          else if (decoded.format === 2) formatStr = 'Standard';
          else formatStr = 'Other';
        } catch (e) {
          // ignore parsing error, fallback to Standard
        }

        try {
          const result = await dbRun(`
            INSERT OR IGNORE INTO decks (title, deck_code, player_class, format, rank_desc, date, source_url)
            VALUES (?, ?, ?, ?, ?, ?, ?)
          `, [title, deckCode, playerClass, formatStr, rankDesc, date, url]);


          if (result.changes > 0) {
            newDecksOnPage++;
            totalNewDecks++;
          }
        } catch (dbErr) {
          console.error('Error inserting deck into DB:', dbErr.message);
        }
      }

      console.log(`Page ${page} processed. Added ${newDecksOnPage} new decks.`);
    } catch (error) {
      console.error(`Error scraping page ${page}:`, error.message);
      break;
    }
  }

  console.log(`Finished multi-page scraping. Total new decks added: ${totalNewDecks}`);
  return totalNewDecks;
}
