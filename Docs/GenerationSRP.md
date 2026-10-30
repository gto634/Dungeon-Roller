# Dungeon, managers et génération SRP

Les managers sont dans `Assets/_Game/Scripts/Managers`, hors des scripts du Dungeon.
Le prefab `Assets/_Game/Prefabs/Managers/GameManager.prefab` contient les trois managers
sur un GameObject extérieur au Dungeon. Dans la scène, ses références pointent vers
le `GeneratorBSP` et le Transform du Rigidbody du Player.

## Responsabilités

- `GameManager` est le singleton en Play Mode et le point d'entrée des mises à jour
  du Dungeon. Son `Update` appelle `RoomManager.RefreshPlayerRoom`, puis
  `DifficultyManager.AdvanceTime(Time.deltaTime)`. Les placements progressifs sont
  avancés par sa coroutine `UpdateDungeonRoutine`.
- `RoomManager` conserve le Transform du Player et l'index de sa salle. Il calcule
  l'appartenance avec `room.transform.InverseTransformPoint(player.position)` et les
  dimensions du sol de chaque salle. Il expose la salle courante et ses voisines
  directement reliées par un couloir. Il ne stocke ni horloge ni difficulté globale.
- `DifficultyManager` conserve la difficulté globale, le temps écoulé et le reste
  de la période courante. Il n'a pas d'`Update` autonome. Une hausse de difficulté
  est transmise par `GameManager` à `RoomManager.UpdateRooms`, qui demande la mise
  à jour des salles de son voisinage ; les tirages sont exécutés par GameManager.

La position X/Z du Player suffit ; aucun trigger n'est nécessaire. Les limites
supérieures d'une salle sont exclusives. L'index vaut -1 dans un couloir, hors des
salles, ou sans Player assigné. La difficulté peut continuer à augmenter dans un
couloir : elle sera rattrapée à l'entrée de la prochaine salle.

Les transformations du Dungeon (déplacement, rotation et échelle) sont prises en
compte. Le calcul de position ignore la hauteur Y, comme la grille 2D du Dungeon.

## Horloge et réglages

L'asset `Assets/_Game/Data/Managers/DifficultySettings.asset` définit :

- `Initial Difficulty` : niveau initial, 0 par défaut ;
- `Seconds Per Level` : durée pour gagner un niveau, 10 secondes par défaut.

`Horloge active` permet de suspendre la clock. Le temps de jeu est fourni par le
GameManager ; `Time.timeScale = 0` arrête donc la clock. Plusieurs périodes écoulées
dans une frame sont cumulées sans perdre le reste du temps. Désactiver/réactiver
le GameManager conserve l'horloge et reprend les tirages interrompus.

Hors Play Mode, l'horloge ne tourne pas automatiquement. L'Inspector de
GameManager regroupe les commandes de test, dont l'avancement manuel d'une période.

```csharp
GameManager game = GameManager.Instance;
game.Difficulty.IncreaseDifficulty(); // Hausse manuelle, même chemin de mise à jour.
game.GenerateDungeon();               // BSP + SRP, difficulté et horloge remises à zéro.
game.ClearDungeon();                  // Effacer les salles et les couloirs.
```

Les boutons du BSP passent également par son GameManager lorsqu'il est branché.
Une démonstration BSP/CA autonome reste utilisable sans ces managers.

La section **Génération** du GameManager et l'Inspector du BSP proposent uniquement
**Générer** et **Clear**. Générer produit le plan complet et le contenu SRP en une
seule action, y compris pour un Dungeon auparavant réglé en mode SRP manuel.
Avant de remplacer les salles, une génération valide annule les mises à jour en
attente et remet la difficulté globale et les compteurs de l'horloge à zéro.
Les nouvelles salles repartent à leur difficulté initiale (0 par défaut).
Une configuration BSP invalide ne réinitialise pas la partie existante.
Les outils de test des salles et de l'horloge restent dans leurs sections dédiées.

## Difficulté locale et coroutines

Chaque `Room` conserve la difficulté demandée ; son `SRPLayout` conserve le niveau
réellement appliqué, le nombre de tentatives et la fraction restante. Chaque niveau
ajouté apporte, pour chaque piège, `largeur × hauteur / x` tentatives, où `x` est
le dénominateur de sa probabilité. `Spike` est configuré à `1 / 50` par défaut.
Les fractions sont cumulées entre les niveaux et conservées lors de la sérialisation :
une salle de 125 cases cumule 2,5 tentatives par niveau, soit 2, 5, 7 puis 10 tentatives
aux niveaux 1 à 4 ; une salle de 150 cases ajoute 3 tentatives par niveau.
Une salle de 100 cases ajoute 2 tentatives par niveau ; un passage 2 → 5 y ajoute
donc 6 tentatives. Une salle déjà à jour ne refait pas de tirage.
Le voisinage est dédoublonné même si plusieurs segments de couloir relient les
mêmes salles. RoomManager active la salle courante et ses voisines, même si leur
difficulté est déjà à jour, et désactive les autres GameObjects de salle avec leur
contenu. Quand l'index vaut temporairement -1 dans une porte ou un couloir, l'état
actif des salles est conservé. Le voisinage actif change seulement lorsqu'un nouvel
index de salle valide est détecté. Sans salle courante, une hausse de difficulté
ne modifie pas non plus cet état ; le rattrapage attend l'entrée dans une salle.
La détection de position inclut les salles inactives pour les réactiver dès que le
Player y entre. Avant la première salle détectée, les salles gardent leur état initial.

