# Actions et feedback des corridors

Le prefab `Assets/_Game/Prefabs/Dungeon/Corridor/Corridor.prefab` utilise cette hiérarchie :

```text
Corridor
  Render
    Modèle
      CorridorAll
    Feedback
      Door Confetti (variante LanaStudio)
  Physic
    Floor
    Wall Left
    Wall Right
    Trigger A
    Trigger B
  Action
    Passage
    Particles
    Rewards
    Multiplier
```

`CorridorPassageAction`, sur Action/Passage, référence explicitement les deux
`CorridorPassage`. Il mémorise leur consommation logique et émet
`OnDoorPassed(Vector3 direction)` lorsque le joueur suivi atteint le deuxième
trigger distinct. La direction est un vecteur horizontal normalisé dans le monde,
orienté vers la sortie : vers B si B est le second trigger, vers A sinon. Revenir
sur le même trigger ne termine pas le passage. Les autres Rigidbody et les
colliders de détection du joueur sont ignorés.

L'événement est émis une seule fois par corridor. La consommation est conservée
pendant les reconstructions visuelles et les désactivations de GameObjects.
Les volumes physiques des triggers restent disponibles pour les traversées
suivantes et les éventuels autres listeners de traversée. Les points et le combo
ne sont plus modifiés par le franchissement individuel d'une porte.

`CorridorParticlesAction`, sur Action/Particles, expose `Play(Vector3)`.
Le UnityEvent de Render/Feedback appelle cette méthode avec la direction dynamique.
Il n'y a plus d'abonnement direct de ce composant à l'événement C# : retirer ou
désactiver son listener dans le UnityEvent suffit à couper le FX. Le tableau de
ParticleSystems reste éditable dans l'Inspector. L'effet part du centre du
corridor sur les axes X/Z, à la même hauteur au-dessus du modèle de la porte,
et reste projeté verticalement vers le haut après le second trigger distinct.
`Door Model` référence le ModelRoot du corridor ; `Height Above Door`
ajoute 0,25 unité au plus haut renderer du modèle, avec le sommet du trigger
comme hauteur minimale. La direction reçue par le UnityEvent oriente le repère
des émetteurs autour de l'axe vertical.

`DoorConfetti_Lana.prefab`, à côté du prefab Corridor, est une variante de
`Assets/_Pack/Lana Studio/Hyper Casual FX/Prefabs/Confetti/Confetti_directional_multicolor.prefab`.
Elle garde les rubans, confettis, textures, couleurs et animations de rotation de LanaStudio.
Ses quatre matériaux `LanaConfetti_*.mat`, dans Art/Dungeon/Materials/Corridor,
utilisent `Pattern Rendering/Confetti Outline`, un shader URP dédié qui conserve
la transparence et les couleurs des particules. `Outline Width (pixels)`, à 1,25
par défaut, règle le contour ; la valeur 0 le coupe. `Outline Color` règle sa
couleur, noire par défaut. Le trait est calculé à l'intérieur de la silhouette
alpha, à partir des pixels voisins de la texture. Il suit les images et les UV
des rubans et garde les trous transparents. Le renderer de contours global du jeu
reste indépendant : il ne traite que les objets opaques. Les fichiers du pack d'origine restent inchangés.

La variante utilise une échelle de 0,55 et sept systèmes, sans lecture au démarrage
ni boucle, avec un maximum de 128 particules par système (896 au total).
Une seule instance est déjà présente sous Render/Feedback ; la traversée ne crée
pas de nouvel objet. Les particules se simulent dans le monde et s'éteignent
selon leurs durées de vie d'origine. La désactivation du groupe d'actions arrête
et efface l'effet, sans le rejouer au retour dans le corridor.

`CorridorRewardsAction`, sur Action/Rewards, écoute le même événement et garde
uniquement la récompense de 100 XP, répartie en 15 orbes. `ExperienceOrbBurst`
partage l'émission des orbes avec Barrel. Les triggers du nouveau prefab n'ont
plus de Barrel. `CorridorDoorReward` reste un adaptateur pour les appels et les
anciens états sérialisés.

`CorridorMultiplierAction`, sur Action/Multiplier, s'abonne directement à
OnDoorPassed et appelle uniquement `PointManager.IncrementMultiplier()`.
Il ne donne ni XP ni points. La première porte et les nouvelles traversées d'un
corridor déjà consommé ne rafraîchissent pas le combo. GameManager ne modifie
plus le combo depuis Traversed. Le GUID de l'ancien CorridorPointsAction est
conservé ; MovedFrom et FormerlySerializedAs préservent les références et l'état.

