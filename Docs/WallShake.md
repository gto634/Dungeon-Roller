# Secousses contre les murs

Le prefab Player possède la chaîne de feedback suivante :

```text
VehicleWallContact.OnWallCollision(point, normale, vitesse d'impact)
  Feedback/WallDeflection : PlayerWallDeflectionFeedback
    On Wall Collision (UnityEvent<float>)
      PlayerImpactFeedback.PlayWallDeflection(vitesse d'impact)
      Sparks/ParticleSystem.Play()
```

VehicleWallContact émet l'action à chaque nouveau contact physique avec un mur
de salle ou de corridor, indépendamment du réaxage, de l'angle du véhicule et
de l'activation de Movement. Un deuxième mur dans un angle est aussi signalé.
Un contact prolongé avec le même collider ne réémet pas la rafale à chaque pas
physique ; quitter puis retoucher le mur la réarme. Les sols et les props ne
sont pas des murs. OnWallDeflected reste réservé au début d'un réaxage.
Le relais s'abonne dans OnEnable et se désabonne dans OnDisable. Il place son
origine au contact, décalée de 0,12 unité vers la normale et relevée de 0,6 unité,
puis oriente la rafale vers l'extérieur et vers le haut avant de déclencher le
UnityEvent sérialisé. Les deux callbacks restent modifiables dans l'Inspector.

`PlayerImpactFeedback`, lu par `PlayerCameraFraming`, reçoit la vitesse d'impact
et fait varier la secousse jusqu'à **Maximum Wall Shake Amplitude**. Ce retour
n'applique aucun arrêt du temps ; le feedback des barrels conserve son bref gel.

`WallDeflectionSparks.prefab`, dans Prefabs/Player, conserve uniquement le système
**Ember** du FX JMO **CFXR3 Hit Fire B (Air)**. Il ne contient ni feu, ni lumière,
ni script de secousse ou de destruction automatique du package. Le système est
non bouclé, sans Play On Awake, et simule ses particules dans le monde pour que
les étincelles émises ne suivent pas ensuite la voiture. Le relais réarme la
rafale avant chaque UnityEvent et l'arrête à sa désactivation ; le système est
réutilisé sans instanciation supplémentaire à chaque choc.

La rafale contient 40 étincelles, de taille 0,14 à 0,28 et de durée de vie
0,4 à 0,65 seconde. L'émetteur garde une échelle de 1 et une émission en
hémisphère ; ces réglages rendent les étincelles lisibles depuis la caméra de
jeu, au-dessus de la carrosserie et de la surface du mur. Le matériau local
`Art/Shared/Materials/WallSparks.mat` réutilise la texture JMO et le shader
`Pattern Rendering/Outlined Sparks` : orange soutenu sans amplification HDR,
masque plus net et contour brun foncé de 0,8 pixel. **Spark color**,
**Outline color**, **Outline width (pixels)** et **Mask threshold** règlent
ce rendu dans le matériau. La couleur évolue vers l'orange foncé et l'opacité
diminue en fin de vie. La texture et les matériaux du package JMO sont conservés.

**Automatic Wall Feedback** est désactivé sur ce prefab : les secousses passent
par le UnityEvent, sans doubler la détection des collisions. Le mode automatique
historique reste disponible sur les autres instances qui l'activent :

L'intensité combine la profondeur de pénétration du contact PhysX et la vitesse
dirigée vers sa normale. Un choc frontal à vitesse élevée donne une secousse forte.
Un déplacement tangent au mur entretient une vibration légère. Les déplacements
réels du Transform sont mesurés entre les pas physiques, car Movement déplace
directement le véhicule ; une vitesse de conduite demandée alors que la voiture
reste bloquée ne suffit pas à entretenir la vibration. Un contact immobile ne
produit pas de secousse, même si les colliders sont légèrement imbriqués.

La vibration s'atténue en 0,14 seconde quand le frottement cesse. Les contacts
du sol et les props SRP sont exclus. Les contacts simultanés d'un angle ou de
plusieurs colliders du véhicule retiennent l'intensité maximale, sans addition.
La pause suspend le retour des murs. Celui-ci ne modifie jamais Time.timeScale ;
le bref arrêt du temps des barrels conserve son comportement existant.

Réglages dans **Wall contacts** du composant :

- **Automatic Wall Feedback** : détection directe historique, désactivée sur Player ;
- **Maximum Wall Shake Amplitude** : amplitude maximale, 0,22 unité par défaut ;
- **Full Wall Impact Speed** : vitesse normale correspondant à un choc maximal, 10 ;
- **Full Wall Penetration** : profondeur de référence, 0,12 unité ;
- **Scraping Strength** : intensité maximale du frottement, 10 % du maximum ;
- **Minimum Wall Movement Speed** : seuil de mouvement réel, 0,1 unité/seconde ;
- **Wall Shake Release Duration** : durée d'atténuation après le dernier contact mobile.
