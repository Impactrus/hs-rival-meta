using System; // Importuje podstawowe biblioteki systemowe, w tym system zdarzeń (Action).
using UnityEngine; // Importuje bibliotekę Unity (obsługuje m.in. MonoBehaviour, Vector3, Colidery).

namespace CCG.UI // Umieszcza ten skrypt w przestrzeni nazw dedykowanej dla interfejsu użytkownika (UI).
{ // Otwarcie bloku przestrzeni nazw CCG.UI.
    [RequireComponent(typeof(Collider))] // Zapewnia, że Unity automatycznie doda Collider (np. Box Collider), jeśli go nie ma.
    public class ThreeDButton : MonoBehaviour // Tworzy publiczną klasę ThreeDButton dziedziczącą po MonoBehaviour (klasa obiektów sceny).
    { // Otwarcie bloku klasy ThreeDButton.
        [Header("Ustawienia Ruchu")] // Wyświetla podsekcję w panelu edytora Unity.
        [SerializeField] private Vector3 hoverOffset = new Vector3(0, 0.1f, 0); // Wektor przesunięcia przycisku w górę po najechaniu myszką.
        [SerializeField] private Vector3 clickOffset = new Vector3(0, -0.05f, 0); // Wektor wciśnięcia przycisku w dół po kliknięciu.
        [SerializeField] private float animationSpeed = 15f; // Prędkość płynnej zmiany pozycji przycisku.

        [Header("Ustawienia Koloru")] // Wyświetla drugą podsekcję w panelu edytora Unity.
        [SerializeField] private Renderer buttonRenderer; // Odnośnik do komponentu renderującego (rysującego) obiekt 3D.
        [SerializeField] private Color hoverColor = Color.white; // Kolor, na jaki zmieni się przycisk przy najechaniu myszką.
        private Color originalColor; // Zmienna do zapamiętania domyślnego koloru materiału.

        public event Action OnClicked; // Zdarzenie wywoływane w momencie pełnego kliknięcia w przycisk.

        private Vector3 startPosition; // Pozycja początkowa przycisku w lokalnej przestrzeni.
        private Vector3 targetPosition; // Pozycja, do której przycisk aktualnie dąży w ruchu.
        private Material buttonMaterial; // Referencja do stworzonego na obiekcie materiału graficznego.

        private void Start() // Uruchamia się raz, gdy obiekt pojawia się w grze.
        { // Otwarcie bloku metody Start.
            startPosition = transform.localPosition; // Zapisuje aktualną lokalną pozycję startową obiektu w przestrzeni 3D.
            targetPosition = startPosition; // Ustala, że początkowym celem ruchu jest pozycja startowa.

            if (buttonRenderer != null) // Sprawdza, czy w edytorze przypisano komponent Renderer.
            { // Otwarcie bloku warunkowego.
                buttonMaterial = buttonRenderer.material; // Pobiera unikalną kopię materiału z renderera obiektu.
                originalColor = buttonMaterial.color; // Zapamiętuje pierwotny kolor materiału, aby móc do niego wrócić.
            } // Zamknięcie bloku warunkowego.
        } // Zamknięcie bloku metody Start.

        private void Update() // Uruchamia się w każdej klatce gry (np. 60-120 razy na sekundę).
        { // Otwarcie bloku metody Update.
            // Vector3.Lerp płynnie przybliża transform.localPosition do targetPosition na podstawie czasu (Time.deltaTime) i prędkości.
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * animationSpeed);
        } // Zamknięcie bloku metody Update.

        private void OnMouseEnter() // Wywoływane przez Unity automatycznie, gdy kursor myszy wejdzie w obszar Collidera.
        { // Otwarcie bloku metody OnMouseEnter.
            targetPosition = startPosition + hoverOffset; // Zmienia cel ruchu na pozycję startową powiększoną o przesunięcie najechania.
            if (buttonMaterial != null) // Sprawdza, czy pobrano poprawnie materiał obiektu.
            { // Otwarcie bloku warunkowego.
                buttonMaterial.color = hoverColor; // Zmienia kolor przycisku na kolor podświetlenia.
            } // Zamknięcie bloku warunkowego.
        } // Zamknięcie bloku metody OnMouseEnter.

        private void OnMouseExit() // Wywoływane przez Unity, gdy kursor myszy opuści obszar Collidera przycisku.
        { // Otwarcie bloku metody OnMouseExit.
            targetPosition = startPosition; // Zmienia cel ruchu z powrotem na pozycję wyjściową (startPosition).
            if (buttonMaterial != null) // Sprawdza, czy materiał przycisku istnieje.
            { // Otwarcie bloku warunkowego.
                buttonMaterial.color = originalColor; // Przywraca oryginalny kolor sprzed najechania myszką.
            } // Zamknięcie bloku warunkowego.
        } // Zamknięcie bloku metody OnMouseExit.

        private void OnMouseDown() // Wywoływane przez Unity w momencie kliknięcia lewym przyciskiem myszy na Colliderze.
        { // Otwarcie bloku metody OnMouseDown.
            targetPosition = startPosition + clickOffset; // Zmienia cel ruchu na pozycję startową z wciśnięciem (clickOffset).
        } // Zamknięcie bloku metody OnMouseDown.

        private void OnMouseUpAsButton() // Wywoływane przez Unity, gdy przycisk myszy zostanie puszczony nad tym samym Colliderem.
        { // Otwarcie bloku metody OnMouseUpAsButton.
            targetPosition = startPosition + hoverOffset; // Ustawia cel ruchu z powrotem na pozycję najechania (hover).
            OnClicked?.Invoke(); // Wywołuje podpięte pod to zdarzenie funkcje innych skryptów, jeśli jakieś istnieją (OnClicked != null).
            Debug.Log($"Kliknięto przycisk 3D: {gameObject.name}"); // Wypisuje w konsoli Unity nazwę klikniętego przycisku.
        } // Zamknięcie bloku metody OnMouseUpAsButton.
    } // Zamknięcie bloku klasy ThreeDButton.
} // Zamknięcie bloku przestrzeni nazw CCG.UI.
