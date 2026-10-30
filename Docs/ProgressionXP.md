# Corridor : deux triggers et une récompense d'XP

`Assets/_Game/Prefabs/Dungeon/Corridor/Corridor.prefab` utilise une seule instance
de `Art/Dungeon/Models/CorridorAll.fbx`, avec ses dimensions fixes et trois
BoxColliders solides (sol et deux murs) enregistrés dans le prefab. CorridorVisual
oriente le modèle et Physic sans les redimensionner. Les actions et les voyants
sont décrits dans CorridorActions.md.

Deux objets `Corridor Trigger A` et `Corridor Trigger B` occupent les extrémités.
Chacun réunit `CorridorPassage`, `CorridorDoorReward` et `Barrel` sur le même
GameObject, avec un unique BoxCollider trigger et aucun enfant. `CorridorVisual`
référence les deux passages distincts ; aucun troisième barrel n'est ajouté.

Chaque volume couvre la largeur de sa porte et sa profondeur, avec une hauteur
de 2,5 unités et une marge de 0,05 unité à chaque bord. Les volumes suivent les
directions et les transformations du Dungeon et restent désactivés tant que le
corridor n'est pas initialisé.

Le premier trigger touché par le Rigidbody du Player suivi est consommé sans
émettre de boule ni donner d'XP. Repasser sur cette même porte n'avance pas la
récompense. Seul le deuxième trigger distinct projette les quinze `ExperienceOrb`.
Les **100 XP sont attribués une seule fois par corridor**, au ramassage des boules.
L'ordre A puis B ou B puis A fonctionne, y compris en marche arrière.

Le jaillissement part du centre de la deuxième porte, 0,15 unité après son bord
extérieur, et suit l'axe du corridor vers l'extérieur. Une traversée du bas vers
le haut projette depuis la sortie du haut vers le haut ; une traversée inverse
projette depuis la sortie du bas vers le bas. Le cône ouvre à **160 degrés**
(`Burst Half Angle` = 80). Chaque boule reçoit une direction aléatoire à
l'intérieur de ce volume, sans espacement angulaire régulier. L'impulsion
verticale est indépendante et aléatoire entre **1,2 et 3,96 unités/seconde**
(`Burst Upward Speed Range`) pour varier la hauteur des arcs. Le décalage est
réglable via `Door Exit Offset`. Les transformations du Dungeon sont prises en compte.

La portée maximale de projection des portes est réduite de moitié : `Burst Launch
Speed` passe de 22 à **11 unités/seconde**, indépendamment de la vitesse du
véhicule. `Burst Speed Variation` vaut **0,25** : chaque boule reçoit entre
**8,25 et 13,75 unités/seconde** de vitesse horizontale, contre 16,5 à 27,5
auparavant. Le plafond vertical reste à 3,96 : les trajectoires les plus hautes
ne compensent pas la réduction de portée. Direction, distance et hauteur varient
indépendamment à chaque émission, tout en restant dans le cône extérieur.

Les quinze boules se partagent toujours **100 XP au total** ; la dernière reçoit
le reliquat pour conserver exactement la récompense. Les rebonds et le rappel
magnétique conservent leur fonctionnement. Les barrels visibles gardent leurs
cinq boules, leur projection adaptée à la vitesse de l'impact, leur origine et
leur dispersion propres.

Chaque barrel reste sur son objet après consommation, avec son collider
désactivé. `CorridorDoorReward` conserve l'état de chaque extrémité après
reconstruction, désactivation, réactivation ou réabonnement du GameManager : un
demi-tour ne réarme pas la première porte et une traversée complète ne recrée
aucune récompense. Le voyant de chaque porte passe à `IndicatorGreen.mat` quand
elle est consommée ; le voyant partagé passe au vert lorsque les deux le sont.

Les triggers ne bloquent pas le véhicule et ne déclenchent ni hitstop ni secousse.
Les barrels visibles conservent leur destruction et leurs effets d'impact habituels.
La pause du menu de montée de niveau reste gérée par le système de progression.

Les anciens corridors restaurent les deux triggers depuis le prefab à la place
du détecteur couvrant tout le couloir. GameManager retire également les anciens
barrels invisibles isolés afin qu'ils ne puissent plus produire une récompense
supplémentaire. L'identité du prefab et les références du BSP sont conservées.

## Système de Julien

`LevelSystem` et `LevelUpSystem` proviennent de `origin/Level-sysytem-Julien`
(commit `4eebf43`). Leurs GUID et leurs API publiques sont conservés. L'intégration
retire les dépendances Editor du code runtime et rend les abonnements symétriques.

- Le niveau, l'XP et le seuil initial viennent du `Stats` du Player.
- Un seuil atteint augmente le niveau, conserve le surplus et multiplie le prochain
  seuil par 1,2 ; ce multiplicateur est configurable sur LevelSystem.
- Chaque niveau donne un choix parmi trois améliorations distinctes tirées parmi
  les cinq de Julien : vie maximale, accélération, défense, vitesse maximale, réserve de nitro.
- Le jeu et la clock de difficulté sont mis en pause pendant le choix. Plusieurs
  niveaux gagnés d'un coup proposent leurs améliorations l'une après l'autre.
- La sélection applique une seule amélioration et restaure la vitesse du temps précédente.
- Les statistiques de progression et les bonus restent propres à la session ; le
  ScriptableObject Stats partagé n'est pas modifié pendant le jeu.

