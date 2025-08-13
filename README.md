> [!CAUTION]
> This plug-in is in BETA-TEST since the latest big CS2 update! Not all dices and features are currently implemented. Use at your own risk and look for updates weekly. Breaking changes are also done in the config during the BETA-UPDATES! So backup before (or just update manually afterwards).


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

#### createmapconfig

Creates a map-specific configuration for the current map in the config file from the global defaults for further editing.

#### deletemapconfig

Deletes a map-specific configuration for the current map in the config file and loads the global defaults.

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
  "sound_command": "sounds/ui/coin_pickup_01.vsnd",
  "price_to_dice": 0,
  "allow_dice_after_respawn": false,
  "dices": {
    "big_taser_battery": {
      "enabled": true,
      "min_batteries": 3,
      "max_batteries": 10
    },
    "change_name": {
      "enabled": true,
      "player_names": [
        "Anonymous",
        "Player",
        "Gamer",
        "Warrior",
        "Hero",
        "Legend",
        "Champion",
        "Stranger"
      ]
    },
    "change_player_model": {
      "enabled": true
    },
    "change_player_size": {
      "enabled": true,
      "min_size": 0.5,
      "max_size": 1.5,
      "adjust_health": true
    },
    "chicken_leader": {
      "enabled": true,
      "amount_chicken": 16
    },
    "decrease_health": {
      "enabled": true,
      "min_health": 10,
      "max_health": 30,
      "prevent_death": true
    },
    "increase_health": {
      "enabled": true,
      "min_health": 10,
      "max_health": 30
    },
    "decrease_money": {
      "enabled": true,
      "min_money": 100,
      "max_money": 1000
    },
    "increase_money": {
      "enabled": true,
      "min_money": 100,
      "max_money": 1000
    },
    "give_health_shot": {
      "enabled": true,
      "min_shots": 1,
      "max_shots": 5
    },
    "one_hp": {
      "enabled": true
    },
    "low_gravity": {
      "enabled": true,
      "gravity_scale": 0.4
    },
    "high_gravity": {
      "enabled": true,
      "gravity_scale": 4
    },
    "suicide": {
      "enabled": true
    },
    "increase_speed": {
      "enabled": true,
      "min_speed": 1.5,
      "max_speed": 2,
      "reset_on_hostage_rescue": true
    },
    "no_recoil": {
      "enabled": true
    },
    "respawn": {
      "enabled": true,
      "default_primary_weapon": "weapon_m4a1",
      "default_secondary_weapon": "weapon_deagle"
    }
  },
  "maps": {},
  "player_configs": {},
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

Hint: if you leave this empty the plug-in will attempt to play a sound event named RollTheDice.{rolledDice}, e.g. *RollTheDice.OneHP*. You can use this to announce the dice a player did roll.

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
