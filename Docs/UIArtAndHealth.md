# Sprites UI et barre de vie

Les sept PNG de `Assets/_Game/Art/Ui` sont utilisés dans `GameUI.prefab` :

- `Background_text` habille les encadrés du timer et du score.
- Les trois segments rouges composent la barre de vie.
- Les trois segments bleus composent la barre d'XP.

Les PNG/PSD sources restent inchangés. Les PNG sont importés en Sprite, avec
filtrage Point, sans mipmaps ni compression. Les bordures et centres sont
assemblés en images séparées ; les centres sont répétés pour conserver le motif.
Les fonds de texte utilisent une bordure découpée et un centre répété.

Le matériau partagé `Art/Shared/Materials/UIPattern.mat` utilise le shader
`Pattern Rendering/UI Pattern` sur les quatorze images des barres et des fonds.
Le motif source a une période de 3 pixels, alors que les segments mesurent
10 pixels : répéter les sprites complets rompait les diagonales aux raccords.
Les bordures découpées des fonds produisaient également des changements de phase.

Le shader garde l'alpha et les contours noirs d'origine, et échantillonne les
deux encres du motif dans une région intérieure de 3 × 3 pixels du même PNG.
La répétition suit une grille commune à l'écran : elle reste continue entre
les centres, les extrémités et les bordures des fonds. La taille d'un pixel du
motif est arrondie à un nombre entier de pixels écran pour éviter les alternances
de largeur lors d'une mise à l'échelle fractionnaire du Canvas.

`Pattern Pixel Size` vaut 4 à la résolution de référence 1920 × 1080, avec
`Match Width Or Height` à 0,5 comme les CanvasScaler du HUD. Ces paramètres sont
sur le matériau. Les PNG sont utilisés séparément, sans SpriteAtlas. Le shader
conserve les teintes des Image, l'alpha, les masques stencil et les RectMask2D :
le Slider continue de découper l'illustration à sa largeur complète.


`HUD/Progression/StatusBars` range la vie au-dessus de l'XP via VerticalLayoutGroup.
Les deux barres ont la même largeur et mesurent 40 unités de haut, séparées de
12 unités. Elles suivent le CanvasScaler 1920 x 1080 et restent présentes en édition.

`HealthDisplay` référence le LifeSystem du joueur de chaque scène et observe
`LifeSystem.Changed`. L'événement est émis après la mise à jour de la vie, ainsi
qu'à l'initialisation, et couvre aussi les changements de vie maximale. Dégâts,
soins, mort et résurrection mettent à jour le remplissage et `PV actuel / maximum`.
Les anciens événements de dégâts, soins et mort conservent leur fonctionnement.

`SegmentedBarFill` garde une illustration à la largeur totale de la barre. Le
Slider déplace uniquement un RectMask2D de découpe : le motif et les extrémités
ne sont pas étirés lors d'une perte de vie ou d'une progression d'XP. Le fond
assombri reste visible sur la partie vide.

La police Determination, le français et les interfaces sauvegardées dans les
scènes Generation/Base sont conservés. Aucun élément de HUD n'est créé en partie.


À chaque perte de PV, HealthDisplay fait trembler légèrement les visuels de la
barre pendant 0,2 seconde, avec une amplitude maximale de 3 unités de Canvas
horizontalement et 1,5 verticalement. Le fond, le remplissage et le texte bougent
ensemble ; le RectTransform piloté par VerticalLayoutGroup et la barre d'XP
restent fixes. Le tremblement s'atténue puis restaure les positions d'origine.
Les impacts rapprochés relancent l'effet sans cumuler les décalages. Les soins
ne le déclenchent pas. Le temps non mis à l'échelle permet le retour au repos
même pendant une pause ; désactiver la vue restaure immédiatement les visuels.
L'amplitude, la durée et la fréquence sont réglables sur HealthDisplay.
