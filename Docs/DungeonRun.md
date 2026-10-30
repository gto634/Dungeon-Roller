# Démarrage et sortie du donjon

Au premier Start en Play Mode, un GameManager configuré avec un Dungeon et un Player
génère automatiquement le BSP et son contenu SRP. Le joueur est placé dans une salle
tirée au hasard, sur une position du sol libre de colliders solides. Le Rigidbody
est téléporté avec une vitesse nulle, puis RoomManager recalcule la salle courante.
Le même placement est appliqué après une nouvelle génération depuis le GameManager
pendant le jeu. Les commandes de génération en édition gardent leur comportement.

La sortie utilise le modèle `Assets/_Game/Art/Blender/Spring.blend`, dans
`Assets/_Game/Prefabs/Dungeon/Spring/Spring.prefab`. Son visuel mesure
**3 × 3 mètres sur X/Z**, pour environ **2,149 mètres de haut** au repos. Le ressort conserve
sa version raccourcie et allégée, avec 3 940 triangles et sept pièces de mesh.

Six matériaux visibles, dans `Art/Dungeon/Materials/Spring`, utilisent le shader
`Pattern Rendering/Pattern Object` et l'atlas du jeu : plateau rouge, disque bleu,
étoile jaune, tranche de l'étoile, ressort et socle. La tranche du plateau est lisse :
le liseré et les rivets latéraux sont retirés du fichier Blender. Le disque et
l'étoile sur le dessus restent conservés. Les matériaux inutilisés restent disponibles.
Les couleurs reprennent la palette du projet, avec motifs, ombres tramées et contours
noirs. Les matériaux sont remappés sur l'importeur du modèle ; le fichier Blender
et son GUID restent conservés.

`SpringExitAppearance` applique une liaison locale aux renderers à leur activation
pour conserver les couleurs de chaque matériau pendant le passage de contours SRP.
Les paramètres du Render Pipeline et les autres objets restent indépendants de
cette liaison ; aucun traitement par frame n'est ajouté.

Un unique BoxCollider trigger englobe le visuel. Il n'ajoute aucun obstacle solide.
Le placement réserve maintenant les dimensions et le centre du collider du prefab,
ce qui garde tout le Spring agrandi sur le sol, au-dessus de sa base et sans
chevauchement avec les murs ou les objets déjà générés. La compensation de l'échelle
de la salle conserve la taille du Spring dans les donjons transformés.

Une référence différente peut être donnée dans **Exit Prefab** sur GameManager.
Une autre salle est choisie quand elle possède un emplacement libre ; sinon la
sortie peut partager la salle du joueur, en gardant une distance minimale de deux
unités et en évitant son collider. La sortie suit l'activation de sa salle.

Seul un collider appartenant au Rigidbody du joueur suivi peut déclencher la
séquence, une seule fois. `GameManager.IsExitSequenceActive` suspend la progression
de la partie ; `IsGameEnded` reste false et l'écran de fin reste masqué pendant
la cinématique. L'événement `ExitSequenceStarted` ferme une amélioration ouverte.
Les récompenses et les météores sont bloqués pendant la séquence.

Le prefab possède une véritable `CinemachineCamera`, placée lors du Bind de la
sortie : à 6,5 mètres derrière le Spring, 2 mètres sur le côté et 4,6 mètres au-dessus
du sol. Le champ de vision est de 66°. La caméra conserve son orientation
horizontale et ajuste son pitch pour suivre le centre de la voiture. Une transition Cut
remplace temporairement les blends du Brain, puis ses réglages sont restaurés
à l'annulation ou à la prochaine génération.

`SpringExitCinematic` joue le clip `Animation/Spring/SpringExit.anim` (6 secondes).
Le clip contient la position et les rotations du véhicule ainsi que la compression
du ressort. Il est échantillonné dans un repère neutre, en mètres, pour fonctionner
dans les salles déplacées, tournées ou redimensionnées. La voiture est téléportée
en hauteur, descend en faisant un salto, comprime le Spring, puis décolle sur
environ 38 mètres vers l'avant et 30 mètres en hauteur, sans rotations supplémentaires après son unique salto d'arrivée.
Le monde est suspendu et le clip utilise le temps non mis à l'échelle. Le Rigidbody
devient temporairement cinématique, sans collisions ; les commandes, les traces
de roues et les retours d'impact sont suspendus.

À la fin du clip, GameManager passe `IsGameEnded` à true, émet `GameEnded` et
affiche « Sortie atteinte ! / Partie terminée ». La caméra conserve le plan de
sortie derrière l'écran. La fin de partie garde la simulation suspendue.
La désactivation du manager ou de la sortie annule une séquence en cours et
restaure la pose, les réglages physiques, les composants et le temps précédents.
Une nouvelle génération annule aussi la séquence avant de replacer la voiture,
relance la partie et retire l'ancien écran de fin.

**Generate On Start**, activé par défaut, permet de désactiver le démarrage automatique
pour les scènes de test. Aucun manager supplémentaire ni modification de scène
n'est nécessaire. Les scènes autonomes sans joueur suivi continuent à fonctionner.

## Défaite

La mort du joueur suivi déclenche également la fin de partie. GameManager écoute
son LifeSystem.OnDeath et expose IsDefeat pour distinguer la défaite de la sortie.
Le même écran affiche « Défaite ! / Partie terminée », avec un titre rouge et la
même présentation que la sortie. Le score reste visible ; le véhicule, le timer
et la simulation sont arrêtés. Le comportement reste actif lorsque l'UI est masquée.

