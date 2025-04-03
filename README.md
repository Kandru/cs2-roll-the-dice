# CounterstrikeSharp - Roll The Dice

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![Discord Support](https://img.shields.io/discord/289448144335536138?label=Discord%20Support&color=darkgreen)](https://discord.gg/bkuF8xKHUt)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-roll-the-dice?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-roll-the-dice/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-roll-the-dice](https://img.shields.io/github/issues/Kandru/cs2-roll-the-dice?color=darkgreen)](https://github.com/Kandru/cs2-roll-the-dice/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

This plugin lets your players roll the dice each round (at any time during an round) to get either a positive or negative effect for the current round.

## Current Features

- Bigger Taser Battery
- Change player name (changes the player name randomly)
- Change player model (disguise as enemy player model)
- Change player size
- Chicken Leader (spawns chickens around the player)
- Decrease health
- Decrease Money
- Instant bomb plant or bomb defuse / instant hostage grab
- Give health shots
- Increase health
- Increase Money
- Increase speed
- No explosives (no grenades)
- (Almost) no recoil (https://www.youtube.com/watch?v=s7PIG3cQo4M)
- Change player to a big chicken
- Cloak (Player is invisible after a given period of time)
- Disguise as Plant (gives player a random prop model)
- Make player glow (X-Ray through walls)
- High Gravity
- Invisibility
- Low Gravity
- Make fake gun sounds
- Make fake hostage sounds
- One HP
- Respawn after death
- Suicide
- Vampire (get the given damage as health)
- Show health bar for enemies (https://www.youtube.com/watch?v=SBKvAz9PDqs)
- Strip weapons
- Third Person View

## Plugin Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-roll-the-dice/releases/).
2. Move the "RollTheDice" folder to the `/addons/counterstrikesharp/plugins/` directory of your gameserver.
3. If you do NOT use german or english please copy the en.json in the *lang*-Folder to your language country code and edit accordingly.
4. Restart the server.

## Plugin Update

Simply overwrite all plugin files and they will be reloaded automatically or just use the [Update Manager](https://github.com/Kandru/cs2-update-manager/) itself for an easy automatic or manual update by using the *um update RollTheDice* command.

## Commands

### !dice / !rtd / !rollthedice

This command triggers the dice for a player. To activate the dice with a button paste the following to the client console:

```
bind "o" rtd
```

### !dice auto

Enables or disables the automatic !rtd mode (will roll the dice automatically on spawn depending on the settings made inside your configuration).

### !givedice (@rollthedice/admin)

This command triggers the !rtd command for all or specific players. Only available with the correct permission (@rollthedice/admin).

```
// roll randomly for everyone
!givedice

// roll randomly for a player
!givedice PlayerName

// roll a specific dice for everyone
!givedice * glow

// roll a specific dice for a player
!givedice PlayerName glow
```

### rollthedice (Server Console Only)

Ability to run sub-commands:

#### reload

Reloads the configuration.

#### disable

Disables the !rtd command instantly and remembers this state.

#### enable

Enables the !rtd command instantly and remembers this state.

## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/RollTheDice/RollTheDice.json`.

```json
{
  "enabled": true,
  "debug": false,
  "allow_rtd_during_warmup": false,
  "roll_the_dice_on_round_start": false,
  "roll_the_dice_every_x_seconds": 0,
  "cooldown_rounds": 0,
  "cooldown_seconds": 0,
  "sound_command": "",
  "price_to_dice": 0,
  "allow_dice_after_respawn": false,
  "default_gui_position": "bottom_center",
  "gui_positions": {
    "top_center": {
      "message_font": "Arial Black Standard",
      "message_font_size": 32,
      "message_color": "#FFFFFF",
      "message_shift_x": -2.9,
      "message_shift_y": 4,
      "message_draw_background": true,
      "message_background_factor": 1,
      "status_font": "Arial Black Standard",
      "status_font_size": 30,
      "status_color_enabled": "#00FF00",
      "status_color_disabled": "#FF0000",
      "status_shift_x": -2.85,
      "status_shift_y": 3.7,
      "status_draw_background": true,
      "status_background_factor": 1
    },
    "bottom_center": {
      "message_font": "Arial Black Standard",
      "message_font_size": 32,
      "message_color": "#FFFFFF",
      "message_shift_x": -2.9,
      "message_shift_y": -3.8,
      "message_draw_background": true,
      "message_background_factor": 1,
      "status_font": "Arial Black Standard",
      "status_font_size": 30,
      "status_color_enabled": "#00FF00",
      "status_color_disabled": "#FF0000",
      "status_shift_x": -2.85,
      "status_shift_y": -4.1,
      "status_draw_background": true,
      "status_background_factor": 1
    }
  },
  "dices": {
    "DiceIncreaseHealth": {
      "enabled": true,
      "max_health": 30,
      "min_health": 10
    },
    "DiceDecreaseHealth": {
      "enabled": true,
      "max_health": 30,
      "min_health": 10
    },
    "DiceIncreaseSpeed": {
      "enabled": true,
      "max_speed": 2,
      "min_speed": 1.5,
      "reset_on_hostage_rescue": true
    },
    "DiceChangeName": {
      "enabled": true,
      "min_players_for_using_player_names": 4,
      "names": [
        "Hans Wurst",
        "Fritz Frosch",
        "Klaus Kleber",
        "Otto Normalverbraucher",
        "Peter Lustig",
        "Karl-Heinz Klammer",
        "Gustav Gans",
        "Heinz Erhardt",
        "Wolfgang Witzig",
        "Ludwig Lustig",
        "Rudi R\u00FCssel",
        "Siggi Sorglos",
        "Berti Bratwurst",
        "Dieter Dosenbier",
        "Erwin Einhorn",
        "Franz Fuchs",
        "G\u00FCnther Gans",
        "Horst Hering",
        "Ingo Igel",
        "J\u00FCrgen Jux",
        "Kurt Ketchup",
        "Lars Lachs",
        "Manfred M\u00F6hre",
        "Norbert Nudel",
        "Olaf Oktopus",
        "Paul Pinguin",
        "Quirin Qualle",
        "Ralf Rabe",
        "Stefan Seestern",
        "Thomas Tintenfisch",
        "Uwe Uhu",
        "Volker Vogel",
        "Willi Wurm",
        "Xaver Xylophon",
        "Yannik Yak",
        "Zacharias Zebra",
        "Albert Apfel",
        "Bernd Banane",
        "Claus Clown",
        "Detlef Dachs",
        "Egon Eule",
        "Ferdinand Frosch",
        "Gerd Giraffe",
        "Helmut Hase",
        "Igor Igel",
        "Jochen Jaguar",
        "Knut K\u00E4nguru",
        "Lothar L\u00F6we",
        "Martin Marder",
        "Norbert Nashorn",
        "Egon Kowalski",
        "Fritz Fink",
        "Heinz Hering"
      ]
    },
    "DicePlayerInvisible": {
      "enabled": true,
      "invisibility_percentage": 0.5
    },
    "DicePlayerSuicide": {
      "enabled": true
    },
    "DicePlayerRespawn": {
      "enabled": true
    },
    "DiceStripWeapons": {
      "enabled": true
    },
    "DiceChickenLeader": {
      "amount_chicken": 16,
      "enabled": true
    },
    "DiceFastMapAction": {
      "enabled": true
    },
    "DicePlayerVampire": {
      "enabled": true,
      "max_health": 200
    },
    "DicePlayerLowGravity": {
      "enabled": true,
      "gravity_scale": 0.4
    },
    "DicePlayerHighGravity": {
      "enabled": true,
      "gravity_scale": 4
    },
    "DicePlayerOneHP": {
      "enabled": true
    },
    "DicePlayerAsChicken": {
      "enabled": true
    },
    "DicePlayerMakeHostageSounds": {
      "enabled": true,
      "max_sound_delay": 10,
      "min_sound_delay": 5
    },
    "DicePlayerMakeFakeGunSounds": {
      "enabled": true,
      "max_sound_delay": 15,
      "min_sound_delay": 5
    },
    "DiceBigTaserBattery": {
      "enabled": true,
      "max_batteries": 10,
      "min_batteries": 2
    },
    "DicePlayerCloak": {
      "enabled": true
    },
    "DiceGiveHealthShot": {
      "enabled": true,
      "max_healthshots": 5,
      "min_healthshots": 1
    },
    "DiceNoExplosives": {
      "enabled": true,
      "swap_delay": 0.1
    },
    "DiceChangePlayerModel": {
      "enabled": true
    },
    "DicePlayerGlow": {
      "enabled": true
    },
    "DiceShowPlayerHealthBar": {
      "enabled": true
    },
    "DiceNoRecoil": {
      "enabled": true
    },
    "DiceChangePlayerSize": {
      "enabled": true,
      "max_size": 1.7,
      "min_size": 1.2
    },
    "DiceIncreaseMoney": {
      "enabled": true,
      "max_money": 1000,
      "min_money": 100
    },
    "DiceDecreaseMoney": {
      "enabled": true,
      "max_money": 1000,
      "min_money": 100
    },
    "DiceThirdPersonView": {
      "enabled": true
    },
    "DicePlayerDisguiseAsProp": {
      "allow_model_change": true,
      "enabled": true
    }
  },
  "maps": {},
  "players": {},
  "ConfigVersion": 1
}
```

You can either disable the complete RollTheDice Plugin by simply setting the *enable* boolean to *false* or disable single dices from being updated. You can also specify a specific map where you want all or specific dices to be disabled (or enabled). Most dices also have further settings for their behaviour. This allows for a maximum customizability.

### enabled

Whether the plugin is globally enabled or disabled.

### debug

Whether the debug mode is enabled or disabled. Only necessary for debugging during development or trouble shooting.

### allow_rtd_during_warmup

Whether to allow !rtd during warmup.

### roll_the_dice_on_round_start

Rolls the dice automatically on round start.

### roll_the_dice_every_x_seconds

Rolls the dice every X seconds.

### cooldown_rounds

Rounds a player needs to wait until he can use !rtd again (enable only this OR the other cooldown option). Resets after map change.

### cooldown_seconds

Seconds a player needs to wait until he can use !rtd again (enable only this OR the other cooldown option). Resets after map change.

### sound_command

Sound to play when a player use !rtd. Can either be a sound file path (e.g. sounds/test/test.vsnd) or a soundevents_addon.vsndevts name (e.g. RollTheDice.Command). Please note: sound file paths will be played at 100% volume regardless of the player's volume choices. This cannot be changed. Only sound events do support changing the volume via the soundevents_addon.vsndevts file.

### price_to_dice

The cost of rolling the dice.

### allow_dice_after_respawn

Whether or not to allow to use dice again after respawn.

### default_gui_position

Sets the default GUI position to use to show the current rolled dice.

### gui_positions

Define your own GUI positions and set one of them as the default GUI position. Check this configuration for a default on top or bottom center.

### dices

All settings for all dices. You can globally enable or disable dices here as well as change some settings for each dice.

### maps

Same as dices but for each individual map. Allos to disable dices per map.

### players

All players which have changed settings for the dices will be saved here automatically.

## Compile Yourself

Clone the project:

```bash
git clone https://github.com/Kandru/cs2-roll-the-dice.git
```

Go to the project directory

```bash
  cd cs2-roll-the-dice
```

Install dependencies

```bash
  dotnet restore
```

Build debug files (to use on a development game server)

```bash
  dotnet build
```

Build release files (to use on a production game server)

```bash
  dotnet publish
```

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
