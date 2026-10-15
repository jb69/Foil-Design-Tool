# Propulsion, layout and powered trim (eFoil and foil assist)

Covers Foil performance's **Propulsion** group (performance 1.007+), the thrust couple in trim
(1.008+) and the eFoil / foil-assist layout rule (1.009+). Exact field labels and limits are in
parameters.md under Foil performance > Propulsion and Foil mast > Motor pod.

## Layout: Pod height decides what the foil is

| Layout | When | Propulsion | What Foil performance reports |
|---|---|---|---|
| **eFoil** | Low pod: it can only clear the water with the wing shallower than 2 MAC (not rideable) | Always the motor | `eFoil layout` in the notice; console gives the Pod height raise that would make it foil assist |
| **Foil assist** | High pod: the wing can still ride (>= 2 MAC deep) with the pod out | Deep ride: motor mode (pod and prop in the water). Shallow ride: pump/wave mode (pod out; the rider pumps or rides wave energy) | Both modes, each at its own depth: motor mode (drag = thrust needed) and pump/wave mode (drag and the watts to sustain) |
| No pod | Motor pod off in Foil mast | Prop assumed submerged at Wing depth | No immersion check, no thrust couple (its height is unknown) |

Neither layout is a fault. With Propulsion on, "in" and "out" cover the whole prop disc, not just
the pod: pod-in depth = pod axis + max(pod, prop) radius + 10 mm; pod-out = axis - that - 10 mm.
Riding with the pod partly submerged (the transition band) is the worst case - friction on the
wetted part plus spray on its waterline width - so check whether Wing depth sits in that band
before quoting a single L/D.

## Method: thrust = drag at steady speed

At constant speed thrust equals total foil drag (wing, stab, mast, pod, fuselage). The group
solves this at every speed, at the **powered ride depth**: Wing depth if the prop is fully under
water there, otherwise the pod-in depth (so `thrust_N` can exceed `drag_N`, which stays at Wing depth).

    drag -> prop rpm that makes that thrust (Wageningen B-series KT/KQ: diameter, P/D, EAR, blades)
         -> shaft torque and power, open-water prop efficiency
         -> motor amps = torque / (gear x gearbox eff x 8.27/kV); motor volts = rpm/kV + amps x winding R
         -> throttle = motor volts / pack volts (cells x 3.7 V)
         -> battery amps and watts (via ESC eff) -> Wh/km -> range from Pack Ah x volts x Usable %

**Top speed** is where full-power thrust (falling with speed) meets drag (rising roughly with
V^2) - not a separate calculation and not the static thrust. Full power = the torque the current
limit allows, capped near no-load rpm by back-EMF over the winding resistance.

The B-series polynomials are the same as in the companion B-series blade Feature Studio, so a
blade designed there and the foil analysed here agree (set Gearbox eff 100 % to compare exactly -
the blade check ignores gear loss).

## Reading the output

Notice: `prop at 20 km/h: 2915 rpm, 59% throttle, 468 W battery, 23.4 Wh/km, range 34 km · top speed 35.4 km/h`
plus, with a pod, `under power: thrust couple -4.5 Nm, trim CG 31 mm (+6), static margin 37% MAC`.

Console Propulsion block: drivetrain summary, the ride depth used, static full-power thrust,
no-load prop rpm and pitch speed, the cruise breakdown (thrust, rpm, J, prop efficiency, shaft W,
motor A, throttle, battery A/W, overall efficiency), Wh/km, range and minutes, top speed, the
thrust couple line, and a note if tip speed exceeds 25 m/s.

CSV columns added: `thrust_N, prop_rpm, J, eta_prop_pct, motor_A, throttle_pct, batt_A, batt_W,
Wh_per_km, max_thrust_N, in_limits, thrust_couple_Nm, trim_cg_powered_mm, static_margin_powered_pct`.
`scripts/analyse_sweep.py` summarises them (lowest Wh/km, throttle range, top speed, lowest
powered static margin).

