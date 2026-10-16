# Perk-Katalog: 68 neue Dices für Roll The Dice

Dieser Katalog sammelt neue Perk-Ideen für `!rtd`. Alle Ideen sind **neu**: Keine ist ein bereits vorhandener Dice (siehe `src/dices/`) oder eine direkte Abwandlung davon. Wo eine Idee an einen bestehenden Dice erinnert, steht unter *Abgrenzung*, worin der Unterschied liegt.

Jede Idee wurde darauf geprüft, ob sie serverseitig mit CounterStrikeSharp umsetzbar ist, ohne Sicherheits- oder Performance-Probleme zu verursachen. Ideen, die das nicht schaffen, stehen am Ende unter [Bewusst verworfen](#bewusst-verworfen).

**Legende**

| Symbol | Bedeutung |
|---|---|
| 🟢 | Positiv (Vorteil für den Würfelnden) |
| 🔴 | Negativ (Nachteil / Fluch) |
| 🟡 | Trade-off, Chaos oder Party (Vor- und Nachteil) |
| **S** | Kleiner Aufwand: bekannte Bausteine, meist < 150 Zeilen |
| **M** | Mittlerer Aufwand: mehrere Hooks, Zustandsverwaltung, Tests auf mehreren Maps |
| **L** | Großer Aufwand: eigene Entities/Projektile, viel Feintuning |

> Alle Zahlenwerte (Schaden, Dauer, Cooldown, Reichweite) sind Vorschläge und sollten wie bei den bestehenden Dices als Config-Werte in `MapConfig` landen.

---

## Inhalt

1. [Gemeinsame technische Bausteine](#gemeinsame-technische-bausteine)
2. [Leitplanken für Sicherheit und Performance](#leitplanken-für-sicherheit-und-performance)
3. [Übersicht aller Perks](#übersicht-aller-perks)
4. [Bewegung (1–12)](#a-bewegung)
5. [Waffen und Treffer-Effekte (13–32)](#b-waffen-und-treffer-effekte)
6. [Überleben und Verteidigung (33–40)](#c-überleben-und-verteidigung)
7. [Granaten und Gadgets (41–53)](#d-granaten-und-gadgets)
8. [Aufklärung (54–56)](#e-aufklärung)
9. [Chaos und Party (57–65)](#f-chaos-und-party)
10. [Meta-Würfel (66–68)](#g-meta-würfel)
11. [Empfohlene Reihenfolge](#empfohlene-reihenfolge)
12. [Bewusst verworfen](#bewusst-verworfen)

---

## Gemeinsame technische Bausteine

Viele Perks brauchen dieselben Grundfunktionen. Wenn diese einmal als Helfer in `src/utils/` landen, wird jeder einzelne Perk deutlich kleiner.

| Baustein | Wofür | Umsetzung |
|---|---|---|
| **ButtonTracker** | Aktivierung per Taste (E, Springen, Ducken, Shift) mit Flanken-Erkennung (gedrückt / losgelassen) | `player.Buttons` (`PlayerButtons`) im `OnTick` nur für `_players` auswerten, vorherigen Zustand pro Slot merken. Falls die genutzte CS#-Version `OnPlayerButtonsChanged` anbietet, das bevorzugen. |
| **SetVelocity** | Jetpack, Sprünge, Rückstoß, Ziehen/Stoßen | `pawn.AbsVelocity` lesen, neuen Vektor über `pawn.Teleport(null, null, velocity)` setzen. |
| **DealDamage** | Schaden mit korrektem Killer (Killfeed, Punkte, Geld) | Das Muster `ApplyRedirectedTakeDamage` aus `PlayAsChicken.cs` (`CBaseEntity_TakeDamageOld` mit eigener `CTakeDamageInfo`) in einen allgemeinen Helfer auslagern. Ein Marker (wie `RedirectMarker`) verhindert, dass die eigenen Hooks den Schaden erneut verarbeiten. |
| **SpawnExplosion** | Splittergranate, Luftschlag, Mine, Martyrium | `Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile")`, Werfer/Team setzen, `DispatchSpawn()`, dann `DetonateTime = Server.CurrentTime` (wie in `WeirdGrenades.cs`). Schaden und Radius über `Damage`/`DmgRadius` begrenzen. |
| **SafeTeleport** | Blink, Ender-Perle, Rückspulen | Mit `RayTrace.TraceLine` Kopf- und Fußhöhe prüfen, Zielpunkt etwas vor die Wand ziehen, auf Boden-Trace stützen und `IsPlayerNearby` (wie in `ReturnToSender.cs`) nutzen, damit niemand in Wänden oder anderen Spielern steckt. |
| **Charges/Cooldown + HUD** | Alle aktiven Fähigkeiten | Pro Spieler Ladungen und `Server.CurrentTime`-Zeitstempel speichern, Status über `NotifyStatus` / `Banner` anzeigen. |
| **Per-Viewer-Sichtbarkeit** | Markierungen, Spuren nur für bestimmte Spieler | `CheckTransmit` wie in `PlayAsChicken.cs` und das Glow-Muster aus `Glow.cs`. |

## Leitplanken für Sicherheit und Performance

Diese Regeln gelten für jeden neuen Perk:

1. **Kein globaler Zustand.** Keine ConVars ändern (`sv_gravity`, `sv_autobunnyhopping`, `host_timescale` …) und keine Waffen-`VData` anfassen. VData gilt für *alle* Waffen dieses Typs auf dem Server und bleibt über die Runde hinaus verändert.
2. **`OnTick` nur über `_players` iterieren** und teure Arbeit (Traces, Abstandsprüfungen über alle Spieler) auf jede 4.–8. Tick drosseln. Der Listener wird dank `Bindings.cs` ohnehin nur registriert, wenn der Dice aktiv ist. Das sollte so bleiben.
3. **Entities begrenzen und aufräumen.** Jede gespawnte Entity (Projektil, Beam, Prop, Partikel) braucht eine Obergrenze pro Spieler und wird in `Remove()`/`Reset()` entfernt.
4. **Rekursionsschutz** bei Schaden, der Schaden auslöst (Dornen, Leibwächter, Schicksalsband, Voodoo): Re-Entrancy-Flag oder Damage-Marker.
5. **Raten begrenzen** bei allem, was pro Schuss oder pro Treffer feuert (Explosivmunition, Kettenblitz). Sonst erzeugt eine Negev hunderte Entities.
6. **Map-Exploits vermeiden.** Mobilitäts-Perks (Jetpack, Enterhaken, Blink) können Spieler auf Dächer oder aus der Map bringen. Darum Höhen- und Reichweitenlimits setzen, Skybox-Treffer ignorieren und den Perk pro Map über die bestehende Map-Config abschaltbar machen.
7. **Keine Client-Befehle ausführen** (`ClientCommand` / `ExecuteClientCommand`), um Effekte zu erzwingen. Alles läuft serverseitig.
8. **Sounds und Assets selbst erstellen** bzw. CS2-eigene Assets nutzen. Keine Original-Sounds aus anderen Spielen (Urheberrecht).
9. **Friendly Fire respektieren.** Flächenschaden und Statuseffekte prüfen `mp_friendlyfire` bzw. treffen standardmäßig nur Gegner.

---

## Übersicht aller Perks

| # | Name (Klassenname) | Typ | Kategorie | Inspiration | Aufwand |
|---|---|---|---|---|---|
| 1 | Jetpack (`Jetpack`) | 🟢 | Bewegung | Halo, Fortnite | M |
| 2 | Doppelsprung (`DoubleJump`) | 🟢 | Bewegung | Platformer, Hollow Knight | S |
| 3 | Blink (`Blink`) | 🟢 | Bewegung | Overwatch (Tracer), Dishonored | M |
| 4 | Enterhaken (`GrapplingHook`) | 🟢 | Bewegung | Just Cause, Apex (Pathfinder) | M |
| 5 | Gleiter (`Glider`) | 🟢 | Bewegung | Fortnite, Zelda BotW | S |
| 6 | Auto-Bhop (`AutoBhop`) | 🟢 | Bewegung | Surf/Bhop-Server, CS 1.6 | S |
| 7 | Slide (`Slide`) | 🟢 | Bewegung | Apex Legends, Titanfall | S |
| 8 | Rückstoß-Antrieb (`RecoilPropulsion`) | 🟡 | Bewegung | Quake, TF2 (Scout) | S |
| 9 | Stampfer (`GroundPound`) | 🟢 | Bewegung | Super Mario, Overwatch (Doomfist) | M |
| 10 | Flummi (`Bouncy`) | 🟡 | Bewegung | Party-Spiele | S |
| 11 | Rückspulen (`Recall`) | 🟢 | Bewegung | Overwatch (Tracer), Prince of Persia | M |
| 12 | Glatteis (`Slippery`) | 🔴 | Bewegung | Eis-Level, Mario Kart | M |
| 13 | Zeus-Railgun (`InfiniteZeus`) | 🟢 | Waffen | Quake (Railgun) | M |
| 14 | Kettenblitz (`ChainLightning`) | 🟢 | Waffen | Diablo, Warcraft III | M |
| 15 | Wurfmesser (`ThrowingKnives`) | 🟢 | Waffen | CoD (Throwing Knife), Hotline Miami | L |
| 16 | Entwaffnen (`Disarm`) | 🟢 | Waffen | Diverse Fighting-Games | S |
| 17 | Butterfinger (`Butterfingers`) | 🔴 | Waffen | Harry Potter, Party-Spiele | S |
| 18 | Explosivmunition (`ExplosiveRounds`) | 🟢 | Waffen | Borderlands, GTA (Cheat) | M |
| 19 | Brandmunition (`IncendiaryRounds`) | 🟢 | Waffen | CoD (Dragon's Breath), Borderlands | M |
| 20 | Frostmunition (`FrostRounds`) | 🟢 | Waffen | Diablo, Borderlands (Cryo) | M |
| 21 | Blendmunition (`FlashRounds`) | 🟢 | Waffen | Rainbow Six (Blitz), Stun-Effekte | S |
| 22 | Rückstoßmunition (`KnockbackRounds`) | 🟢 | Waffen | Smash Bros., TF2 (Force-A-Nature) | S |
| 23 | Harpune (`Harpoon`) | 🟢 | Waffen | Mortal Kombat (Scorpion), Dota (Pudge) | S |
| 24 | Markierungsmunition (`MarkingRounds`) | 🟢 | Waffen | Valorant (Sova), Far Cry | M |
| 25 | Schnellfeuer (`RapidFire`) | 🟢 | Waffen | CoD (Double Tap) | M |
| 26 | Blitz-Nachladen (`QuickReload`) | 🟢 | Waffen | CoD (Sleight of Hand) | M |
| 27 | Ladehemmung (`WeaponJam`) | 🔴 | Waffen | Far Cry 2 | S |
| 28 | Blutmagie (`BloodMagic`) | 🟡 | Waffen | Diablo (Blood Magic), Isaac | S |
| 29 | Goomba-Stomp (`GoombaStomp`) | 🟢 | Waffen | Super Mario | S |
| 30 | Taschendieb (`Pickpocket`) | 🟢 | Waffen | Thief, Skyrim | S |
| 31 | One in the Chamber (`OneInTheChamber`) | 🟡 | Waffen | CoD-Spielmodus | S |
| 32 | Gun Game (`GunGame`) | 🟡 | Waffen | CS-Spielmodus Arms Race | M |
| 33 | HP-Regeneration (`HealthRegen`) | 🟢 | Überleben | CoD, Halo, Overwatch | S |
| 34 | Schild (`Overshield`) | 🟢 | Überleben | Halo, Apex (Evo-Shield) | M |
| 35 | Dornen (`Thorns`) | 🟢 | Überleben | Diablo, Minecraft | S |
| 36 | Schildkrötenpanzer (`TurtleShell`) | 🟢 | Überleben | Mario Kart, Dark Souls | S |
| 37 | Totstellen (`FeignDeath`) | 🟢 | Überleben | TF2 (Dead Ringer), WoW (Jäger) | M |
| 38 | Superstern (`SuperStar`) | 🟢 | Überleben | Super Mario | M |
| 39 | Leibwächter (`Bodyguard`) | 🟡 | Überleben | Overwatch (Reinhardt), MMO-Tanks | M |
| 40 | Glasknochen (`BrittleBones`) | 🔴 | Überleben | Isaac, Roguelikes | S |
| 41 | Splittergranate (`ClusterGrenade`) | 🟢 | Granaten | Worms, Battlefield | M |
| 42 | Zielsuchende Granaten (`HomingGrenades`) | 🟢 | Granaten | Worms (Homing Missile) | M |
| 43 | Lenkrakete (`GuidedDecoy`) | 🟢 | Granaten | Unreal Tournament (Redeemer), Half-Life (RPG) | L |
| 44 | Ender-Perle (`EnderPearl`) | 🟢 | Granaten | Minecraft | S |
| 45 | Näherungsmine (`ProximityMine`) | 🟢 | Granaten | GoldenEye, CoD (Claymore) | M |
| 46 | Schwarzes Loch (`BlackHole`) | 🟢 | Granaten | Halo (Gravity Hammer), Overwatch (Zarya) | M |
| 47 | Heilrauch (`HealingSmoke`) | 🟢 | Granaten | Overwatch (Moira), Apex (Lifeline) | S |
| 48 | Giftrauch (`ToxicSmoke`) | 🟢 | Granaten | Apex (Caustic), Valorant (Viper) | S |
| 49 | Luftschlag (`Airstrike`) | 🟢 | Granaten | CoD, Worms | M |
| 50 | Hologramm-Köder (`HoloDecoy`) | 🟢 | Granaten | Apex (Mirage) | M |
| 51 | Raketensprung (`RocketJump`) | 🟡 | Granaten | TF2 (Soldier), Quake | S |
| 52 | Ninja-Rauchbombe (`NinjaSmoke`) | 🟢 | Granaten | Ninja-Games, Overwatch (Reaper) | S |
| 53 | Sonnenfackel (`SolarFlare`) | 🟢 | Granaten | Dragon Ball (Taiyoken) | S |
| 54 | Radar-Scan (`UavScan`) | 🟢 | Aufklärung | CoD (UAV) | M |
| 55 | Herzschlagsensor (`HeartbeatSensor`) | 🟢 | Aufklärung | CoD (Heartbeat Sensor), Alien | S |
| 56 | Spurenleser (`Tracker`) | 🟢 | Aufklärung | The Witcher, Batman (Detective Mode) | L |
| 57 | Betrunken (`Drunk`) | 🔴 | Chaos | GTA, Party-Spiele | S |
| 58 | Laserpointer (`LaserSight`) | 🔴 | Chaos | Taktik-Shooter | M |
| 59 | Heiße Kartoffel (`HotPotato`) | 🟡 | Chaos | Kinderspiel, Party-Modi | M |
| 60 | Martyrium (`Martyrdom`) | 🟡 | Chaos | CoD (Martyrdom) | S |
| 61 | Voodoo-Puppe (`VoodooDoll`) | 🟢 | Chaos | Voodoo-Mythos, Isaac | S |
| 62 | Schicksalsband (`FateBond`) | 🟡 | Chaos | Diverse RPGs (Soul Link) | S |
| 63 | Kopfgeld (`Bounty`) | 🟡 | Chaos | Red Dead, Fortnite | S |
| 64 | Auftragskiller (`Hitman`) | 🟡 | Chaos | Hitman, Assassin's Creed | S |
| 65 | Konfetti-Kills (`ConfettiKills`) | 🟡 | Chaos | TF2 (Birthday Mode) | M |
| 66 | Doppelwurf (`DoubleRoll`) | 🟡 | Meta | Brettspiele, Isaac (D6) | M |
| 67 | Joker (`RerollJoker`) | 🟢 | Meta | Balatro, Brettspiele | S |
| 68 | Würfeltausch (`DiceSwap`) | 🟡 | Meta | Mario Party | M |

---

## A. Bewegung

### 1. Jetpack 🟢 · M
*Inspiration:* Halo, Fortnite
- **Effekt:** Springen in der Luft halten gibt Schub nach oben. Der Treibstoff (z. B. 3 s) wird als Balken im Banner angezeigt und lädt am Boden wieder auf.
- **Umsetzung:** `OnTick` → wenn `Jump` gehalten und nicht `FL_ONGROUND`: `AbsVelocity.Z` pro Tick um den Schub erhöhen, gedeckelt auf `max_up_speed`. Treibstoff pro Tick abziehen. Optional ein kurzer Partikel-/Soundeffekt alle paar Ticks.
- **Risiko/Hinweis:** Map-Exploits (Dächer, Skybox). Maximale Flughöhe über dem Startpunkt begrenzen und per Map-Config abschaltbar machen.
- **Config:** `fuel_seconds`, `thrust`, `max_up_speed`, `recharge_per_second`.

### 2. Doppelsprung 🟢 · S
*Inspiration:* Platformer, Hollow Knight
- **Effekt:** Ein (oder N) zusätzlicher Sprung in der Luft.
- **Umsetzung:** ButtonTracker erkennt eine neue `Jump`-Flanke in der Luft → `AbsVelocity.Z = jump_velocity`, Zähler hoch. Bei Bodenkontakt zurücksetzen.
- **Abgrenzung:** Low Gravity verändert dauerhaft die Physik. Hier gibt es einen gezielten, begrenzten Extra-Sprung.
- **Config:** `extra_jumps`, `jump_velocity`.

### 3. Blink 🟢 · M
*Inspiration:* Overwatch (Tracer), Dishonored
- **Effekt:** E drücken teleportiert ~400 Einheiten in Blickrichtung. Es gibt 3 Ladungen, die sich nach 6 s regenerieren.
- **Umsetzung:** Trace von Augenhöhe entlang der Blickrichtung, Zielpunkt 32 Einheiten vor dem Treffer, dann Boden-Trace. Teleport mit SafeTeleport. Die horizontale Geschwindigkeit bleibt erhalten.
- **Risiko/Hinweis:** Die Traces auf Kopf- und Fußhöhe verhindern, dass Spieler in Wänden landen. Durch Wände geht es nicht, weil der Trace dort stoppt. E ist auch die Defuse-/Aufheben-Taste: nur auslösen, wenn kein Use-Ziel vorhanden ist, oder eine andere Taste konfigurieren.
- **Config:** `distance`, `charges`, `recharge_seconds`, `button`.

### 4. Enterhaken 🟢 · M
*Inspiration:* Just Cause, Apex (Pathfinder)
- **Effekt:** E schießt einen Haken (sichtbarer Strahl) bis 1500 Einheiten weit. Solange E gehalten wird, zieht er den Spieler zum Ankerpunkt.
- **Umsetzung:** Trace → Ankerpunkt merken. `env_beam` vom Spieler zum Anker. `OnTick`: Geschwindigkeit in Richtung Anker (z. B. 900 u/s), lösen bei Loslassen oder < 64 Einheiten Abstand.
- **Risiko/Hinweis:** Skybox- und Nodraw-Treffer ignorieren, Nutzungen pro Runde begrenzen, Beam beim Lösen entfernen. Per Map abschaltbar.
- **Config:** `range`, `pull_speed`, `uses_per_round`.

### 5. Gleiter 🟢 · S
*Inspiration:* Fortnite, Zelda BotW
- **Effekt:** E in der Luft halten begrenzt die Fallgeschwindigkeit (z. B. auf −80 u/s) und gibt etwas Vortrieb. Kein Fallschaden während des Gleitens.
- **Umsetzung:** `OnTick`: wenn in der Luft und E gehalten → `Z = max(Z, -glide_speed)`. Fallschaden in `OnPlayerTakeDamagePre` nullen, solange zuletzt geglitten wurde.
- **Abgrenzung:** Low Gravity wirkt immer und erhöht auch die Sprunghöhe. Der Gleiter wirkt nur beim Halten und nur nach unten.

### 6. Auto-Bhop 🟢 · S
*Inspiration:* Bhop-/Surf-Server
- **Effekt:** Gedrückt gehaltenes Springen hüpft automatisch weiter, ohne Timing.
- **Umsetzung:** `OnTick`: `Jump` gehalten und `FL_ONGROUND` → `Z = jump_velocity` setzen. Horizontale Geschwindigkeit auf `max_speed` deckeln. **Nicht** `sv_autobunnyhopping` setzen, denn das wäre global.
- **Config:** `max_speed`.

### 7. Slide 🟢 · S
*Inspiration:* Apex Legends, Titanfall
- **Effekt:** Ducken beim schnellen Laufen löst eine Rutschpartie mit 1,6-facher Geschwindigkeit für 0,6 s aus. Cooldown 2 s.
- **Umsetzung:** ButtonTracker auf `Duck`-Flanke, Bedingung: am Boden und horizontale Geschwindigkeit > 200. Dann einmalig einen Geschwindigkeits-Boost in Laufrichtung setzen.
- **Abgrenzung:** Increase Speed ist ein dauerhafter Tempo-Bonus. Der Slide ist ein kurzer, aktiv ausgelöster Burst.

### 8. Rückstoß-Antrieb 🟡 · S
*Inspiration:* Quake (Shotgun-Jump), TF2 (Scout)
- **Effekt:** Jeder Schuss stößt den Spieler entgegen der Blickrichtung zurück. Schrotflinten und die AWP stoßen stark, Gewehre schwach. Wer nach unten schießt, springt hoch, aber Zielen wird schwerer.
- **Umsetzung:** `EventWeaponFire` → Blickrichtung aus `EyeAngles` berechnen, `velocity -= forward * force[weaponClass]`.
- **Config:** Kraft pro Waffenklasse.

### 9. Stampfer 🟢 · M
*Inspiration:* Super Mario (Ground Pound), Overwatch (Doomfist)
- **Effekt:** E in der Luft schmettert den Spieler senkrecht nach unten. Bei der Landung erleiden Gegner im Umkreis von 200 Einheiten abstandsabhängig 20–60 Schaden und werden hochgeschleudert. Der Spieler selbst erhält keinen Fallschaden.
- **Umsetzung:** Zustand „pounding“ setzen, `Z = -1500`. Im `OnTick` den Übergang zu `FL_ONGROUND` erkennen → Gegner im Radius per DealDamage treffen und hochstoßen. Fallschaden in `OnPlayerTakeDamagePre` nullen.
- **Config:** `radius`, `min_damage`, `max_damage`, `cooldown`.

### 10. Flummi 🟡 · S
*Inspiration:* Party-Spiele
- **Effekt:** Jede Landung aus größerer Höhe federt automatisch zurück (70 % der Aufprallgeschwindigkeit). Kein Fallschaden, aber Stehenbleiben nach einem Sprung ist schwer.
- **Umsetzung:** `OnTick`: letzte Fallgeschwindigkeit merken. Beim Landen mit `|Z| > 300` → `Z = |Z| * bounce_factor`. Ducken unterdrückt das Federn (Konfigurationsoption).

### 11. Rückspulen 🟢 · M
*Inspiration:* Overwatch (Tracer), Prince of Persia
- **Effekt:** E setzt Position, Blickrichtung und HP auf den Stand von vor 3 Sekunden zurück. 1–2 Ladungen pro Runde.
- **Umsetzung:** Ringpuffer mit 12 Einträgen, alle 0,25 s geschrieben (gedrosselt im `OnTick`). Beim Auslösen per Teleport zum ältesten Eintrag. HP nur erhöhen, nie verringern.
- **Risiko/Hinweis:** Die alte Position kann inzwischen von einem anderen Spieler belegt sein. Dann `IsPlayerNearby` prüfen und auf den nächstjüngeren Eintrag ausweichen.

### 12. Glatteis 🔴 · M
*Inspiration:* Eis-Level, Mario Kart
- **Effekt:** Der Spieler rutscht wie auf Eis. Abbremsen und Richtungswechsel dauern sehr lange.
- **Umsetzung:** Variante A: `pawn.Friction` (Schema `m_flFriction`) reduzieren. Ob die CS2-Bewegung das berücksichtigt, ist **auf dem Testserver zu prüfen**. Variante B (sicher): Im `OnTick` die horizontale Geschwindigkeit des vorherigen Ticks mit Faktor 0,95 übernehmen, wenn sie größer ist als die aktuelle.

---

## B. Waffen und Treffer-Effekte

### 13. Zeus-Railgun 🟢 · M
*Inspiration:* Quake (Railgun) · *Idee aus der Anfrage*
- **Effekt:** Der Zeus hat unbegrenzte Reichweite und hinterlässt einen sichtbaren Blitzstrahl.
- **Umsetzung:** `EventWeaponFire` mit `weapon_taser` → Trace von Augenhöhe bis 8192 Einheiten. Trifft er einen gegnerischen Pawn außerhalb der normalen Zeus-Reichweite (~190 Einheiten), tödlicher Schaden per DealDamage (Killfeed zeigt den Zeus). `env_beam` für 0,3 s als Effekt.
- **Abgrenzung:** Big Taser Battery gibt mehr Schüsse. Hier geht es um die Reichweite, die Zahl der Ladungen bleibt bei 1.
- **Risiko/Hinweis:** Keine Waffen-VData anfassen (globale Reichweite).

### 14. Kettenblitz 🟢 · M
*Inspiration:* Diablo, Warcraft III
- **Effekt:** Ein Zeus-Treffer springt auf bis zu 2 weitere Gegner im Umkreis von 400 Einheiten über (je 50 Schaden).
- **Umsetzung:** `EventPlayerHurt` (Waffe `taser`, Angreifer = Würfelnder) → die nächsten Gegner mit Sichtlinie (Trace) suchen, DealDamage, Beam-Effekt zwischen den Opfern. Kettenschaden über Marker vom erneuten Auslösen ausschließen.

### 15. Wurfmesser 🟢 · L
*Inspiration:* CoD (Throwing Knife), Hotline Miami · *Idee aus der Anfrage*
- **Effekt:** Mit Messer in der Hand wirft E (oder Rechtsklick, konfigurierbar) ein Messer. Ein Treffer tötet sofort. Es gibt 3 Messer, die sich alle 10 s regenerieren.
- **Umsetzung:** Ein `decoy_projectile` spawnen, Modell per `SetModel` auf ein Messer setzen, Geschwindigkeit in Blickrichtung. `OnTick` (nur solange Messer fliegen) prüft den Abstand zu gegnerischen Hitboxen bzw. traced die Flugstrecke des letzten Ticks. Treffer → tödlicher Schaden per DealDamage, Projektil entfernen. Bei `EventGrenadeBounce` an Wänden entfernen.
- **Abgrenzung:** Cutter tötet mit dem Messer im Nahkampf. Hier geht es um eine Fernkampf-Fähigkeit mit Projektil, Flugzeit und begrenzter Munition.
- **Risiko/Hinweis:** Höchstens 3 Projektile gleichzeitig pro Spieler, nach 3 s automatisch entfernen.

### 16. Entwaffnen 🟢 · S
*Inspiration:* Fighting-Games · *Idee aus der Anfrage*
- **Effekt:** Jeder Treffer hat eine Chance von 20 % (Messer: 100 %), dass der Gegner seine aktive Waffe fallen lässt.
- **Umsetzung:** `EventPlayerHurt` (Angreifer = Würfelnder) → Zufallsprüfung → `victim.DropActiveWeapon()`. Messer und Zeus ausnehmen. Der Waffe optional einen kleinen Impuls weg vom Opfer geben.

### 17. Butterfinger 🔴 · S
*Inspiration:* Harry Potter, Party-Spiele · *Idee aus der Anfrage*
- **Effekt:** Wenn der Spieler einen Gegner trifft, lässt er mit 15 % Chance seine eigene Waffe fallen.
- **Umsetzung:** `EventPlayerHurt` (Angreifer = Würfelnder) → Zufallsprüfung → `attacker.DropActiveWeapon()`. Optional zusätzlich eine kleine Chance pro Nachladen (`EventWeaponReload`).

### 18. Explosivmunition 🟢 · M
*Inspiration:* Borderlands, GTA
- **Effekt:** Kugeln erzeugen am Einschlagpunkt eine kleine Explosion (z. B. 20 Schaden, 100 Einheiten Radius).
- **Umsetzung:** `EventBulletImpact` → SpawnExplosion mit reduziertem `Damage`/`DmgRadius`. **Höchstens eine Explosion pro 0,25 s pro Spieler**, sonst spawnt eine Negev 15 Entities pro Sekunde. Eigenschaden optional abschalten.

### 19. Brandmunition 🟢 · M
*Inspiration:* CoD (Dragon's Breath)
- **Effekt:** Getroffene Gegner brennen 3 s lang (4 Schaden alle 0,5 s) und werden orange eingefärbt. Ein weiterer Treffer frischt die Dauer auf, stapelt sich aber nicht.
- **Umsetzung:** `EventPlayerHurt` → Opfer in ein Burn-Dictionary mit Ablaufzeit eintragen. Ein Timer verteilt den Schaden per DealDamage (`DMG_BURN`, damit das Feuer-Icon im Killfeed erscheint). `Render`-Farbe setzen und am Ende zurücksetzen.

### 20. Frostmunition 🟢 · M
*Inspiration:* Diablo, Borderlands (Cryo)
- **Effekt:** Getroffene Gegner sind 1,5 s lang auf 40 % Laufgeschwindigkeit verlangsamt und bläulich eingefärbt.
- **Umsetzung:** `EventPlayerHurt` → Opfer mit Ablaufzeit merken. `OnTick` (nur für betroffene Opfer): `pawn.VelocityModifier = 0.4` setzen, weil CS2 den Wert nach jedem Treffer selbst zurücksetzt. Render-Farbe.

### 21. Blendmunition 🟢 · S
*Inspiration:* Rainbow Six (Blitz)
- **Effekt:** Mit 25 % Chance blendet ein Treffer den Gegner für 0,7 s.
- **Umsetzung:** `EventPlayerHurt` → `FlashDuration`/`BlindUntilTime` setzen, wie in `WeirdGrenades.EventPlayerBlind`.
- **Abgrenzung:** Longer Flashes verlängert Blendgranaten. Hier blenden die Kugeln selbst.

### 22. Rückstoßmunition 🟢 · S
*Inspiration:* Smash Bros., TF2
- **Effekt:** Treffer stoßen Gegner schadensabhängig weg. Auf Maps mit Abgründen (Vertigo, Nuke-Dach) ist das besonders gemein.
- **Umsetzung:** `EventPlayerHurt` → Richtung Angreifer → Opfer normalisieren, `velocity += dir * dmg * factor + Z-Anteil`. Den Gesamtimpuls deckeln.

### 23. Harpune 🟢 · S
*Inspiration:* Mortal Kombat („Get over here!“), Dota (Pudge)
- **Effekt:** Der erste Treffer alle 10 s zieht den Gegner ruckartig zum Schützen.
- **Umsetzung:** `EventPlayerHurt` → Cooldown prüfen → Opfer-Geschwindigkeit Richtung Angreifer (z. B. 800 u/s plus 200 nach oben). Eigener Sound.

### 24. Markierungsmunition 🟢 · M
*Inspiration:* Valorant (Sova), Far Cry
- **Effekt:** Getroffene Gegner leuchten 5 s lang durch Wände, aber nur für das Team des Schützen.
- **Umsetzung:** Glow-Proxy wie in `Glow.CreateGlow` am Opfer erzeugen. Per `CheckTransmit` nur an das eigene Team senden. Nach Ablauf entfernen.
- **Abgrenzung:** Glow lässt den Würfelnden selbst für alle leuchten. Hier werden Gegner nach einem Treffer markiert, und nur das eigene Team sieht die Markierung.

### 25. Schnellfeuer 🟢 · M
*Inspiration:* CoD (Double Tap)
- **Effekt:** Die Feuerrate steigt um 40 % (nur Schusswaffen).
- **Umsetzung:** `EventWeaponFire` → im `Server.NextFrame` `weapon.NextPrimaryAttackTick` um den Faktor nach vorne ziehen. Messer, Granaten und Zeus ausnehmen.
- **Risiko/Hinweis:** Die Client-Vorhersage kann leicht ruckeln. Das muss getestet werden. Keine VData-Änderung.

### 26. Blitz-Nachladen 🟢 · M
*Inspiration:* CoD (Sleight of Hand)
- **Effekt:** Nachladen dauert nur noch rund 40 % der normalen Zeit.
- **Umsetzung:** `EventWeaponReload` → Timer nach x s: `Clip1` auf Magazingröße setzen, `ReserveAmmo` um die Differenz verringern, Reload-Status beenden, `NextPrimaryAttackTick = jetzt`. Die Animation darf abbrechen.
- **Abgrenzung:** Infinite Ammo hebt den Munitionsverbrauch auf. Hier bleibt die Munition begrenzt, nur das Nachladen wird schneller.

### 27. Ladehemmung 🔴 · S
*Inspiration:* Far Cry 2
- **Effekt:** Jeder Schuss hat 8 % Chance auf eine Ladehemmung. Dann kann der Spieler 2 s lang nicht schießen, begleitet von einem Klick-Sound und einer Meldung.
- **Umsetzung:** `EventWeaponFire` → Zufallsprüfung → `NextPrimaryAttackTick += 2 s in Ticks`.

### 28. Blutmagie 🟡 · S
*Inspiration:* Diablo (Blood Magic), The Binding of Isaac
- **Effekt:** Jeder Schuss kostet 1 HP, der Schaden steigt dafür um 50 %. Unter 1 HP fällt der Spieler durch die Kosten nicht.
- **Umsetzung:** `EventWeaponFire` → `Health = max(1, Health - cost)`. In `OnPlayerTakeDamagePre` den Schaden des Würfelnden mit 1,5 multiplizieren.
- **Abgrenzung:** Damage Multiplier würfelt einen festen Faktor ohne Gegenleistung. Hier ist der Bonus an echte Kosten pro Schuss gebunden.

### 29. Goomba-Stomp 🟢 · S
*Inspiration:* Super Mario
- **Effekt:** Wer einem Gegner auf den Kopf springt, tötet ihn sofort und federt zurück nach oben.
- **Umsetzung:** `OnTick`: `pawn.GroundEntity` des Würfelnden prüfen. Ist es ein gegnerischer Spieler-Pawn → tödlicher Schaden per DealDamage, `Z = 400`. Sehr günstig, nur ein Handle-Vergleich pro Tick.

### 30. Taschendieb 🟢 · S
*Inspiration:* Thief, Skyrim
- **Effekt:** Jeder Treffer stiehlt dem Opfer 100 $ (höchstens so viel, wie es hat).
- **Umsetzung:** `EventPlayerHurt` → `InGameMoneyServices.Account` bei Opfer und Angreifer anpassen, `SetStateChanged`. Kurze Chatmeldung an beide.
- **Abgrenzung:** Increase/Decrease Money ändern Geld einmalig aus dem Nichts. Hier wird Geld bei jedem Treffer vom Gegner übertragen.

### 31. One in the Chamber 🟡 · S
*Inspiration:* CoD-Spielmodus
- **Effekt:** Der Spieler hat nur noch Messer und eine Deagle mit genau 1 Kugel. Jeder Deagle-Treffer tötet, jeder Kill gibt eine Kugel zurück.
- **Umsetzung:** Waffen entfernen, Deagle mit `Clip1 = 1`, `ReserveAmmo = 0`. In `OnPlayerTakeDamagePre` den Deagle-Schaden des Würfelnden auf tödlich setzen. Bei `EventPlayerDeath` (Angreifer = Würfelnder) `Clip1++`.

### 32. Gun Game 🟡 · M
*Inspiration:* CS Arms Race
- **Effekt:** Jeder Kill tauscht die Waffe gegen die nächste aus einer Liste (z. B. AK → MP9 → Nova → Deagle → … → Messer).
- **Umsetzung:** `EventPlayerDeath` → aktuelle Primär- und Sekundärwaffe entfernen, nächste per `GiveNamedItem` geben und aktiv schalten. Bei Rundenende nichts zurückgeben.
- **Abgrenzung:** Random Weapon würfelt einmalig eine Waffe. Gun Game ist eine Progression über Kills.

---

## C. Überleben und Verteidigung

### 33. HP-Regeneration 🟢 · S
*Inspiration:* CoD, Halo, Overwatch · *Idee aus der Anfrage*
- **Effekt:** Nach 4 s ohne Schaden regeneriert der Spieler 5 HP pro Sekunde bis zum Maximum.
- **Umsetzung:** Zeitpunkt des letzten Schadens über `EventPlayerHurt` merken. Ein Timer (1 Hz) erhöht `Health` bis `MaxHealth`.
- **Abgrenzung:** Give Health Shot gibt aktive Heilitems, Vampire heilt über verursachten Schaden. Hier heilt der Spieler passiv mit der Zeit.

### 34. Schild 🟢 · M
*Inspiration:* Halo, Apex Legends
- **Effekt:** Ein 50-Punkte-Schild absorbiert Schaden vor den HP. Er lädt nach 4 s ohne Schaden mit 10 Punkten pro Sekunde wieder auf. Der Stand wird im Banner angezeigt.
- **Umsetzung:** In `OnPlayerTakeDamagePre` den Schaden zuerst vom Schildwert abziehen und den Rest durchlassen. Ein Timer lädt nach. Beim Brechen des Schilds einen Sound und ein kurzes blaues Aufblitzen (Render-Farbe) ausgeben.
- **Abgrenzung:** Increase Health erhöht die HP einmalig. Der Schild regeneriert sich und ist von den HP getrennt.

### 35. Dornen 🟢 · S
*Inspiration:* Diablo, Minecraft
- **Effekt:** 30 % des erlittenen Schadens gehen an den Angreifer zurück.
- **Umsetzung:** `OnPlayerTakeDamagePre` → DealDamage auf den Angreifer, mit dem Würfelnden als Attacker. **Rekursionsschutz** per Marker, falls beide Dornen haben.
- **Abgrenzung:** Vampire heilt den Angreifer selbst. Dornen bestrafen den Gegner.

### 36. Schildkrötenpanzer 🟢 · S
*Inspiration:* Mario Kart, Dark Souls
- **Effekt:** Schaden von hinten wird um 75 % reduziert.
- **Umsetzung:** In `OnPlayerTakeDamagePre` den Winkel zwischen der Blickrichtung des Opfers und der Richtung zum Angreifer berechnen (Skalarprodukt < −0,3 bedeutet von hinten) → Schaden reduzieren.

### 37. Totstellen 🟢 · M
*Inspiration:* TF2 (Dead Ringer), WoW (Jäger)
- **Effekt:** Beim ersten Treffer unter 50 HP erscheint ein gefälschter Kill im Killfeed. Der Spieler wird 3 s lang unsichtbar, nimmt 90 % weniger Schaden und ist leicht schneller.
- **Umsetzung:** Ein `EventPlayerDeath` manuell erzeugen und feuern (`new EventPlayerDeath(true)` + `FireEvent`). Optional einen Ragdoll-Prop mit dem Spielermodell spawnen. Danach Render-Alpha, Schadensreduktion und Speed für 3 s.
- **Abgrenzung:** Invisible ist ein dauerhafter Zustand. Hier geht es um die Täuschung (falscher Kill) als einmalige Reaktion auf Schaden.
- **Risiko/Hinweis:** Das falsche Event darf keine Statistik-Plugins verfälschen. Darum prüfen, ob andere Plugins auf `player_death` zählen, und gegebenenfalls nur den Killfeed per Usermessage zeigen.

### 38. Superstern 🟢 · M
*Inspiration:* Super Mario
- **Effekt:** E aktiviert 6 s lang Unverwundbarkeit, +30 % Tempo und Regenbogen-Farben. Gegner, die der Spieler berührt (< 40 Einheiten), sterben sofort. Dazu läuft eine eigene Musik. Einmal pro Runde.
- **Umsetzung:** Zeitfenster speichern, Schaden in `OnPlayerTakeDamagePre` blocken, `VelocityModifier`/Speed setzen. `OnTick` wechselt die Render-Farbe alle 4 Ticks und prüft den Abstand zu Gegnern.
- **Risiko/Hinweis:** Sehr stark, daher kurze Dauer und eine geringere Würfelwahrscheinlichkeit.

### 39. Leibwächter 🟡 · M
*Inspiration:* Overwatch (Reinhardt), MMO-Tanks
- **Effekt:** Teammates im Umkreis von 300 Einheiten nehmen 40 % weniger Schaden. Dieser Anteil trifft stattdessen den Leibwächter.
- **Umsetzung:** `OnPlayerTakeDamagePre` für Teammates in Reichweite → Schaden kürzen und den Anteil per DealDamage auf den Leibwächter umlenken (Marker gegen Rekursion). Optional einen dezenten Ring-Partikel um den Leibwächter.

### 40. Glasknochen 🔴 · S
*Inspiration:* Roguelikes
- **Effekt:** Fallschaden ×4, schon kleine Stürze tun weh.
- **Umsetzung:** In `OnPlayerTakeDamagePre` den Schaden bei `DMG_FALL` multiplizieren. Zusätzlich im `OnTick` Landungen mit `|Z| > 350` erkennen, die das Spiel noch nicht als Fallschaden wertet, und kleinen Schaden per DealDamage zufügen.

---

## D. Granaten und Gadgets

### 41. Splittergranate 🟢 · M
*Inspiration:* Worms (Cluster Bomb), Battlefield · *Idee aus der Anfrage*
- **Effekt:** Eine HE-Granate zerplatzt bei der Explosion in 3–4 Tochtergranaten, die in alle Richtungen fliegen und nach 1 s explodieren.
- **Umsetzung:** `EventHegrenadeDetonate` (Werfer = Würfelnder) → an der Position 3–4 `hegrenade_projectile` spawnen, mit zufälliger horizontaler Geschwindigkeit plus Z-Anteil, `DetonateTime = jetzt + 1 s`, Werfer = Würfelnder. Die Tochtergranaten in einem HashSet markieren, damit sie **nicht erneut splittern**. Den Schaden der Tochtergranaten über `Damage` reduzieren.

### 42. Zielsuchende Granaten 🟢 · M
*Inspiration:* Worms (Homing Missile)
- **Effekt:** HE- und Brandgranaten steuern sanft auf den nächsten sichtbaren Gegner zu.
- **Umsetzung:** `OnEntitySpawned` merkt sich die Projektile des Würfelnden. Ein gedrosselter `OnTick` (jeder 2. Tick) sucht den nächsten Gegner in einem Kegel von 60° und 1200 Einheiten mit Sichtlinie und interpoliert die Geschwindigkeit leicht in Richtung Ziel. Nach der Detonation aus der Liste entfernen.

### 43. Lenkrakete 🟢 · L
*Inspiration:* Unreal Tournament (Redeemer), Half-Life (RPG)
- **Effekt:** Eine geworfene Decoy fliegt ohne Schwerkraft als Rakete in Blickrichtung des Werfers, der sie mit der Maus lenkt. Beim ersten Aufprall explodiert sie (HE-Explosion).
- **Umsetzung:** Decoy-Projektil mit `GravityScale = 0`. `OnTick` setzt seine Geschwindigkeit auf `forward(EyeAngles) * speed`. `EventGrenadeBounce` → SpawnExplosion an der Position und die Decoy entfernen. Optional die Kamera per `CameraServices.ViewEntity` auf die Rakete setzen (Machbarkeit testen).
- **Risiko/Hinweis:** Höchstens eine Rakete gleichzeitig, Selbstzerstörung nach 5 s. Der Werfer steht während des Lenkens ungeschützt.

### 44. Ender-Perle 🟢 · S
*Inspiration:* Minecraft
- **Effekt:** Der Spieler wird an die Stelle teleportiert, an der seine Decoy zuerst aufschlägt. Optional kostet das 5 HP.
- **Umsetzung:** `EventGrenadeBounce` bzw. `EventDecoyStarted` (Werfer = Würfelnder) → SafeTeleport zur Position mit einem kleinen Z-Offset. Die Decoy entfernen. Das funktioniert mit 2 Decoys pro Runde.
- **Risiko/Hinweis:** Decoys können an Stellen außerhalb der spielbaren Fläche landen. Zielpunkt per Trace absichern und Maps mit bekannten Problemstellen über die Map-Config ausschließen.

### 45. Näherungsmine 🟢 · M
*Inspiration:* GoldenEye, CoD (Claymore)
- **Effekt:** Eine geworfene HE-Granate bleibt liegen und explodiert erst, wenn ein Gegner näher als 120 Einheiten kommt. Sie piept leise. Höchstens 2 Minen gleichzeitig, nach 60 s explodieren sie von selbst.
- **Umsetzung:** `OnEntitySpawned` → `DetonateTime` weit in die Zukunft setzen. Ein gedrosselter `OnTick` (8 Hz) prüft Gegnerabstände → `DetonateTime = jetzt`. Optional ein roter Glow, sichtbar nur für das eigene Team.

### 46. Schwarzes Loch 🟢 · M
*Inspiration:* Overwatch (Zarya), Halo
- **Effekt:** Eine Decoy wird zum Gravitationsfeld. Gegner im Umkreis von 400 Einheiten werden 3 s lang zur Mitte gezogen. Am Ende gibt es eine kleine Implosion mit 25 Schaden.
- **Umsetzung:** `EventDecoyStarted` liefert die Position. `OnTick` (jeder 2. Tick, 3 s lang) addiert einen abstandsabhängigen Geschwindigkeitsvektor zur Mitte. Danach SpawnExplosion mit niedrigem Schaden und die Decoy entfernen.

### 47. Heilrauch 🟢 · S
*Inspiration:* Overwatch (Moira), Apex (Lifeline)
- **Effekt:** Teammates, die in der eigenen Smoke stehen, heilen 5 HP pro Sekunde.
- **Umsetzung:** `EventSmokegrenadeDetonate` → Timer (1 Hz) für die Smoke-Dauer: Teammates innerhalb von ~150 Einheiten heilen bis `MaxHealth`. Den Timer stoppen, wenn die Smoke-Entity ungültig wird.

### 48. Giftrauch 🟢 · S
*Inspiration:* Apex (Caustic), Valorant (Viper)
- **Effekt:** Gegner in der eigenen Smoke erleiden 4 Schaden pro Sekunde.
- **Umsetzung:** Wie Heilrauch, aber mit DealDamage auf Gegner und dem Werfer als Attacker, damit Kills korrekt gutgeschrieben werden.

### 49. Luftschlag 🟢 · M
*Inspiration:* CoD, Worms
- **Effekt:** Eine geworfene Smoke markiert ein Ziel. 3 s später fallen 5 HE-Granaten verstreut vom Himmel. Ein Warnsound ist für alle hörbar.
- **Umsetzung:** `EventSmokegrenadeDetonate` → Trace von der Markierung nach oben bis zur Decke/Skybox und den Startpunkt 50 Einheiten darunter setzen. Timer → 5× SpawnExplosion mit Fallgeschwindigkeit nach unten und Streuung von ±250 Einheiten.
- **Risiko/Hinweis:** In Innenräumen funktioniert es dank des Decken-Traces trotzdem. Der Luftschlag ersetzt die Smoke, sie wird also entfernt.

### 50. Hologramm-Köder 🟢 · M
*Inspiration:* Apex (Mirage)
- **Effekt:** Eine Decoy erzeugt statt Schussgeräuschen eine stehende Kopie des Werfers mit gleichem Modell und gleicher Blickrichtung. Wird die Kopie beschossen, zerfällt sie.
- **Umsetzung:** `EventDecoyStarted` → `prop_dynamic` mit dem Spielermodell des Würfelnden (Modellpfad aus dem SceneNode) an der Position spawnen, Idle-Animation setzen. Treffer über `OnEntityTakeDamagePre` erkennen → Prop entfernen und einen Partikeleffekt abspielen. Höchstens 2 Kopien.
- **Abgrenzung:** Disguise as Prop verwandelt den Spieler selbst. Hier entsteht eine zusätzliche Täuschfigur.

### 51. Raketensprung 🟡 · S
*Inspiration:* TF2 (Soldier), Quake
- **Effekt:** Eigene HE-Granaten verursachen beim Werfer keinen Schaden, schleudern ihn aber kräftig weg. Dazu gibt es eine neue HE alle 15 s.
- **Umsetzung:** In `OnPlayerTakeDamagePre` bei Opfer = Angreifer = Würfelnder und Explosionsschaden: Schaden auf 0 setzen, Richtung von `info.Inflictor`-Position zum Spieler berechnen und `velocity += dir * force`. Ein Timer gibt die HE nach.

### 52. Ninja-Rauchbombe 🟢 · S
*Inspiration:* Ninja-Games, Overwatch (Reaper)
- **Effekt:** E zündet sofort eine Smoke an den eigenen Füßen und gibt 2 s lang +40 % Tempo. 2 Ladungen.
- **Umsetzung:** `smokegrenade_projectile` an der Spielerposition mit Geschwindigkeit 0 spawnen, sodass die Smoke sofort zündet. Den Tempo-Boost per `VelocityModifier` bzw. Geschwindigkeits-Skalierung im `OnTick` umsetzen.

### 53. Sonnenfackel 🟢 · S
*Inspiration:* Dragon Ball (Taiyoken)
- **Effekt:** E blendet alle Gegner im Umkreis von 600 Einheiten, die den Spieler gerade ansehen. Einmal pro Runde.
- **Umsetzung:** Für jeden Gegner in Reichweite: Sichtlinie per Trace prüfen und per Skalarprodukt testen, ob er in Richtung des Spielers schaut. Dann `FlashDuration`/`BlindUntilTime` setzen. Effekt mit einem weißen Partikel und eigenem Sound.

---

## E. Aufklärung

### 54. Radar-Scan 🟢 · M
*Inspiration:* CoD (UAV)
- **Effekt:** Alle 15 s erscheinen alle Gegner für 3 s auf dem Radar des eigenen Teams.
- **Umsetzung:** Im Scan-Fenster jeden Tick `EntitySpottedState.Spotted = true` und die `SpottedByMask`-Bits der Teammates bei gegnerischen Pawns setzen. CS2 berechnet das jeden Tick neu, darum muss der Wert pro Tick erneuert werden. **Die Machbarkeit auf dem Testserver prüfen**, bevor viel Zeit hineinfließt.
- **Abgrenzung:** Echtes ESP (Umrisse durch Wände) ist bewusst verworfen. Das Radar zeigt nur grobe Positionen und ist zeitlich begrenzt.

### 55. Herzschlagsensor 🟢 · S
*Inspiration:* CoD (Heartbeat Sensor), Alien
- **Effekt:** Der Spieler sieht im Banner die Entfernung zum nächsten Gegner (z. B. „♥ 12 m“) und hört einen Herzschlag, der schneller wird, je näher der Gegner ist. Eine Richtung wird nicht angezeigt.
- **Umsetzung:** Ein Timer (2 Hz) sucht den nächsten lebenden Gegner und aktualisiert `NotifyStatus`. Der Sound wird je nach Entfernung im passenden Intervall abgespielt.

### 56. Spurenleser 🟢 · L
*Inspiration:* The Witcher, Batman (Detective Mode)
- **Effekt:** Der Spieler sieht leuchtende Fußspuren von Gegnern, die nach 5 s verblassen.
- **Umsetzung:** `EventPlayerFootstep` von Gegnern → kleiner Partikel/Decal-Prop an der Position, per `CheckTransmit` nur an den Würfelnden gesendet. **Ringpuffer mit höchstens 30 Entities**, nur jeden 2. Schritt markieren und Spuren nach 5 s entfernen.
- **Risiko/Hinweis:** Entity-Anzahl ist der Hauptkostenfaktor, darum ist das Limit Pflicht.

---

## F. Chaos und Party

### 57. Betrunken 🔴 · S
*Inspiration:* GTA, Party-Spiele
- **Effekt:** Die Kamera schwankt ständig leicht. Ab und zu macht der Spieler unkontrolliert einen kleinen Schritt zur Seite.
- **Umsetzung:** Ein Timer (alle 0,3–0,6 s) setzt `AimPunchAngle` auf einen kleinen Zufallswert (bevorzugt, weil der Effekt weich wirkt). Alternativ `Teleport(null, eyeAngles ± 3°, null)`. Gelegentlich einen kleinen seitlichen Geschwindigkeitsimpuls.

### 58. Laserpointer 🔴 · M
*Inspiration:* Taktik-Shooter
- **Effekt:** Ein für alle sichtbarer Laser in Teamfarbe zeigt, wohin der Spieler zielt, und verrät so seine Position.
- **Umsetzung:** Ein `env_beam` pro Spieler, dessen Endpunkte gedrosselt im `OnTick` (jeder 2. Tick) auf Augenhöhe und den Trace-Endpunkt gesetzt werden. Beim Tod und beim `Reset` entfernen.

### 59. Heiße Kartoffel 🟡 · M
*Inspiration:* Kinderspiel, Party-Modi
- **Effekt:** Der Spieler trägt eine tickende Bombe (30 s, Countdown im Banner, schneller werdendes Piepen). Mit einem Treffer gibt er sie an einen Gegner weiter, und dessen Timer startet bei 15 s neu. Läuft die Zeit ab, explodiert der Träger und reißt alle Spieler im Umkreis mit.
- **Umsetzung:** Den Bombenträger dice-intern verwalten, unabhängig von `_players`, damit das Opfer seinen eigenen Würfel behält. `EventPlayerHurt` (Angreifer = Träger) → Übergabe. Bei Ablauf SpawnExplosion an der Position des Trägers.

### 60. Martyrium 🟡 · S
*Inspiration:* CoD (Martyrdom)
- **Effekt:** Beim Tod lässt der Spieler eine scharfe HE-Granate fallen, die nach 1,5 s explodiert.
- **Umsetzung:** `EventPlayerDeath` (Opfer = Würfelnder) → SpawnExplosion an der Todesposition mit `DetonateTime = jetzt + 1.5`, Werfer = Würfelnder, damit Kills gutgeschrieben werden.

### 61. Voodoo-Puppe 🟢 · S
*Inspiration:* Voodoo-Mythos
- **Effekt:** Ein zufälliger Gegner wird zur Voodoo-Puppe und beide werden informiert. Jeder Schaden, den der Würfelnde erleidet, trifft zu 50 % auch die Puppe.
- **Umsetzung:** Beim `Add` einen Gegner auswählen. `EventPlayerHurt` (Opfer = Würfelnder) → DealDamage auf die Puppe (Marker gegen Rekursion). Stirbt die Puppe, wird ein neuer Gegner gewählt (optional).
- **Abgrenzung:** Bei Dornen trifft der Rückschaden den Angreifer, hier immer dieselbe zufällige Person, egal wer schießt.

### 62. Schicksalsband 🟡 · S
*Inspiration:* RPGs (Soul Link)
- **Effekt:** Der Spieler ist mit einem zufälligen anderen Spieler verbunden (Teammate oder Gegner, konfigurierbar). Stirbt einer, stirbt der andere auch.
- **Umsetzung:** `EventPlayerDeath` → beim Partner `CommitSuicide` bzw. DealDamage mit dem ursprünglichen Killer als Attacker. Re-Entrancy-Flag setzen, damit nur einmal ausgelöst wird.

### 63. Kopfgeld 🟡 · S
*Inspiration:* Red Dead Redemption, Fortnite
- **Effekt:** Auf den Spieler wird ein Kopfgeld von 2000 $ ausgesetzt und allen angekündigt. Wer ihn tötet, kassiert es. Überlebt er die Runde, erhält er selbst das Doppelte.
- **Umsetzung:** `EventPlayerDeath` → Geld an den Killer. `EventRoundEnd` → falls der Spieler noch lebt, Geld an ihn.

### 64. Auftragskiller 🟡 · S
*Inspiration:* Hitman
- **Effekt:** Der Spieler erhält einen geheimen Zielgegner. Tötet er ihn, bekommt er +3000 $ und volle HP. Andere Kills bringen kein Kill-Geld.
- **Umsetzung:** Beim `Add` ein Ziel wählen und nur dem Würfelnden mitteilen. `EventPlayerDeath` auswerten und das Kill-Geld bei Nicht-Zielen zurückbuchen.

### 65. Konfetti-Kills 🟡 · M
*Inspiration:* TF2 (Birthday Mode)
- **Effekt:** Vom Spieler getötete Gegner zerplatzen in Konfetti, begleitet von einer Party-Tröte. Rein kosmetisch, aber die Kills sind laut und auffällig.
- **Umsetzung:** `EventPlayerDeath` → `info_particle_system` an der Position des Opfers, nach 3 s entfernen. Partikel und Sound über das bestehende Workshop-Paket bzw. `Precache.cs` einbinden.

---

## G. Meta-Würfel

Diese Perks greifen in das Würfelsystem selbst ein. Heute verwaltet `RollTheDice.cs` genau **einen** Würfel pro Spieler (`_playersThatRolledTheDice`). Für 66 und 68 muss das auf mehrere Würfel erweitert werden. Dazu gehört eine Ausschlussliste für unverträgliche Kombinationen (z. B. `PlayAsChicken` + `DisguiseAsProp`).

### 66. Doppelwurf 🟡 · M
- **Effekt:** Der Spieler erhält sofort **zwei** zufällige andere Würfel.
- **Umsetzung:** Zweimal `RollTheDiceForPlayer` ohne Meta-Würfel aufrufen. Dafür ein Spieler→Liste-Mapping und Kompatibilitätsregeln einführen.

### 67. Joker 🟢 · S
- **Effekt:** Der Spieler darf in dieser Runde einmal `!rtd` erneut nutzen, um neu zu würfeln, auch wenn ihm der erste Wurf gefällt.
- **Umsetzung:** Flag pro Spieler. Beim nächsten `!rtd` erst `RemoveDiceForPlayer(…, DiceRemoveReason.NewDice)`, dann neu würfeln.

### 68. Würfeltausch 🟡 · M
- **Effekt:** Der Spieler tauscht seinen Würfel mit dem eines zufälligen Gegners, der schon gewürfelt hat. Beide erfahren, was sie bekommen haben.
- **Umsetzung:** Bei beiden Spielern `Remove`, danach das `Add` über Kreuz. Würfel mit Zustand, der an die Spielerposition gebunden ist (z. B. `PlayAsChicken`), ausschließen.

---

## Empfohlene Reihenfolge

**Welle 1: Quick Wins** (Aufwand S, hoher Spaßfaktor, keine neuen Bausteine nötig):
HP-Regeneration (33), Doppelsprung (2), Entwaffnen (16), Butterfinger (17), Rückstoßmunition (22), Harpune (23), Taschendieb (30), Ladehemmung (27), Goomba-Stomp (29), Martyrium (60), Kopfgeld (63), Joker (67).

**Welle 2: Bausteine einführen** (DealDamage, SpawnExplosion, SafeTeleport, ButtonTracker auslagern):
Jetpack (1), Zeus-Railgun (13), Splittergranate (41), Ender-Perle (44), Dornen (35), Schild (34), Heil-/Giftrauch (47/48), Raketensprung (51), Heiße Kartoffel (59).

**Welle 3: Aufwendig, aber spektakulär:**
Wurfmesser (15), Lenkrakete (43), Enterhaken (4), Luftschlag (49), Hologramm-Köder (50), Spurenleser (56), Meta-Würfel (66/68).

---

## Bewusst verworfen

| Idee | Grund |
|---|---|
| Aimbot / Auto-Aim | Unfair und wirkt für Mitspieler wie ein Cheat. Schadet dem Ruf des Servers. |
| Wallhack / volles ESP | Wie oben. Der Radar-Scan (54) und die Markierungsmunition (24) sind die fairen, begrenzten Alternativen. |
| Noclip / durch Wände gehen | Spieler bleiben stecken oder gelangen aus der Map. Exploits sind kaum abzusichern. |
| Zeitlupe / Bullet Time | Ginge nur über `host_timescale` (Cheat-ConVar). Das wirkt serverweit und desynchronisiert Clients. |
| Waffen-Umbauten über VData (Schrot-Pistole, Wallbang-Railgun, größere Magazine) | VData ist global: Alle Spieler mit dieser Waffe wären betroffen, und die Änderung überdauert die Runde. |
| Feuerspur (Molotov hinter dem Spieler) | Viele gleichzeitige Inferno-Entities sind teuer für Server und Clients. |
| Geschützturm (TF2-Sentry) | Dauerhafte Traces und eigene Schusslogik pro Turm. Großer Performance- und Wartungsaufwand. Eventuell später als stark begrenzte Variante. |
| Invertierte Steuerung | Die Client-Eingabe lässt sich serverseitig nicht zuverlässig umkehren. |
| Effekte per `ClientCommand` erzwingen | Sicherheits- und Missbrauchsrisiko. Valve blockt viele Befehle ohnehin. |
| Kamikaze (Selbstsprengung per Taste) | Zu nah an Martyrium (60). |
| Totem / Zweite Chance (Tod einmal abwenden) | Zu nah am bestehenden Respawn (beides ist ein Extra-Leben). |
| Unsichtbar beim Ducken | Direkte Abwandlung von Cloak. |
| Rückenstich-Instakill | Direkte Abwandlung von Cutter. |
| Zufälliger Teleport alle X Sekunden | Technisch identisch mit Return To Sender (Spawn-Teleport). |
| Glaskanone (doppelter Schaden, halbe HP) | Nur eine Kombination aus Damage Multiplier und Decrease Health. |
| Original-Sounds aus anderen Spielen (z. B. „Get over here!“) | Urheberrecht. Eigene Sounds aufnehmen oder lizenzfreie verwenden. |
