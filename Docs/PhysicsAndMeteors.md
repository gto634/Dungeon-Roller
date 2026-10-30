# Météores, corridors et glissement contre les murs

L'échelle initiale des météores varie de 1,5 à 2,5. Après leur délai initial de
120 secondes, cette plage augmente continûment avec la difficulté de leur phase :
+0,1 par période de 10 secondes par défaut, avec un plafond absolu de 4 sur les
trois axes. La progression partage Initial Delay et Interval Decrease Period
avec l'accélération des vagues ; elle continue donc après l'arrêt de la difficulté
des pièges et lorsque l'intervalle minimal d'apparition est atteint.
Scale Increase Per Period, sur MeteorManager, règle la croissance (zéro la coupe).
La pause suspend cette progression et la remise à zéro de l'horloge rétablit la
plage initiale. La taille est choisie à l'apparition : les météores déjà en chute
conservent leur échelle. Leur empreinte respecte la marge des murs ; une salle
trop étroite pour la taille minimale courante est ignorée.
Le nombre par vague dépend de la surface de la salle, avec report du reste
d'arrondi, comme pour les pièges. Les scènes et le prefab Meteor sont conservés.

Les fichiers et dossiers `Meteorite` et `Meteorit` sont renommés en `Meteor`.
Les types deviennent `MeteorManager` et `MeteorBrain`. Les références dans les
scripts, l'éditeur, les scènes, les prefabs et les tests suivent ces noms. Les
fichiers meta conservent leurs GUID ; MovedFrom et FormerlySerializedAs
conservent la compatibilité des types et champs renommés.

Le prefab Corridor utilise trois BoxColliders physiques continus sous
Corridor/Physic : Floor, Wall Left et Wall Right. Ils couvrent le
tronçon central et les deux portes. Les anciens groupes de collisions séparés
et les colliders des linteaux sont supprimés ; le toit visible reste sans
collision. Les deux triggers de passage sont conservés sous Physic. Action/Passage détecte
leur consommation et publie OnDoorPassed(direction) ; les effets et les récompenses
sont abonnés séparément. Voir CorridorActions.md pour le câblage du UnityEvent.

Les trois volumes ont des dimensions fixes enregistrées dans le prefab :
longueur de 4,002 unités, largeur utile de 3 et murs de 1 unité d'épaisseur.
CorridorVisual les oriente avec le modèle, sans modifier leurs dimensions.
Le sol reste à y = 0, comme celui des salles, avec un léger recouvrement aux extrémités. Son ancien sommet à environ 0,03843 créait
une marche heurtant les colliders bas du joueur. La géométrie visible et les
liaisons des indicateurs de porte sont conservées. CorridorVisual remplace les
anciens modules de scène qui n'ont pas les nouvelles références de colliders.

WallSlide est un PhysicsMaterial sans frottement et sans rebond, avec combinaison
Minimum. Il est affecté aux murs des salles et aux deux parois continues des
corridors. Les matériaux des sols sont conservés. Un impact oblique perd sa
composante vers le mur et conserve son déplacement parallèle au mur.

La reproduction avant correction bloquait les 94 traversées testées sur le
donjon de seed 1806, et annulait une vitesse parallèle de 3 lors de l'impact.
Après correction, les 94 traversées passent et cette vitesse est conservée.
Les tests permanents couvrent les passages dans les deux sens, plusieurs
incidences dans les quatre directions, l'arrêt frontal et le contact immobile.

## Friction de la voiture

Le Rigidbody du Player utilise une physique sur X/Z : gravité désactivée et
position Y bloquée. Movement applique aussi ces réglages au démarrage, conserve
les contraintes de rotation et retire le Linear Damping uniforme.

Le frottement est calculé à chaque pas physique selon l'orientation de la voiture.
Rolling Friction ralentit les mouvements avant et arrière de la même manière ;
Lateral Friction réduit le glissement sur les côtés. Les valeurs du prefab Player
sont respectivement 0,5 et 5 par seconde : l'adhérence latérale est dix fois plus
forte que la résistance au roulement. Ces deux valeurs sont modifiables dans
Movement, sous Friction. Une valeur nulle désactive la résistance sur cet axe.

Le freinage fonctionne même sans contact avec le sol et n'ajoute aucune force
verticale. Il s'arrête avec les pas physiques pendant la pause. Les collisions,
le recul des pièges, l'accélération et la marche arrière restent actifs.

## Direction et chocs

La rotation physique autour de Y est bloquée sur le Player et au démarrage de
Movement. Les collisions conservent leur recul linéaire mais ne peuvent plus
laisser la voiture tourner sans fin.

VehicleWallContact transmet les contacts des murs des salles et corridors à
Movement. Un choc ou une accélération vers le mur déclenche une déviation vers
sa tangente la plus proche. La rotation s'arrête dès que la voiture est parallèle,
même si le contact s'est interrompu pendant cette courte déviation.

Wall Alignment Speed, sous Wall Collisions dans Movement, règle cette rotation
(180 degrés par seconde par défaut ; zéro la désactive). Une voiture immobile
ne pivote pas au simple contact, et le sol ainsi que les obstacles dynamiques
ne déclenchent pas cette aide. Dans un coin entre deux murs perpendiculaires,
l'aide s'interrompt pour éviter une oscillation. La marche arrière est prise en compte.
Q/D peuvent
interrompre l'aide pour s'écarter du mur, et reculer pour s'en éloigner l'annule.
La direction reste commandée par les actions d'entrée existantes.

Le braquage fonctionne également après avoir relâché Z/S, pendant la décélération.
Sans accélération, le sens de braquage dépend du déplacement avant ou arrière.
En dessous de 0,1 unité par seconde de vitesse de roulement, le véhicule ne pivote
plus sans accélération. Relâcher Q/D conserve le cap actuel, et ne remet pas la
voiture dans une direction du monde prédéfinie.
