# Model limits

- **The operating check** is the Wageningen B-series open-water polynomials (Oosterveld and van Oossanen, 1975), corrected for the hub ratio. It uses diameter, P/D, EAR, blade count and the hub only. Rake, skew, tip pitch, thickness, section, camber and tip shape do not enter, so changing them never moves the check's numbers even when they matter on the water. Treat its figures as about ±20 %.
- **Static thrust** is its most reliable figure. **Cruise energy** reads low: it leaves out motor iron and no-load losses, the ESC, the pod's wake on the prop and the mast.
- **A non-B-series section**: the check describes the equivalent B-series blade.
- **Cavitation** is judged from the cavitation number at 0.7R against the blade loading, not from the section's pressure distribution.
- **Folder loads** are the blade's moment carried as a couple between pin and stop at the steady point; the impact when the blades snap open is not modelled and can be many times larger. A soft bumper on the stop and a throttle ramp are the remedies.
- **The mold's layup line** is cloth arithmetic from the section thickness, not a structural analysis. Its ply counts assume the section's thickness falls off about its crest like the B-series form.
- **Propeller model from part** measures with rays along the shaft; check its pitch, chord and thickness against the propeller's own drawing.
- **The forging mold's strength check** uses estimated strengths for forged carbon and printed PETG.
