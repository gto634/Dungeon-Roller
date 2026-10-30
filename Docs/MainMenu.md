# Accueil 3D

La scène `Assets/_Game/Scenes/MainMenu.unity` est la première scène activée dans
les Build Settings. Generation reste la scène de jeu suivante et peut toujours
être ouverte directement pour travailler sur le donjon.

La caméra cadre la voiture de face, avec les boutons Play et Exit devant le
pare-brise et un décor utilisant les assets existants : sol, mur identique à ceux de Base et
barrels. La voiture est un modèle décoratif, sans Movement, Rigidbody ou
GameManager. Aucun donjon ni danger n'est lancé pendant l'accueil.

```text
--- Managers ---
  MainMenu
--- Environment ---
  Main Camera
  Display Car
  Sun
  Floor
  Back Wall
  Barrel...
--- Canvas ---
  Windshield Menu
    Title
    Buttons (Canvas World Space)
      Play
        Label
      Exit
        Label
  EventSystem
```

Play et Exit sont des boutons uGUI présents dans la scène avant Play Mode,
sur un Canvas en **World Space** incliné avec le pare-brise. Ils reprennent
l'Image violette, le sprite à motif, le matériau UIPattern, les couleurs de
survol et la police Determination des boutons habituels du GameUI. Les labels
utilisent TextMeshProUGUI. Les anciennes plaques, cadres et colliders 3D sont
remplacés par ces éléments UI ; le titre et le décor existants sont conservés.

Le GraphicRaycaster utilise la caméra du menu. Un EventSystem et son
InputSystemUIInputModule reprennent la configuration d'entrée du GameUI et
gèrent le pointeur, le clic, le survol et la navigation clavier/manette.
Play est sélectionné initialement ; Haut/Bas permettent de choisir Exit et de
revenir à Play. Les onClick sérialisés appellent MainMenuController.Play/Exit.
Le survol utilise la teinte habituelle des boutons et conserve leur position.

Le menu suit la position, la rotation et l'échelle de Vitrages_Habitacle. Il ne
s'oriente pas automatiquement face à la caméra : il reste aligné sur la vitre.
Le cadrage conserve une ouverture horizontale stable en 16:9, 4:3 et 21:9.

Play remet Time.timeScale à 1 et charge Generation en mode Single. Les clics
supplémentaires sont ignorés pendant le chargement. Exit appelle Application.Quit
dans un build et arrête Play Mode dans l'éditeur.

Les tests vérifient le style partagé, les boutons et leur Canvas en espace monde,
les deux cibles du raycast UI, le suivi de la vitre, le survol par une souris
simulée, la navigation, les formats d'écran et la transition vers une partie
générée. Les actions sont désactivées pendant la transition. La vérification
visuelle distincte active également Exit par le pointeur UI pour confirmer la
sortie de Play Mode.

## Capot et crédits

Capot_AV possède un pivot à son bord arrière, près du pare-brise, dans Player.prefab
et sur Display Car dans le menu. Car_HoodHinged.asset reprend le maillage d'origine
avec ses sommets translatés, et sa position locale compense exactement ce décalage :
la géométrie fermée, les matériaux et le FBX source sont conservés. Le prefab Player
reçoit uniquement cette correction de pivot, sans logique de menu ni collider ajouté.

MainMenuHoodCredits est enregistré sur Display Car dans MainMenu.unity. Le survol
soulève le capot de 7 degrés et le clic l'ouvre à 65 degrés, avec une interpolation
amortie indépendante de Time.timeScale. Un second clic le referme. Un volume de
survol fixe évite le clignotement quand le capot se soulève ; le capot mobile reste
également cliquable. Les raycasts utilisent la caméra du menu et respectent les
éléments UI. Le GraphicRaycaster prend les obstacles 3D en compte.

Hood Credits, sous Capot_AV, contient trois objets TextMeshPro 3D : MAHIOU Ryad,
BRUCHET Loïc et BOULANGER Julien. Leurs noms sont visibles sur la face intérieure
du capot lorsqu'il est ouvert, avec un fond sombre et une indication de fermeture.
Les objets existent en édition et restent masqués au départ. La désactivation et
le rechargement de la scène rétablissent le capot fermé. Les références, angles
et paramètres d'animation sont modifiables dans l'Inspector de MainMenuHoodCredits.