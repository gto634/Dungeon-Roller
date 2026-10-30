# Traces de pneus par salle

`VehicleTireTracks`, sur le prefab Player, projette les quatre contacts des pneus
sur les colliders du sol. Les positions sont réglables dans `Wheel Contacts`, en
coordonnées locales du Player. `Tire Width`, `Opacity` et `Minimum Speed` règlent
la trace ; `Ground Tolerance` définit la proximité nécessaire du pneu au sol.

Chaque `RoomTireTracks` possède son propre masque `Texture2D`, créé au premier
passage. Les traits sont interpolés entre deux contacts du même pneu sur le sol
de la même salle. La projection utilise X/Z dans le repère de la salle, sans
dépendre des UV du modèle. Le shader Pattern Object applique ce masque uniquement
aux renderers du sol, via un `MaterialPropertyBlock` ; le matériau partagé reste
commun aux salles.

Les traces restent présentes pendant les désactivations de RoomManager et les
reconstructions des visuels du sol. Une nouvelle salle ou un changement de ses
données repart avec un masque vide. Détruire la salle libère sa texture.
`Clear Tire Tracks`, dans le menu du composant RoomTireTracks, efface son masque.
Les textures sont conservées pendant la partie, sans sauvegarde sur disque.

Le masque utilise un canal R8 quand il est pris en charge, sinon RGBA32. La densité
par défaut est de 32 pixels par unité, avec un maximum de 1024 pixels par côté.
Seules les salles traversées allouent leur texture. Les quatre pneus sont regroupés
avant un seul transfert de pixels par salle touchée et par frame de déplacement.
L'arrêt, la pause et les contacts en dehors d'un sol de salle ne peignent pas.
Les couloirs n'ont pas de masque dans cette version. Un changement de salle, une
perte de contact ou un déplacement dépassant `Maximum Connection Distance`
interrompt la continuité, pour éviter de tracer entre deux positions éloignées.


## Inclinaison visuelle pendant une glissade

Le prefab Player possède `VehicleVisualLean`, relié au seul enfant `Model`.
Quand la valeur absolue de la vitesse latérale dépasse celle de la vitesse
longitudinale (avant ou arrière), le modèle s'incline progressivement en Z
vers le côté du déplacement. Le repère vient de la direction du véhicule,
indépendamment de son orientation dans la salle. Le retour à plat est progressif.

L'inclinaison maximale est de 25°, atteinte avec un excès de vitesse latérale
de 8 unités/s. Le seuil minimal est de 0,5 unité/s, avec une réponse exponentielle
de 12/s. Les paramètres `Maximum Roll`, `Full Roll Lateral Speed`,
`Minimum Lateral Speed` et `Response` sont réglables sur le Player.

Le pivot se situe sur les deux contacts des pneus du côté bas. La position locale
du modèle est corrigée avec sa rotation afin que ces pneus restent à leur place.
Le Rigidbody, les colliders, les contraintes, la direction et la vitesse physiques
ne sont pas modifiés par ce composant.

Les contacts de `Wheel Contacts` restent la référence à plat, en coordonnées
locales du Player. `VehicleVisualLean` les transforme avec la pose réelle du modèle
et partage leurs résultats de raycast avec les traces et `WheelRotation`.
Chaque roue est associée à son `Ground Contact Index` dans le prefab ; les indices
suivent les positions réelles des quatre roues, pas l'ordre de leur liste.

Une roue qui se soulève de plus de 4 cm, ou qui ne trouve plus de sol à 6 cm,
reste visible mais cesse de tourner, de braquer et de peindre. Les roues basses
continuent de tourner et de tracer à leurs positions visuelles. Lorsqu'un pneu
retouche le sol, sa rotation et sa trace reprennent ; la continuité de la trace
est coupée pendant la perte de contact pour éviter de dessiner en l'air.
Les raycasts ignorent les triggers et les propres colliders du véhicule.

Les composants se mettent à jour dans cet ordre : pose visuelle, roues, traces.
L'arrêt du temps fige l'effet dès la frame de pause. La désactivation du composant
rend au modèle sa position et sa rotation initiales. Un Rigidbody cinématique ou
un Movement désactivé remet aussi le modèle à plat, notamment pendant la cinématique
du Spring et la fin de partie. Une téléportation de plus de huit unités réinitialise
la pose et l'échantillonnage des traces.