`ExperienceOrb` crédite son XP et ses points de base une seule fois au ramassage.
Les points utilisent le multiplicateur courant à cet instant ; l'XP reste sans
multiplicateur. Les barrels et les corridors ne donnent plus de points lors de
l'émission des orbes. L'orbe garde le GameManager de son joueur et de son
LevelManager, afin de ne pas créditer une autre partie. Un manager désactivé ou
une partie terminée empêche le ramassage. Les points sont crédités avant l'XP pour
que le menu de montée de niveau ne puisse pas interrompre la récompense.

`CorridorIndicatorFeedback`, sur Render/Feedback, possède les liaisons
matériaux des voyants et expose `SetAllGreen()`. Il ne connaît ni la détection du
passage ni les récompenses.

`CorridorFeedbackEvents`, sur Render/Feedback, s'abonne à
`OnDoorPassed` et transmet la direction à son UnityEvent éditable dans l'Inspector.
Cet événement contient deux listeners persistants : `Feedback.SetAllGreen()`
et `Action/Particles.Play(Vector3)` en mode dynamique. Les confettis partent donc
avec les voyants verts, une fois au second trigger distinct. Pour ajouter un son,
une animation ou un autre FX, ajouter un listener à ce UnityEvent. Une méthode prenant un Vector3 peut utiliser le paramètre dynamique
pour connaître la direction de sortie. Les abonnements sont retirés à OnDisable.

Le prefab conserve une géométrie fixe : modèle complet et trois colliders solides
(sol et deux murs). CorridorVisual oriente directement Modèle et Physic selon le
corridor et relie les deux portes aux actions. Les dimensions et l'échelle
éditées dans le prefab sont conservées au rebuild ; aucun redimensionnement du
modèle ou des colliders solides n'est appliqué. Les anciens réglages Prefab Width
et Corridor Unit Length sont retirés de StyleCorridor ; Door Depth sert encore
aux triggers. CorridorIndicatorFeedback possède directement ses liaisons de
matériaux. Les triggers calculent leur pose dans le monde pour suivre la rotation
de Physic. Les dimensions et matériaux de friction des trois solides sont conservés.
Les anciens corridors enregistrés dans les scènes sont remplacés par les groupes
du nouveau prefab lors du rebuild ; les références croisées et les UnityEvents
sont remappés ensemble et les portes déjà consommées restent consommées. La
version de hiérarchie assure aussi la mise à niveau des anciens groupes Action
qui utilisent encore les anciens composants de score, sans rejouer leurs récompenses.

Les tests PlayMode couvrent les deux sens dans les quatre directions, la
configuration persistante du UnityEvent, les contacts physiques, le filtrage des
colliders et l'absence de replay après désactivation/rebuild. Les tests de
récompenses, de multiplicateur et de traversée du donjon restent actifs.

Les tests de score vérifient l'absence de gain immédiat, le crédit au ramassage,
le multiplicateur courant, la séparation entre XP et points, le menu de montée
de niveau, les managers désactivés et l'absence de doublons.

La version 4 de la hiérarchie Corridor remplace aussi les anciennes instances
enregistrées dans les scènes pour leur donner la variante et le listener de
confettis. Les portes déjà consommées restent consommées pendant cette migration.
Les tests vérifient l'origine LanaStudio de la variante, l'absence de lancement
au démarrage ou au premier trigger, le rendu URP, l'arrêt lors d'une désactivation
et l'absence de FX quand son listener UnityEvent est retiré.

La version 5 utilise directement la géométrie fixe du prefab et les références de
CorridorVisual. Les modèles et colliders des instances enregistrées sont conservés
sans composant de redimensionnement ; les anciennes hiérarchies restent migrées
vers le prefab actuel avec les confettis et les états de portes.

Les quatre émetteurs de papier utilisent un cône de 25° aligné avec le Z local
du conteneur, qui est tourné vers le haut à chaque passage. Les vitesses initiales
sont comprises entre 10 et 14 avant l'échelle 0,55 du FX, et la gravité vaut 1,2.
Leurs anciennes vitesses latérales imposées sont désactivées ; les tailles,
couleurs, rotations et durées de vie restent celles de la variante LanaStudio.
Les deux flashes latéraux ont leur émission coupée pour garder une fontaine
de confettis dirigée vers le haut. La version 6 met à niveau les anciennes
instances en conservant l'état des portes et les listeners du UnityEvent.

La validation comprend une comparaison GPU avec/sans contours ainsi que les
vitesses et positions des particules réelles, les quatre orientations de couloir,
les deux sens de traversée et l'absence de replay.