Une fin ne peut être déclenchée qu'une fois : la sortie ne remplace pas une défaite,
et une mort après la sortie ne remplace pas la victoire. Les pauses d'impact et
d'amélioration sont libérées avant que la fin de partie prenne la sienne.
Une nouvelle génération masque l'écran, réinitialise le résultat et ranime le joueur
s'il était mort. Les scènes Generation et Base héritent de la référence au texte
depuis GameUI ; aucune création d'UI à l'exécution n'est nécessaire.


## Téléportation de test vers la sortie

Dans l'Inspector de GameManager, section **Salles**, le bouton **Téléporter le
joueur dans la salle de sortie** est disponible en Play Mode pendant une partie
active avec une sortie et un joueur valides. Il est désactivé après la fin de la
partie et pendant la cinématique de sortie.

`GameManager.TryTeleportPlayerToExitRoom()` cherche une position libre dans la
salle de sortie, en réactivant sa géométrie si nécessaire. Le placement évite
les volumes physiques et les triggers, notamment celui de la sortie, pour ne
pas déclencher automatiquement la cinématique à l'arrivée. Si aucune place ne
convient, le joueur reste à sa position et l'activation de la salle est restaurée.

La téléportation annule un éventuel gel d'impact, arrête le véhicule, réinitialise
ses traces de pneus et les contacts de porte, puis met à jour immédiatement
l'index de salle et le voisinage actif. Le score, le multiplicateur, la sortie
et la progression de la partie sont conservés. Le retour de la commande est
affiché dans l'Inspector.


## Organisation du prefab Spring

```text
Spring (DungeonExit)
  Render
    Modèle
      Spring (base et siège inférieur)
      Coil
        Spring_Coil
          PlatePos
      Plate
        Spring_Plate_Assembly
    Feedback (SpringExitAppearance)
  Physic
    Trigger (BoxCollider, SpringExitTrigger)
  Action
    Plate Position (FollowTransformPosition)
    Exit (SpringExitCinematic)
      Animation Pose
        Compression
        Plate
        Shake
        Camera Shake
      Spring Cinemachine Camera
```

Le prefab principal se trouve sous Dungeon/Spring, avec le même GUID que
l'ancienne sortie. Les références des scènes restent valides. Le fichier
Resources/DungeonExit.prefab est une variante du prefab principal, conservée
pour le chargement de secours Resources.Load.

DungeonExit expose Volume et Cinematic pour accéder aux composants déplacés.
Le trigger sous Physic transmet uniquement le contact à DungeonExit.TryComplete.
La cinématique et ses poses sont sous Action ; Compression reste enfant
d'Animation Pose pour conserver les chemins du clip. La caméra, les annulations
de séquence et la restauration de la physique suivent le fonctionnement précédent.
La voiture reste posée sur le sommet réel de la plate pendant la compression.
Son origine d'éjection est figée au départ du saut pour que les oscillations
du ressort ne modifient pas sa trajectoire en vol.

Le collider suit les dimensions du modèle et garde sa base au sol. Le script
d'apparence sous Feedback référence explicitement Modèle pour conserver les
matériaux et leurs couleurs. Le Bumper reste à ses dimensions, sa structure et
son emplacement précédents sous Dungeon/Trap.


## PlatePos et cinématique cartoon

Action/Plate Position porte `FollowTransformPosition`. Target référence
Render/Modèle/Plate ; Position Source référence PlatePos sous le coil.
Le script copie la position du repère dans le monde et ne copie ni sa rotation
ni son échelle. ExecuteAlways et LateUpdate permettent aussi l'édition et la pause.
La cinématique appelle Apply immédiatement après la déformation du coil.

Les groupes Coil et Plate ont des axes Unity verticaux sur Y. Les meshes importés
gardent leurs références et leurs matériaux. Le pivot du plateau est placé à sa
base, sur PlatePos, pour qu'il suive le ressort sans hériter de son étirement.
Le coil peut donc être redimensionné verticalement dans l'Inspector tandis que
la plate garde son échelle propre. PlatePos marque le sommet du coil au repos.

Le clip garde son GUID et utilise les poses de commande suivantes sous Animation Pose :
Compression (échelle Y du coil), Plate (échelles X/Y/Z du plateau), Shake (pose
du modèle complet), Camera Shake (pose additive de la caméra).

| Temps | Mouvement |
| --- | --- |
| 0 à 1 s | Arrivée depuis 3 m au-dessus de la plate et 4 m derrière ; un seul salto de 360°. |
| 1 à 2,8 s | Compression lente du coil de 100 % à 30 % ; plate à 78 % en Y et 112 % en X/Z. |
| 2,65 à 3,24 s | Anticipation : tremblement progressif de tout le modèle et de la caméra. |
| 3,2 s | Éjection brusque ; la voiture ne refait aucun salto. |
| 3,31 à 6 s | Coil alternativement étiré et comprimé, avec amplitude décroissante jusqu'au repos. |

La caméra suit le centre de la voiture en pitch avec le temps non mis à l'échelle,
et ajoute les tremblements du clip. La simulation du monde reste suspendue pendant
la séquence. L'annulation restaure la pose du modèle, les échelles du coil et de
la plate, les contrôles du joueur, sa physique, le temps et les réglages du Brain.
La fin restaure le ressort au repos et garde le dernier cadrage pour le résultat.

Validation : suivi de PlatePos sans transfert d'échelle, unique rotation de 360°,
compression lente, squash/stretch de la plate, tremblements du modèle et de la
caméra, vitesse d'éjection, oscillations amorties, suivi en pitch et restauration
après annulation. Des captures dans Generation vérifient les étapes du cadrage.
