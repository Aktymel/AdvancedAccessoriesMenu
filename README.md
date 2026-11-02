# Advanced Accessories Menu

A plugin to create accessory groups for easier managment, you can then move/copy/hide multiple accessories at once.

Supported games: HS2, AI, KK, KKS

| Old menu messy | | New menu clean |
|---|:---:|---|
| ![Old menu messy](AdvancedAccessoriesMenu/screenshots/1_mess.png) | ➜ | ![New menu clean](AdvancedAccessoriesMenu/screenshots/1_neat.png) |

## Features

- Create groups for accessory slots.
- Copy and move entire groups at once.
- Toggle between advanced menu and old style.
- KKS / KK outfits have their own groups.
- Groups can be hidden.
- Advanced loader integration.

## Requirements

- [BepInEx](https://github.com/BepInEx/BepInEx) 5.4
- [HS2API / KKSAPI / AIAPI](https://github.com/IllusionMods/IllusionModdingAPI) 1.48, KKAPI 1.42.2

## Installation

Copy .dll into `BepInEx\plugins`.

## Usage

**Character creator**
1. Press **Advanced** button to toggle between new menu and old.
2. Create groups by giving them a name and set range.
3. Move/copy entire groups to another slot.

**Main game**
1. open accessories, toggle with **Advanced** button

**Studio**
1. In studio look for AAM/groups under anim/status/other to open AAM window

## Compatibility

| Plugin | HS2 / AI | KKS / KK |
|---|---|---|
| MoreAccessories | [1.2.2+](https://bitbucket.org/Joan6694/hsplugins) | [2.0.21+](https://github.com/jalil49/MoreAccessories) |
| SliderHighlight | — | 2.2+ |
| [Pregnancy Plus](https://github.com/thojmr/PregnancyPlus) | ✓ | 7.9 (KKS) |

## Changelog

- 0.2.0 — Hide feature and Advanced Loader integration.
- 0.1.1 — Bug fix for KK / KKS.
- 0.1.0 — first version.

## License

GPL-3.0, see [LICENSE](LICENSE).
