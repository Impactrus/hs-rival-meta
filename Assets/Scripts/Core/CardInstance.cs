using System; // Importuje podstawowy system C# (niezbędny do obsługi podstawowych typów danych).

namespace CCG.Core // Umieszcza ten skrypt w tej samej przestrzeni nazw (folderze kodu) co pozostałe pliki rdzenia gry.
{ // Otwarcie bloku przestrzeni nazw CCG.Core.
    public class CardInstance // Deklaruje publiczną klasę CardInstance (reprezentację karty w trakcie gry).
    { // Otwarcie bloku klasy CardInstance.
        public CardData Data { get; private set; } // Właściwość przechowująca referencję do statycznych danych karty (szablonu).
        
        // Zmienne dynamiczne (runtime stats) – wartości, które zmieniają się w trakcie rozgrywki:
        public int CurrentManaCost { get; set; } // Zmienna przechowująca aktualny koszt many tej konkretnej karty.
        public int CurrentAttack { get; set; } // Zmienna przechowująca aktualną siłę ataku tej karty na stole.
        public int CurrentHealth { get; set; } // Zmienna przechowująca aktualne punkty życia karty na stole.
        public int MaxHealth { get; set; } // Zmienna przechowująca maksymalną ilość życia karty (uwzględnia ewentualne bonusy).
 
        public bool CanAttack { get; set; } // Zmienna logiczna (true/false) określająca, czy ta karta może zaatakować w bieżącej turze.
        public bool IsSilenced { get; set; } // Zmienna logiczna określająca, czy karta została wyciszona (straciła efekty).
 
        public CardInstance(CardData data) // Konstruktor klasy: funkcja wywoływana przy tworzeniu nowej karty w grze.
        { // Otwarcie bloku konstruktora.
            Data = data; // Przypisuje przekazane dane startowe z szablonu do właściwości Data.
            ResetStats(); // Wywołuje funkcję przepisywania statystyk startowych z szablonu na zmienne gry.
        } // Zamknięcie bloku konstruktora.
 
        public void ResetStats() // Funkcja (metoda) przywracająca statystyki karty do wartości bazowych z szablonu.
        { // Otwarcie bloku metody ResetStats.
            CurrentManaCost = Data.manaCost; // Ustawia aktualny koszt many na koszt domyślny z pliku danych karty.
            CurrentAttack = Data.attack; // Ustawia aktualną siłę ataku na atak domyślny z pliku danych karty.
            CurrentHealth = Data.maxHealth; // Ustawia aktualne punkty życia na życie domyślne z pliku danych karty.
            MaxHealth = Data.maxHealth; // Ustawia maksymalne punkty życia na życie domyślne z pliku danych karty.
            CanAttack = false; // Nowa lub zresetowana karta domyślnie nie może od razu atakować (summon sickness).
            IsSilenced = false; // Nowo stworzona karta nie jest na początku meczu wyciszona.
        } // Zamknięcie bloku metody ResetStats.
 
        public void TakeDamage(int amount) // Funkcja zadawania obrażeń karcie. Przyjmuje ilość obrażeń jako parametr.
        { // Otwarcie bloku metody TakeDamage.
            CurrentHealth -= amount; // Odejmuje otrzymane obrażenia od aktualnego zdrowia karty (CurrentHealth = CurrentHealth - amount).
            if (CurrentHealth < 0) // Sprawdza, czy po odjęciu obrażeń zdrowie karty spadło poniżej zera.
            { // Otwarcie bloku warunkowego.
                CurrentHealth = 0; // Jeśli tak, ustawia zdrowie dokładnie na 0 (zapobiega ujemnemu zdrowiu).
            } // Zamknięcie bloku warunkowego.
        } // Zamknięcie bloku metody TakeDamage.
 
        public void Heal(int amount) // Funkcja leczenia karty. Przyjmuje ilość punktów leczenia jako parametr.
        { // Otwarcie bloku metody Heal.
            CurrentHealth += amount; // Dodaje punkty leczenia do aktualnego zdrowia karty (CurrentHealth = CurrentHealth + amount).
            if (CurrentHealth > MaxHealth) // Sprawdza, czy po uleczeniu zdrowie przekroczyło maksymalny dopuszczalny limit karty.
            { // Otwarcie bloku warunkowego.
                CurrentHealth = MaxHealth; // Jeśli tak, obcina zdrowie dokładnie do maksymalnej wartości karty (nie można przeleczyć).
            } // Zamknięcie bloku warunkowego.
        } // Zamknięcie bloku metody Heal.
    } // Zamknięcie bloku klasy CardInstance.
} // Zamknięcie bloku przestrzeni nazw CCG.Core.
