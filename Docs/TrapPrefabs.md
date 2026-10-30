# Prefabs Spike, Barrel, Meteor et Bumper

La définition SRP `Data/Dungeon/SRP/Spike.asset` référence le prefab fonctionnel
`Prefabs/Dungeon/Trap/Spike.prefab`, avec `BrainSpike`, ses deux clocks,
`AttackSystem`, sa hitbox et un Animator. L'ancien prefab visuel CA reste présent
pour conserver la démonstration scolaire.

Le modèle vient de `Art/Blender/Spikes.blend`, exporté vers
`Art/props/SpikeTrap.fbx`. Ses trois meshes restent séparés : `Spikes`, `Platform`
et `Holes`. Le groupe `SpikeMotion` anime uniquement les pointes sur Y ; la
plateforme et les trous restent fixes. Le controller `Animation/spike/SpikeTrap`
commence rétracté, monte sur `Attack`, puis redescend. Les timings d'attaque
existants restent configurés par les clocks du prefab : 3 s entre attaques,
0,5 s pour la fenêtre d'attaque. La montée et le retrait durent chacun 0,15 s.

Le pivot de la racine est au sol, au coin haut gauche de l'empreinte 2 × 2 :
X de 0 à 2 et Z de -2 à 0. La hitbox couvre cette même surface. Le Rigidbody
est cinématique. L'aperçu du prefab montre les pointes sorties pour l'édition ;
en Play Mode, l'Animator les rentre avant la première attaque.

Le spawn des barrels utilise `Prefabs/Dungeon/Trap/Barrel.prefab`. La définition
`Data/Dungeon/SRP/Barrel.asset` et l'état `Data/Dungeon/CA/States/Barrel.asset`
référencent tous deux ce prefab, avec ses réglages actuels de projection, son
composant `Barrel`, son BoxCollider et son prefab de boules d'XP.
Il utilise `Art/props/BarrelTrap.fbx`, issu de `Art/Blender/Barrel.blend`, avec un
corps et deux cerclages séparés. Son pivot reste au coin haut gauche d'une case ;
le modèle de 0,9 × 0,9 m tient dans cette case.

Les cinq matériaux dans `Art/Dungeon/Materials/Traps` utilisent tous
`Pattern Rendering/Pattern Object` et l'atlas du projet :

| Matériau | Partie | Palette | Assombrissement |
| --- | --- | --- | --- |
| SpikeLightGray | Pointes | Gris clair | 0 |
| TrapPlatformGray | Plateforme | Gris clair | 0,45 |
| TrapHolesBlack | Trous | Noir, rendu uni | 0 |
| BarrelBodyGray | Corps du barrel | Gris clair | 0,20 |
| BarrelBandsDark | Cerclages | Gris clair | 0,65 |

Les exports sont orientés pour Unity (+Y en haut), avec leurs transformations
appliquées et une échelle de 1. Les fichiers Blender sources et les anciens
exports CA sont conservés. Le réglage d'outil Unity est `Pivot`, commun à la
Scene View ; ce réglage n'est pas une propriété de chaque GameObject.

## Meteor

`Art/Blender/Meteor.blend` est exporté vers `Art/props/Meteor.fbx`. Le fichier
Blender source conserve désormais les trois petites pierres comme objets séparés,
avec leurs contours correspondants. Les neuf meshes du rocher, de ses contours et de
sa traînée sont exportés ensemble avec leurs transformations appliquées et
l'axe Y vertical pour Unity. L'empreinte est normalisée à un diamètre maximal
de 1 unité sur X/Z, autour du centre du rocher, pour respecter la marge de chute.

Le prefab fonctionnel est renommé `Prefabs/Dungeon/Trap/Meteor.prefab`, avec le
même GUID et le même identifiant de racine que l'ancien `Meteor.prefab`.
Les références des spawners restent donc valides. La racine s'appelle `Meteor`.
Le modèle est enfant du Transform déjà piloté par `MeteorBrain` : rocher,
contours et traînée tombent ensemble. L'ombre, la hitbox et les réglages d'attaque
sont conservés, ainsi que le délai et la progression de fréquence du spawner.

Les matériaux et réglages de rendu actuels sont conservés par rôle : traînée,
gros rocher, petites pierres et contours. Les nouveaux morceaux reprennent les
matériaux des petites pierres d'origine. Aucun matériau partagé n'est modifié.

Le gros rocher et les trois petits morceaux sont regroupés séparément dans le
prefab, chaque pierre conservant son contour noir comme enfant. Chaque groupe
porte `MeteorStoneOffset` : `Start` choisit une direction horizontale aléatoire.
Pendant la chute, il se téléporte à 0,075 unité de sa position normale après
1 seconde, revient à sa position normale après une autre seconde, puis répète.
`Interval` et `Distance` se règlent sur chaque composant. La distance s'exprime en
unités du monde, indépendamment de l'échelle du météore ; aucun décalage n'est
appliqué sur Y. Les morceaux reviennent au repos à l'impact et à la désactivation.

Le groupe `MeteorTrail` porte `MeteorTrailRotation` et tourne continuellement
autour de son axe Y local. `Degrees Per Second` vaut 90 par défaut et reste
modifiable. Les animations utilisent le temps de jeu et s'arrêtent en pause.

## Bumper

`Art/Blender/Bumper.blend` est exporté vers `Art/props/Bumper.fbx`. Les trois
parties restent séparées : BumperFrame, BumperPads et BumperCap. Les transformations
sont appliquées pour Unity, avec Y vertical et une échelle d'import de 1.
Le rayon horizontal maximal est de 0,49 unité et la hauteur de 0,515 unité.

Le prefab `Prefabs/Dungeon/Trap/Bumper.prefab` conserve son GUID, ses identifiants
de racine et de BrainBumper, son Rigidbody cinématique et sa capsule de rebond.
Le modèle remplace la primitive provisoire sous Model. Sa base rejoint celle de
l'ancienne capsule, à Y local -1, sans déplacement du prefab dans les scènes.
La force de rebond, la remise à zéro de vitesse et les références de hitboxes
sont conservées.

Trois matériaux propres au Bumper réutilisent le shader Pattern Object et l'atlas
du projet, avec contours noirs et ombres à motifs :

| Matériau | Partie | Couleur |
| --- | --- | --- |
| BumperPadsOrange | Coussins périphériques | Orange |
| BumperCapRed | Plateau supérieur | Rouge |
| BumperFrameDark | Armature | Gris sombre |


Le prefab Bumper utilise maintenant les groupes Render (Modèle et Feedback),
Physic (Collision) et Action (Bump), comme Corridor. Le modèle et le volume de
collision ont des dimensions doublées ; la base du modèle reste à Y local -1.
Le groupe périphérique s'appelle Bumpers_sides. BumperRotationFeedback, sur
Render/Feedback/Rotation, le référence explicitement et le fait tourner autour de son Y
local à 90°/s par défaut. La vitesse est modifiable dans l'Inspector.


Feedback est un conteneur : Rotation porte BumperRotationFeedback, et BounceEffect
porte BumperBounceFeedback. Ce dernier s'abonne à OnBump de BrainBumper, sur
Action/Bump, après un contact de la hitbox accepté par cette action. Les côtés
et le plateau grossissent brièvement de 15 %, puis reviennent à leur échelle
initiale ; l'intensité et les deux durées sont réglables. Le plateau utilise
désormais le matériau dédié BumperCapRed, avec la palette rouge du projet.
