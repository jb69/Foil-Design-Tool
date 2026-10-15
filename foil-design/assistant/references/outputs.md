# Reading the output: console lines, notices and warnings

Everything the features print, what it means, and what to change. Field names in *italics* are dialog labels (see parameters.md).

## Console lines

Turn on *Log inputs* on a feature to get its `*_INPUTS` line. Every line starts with `toolVersion=`; the Feature Studio header carries the build number.

| Line | From | What it says |
|---|---|---|
| `WING_INPUTS`, `STAB_INPUTS`, `MAST_INPUTS`, `FUSELAGE_INPUTS`, `PERFORMANCE_INPUTS`, `MOLD_INPUTS` | each feature | Every input as `key=value`. `STAB_INPUTS` has `height_mm` (typed: seat offset from the fuselage centreline) and `pivotZ_mm` (where the stab pivot ended up). |
| `===== FOIL PERFORMANCE =====` ... `===== END =====` | Foil performance | The full report, then the CSV. |
| `FUSELAGE_SEAT` | Foil fuselage | The seat on the stab and the tapped holes made, or the conflict and its fix. |
| `MAST_FILLET` | Foil mast | Seam edges found where the mast meets the plate, and the fillet attempt that worked. |
| `UD_CAP_PLIES` | Foil mold | Cut list per cap: ply, length, width at root, width at the end. |
| `UD_TABS` | Foil mold (mast) | Tab plies per face, their size, fold position and interleave. |
| `LAYUP` | Foil mold | Laminate volume and cured mass, skins, UD caps, tow, resin, what to buy. |
| `STIFFNESS` | Foil mold | Deflection and cap stress; mast: stress at the plate and at the worst point, bolt tension, junction, plate bending. |
| `STAB_CONES` | Foil mold (stab) | Bolt cones made and their positions. |
| `MAST_PLUG` | Foil mold (mast) | The pod plug's cores and depth. |
| (warning) countersink deeper than the section | Foil stabiliser, and the mold's cone note | A bolt's countersink leaves under 1 mm of laminate (an M5 is 2.9 mm deep; a bolt in a 3 mm root tail cannot take it): move it forward with *Bolt 1 at* or *Bolt pitch*, a smaller *Bolt*, or *Countersink* off. The mold then shows a flat disc instead of a cone there. |
| (warning) socket as half cores | Foil mold (wing) | *Socket plug* is off with the wing's Round or Square socket on: the halves mold the bore with a seam and meeting pins; turn Socket plug on in the Socket group. |
| `MAST_PLATE` | Foil mold (mast) | The plate measured off the part (length x width x thickness) against the Foil mast's numbers; says when direct edits were found and the tray followed them, or when it could not measure and used the nominal plate. |
| `TEMPLATES` | Foil mold | Order of shapes on the template plate and each one's cut count. |

## Foil stabiliser notice

- **Seat line.** It names what *Height* is measured from. With a Round or Square socket on the wing: "Height is measured from the square socket's axis, the fuselage centreline (Z …): seat plane 4 mm below it, pivot at Z … mm (the Height that keeps the stab here with no socket on the wing)". With the pad on top the seat must be at or below the centreline; with the pad underneath, at or above. A WARNING in this line names the sign to use. With no socket (none, Flat hub, Flat mount): "Height is the stab pivot's offset from the wing pivot …". Turning a socket on or off therefore moves the stab unless *Height* changes with it: the pivot Z in the socket line is the no-socket Height (build 233).
- **Fixed interface.** "flat from … mm, full width to …, tail to …": positions from the stab pivot. They stay the same for every stab with the same Arm, so any stab fits the same fuselage.
- **Interface warnings.** "Flat start is ahead of the leading edge" or "is within 6 mm of the leading edge (LE at X mm) - held at Y mm" (6 mm with *Front ramp* on, room for the ramp; 2 mm with it off, a square front; build 234), "the trailing edge is behind Tail start", "Tail end is inside the root": the geometry is held so it still builds, but the interface is no longer fixed. Move the named field, or accept it for a stab that only ever fits its own fuselage. With a *Root tail* the LE stays where the base chord puts it (the tail is all aft); up to build 233 the notice put it a quarter of the tail further forward (LE -18 on a stab whose LE is at -13), and the pad, the bolt check and the fuselage seat followed that wrong root.
- **Bolts.** "2 M5 bolt holes 5.4 mm at 0 mm from the pivot, pitch 35 mm, 2 countersunk 11.2 mm on the underside". A skipped bolt is off the root; change *Bolt 1 at* or *Bolt pitch*. The countersink warning counts the pad, and a *Block to body* fill on the bolt line, as material under the head from build 234 ("..., pad included"), as the wing's flat mount does.
- **Tail volume and decalage** appear when a wing is above. See targets.md.

