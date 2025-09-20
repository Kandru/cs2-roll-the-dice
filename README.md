> [!CAUTION]
> This plug-in is in BETA-TEST since the latest big CS2 update! Not all dices and features are currently implemented. Use at your own risk and look for updates weekly. Breaking changes are also done in the config during the BETA-UPDATES! So backup before (or just update manually afterwards).


# CounterstrikeSharp - Roll The Dice

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![Discord Support](https://img.shields.io/discord/289448144335536138?label=Discord%20Support&color=darkgreen)](https://discord.gg/bkuF8xKHUt)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-roll-the-dice?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-roll-the-dice/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-roll-the-dice](https://img.shields.io/github/issues/Kandru/cs2-roll-the-dice?color=darkgreen)](https://github.com/Kandru/cs2-roll-the-dice/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

Roll The Dice is a hilarious and chaotic game mode plugin for Counter-Strike 2 that adds an element of unpredictable fun to every round. Players can use the `!rtd` command to roll a virtual dice and receive a random effect that can either give them a significant advantage or create amusing challenges. From turning into a giant chicken to becoming invisible, gaining superhuman speed, or being cursed with terrible aim, every dice roll brings excitement and laughter to the server.

This plugin transforms the traditional competitive nature of Counter-Strike 2 into a party-like atmosphere where anything can happen. Whether you're boosting your health, spawning an army of chickens as your loyal followers, or finding yourself stuck with vegetables instead of grenades, Roll The Dice ensures that no two rounds are ever the same. The effects are temporary and reset each round, keeping the gameplay fresh and encouraging players to take risks and embrace the chaos.


## Currently Available Dices

- **Big Taser Battery** - Gives you extra taser shots to zap enemies multiple times
- **Change Name** - Randomly changes your player name to confuse opponents
- **Change Player Model** - Disguises you as an enemy team player model
- **Change Player Size** - Scales your player model size randomly (smaller or larger)
- **Chicken Leader** - Spawns a flock of chickens that follow you around the map
- **Decrease Health** - Reduces your health to make you more vulnerable
- **Decrease Money** - Takes away some of your hard-earned cash
- **Give Health Shot** - Provides you with health shots for healing
- **High Gravity** - Increases gravity making you fall faster and jump lower
- **Increase Health** - Boosts your health above the normal maximum
- **Increase Money** - Adds extra money to your account for better equipment
- **Increase Speed** - Makes you run significantly faster than normal
- **Invisible** - Makes you partially or fully invisible to enemies
- **Low Gravity** - Reduces gravity allowing you to jump higher and fall slower
- **No Explosives** - Replaces all your grenades with harmless vegetables and fruits
- **No Recoil** - Removes weapon recoil for perfect accuracy
- **One HP** - Sets your health to just 1 hit point - survive if you can!
- **Respawn** - Brings you back to life after death with default weapons
- **Strip Weapons** - Removes all your weapons leaving you defenseless
- **Suicide** - Instantly eliminates you from the round
- **Vampire** - Converts damage you deal to enemies into health for yourself

## Dice Roadmap

The following dices are planned for future releases:

- **Fast Map Action** - Speeds up map-specific interactions like bomb plant and hostage pick-up
- **Glow** - Makes you glow with a colored outline visible through walls for everyone else
- **Wallhack** - Shows you the best enemy player through the wall in a given interval
- **Player as Chicken** - Transforms your player model into a chicken while retaining normal gameplay
- **Cloak** - Advanced invisibility effect while you're standing still
- **Disguise as Prop** - Transforms you into a random map prop for ultimate camouflage
- **Make Fake Gun Sounds** - You will produce random weapon firing sounds
- **Make Hostage Sounds** - Makes random hostage sounds every now and then
- **Show Player Health Bar** - Displays health bars when aiming at players
- **Third Person View** - Forces your camera into third-person perspective for the round
- **More Recoil** - Reduces weapon accuracy for less precision

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
This admin command allows administrators to force dice rolls for players. Requires the @rollthedice/admin permission. Dice names correspond to the filenames in the [dices folder](src/dices/) and are case-insensitive.

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

Deletes a map-specific configuration for the currvent map in the config file and loads the global defaults.

## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/RollTheDice/RollTheDice.json`.

```json
{
  "enabled": true,
  "debug": false,
  "allow_rtd_during_warmup": false,
  "trigger": {
    "event": "RoundStart",
    "force_all_players": false,
    "allow_player_auto_rtd": true,
    "roll_the_dice_every_x_seconds": 0
  },
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
    },
    "no_explosives": {
      "enabled": true,
      "swap_delay": 0.1,
      "model_scale": 1,
      "RandomModels": [
        "models/food/fruits/banana01a.vmdl",
        "models/food/fruits/watermelon01a.vmdl",
        "models/food/vegetables/cabbage01a.vmdl",
        "models/food/vegetables/onion01a.vmdl",
        "models/food/vegetables/pepper01a.vmdl",
        "models/food/vegetables/potato01a.vmdl",
        "models/food/vegetables/zucchini01a.vmdl"
      ]
    },
    "strip_weapons": {
      "enabled": true
    },
    "vampire": {
      "enabled": true,
      "max_health": 200
    },
    "invisible": {
      "enabled": true,
      "percentage_visible": 0.5
    }
  },
  "maps": {},
  "player_configs": {
    "76561198019045610": {
      "rtd_on_spawn": true
    }
  },
  "ConfigVersion": 1
}
```

### enabled

Enables or disables the plugin globally.

### debug

Enables debug mode for development and troubleshooting.

### allow_rtd_during_warmup

Allows players to use !rtd during warmup rounds.

### trigger

Controls when dice are automatically rolled:
- **event**: When to trigger auto-roll (`RoundStart` or `RoundFreezeEnd`)
- **force_all_players**: Forces all players to roll dice at the given event
- **allow_player_auto_rtd**: Allows players to use their automatic dice roll they can activate via `!rtd auto`
- **roll_the_dice_every_x_seconds**: Automatically rolls dice every X seconds (0 = disabled)

### cooldown_rounds

Number of rounds a player must wait before using !rtd again. Use this OR cooldown_seconds, not both (0 = disabled).

### cooldown_seconds

Number of seconds a player must wait before using !rtd again. Use this OR cooldown_rounds, not both (0 = disabled).

### sound_command

Sound played when using `!rtd`. Use sound file paths (e.g. `sounds/test.vsnd`) or sound events (e.g. `RollTheDice.Command`). Leave empty to play `RollTheDice.{DiceName}` sound events automatically.

### price_to_dice

Money it costs to roll the dice.

### allow_dice_after_respawn

Allows players to use `!rtd` again after respawning in the same round.

### dices

Configuration for all available dice effects. Each dice can be enabled/disabled and has specific settings:

- **big_taser_battery**: Extra taser shots (min_batteries to max_batteries)
- **change_name**: Random name from player_names list or actual players on the server
- **change_player_model**: Disguise as enemy team model
- **change_player_size**: Scale player size (min_size to max_size, adjust_health scales HP accordingly)
- **chicken_leader**: Spawns chickens around player (amount_chicken)
- **decrease_health**: Removes health (min_health to max_health, prevent_death stops at 1 HP)
- **increase_health**: Adds health (min_health to max_health)
- **decrease_money**: Removes money (min_money to max_money)
- **increase_money**: Adds money (min_money to max_money)
- **give_health_shot**: Gives health shots (min_shots to max_shots)
- **one_hp**: Sets player to 1 HP
- **low_gravity**: Reduces gravity (gravity_scale multiplier)
- **high_gravity**: Increases gravity (gravity_scale multiplier)
- **suicide**: Instantly kills the player
- **increase_speed**: Increases movement speed (min_speed to max_speed, reset_on_hostage_rescue)
- **no_recoil**: Removes weapon recoil
- **respawn**: Respawns player after death with specified weapons
- **no_explosives**: Replaces grenades with random food models
- **strip_weapons**: Removes all weapons
- **vampire**: Converts damage dealt into health (max_health limit)
- **invisible**: Makes player partially invisible (percentage_visible: 0.0 = fully invisible, 1.0 = fully visible)

### maps

Map-specific configuration overrides. Same structure as dice settings but only applies to specific maps.

### player_configs

Stores individual player settings automatically. Players are identified by SteamID64.


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
