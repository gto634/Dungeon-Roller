# Points, chance et raretés

Les stats affichées et leurs gains utilisent 100 points pour 1 unité de jeu,
à l’exception des PV qui restent en valeurs directes.
Les accesseurs de Stats et les API gameplay restent en unités de jeu ;
Stats.ToPoints et Stats.FromPoints assurent la conversion. Le marqueur sérialisé
_statPointVersion garde la compatibilité avec les anciens assets. Le PlayerStats
du projet est converti sans changer son GUID.

Au départ : accélération 52, vitesse maximale 640, maniabilité 40, nitro 0,
chance 0 et PV 100. Cela représente respectivement 0,52, 6,4, 0,4, 0, 0 et
100 unités de jeu. Niveaux, XP, score et temps ne sont pas des stats et gardent
leur échelle.

LevelManager ajoute 5 points de chance par tranche complète de 20 secondes de
jeu, y compris après le seuil de difficulté. La pause n'avance pas cette horloge.
Les sauts de temps comptent toutes les tranches ; une désactivation/réactivation
ne double pas les gains. Un recul de l'horloge ou ResetLuck remet la chance à la
valeur initiale. Les cartes Chance ajoutent aussi un bonus à cette stat de partie,
sans modifier le ScriptableObject partagé.

| Rareté | Couleur | Multiplicateur | Probabilité à 0 chance | À 100 chance |
| --- | --- | --- | --- | --- |
| Commun | Gris | ×1 | 65 % | 35 % |
| Rare | Bleu | ×2 | 23 % | 19 % |
| Super rare | Cyan | ×3 | 8 % | 16,5 % |
| Épique | Violet | ×5 | 3 % | 16,5 % |
| Légendaire | Or | ×8 | 1 % | 13 % |

Les poids interpolent vers 5/15/25/30/25 avec l'influence L/(1+L), où L est
la chance en unités de jeu (points / 100). La progression est graduelle ; une
chance plus élevée améliore la distribution des gains, avec un rendement
décroissant. Les règles sont regroupées dans UpgradeRarityRules.

Gains communs : PV +5, défense +100, accélération +20, vitesse +20, nitro +20,
maniabilité +20 et chance +20. Les autres raretés multiplient ces bases. Le nitro
reste un multiple de 20. Les PV utilisent des gains directs de 5/10/15/25/40,
et la défense est arrondie à des unités de jeu entières pour conserver les
calculs de LifeSystem. Le format de données version 2 stocke les PV directement ;
la lecture du format version 1 reste compatible avec les anciens PV multipliés.

Une offre tire trois stats distinctes parmi sept. UpgradeOffer conserve la stat,
la rareté et le gain exact. Le clic consomme cette offre sans nouveau tirage.
Les gains sont lus directement dans les StatDefinition, et les noms/couleurs de
rareté dans les RarityDefinition. UpgradeRarityRules conserve uniquement le tirage
et ses probabilités, sans imposer les gains ni les couleurs.


## Assets de configuration

`Assets/_Game/Data/Progression/Stats/` contient Health, Acceleration, Defense,
MaximumSpeed, MaximumNitro, Handling et Luck. Chaque StatDefinition possède son
identifiant, son nom, sa couleur, sa valeur de départ et cinq gains explicites,
chacun associé à un RarityDefinition. Les gains initiaux reprennent l’équilibrage
précédent ; ils peuvent ensuite être réglés indépendamment pour chaque rareté.

`Assets/_Game/Data/Progression/Rarities/` contient Common, Rare, SuperRare, Epic
et Legendary. Chaque asset porte l’identifiant, le nom affiché et la couleur.
Ces cinq assets sont partagés par toutes les stats. Les menus Create > Progression
permettent de créer de nouvelles définitions.

PlayerStats référence les sept définitions. Ses accesseurs lisent leurs valeurs
de départ ; les anciens champs restent des valeurs de repli pour les anciens
assets dépourvus de définition. Les setters restent compatibles et peuvent
surcharger une copie de Stats sans modifier les définitions. Le joueur conserve
ensuite ses valeurs courantes dans Movement, LifeSystem et LevelManager. Les
améliorations de partie ne modifient jamais les ScriptableObjects de définition.

Les valeurs sont en points : PV directs, autres stats à 100 points par unité de
jeu. La défense doit rester un multiple de 100 et les gains de nitro un multiple
de 20, comme auparavant. Une définition incomplète ou invalide empêche l’ouverture
des offres et produit une erreur explicite plutôt qu’une récompense incohérente.

## Cartes

Les trois boutons existants utilisent `Assets/_Game/Art/Ui/Card/Card.png`, en
Point, sans compression ni mipmaps, avec leur format 300 × 600 conservé.
`UpgradeCardView` lit les couleurs des deux définitions et fournit un matériau
de rendu indépendant par carte, libéré à la désactivation/destruction. Le
matériau partagé et le sprite restent inchangés pendant une partie.

Le shader `Pattern Rendering/Upgrade Card` sépare la bordure de cinq pixels du
contenu. Dans la bordure, les pixels blancs deviennent la couleur de rareté ;
dans le contenu, ils deviennent la couleur de stat. Les pixels gris prennent
la même couleur légèrement assombrie : les rayures intérieures utilisent une
luminosité de 0,90, réglable dans Card.mat avec Interior stripe brightness. Le
motif gris du contour garde sa luminosité de 0,54 (Border grey brightness).
Les traits noirs et la transparence sont conservés. Les raretés utilisent des
teintes foncées et les stats des teintes pastel. Tous les textes sont noirs, sans
contour. Un texte RarityText est ancré en haut de chaque carte et reçoit le nom
de la rareté ; le nom de stat et les valeurs restent au centre.

L’offre mémorise le gain exact au tirage : le texte et le clic utilisent cette
même valeur. Les trois stats restent distinctes, et le choix n’effectue pas
un nouveau tirage. La pause, la navigation et les événements existants restent
ceux de LevelUpSystem.
