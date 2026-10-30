# Phases des dangers

Le temps de partie de DifficultyManager pilote la transition :

- Avant **2:00**, la difficulté des pièges augmente toutes les 10 secondes.
- Dès **2:00**, la difficulté attendue des pièges est figée : aucun nouveau
  palier à la frontière ni bonus en overtime. Le dernier palier automatique est
  à 1:50, soit le niveau 11 avec les réglages actuels.
- Les mises à jour en cours peuvent finir et les salles en retard, y compris
  celles qui étaient inactives, peuvent encore générer les pièges nécessaires
  pour atteindre ce niveau. Elles ne le dépassent pas, même après une demande
  manuelle de difficulté supérieure. Les pièges déjà posés sont conservés.
- Les vagues de météores commencent à **2:00**, puis l'intervalle suit :

| Temps de partie | Intervalle |
| --- | --- |
| 2:00 | 3 s |
| 2:10 | 2,4 s |
| 2:20 | 1,92 s |
| 2:30 | 1,536 s |
| 3:20 | environ 0,5033 s |
| 3:30 et après | 0,5 s |

La formule est `max(0.5, 3 * 0.8^floor((temps - 120) / 10))` après le début des
météorites. Une réduction raccourcit également le cooldown en cours. Les retards
de frame ou les passages hors d'une salle ne provoquent pas de rafale de rattrapage.

L'échelle uniforme des météores est comprise entre **1,5 et 2,5**, pour l'objet
complet : modèle, ombre et volumes de collision. Ils restent limités à la salle
du joueur et à ses marges de mur, sans réduction sous la taille minimale.

Le nombre par vague utilise le même calcul que les pièges, partagé dans
AreaSpawnBudget : surface de la salle divisée par la densité, arrondi inférieur,
puis conservation du reste pour la vague suivante. La surface est calculée à
partir des dimensions entières de RoomData.Bounds, comme pour SRP. La densité
des météores est réglable par Cells Per Meteor et vaut **150 unités²** par
météore, comme le dénominateur actuel des pics.

| Surface de salle | Vague 1 | Vague 2 | Vague 3 |
| --- | --- | --- | --- |
| 100 unités² | 0 | 1 | 1 |
| 400 unités² | 2 | 3 | 3 |
| 600 unités² | 4 | 4 | 4 |

Chaque salle garde son propre reste pendant les passages et désactivations.
Les intervalles passés dans un corridor ne créent pas de dette de météores.
Une nouvelle génération, un redémarrage du manager ou une remise à zéro du
temps effacent ces budgets. La commande manuelle crée toujours un seul météore,
sans consommer le budget automatique. Le mode autonome sans salle conserve une
apparition par intervalle.

Dans Generation, MeteorManager référence explicitement DifficultyManager et
s'abonne à ClockChanged. Le Clock historique est désactivé pendant cette liaison
puis restauré à la désactivation du manager. Il reste disponible pour le mode
autonome sans DifficultyManager. La commande manuelle de test reste indépendante
du calendrier automatique.

Les pauses, choix d'amélioration et fins de partie suspendent le temps de jeu.
Une régénération remet le calendrier à zéro : les pièges sont à nouveau autorisés
et les météorites attendent de nouveau deux minutes.

GameManager transmet la difficulté maximale figée aux générateurs des salles
actives et inactives. Les files de travail restent disponibles pour le rattrapage
jusqu'à cette limite. L'activation des salles continue de suivre le changement
effectif d'index du joueur. Une nouvelle génération retire la limite et remet la
progression des pièges à zéro.
