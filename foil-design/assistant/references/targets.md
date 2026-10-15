# Targets and typical ranges

Rules of thumb for judging tool output. They are starting ranges drawn from common practice and the tool's own tooltips, not certified figures; brands and riders differ. Always weigh them against the user's stated goal.

## Front wing by discipline

| Discipline | Area cm^2 | Aspect ratio | Take-off km/h | Cruise km/h | Notes |
|---|---|---|---|---|---|
| Pump / downwind | 900-1500 | 9-14+ | 8-12 | 12-18 | Glide and low induced drag matter most; high AR, thin-ish sections |
| SUP / surf | 1200-2500 | 4-7 | 10-14 | 14-22 | Turning and low-speed lift; lower AR, more sweep and anhedral |
| Wing (winging) | 900-1800 | 6-10 | 12-16 | 18-28 | All-round compromise |
| Windfoil | 700-1200 | 6-10 | 12-18 | 25-35 | Higher speed; cavitation starts to matter |
| Kite race | 300-700 | 8-12 | 15-25 | 30-45+ | Small, thin, high speed; cavitation governs section choice |
| eFoil | 1000-2000 | 5-9 | 12-16 | 18-25 | Efficiency at cruise sets range; beginners want large area |

Scale area with mass: roughly proportional to total mass for the same take-off speed.

## Section

| Item | Typical |
|---|---|
| Camber % | 3-4 pump/SUP/surf, 2-3 wing/eFoil/wind, 1.5-2.5 kite; stab 0 (or -1 to -2 inverted) |
| Root thick % | 11-14 front wing, 8-10 high speed, 8-11 stab; a full-base stab (Double taper) runs thinner at the root than outboard, 7-8 % root / 9 % tip, so the wide root carrying the bolts is not a tubercle |
| Profile | NACA 4-digit for anything parametric; E817/E818 the DIY hydrofoil standards; NACA 63-412 laminar; Clark Y flat-bottomed, the easy one to lay up and check flat, and on a stab its flat face mates to the fuselage (Invert on). All four have Built-in polars |
| Camber position | Forward camber lowers the pitching moment; aft camber (a cupped trailing edge) widens the range and helps low end but costs top-end drag and needs a more powerful stab. A cupped tail is not proof of high camber: its position matters as much as its size. Reflex at the trailing edge cuts the pitching moment |
| Section CL max input | 1.0-1.2 for 8-10 % thick low camber, 1.2-1.4 for 12 % with 2-3 % camber, 1.4-1.6 for 14 % with 4 % camber; lower on small stabs (low Reynolds number) |

## Stabiliser and pitch

| Item | Target | Why |
|---|---|---|
| Stab area / wing area | 0.15-0.22 | Below: weak pitch damping; above: extra drag |
| Stab planform | Superellipse n 2.5-4, or Double taper Break 20-30 % / Mid 0.55-0.7 / Tip 0.15-0.3 / Blend 8-15 with Tip cap = Rounded + plan for the full-base outline of current production stabs | Full base: the mount and both bolts sit in the wide root, and the outer stab can be high aspect |
| Tail volume | about 0.5-0.8 | Estimate for typical foils; higher = more stable and damped, slower to pitch |
| Arm | 550-800 mm (short 450-550 surf, long 700-900+ pump) | Longer = more stable, less pitchy |
| Geometric decalage | 1.5-3 deg | Sets how much stance shifts with speed (see levers) |
| Stab CL across the speed range | within about +/-0.5; warning above 0.8 | Stab near its own stall is unpredictable |
| Static margin at take-off speed | positive; under about 5 % MAC feels twitchy | Lowest point of the range. With Propulsion on and a motor pod, judge static_margin_powered_pct instead - a high thrust line lowers it |
| Stance shift take-off to max speed | tens of mm is normal; over about 100 mm is a lot to move | Depends on decalage and camber |
| Stab trimmed to the wing | each front wing section needs its own stab power: a garage builder makes the stab oversize and trims a few mm off its tips at a time until the balance is right; in the tool, lower *Area* on the stab a few % per step |
| Board attitude (alpha) at cruise | within a few degrees of level | Riders dislike riding nose-up or nose-down |

## Performance sanity

