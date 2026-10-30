# Timer de partie

GameManager affiche automatiquement un timer en haut à gauche en Play Mode.
L'affichage utilise le temps de jeu de DifficultyManager : il décompte de 02:00
à 00:00 en blanc, puis devient rouge et compte le temps passé après le seuil.
Les pauses, les choix d'améliorations et la fin de partie suspendent le timer.
Une nouvelle génération remet l'horloge et le décompte à zéro.

DifficultySettings configure le seuil (`Overtime After Seconds`, 120 secondes),
le bonus immédiat de difficulté (`Overtime Level Bonus`, 5 niveaux) et la vitesse
de progression ensuite (`Overtime Difficulty Multiplier`, 3). Le bonus s'applique
une seule fois au franchissement du seuil. Avant celui-ci, la progression garde
son rythme habituel d'un niveau toutes les 10 secondes ; après, elle progresse
d'un niveau toutes les 3,33 secondes. Le temps affiché garde la vitesse normale
du temps de jeu.

Le HUD est créé par RunTimerDisplay et suit l'échelle 1920 × 1080 des autres HUD.
Aucune référence de scène ou de prefab supplémentaire n'est nécessaire.
