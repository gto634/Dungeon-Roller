# Cadrage du joueur et anticipation

Le prefab `Player/Player.prefab` possède `PlayerCameraFraming` sur son
`Camera/CinemachineCamera`. Son `Movement` est assigné dans le prefab ; les scènes
qui utilisent ce Player héritent du cadrage, notamment `Generation`.

`Movement.CurrentSpeed` lit la valeur absolue de la vitesse utilisée pour les
translations. `NormalizedSpeed` la rapporte à la vitesse maximale courante,
avec un résultat borné entre 0 et 1. Les bonus de vitesse maximale sont donc
pris en compte et la nitro ne pousse pas le joueur hors du cadrage prévu.
`FacingDirection` vient du Transform qui oriente effectivement le véhicule.

À l'arrêt, le joueur reste au centre. Le décalage augmente proportionnellement
à `NormalizedSpeed`. À pleine vitesse, il est placé au tiers opposé à son regard :

- regard à droite : joueur au tiers gauche ;
- regard à gauche : joueur au tiers droit ;
- regard vers le haut : joueur au tiers inférieur ;
- regard vers le bas : joueur au tiers supérieur.

La projection tient compte de la rotation de la caméra, du format de l'écran et
du mode perspective ou orthographique. Pour les diagonales, le cadrage reste dans
le carré délimité par les lignes des tiers. En marche arrière, le regard reste la
référence et la vitesse absolue détermine l'amplitude.

Réglages de `PlayerCameraFraming` :

- `Maximum Screen Offset` : 1/6 par défaut, soit l'écart entre le centre et un tiers ;
- `Smoothing Time` : 0,35 seconde par défaut, pour lisser accélération, virages et freinage.

Le `CinemachinePositionComposer` conserve sa distance de repos de 7 unités et l'inclinaison
de 70 degrés. Son anticipation native est désactivée et son damping de position
est nul : il suit la position du Player, tandis que l'extension lisse le cadrage
directionnel. Cela évite d'additionner deux anticipations et garantit les proportions
de l'écran même quand le Player avance à vitesse constante.

L'extension ajoute une correction à la sortie du pipeline Cinemachine. Elle ne
modifie pas les coordonnées sérialisées du Composer pendant le jeu et n'accumule
pas les offsets entre les images. La pause fige le lissage ; désactiver l'extension
rend le suivi centré standard.

## Dézoom lié à la vitesse

L'extension utilise le même pourcentage de vitesse pour agrandir progressivement
la zone visible. En perspective, elle recule la caméra sur son axe, de la distance
de repos du Composer jusqu'à `Maximum Camera Distance` (15 unités par défaut, plafond absolu).
Le champ de vision et l'inclinaison restent inchangés. En mode orthographique,
la taille orthographique augmente dans la même proportion pour produire le dézoom.

`Zoom Smoothing Time` (0,5 seconde par défaut) contrôle la transition, indépendamment
du lissage du décalage directionnel. Le freinage ramène progressivement au zoom de
repos. La pause fige aussi le dézoom. La vitesse normalisée est bornée entre 0 et 1 :
une survitesse ou la nitro ne dépassent pas le dézoom maximal. La limite ne peut pas
être inférieure à la distance de repos.

Le décalage directionnel est calculé après le dézoom, à partir de la nouvelle zone
visible, pour conserver la règle des tiers. Aucun paramètre de lentille ni distance
sérialisée du Composer n'est modifié pendant le jeu ; le dézoom ne s'accumule pas
entre les images.

## Accélération logarithmique

La vitesse maximale initiale du Player est de 10 unités/seconde (auparavant 15).
`Movement.Acceleration Curvature` règle la forme de la montée en vitesse : 0 conserve
une accélération linéaire, 9 est la valeur par défaut. Le départ est plus vif puis
la montée ralentit à l'approche du maximum, qui est atteint en un temps fini.

La vitesse suit `v = vmax × ln(1 + k × u) / ln(1 + k)`, où `u` progresse de 0 à 1
en `vmax / Stats.Speed` secondes et `k` est la courbure. Le calcul récupère `u` à
partir de la vitesse courante : reprendre l'accélération ou gagner un bonus de
vitesse ne remet pas le véhicule à zéro. La nitro conserve ses multiplicateurs.
Le freinage et les changements de sens passent progressivement par une vitesse nulle.

Le décalage et le dézoom continuent de suivre le pourcentage de vitesse réelle,
donc la même progression logarithmique, avec leurs lissages respectifs. La distance
de dézoom est plafonnée à 15 unités dans le code et dans l'Inspector, même si une
configuration demandait davantage ou si le Player dépasse sa vitesse normale.