| Item | Guide |
|---|---|
| Take-off % of CL max | under 80-85 %; 100 % = cannot fly at that speed |
| Stall vs take-off speed | stall roughly 15-25 % below intended take-off |
| L/D wing + stab only | high-teens to mid-20s for efficient designs at best-L/D speed. With a real Foil mast + Foil fuselage present, expect roughly 5-10 points lower at cruise once their drag is added - always quote which one a number is; the notice splits out mast and fuselage/pod drag |
| Other cm^2 | 1-3 - bolt heads, fittings and cables only. The mast, fuselage and motor pod come from their features, and since build 240 the joints (wing, stab and mast junctions, the socket nacelle's and hub's faces) from *Joint finish*; do not count them twice |
| Cavitation number at max speed | should exceed the section's -Cp min; below about 1.5 check with XFoil |
| Depth / MAC | above 2 to avoid free-surface effects |

## Structure

| Item | Guide |
|---|---|
| Load factor | 3 general, 4-5 pumping/jumps/hard landings, 6+ kite freestyle |
| Cap stress | 300-500 MPa wet-laid UD carbon with margin, 600-900 prepreg |
| Cap thickness | comfortable under about 20 % of the root section depth; if more, thicken the root section, widen the caps or accept a lower load factor |

## Propulsion and powered trim (eFoil / foil assist)

Full guide and sanity checks: propulsion.md. The headline targets:

| Item | Guide |
|---|---|
| Powered static margin | >= 5 % MAC from the lowest speed the motor is used while foiling (usually take-off) |
| Cruise throttle | 40-70 %; over 85 % at cruise leaves little margin |
| Cruise motor current | a third to a half of the limit or less |
| Static thrust | several times cruise drag (ploughing before lift-off is not modelled) |
| Foil-assist pod-out depth | >= 2 MAC or it is not a real mode (the tool then calls the layout eFoil) |

## Mast, joint and molds