Les méthodes de statistiques de Movement et LifeSystem nécessaires aux améliorations
sont intégrées depuis le travail de Julien. La consommation/régénération de la nitro
utilise sa réserve runtime. Les champs de références existants sont conservés.

## Generation

La scène contient le nouveau prefab `Prefabs/UI/ProgressionHUD.prefab`, issu du Canvas
de la scène Base de Julien, avec sa barre d'XP, les trois boutons et le module UI
du nouveau Input System. Le HUD s'adapte à la taille de l'écran. Les choix affichent
le nom et la valeur du bonus et restent accessibles au clavier et à la manette.
Les ressources TMP de la branche sont incluses avec leurs GUID d'origine.

GameManager référence le LevelSystem du HUD. Le LevelUpSystem référence le Player
déjà suivi par RoomManager. Le raccourci d'XP de démonstration de Julien reste optionnel
et n'est pas assigné dans Generation. L'XP du Player et la difficulté du Dungeon
restent deux systèmes distincts.

Les couloirs déjà générés restaurent les deux triggers sans relancer la génération
ni changer la seed, les salles, les pièges ou la position du Player.

## Barrels : boules d'XP et impact

Un barrel touché par le Rigidbody du joueur suivi disparaît et projette cinq
`ExperienceOrb` bleues, de la couleur de la carrosserie, avec une courte traînée.
Les **10 XP** sont répartis entre ces boules ; la collision ne donne plus directement
d'expérience. Chaque boule
ne crédite sa part qu'une fois, à son arrivée sur le joueur.

Les boules partent toutes du centre du barrel, dans le cône configuré sur son prefab,
avec une impulsion verticale. L'axe pointe du point de contact vers le centre
du barrel, sur le plan du sol : un choc depuis le haut projette vers le bas, un choc
depuis la gauche projette vers la droite. Un choc rasant respecte aussi le côté
opposé à l'impact. La vitesse de projection augmente avec celle de l'impact. Les contacts
des colliders composés ne multiplient pas la récompense.

La projection est balistique : la gravité est multipliée par 1,6 et la traînée
ralentit le mouvement horizontal. Les boules ralentissent pendant la montée, puis
accélèrent à la retombée. Au contact du sol, elles rebondissent en conservant 55 %
de leur vitesse normale et 72 % de leur vitesse tangentielle ; les rebonds perdent
donc progressivement de l'énergie.
Un balayage de la sphère anticipe le contact pendant les retombées rapides pour
éviter une pénétration visible du sol entre deux pas de physique.

Le mesh est dans un enfant `Visual` : il s'étire en vol, s'écrase à l'impact, puis
s'allonge au rebond pendant 0,22 seconde. La déformation conserve approximativement
le volume et ne modifie pas la taille du SphereCollider ni la trajectoire physique.
La pause fige aussi ces animations.

Les boules laissent voir au moins 0,35 seconde de rebond avant leur aimantation,
avec un délai initial minimal de 0,4 seconde. La phase physique ne bloque pas
l'attraction plus de 1,1 seconde, notamment si une boule tombe hors du sol.
Elles suivent ensuite le joueur quand il se trouve à moins de 15 unités. Cette
portée couvre aussi la distance parcourue pendant la phase physique, nitro comprise.
L'aimantation démarre progressivement : sa vitesse propre passe de 4 à
18 unités/seconde selon la proximité, avec une courbe quadratique.
Les boules ajoutent 120 % de la vitesse réelle de la voiture à leur attraction,
avec un plafond total élevé de **50 unités/seconde**. Ce plafond inclut le bonus
de rattrapage : une voiture qui s'éloigne plus vite peut donc les semer. Il est
réglable via **Maximum Attraction Speed** sur le prefab ExperienceOrb. La limite
est appliquée à chaque pas physique, même si elle est réduite pendant la poursuite.
La vitesse du joueur tient compte du Rigidbody et du composant Movement actif.
L'accélération de base vaut 55 unités/seconde² et augmente avec la vitesse du joueur.
La projection initiale, la gravité et les rebonds gardent leur fonctionnement.
Elles restent ensuite attirées même s'il s'éloigne. Avant leur première attraction,
les boules hors de portée restent au sol jusqu'au ramassage ou au déchargement de la scène.
Elles ne sont pas enfants du barrel ou de la salle et survivent donc à leur retrait.

`PlayerImpactFeedback`, sur le Player, produit une secousse amortie de 0,18 seconde,
plafonnée à 0,09 unité, et un bref arrêt du temps de **25 à 55 ms**. L'intensité
dépend de la vitesse d'impact, avec une référence de 10 unités/seconde. Les durées
utilisent le temps réel pour reprendre même quand `Time.timeScale` vaut zéro.
Le cadrage Cinemachine applique la secousse sans déplacer durablement la caméra.

Le menu de montée de niveau termine d'abord le freeze d'impact avant de prendre
la pause à son compte. Sa fermeture retrouve ainsi la vitesse de temps antérieure,
y compris en ralenti. La désactivation du feedback restaure aussi le temps.

Les réglages de projection et le prefab de boule sont sur `Barrel`. Le délai,
le rayon, les vitesses minimale/maximale, l'accélération et le multiplicateur de
rattrapage, la gravité, les rebonds et la déformation sont sur `ExperienceOrb`.
L'amplitude et les durées de feedback sont sur
`PlayerImpactFeedback`. Les prefabs Barrel et Player sont déjà configurés ; aucun
branchement manuel n'est nécessaire.