GameManager traite une demande de salle par frame et avance chaque générateur SRP
actif d'un niveau par frame, avec les tentatives correspondant à sa surface.
Les générateurs du Dungeon sont explicitement pilotés
par GameManager ; ils ne démarrent pas une deuxième coroutine locale en parallèle.
Les demandes successives se regroupent vers la dernière cible. La désactivation
du GameManager suspend ces placements, qui reprennent à la réactivation.

Les objets déjà posés sont conservés en cas de baisse manuelle de difficulté.
Les mises à jour du voisinage ne diminuent jamais le niveau déjà demandé d'une salle.

`RoomUpdateEditorCoroutines`, dans `Managers/Editor`, avance les mêmes routines hors
Play Mode sans package supplémentaire. Les contrôles de difficulté des Inspectors
prennent en charge Undo/Redo et les overrides de prefab.

## Placement SRP et trace CA

Le BSP crée salles et couloirs. `GeneratorSRP` et `SRPLayout` préparent les données,
`VisualSRP` instancie les prefabs configurés dans les définitions de placement. La seed de chaque salle reste `seed BSP + index`.
Le SRP de production ne dépend d'aucun script CA.

- Les barrels sont tous générés au début, indépendamment de la difficulté.
  Leur probabilité est réglée dans `Barrel` : `1 / 37` par case par défaut, équivalent
  à la probabilité précédente `0.1 / (3 + 0.6 + 0.1)`.
- Chaque niveau ajouté augmente le budget de chaque piège de `surface / x`, selon
  sa propre probabilité. Seule la partie entière cumulée est tirée ; chaque piège
  conserve sa fraction indépendamment des autres, même si un niveau ne produit
  aucune tentative. Chaque tentative tire un seul emplacement. Une collision consomme
  la tentative sans ajouter de piège et ne remet pas la fraction à zéro.
- Les spikes occupent 2 × 2 cases, avec une marge de 1 case aux murs. Les barrels
  occupent 1 × 1 case. Les empreintes ne se chevauchent pas.
- Un seul modèle est placé au coin haut gauche. Pivots et échelles sont conservés.
  Un spike SRP est actif dès son placement : son prefab fonctionnel pilote ensuite
  ses pointes, sa hitbox et ses attaques. Barrels et sols ne sont pas recréés.

### Modifier les probabilités