| Item | Guide |
|---|---|
| Mast side load case (*Mast side g*) | 0.3-0.5 g of all-up mass is a hard carve; 1 g is a severe design case |
| Mast deflection at 1 g | tens of mm is typical for a carbon mast; halve it by adding about 40 % to the section thickness |
| Mast cap stress | worst point, not only the plate, under about 225 MPa (factor of 2 on wet-laid UD) |
| Mast plate junction shear | under the allowable (Shear allow / 2); 3-5 tab plies per face usually gets it well under |
| Mast thickening | start it near the waterline at ride depth: it adds stiffness with no wetted thickness |
| Wing tip deflection at the load factor | a few % of the half span at most; stiffer feels more direct |
| Minimum tip thickness | 1 mm or more molds reliably (*Min tip* on) |
| Stab seat | *Height* -3 to -5 mm with the pad on top, +3 to +5 with it underneath |
| Stab bolts | M5 or M6 for small stabs, M6 to M8 for large |
| Mast thickness for speed | Formula Kite race masts are about 12 mm thick; a home-built kite foil on a 15 mm mast could not pass about 25 knots without wipeouts, and a 12.6 to 15 mm solid carbon mast was the fix (HANDIY, 2025). Thinner needs a solid, compressed laminate to stay as stiff, and leaves less room for inserts |
| Mast plate joint | the Foil mold molds the plate with the mast, cap plies folded over a root fillet with UD tabs, and checks the shear. The home-built alternative is a separate plate, about 6 mm of carbon and glass, bonded on with a filled-epoxy fillet and a few plies each side over the joint (HANDIY). It is not checked, and it sits where the mast's moment is largest, so advise the molded joint for anything ridden hard |
| Mast to fuselage, barrel nuts | barrel nuts set in the mast on its centreline, sanded thinner than the mast and bonded under bolt load, with the tip tapered so the fuselage seats on it like a Tuttle: no box in the fuselage, but holes through the mast's thinnest section (HANDIY, 2025) |
| Mast to fuselage | bolted-through foot: no wobble but the saddle costs fuselage torsion and threaded inserts need a bulge in the foot; mortise and tenon: tight or slightly loose, never both; a permanently bonded joint with glued cross pins avoids both, but the fuselage then stays on the mast (aluminium piece with a hidden zinc anode, or titanium) |
| Mold tiles | fewer is better; each seam is a line to fair on the part |
| Clark Y incidence | ask what line a flat-bottomed plan's angle is measured to. Flat mounted directly on the fuselage: the quoted angle is the chord to the flat and goes straight in as Incidence (negative for a stab hung flat side up); the tool's table has its chord 2.1° from its flat, so 2.1 / −2.1 puts the table's flat on the bar. Angle quoted for the flat itself: add 2.1° for the chord. From build 227 the wing's Flat mount carries a *Mount pad* (on by default) that seats the typed *Incidence* whatever the section, and the notice reports the bare-underside seat angle; the 2.1 rule applies only with the pad off, or to a stab hung flat on the bar |
| Fibre volume | 45-50 % wet layup clamped in a matched mold, 55-60 % bagged or prepreg |
| Closing | *Closing* = Clamps (default) or Vacuum bag round the whole mold: the bag gives bag pressure x block plan area (5-15 kN), 1.5-4 bar on the laminate, uniform; with the bag set *Overcharge %* 3-5, keep *Groove vents* on and *Edge round* 4-6 mm; the notice's "holds X cm3 against Y cm3 of flash" must have X above Y or deepen *Groove depth* |
| Printed core | *Core* = Printed core: the mold feature generates the core as printable segments with two dowel holes at each joint, either side of the thickest point. Print with 2 walls and 10-15 % gyroid, standing on the joint face (mold settings with 5 walls gave a near-solid 510 g core: 1000 kg/m3). *Core kg/m^3* = slicer filament mass / core volume from the LAYUP line; *Core GPa* 0.5-1 |
| Printed core shear | the core between the caps carries 1.6-2.3 MPa at 3 g. Foamed filaments cannot: about 0.1-0.4 MPa at a 35 % gyroid band. Plain ASA can: about 2 MPa at 45 % gyroid, 2.3-3.3 at 50 %, and it holds its shape to 90 °C (PLA deforms at 54 °C, PETG at 68 °C, too close to a thick laminate's exotherm). Print the cap band at 45-50 % and the rest at 10 %, each segment stood on its leading edge so the layers lie across the chord. Enter *Core shear MPa* by hand: about 2 for such a band, 0.2-0.3 for a 10 % ASA core (build 226's default treats any printed core as PVC foam). |
| Wood core (no mold) | a solid wood core bagged one face at a time is the one-off route, e.g. a 1351 cm² half-ellipse on NACA 63-412 in poplar (James Goldney, 2025). Model it as Foam core with *Core kg/m^3* about 450 and *Core shear MPa* about 6. *Core GPa* stops at 5, below poplar's 8-10 along the grain, so the stiffness check reads such a wing a little soft |
| Core clear | with a joint on the wing (Round or Square socket, Flat hub, Flat mount) and *Clear joint* ticked (*Clear socket* before build 233), a foam or printed core starts at the joint's half width (nacelle, hub, or the flat mount's heads + 3 mm or half its pad) + *Core clear* (15 mm) each side, in the core mold too; the root zone is solid laminate, counted as fill in LAYUP ("root zone solid"); do not set 0 unless the socket wall is meant to be the only solid material there |
| Core mold | a second Foil mold on the same part with *Mold of* = Its foam core: molds the foam itself (sections less the skins, cap grooves), open at the tips for a poured expanding foam |
| Core | Tow fill for a solid laminate; Foam core (Corecell M80 85 kg/m3, Core GPa 0.1) to match production weights, about 0.75 kg per 1000 cm2 of wing; Monolithic where stiffness matters more than weight |
| Mold material | PLA for wing and stab molds with a slow hardener and room-temperature cure (softens at 55-60 °C); PETG for the mast mold and anything post-cured (up to about 60 °C). Print each tile standing on its spanwise end so every layer contains the section profile and the steps run along the span, not across the cavity; 0.16-0.20 mm layers, 5 walls, seam at a back corner, 8-10 mm brim, 15-20 % adaptive cubic infill with 40 % only under flanges and keys (a stab mold with the default 25 mm flange and 20 mm base sliced at 1.29 kg / 19 h with 5 walls and light infill, 1.69 kg / 39 h with dense gyroid); for a stab, Flange width 18 and Base thickness 16; enter the printer height as Printer Y; seal with epoxy, sand to 800-1200, wax. Full settings in the user guide, section 6.4 |