## FUSELAGE_SEAT

- `seats on the stab's mount contour, X a-b, pad plane Z … (d mm under the axis)` with `n of n tapped holes d mm for Mx`: working.
- `stab seat on top, X a-b at Z …`: the stab sits on top (*Mount side* Bottom). A flat into the top of the body is normal.
- The block that fills between the tube and the seat is the stab's own since build 198 (*Block to body* in the stab's Mount pad group, off by default from build 228: its height is minus *Height*, so it grows as the stab moves off the centreline, and with it off the pad stays exactly *Pad height*); the stab's notice reports it: `block to the body … (cheeks beside it)`, `(fills … mm under it)`, or `no block to the body needed (the fuselage's flat at the pad face is … mm wide)`.

| Conflict | Fix |
|---|---|
| pad plane above the fuselage centreline, nothing to seat on | Foil stabiliser *Height* 0 or below (−4 typical) |
| stab's underside below the centreline (pad underneath) | *Height* 0 or above (+4 typical) |
| pad plane below the tube, the stab's Block to body carries it | valid; set *Height* nearer 0 for a flush seat |
| flat seat on the tube only x mm wide | turn the stab's *Block to body* on (cheeks beside the tube), or *Height* closer to 0 |
| stab's clearance hole not bigger than the tapping drill | only with *Bolt* = Custom: fix the two hole sizes, or pick a named bolt |
| stab bolt at X skipped, off the seat | *Bolt 1 at* or *Bolt pitch* on the stab |
| parallel section runs into the tail taper | move the mast forward, the stab aft, or reduce *Parallel aft* |
| mast base not on the tube / mast leading edge outside the body | move the mast (*Mast X*) or change *Base height* |
| Mast pocket on but no Foil mast | add Foil mast above, or turn *Mast pocket* off |

Errors that stop the fuselage: no Round tube socket on the wing (*Fuselage joint* on, *Joint* = Round tube socket), a Square socket on the wing (it takes a flat bar, which Foil fuselage does not model: use your own bar, build 233), no stab above it, stab feature older than the fuselage (paste the whole Feature Studio again), no room between socket and tail (longer *Arm* or more *Overhang*).

## Foil mast

- Notice: height, chord and taper, thickness law, the root fillet result, pod summary, plate. From build 233, `WARNING: the mast's leading edge (X …) is ahead of the rear face of the wing's round/square socket (X …), so the mast foot stands on the nacelle …: set Mast X … or more, or the wing's Rear extension … mm or less`: the tube or bar starts at the socket's rear face, so the mast belongs behind it (the second fix is offered only where the wing's bore and fairing still fit).
- `MAST_FILLET: ... OK: long edges only, no propagation at 12 mm` is normal: Onshape fillets the section but not its 0.8 mm trailing edge line, which is left as a small notch. "ROOT FILLET FAILED" lists every attempt; lower *Root fillet* or add a manual fillet above Foil mold.
- Errors: thickness outside 4-25 % of chord (*Thickness* (Foil mast), *Top thick*); countersink deeper than the plate (*Plate thick* or a smaller *Bolt*); plate too narrow or too short for the bolts; bolt heads would hit the mast (*Track spacing* or smaller *Bolt*); pod doesn't fit the mast height (*Pod height*); fairing doesn't fit (*Fairing length*, *Nose start %*, *Aft extension*).

## Foil mold

Mold notice: block size, number of tiles and bed size, keys, plugs, cones, pinch-off, then the LAYUP and STIFFNESS summaries. Mast: "plate tray ... from the plate as modelled" means the tray pocket was sized from the part; fillets, chamfers and outline changes made to the plate above the mold are in it, and the bolt pins stay on the Foil mast's pattern (move bolts in the mast's dialog, not by editing holes).

