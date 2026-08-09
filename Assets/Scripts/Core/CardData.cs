using UnityEngine; // Importuje bibliotekę Unity, aby móc korzystać z gotowych klas silnika (jak ScriptableObject, Sprite, itp.).

namespace CCG.Core // Organizuje nasz kod w logiczny folder (przestrzeń nazw), co ułatwia zarządzanie i zapobiega konfliktom nazw.
{ // Otwarcie bloku przestrzeni nazw CCG.Core. Wszystko w środku należy do tej grupy.
    public enum CardType // Tworzy nową listę stałych opcji (typ wyliczeniowy) reprezentującą rodzaje kart w grze.
    { // Otwarcie bloku z opcjami typu karty.
        Minion, // Opcja 0: Stronnik (jednostka bojowa, która stoi na stole i walczy).
        Spell, // Opcja 1: Czar (karta z jednorazowym efektem magicznym, która od razu trafia na cmentarz).
        Weapon // Opcja 2: Broń (karta dająca bohaterowi możliwość bezpośredniego ataku).
    } // Zamknięcie bloku opcji typu karty.

    [CreateAssetMenu(fileName = "NewCard", menuName = "CCG/Card Data")] // Umożliwia tworzenie plików z danymi kart przez menu Unity (prawy klik -> Create -> CCG -> Card Data).
    public class CardData : ScriptableObject // Deklaruje publiczną klasę CardData dziedziczącą po klasie ScriptableObject (stała baza danych na dysku).
    { // Otwarcie bloku klasy CardData.
        [Header("Podstawowe Informacje")] // Rysuje w edytorze Unity pogrubiony napis-nagłówek dla ułatwienia organizacji.
        public string cardId; // Zmienna tekstowa przechowująca unikalny identyfikator techniczny karty (np. "dragon_01").
        public string cardName; // Zmienna tekstowa przechowująca nazwę karty pokazywaną graczowi na ekranie (np. "Czerwony Smok").
        
        [TextArea(2, 5)] // Zmienia pole tekstowe w edytorze Unity na duże okienko do pisania (od 2 do 5 linijek wysokości).
        public string description; // Zmienna tekstowa przechowująca opis zdolności karty (np. "Zadaj 2 obrażenia wrogom").
        
        public Sprite cardArt; // Referencja do pliku graficznego (obrazka), który będzie widniał na karcie.
        public CardType cardType; // Przechowuje typ karty, który wybieramy z naszej zdefiniowanej wyżej listy (Minion/Spell/Weapon).

        [Header("Statystyki")] // Rysuje kolejny nagłówek sekcji w edytorze Unity.
        public int manaCost; // Zmienna przechowująca liczbę całkowitą (int) oznaczającą koszt many potrzebnej do zagrania karty.
        public int attack; // Zmienna oznaczająca startową siłę ataku (punkty obrażeń, jakie zadaje ta karta).
        public int maxHealth; // Zmienna oznaczająca maksymalną startową ilość zdrowia jednostki (lub wytrzymałość broni).
    } // Zamknięcie bloku klasy CardData.
} // Zamknięcie bloku przestrzeni nazw CCG.Core.