Ouvrir `Assets/_Game/Data/Dungeon/SRP/Barrel.asset` ou `Spike.asset`. Dans l'Inspector,
modifier uniquement le nombre après **Probabilité : 1 /**. Un nombre plus grand
rend l'objet plus rare ; la valeur minimale est 1. Par exemple, `Spike` à `1 / 100`
ajoute une tentative par niveau dans une salle de 100 cases ; à `1 / 50`, il en ajoute 2.
Les cases accumulées dans le budget restent conservées lors d'un changement de réglage.

`SettingsSRP` référence seulement les barrels initiaux et la liste des pièges.
Il n'y a plus de `Weight`, de probabilité de barrel dupliquée ni de ratio fixé dans
le code. Chaque définition rassemble sa probabilité, son prefab, sa taille et sa
marge aux murs ; l'identifiant et la couleur de grille sont dans un volet repliable.
Retirer la référence Barrel désactive les barrels initiaux ; une liste de pièges
vide produit une salle sans pièges. Les changements s'appliquent aux prochaines
générations de barrels et aux prochains niveaux ajoutés pour les pièges.

Le Spike de production utilise `Prefabs/Dungeon/Trap/Spike.prefab`, avec le modèle
de `Art/Blender/Spikes.blend` et les matériaux du shader à motifs. Le prefab Barrel
actuel conserve son identité et utilise `Art/Blender/Barrel.blend`, sans nouvelle
logique. Voir [TrapPrefabs.md](TrapPrefabs.md) pour les pivots et les matériaux.

Le GameObject GeneratorCA est conservé dans le prefab de salle, désactivé et
déconnecté. Ses scripts, données et modèles restent présents. Le prefab
`Prefabs/Dungeon/CA/RoomCA.prefab` permet de montrer l'ancienne salle CA active ;
cette démonstration reste utilisable indépendamment du SRP.

Les scènes déjà générées conservent leur difficulté locale ; la difficulté globale
précédente est transférée au nouveau DifficultyManager pendant l'intégration.

## Panneau de test centralisé

Sélectionner le composant `GameManager` ouvre les commandes regroupées par domaine :

- **Génération BSP / SRP** : partition initiale, Next cut, génération complète,
  contenu SRP, mémorisation de la seed, effacement et génération SRP automatique.
- **Salles** : recalcul de la salle du Player, rattrapage de son voisinage, choix
  d'une salle précise et modification de sa difficulté demandée.
- **Météorites** : en mode Play, **Faire apparaître une météorite** crée immédiatement
  un `Meteor` dans la salle courante, même pendant la minute d'attente initiale.
  Cette commande conserve l'horloge et la progression de la fréquence automatique.
  Le bouton est désactivé sans salle valide ou sans MeteorManager actif.
- **Difficulté et horloge** : niveau global, hausse de difficulté, pause/reprise,
  avancement d'une période et remise à zéro de l'horloge.

`GameManagerEditorCommands` répertorie les commandes, leurs conditions de disponibilité
et leurs actions. `GameManagerEditor` construit l'interface UI Toolkit à partir de ce
catalogue et actualise les informations affichées sans faire tourner l'horloge en édition.
Les changements de difficulté et d'horloge prennent en charge Undo/Redo et les overrides
de prefab. Les générations remplacent le contenu comme les anciens boutons de génération.

Les Inspectors spécifiques de `RoomManager` et `DifficultyManager`, ainsi que le menu
contextuel de test de difficulté, sont retirés. Leurs références de configuration restent
éditables dans l'Inspector standard. Aucun composant supplémentaire n'est requis dans la
scène. Les outils autonomes BSP, Room/SRP et CA restent disponibles pour les démonstrations.

## Météorites dans Generation

La scène Generation contient un `MeteorManager` sous `--- Managers ---`, avec
son `Clock`, le prefab de météorite existant et une référence explicite au
`RoomManager`. `Spawn In Current Room` est activé dans cette scène.

Chaque expiration de la clock recalcule la salle du Player avant de tirer un point
de chute dans son repère local. La zone suit donc les dimensions et la transformation
de la salle courante. La marge aux murs vaut 1 case ; le rayon de la météorite est
également réservé. Sa taille aléatoire est réduite si nécessaire pour tenir dans la
salle. Une salle trop petite pour la marge ne reçoit pas de météorite.

La première météorite arrive après **60 secondes de jeu**. Ensuite, une seule
météorite apparaît à chaque expiration de la clock. L'intervalle commence à
**10 secondes**, diminue de **1 seconde toutes les 30 secondes** et reste au
minimum à **1 seconde** : 10 s à 1 min, 9 s à 1 min 30, 8 s à 2 min, etc.
Le nouvel intervalle s'applique aussi au compte à rebours déjà en cours, sans le
remettre à zéro. Il n'y a aucun rattrapage de plusieurs météorites dans une frame.
La hauteur reste comprise entre 10 et 14 unités, la vitesse entre 2 et 9 unités
par seconde. Les champs `Initial Delay`, `Interval Decrease Period`,
`Initial Spawn Interval`, `Interval Decrease` et `Minimum Spawn Interval`
permettent de modifier ces réglages dans l'Inspector.
Un changement de salle ne remet ni l'attente initiale ni la progression à zéro.
L'activation du MeteorManager démarre une nouvelle progression.
Aucun spawn n'a lieu dans un couloir, hors du Dungeon ou sans salle active.
Les météorites sont enfants de leur salle : elles suivent sa désactivation et sont
supprimées avec elle lors d'un Clear ou d'une nouvelle génération. Une météorite
déjà en chute conserve sa cible dans la salle précédente.

Le modèle apparaît au-dessus du point tiré ; l'ombre et la hitbox restent à ce point.
L'impact ne se déclenche qu'une fois, puis le système d'attaque détruit la météorite.
La pause par `Time.timeScale = 0` suspend l'horloge, la progression et la chute.
Le mode autonome de `Loic/PlayerControl` reste disponible lorsque
`Spawn In Current Room` est désactivé.

## Sol des salles

Seul le sol utilise `largeur - 2` en Scale X et `hauteur - 2` en Scale Z.
Il reste centré avec une marge d'une unité de chaque côté, sans bandes de sol
supplémentaires : il ne s'étend plus sous les murs ni dans les corridors.
Les dimensions et le Transform de la Room, ses murs et les points de raccordement
des corridors restent identiques. Une échelle minimale de 0,01 évite les valeurs
nulles ou négatives sur les salles trop petites.

`RoomTireTracks` reste lié au sol réduit et conserve le repérage des traces dans
les coordonnées de la salle.
