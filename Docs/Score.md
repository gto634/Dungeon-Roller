# Points et multiplicateur

`PointManager`, dans `Scripts/Managers` et l'assemblage `Game.Management`, possède
le total de points, le multiplicateur et son délai d'expiration. Il est sauvegardé
sous `--- Managers ---` dans Generation et Base, et dans le prefab GameManager.
Chaque instance possède ses propres données ; aucun singleton supplémentaire
n'est créé.

```csharp
GameManager.Instance.PointManager.IncrementPoints(100f);
GameManager.Instance.PointManager.IncrementMultiplier();
```

`IncrementPoints(float amount)` applique lui-même le multiplicateur courant avant
d'ajouter les points. Les appelants fournissent le montant de base, sans le
multiplier. Les valeurs fractionnaires sont conservées ; les montants négatifs,
NaN et infinis sont refusés. Le total est plafonné à float.MaxValue.

`IncrementMultiplier()` démarre à x1 au premier appel, puis passe à x2, x3, x4
et x5. Cette méthode gère elle-même le plafond à **x5** et relance le délai même
si le plafond est déjà atteint. Sans combo actif, Multiplier vaut 1.
`AdvanceTime(float seconds)` efface le combo après **10 secondes de jeu** sans
nouveau corridor ; Multiplier Timeout est réglable sur PointManager. GameManager
avance ce temps avec le delta de jeu, comme pour DifficultyManager. Les pauses
d'amélioration, d'impact et de fin de partie suspendent le délai.
`ResetRun()` remet les points et le combo à zéro lors d'une nouvelle partie.

Les appels publics restent bloqués si PointManager est désactivé, ou si son
GameManager est désactivé ou a terminé la partie. La fin de partie conserve les
valeurs pour l'écran de résultat. Les événements PointsChanged et MultiplierChanged
permettent aux vues d'observer les changements sans posséder les données.

## Sources de points et portes

Les barrels émettent des orbes pour **10 XP** au total par défaut. La seconde
porte distincte d'un corridor émet des orbes pour **100 XP** ; la première ne
donne aucune récompense. Aucun point n'est ajouté lors de cette émission.

Chaque `ExperienceOrb` crédite son montant d'XP au ramassage via LevelManager,
et le même montant de base en points via PointManager.IncrementPoints. Les points
utilisent le multiplicateur actif au ramassage, et l'XP conserve sa valeur de
base. Une orbe émise à x2 mais récupérée à x4 donne donc quatre fois son montant
en points et son montant normal en XP. Un appel direct à LevelManager.GetExp
n'ajoute pas de points : ce crédit appartient au ramassage des orbes.

Le composant `CorridorMultiplierAction`, sous Action/Multiplier du prefab
Corridor, s'abonne à `CorridorPassageAction.OnDoorPassed`. Il appelle uniquement
`IncrementMultiplier`. `CorridorRewardsAction` reste responsable de l'émission
des orbes d'XP. L'ancien CorridorPointsAction est renommé avec le même GUID.

La première porte ne fait pas progresser le multiplicateur. Un corridor déjà
consommé ne réémet pas d'orbes, ne redonne pas d'incrément de combo et ne rafraîchit
pas le délai. GameManager ne modifie plus le combo depuis CorridorPassage.Traversed ;
il garde ce signal pour actualiser la salle du joueur. Les colliders multiples,
les réactivations et les reconstructions ne peuvent pas dédoubler les récompenses.

Le ramassage reste en attente si son GameManager ou son PointManager est désactivé,
et ne crédite pas une partie terminée. Chaque orbe est marquée collectée avant de
notifier les managers. Les points sont crédités avant l'XP pour conserver le gain
même si la montée de niveau ouvre immédiatement un menu de pause.

## Accès et affichage

GameManager expose les références publiques en lecture seule **PointManager**,
**RoomManager**, **DifficultyManager**, **LevelManager** et **MeteorManager**.
Les accès Rooms, Difficulty, ExperienceSystem et Meteors restent disponibles.
Les anciens accès Score, AwardPoints et événements de score redirigent vers
PointManager et ne contiennent plus de calcul ni de données de points.

ScoreDisplay obtient PointManager via le GameManager de sa scène et observe
directement PointsChanged et MultiplierChanged. Il réutilise le texte du HUD,
avec la police Determination, par exemple `240 pts  x3`. Le bonus est jaune et
disparaît à l'expiration. Le score reste visible sur les écrans de fin. Aucun
Canvas ou texte supplémentaire n'est créé à l'exécution.

## Écran de résultat et meilleur score

À la fin d'une partie, le cadre de score du HUD et le meilleur score forment un
bloc centré, côte à côte, dans deux cadres de même taille.
GameEndDisplay anime sa valeur affichée de zéro au total final pendant deux
secondes, avec un ralentissement à l'arrivée. Le temps utilisé est non mis à
l'échelle : le compteur continue lorsque la partie est en pause. Le multiplicateur
n'est pas réappliqué pendant cette présentation, et le total de PointManager ne
change pas.

Le cadre « Meilleur score », à droite, commence avec l'ancien record. Dès que le
compteur animé le dépasse, les deux valeurs affichées progressent ensemble.
Un score inférieur conserve le record précédent. La police et les fonds sont
ceux du HUD existant ; le nouveau cadre est sauvegardé dans GameUI.prefab.
Le début d'une nouvelle partie remet le cadre de score à sa place dans le HUD.
Masquer puis réafficher l'UI reprend le compteur sans le relancer.

PointManager.CompleteRun est appelé par GameManager avant GameEnded. Il capture
BestScoreBeforeResult, incrémente ResultVersion et sauvegarde immédiatement un
nouveau BestScore via PlayerPrefs, sous la clé DungeonRoller.BestScore.v1.
L'ancien record sert à l'animation ; le résultat final reste sauvegardé même si
l'UI est masquée ou si le joueur quitte avant la fin du comptage. ResetRun efface
le score courant mais conserve le record. Aucune sauvegarde n'est faite à chaque
image de l'animation. BestScoreChanged notifie les autres vues lorsqu'un record
est validé.

Score Count Duration, sur GameEndDisplay, règle la durée. Les tests de fin de
partie utilisent BestScorePrefsScope pour restaurer la préférence antérieure et
éviter de modifier le record personnel lors des validations.

Les boutons « Rejouer » et « Menu principal » sont sauvegardés sous les deux
compteurs. Le premier recharge la scène de jeu pour remettre à zéro le score,
le combo, l'XP, les niveaux et les améliorations ; le record reste sauvegardé.
Le second charge Assets/_Game/Scenes/MainMenu.unity. La pause de fin est libérée
par GameManager.PrepareForSceneChange avant le chargement asynchrone, et la vitesse
du jeu revient à 1. Les destinations sont validées dans les scènes de build.
Les actions sont disponibles uniquement à la fin de la partie et se verrouillent
dès le premier choix. Rejouer est sélectionné par défaut ; souris, navigation
horizontale et validation clavier/manette utilisent l'EventSystem existant.