Warnings: full power cannot hold take-off speed; cruise needs more than the current limit; cruise
needs more than 100 % throttle; the prop can never be fully submerged; powered static margin under
5 % (or negative) anywhere from take-off to max speed.

## Powered trim: the thrust couple

Thrust equals drag, so the two form a **pure couple** - the same about any point, so the CG height
does not matter. Couple = sum(each drag x its height) - thrust x prop-axis height (nose-up
positive). Heights: wing drag at the wing root pivot, stab at its Height, fuselage at its axis,
mast friction at mid-wetted span, spray at the surface, pod at its axis.

- **Thrust line above the drag centroid** (a high pod, typical of foil assist) = nose-down couple:
  the rider must stand further back under power, and the powered static margin falls.
- **Thrust line below the drag centroid** (a very low pod) = nose-up couple.
- The couple barely changes with speed while the stab's correcting moment grows with speed
  squared, so the **lowest powered static margin is at the lowest powered speed** - usually
  take-off, which is exactly when a foil-assist rider uses the motor.
- Acceleration (thrust above drag) makes the couple bigger than the steady-state value shown.

## Levers for powered pitch stability (strongest first)

Use `scripts/pitch_sweep.py` on the console log to size these for a specific design.

1. **Pod height** - the couple is thrust x (pod height - drag centroid height), so lowering the
   pod is the direct fix. It also sets the layout: below the foil-assist threshold it becomes an eFoil.
2. **Arm** - moves the neutral point aft at no stab drag cost (a longer Foil fuselage adds a
   little). Raises stance shift.
3. **Lowest powered speed** (Take-off km/h, or how the rider launches) - the margin improves quickly
   with speed; stable from 16 km/h is much easier than from 12.
4. **Stab area** - effective but costs drag and a large stance shift, because more stab also
   raises the unpowered static margin at speed.
5. **Decalage** - acts on the stab's lift, which scales with speed squared, so it is weak at
   take-off and mostly adds stance shift at speed.
6. **Tilting the motor** is not worth modelling: its vertical component acts over only the short
   distance between the pod and the CG, far less than the thrust height lever.

Worked example (prone foil assist, 95 kg, 1000 cm^2 AR 7.2 wing, 160 mm prop, pod at 600 mm on a
750 mm mast, 150 cm^2 linked stab at 620 mm, 2 deg decalage): couple -36 Nm at take-off, powered
static margin -16 % at 15 km/h, positive only from 19. Fixing it with the stab alone needed 290 cm^2
on a 920 mm arm. Pod at 550 mm, a 180 cm^2 stab at 660 mm and 3 deg decalage reached -0.6 % at 15
and +5.5 % at 16, at the cost of a 130 mm stance shift. See examples.md, session 1.

## Sanity checks

| Item | Guide |
|---|---|
| Cruise throttle | 40-70 % leaves headroom for take-off, gusts and turns; over 85 % at cruise is little margin |
| Cruise motor current | well under the limit (a third to a half); take-off needs much more |
| Static thrust vs foiling drag | ploughing before lift-off needs roughly 2-3x the foiling drag at that speed (not modelled); aim for static thrust several times cruise drag |
| Lowest Wh/km | usually just above take-off; cruising much faster than best-Wh/km speed costs range roughly with drag x speed |
| Top speed vs pitch speed | top speed can slightly exceed "pitch speed at no-load" because a cambered blade's effective pitch is above nominal - not a bug |
| Tip speed | over about 25 m/s near the surface expect ventilation and noise |
| Powered static margin | >= 5 % MAC from the lowest speed the motor is used while foiling |
| Prop choice | higher P/D = more top speed and cruise efficiency, less static thrust; judge on the full thrust curve crossing drag, never on static thrust alone |

## Not modelled

Ploughing before lift-off; prop cavitation and ventilation (only a tip-speed note); pod and mast
wake on the prop (open water); part-throttle behaviour beyond the B-series curves; acceleration;
the small lift from the prop axis tilting with body angle. B-series is an open-water estimate,
about +/- 20 % absolute - best for comparing props and layouts.