**LAYUP** example: `layup 1036 cm3, 1.59 kg cured at Vf 50% | skin 2 x 200 gsm each face: 0.43 m2, 92 g | UD caps 300 gsm, 40% chord: 15 plies per cap at the root to 4 at the tip (cut 2 of each), 819 mm long | tow 800 tex: 600 g | resin 596 g in the part, mix 834 g | buy 0.53 m2 woven, 1.27 m2 UD`.

- Ply counts are **per cap**: cut every UD strip twice, one per face.
- "skins + UD caps exceed the part": lower *Cap thick %*, *Cap width %* or *Skin plies*.
- Mast: the plate's woven fill, cap run-out and tab plies are included. "tabs take more than the tow core at the plate": fewer *Tab plies* or a larger *Top thick*.

**STIFFNESS**, wing or stab: `4 g: tip deflection 6.5 mm (1.6 mm at 1 g), root EI 2459 N m2, root cap stress 137 MPa`. The load is the spanwise moment Foil performance published at its *Load factor g*; without Foil performance above, there's no check.

**STIFFNESS**, mast: `mast side 1 g at the fuselage, plate fixed: F=932 N, deflection at the fuselage 53 mm, cap stress at the plate 198 MPa, worst 243 MPa at 494 mm, track bolt tension 3.9 kN each, junction: cap force 38.5 kN, shear 2.1 MPa over 47 mm x (1 + 2 x 4 tabs), allow 15, needs 6 mm, plate bending 96 MPa, fillet 12 mm`.

- The worst point is often where the thickening starts, not the plate. To flatten it, start *Thicken from %* lower or raise *Thickness* (Foil mast).
- Junction shear over its allowable means the caps would peel off the plate: more *Tab plies*, longer *Cap run-out*, thicker *Top thick*, or a wider plate.
- 1 g of the whole mass sideways is a severe case; a hard carve is roughly 0.3-0.5 g. The rider picks *Mast side g*.

| Mold warning or error | Fix |
|---|---|
| tip section only x mm thick where the cap starts | Foil wing / stab: turn *Min tip* on, or raise *End ratio* or *Tip thick %* |
| expected n upper and m lower tiles, found … | usually harmless after a skipped key; inspect the bodies if pieces look wrong |
| block taller than the usable print height | lower *Base thickness* or the tip drop, or a taller printer |
| pinch-off groove skipped | the mold is complete; add a groove by hand or turn *Pinch-off groove* off |
| n keys skipped where a seam crosses the plug channel | normal; the neighbouring keys locate the tiles |
| the parting leaves the section by x mm near the trailing (leading) edge at y = … mm: the lower half laps over / the upper half laps under the TE (LE) … | build 235's self-check found a half wrapping round the edge, an undercut: report it with the mold's MOLD_INPUTS and the wing's WING_INPUTS. More *Split stations* and *Tip bias* are the first things to try; the console's `PARTING_CHECK` line gives the worst gap at each edge on every run |
| n slivers under 0.05 cm3 removed (a knife-edge tip …) | not a warning: a knife-edge tip left bodies of almost no volume at the cut, now deleted (build 235); turn *Min tip* on or raise *End ratio* in the Foil wing / stabiliser for a tip that can be laid up |
| Tile key … skipped: its flange leaves x mm beyond the groove and the pocket needs y | *Tile joint* = Puzzle inlay: raise *Flange width* to the value given, or a narrower *Inlay width* |
| Tile key … skipped: it would reach a half key / the socket plug's channel / a groove vent / another tile seam … | the pocket found no clear place in its flange; usually the seam beside the nacelle's plug channel; that seam keeps its other key |
| n inlay keys tilt more than 12 deg with the flange … | the floors and keys follow the flange; past 12 deg the end wedges get large: a shorter *Inlay length*, or a seam elsewhere (another *Printer Y*) |
| the flange curves off the plane its key follows (Tile key …): that key stands x mm proud … | the flange under that key is not flat (a parting station's kink, a tip's camber): sand it flush after gluing, or a shorter *Inlay length* |
| Tile key … skipped: tilted x deg with the flange, its longer pocket would reach … | the tilt allowance pushed the pocket into a key or a seam beside it; the seam keeps its other key |
| joint k at y … has no inlay: the core is x mm thick … under 6 mm | milled core with Puzzle inlay: that joint is glued without a key; a thicker section there, or a dowel by hand |
| Could not cut the cavity into the UPPER / LOWER half | first check the tip thickness as above; then more *Split stations* and a higher *Tip bias*; then confirm the Source model matches the selected part |
| Source model not found / feature older than this Foil mold | add the source feature above, or paste the whole Feature Studio again |
| key size errors | keep *Pin dia*, *Key dia* and *Key height* inside *Base thickness* − 4 mm and *Flange width* − 4 mm |
| pad underneath in the design: molded upside down | expected for a stab that sits on top of the fuselage; turn the finished part over |

