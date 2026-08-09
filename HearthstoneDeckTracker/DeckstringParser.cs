using System;
using System.Collections.Generic;
using System.IO;

namespace HearthstoneDeckTracker
{
    public class DeckContents
    {
        public int Format { get; set; }
        public List<int> HeroDbfIds { get; set; } = new List<int>();
        public Dictionary<int, int> CardCounts { get; set; } = new Dictionary<int, int>(); // dbfId -> count
    }

    public static class DeckstringParser
    {
        public static DeckContents Parse(string deckstring)
        {
            if (string.IsNullOrWhiteSpace(deckstring))
                throw new ArgumentException("Deckstring cannot be null or empty.");

            string cleanDeckstring = CleanDeckstring(deckstring);

            byte[] bytes = Convert.FromBase64String(cleanDeckstring);
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms))
            {
                // Reserved byte (always 0)
                byte reserved = reader.ReadByte();
                if (reserved != 0)
                {
                    // Some older formats or custom systems might differ, but standard is 0.
                    // We continue anyway, but log or check.
                }

                // Version (usually 1)
                int version = ReadVarint(reader);
                if (version != 1)
                {
                    // Format version mismatch, standard is 1.
                }
                
                var deck = new DeckContents();
                
                // Format
                deck.Format = ReadVarint(reader);
                
                // Heroes
                int numHeroes = ReadVarint(reader);
                for (int i = 0; i < numHeroes; i++)
                {
                    deck.HeroDbfIds.Add(ReadVarint(reader));
                }
                
                // Single cards (count = 1)
                int numSingle = ReadVarint(reader);
                for (int i = 0; i < numSingle; i++)
                {
                    int dbfId = ReadVarint(reader);
                    deck.CardCounts[dbfId] = 1;
                }
                
                // Double cards (count = 2)
                int numDouble = ReadVarint(reader);
                for (int i = 0; i < numDouble; i++)
                {
                    int dbfId = ReadVarint(reader);
                    deck.CardCounts[dbfId] = 2;
                }
                
                // Multi-copy cards (count = n)
                int numMulti = ReadVarint(reader);
                for (int i = 0; i < numMulti; i++)
                {
                    int dbfId = ReadVarint(reader);
                    int count = ReadVarint(reader);
                    deck.CardCounts[dbfId] = count;
                }
                
                return deck;
            }
        }

        private static string CleanDeckstring(string input)
        {
            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;
                
                // The first non-comment line should be the deck code
                return trimmed;
            }
            throw new FormatException("Could not find a valid deck code in the input.");
        }

        private static int ReadVarint(BinaryReader reader)
        {
            int result = 0;
            int shift = 0;
            while (true)
            {
                byte b = reader.ReadByte();
                result |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    break;
                shift += 7;
            }
            return result;
        }
    }
}
