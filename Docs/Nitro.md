# Nitro : actions, feedback et HUD

Movement, déjà placé sous Actions/Movement du Player, possède l'état du nitro.
OnNitroActivated et OnNitroDeactivated sont des événements Action émis uniquement
au changement d'état. NitroChanged signale une modification de quantité ou de
capacité, y compris l'initialisation et les améliorations. IsNitroBoosting expose
l'état actif. La consommation, la recharge et le blocage jusqu'à recharge complète
après épuisement restent dans Movement.

```text
Player
  Actions
    Movement (Movement)
  Feedback
    Nitro (NitroFeedbackEvents, NitroFlamesFeedback)
      Echappement_G_Exterieur
        CFXR Fire Breath
      Echappement_G_Interieur
        CFXR Fire Breath
      Echappement_D_Interieur
        CFXR Fire Breath
      Echappement_D_Exterieur
        CFXR Fire Breath
--- Canvas ---
  HUD
    Nitro (Canvas, CanvasScaler, NitroDisplay)
      Nitro Meter (CanvasGroup)
        Gauge (Slider vertical, segments)
        Label
```

NitroFeedbackEvents observe les événements de Movement et les relaie dans deux
UnityEvents éditables dans l'Inspector. On Nitro Activated appelle
NitroFlamesFeedback.Play ; On Nitro Deactivated appelle Stop. Les quatre effets
sont des instances du prefab JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR
Fire Breath, placées à la sortie des quatre échappements et dirigées vers l'arrière.
Ils sont conservés dans le prefab, sans instanciation à chaque utilisation.
Play On Awake est désactivé ; le Clear Behavior du contrôleur CFXR vaut None
pour éviter sa destruction. Les réglages sont des overrides du Player et ne
modifient pas les assets JMO. L'effet ne déclenche pas le camera shake du pack.

Le nitro multiplie l'accélération moteur par **2**, soit **+100 %** : avec les
stats de base actuelles, elle passe de 5,2 à 10,4 unités/s². Le réglage
**Nitro Acceleration Multiplier** se trouve dans Movement. Le relâchement ou
l'arrêt du boost restaure l'accélération normale. Le réglage **Nitro Speed
Multiplier** multiplie aussi la vitesse maximale par **2** pendant le boost :
avec les stats de base, la vitesse passe de 6,4 à 12,8 unités/s. Le nitro accélère
donc encore la voiture lorsqu'elle roule déjà à sa vitesse maximale normale.
Au relâchement, le frottement la ramène progressivement à sa limite normale.

Les mêmes UnityEvents pilotent la secousse : **On Nitro Activated** appelle aussi
`PlayerImpactFeedback.StartNitroShake` et **On Nitro Deactivated** appelle
`StopNitroShake`. Elle reste active pendant le boost, avec une amplitude de
**0,03 unité**, réglable dans **Nitro Shake Amplitude** sur PlayerImpactFeedback.
PlayerCameraFraming ajoute ce décalage à la caméra. Le relâchement, l'épuisement,
la pause et les désactivations coupent la secousse. Elle ne modifie pas le temps
de jeu et coexiste avec les retours des murs et des barrels.

Les flammes et les petites braises des quatre échappements utilisent les matériaux
locaux `Art/Shared/Materials/NitroFlameOutline.mat` et `NitroEmberOutline.mat`.
Le shader `Pattern Rendering/Outlined Flames` ajoute un contour brun foncé
de 1,2 pixel, réglable avec **Outline color** et **Outline width (pixels)**.
Il conserve les textures, la distorsion animée du feu, les couleurs des particules,
leur luminosité et l'atténuation aux intersections. La fumée utilise son matériau
JMO d'origine. Ces matériaux sont assignés par des overrides dans Player ;
les prefabs et matériaux du package restent partagés sans modification.

Un relâchement, l'épuisement, Stop, la désactivation de Movement ou une pause
coupent le boost et effacent les particules. La désactivation de Feedback retire
ses abonnements et coupe aussi les effets. Une réactivation synchronise l'état.

Le HUD possède une jauge segmentée verticale orange en bas à droite et un libellé
Determination. NitroDisplay observe NitroChanged et présente CurrentNitro /
MaximumNitro. Son CanvasGroup reste transparent tant que MaximumNitro vaut zéro.
La première amélioration de nitro rend la jauge visible, y compris lorsqu'elle
est ensuite vide ; elle suit aussi les augmentations ultérieures de capacité.
Les objets existent déjà dans les scènes avant Play et ne capturent aucun clic.
La barre s'assombrit de 30 % pendant le blocage après épuisement, puis retrouve
ses couleurs dès que CanActivateNitro redevient vrai. Un relâchement avant
épuisement conserve la luminosité normale. Unavailable Brightness, réglable dans
NitroDisplay, contrôle l'intensité ; le libellé et l'opacité restent inchangés.
Le prefab référence sa barre de vie pour retrouver une seule fois le Movement
sur le Rigidbody de son joueur. Il réutilise ainsi les références LifeSystem
déjà renseignées dans Generation et Base, sans modifier les scènes. Une référence
Movement directe reste disponible pour une autre configuration de HUD.

Validation : événements uniques au maintien, entrée clavier, relâchement,
épuisement et recharge, améliorations, pause, désactivation, réabonnement,
réutilisation des quatre effets et raccordement du HUD dans les deux scènes.

La validation ciblée inclut 41 réussites sur 45 tests. Les quatre échecs de
PlayerHandlingPlayModeTests sont reproduits à l'identique sur les fichiers
d'origine, sans ce système de nitro : grip après drift, impulsions latérales,
référence d'input drift absente et attente obsolète du gain de maniabilité.
Les sept tests propres au nitro passent. Le rendu du HUD et des effets est
également contrôlé dans Generation avec un vrai rendu URP.
Le raccordement final via HealthDisplay est validé par 23 tests nitro et HUD
réussis ; aucune modification des scènes n'est nécessaire.


## Commandes

Nitro : **Espace / LT** (`leftTrigger`). Dérapage : **Shift / RT**
(`rightTrigger`). Les deux Shift sont pris en charge. InputControl référence
Drift dans le prefab Player ; le nitro utilise la référence Sprint existante.
Les commandes fonctionnent au maintien, se coupent au relâchement et peuvent être
utilisées simultanément. Le nitro exige toujours une capacité débloquée.
Les tests d'entrée utilisent les véritables actions du prefab, sans les remplacer
par des actions de test ni les activer manuellement.

La correction rétablit aussi la référence Drift auparavant absente. Les huit
régressions d’entrée reproduites avant correction passent après le correctif ;
la validation ciblée totalise 16 tests réussis.