## Foil performance warnings

| Warning | Meaning and first fix |
|---|---|
| wing stalls at take-off speed | raise *Take-off km/h*, or more area / CL max; see levers.md |
| take-off within 15 % of stall | the same, less urgent |
| stab CL above 0.8 in range | stab near its own stall: larger stab or change its incidence |
| cavitation number below 1.5 | thinner, less cambered sections or more depth; check with XFoil |
| depth under 2 MAC | free surface not modelled; results optimistic |
| no Foil mast / fuselage above this feature | move Foil performance below them in the feature list |
| wing / stab polar used outside its Re range | the polar's CL max is extrapolated; add a polar at nearer Reynolds numbers |
| motor pod or prop can never be submerged | lower *Pod height*, smaller *Prop dia* |
| full power cannot hold take-off speed | more *Current lim A*, *Gear ratio* or *Prop dia* |
| cruise needs more than the current limit / full throttle | more cells, lower *Prop P/D*, or a smaller foil |
| pitch-unstable under power / static margin under power only x % | lower *Pod height*, longer *Arm*, more stab area or decalage; run pitch_sweep.py |

## LE tubercles line (build 232)

With *LE tubercles* on the wing and *Tubercles* on in Foil performance, the notice ends with `LE tubercles, reported apart (not in the figures above): height h % of chord, pitch p c, flank sweep s deg; trough-first stall, local cl +r %: take-off a % of CL max (smooth b), stall v km/h (w); published CL max -5 to +6 %: take-off … %, stall … km/h; the flipper's +4 deg stall angle: … km/h if the lift curve held …; drag before stall of unknown sign within 8 % of the wing's profile drag: glide x to y at cruise (z)`, and the report carries a `Tubercles - reported apart` block with the same numbers and their sources. Rules for reading it:

- Never fold it into the sweep. The sweep, the CSV, the stall and the take-off loading in the notice are the smooth wing's; the line is what the tubercles would change, as a separate statement.
- The trough-first numbers are the tool's own: the local cl rises in the troughs by the chord's share of half the height, so the first stall begins there, a little earlier. That is the only part computed from the geometry.
- The CL max range and the stall-angle bound are published test results (Johari 2007, Hansen 2011 for 2D sections at low Re; Miklosovic 2004 for the humpback flipper at Re 0.5 million). The point of tubercles, lift kept past stall, has no number here because the polars have no post-stall branch: say so, and say it is what softens a pump stroke or a carve that has stalled the most loaded station.
- `outside the tested range` means the height or pitch, as fractions of the chord, lie beyond what was tested (height 2.5-12 % c, pitch 0.11-0.5 c): the credits are extrapolated.
- The glide range is ±8 % of the wing's profile drag, the spread between finite-wing tests (less drag) and 2D tests (more). Recommend the smooth-against-tubercled test from the same molds when a builder wants a real number.

