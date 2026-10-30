# Interfaces de scène et progression

`Generation` et `Base` possèdent un objet racine **--- Canvas ---**, instance du
prefab `Assets/_Game/Prefabs/UI/GameUI.prefab`. Toutes les vues sont sauvegardées
dans les scènes et modifiables avant Play. Aucun manager ne crée de Canvas,
texte ou écran à l'exécution.

```text
--- Managers ---
  GameManager
  RoomManager
  DifficultyManager
  MeteorManager
  LevelManager
  PointManager
--- Canvas ---
  EventSystem
  HUD
    Progression
      StatusBars
        HealthBar
        XPBar
    Timer
    Score
    Nitro
      Nitro Meter
        Gauge
        Label
  Overlays
    Upgrades
      Choices
    RunComplete
      Backdrop
        Result
        BestScore
        Actions
          Replay
          MainMenu
```

Le HUD et les écrans utilisent des Canvas séparés : une modification du timer ne
reconstruit pas l'ensemble de l'interface. Les choix d'amélioration utilisent un
HorizontalLayoutGroup. Les écrans sont masqués par CanvasGroup ; leurs objets
existent déjà en édition et conservent leur identité au cours de la partie.
Le cadre de score reste au-dessus du masque de fin. Les deux compteurs sont de même taille, centrés côte à côte, et les boutons sont alignés en dessous.

`Game.UI` référence les managers et `Dungeon.Runtime`. Les vues contiennent des
références sérialisées explicites vers leurs textes, groupes et managers, puis
s'abonnent aux changements dans OnEnable et se désabonnent dans OnDisable.
Le prefab est réutilisable ; les références vers les managers et le joueur sont
renseignées dans chaque scène. Aucun nom d'objet n'est recherché à l'exécution.

| Vue | Source | Responsabilité |
| --- | --- | --- |
| ScoreDisplay | PointManager.PointsChanged / MultiplierChanged | Score et multiplicateur |
| RunTimerDisplay | DifficultyManager.ClockChanged | Compte à rebours et overtime |
| LevelDisplay | LevelManager.Changed | Niveau, XP et barre |
| HealthDisplay | LifeSystem.Changed | Vie actuelle, maximale et barre |
| NitroDisplay | Movement.NitroChanged | Jauge verticale, visible après déblocage du nitro |
| LevelUpSystem | LevelManager.LevelUP et choix en attente | Choix et pause d'amélioration |
| GameEndDisplay | GameManager.GameEnded / RunStarted et résultat de PointManager | Compteurs centrés, comptage animé, rejouer et retour au menu |

`LevelSystem` devient **LevelManager**, dans `Scripts/Managers` et l'assemblage
`Game.Management`. Son GUID de script est conservé et MovedFrom documente son
ancien type. Il possède les niveaux, l'XP et les améliorations en attente ; il ne
référence aucune vue. `GetExp` conserve la progression et la croissance des seuils.
`ConsumeUpgrade` consomme un choix validé. Le masquage du groupe UI ne désactive
plus la progression ni le ramassage d'XP. `GameManager.ExperienceSystem` et
`SetExperienceSystem` restent disponibles avec le type LevelManager.

`PointManager` conserve le score et le multiplicateur. `GameManager` conserve les
règles de partie et émet les événements
de fin et de lancement ; les propriétaires d'une pause UI libèrent leur pause
avant que la fin de partie prenne la sienne. Les trois méthodes d'amélioration
du véhicule sont publiques pour être appelées depuis l'assemblage UI.

Pour ajouter une interface : créer une vue dans HUD ou Overlays du prefab,
référencer les composants dans l'Inspector, puis connecter la source de données
dans la scène. Conserver la police Determination et le CanvasScaler 1920 x 1080.
Les tests PlayMode couvrent les deux scènes, les événements, les choix successifs,
les réactivations, les pauses, l'XP et la régénération sans nouvelle UI.

Les sprites du dossier `Art/Ui` habillent les fonds de texte et les barres.
La barre de vie est placée au-dessus de l’XP ; voir `UIArtAndHealth.md`.

Les boutons de résultat sont des composants uGUI Button avec une navigation
explicite et des références sérialisées sur GameEndDisplay. Les listeners sont
ajoutés dans OnEnable et retirés dans OnDisable. Le rechargement complet de la
scène réinitialise la progression, tandis que PlayerPrefs conserve le record.

`HUD/Progression/DamageFeedback` présente les dégâts derrière les barres. Il
réutilise la source de vie de HealthDisplay, donc les références de joueur déjà
enregistrées dans Generation et Base. DamageScreenFeedback observe OnDamage et
Changed : flash bref aux dégâts, puis double battement chaque seconde strictement
sous 20 % des PV maximaux. L'opacité aux bords oscille entre 3,5 % et 20 %, contre
29,4 % pour un dégât normal. ScreenEdgeVignette dessine un dégradé par sommets,
transparent au centre et plus opaque vers chaque bord, sans texture ni shader
supplémentaire. L'effet n'intercepte aucun clic, suit le temps de jeu (pause
comprise), s'efface après un soin au-dessus du seuil ou à la mort et se réinitialise
à la désactivation. Les intensités, le seuil et la période sont réglables dans
l'Inspector du composant.


Les six cartes d'amélioration affichent désormais les valeurs de la partie sous
la forme « valeur actuelle → valeur après le choix », par exemple « 8 → 9 » pour
la vitesse maximale. La vie utilise les PV maximaux, la défense lit LifeSystem,
et l'accélération, la vitesse maximale, le nitro et la maniabilité lisent Movement.
Les valeurs sont recalculées à chaque offre : les choix précédents et les gains
configurés dans LevelUpSystem sont pris en compte. L'aperçu ne modifie aucune stat.


Les points affichés valent 100 fois les unités de jeu, à l’exception des PV.
La barre de vie et les cartes de PV affichent les valeurs directes : 100 / 100,
puis par exemple 100 → 105 pour une carte commune. Le remplissage et les dégâts
réels gardent leur fonctionnement.
Les cartes gardent les trois emplacements existants : trois stats distinctes
sont tirées parmi vie, accélération, défense, vitesse, nitro, maniabilité et chance.
Chaque carte montre la stat, sa rareté et les valeurs avant/après. Sa couleur et
son gain sont déterminés par la rareté, puis conservés jusqu'au choix. Le gain
affiché est celui appliqué, même si la chance change entre-temps.

LevelManager possède la chance de la partie et observe l'horloge de difficulté,
branchée par GameManager. Cette horloge continue en overtime et s'arrête pendant
les pauses. ResetLuck est appelé au lancement d'une nouvelle partie. Voir
LuckAndRarities.md pour les gains et les probabilités.


Le Canvas Nitro conserve une jauge verticale segmentée en bas à droite. Son
CanvasGroup masque la jauge tant que MaximumNitro vaut zéro ; le premier gain
de capacité la rend visible. NitroDisplay observe le Movement du joueur déjà
référencé par HealthDisplay, sans créer d’objets en partie. Voir Nitro.md pour les événements
d’activation et les flammes des quatre échappements.
