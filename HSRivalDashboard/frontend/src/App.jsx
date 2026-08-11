import React, { useState, useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';

const API_URL = import.meta.env.VITE_API_URL || '/api';

const CLASS_COLORS = {
  'Death Knight': 'var(--color-death-knight)',
  'Demon Hunter': 'var(--color-demon-hunter)',
  'Druid': 'var(--color-druid)',
  'Hunter': 'var(--color-hunter)',
  'Mage': 'var(--color-mage)',
  'Paladin': 'var(--color-paladin)',
  'Priest': 'var(--color-priest)',
  'Rogue': 'var(--color-rogue)',
  'Shaman': 'var(--color-shaman)',
  'Warlock': 'var(--color-warlock)',
  'Warrior': 'var(--color-warrior)',
  'Neutral': 'var(--color-neutral)'
};

const CLASS_NAMES = {
  en: {
    'All': 'All Classes',
    'Death Knight': 'Death Knight',
    'Demon Hunter': 'Demon Hunter',
    'Druid': 'Druid',
    'Hunter': 'Hunter',
    'Mage': 'Mage',
    'Paladin': 'Paladin',
    'Priest': 'Priest',
    'Rogue': 'Rogue',
    'Shaman': 'Shaman',
    'Warlock': 'Warlock',
    'Warrior': 'Warrior',
    'Neutral': 'Neutral'
  },
  pl: {
    'All': 'Wszystkie klasy',
    'Death Knight': 'Rycerz Śmierci',
    'Demon Hunter': 'Łowca Demonów',
    'Druid': 'Druid',
    'Hunter': 'Łowca',
    'Mage': 'Mag',
    'Paladin': 'Paladyn',
    'Priest': 'Kapłan',
    'Rogue': 'Łotr',
    'Shaman': 'Szaman',
    'Warlock': 'Czarnoksiężnik',
    'Warrior': 'Wojownik',
    'Neutral': 'Neutralne'
  }
};

const I18N = {
  en: {
    siteTitle: 'HS Rival Meta & Deck Tracker - Best Hearthstone Decks',
    siteDesc: 'Discover the top meta decks in Hearthstone! Real-time winrates, automatic collection dust calculator, and mulligan guides.',
    decksTab: 'Decks',
    matchesTab: 'My Matches',
    collectionTab: 'Collection',
    scanCollection: '🔍 Scan Collection (HDT)',
    syncDecks: 'Sync HDT Decks',
    updating: 'Updating...',
    downloadTracker: '📥 Download Tracker (HDT)',
    buyCoffee: '☕ Buy Me a Coffee',
    clearFilters: 'Clear Filters',
    playerClass: 'Player Class',
    opponentClass: 'Opponent Class',
    gameMode: 'Game Mode',
    standardRanked: 'Ranked Standard',
    wildRanked: 'Ranked Wild',
    rankRange: 'Rank Range',
    myCollectionDust: 'My Collection & Dust',
    limitToCollection: '100% Owned Decks Only',
    yourDust: '✨ Your Dust (Arcane Dust):',
    fromHDT: 'from HDT',
    typeDustPlaceholder: 'Type dust amount...',
    fromGame: 'From game',
    dustFilterCheckbox: 'Decks in my budget',
    allClasses: 'All Classes',
    allRanks: 'All',
    legend: 'Legend',
    diamond: 'Diamond',
    platinum: 'Platinum',
    gold: 'Gold',
    silver: 'Silver',
    bronze: 'Bronze',
    localDecks: 'My Decks (HDT)',
    allCards: 'All Decks',
    ownedCards: 'Owned Only (100%)',
    craftableCards: 'Craftable',
    searchPlaceholder: 'Search deck by title...',
    allCount: 'All Decks',
    ownAllCards: '✓ You own all',
    youOwn: 'You own',
    ofCards: 'cards',
    missing: 'Missing',
    dust: 'dust',
    craftableWithDust: '✨ Craftable with dust!',
    winrate: 'Winrate',
    games: 'Games',
    avgDuration: 'Avg Duration',
    copyDeck: 'Copy Deck',
    copied: '✓ Copied',
    manaCurve: '📊 Mana Curve (Card Count by Cost)',
    mulliganGuide: '🃏 Mulligan Guide (Winrate when kept)',
    cardName: 'Card Name',
    inHandWinrate: 'In-Hand Winrate',
    keptWinrate: 'Kept Winrate',
    keepRate: 'Keep %',
    myMatches: 'My Played Matches History',
    matchupMatrix: 'Class Winrate Matchup Matrix',
    date: 'Date',
    result: 'Result',
    rank: 'Rank',
    win: 'WIN',
    loss: 'LOSS',
    collectionManager: 'Automatic Collection Scanner & Manager',
    scanHDT: '🔍 Scan from HDT (Base + HDT)',
    markAll100: '✓ Mark 100% Cards Owned',
    manualJSON: 'Manual JSON Collection Import',
    saveCollection: 'Save Collection',
    promoTitle: '🚀 HDT Rival Tracker',
    promoDesc: 'Download our dedicated HDT app to auto-scan your collection & record live matches!',
    promoDownload: '📥 Download HDT Tracker (.zip)',
    promoCoffee: '☕ Buy Me a Coffee!',
    widgetTitle: 'Enjoying the project?',
    widgetDesc: 'Support the development of HS Rival Meta and the deck scanner by buying a coffee!',
    widgetCTA: 'Buy Me a Coffee (buymeacoffee.com/impacter)',
    downloadPlugin: '🔌 HDT Plugin (.dll)',
    downloadInstaller: '⚡ 1-Click HDT Installer (.exe)'
  },
  pl: {
    siteTitle: 'HS Rival Meta & Deck Tracker - Najlepsze Talie Hearthstone Po Polsku',
    siteDesc: 'Odkryj najsilniejsze talie Hearthstone w polskiej wersji językowej! Statystyki mety, automatyczny kalkulator kosztu pyłu pod Twoją kolekcję oraz przewodniki Mulligan.',
    decksTab: 'Talie',
    matchesTab: 'Moje Mecze',
    collectionTab: 'Kolekcja',
    scanCollection: '🔍 Skanuj kolekcję (HDT)',
    syncDecks: 'Synchronizuj talie z HDT',
    updating: 'Aktualizacja...',
    downloadTracker: '📥 Pobierz Tracker (HDT)',
    downloadPlugin: '🔌 Wtyczka HDT (Plugin)',
    downloadInstaller: '⚡ Zainstaluj Wtyczkę HDT (.exe)',
    buyCoffee: '☕ Buy Me a Coffee',
    clearFilters: 'Wyczyść filtry',
    playerClass: 'Klasa Gracza',
    opponentClass: 'Klasa Przeciwnika',
    gameMode: 'Tryb Gry',
    standardRanked: 'Rankingowy Standard',
    wildRanked: 'Rankingowa Dzicz',
    rankRange: 'Przedział rang',
    myCollectionDust: 'Moja Kolekcja & Pył',
    limitToCollection: 'Ogranicz do mojej kolekcji (100%)',
    yourDust: '✨ Twój Pył (Arcane Dust):',
    fromHDT: 'z HDT',
    typeDustPlaceholder: 'Wpisz ilość pyłu...',
    fromGame: 'Z gry',
    dustFilterCheckbox: 'Talie w moim budżecie',
    allClasses: 'Wszystkie klasy',
    allRanks: 'Wszystkie',
    legend: 'Legenda',
    diamond: 'Diament',
    platinum: 'Platyna',
    gold: 'Złoto',
    silver: 'Srebro',
    bronze: 'Brąz',
    localDecks: 'Moje (HDT)',
    allCards: 'Wszystkie talie',
    ownedCards: 'Wybierz posiadane (100%)',
    craftableCards: 'Możliwe do stworzenia',
    searchPlaceholder: 'Szukaj wg nazwy talii...',
    allCount: 'Wszystkie talie',
    ownAllCards: '✓ Posiadasz wszystkie',
    youOwn: 'Posiadasz',
    ofCards: 'kart',
    missing: 'Brak',
    dust: 'pyłu',
    craftableWithDust: '✨ Stworzysz za pył!',
    winrate: 'Współczynnik zwycięstw',
    games: 'Gry',
    avgDuration: 'Avg Duration',
    copyDeck: 'Kopiuj talię',
    copied: '✓ Skopiowano',
    manaCurve: '📊 Krzywa many (Liczba kart wg kosztu)',
    mulliganGuide: '🃏 Przewodnik Mulligan (Winrate po zatrzymaniu w dłoni)',
    cardName: 'Nazwa Karty',
    inHandWinrate: 'Winrate w dłoni',
    keptWinrate: 'Winrate po zatrzymaniu',
    keepRate: 'Częstość zatrzymania %',
    myMatches: 'Historia Moich Rozegranych Meczów',
    matchupMatrix: 'Klasowa Macierz Wygranych (Matchup Matrix)',
    date: 'Data',
    result: 'Wynik',
    rank: 'Ranga',
    win: 'WYGRANA',
    loss: 'PRZEGRANA',
    collectionManager: 'Automatyczny Skaner & Synchronizacja Kolekcji',
    scanHDT: '🔍 Skanuj z HDT (Karty Bazowe + HDT)',
    markAll100: '✓ Oznacz 100% kart jako posiadane',
    manualJSON: 'Ręczny podgląd / Import kolekcji JSON',
    saveCollection: 'Zapisz Kolekcję',
    promoTitle: '🚀 HDT Rival Tracker',
    promoDesc: 'Pobierz nasz dedykowany program HDT do skanowania kolekcji i nagrywania meczów!',
    promoDownload: '📥 Pobierz HDT Tracker (.zip)',
    promoCoffee: '☕ Postaw kawkę!',
    widgetTitle: 'Podoba Ci się projekt?',
    widgetDesc: 'Możesz wesprzeć dalszy rozwój HS Rival Meta oraz skanera kolekcji stawiając symboliczną kawkę!',
    widgetCTA: 'Postaw kawkę (buymeacoffee.com/impacter)'
  }
};

const getClassIconUrl = (cls) => {
  const name = cls.toLowerCase().replace(' ', '_');
  return `/images/classes/${name}.png`;
};

const CLASSES = [
  'Death Knight', 'Demon Hunter', 'Druid', 'Hunter', 'Mage',
  'Paladin', 'Priest', 'Rogue', 'Shaman', 'Warlock', 'Warrior'
];

const getRankTabs = (t) => [
  { id: 'All', name: t.allRanks, icon: '🏆' },
  { id: 'Legend', name: t.legend, icon: '👑' },
  { id: 'Diamond', name: t.diamond, icon: '💎' },
  { id: 'Platinum', name: t.platinum, icon: '🩶' },
  { id: 'Gold', name: t.gold, icon: '🥇' },
  { id: 'Silver', name: t.silver, icon: '🥈' },
  { id: 'Bronze', name: t.bronze, icon: '🥉' }
];

const DUST_VALUES = {
  'LEGENDARY': 1600,
  'EPIC': 400,
  'RARE': 100,
  'COMMON': 40,
  'FREE': 0
};

// Helper: Return real stats for a deck based on database fields
function getDeckMetaStats(deck) {
  const winrate = deck.winrate && deck.winrate > 0 ? Number(deck.winrate).toFixed(1) : "50.0";
  const games = deck.games && deck.games > 0 ? Number(deck.games) : 0;
  const duration = deck.duration && deck.duration > 0 ? Number(deck.duration).toFixed(1) : "7.0";
  
  return { winrate, games, duration };
}

function CardRow({ card, missing, lang = 'en' }) {
  const [tooltipPos, setTooltipPos] = useState(null);
  
  const RARITY_COLORS = {
    LEGENDARY: '#f59e0b',
    EPIC: '#a855f7',
    RARE: '#3b82f6',
    COMMON: '#64748b',
    FREE: '#64748b'
  };
  const rarityColor = RARITY_COLORS[card.rarity] || '#64748b';
  
  const cardArtUrl = `https://art.hearthstonejson.com/v1/tiles/${card.id}.png`;
  const fullCardUrl = `https://art.hearthstonejson.com/v1/render/latest/${lang === 'pl' ? 'plPL' : 'enUS'}/512x/${card.id}.png`;
  const displayName = lang === 'pl' ? (card.name_pl || card.name) : (card.name_en || card.name);
  
  const isMissingAll = card.owned === 0;
  const isPartial = card.owned > 0 && card.owned < card.count;
  
  const bg = isMissingAll ? '#fff5f5' : (isPartial ? '#fffbeb' : '#ffffff');
  const border = isMissingAll ? '#fca5a5' : (isPartial ? '#fcd34d' : '#cbd5e1');

  const handleMouseEnter = (e) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const cardWidth = 280;
    const cardHeight = 400;
    
    // Position top of preview aligned with the top of the CardRow
    const calculatedLeft = rect.left - cardWidth - 12; // 12px gap to the left
    const calculatedTop = rect.top - 5; // Align top with the row
    
    // Prevent going off left screen edge
    const finalLeft = Math.max(10, calculatedLeft);
    // Prevent going off top edge, and softly clamp bottom edge if screen is small
    let finalTop = Math.max(10, calculatedTop);
    if (finalTop + cardHeight > window.innerHeight - 10) {
      finalTop = Math.max(10, window.innerHeight - cardHeight - 10);
    }
    
    setTooltipPos({ left: finalLeft, top: finalTop });
  };
  
  const handleMouseLeave = () => {
    setTooltipPos(null);
  };

  return (
    <div 
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      style={{
        position: 'relative',
        display: 'flex',
        alignItems: 'center',
        gap: '12px',
        padding: '8px 12px',
        borderRadius: '6px',
        background: bg,
        border: `1.5px solid ${border}`,
        boxShadow: tooltipPos ? '0 4px 12px rgba(0,0,0,0.1)' : '0 1px 3px rgba(0,0,0,0.05)',
        top: tooltipPos ? '-1px' : '0',
        transition: 'all 0.15s ease',
        cursor: 'default',
        zIndex: tooltipPos ? 10 : 1
      }}>
      
      {/* Tooltip with full card image rendered into document.body to bypass parent CSS transforms */}
      {tooltipPos && createPortal(
        <div style={{
          position: 'fixed',
          top: `${tooltipPos.top}px`,
          left: `${tooltipPos.left}px`,
          zIndex: 99999, // Extremely high z-index to overlap sidebar
          pointerEvents: 'none',
          animation: 'fadeIn 0.15s ease-in-out'
        }}>
          <img src={fullCardUrl} alt={displayName} style={{ width: '280px', height: 'auto', filter: 'drop-shadow(0 15px 35px rgba(0,0,0,0.6))' }} />
        </div>,
        document.body
      )}

      {/* Card Art Thumbnail */}
      <div style={{
        width: '40px',
        height: '40px',
        borderRadius: '5px',
        backgroundImage: `url(${cardArtUrl})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        flexShrink: 0,
        border: `2.5px solid ${rarityColor}`,
        boxShadow: `0 0 8px ${rarityColor}44`
      }} />
      {/* Mana cost circle */}
      <div style={{
        width: '26px', height: '26px', borderRadius: '50%',
        background: 'linear-gradient(135deg, #3b82f6, #1d4ed8)',
        display: 'flex', justifyContent: 'center', alignItems: 'center',
        fontSize: '13px', fontWeight: '800', color: '#fff',
        flexShrink: 0, boxShadow: '0 2px 4px rgba(29,78,216,0.3)'
      }}>
        {card.cost ?? '?'}
      </div>
      {/* Card name */}
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{
          fontSize: '14px', fontWeight: '800',
          color: isMissingAll ? '#dc2626' : (isPartial ? '#b45309' : '#0f172a'),
          whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis'
        }}>
          {displayName || card.id}
        </div>
        {card.owned >= (card.count || 1) ? (
          <div style={{ fontSize: '11px', color: '#16a34a', fontWeight: '700' }}>
            {lang === 'pl' ? `✓ posiadasz (${card.owned || card.count})` : `✓ owned (${card.owned || card.count})`}
          </div>
        ) : card.owned > 0 ? (
          <div style={{ fontSize: '11px', color: '#f59e0b', fontWeight: '700' }}>
            {lang === 'pl' ? `⚠ brakuje ${card.count - card.owned} (masz ${card.owned}/${card.count})` : `⚠ missing ${card.count - card.owned} (owned ${card.owned}/${card.count})`}
          </div>
        ) : (
          <div style={{ fontSize: '11px', color: '#dc2626', fontWeight: '700' }}>
            {lang === 'pl' ? `✗ brak ${card.count}x` : `✗ missing ${card.count}x`}
          </div>
        )}
      </div>

      {/* Count badge */}
      <div style={{
        fontSize: '12px', fontWeight: '800', color: '#fff',
        background: card.count > 1 ? '#f59e0b' : '#64748b',
        borderRadius: '50%', width: '22px', height: '22px',
        display: 'flex', justifyContent: 'center', alignItems: 'center',
        flexShrink: 0
      }}>
        {card.count}
      </div>
    </div>
  );
}


export default function App() {
  const [activeTab, setActiveTab] = useState('meta');
  const [decks, setDecks] = useState([]);
  const [matches, setMatches] = useState([]);
  const [collection, setCollection] = useState(() => {
    try {
      const cached = localStorage.getItem('hs_rival_collection');
      return cached ? JSON.parse(cached) : {};
    } catch {
      return {};
    }
  });
  const [matchups, setMatchups] = useState({});
  const [loadingDecks, setLoadingDecks] = useState(false);
  const [hdtConnected, setHdtConnected] = useState(false);
  const [deckOffset, setDeckOffset] = useState(0);
  const [hasMoreDecks, setHasMoreDecks] = useState(true);
  const loadMoreRef = useRef(null);
  const bookmarkletRef = useRef(null);
  const DECK_BATCH = 20;
  const [loadingMatches, setLoadingMatches] = useState(false);

  useEffect(() => {
    if (bookmarkletRef.current) {
      bookmarkletRef.current.href = `javascript:(async function(){try{const r=await fetch("https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL&TimeRange=CURRENT_EXPANSION");const d=await r.text();const f=document.createElement('form');f.method='POST';f.action='https://hs-rival-meta.onrender.com/api/meta/hsreplay-sync';f.target='_blank';const i=document.createElement('input');i.type='hidden';i.name='payload';i.value=d;f.appendChild(i);document.body.appendChild(f);f.submit();}catch(e){alert("HS Rival Blad: "+e.message);}})();`;
    }
  }, []);

  // Filters (HSReplay Layout)
  const [selectedClass, setSelectedClass] = useState('All');
  const [opponentClass, setOpponentClass] = useState('All');
  const [searchQuery, setSearchQuery] = useState('');
  const [gameMode, setGameMode] = useState('Standard');
  const [ownedOnly, setOwnedOnly] = useState(false);
  const [sortBy, setSortBy] = useState('Gry'); // 'Gry' | 'Winrate' | 'Pył'
  const [sortDirection, setSortDirection] = useState('desc'); // 'asc' | 'desc'
  const [cardsInDecks, setCardsInDecks] = useState('All'); // 'All' | 'Owned' | 'Craftable'

  // Expanded Deck ID (Accordion)
  const [expandedDeckId, setExpandedDeckId] = useState(null);

  // Rank range filter
  const [rankRange, setRankRange] = useState('All');

  // Mulligan stats for expanded deck
  const [mulliganStats, setMulliganStats] = useState(null);
  const [loadingMulligan, setLoadingMulligan] = useState(false);

  // Custom collection & Dust states
  const [collectionJsonText, setCollectionJsonText] = useState('');
  const [collectionStatus, setCollectionStatus] = useState('');
  const [userDust, setUserDust] = useState(0);
  const [dustBudget, setDustBudget] = useState(1600);
  const [filterByDust, setFilterByDust] = useState(false);
  const [isDustManual, setIsDustManual] = useState(false);
  const [collectionCards, setCollectionCards] = useState([]);
  const [collSearch, setCollSearch] = useState('');
  const [collClass, setCollClass] = useState('All');
  const [visibleCollCount, setVisibleCollCount] = useState(80);

  // Language & i18n states (Default: English)
  const [lang, setLang] = useState(() => localStorage.getItem('hs_rival_lang') || 'en');
  const t = I18N[lang] || I18N.en;
  const rankTabs = getRankTabs(t);
  const classNames = CLASS_NAMES[lang] || CLASS_NAMES.en;

  // Copy & Sync & Donate states
  const [copiedId, setCopiedId] = useState(null);
  const [syncing, setSyncing] = useState(false);
  const [syncStatus, setSyncStatus] = useState('');
  const [showDonateWidget, setShowDonateWidget] = useState(true);

  // Mobile sidebar toggle
  const [sidebarOpen, setSidebarOpen] = useState(false);

  // Per-user token for collection sync with tracker
  const [userToken] = useState(() => {
    let token = localStorage.getItem('hs_rival_token');
    if (!token) {
      token = crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).slice(2) + Date.now().toString(36);
      localStorage.setItem('hs_rival_token', token);
    }
    return token;
  });
  const [tokenCopied, setTokenCopied] = useState(false);

  // Visitor counter
  const [visitStats, setVisitStats] = useState(null);

  // HTTPS-safe HDT plugin connection status check & collection auto-refresh
  useEffect(() => {
    const checkHdtPlugin = async () => {
      if (!userToken) return;
      try {
        const res = await fetch(`${API_URL}/plugin-status`, {
          headers: { 'X-User-Token': userToken }
        });
        if (res.ok) {
          const data = await res.json();
          if (data.connected) {
            setHdtConnected(true);
            fetchCollection();
          } else {
            setHdtConnected(false);
          }
        }
      } catch {
        setHdtConnected(false);
      }

      // Optional local HTTP pairing attempt
      try {
        const localRes = await fetch('http://127.0.0.1:48854/ping');
        if (localRes.ok) {
          const localData = await localRes.json();
          if (userToken && localData.userToken !== userToken) {
            await fetch('http://127.0.0.1:48854/token', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ token: userToken })
            });
          }
        }
      } catch {}
    };

    checkHdtPlugin();
    const interval = setInterval(checkHdtPlugin, 6000);
    return () => clearInterval(interval);
  }, [userToken]);

  // Track visit once per session & fetch stats
  useEffect(() => {
    const fetchVisits = async () => {
      try {
        const res = await fetch(`${API_URL}/stats/visits`);
        const data = await res.json();
        setVisitStats(data);
      } catch {}
    };
    const trackVisit = async () => {
      if (!sessionStorage.getItem('hs_visited')) {
        sessionStorage.setItem('hs_visited', '1');
        try { await fetch(`${API_URL}/stats/visit`, { method: 'POST' }); } catch {}
      }
      fetchVisits();
    };
    trackVisit();
  }, []);

  // Fetch Decks: reset on filter change
  useEffect(() => {
    setDeckOffset(0);
    setHasMoreDecks(true);
    setDecks([]);
    fetchDecks(0, true);
    fetchCollection();
  }, [selectedClass, searchQuery, gameMode, ownedOnly, cardsInDecks, filterByDust, dustBudget]);

  // Infinite scroll: load more when sentinel is visible
  useEffect(() => {
    if (!loadMoreRef.current) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMoreDecks && !loadingDecks) {
          setDeckOffset(prev => prev + DECK_BATCH);
        }
      },
      { threshold: 0.1 }
    );
    observer.observe(loadMoreRef.current);
    return () => observer.disconnect();
  }, [hasMoreDecks, loadingDecks]);

  // Load next batch when offset changes (but not on initial 0 — that's handled above)
  useEffect(() => {
    if (deckOffset === 0) return;
    fetchDecks(deckOffset, false);
  }, [deckOffset]);

  // Load Matches when active tab changes
  useEffect(() => {
    if (activeTab === 'matches') {
      fetchMatches();
      fetchMatchups();
    }
  }, [activeTab]);

  // Load Mulligan stats when deck is expanded
  useEffect(() => {
    if (expandedDeckId) {
      const deck = decks.find(d => d.id === expandedDeckId);
      if (deck) {
        fetchMulliganStats(deck.deck_code);
      }
    } else {
      setMulliganStats(null);
    }
  }, [expandedDeckId, decks]);

  // Dynamic SEO Page Title & Meta Description updates
  useEffect(() => {
    let title = t.siteTitle;
    let desc = t.siteDesc;

    const classNameTr = classNames[selectedClass] || selectedClass;
    if (activeTab === 'meta') {
      if (selectedClass !== 'All') {
        title = lang === 'pl' ? `Talie dla ${classNameTr} - HS Rival Hearthstone Meta` : `${classNameTr} Decks - HS Rival Hearthstone Meta`;
        desc = lang === 'pl' ? `Najlepsze talie dla klasy ${classNameTr} w Hearthstone. Sprawdź statystyki wygranych, kalkulator pyłu i karty dla ${classNameTr}.` : `Best ${classNameTr} decks in Hearthstone. Check winrates, dust calculator, and cards for ${classNameTr}.`;
      } else {
        title = lang === 'pl' ? 'Najlepsze Talie Hearthstone (Meta Tier List) - HS Rival' : 'Top Hearthstone Decks (Meta Tier List) - HS Rival';
      }
    } else if (activeTab === 'matches') {
      title = lang === 'pl' ? 'Moje Mecze i Macierz Matchupów - HS Rival Deck Tracker' : 'My Played Matches & Matchup Matrix - HS Rival Deck Tracker';
      desc = lang === 'pl' ? 'Przeglądaj historię własnych meczów z Hearthstone Deck Trackera oraz analizuj wygrane w macierzy klasowej.' : 'View your match history from Hearthstone Deck Tracker and analyze class winrate matrix.';
    } else if (activeTab === 'collection') {
      title = lang === 'pl' ? 'Zarządzanie Kolekcją Kart - HS Rival' : 'Card Collection Manager - HS Rival';
      desc = lang === 'pl' ? 'Wprowadź lub zsynchronizuj swoją kolekcję kart Hearthstone, aby automatycznie obliczać brakujący pył dla najlepszych talii.' : 'Sync your Hearthstone card collection to auto-calculate missing dust for top meta decks.';
    }

    document.title = title;

    const metaDesc = document.querySelector('meta[name="description"]');
    if (metaDesc) {
      metaDesc.setAttribute('content', desc);
    }
  }, [activeTab, selectedClass, lang]);

  const fetchDecks = async (offset = 0, reset = true) => {
    if (loadingDecks) return;
    setLoadingDecks(true);
    try {
      let url = `${API_URL}/decks?limit=${DECK_BATCH}&offset=${offset}&`;
      if (selectedClass !== 'All') url += `playerClass=${encodeURIComponent(selectedClass)}&`;
      if (gameMode !== 'All') url += `gameMode=${encodeURIComponent(gameMode)}&`;
      if (ownedOnly) url += `ownedOnly=true&`;
      if (filterByDust && dustBudget !== undefined && dustBudget !== null) url += `maxDust=${dustBudget}&`;
      if (cardsInDecks !== 'All') url += `cardsInDecks=${encodeURIComponent(cardsInDecks)}&`;
      if (searchQuery) url += `search=${encodeURIComponent(searchQuery)}`;

      const res = await fetch(url, {
        headers: userToken ? { 'X-User-Token': userToken } : {}
      });
      const data = await res.json();
      const total = parseInt(res.headers.get('X-Total-Count'), 10) || 0;
      const batch = Array.isArray(data) ? data : [];

      if (reset) {
        setDecks(batch);
      } else {
        setDecks(prev => [...prev, ...batch]);
      }

      // No more decks if batch is smaller than requested or offset+batch >= total
      if (batch.length < DECK_BATCH || (total > 0 && offset + batch.length >= total)) {
        setHasMoreDecks(false);
      } else {
        setHasMoreDecks(true);
      }
    } catch (err) {
      console.error('Error fetching decks:', err);
    } finally {
      setLoadingDecks(false);
    }
  };

  const fetchMatches = async () => {
    setLoadingMatches(true);
    try {
      const res = await fetch(`${API_URL}/matches`);
      const data = await res.json();
      setMatches(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Error fetching matches:', err);
    } finally {
      setLoadingMatches(false);
    }
  };

  const fetchCollection = async (includeCards = false) => {
    try {
      // Fetch from server using user token (per-user collection)
      const url = includeCards ? `${API_URL}/collection?cards=true` : `${API_URL}/collection`;
      const res = await fetch(url, {
        headers: { 'X-User-Token': userToken }
      });
      const data = await res.json();
      const coll = data.collection || {};
      setCollection(coll);
      if (data.cards && Array.isArray(data.cards)) {
        setCollectionCards(data.cards);
      }
      if (Object.keys(coll).length > 0) {
        setCollectionJsonText(JSON.stringify(coll, null, 2));
        // Cache locally
        localStorage.setItem('hs_rival_collection', JSON.stringify(coll));
      } else {
        // No server collection yet — check localStorage cache
        const localColl = localStorage.getItem('hs_rival_collection');
        if (localColl) {
          const parsed = JSON.parse(localColl);
          setCollection(parsed);
          setCollectionJsonText(JSON.stringify(parsed, null, 2));
        }
      }
      if (data.dust !== undefined && data.dust > 0) {
        setUserDust(data.dust);
        if (!isDustManual) setDustBudget(data.dust);
      }
    } catch (err) {
      console.error('Error fetching collection:', err);
      // Fallback to localStorage on network error
      const localColl = localStorage.getItem('hs_rival_collection');
      if (localColl) {
        const parsed = JSON.parse(localColl);
        setCollection(parsed);
      }
    }
  };

  const handleDustChange = (val) => {
    const num = parseInt(val, 10);
    const validNum = isNaN(num) ? 0 : Math.max(0, num);
    setDustBudget(validNum);
    setIsDustManual(true);
    setFilterByDust(true);
    fetch(`${API_URL}/collection/dust`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dust: validNum })
    }).catch(err => console.error('Error saving dust:', err));
  };

  const fetchMatchups = async () => {
    try {
      const res = await fetch(`${API_URL}/stats/matchups`);
      const data = await res.json();
      setMatchups(data || {});
    } catch (err) {
      console.error('Error fetching matchups:', err);
    }
  };

  const fetchMulliganStats = async (deckCode) => {
    setLoadingMulligan(true);
    try {
      const res = await fetch(`${API_URL}/stats/mulligan/${encodeURIComponent(deckCode)}`);
      const data = await res.json();
      setMulliganStats(data);
    } catch (err) {
      console.error('Error fetching mulligan stats:', err);
      setMulliganStats(null);
    } finally {
      setLoadingMulligan(false);
    }
  };

  const handleCopyCode = (code, id) => {
    navigator.clipboard.writeText(code);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const handleSync = async () => {
    setSyncing(true);
    setSyncStatus('Synchronizowanie...');
    try {
      const res = await fetch(`${API_URL}/decks/sync`, { method: 'POST' });
      const data = await res.json();
      if (data.success) {
        setSyncStatus('Dane zaktualizowane (wczytano talie z HDT)!');
        fetchDecks();
        fetchCollection();
      } else {
        setSyncStatus(`Błąd: ${data.error}`);
      }
    } catch (err) {
      setSyncStatus('Błąd połączenia.');
    } finally {
      setSyncing(false);
      setTimeout(() => setSyncStatus(''), 5000);
    }
  };

  // Calculate mana curve distribution
  const getManaCurve = (cards) => {
    const curve = Array(8).fill(0); // 0 to 7+
    if (!cards) return curve;
    cards.forEach(card => {
      const cost = Math.min(card.cost, 7);
      curve[cost] += card.count;
    });
    return curve;
  };

  // Calculate deck composition stats
  const getCompositionStats = (cards) => {
    const stats = { Minion: 0, Spell: 0, Weapon: 0, Hero: 0, Other: 0 };
    if (!cards) return stats;
    cards.forEach(card => {
      const type = card.type ? card.type.charAt(0) + card.type.slice(1).toLowerCase() : 'Other';
      if (type in stats) {
        stats[type] += card.count;
      } else {
        stats.Other += card.count;
      }
    });
    return stats;
  };

  const handleSaveCollection = async () => {
    setCollectionStatus('Zapisywanie...');
    try {
      const parsed = JSON.parse(collectionJsonText);
      // Save to server with user token
      const res = await fetch(`${API_URL}/collection`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-User-Token': userToken },
        body: JSON.stringify({ collection: parsed, isFullSync: true })
      });
      const data = await res.json();
      if (data.success) {
        // Cache locally too
        localStorage.setItem('hs_rival_collection', JSON.stringify(parsed));
        setCollection(parsed);
        setCollectionStatus('Zapisano! ✅ Kolekcja zsynchronizowana.');
        fetchDecks();
      } else {
        setCollectionStatus(`Błąd: ${data.error}`);
      }
    } catch (e) {
      setCollectionStatus('Błąd: Niepoprawny JSON.');
    } finally {
      setTimeout(() => setCollectionStatus(''), 5000);
    }
  };

  const handleScanCollection = async () => {
    setCollectionStatus('Skanowanie HDT...');
    try {
      const res = await fetch(`${API_URL}/collection/scan`, { method: 'POST' });
      const data = await res.json();
      if (data.success) {
        setCollectionStatus(data.message);
        // Fetch from server and save to localStorage
        const collRes = await fetch(`${API_URL}/collection`);
        const collData = await collRes.json();
        if (collData.collection && Object.keys(collData.collection).length > 0) {
          localStorage.setItem('hs_rival_collection', JSON.stringify(collData.collection));
          if (collData.dust > 0) localStorage.setItem('hs_rival_dust', String(collData.dust));
        }
        fetchCollection();
        fetchDecks();
      } else {
        setCollectionStatus(`Błąd: ${data.error}`);
      }
    } catch (e) {
      setCollectionStatus('Błąd połączenia podczas skanowania.');
    } finally {
      setTimeout(() => setCollectionStatus(''), 6000);
    }
  };

  const handleMarkAllOwned = async () => {
    setCollectionStatus('Oznaczanie wszystkich kart mety jako posiadane...');
    try {
      const allDbfIds = {};
      decks.forEach(d => {
        d.cards?.forEach(c => {
          if (c.dbf_id) allDbfIds[c.dbf_id] = c.count || 2;
        });
      });
      // Save to server with token
      await fetch(`${API_URL}/collection`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-User-Token': userToken },
        body: JSON.stringify({ collection: allDbfIds, isFullSync: true })
      });
      localStorage.setItem('hs_rival_collection', JSON.stringify(allDbfIds));
      setCollection(allDbfIds);
      setCollectionStatus('Zaznaczono 100% kart mety jako posiadane! ✅');
      fetchDecks();
    } catch (e) {
      setCollectionStatus('Błąd podczas zapisywania.');
    } finally {
      setTimeout(() => setCollectionStatus(''), 5000);
    }
  };

  const handleResetFilters = () => {
    setSelectedClass('All');
    setOpponentClass('All');
    setSearchQuery('');
    setGameMode('Standard');
    setOwnedOnly(false);
    setCardsInDecks('All');
    setRankRange('All');
    setFilterByDust(false);
  };

  // Process and Filter decks list based on sidebar filters
  const getProcessedDecks = () => {
    let result = [...decks];

    // Filter by collection owning
    if (ownedOnly) {
      result = result.filter(d => d.missingCount === 0);
    } else if (cardsInDecks === 'Owned') {
      result = result.filter(d => d.missingCount === 0);
    } else if (cardsInDecks === 'Craftable') {
      result = result.filter(d => d.missingCount > 0);
    }

    // Filter by dust budget
    if (filterByDust) {
      result = result.filter(d => d.dustCost <= dustBudget);
    }

    // Filter by rank range (Exact separate rank tabs)
    if (rankRange === 'Legend') {
      result = result.filter(d => d.rank_desc === 'Legend' || d.rank_desc?.toLowerCase().includes('legend'));
    } else if (rankRange === 'Diamond') {
      result = result.filter(d => d.rank_desc === 'Diamond' || d.rank_desc?.toLowerCase().includes('diamond') || d.rank_desc === 'Legend');
    } else if (rankRange === 'Platinum') {
      result = result.filter(d => d.rank_desc === 'Platinum' || d.rank_desc?.toLowerCase().includes('platinum') || d.dustCost <= 10000);
    } else if (rankRange === 'Gold') {
      result = result.filter(d => d.rank_desc === 'Gold' || d.rank_desc?.toLowerCase().includes('gold') || d.dustCost <= 8000);
    } else if (rankRange === 'Silver') {
      result = result.filter(d => d.rank_desc === 'Silver' || d.rank_desc?.toLowerCase().includes('silver') || d.dustCost <= 6000);
    } else if (rankRange === 'Bronze') {
      result = result.filter(d => d.rank_desc === 'Bronze' || d.rank_desc?.toLowerCase().includes('bronze') || d.dustCost <= 5000);
    }

    // Sort decks
    result.sort((a, b) => {
      const statsA = getDeckMetaStats(a);
      const statsB = getDeckMetaStats(b);

      let valA, valB;
      if (sortBy === 'Gry') {
        valA = statsA.games;
        valB = statsB.games;
      } else if (sortBy === 'Winrate') {
        valA = parseFloat(statsA.winrate);
        valB = parseFloat(statsB.winrate);
      } else if (sortBy === 'Pył') {
        valA = a.dustCost;
        valB = b.dustCost;
      }

      if (sortDirection === 'asc') {
        return valA - valB;
      } else {
        return valB - valA;
      }
    });

    return result;
  };

  const processedDecks = getProcessedDecks();
  const totalGamesInMeta = processedDecks.reduce((sum, d) => sum + getDeckMetaStats(d).games, 0);

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      
      {/* Sticky dark header */}
      <header className="mob-header" style={{ height: '60px', background: 'var(--bg-header)', borderBottom: '2px solid #2e2646', boxShadow: '0 4px 20px rgba(0,0,0,0.6)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0 24px', position: 'sticky', top: 0, zIndex: 90 }}>
        {/* Top row: logo + nav (on mobile also filter toggle) */}
        <div className="mob-header-top" style={{ display: 'flex', alignItems: 'center', gap: '36px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <span style={{ fontSize: '24px' }}>🐉</span>
            <h1 style={{ fontFamily: 'var(--font-hs), serif', fontSize: '22px', fontWeight: '900', background: 'linear-gradient(135deg, #ffd700, #f59e0b, #d4af37)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent', letterSpacing: '0.04em', margin: 0 }}>
              HS RIVAL META
            </h1>
          </div>
          <nav className="mob-header-nav" style={{ display: 'flex', gap: '20px', height: '56px' }}>
            <button 
              onClick={() => setActiveTab('meta')}
              style={{ background: 'none', border: 'none', color: activeTab === 'meta' ? '#fff' : 'var(--text-light-muted)', fontSize: '14px', fontWeight: '700', cursor: 'pointer', borderBottom: activeTab === 'meta' ? '3px solid var(--gold)' : '3px solid transparent', padding: '0 4px', transition: 'all 0.2s', whiteSpace: 'nowrap' }}
            >
              {t.decksTab}
            </button>
            <button 
              onClick={() => {
                setActiveTab('collection');
                fetchCollection(true);
              }}
              style={{ background: 'none', border: 'none', color: activeTab === 'collection' ? '#fff' : 'var(--text-light-muted)', fontSize: '14px', fontWeight: '700', cursor: 'pointer', borderBottom: activeTab === 'collection' ? '3px solid var(--gold)' : '3px solid transparent', padding: '0 4px', transition: 'all 0.2s', whiteSpace: 'nowrap' }}
            >
              {t.collectionTab}
            </button>
          </nav>
          {/* Mobile-only filter toggle button */}
          {activeTab !== 'collection' && (
            <button
              className="mob-filter-toggle"
              onClick={() => setSidebarOpen(prev => !prev)}
              style={{ background: sidebarOpen ? 'rgba(2,132,199,0.3)' : 'rgba(255,255,255,0.07)', border: '1px solid rgba(255,255,255,0.2)', color: '#fff', borderRadius: '6px', padding: '6px 10px', fontSize: '18px', cursor: 'pointer', flexShrink: 0 }}
            >
              🔍
            </button>
          )}
        </div>

        <div className="mob-header-actions" style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          {/* Language Toggle Switcher */}
          <div style={{ display: 'flex', background: 'rgba(255, 255, 255, 0.08)', borderRadius: '6px', padding: '2px', border: '1px solid rgba(255, 255, 255, 0.12)', marginRight: '6px' }}>
            <button
              onClick={() => { setLang('en'); localStorage.setItem('hs_rival_lang', 'en'); }}
              style={{
                padding: '4px 8px', borderRadius: '4px', border: 'none',
                background: lang === 'en' ? 'var(--blue-hdt)' : 'transparent',
                color: lang === 'en' ? '#fff' : 'var(--text-light-muted)',
                fontWeight: '800', fontSize: '11px', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '4px'
              }}
            >
              🇬🇧 EN
            </button>
            <button
              onClick={() => { setLang('pl'); localStorage.setItem('hs_rival_lang', 'pl'); }}
              style={{
                padding: '4px 8px', borderRadius: '4px', border: 'none',
                background: lang === 'pl' ? 'var(--blue-hdt)' : 'transparent',
                color: lang === 'pl' ? '#fff' : 'var(--text-light-muted)',
                fontWeight: '800', fontSize: '11px', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '4px'
              }}
            >
              🇵🇱 PL
            </button>
          </div>

          {hdtConnected && (
            <span style={{ padding: '5px 12px', background: 'rgba(34,197,94,0.15)', border: '1px solid #22c55e', color: '#4ade80', borderRadius: '20px', fontSize: '12px', fontWeight: '800', display: 'flex', alignItems: 'center', gap: '6px', boxShadow: '0 0 10px rgba(34,197,94,0.2)' }}>
              🟢 {lang === 'pl' ? 'HDT Połączony!' : 'HDT Connected!'}
            </span>
          )}
          {syncStatus && <span style={{ fontSize: '12px', color: 'var(--gold)', fontWeight: '600' }}>{syncStatus}</span>}
          <a
            ref={bookmarkletRef}
            href="#"
            onClick={(e) => {
              e.preventDefault();
              alert(lang === 'pl' ? 'Przeciągnij ten przycisk na swój pasek zakładek w przeglądarce, a następnie wejdź na hsreplay.net i kliknij zakładkę!' : 'Drag this button to your bookmarks bar, then visit hsreplay.net and click the bookmark!');
            }}
            style={{ 
              padding: '6px 14px', 
              background: 'linear-gradient(135deg, #a855f7, #9333ea)', 
              border: '1px solid #c084fc', 
              color: '#fff', 
              borderRadius: '4px', 
              fontWeight: '800', 
              cursor: 'grab', 
              fontSize: '12px', 
              display: 'flex', 
              alignItems: 'center', 
              gap: '6px',
              textDecoration: 'none',
              boxShadow: '0 0 12px rgba(168, 85, 247, 0.4)'
            }}
            title={lang === 'pl' ? "Przeciągnij mnie na pasek zakładek!" : "Drag me to bookmarks!"}
          >
            <span>🪄</span> {lang === 'pl' ? 'Aktualizuj Meta (Przeciągnij)' : 'Update Meta (Drag me)'}
          </a>
          <a
            href="/api/download/tracker"
            download
            style={{ 
              padding: '6px 12px', 
              background: 'linear-gradient(135deg, #16a34a, #15803d)', 
              border: '1px solid #22c55e', 
              color: '#fff', 
              borderRadius: '4px', 
              fontWeight: '800', 
              cursor: 'pointer', 
              fontSize: '12px', 
              display: 'flex', 
              alignItems: 'center', 
              gap: '6px',
              textDecoration: 'none',
              boxShadow: '0 0 10px rgba(34, 197, 94, 0.3)'
            }}
          >
            {t.downloadTracker}
          </a>
          <a
            href="https://buymeacoffee.com/impacter"
            target="_blank"
            rel="noopener noreferrer"
            style={{ 
              padding: '6px 12px', 
              background: '#FFDD00', 
              border: '1px solid #facc15', 
              color: '#000000', 
              borderRadius: '4px', 
              fontWeight: '800', 
              cursor: 'pointer', 
              fontSize: '12px', 
              display: 'flex', 
              alignItems: 'center', 
              gap: '6px',
              textDecoration: 'none',
              boxShadow: '0 0 10px rgba(250, 204, 21, 0.3)'
            }}
          >
            {t.buyCoffee}
          </a>
        </div>
      </header>

      {/* Main Container Layout */}
      <div className="mob-layout" style={{ flex: 1, display: 'flex' }}>
        
        {/* Left Sidebar (Only visible on Talie & Moje Mecze tabs) */}
        {activeTab !== 'collection' && (
          <aside className={`mob-sidebar${sidebarOpen ? ' mob-sidebar-open' : ''}`} style={{ width: '240px', background: 'var(--bg-sidebar)', padding: '20px', display: 'flex', flexDirection: 'column', gap: '20px', flexShrink: 0 }}>
            {/* Reset Filters button */}
            <button 
              onClick={handleResetFilters}
              style={{ width: '100%', padding: '12px 0', background: '#dc2626', border: 'none', color: '#fff', fontWeight: '800', borderRadius: '6px', cursor: 'pointer', fontSize: '13px', textTransform: 'uppercase', letterSpacing: '0.05em', transition: 'opacity 0.15s' }}
              onMouseOver={e=>e.target.style.opacity=0.9}
              onMouseOut={e=>e.target.style.opacity=1}
            >
              {t.clearFilters}
            </button>

            {/* Player Class Filter Grid */}
            <div>
              <h4 style={{ color: 'var(--gold)', fontSize: '13px', textTransform: 'uppercase', fontWeight: '800', marginBottom: '10px', letterSpacing: '0.05em' }}>{t.playerClass}</h4>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '8px' }}>
                {CLASSES.map(cls => {
                  const isSelected = selectedClass === cls;
                  return (
                    <button 
                      key={cls}
                      onClick={() => setSelectedClass(isSelected ? 'All' : cls)}
                      title={classNames[cls]}
                      style={{
                        width: '44px', height: '44px', borderRadius: '50%',
                        border: isSelected ? `2.5px solid ${CLASS_COLORS[cls]}` : '1.5px solid #475569',
                        background: isSelected ? 'rgba(255, 255, 255, 0.12)' : 'rgba(0, 0, 0, 0.35)',
                        cursor: 'pointer', display: 'flex', justifyContent: 'center', alignItems: 'center', transition: 'all 0.15s',
                        boxShadow: isSelected ? `0 0 12px ${CLASS_COLORS[cls]}` : 'none'
                      }}
                    >
                      <img 
                        src={getClassIconUrl(cls)} 
                        alt={cls} 
                        style={{ 
                          width: '30px', height: '30px', objectFit: 'contain',
                          filter: isSelected ? 'none' : 'grayscale(35%) contrast(90%) brightness(85%)' 
                        }} 
                      />
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Opponent Class Filter Grid */}
            <div>
              <h4 style={{ color: 'var(--gold)', fontSize: '13px', textTransform: 'uppercase', fontWeight: '800', marginBottom: '10px', letterSpacing: '0.05em' }}>{t.opponentClass}</h4>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '8px' }}>
                {CLASSES.map(cls => {
                  const isSelected = opponentClass === cls;
                  return (
                    <button 
                      key={cls}
                      onClick={() => setOpponentClass(isSelected ? 'All' : cls)}
                      title={classNames[cls]}
                      style={{
                        width: '44px', height: '44px', borderRadius: '50%',
                        border: isSelected ? `2.5px solid ${CLASS_COLORS[cls]}` : '1.5px solid #475569',
                        background: isSelected ? 'rgba(255, 255, 255, 0.12)' : 'rgba(0, 0, 0, 0.35)',
                        cursor: 'pointer', display: 'flex', justifyContent: 'center', alignItems: 'center', transition: 'all 0.15s',
                        boxShadow: isSelected ? `0 0 12px ${CLASS_COLORS[cls]}` : 'none'
                      }}
                    >
                      <img 
                        src={getClassIconUrl(cls)} 
                        alt={cls} 
                        style={{ 
                          width: '30px', height: '30px', objectFit: 'contain',
                          filter: isSelected ? 'none' : 'grayscale(35%) contrast(90%) brightness(85%)' 
                        }} 
                      />
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Rank range filter (Sidebar Buttons) */}
            <div>
              <h4 style={{ color: 'var(--gold)', fontSize: '13px', textTransform: 'uppercase', fontWeight: '800', marginBottom: '10px', letterSpacing: '0.05em' }}>{t.rankRange}</h4>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '6px' }}>
                {rankTabs.map(tab => {
                  const isActive = rankRange === tab.id;
                  return (
                    <button
                      key={tab.id}
                      onClick={() => setRankRange(tab.id)}
                      style={{
                        padding: '8px 10px', borderRadius: '6px',
                        background: isActive ? 'rgba(2, 132, 199, 0.35)' : 'rgba(255, 255, 255, 0.05)',
                        border: isActive ? '1.5px solid var(--blue-hdt)' : '1px solid #334155',
                        color: isActive ? '#fff' : '#cbd5e1',
                        fontSize: '13px', fontWeight: '700', cursor: 'pointer',
                        display: 'flex', alignItems: 'center', gap: '6px', justifyContent: 'flex-start',
                        transition: 'all 0.15s'
                      }}
                    >
                      <span style={{ fontSize: '14px' }}>{tab.icon}</span>
                      <span style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{tab.name}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Game Mode Filters */}
            <div>
              <h4 style={{ color: 'var(--gold)', fontSize: '13px', textTransform: 'uppercase', fontWeight: '800', marginBottom: '10px', letterSpacing: '0.05em' }}>{t.gameMode}</h4>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                {['Standard', 'Wild'].map(mode => (
                  <button
                    key={mode}
                    onClick={() => setGameMode(mode)}
                    style={{
                      width: '100%', padding: '10px 14px', borderRadius: '6px', textAlign: 'left',
                      background: gameMode === mode ? 'rgba(255,255,255,0.08)' : 'transparent',
                      border: gameMode === mode ? '1px solid rgba(255,255,255,0.2)' : 'none', color: gameMode === mode ? '#fff' : '#94a3b8',
                      fontSize: '14px', fontWeight: '700', cursor: 'pointer'
                    }}
                  >
                    {mode === 'Standard' ? t.standardRanked : t.wildRanked}
                  </button>
                ))}
              </div>
            </div>

            {/* My Collection & Dust Controls */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <h4 style={{ color: 'var(--gold)', fontSize: '13px', textTransform: 'uppercase', fontWeight: '800', letterSpacing: '0.05em' }}>{t.myCollectionDust}</h4>
              
              <label style={{ display: 'flex', alignItems: 'center', gap: '10px', color: '#f1f5f9', fontSize: '14px', fontWeight: '600', cursor: 'pointer' }}>
                <input 
                  type="checkbox" 
                  checked={ownedOnly} 
                  onChange={(e) => setOwnedOnly(e.target.checked)} 
                  style={{ width: '18px', height: '18px', accentColor: 'var(--gold)' }}
                />
                {t.limitToCollection}
              </label>

              {/* Arcane Dust Budget Control */}
              <div style={{ background: 'rgba(255, 255, 255, 0.04)', border: '1px solid rgba(255, 255, 255, 0.1)', borderRadius: '8px', padding: '12px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ fontSize: '13px', fontWeight: '800', color: 'var(--gold-light)' }}>{t.yourDust}</span>
                  {userDust > 0 && (
                    <span style={{ fontSize: '11px', background: 'rgba(234, 179, 8, 0.2)', color: '#fde047', padding: '2px 8px', borderRadius: '4px', border: '1px solid rgba(234, 179, 8, 0.4)', fontWeight: '700' }}>
                      {t.fromHDT}: {userDust}
                    </span>
                  )}
                </div>

                <div style={{ display: 'flex', gap: '6px' }}>
                  <input 
                    type="number" 
                    value={dustBudget}
                    onChange={(e) => handleDustChange(e.target.value)}
                    placeholder={t.typeDustPlaceholder}
                    style={{
                      flex: 1, padding: '8px 10px', borderRadius: '6px',
                      background: '#0f0d1b', border: '1px solid #334155', color: '#fff',
                      fontSize: '14px', fontWeight: '700', outline: 'none'
                    }}
                  />
                  {userDust > 0 && (
                    <button
                      onClick={() => { setDustBudget(userDust); setIsDustManual(false); }}
                      title="Restore value from HDT game scan"
                      style={{
                        padding: '8px 10px', borderRadius: '6px', background: 'rgba(255,255,255,0.08)',
                        border: '1px solid #475569', color: '#cbd5e1', fontSize: '12px', fontWeight: '700', cursor: 'pointer'
                      }}
                    >
                      {t.fromGame} ({userDust})
                    </button>
                  )}
                </div>

                <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap', marginTop: '4px' }}>
                  {[400, 1600, 3200, 4800].map(val => (
                    <button
                      key={val}
                      onClick={() => handleDustChange(val)}
                      style={{
                        padding: '4px 8px', borderRadius: '4px',
                        background: dustBudget === val ? 'rgba(234, 179, 8, 0.3)' : 'rgba(255,255,255,0.05)',
                        border: dustBudget === val ? '1.5px solid var(--gold)' : '1px solid #334155',
                        color: dustBudget === val ? '#fff' : '#cbd5e1',
                        fontSize: '12px', fontWeight: '700', cursor: 'pointer'
                      }}
                    >
                      {val} {t.dust}
                    </button>
                  ))}
                </div>

                <label style={{ display: 'flex', alignItems: 'center', gap: '10px', color: '#f1f5f9', fontSize: '13px', fontWeight: '600', cursor: 'pointer', marginTop: '4px' }}>
                  <input 
                    type="checkbox" 
                    checked={filterByDust} 
                    onChange={(e) => setFilterByDust(e.target.checked)} 
                    style={{ width: '16px', height: '16px', accentColor: 'var(--gold)' }}
                  />
                  {t.dustFilterCheckbox} (≤ {dustBudget} ✨)
                </label>
              </div>

              {/* Download Tracker & Buy Me a Coffee Promo Card */}
              <div style={{ marginTop: 'auto', background: 'rgba(255, 255, 255, 0.04)', border: '1px solid rgba(255, 255, 255, 0.1)', borderRadius: '8px', padding: '16px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                <div style={{ fontSize: '14px', fontWeight: '800', color: 'var(--gold-light)', display: 'flex', alignItems: 'center', gap: '6px' }}>
                  {t.promoTitle}
                </div>
                <p style={{ fontSize: '12px', color: '#cbd5e1', margin: 0, lineHeight: '1.4' }}>
                  {t.promoDesc}
                </p>
                <a
                  href="/api/download/tracker"
                  download
                  style={{
                    width: '100%', padding: '10px 0', background: 'linear-gradient(135deg, #16a34a, #15803d)',
                    border: 'none', color: '#fff', fontWeight: '800', borderRadius: '6px', textAlign: 'center',
                    fontSize: '13px', cursor: 'pointer', textDecoration: 'none', display: 'block',
                    boxShadow: '0 4px 12px rgba(22, 163, 74, 0.3)'
                  }}
                >
                  {t.promoDownload}
                </a>
                <a
                  href="https://buymeacoffee.com/impacter"
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{
                    width: '100%', padding: '10px 0', background: '#FFDD00',
                    border: 'none', color: '#000', fontWeight: '800', borderRadius: '6px', textAlign: 'center',
                    fontSize: '13px', cursor: 'pointer', textDecoration: 'none', display: 'block',
                    boxShadow: '0 4px 12px rgba(250, 204, 21, 0.3)'
                  }}
                >
                  {t.promoCoffee}
                </a>
              </div>
            </div>
          </aside>
        )}

        {/* Content Area */}
        <main className="mob-main" style={{ flex: 1, padding: '24px', overflowY: 'auto' }}>
          
          {/* Tab 1: Decks List (Authentic HSReplay Design) */}
          {activeTab === 'meta' && (
            <div className="animate-fade-in" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              
              {/* Top Dedicated Rank Tabs Bar */}
              <div className="mob-rank-tabs" style={{ display: 'flex', gap: '8px', background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '8px', padding: '8px', overflowX: 'auto', boxShadow: '0 4px 14px rgba(0,0,0,0.3)' }}>
                {rankTabs.map(tab => {
                  const isActive = rankRange === tab.id;
                  return (
                    <button
                      key={tab.id}
                      onClick={() => setRankRange(tab.id)}
                      style={{
                        flex: 1, minWidth: '100px', padding: '10px 12px', border: 'none', borderRadius: '6px',
                        background: isActive ? 'var(--blue-hdt)' : 'rgba(255,255,255,0.04)',
                        color: isActive ? '#fff' : '#cbd5e1',
                        fontSize: '14px', fontWeight: '800', cursor: 'pointer',
                        display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px',
                        transition: 'all 0.15s',
                        boxShadow: isActive ? '0 4px 12px rgba(2, 132, 199, 0.35)' : 'none'
                      }}
                    >
                      <span style={{ fontSize: '16px' }}>{tab.icon}</span>
                      <span>{tab.name}</span>
                    </button>
                  );
                })}
              </div>

              {/* Top Filters & Statistics bar */}
              <div className="mob-filters-bar" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '8px', padding: '12px 20px', boxShadow: '0 4px 14px rgba(0,0,0,0.3)' }}>
                {/* Cards In Decks Tab selector */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '24px' }}>
                  <span style={{ fontSize: '12px', fontWeight: '700', color: 'var(--text-dark-muted)', textTransform: 'uppercase' }}>
                    {lang === 'pl' ? 'Karty w taliach:' : 'Cards in decks:'}
                  </span>
                  <div style={{ display: 'flex', border: '1px solid var(--panel-border)', borderRadius: '4px', overflow: 'hidden', background: '#120e22' }}>
                    {['All', 'Owned', 'Craftable'].map(type => (
                      <button
                        key={type}
                        onClick={() => setCardsInDecks(type)}
                        style={{
                          padding: '6px 14px', border: 'none', fontSize: '12px', fontWeight: '700', cursor: 'pointer',
                          background: cardsInDecks === type ? 'var(--card-bg-hover)' : 'transparent',
                          color: cardsInDecks === type ? 'var(--gold-light)' : 'var(--text-dark-muted)',
                          borderRight: type !== 'Craftable' ? '1px solid var(--panel-border)' : 'none'
                        }}
                      >
                        {type === 'All' ? t.allCards : type === 'Owned' ? t.ownedCards : t.craftableCards}
                      </button>
                    ))}
                  </div>
                </div>

                {/* Sort selector */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  <span style={{ fontSize: '12px', fontWeight: '700', color: 'var(--text-dark-muted)', textTransform: 'uppercase' }}>
                    {lang === 'pl' ? 'Sortuj:' : 'Sort by:'}
                  </span>
                  <select 
                    value={sortBy} 
                    onChange={e => setSortBy(e.target.value)}
                    style={{ padding: '6px 12px', borderRadius: '4px', border: '1px solid var(--panel-border)', fontSize: '13px', outline: 'none', background: '#120e22', color: '#f1f5f9', fontWeight: '600' }}
                  >
                    <option value="Gry">{lang === 'pl' ? 'Gry rozegrane' : 'Games played'}</option>
                    <option value="Winrate">{lang === 'pl' ? 'Współczynnik zwycięstw' : 'Winrate'}</option>
                    <option value="Pył">{lang === 'pl' ? 'Koszt pyłu' : 'Dust cost'}</option>
                  </select>
                  <button 
                    onClick={() => setSortDirection(prev => prev === 'asc' ? 'desc' : 'asc')}
                    style={{ padding: '6px 10px', background: '#120e22', border: '1px solid var(--panel-border)', color: '#f1f5f9', borderRadius: '4px', cursor: 'pointer', fontSize: '13px' }}
                  >
                    {sortDirection === 'desc' ? '↓' : '↑'}
                  </button>
                </div>

                {/* Decks counters */}
                <div style={{ fontSize: '12px', color: 'var(--text-dark-muted)', fontWeight: '600' }}>
                  {lang === 'pl' 
                    ? <>Pokazuje <b style={{ color: 'var(--gold-light)' }}>{processedDecks.length} talii</b> z <b style={{ color: 'var(--gold-light)' }}>{totalGamesInMeta.toLocaleString('pl-PL')} gier</b></>
                    : <>Showing <b style={{ color: 'var(--gold-light)' }}>{processedDecks.length} decks</b> from <b style={{ color: 'var(--gold-light)' }}>{totalGamesInMeta.toLocaleString('en-US')} games</b></>
                  }
                </div>
              </div>

              {/* Text Search input */}
              <input 
                type="text" 
                placeholder={t.searchPlaceholder}
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                style={{ width: '100%', padding: '10px 14px', background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '6px', color: '#f1f5f9', fontSize: '14px', outline: 'none' }}
              />

              {/* Decks List Rows */}
              <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                {/* Show skeleton cards while initial load */}
                {loadingDecks && decks.length === 0 ? (
                  Array.from({ length: 6 }).map((_, i) => (
                    <div key={`sk-${i}`} style={{
                      background: '#fff', border: '1px solid #e2e8f0', borderRadius: '12px',
                      padding: '16px 20px', display: 'flex', alignItems: 'center', gap: '16px',
                      animation: 'pulse 1.4s ease-in-out infinite'
                    }}>
                      <div style={{ width: '48px', height: '48px', borderRadius: '50%', background: 'linear-gradient(90deg, #e2e8f0 25%, #f1f5f9 50%, #e2e8f0 75%)', backgroundSize: '200% 100%', flexShrink: 0 }} />
                      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '8px' }}>
                        <div style={{ height: '16px', width: `${55 + (i % 3) * 15}%`, borderRadius: '6px', background: 'linear-gradient(90deg, #e2e8f0 25%, #f1f5f9 50%, #e2e8f0 75%)', backgroundSize: '200% 100%' }} />
                        <div style={{ height: '12px', width: `${35 + (i % 2) * 20}%`, borderRadius: '6px', background: 'linear-gradient(90deg, #e2e8f0 25%, #f1f5f9 50%, #e2e8f0 75%)', backgroundSize: '200% 100%' }} />
                      </div>
                      <div style={{ width: '80px', height: '32px', borderRadius: '8px', background: 'linear-gradient(90deg, #e2e8f0 25%, #f1f5f9 50%, #e2e8f0 75%)', backgroundSize: '200% 100%' }} />
                    </div>
                  ))
                ) : processedDecks.length === 0 && !loadingDecks ? (
                  <div style={{ display: 'flex', justifyContent: 'center', padding: '40px', color: 'var(--text-dark-muted)' }}>
                    {lang === 'pl' ? 'Brak talii spełniających wybrane kryteria.' : 'No decks match selected criteria.'}
                  </div>
                ) : (
                  <>
                  {processedDecks.map(deck => {
                    const stats = getDeckMetaStats(deck);
                    const isLocal = deck.source_url === 'local';
                    const curveData = getManaCurve(deck.cards);

                    return (
                      <div 
                        key={deck.id}
                        onClick={() => setExpandedDeckId(prev => prev === deck.id ? null : deck.id)}
                        className="deck-card"
                        style={{ 
                          padding: '16px 20px', 
                          cursor: 'pointer', 
                          display: 'flex', 
                          flexDirection: 'column', 
                          gap: '12px',
                          border: expandedDeckId === deck.id ? '2px solid var(--blue-hdt)' : '1px solid #e2e8f0',
                          boxShadow: expandedDeckId === deck.id ? '0 4px 20px rgba(2, 132, 199, 0.15)' : 'none'
                        }}
                      >
                        {/* Upper row: Avatar, Info, Stats, Copy Button */}
                        <div className="mob-deck-top-row" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          
                          {/* Avatar, Title, Class */}
                          <div style={{ display: 'flex', alignItems: 'center', gap: '16px', flex: 1 }}>
                            <span style={{ background: 'rgba(0,0,0,0.02)', width: '48px', height: '48px', borderRadius: '50%', display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
                              <img src={getClassIconUrl(deck.player_class)} style={{ width: '38px', height: '38px', objectFit: 'contain' }} />
                            </span>
                            <div>
                              <div style={{ fontWeight: '800', fontSize: '17px', color: 'var(--text-dark-main)', display: 'flex', alignItems: 'center', gap: '8px' }}>
                                {deck.title.split(/ #| - /)[0].trim()}
                              </div>
                              <div style={{ fontSize: '13px', marginTop: '4px', display: 'flex', alignItems: 'center', gap: '6px' }}>
                                {(() => {
                                  const missingDust = deck.dustCost || 0;
                                  const totalDeckDust = deck.totalDust || deck.cards?.reduce((sum, c) => sum + (c.count * (DUST_VALUES[c.rarity] || 40)), 0) || 0;
                                  
                                  if (deck.missingCount === 0) {
                                    return (
                                      <span style={{ color: '#22c55e', fontWeight: '700', display: 'flex', alignItems: 'center', gap: '4px' }}>
                                        <span style={{ fontSize: '16px' }}>✓</span>
                                        {lang === 'pl' ? 'Można złożyć' : 'Buildable'}
                                      </span>
                                    );
                                  }

                                  return (
                                    <span style={{ color: '#94a3b8', fontWeight: '600', display: 'flex', alignItems: 'center', gap: '4px' }}>
                                      <span style={{ color: '#38bdf8', fontSize: '14px', textShadow: '0 0 4px rgba(56,189,248,0.5)' }}>⚗️</span>
                                      {missingDust > 0 ? missingDust.toLocaleString() : totalDeckDust.toLocaleString()}
                                    </span>
                                  );
                                })()}
                              </div>
                            </div>
                          </div>

                          {/* Stats columns */}
                          <div className="mob-deck-stats-row" style={{ display: 'flex', gap: '40px', marginRight: '32px', textAlign: 'left' }}>
                            <div>
                              <div style={{ fontSize: '12px', color: 'var(--text-dark-muted)', textTransform: 'uppercase', fontWeight: '700', letterSpacing: '0.04em' }}>{t.winrate}</div>
                              <div style={{ fontSize: '18px', fontWeight: '800', color: 'var(--win)' }}>{stats.winrate}%</div>
                            </div>
                            <div>
                              <div style={{ fontSize: '12px', color: 'var(--text-dark-muted)', textTransform: 'uppercase', fontWeight: '700', letterSpacing: '0.04em' }}>{t.games}</div>
                              <div style={{ fontSize: '18px', fontWeight: '800', color: 'var(--text-dark-main)' }}>{stats.games.toLocaleString(lang === 'pl' ? 'pl-PL' : 'en-US')}</div>
                            </div>
                            <div>
                              <div style={{ fontSize: '12px', color: 'var(--text-dark-muted)', textTransform: 'uppercase', fontWeight: '700', letterSpacing: '0.04em' }}>{t.avgDuration}</div>
                              <div style={{ fontSize: '18px', fontWeight: '800', color: 'var(--text-dark-main)' }}>{stats.duration} min</div>
                            </div>
                          </div>

                          {/* Blue Copy Button */}
                          <button 
                            className="mob-deck-copy-btn"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleCopyCode(deck.deck_code, deck.id);
                            }}
                            style={{ 
                              padding: '8px 16px', background: 'var(--blue-hdt)', border: 'none', color: '#fff', 
                              fontWeight: '800', borderRadius: '4px', cursor: 'pointer', fontSize: '13px',
                              transition: 'opacity 0.15s'
                            }}
                            onMouseOver={e=>e.target.style.opacity=0.9}
                            onMouseOut={e=>e.target.style.opacity=1}
                          >
                            {copiedId === deck.id ? t.copied : t.copyDeck}
                          </button>
                        </div>

                        {/* Lower row: Cards Circles list + mini curve */}
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderTop: '1px solid var(--panel-border)', paddingTop: '10px' }}>
                          
                          {/* Cards circular preview */}
                          <div style={{ display: 'flex', gap: '3px', flexWrap: 'wrap', maxWidth: '85%' }}>
                            {deck.cards?.map((card, cardIdx) => {
                              const missing = card.owned < card.count;
                              const cardTitle = lang === 'pl' ? (card.name_pl || card.name) : (card.name_en || card.name);
                              return (
                                <div 
                                  key={cardIdx} 
                                  title={`${card.count}x ${cardTitle}`}
                                  style={{ 
                                    width: '32px', height: '32px', borderRadius: '50%',
                                    backgroundImage: `url(https://art.hearthstonejson.com/v1/tiles/${card.id}.png)`,
                                    backgroundSize: 'cover', backgroundPosition: 'center', position: 'relative',
                                    border: '2px solid rgba(255,255,255,0.15)',
                                    boxShadow: '0 2px 4px rgba(0,0,0,0.4)',
                                    filter: missing ? 'grayscale(100%) opacity(0.4)' : 'none'
                                  }}
                                >
                                  {/* Multiplier badge */}
                                  {card.count > 1 && (
                                    <span style={{ position: 'absolute', right: '-4px', bottom: '-4px', background: '#fbbf24', color: '#000', fontSize: '9px', fontWeight: '800', borderRadius: '50%', width: '13px', height: '13px', display: 'flex', justifyContent: 'center', alignItems: 'center', border: '1px solid #000' }}>
                                      2
                                    </span>
                                  )}
                                  {/* Legendary Star */}
                                  {card.rarity === 'LEGENDARY' && (
                                    <span style={{ position: 'absolute', left: '-2px', top: '-2px', color: '#fbbf24', fontSize: '10px', textShadow: '0 0 2px #000' }}>
                                      ★
                                    </span>
                                  )}
                                </div>
                              );
                            })}
                          </div>

                          {/* Mini Mana Curve Chart */}
                          <div style={{ display: 'flex', gap: '2px', alignItems: 'flex-end', height: '24px', width: '60px', flexShrink: 0 }}>
                            {curveData.map((count, i) => {
                              const max = Math.max(...curveData, 1);
                              const pct = (count / max) * 100;
                              return (
                                <div key={i} style={{ width: '5px', height: `${pct}%`, background: 'var(--gold)', borderRadius: '1px 1px 0 0' }} />
                              );
                            })}
                          </div>
                        </div>

                        {/* Expanded details (in-place accordion) */}
                        {expandedDeckId === deck.id && (
                          <div 
                            onClick={(e) => e.stopPropagation()} 
                            style={{ 
                              borderTop: '1px solid var(--panel-border)', 
                              paddingTop: '16px', 
                              marginTop: '12px', 
                              display: 'flex', 
                              flexDirection: 'column', 
                              gap: '20px', 
                              cursor: 'default',
                              animation: 'fadeIn 0.2s ease-out' 
                            }}
                          >
                            {/* Stats Curve & Details */}
                              <div className="mob-expanded-row" style={{ display: 'flex', gap: '24px', background: '#120e22', padding: '16px', borderRadius: '8px', border: '1px solid var(--panel-border)' }}>
                              {/* Mana Curve Bar Chart */}
                              <div style={{ flex: 1 }}>
                                <h4 style={{ fontSize: '12px', textTransform: 'uppercase', color: 'var(--gold-light)', marginBottom: '12px', letterSpacing: '0.05em', fontWeight: '800' }}>
                                  {t.manaCurve}
                                </h4>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', height: '110px', paddingBottom: '4px', borderBottom: '2px solid var(--panel-border)' }}>
                                  {curveData.map((count, index) => {
                                    const max = Math.max(...curveData, 1);
                                    const percent = Math.round((count / max) * 100);
                                    return (
                                      <div key={index} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', width: '12%', height: '100%', justifyContent: 'flex-end' }}>
                                        <span style={{ fontSize: '12px', fontWeight: '800', color: count > 0 ? '#f1f5f9' : 'transparent', marginBottom: '4px' }}>
                                          {count}
                                        </span>
                                        <div style={{ width: '28px', height: '70px', display: 'flex', alignItems: 'flex-end', background: 'rgba(255,255,255,0.05)', borderRadius: '4px 4px 0 0', overflow: 'hidden' }}>
                                          <div style={{ 
                                            width: '100%', 
                                            height: `${percent}%`, 
                                            minHeight: count > 0 ? '6px' : '0', 
                                            background: 'linear-gradient(to top, #d4af37, #ffd700)', 
                                            borderRadius: '3px 3px 0 0',
                                            transition: 'height 0.3s ease-out'
                                          }} />
                                        </div>
                                        <span style={{ fontSize: '11px', color: '#f1f5f9', marginTop: '6px', fontWeight: '800', background: 'rgba(255,255,255,0.08)', borderRadius: '50%', width: '18px', height: '18px', display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
                                          {index === 7 ? '7+' : index}
                                        </span>
                                      </div>
                                    );
                                  })}
                                </div>
                              </div>

                              {/* Composition Stats */}
                              <div style={{ width: '180px', borderLeft: '1px solid #cbd5e1', paddingLeft: '20px', display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: '6px' }}>
                                <h4 style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-dark-muted)', marginBottom: '4px', letterSpacing: '0.05em', fontWeight: '700' }}>
                                  {lang === 'pl' ? 'Statystyki' : 'Composition'}
                                </h4>
                                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px' }}>
                                  <span style={{ color: 'var(--text-dark-muted)' }}>{lang === 'pl' ? 'Stronnicy:' : 'Minions:'}</span>
                                  <span style={{ fontWeight: '800' }}>{getCompositionStats(deck.cards).Minion || 0}</span>
                                </div>
                                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px' }}>
                                  <span style={{ color: 'var(--text-dark-muted)' }}>{lang === 'pl' ? 'Zaklęcia:' : 'Spells:'}</span>
                                  <span style={{ fontWeight: '800' }}>{getCompositionStats(deck.cards).Spell || 0}</span>
                                </div>
                                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px' }}>
                                  <span style={{ color: 'var(--text-dark-muted)' }}>{lang === 'pl' ? 'Bronie:' : 'Weapons:'}</span>
                                  <span style={{ fontWeight: '800' }}>{getCompositionStats(deck.cards).Weapon || 0}</span>
                                </div>
                              </div>
                            </div>

                            {/* Cards Grid */}
                            <div>
                              <h4 style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-dark-muted)', marginBottom: '12px', letterSpacing: '0.05em', fontWeight: '700' }}>
                                {lang === 'pl' ? 'Spis Kart' : 'Deck List'}
                              </h4>
                              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                                {deck.cards?.map((card, idx) => {
                                  const missing = card.owned < card.count;
                                  return (
                                    <CardRow key={idx} card={card} missing={missing} lang={lang} />
                                  );
                                })}
                              </div>
                            </div>

                            {/* Mulligan Guide Table */}
                            <div style={{ borderTop: '1px solid #e2e8f0', paddingTop: '16px' }}>
                              <h4 style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-dark-muted)', marginBottom: '12px', letterSpacing: '0.05em', fontWeight: '700' }}>
                                {t.mulliganGuide}
                              </h4>
                              {loadingMulligan ? (
                                <div style={{ fontSize: '12px', color: 'var(--text-dark-muted)' }}>
                                  {lang === 'pl' ? 'Wczytywanie statystyk Mulligan...' : 'Loading Mulligan stats...'}
                                </div>
                              ) : !mulliganStats || mulliganStats.matchesCount === 0 ? (
                                <div style={{ border: '1px dashed #cbd5e1', padding: '12px', borderRadius: '6px', color: 'var(--text-dark-muted)', fontSize: '12px', textAlign: 'center' }}>
                                  {lang === 'pl' ? 'Brak rozegranych gier dla tej talii. Rozegraj mecze z trackerem, by wyliczyć Mulligan.' : 'No recorded matches for this deck. Play games with tracker to calculate Mulligan stats.'}
                                </div>
                              ) : (
                                <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: '6px', overflow: 'hidden' }}>
                                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '12px', textAlign: 'left' }}>
                                    <thead>
                                      <tr style={{ borderBottom: '1px solid #cbd5e1', color: 'var(--text-dark-muted)', background: 'rgba(0,0,0,0.02)' }}>
                                        <th style={{ padding: '8px 12px' }}>{t.cardName}</th>
                                        <th style={{ padding: '8px 12px', textAlign: 'right' }}>{t.keepRate}</th>
                                        <th style={{ padding: '8px 12px', textAlign: 'right' }}>{t.keptWinrate}</th>
                                      </tr>
                                    </thead>
                                    <tbody>
                                      {deck.cards?.map((card, idx) => {
                                        const cardStat = mulliganStats.cardStats[card.dbf_id] || { mulliganCount: 0, keptCount: 0, keptWins: 0 };
                                        const keep = cardStat.mulliganCount > 0 ? Math.round((cardStat.keptCount / cardStat.mulliganCount) * 100) : '-';
                                        const win = cardStat.keptCount > 0 ? Math.round((cardStat.keptWins / cardStat.keptCount) * 100) + '%' : '-';
                                        const cardTitle = lang === 'pl' ? (card.name_pl || card.name) : (card.name_en || card.name);
                                        return (
                                          <tr key={idx} style={{ borderBottom: '1px solid #f1f5f9' }}>
                                            <td style={{ padding: '8px 12px', fontWeight: '600' }}>{cardTitle}</td>
                                            <td style={{ padding: '8px 12px', textAlign: 'right', fontWeight: '700', color: '#b45309' }}>{keep !== '-' ? `${keep}%` : '-'}</td>
                                            <td style={{ padding: '8px 12px', textAlign: 'right', fontWeight: '800', color: win !== '-' ? (parseInt(win) >= 50 ? 'var(--win)' : 'var(--loss)') : 'var(--text-dark-muted)' }}>{win}</td>
                                          </tr>
                                        );
                                      })}
                                    </tbody>
                                  </table>
                                </div>
                              )}
                            </div>
                          </div>
                        )}

                      </div>
                    );
                  })}

                  {/* Sentinel div — triggers loading more via IntersectionObserver */}
                  <div ref={loadMoreRef} style={{ height: '40px', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                    {loadingDecks && decks.length > 0 && (
                      <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                        {[0, 0.2, 0.4].map((delay, i) => (
                          <div key={i} style={{ width: '7px', height: '7px', borderRadius: '50%', background: 'var(--blue-hdt)', animation: `pulse 1s ease-in-out ${delay}s infinite` }} />
                        ))}
                      </div>
                    )}
                    {!hasMoreDecks && decks.length > 0 && (
                      <span style={{ fontSize: '12px', color: 'rgba(148,163,184,0.5)' }}>
                        {lang === 'pl' ? `Załadowano wszystkie ${decks.length} talie` : `All ${decks.length} decks loaded`}
                      </span>
                    )}
                  </div>
                  </>
                )}
              </div>
            </div>
          )}

          {/* Tab 2: Visual Collection Manager */}
          {activeTab === 'collection' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }} className="animate-fade-in">
              <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: '8px', padding: '24px', display: 'flex', gap: '32px', alignItems: 'center' }}>
                <div style={{ textAlign: 'center' }}>
                  <span style={{ fontSize: '48px' }}>💎</span>
                  <div style={{ fontSize: '12px', textTransform: 'uppercase', color: '#64748b', marginTop: '8px', fontWeight: '800' }}>
                    {lang === 'pl' ? 'Wczytane karty' : 'Loaded cards'}
                  </div>
                  <div style={{ fontSize: '28px', fontWeight: '800', color: 'var(--gold-dark)' }}>{Object.keys(collection).length} pcs</div>
                </div>
                <div style={{ flex: 1 }}>
                  <h3 style={{ fontSize: '18px', marginBottom: '8px', color: '#0f172a' }}>{t.collectionManager}</h3>
                  <p style={{ fontSize: '14px', color: '#334155', lineHeight: '1.5', marginBottom: '12px' }}>
                    {lang === 'pl' 
                      ? <>Poniżej znajduje się pełna wizualna lista kart z Twojej kolekcji zczytana z <b>Hearthstone Deck Trackera</b>!</>
                      : <>Below is the full visual list of cards in your collection synced from <b>Hearthstone Deck Tracker</b>!</>
                    }
                  </p>
                  <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
                    <button
                      onClick={handleScanCollection}
                      style={{ padding: '10px 20px', background: 'var(--blue-hdt)', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: '800', fontSize: '14px', display: 'flex', alignItems: 'center', gap: '8px' }}
                    >
                      <span>🔍</span> {t.scanHDT}
                    </button>
                    <button
                      onClick={handleMarkAllOwned}
                      style={{ padding: '10px 20px', background: '#16a34a', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: '800', fontSize: '14px', display: 'flex', alignItems: 'center', gap: '8px' }}
                    >
                      <span>✓</span> {t.markAll100}
                    </button>
                  </div>
                </div>
              </div>

              {/* HDT Plugin Connection Status Banner */}
              {hdtConnected && (
                <div style={{ background: 'linear-gradient(135deg, #064e3b, #047857)', border: '1px solid #34d399', borderRadius: '8px', padding: '16px 20px', color: '#ecfdf5', fontWeight: '700', fontSize: '13px', display: 'flex', alignItems: 'center', gap: '12px', boxShadow: '0 4px 15px rgba(52, 211, 153, 0.15)' }}>
                  <span style={{ fontSize: '24px' }}>⚡</span>
                  <div>
                    <div style={{ fontSize: '14px', fontWeight: '800', color: '#6ee7b7' }}>
                      {lang === 'pl' ? '🟢 Oficjalny HDT Połączony Automatycznie!' : '🟢 Official HDT Connected Automatically!'}
                    </div>
                    <div style={{ fontSize: '12px', marginTop: '2px', opacity: 0.9 }}>
                      {lang === 'pl' 
                        ? 'Wtyczka wykryta w Twoim HDT. Wszystko synchronizuje się automatycznie w tle!' 
                        : 'Plugin detected in your HDT. Syncing automatically in the background!'}
                    </div>
                  </div>
                </div>
              )}

              {/* Visual Card Collection Grid with Search */}
              <div style={{ background: '#fff', border: '1px solid #e2e8f0', borderRadius: '8px', padding: '24px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '12px' }}>
                  <h3 style={{ fontSize: '16px', color: '#0f172a', margin: 0, fontWeight: '800' }}>
                    {lang === 'pl' ? '📜 Lista Zsynchronizowanych Kart' : '📜 Synced Cards List'} ({Object.keys(collection).length})
                  </h3>
                  <input
                    type="text"
                    placeholder={lang === 'pl' ? "Szukaj karty wg nazwy..." : "Search cards by name..."}
                    value={collSearch}
                    onChange={e => {
                      setCollSearch(e.target.value);
                      setVisibleCollCount(80);
                    }}
                    style={{
                      padding: '8px 14px', borderRadius: '6px', border: '1px solid #cbd5e1',
                      fontSize: '13px', width: '260px', outline: 'none', background: '#f8fafc'
                    }}
                  />
                </div>

                {/* Cards Grid */}
                {(() => {
                  let filtered = collectionCards.length > 0
                    ? [...collectionCards]
                    : Object.keys(collection).map(dbfId => ({
                        dbf_id: parseInt(dbfId, 10),
                        count: collection[dbfId],
                        owned: collection[dbfId],
                        id: `DBF_${dbfId}`,
                        name: `Card #${dbfId}`,
                        cost: 0,
                        rarity: 'COMMON'
                      }));

                  if (collSearch) {
                    const q = collSearch.toLowerCase();
                    filtered = filtered.filter(c => 
                      (c.name && c.name.toLowerCase().includes(q)) ||
                      (c.name_pl && c.name_pl.toLowerCase().includes(q)) ||
                      (c.name_en && c.name_en.toLowerCase().includes(q)) ||
                      String(c.dbf_id).includes(q)
                    );
                  }

                  // Sort by cost ascending, then name
                  filtered.sort((a, b) => (a.cost || 0) - (b.cost || 0) || (a.name || '').localeCompare(b.name || ''));

                  if (filtered.length === 0) {
                    return (
                      <div style={{ color: '#64748b', textAlign: 'center', padding: '30px', fontSize: '14px' }}>
                        {lang === 'pl' ? 'Brak kart spełniających kryteria.' : 'No cards found.'}
                      </div>
                    );
                  }

                  const sliced = filtered.slice(0, visibleCollCount);
                  const hasMore = filtered.length > visibleCollCount;

                  return (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '10px', maxHeight: '650px', overflowY: 'auto', paddingRight: '4px' }}>
                        {sliced.map(card => (
                          <CardRow key={card.dbf_id || card.id} card={card} lang={lang} />
                        ))}
                      </div>
                      {hasMore && (
                        <button
                          onClick={() => setVisibleCollCount(prev => prev + 100)}
                          style={{
                            padding: '10px 20px', background: '#f1f5f9', border: '1px solid #cbd5e1',
                            borderRadius: '6px', cursor: 'pointer', fontWeight: '800', fontSize: '13px',
                            color: '#334155', transition: 'all 0.2s', alignSelf: 'center', marginTop: '4px'
                          }}
                        >
                          {lang === 'pl' ? `➕ Pokaż więcej kart (wyświetlono ${sliced.length} z ${filtered.length})` : `➕ Show more cards (showing ${sliced.length} of ${filtered.length})`}
                        </button>
                      )}
                    </div>
                  );
                })()}
              </div>

              {/* Advanced / Developer Options (Token & Manual Import) */}
              <details style={{ background: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: '8px', padding: '16px' }}>
                <summary style={{ cursor: 'pointer', fontWeight: '800', color: '#475569', fontSize: '13px' }}>
                  ⚙️ {lang === 'pl' ? 'Zaawansowane: Token synchronizacji & Ręczny import JSON' : 'Advanced: Sync Token & Manual JSON Import'}
                </summary>
                
                <div style={{ marginTop: '16px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                  {/* Token panel */}
                  <div style={{ background: 'linear-gradient(135deg, #0f172a, #1e1b4b)', border: '1px solid #3730a3', borderRadius: '8px', padding: '16px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <span style={{ fontSize: '18px' }}>🔑</span>
                      <h4 style={{ fontSize: '14px', color: '#a5b4fc', margin: 0, fontWeight: '800' }}>
                        {lang === 'pl' ? 'Twój token synchronizacji' : 'Your sync token'}
                      </h4>
                    </div>
                    <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                      <code style={{
                        flex: 1, background: '#0f172a', border: '1px solid #334155', borderRadius: '6px',
                        padding: '8px 10px', fontSize: '12px', fontFamily: 'var(--font-mono)',
                        color: '#86efac', overflowX: 'auto', wordBreak: 'break-all'
                      }}>
                        {userToken}
                      </code>
                      <button
                        onClick={() => {
                          navigator.clipboard.writeText(userToken);
                          setTokenCopied(true);
                          setTimeout(() => setTokenCopied(false), 2000);
                        }}
                        style={{ padding: '8px 14px', background: tokenCopied ? '#16a34a' : '#4f46e5', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: '800', fontSize: '12px' }}
                      >
                        {tokenCopied ? '✓ ' + (lang === 'pl' ? 'Skopiowano!' : 'Copied!') : '📋 ' + (lang === 'pl' ? 'Kopiuj' : 'Copy')}
                      </button>
                    </div>
                  </div>

                  {/* Manual JSON */}
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                    <h4 style={{ fontSize: '14px', margin: 0, color: '#0f172a' }}>{t.manualJSON}</h4>
                    <textarea
                      value={collectionJsonText}
                      onChange={e => setCollectionJsonText(e.target.value)}
                      style={{ width: '100%', height: '140px', background: '#fff', border: '1px solid #cbd5e1', borderRadius: '6px', padding: '10px', fontSize: '12px', fontFamily: 'var(--font-mono)', outline: 'none' }}
                    />
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span style={{ color: 'var(--gold-dark)', fontWeight: '700', fontSize: '13px' }}>{collectionStatus}</span>
                      <button 
                        onClick={handleSaveCollection}
                        style={{ padding: '8px 16px', background: '#334155', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: '800', fontSize: '13px' }}
                      >
                        {t.saveCollection}
                      </button>
                    </div>
                  </div>
                </div>
              </details>
            </div>
          )}

        </main>
      </div>

      {/* Floating Buy Me a Coffee Cloud Banner Widget */}
      {showDonateWidget && (
        <div 
          className="mob-donate-widget"
          style={{ 
            position: 'fixed', 
            bottom: '24px', 
            right: '24px', 
            zIndex: 9999, 
            width: '340px', 
            background: 'linear-gradient(135deg, #1e293b, #0f172a)', 
            border: '2px solid #FFDD00', 
            borderRadius: '16px', 
            padding: '18px 20px', 
            boxShadow: '0 12px 35px rgba(0, 0, 0, 0.45), 0 0 20px rgba(255, 221, 0, 0.25)', 
            display: 'flex', 
            flexDirection: 'column', 
            gap: '12px',
            animation: 'fadeIn 0.3s ease-out',
            color: '#fff'
          }}
        >
          {/* Header row with title & close button */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span style={{ fontSize: '24px', filter: 'drop-shadow(0 2px 4px rgba(0,0,0,0.3))' }}>☕</span>
              <h4 style={{ margin: 0, fontSize: '15px', fontWeight: '800', color: '#FFDD00', letterSpacing: '-0.01em' }}>
                {t.widgetTitle}
              </h4>
            </div>
            <button
              onClick={() => setShowDonateWidget(false)}
              title="Close widget"
              style={{ 
                background: 'rgba(255, 255, 255, 0.1)', 
                border: 'none', 
                color: '#94a3b8', 
                fontSize: '16px', 
                fontWeight: '800', 
                width: '26px', 
                height: '26px', 
                borderRadius: '50%', 
                cursor: 'pointer', 
                display: 'flex', 
                justifyContent: 'center', 
                alignItems: 'center',
                transition: 'all 0.15s'
              }}
              onMouseOver={e => { e.target.style.background = 'rgba(239, 68, 68, 0.2)'; e.target.style.color = '#ef4444'; }}
              onMouseOut={e => { e.target.style.background = 'rgba(255, 255, 255, 0.1)'; e.target.style.color = '#94a3b8'; }}
            >
              ✕
            </button>
          </div>

          {/* Description */}
          <p style={{ margin: 0, fontSize: '12px', color: '#cbd5e1', lineHeight: '1.45' }}>
            {t.widgetDesc}
          </p>

          {/* CTA Button */}
          <a
            href="https://buymeacoffee.com/impacter"
            target="_blank"
            rel="noopener noreferrer"
            style={{
              width: '100%',
              padding: '10px 0',
              background: 'linear-gradient(135deg, #FFDD00, #facc15)',
              border: 'none',
              color: '#000000',
              fontWeight: '800',
              borderRadius: '8px',
              textAlign: 'center',
              fontSize: '13px',
              cursor: 'pointer',
              textDecoration: 'none',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '8px',
              boxShadow: '0 4px 12px rgba(250, 204, 21, 0.35)',
              transition: 'transform 0.15s ease'
            }}
            onMouseOver={e => e.target.style.transform = 'scale(1.02)'}
            onMouseOut={e => e.target.style.transform = 'scale(1)'}
          >
            <span>☕</span>
            <span>{t.widgetCTA}</span>
          </a>
        </div>
      )}

      {/* Subtle visitor counter footer */}
      <footer style={{
        background: 'rgba(10, 8, 20, 0.7)',
        borderTop: '1px solid rgba(46, 38, 70, 0.5)',
        padding: '8px 24px',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        fontSize: '11px',
        color: 'rgba(148, 163, 184, 0.55)',
        gap: '12px',
        flexWrap: 'wrap'
      }}>
        <span>© 2025 HS Rival Meta</span>
        {visitStats && (
          <span style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <span title={lang === 'pl' ? 'Łączna liczba odwiedzin' : 'Total visits'}>
              👁 {visitStats.total.toLocaleString(lang === 'pl' ? 'pl-PL' : 'en-US')} {lang === 'pl' ? 'odwiedzin' : 'visits'}
            </span>
            <span style={{ opacity: 0.4 }}>•</span>
            <span title={lang === 'pl' ? 'Dzisiaj' : 'Today'}>
              🟢 {visitStats.today} {lang === 'pl' ? 'dziś' : 'today'}
            </span>
          </span>
        )}
        <span style={{ opacity: 0.5 }}>{lang === 'pl' ? 'Dane z HSReplay.net' : 'Data from HSReplay.net'}</span>
      </footer>

    </div>
  );
}