## Joints and mounts (builds 185-228)

From build 230 the wing and stab notices carry `rolls like x° of anhedral (Cl_beta ±y/rad, this surface alone)` after the planform: the surface's own drop, gull, winglets and sweep, from a vortex lattice, as the straight anhedral from the root that rolls the same. + anhedral rolls into a sideslip, − dihedral. It compares shapes; it does not include the mast or rider.

Wing notice fragments by *Joint*:

- `tube socket: … mm tube, nacelle …` / `square socket: w x h mm bore (corners R …), nacelle …`: the two sockets, held by countersunk bolts through the nacelle's top (*Bolts*, *Bolt*, *Bolt pitch*); *Bore corner* rounds the square bore like extruded bar. From build 233 the line ends `tapers to … mm (square: w x h) by the rear face at X …, mouth wall … mm, bore … mm deep from X …`; *Mouth wall %* sets the wall left at the rear face (50 = every earlier build; 75-100 for a metal bar, `full size to the rear face` at 100). `the bore starts … mm behind the root trailing edge …: Socket depth … mm or Rear extension … mm puts 10 mm of it under the wing` is a notice, not a fault: with *Rear extension* longer than *Socket depth* (the defaults) the joint is a pod behind the wing on any root.
- `flat hub w x l mm from X … (square back), … mm fillet round the join to X …`: the hub, a D in plan with its pocket open through the square back; `(no join fillet: …)` names why a fillet could not be built (hub too close to the trailing edge, off the wing); `JOIN FILLET FAILED` should not happen and wants the WING_INPUTS line.
- `flat mount: n x M6 countersunk through holes … least laminate under a head x mm`: bolts straight down through the wing onto a bar; a skipped bolt is outside the root chord. From build 227 it continues `mount pad under the root: w mm wide, seat face h mm below the underside's lowest point at Z …, flat from X … to the TE; sets the modelled Incidence i deg against the bar (on its bare underside the wing would seat at a deg: seat line s deg off the bar)`: the pad is the shim that makes the typed *Incidence* the built one; `(no pad body: the underside already lies on the pad plane …)` on a Clark Y drawn at 2.1° with *Pad height* 0. With *Mount pad* off the same numbers come as `WARNING: no mount pad - bolted flat on its bare underside the wing seats at a deg, not the modelled i`: either turn the pad on, or set *Incidence* to a to analyse the wing as it will be built (the seat line then reads 0).

Stab notice fragments: `mount pad on top / underneath, face h mm off the skin`, `(no pad body: the section's own flat is the seat …)` when *Pad height* 0 sits on a flat side, `(square front at Flat start, no ramp)` with *Front ramp* off, `aft end on the trailing edge, which the root tail curves forward across the pad` with a *Root tail* and no *Tail* fairing (build 234: the pad stops on the trailing edge instead of its corners hanging behind it), and the block lines above.

