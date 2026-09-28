The files
- undergarmentTop.png
- undergarmentBottom.png
- undergarmentSocks.png
Were made by Mnemotechnician (Github) for use in Floofstation/Panta-Rhei/Euphoria using SlotBackground as a template, and can be distributed under the license CC-BY-SA-3.0.

Sprites not listed above were made by... someone else, see meta.json in the parent directory.

Spriting strategy per theme:
- Default: baseline sprite - based on Default/SlotBackground.png; made using the color pallete of other Default sprites
  Sprites for other themes were based on their respective SlotBackgrounds, by pasting the unique parts of sprites made for the default theme on top of the respective theme's background and hueshifting them in the hsl+ colorspace until the colors roughly match the colors of the og sprites:
```
- ashen:      -100s -41l  (for some reason the socks sprite needed -21l? not sure, may have used the wrong colorspace - i think -21 is in hsl+)
- minimalist: +18h -8s -15l
- plasmafire: -180h +40s +3l (after further comparison might need extra +5h? i also had to tweak some sprites manually to remove way too saturated highlights)
- retro:      -87h +100s -5l (wasn't really good but i couldn't care less at this point)
- slimecore:  -76h +19s
- clockwork:  -162h +34
```
