## Overview
An "OOC Consent and Preferences" menu that adds toggles to consent to ERP mechanics (no toggles added yet), and a textbox input to communicate preferences and give consent to other players.

This is a port of Consent System V2 which is maintained here: https://gitlab.com/0x40/ss14-patches/-/tree/consent-system-v2
If you make useful changes to it on a downstream, I'd appreciate if you submitted them to the gitlab repo as well, or
ping/dm me with a link to your patch on discord: _0x40

## Features
- Consent toggles. These are easy to add with prototypes, and can be checked for with the API in `SharedConsentSystem`.
- `ConsentCondition` effect condition to make it easy to make reagents/entity effects that require a consent toggle.
- Textbox input for consent and preferences, with limited richtext support (no colors but you can use bold and the like). There's an examine icon that lets you view other players' consent texts.
- Read receipts, saving the date you last read someone's consent/preferences text. If the text was updated since you last read it, there will be a red dot on the examine icon.

## Technical details
### Types
Server.ConsentManager:
Source of truth for player consent settings.
Saves and loads settings to and from the db.
Public API for getting consent settings outside of the gamesim, from a NetUserId. (You should almost never need to use this)

Client.ConsentManager:
Stores the local player's consent settings.
Sends the consent settings to the server when the client updates it.

Shared.ConsentComponent:
Stores the entity's consent settings.
On players, this always mirrors the player's consent settings in Server.ConsentManager. (Needed for prediction)
On non-player entities, it can be set but will be overridden if a player takes control of it. So you can use this comp if you want a non-player controlled mob to never "consent" to a toggle, but are fine with it consenting while being controlled by a player. If you want a mob to **never** be subject to a certain mechanic, you should have a component that blocks it and check for that in addition to checking consent.

Shared.ConsentSystem:
Adds the examine verb for checking the consent/preferences freetext of an entity.
Public API for cheking a consent toggle of an entity.

Server.ConsentSystem:
Responsible for mirroring the consent settings from `ConsentManager` onto `ConsentComponent`s

## Changelog

### 1.0
- Initial release

### 1.1
- Make the UI auto-generate the toggle buttons.
- Make it easier to localize consent toggles.

### 2.0
- Move code to _Common folder and add MIT spdx headers.
- Add a new ConsentComponent, to allow for client prediction and for non-player entities to have consent settings.
- Add read recipes, displaying a red dot if the text has changed since you last read it.

### Floof
- Ported by SyaoranFox for Floof-Station