Wing mold: `socket plug = the bore as modelled (2 cores) on a … handle`, `n bolt shanks cleared through the nacelle's walls, the countersinks molded by the upper half`, `hub molded as modelled (pocket, fillet, holes): n tapered cones …`, `flat mount: n through holes mold as pins from both halves` (`; the mount pad under the root molds in the half that forms the underside` from build 227). A plug built from 0 cores means the bore is not open at the rear face or a hole off the wing's pattern still ties it to a half. From build 235: `hub molded as modelled (pocket, fillet, nose fairing, holes): its n through holes from the wing's underside are filled, and the lower half forms each countersink and a tapered cone … to … mm under the pocket floor` for a hub bolted from below (`… mold as pins from both halves …` with *Bolt cones* off).

**Puzzle inlays** (build 235, *Tile joint* = Puzzle inlay, for a mold milled on a 3-axis router). The tile-keys segment of the mold notice becomes `puzzle inlays: n bow-tie pockets L x W mm (waist w) on a level flange, d mm deep along the flange's normal, straddling the tile seams in the parting-face flanges, inside corners R …, the waist's outside corners R …, walls vertical: mill each from the top in its tile's cavity setup | n keys built as parts (Tile key ...), L x W mm (waist w) x t mm thick, square-edged, corners rounded R … as the cutter leaves them: cut from N mm sheet faced to t, glue each into its pocket across the seam, 0.15 mm under the parting face`, then `; floors and keys follow the flange: tilt up to x deg (Tile key …), end gap up to y mm beyond the Glue gap (glue-filled)` on a sloping flange (0.6 mm at 7 deg for the default 4.85 mm key) and `; k skipped (see the warning)`, and `| n half keys …`. Each pocket's floor is a plane fitted to the flange under it, moved Inlay depth into the half along its normal; the key lies on it, so it never stands proud on a drooped wing. The keys are parts named `Tile key upper seam 1 LE` (spanwise seams: LE and TE flanges) or `Tile key lower chord seam 1 -Y` (seams along the span: the flanges at each end). With defaults the pockets are 30.2 x 12.2 mm, 5 deep, and the keys 30 x 12 x 4.85 mm cut from 5 mm sheet. The pocket is centred in the flange left beyond the land and the groove with at least 2 mm each side: a 25 mm flange with a 3 mm land and a 4 mm groove leaves 18 mm, 2.9 mm each side of the default pocket, and needs about 23.3 mm. Milled core (*Core made* = Milled): `puzzle inlays on the top and bottom faces across n of m joints, just outside the cap band: pockets …, d mm deep (Inlay depth or a quarter of the core) …; 2n keys built as parts (Core key ...) … standing 1 mm proud: glue them across the joints and sand them to the core's profile`; the dowels are dropped.

**Hub bolted from the wing's underside** (build 235, Custom hub, *Bolts from* = Wing underside): the hub line reads `n x M6 clearance holes 6.4 mm up through the wing from its underside to the pocket floor, countersunk 13.4 mm at 90 deg in the lower skin so the heads sit flush, for bolts up into the bar or the fuselage's tongue, which is tapped; least laminate under a head x mm (countersink to pocket floor); keep Clear joint on in Foil mold with a Core clear so the bolts pass through solid laminate, not foam`, with `WARNING: bolt k at X …: only x mm of laminate between its countersink and the pocket floor (…)` under 2 mm and `its countersink breaks … mm into the pocket` below 0. Move the bolts where the root is thick (*Bolt 1 from*, *Bolt pitch*), raise *Hub above apex*, shallow the *Pocket depth* or take a smaller *Hub bolt*. Example: a 138 mm root, 12 % thick with 4 % camber at 50 %, *Hub above apex* 3 and *Pocket depth* 6 leaves about 11 mm under an M6 head at 35 mm behind the leading edge and 9 mm at 70 mm. The Foil fuselage's hub body then taps blind holes up from its tongue's underside (`n x M6 blind tapped holes 5.0 mm up from the tongue's underside, d mm deep or more for the wing's bolts, which come up through it`).

**Nose fairing** (build 235): `nose fairing from X a (L mm ahead of the pocket's front wall) rising to H mm above the hub's top at the wall (Z z), h mm above the root section's apex; its back face is the wall's plane for the bar's nose, the hub's width with a rounded nose`; `(it dips … mm under the hub's top where it crosses the hub's front …)` when a short or low fairing starts just ahead of a hub that stands proud; `WARNING: the nose fairing would start … mm ahead of the root's leading edge …: started 3 mm behind it instead - Fairing length N mm or less keeps it on the wing`. *Fairing height* is normally the bar's height less *Pocket depth*. Foil fuselage's hub body warns when the wing has one: it is for a bar of your own.

Stab mold: `pad underneath in the design: molded upside down` appears only with the pad's tail fairing on; otherwise the mold keeps the stab's orientation.
