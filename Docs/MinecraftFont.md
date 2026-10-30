> Configuration historique : Minecraft est remplacée par **Determination**.
> Voir [DeterminationFont.md](DeterminationFont.md) pour la police active.

# Police des textes du jeu

La police `Minecraft.ttf` installée sur le poste est copiée dans
`Assets/_Game/Art/Shared/Fonts/`. Le jeu ne dépend donc pas de son installation
sur l'ordinateur du joueur.

`Minecraft SDF.asset` est la police TextMeshPro par défaut, configurée dans
`Assets/TextMesh Pro/Resources/TMP Settings.asset`. Les textes créés par les
scripts (score, minuteur et écran de fin) héritent automatiquement de cette
configuration. Les quatre textes du prefab `ProgressionHUD` sont également
migrés : affichage d'XP et trois choix d'amélioration.

L'atlas statique 1024 x 1024 contient les 107 caractères fournis par cette version
de Minecraft. Elle ne contient pas les accents français. La police existante
Liberation Sans et ses fallbacks restent disponibles uniquement pour les
caractères absents, afin de conserver les textes accentués sans carré manquant.

Le matériau `Minecraft SDF - Outline` conserve le contour noir et l'épaisseur du
texte d'XP. Les autres textes utilisent le matériau principal Minecraft. Les
couleurs, tailles, ancres et comportements des textes sont conservés.

Pour les futurs textes, utiliser la police TMP par défaut ou sélectionner
`Minecraft SDF`, avec un matériau utilisant son atlas. Aucun script de remplacement
de police à chaque frame n'est nécessaire.
