# Police de l'interface : Determination

`Determination.ttf` est une copie de la police installée sur le poste. Elle est
importée dans `Assets/_Game/Art/Shared/Fonts/` et ne dépend pas des polices du système
du joueur. La famille Determination comprend les accents français ; les textes du
jeu restent en français et les caractères affichés sont vérifiés sans repli.

`Determination SDF` est la police TextMeshPro par défaut. Les textes du HUD, des améliorations et de la fin sont sauvegardés dans
`GameUI.prefab`, sous `--- Canvas ---`. Ils utilisent tous cette police.
La police par défaut s'applique également aux futurs textes. Le matériau `Determination SDF - Outline` conserve le contour noir du texte
d'XP. Les couleurs et le comportement des autres éléments restent identiques.

L'atlas dynamique 1024 x 1024 génère les glyphes à la demande depuis la police
source incluse ; des atlas supplémentaires sont autorisés. Le repli Liberation Sans est
conservé pour les rares caractères absents de la source. Minecraft ne fait plus
partie de la configuration active de l'interface.

Le score s'affiche sur une ligne, par exemple `240 pts`. Son encadré mesure 80
unités de haut et utilise les mêmes marges verticales et réglages de taille de
police que le minuteur. Les valeurs fractionnaires gardent leur virgule française.
