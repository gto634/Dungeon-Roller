# Conduite, dérapage et maniabilité

`Movement` conserve les commandes avant/arrière, l'accélération et le nitro.
Le braquage et l'adhérence s'appliquent dans FixedUpdate. En conduite normale,
le changement de direction entraîne la vitesse horizontale avec la voiture :
un virage ne crée plus de glissade latérale. Les impacts latéraux restent possibles
et l'adhérence les amortit. La composante verticale de vitesse reste conservée.

## Braquage avec la vitesse

Le braquage démarre progressivement entre 0 et 3 unités/s pour éviter de tourner
sur place, puis devient moins efficace à grande vitesse. La référence de base
est de 20 unités/s. Le facteur à pleine mobilité est `1 / (1 + (v / référence)²)`.
La vitesse utilisée est la vitesse horizontale réelle, y compris en marche arrière.
La marche arrière inverse le sens du braquage comme auparavant. Le mécanisme
d'alignement avec les murs garde son fonctionnement.

La stat sérialisée existante `_maniability` est conservée dans `Stats`, avec le
libellé **Maniabilité**. Chaque Player en copie sa valeur au démarrage. Chaque
unité de jeu (100 points affichés) ajoute 20 % de la référence de base à la référence de virage :
`référence = 20 × (1 + 0,2 × maniabilité)`. Le gain agit surtout à grande vitesse ;
il ne multiplie pas la vitesse de rotation maximale à l'arrêt.

`Movement.Maniability` expose la valeur de la partie et `AddManiability(float)`
permet de l'améliorer sans modifier le ScriptableObject partagé. La montée de
niveau peut proposer **Maniabilité** parmi les sept améliorations possibles,
en utilisant les trois cartes existantes.

## Dérapage maintenu

L'action **PlayerMovement/Drift** utilise **Shift** (gauche ou droit) au clavier
et **RT/R2** (`rightTrigger`) à la manette. Le prefab Player référence explicitement
cette action sur InputControl. Le nitro conserve son action `Player/Sprint`,
désormais liée à **Espace** et **LT/L2** (`leftTrigger`). Les GUID et identifiants
des actions existantes sont conservés ; les deux commandes peuvent être maintenues
ensemble. Les maps sont déjà activées par les actions globales du projet.

Pendant le dérapage, le véhicule braque sans entraîner immédiatement sa vitesse
dans cette direction. La résistance latérale passe de 18/s à 0,7/s et le braquage
est multiplié par 1,25. La réduction du braquage à grande vitesse reste active.
L'effet exige une vitesse horizontale d'au moins 2 unités/s. Relâcher la touche
rend l'adhérence normale ; la voiture récupère progressivement son accroche.

`InputControl` lit les actions existantes et la nouvelle action à chaque frame,
sans créer de callbacks persistants. Sa désactivation ou une pause efface les
commandes. Après réactivation, l'état réel des touches est relu : une touche
relâchée pendant une pause ne laisse pas un dérapage bloqué. `Movement.Stop()`
et la désactivation de Movement effacent aussi la demande de dérapage.

## Visuel et réglages

`VehicleVisualLean`, `WheelRotation` et `VehicleTireTracks` continuent d'utiliser
la vitesse et les contacts des pneus. Une glissade importante peut incliner le
modèle ; les roues soulevées restent visibles, sans rotation ni traces. La
cinématique du Spring suspend toujours les commandes et remet le modèle à plat.

Les paramètres sont réglables sur le Movement du prefab Player : `Lateral Friction`,
`Steering Speed Reference`, `Full Steering Speed`, `Handling Bonus Per Point`,
`Drift Lateral Friction`, `Drift Steering Multiplier` et `Minimum Drift Speed`.
Le champ `Maniability Upgrade` de LevelUpSystem règle le gain par amélioration.


## Départ et accélération

PlayerStats sérialise 640 points de vitesse maximale et 52 points d'accélération.
Stats reconvertit ces valeurs en 6,4 et 0,52 unités de jeu : le changement d'échelle
reste visuel. La maniabilité de départ est ramenée à 40 points (0,4 unité).
Movement copie ces valeurs pour chaque partie. La poussée s'applique dans FixedUpdate : chaque point d'accélération
produit 10 unités/s², réglable avec Acceleration Per Point. La stat d'accélération
agit sur le temps nécessaire pour atteindre la vitesse maximale, indépendamment
de cette limite. Une amélioration d'accélération augmente donc effectivement la
reprise. Les améliorations ne modifient pas le ScriptableObject partagé.

La poussée compense le frottement longitudinal pendant la conduite pour atteindre
la vitesse affichée. Après relâchement, les frottements et le braquage pendant la
décélération sont conservés. Le nitro relève la limite de 10 %. Une impulsion qui
dépasse cette limite est amortie naturellement, sans coupure brutale de vitesse.


Les valeurs de stats, hors PV, utilisent 100 points pour 1 unité de jeu. Stats conserve
ses accesseurs publics en unités de jeu et gère la lecture des anciens assets
non convertis. Les cartes reconvertissent le gain affiché avant application :
+20 points de vitesse ou d'accélération appliquent +0,2. Les PV restent en valeurs directes, sans conversion ; la défense
reste entière dans le gameplay et utilise des gains multiples de 100 points.

Le nitro commence avec une capacité et une réserve nulles. Une carte de nitro
ajoute un multiple de 20 points à la capacité et accorde cette nouvelle réserve.
La consommation est de 10 points/s et la recharge de 5 points/s. Ainsi +20 offre
2 secondes d'utilisation. Après épuisement, la réserve doit se recharger avant
de reprendre. Les taux se règlent sur Movement. La chance et les raretés sont
décrites dans LuckAndRarities.md.
