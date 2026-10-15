FeatureScript 3083;
import(path : "onshape/std/common.fs", version : "3083.0");

// ============================================================================
// B-Series Propeller Blade Generator
// Version: build 115 (2026-10-15)
//   blade 74 | prop 81 | mold 109 | from part 2 | forge 4
// The build number is PROP_FILE_BUILD below, printed as the first line of every
// log ("[blade] B-Series Propeller Blade Generator build 71"), so a pasted log
// always says which build made it. The changelog follows this header.
//
// PASTE-IN: from build 104 the file starts with the two header lines ("FeatureScript 3083;" and the
// common.fs import): paste it over the whole Feature Studio. A push without them does not compile
// (the studio lists no features and new features are refused with "feature list becoming invalid").
//
// Frame:  Z = shaft axis; the maths runs with +Z = direction of travel (suction side faces +Z), and the finished bodies are
//         turned 180 deg about X at the end so the part is shown AFT UP: the tail toward +Z, travel toward -Z (b71). Every z in the log is in the maths frame (+Z upstream).
//         +Y = blade generator line (radial), origin on the shaft axis
// Output: one standalone solid blade, lofted through "Sections" wrapped
//         profiles from r/R = 0.2 to the tip, plus a
//         mate connector at the origin. Union with the hub / root knuckle and
//         fillet downstream.
//
// Sections are true Wageningen B-series (V1/V2 ordinates, B-series chord,
// generator-line and max-thickness distributions), wrapped onto cylinders of
// radius r so the finished blade really is diameter D.
//
// Thickness: "Root thickness" applies at the hub surface (Hub diameter / D),
// tapering to "Tip thickness" as (1 - n)^exponent: 1 = linear, 2 = parabolic
// ("Thickness taper exponent"). The part of the blade inside the
// hub keeps following the same curve, so it is thicker still.
// Edges: leading edge closed with a semi-ellipse, trailing edge square-cut.
// Pitch: nominal (P/D x D) from the root to 0.7R, then blended to the tip by
// "Tip pitch change" (negative = unloaded tip).
// ============================================================================

// The build that made this file; bump it on every change together with the Version line above.
export const PROP_FILE_BUILD = "115";

// The build history is kept with the source, not in the published copy.

const TABLE_STATIONS = [0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 0.95, 1.0];

// Chord factor K = c * Z / (D * EAR). Published to 0.9; 0.95 is a faired
// estimate; the 1.0 station uses the "Tip chord" parameter (published K = 0).
const K_Z3 = [1.633, 1.832, 2.000, 2.120, 2.186, 2.168, 2.127, 1.657, 1.22, 0];   // Z = 2, 3
const K_Z4 = [1.662, 1.882, 2.050, 2.152, 2.187, 2.144, 1.970, 1.582, 1.17, 0];   // Z = 4..7

// a/c : leading edge to generator line, as a fraction of chord.
// Published to r/R = 0.9; the last two are faired continuations of the trend.
// They must stay monotonic - a dip or jump here kicks the outline sideways and
// shows up as a hook or fin near the tip.
const AC_Z3 = [0.616, 0.611, 0.599, 0.583, 0.558, 0.526, 0.481, 0.400, 0.355, 0.320];
const AC_Z4 = [0.617, 0.613, 0.601, 0.586, 0.561, 0.524, 0.463, 0.351, 0.300, 0.260];

// b/c : leading edge to max thickness (0.35c inboard, moving to 0.5c at the tip)
const BC_Z3 = [0.350, 0.350, 0.350, 0.355, 0.389, 0.442, 0.478, 0.500, 0.500, 0.500];
const BC_Z4 = [0.350, 0.350, 0.351, 0.355, 0.389, 0.443, 0.479, 0.500, 0.500, 0.500];

// ---- Chordwise stations: P = +1 leading edge, 0 max thickness, -1 trailing edge
const P_STATIONS = [1.0, 0.95, 0.9, 0.85, 0.8, 0.7, 0.6, 0.5, 0.4, 0.2, 0.0,
                    -0.2, -0.4, -0.5, -0.6, -0.7, -0.8, -0.9, -0.95, -1.0];

// V1 (face lift / washback). Rows r/R = 0.2 .. 0.6; zero from 0.7 outward.
const V1_TABLE = [
    [0.3560, 0.2821, 0.2353, 0.2000, 0.1685, 0.1180, 0.0804, 0.0520, 0.0304, 0.0049, 0, 0.0172, 0.0592, 0.0880, 0.1207, 0.1570, 0.1967, 0.2400, 0.2630, 0.2826],
    [0.2923, 0.2186, 0.1760, 0.1445, 0.1191, 0.0790, 0.0503, 0.0300, 0.0148, 0.0027, 0, 0.0033, 0.0202, 0.0376, 0.0623, 0.0943, 0.1333, 0.1790, 0.2040, 0.2306],
    [0.2181, 0.1467, 0.1088, 0.0833, 0.0637, 0.0357, 0.0189, 0.0090, 0.0033, 0, 0, 0, 0.0044, 0.0116, 0.0214, 0.0395, 0.0630, 0.0972, 0.1200, 0.1467],
    [0.1278, 0.0778, 0.0500, 0.0328, 0.0211, 0.0085, 0.0034, 0.0008, 0, 0, 0, 0, 0, 0.0012, 0.0040, 0.0100, 0.0190, 0.0330, 0.0420, 0.0522],
    [0.0382, 0.0169, 0.0067, 0.0022, 0.0006, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
];

// V2 (thickness form). Rows r/R = 0.2 .. 0.8; from 0.9 outward V2 = 1 - P^2.
const V2_TABLE = [
    [0, 0.1560, 0.2840, 0.3905, 0.4777, 0.6190, 0.7277, 0.8170, 0.8875, 0.9750, 1, 0.9446, 0.7984, 0.6995, 0.5842, 0.4535, 0.3060, 0.1455, 0.0640, 0],
    [0, 0.1890, 0.3197, 0.4265, 0.5130, 0.6505, 0.7520, 0.8315, 0.8920, 0.9750, 1, 0.9583, 0.8265, 0.7335, 0.6195, 0.4885, 0.3360, 0.1670, 0.0800, 0],
    [0, 0.1935, 0.3235, 0.4335, 0.5220, 0.6590, 0.7593, 0.8345, 0.8933, 0.9725, 1, 0.9645, 0.8415, 0.7525, 0.6353, 0.5040, 0.3500, 0.1810, 0.0905, 0],
    [0, 0.1750, 0.3056, 0.4135, 0.5039, 0.6430, 0.7478, 0.8275, 0.8880, 0.9710, 1, 0.9639, 0.8456, 0.7580, 0.6439, 0.5140, 0.3569, 0.1865, 0.0950, 0],
    [0, 0.1485, 0.2720, 0.3775, 0.4620, 0.6060, 0.7200, 0.8090, 0.8790, 0.9690, 1, 0.9613, 0.8426, 0.7530, 0.6415, 0.5110, 0.3585, 0.1885, 0.0965, 0],
    [0, 0.1240, 0.2337, 0.3300, 0.4140, 0.5615, 0.6840, 0.7850, 0.8660, 0.9675, 1, 0.9600, 0.8400, 0.7500, 0.6400, 0.5100, 0.3600, 0.1900, 0.0975, 0],
    [0, 0.1050, 0.2028, 0.2925, 0.3765, 0.5265, 0.6545, 0.7635, 0.8520, 0.9635, 1, 0.9600, 0.8400, 0.7500, 0.6400, 0.5100, 0.3600, 0.1900, 0.0975, 0]
];

// Tip planform. Outboard of TIP_BLEND_START the chord is drawn down to the tip
// chord as (1 - m^2)^TIP_BLEND_EXPONENT, m = 0 at the blend start, 1 at the tip.
// The exponent controls the corner: 1 = parabolic (soft but still pointed at the
// tip), 0.5 = elliptical (a fully rounded tip). Below 1 the outline turns over
// smoothly at the tip instead of meeting it at an angle.
const TIP_BLEND_START = 0.9;

// Rounded tip: over the outermost TIP_ROUND_SPAN of radius the whole section -
// chord and thickness together - is scaled on an ellipse, so the blade closes
// with a dome instead of a flat end face. It stops at TIP_ROUND_FLOOR rather
// than zero, because a section scaled to nothing cannot be lofted or printed.
const TIP_ROUND_SPAN = 0.02;
const TIP_ROUND_FLOOR = 0.45;
const TIP_ROUND_SECTIONS = 3;
const TIP_BLEND_EXPONENT = 0.7;

// ---- Alternative section profiles ------------------------------------------
// The B-series carries its own tabulated ordinates (V1/V2 above). These are the
// alternatives, given at the same P stations as normalised shapes: thickness 1.0
// at the point of maximum thickness, camber 1.0 at the point of maximum camber.
//
// NACA_16 is the 16-series thickness form (NACA four-digit modified, maximum
// thickness at 0.50c, leading-edge radius index 4) - the classic propeller and
// cavitation section. NACA_66 is the same construction placed at 0.45c, which
// matches the 66-series crest position; it is an approximation of the published
// 66 ordinates, not a transcription of them. Both use the NACA a = 0.8 mean line,
// the standard marine camber line, which loads the section evenly over the
// forward 80 % of chord and unloads the tail.
//
// OGIVAL is a flat-faced circular-arc back, the traditional cheap workboat
// section: easy to print, tolerant, and less efficient. BICONVEX is a
// symmetrical circular arc on both sides, with camber only if you add it.

const NACA16_THICKNESS = [0.0000, 0.3287, 0.4681, 0.5719, 0.6549, 0.7796, 0.8658, 0.9243, 0.9620, 0.9954,
                          1.0000, 0.9922, 0.9373, 0.8775, 0.7883, 0.6639, 0.4982, 0.2856, 0.1598, 0.0200];
const NACA16_CAMBER    = [0.0000, 0.1586, 0.2711, 0.3657, 0.4481, 0.5870, 0.6993, 0.7905, 0.8635, 0.9615,
                          1.0000, 0.9786, 0.8892, 0.8121, 0.7027, 0.5424, 0.3586, 0.1712, 0.0818, 0.0000];
const NACA66_THICKNESS = [0.0000, 0.4074, 0.5515, 0.6506, 0.7254, 0.8315, 0.9004, 0.9452, 0.9730, 0.9968,
                          1.0000, 0.9922, 0.9373, 0.8775, 0.7883, 0.6639, 0.4982, 0.2856, 0.1598, 0.0200];
const NACA66_CAMBER    = [0.0000, 0.1466, 0.2517, 0.3406, 0.4186, 0.5514, 0.6607, 0.7514, 0.8262, 0.9347,
                          0.9934, 1.0000, 0.9291, 0.8585, 0.7557, 0.5973, 0.3983, 0.1907, 0.0910, 0.0000];
const ARC_SHAPE        = [0.0000, 0.0975, 0.1900, 0.2775, 0.3600, 0.5100, 0.6400, 0.7500, 0.8400, 0.9600,
                          1.0000, 0.9600, 0.8400, 0.7500, 0.6400, 0.5100, 0.3600, 0.1900, 0.0975, 0.0000];

// NACA four-digit 00xx thickness, from the original polynomial. Crest at 0.30c,
// blunter nose than the 16-series, and forgiving at low Reynolds number.
const NACA4_THICKNESS  = [0.0000, 0.3438, 0.4732, 0.5661, 0.6394, 0.7506, 0.8311, 0.8906, 0.9342, 0.9852,
                          1.0000, 0.9382, 0.7872, 0.6886, 0.5776, 0.4554, 0.3223, 0.1779, 0.1011, 0.0210];

// Clark Y, from its published ordinates. Crest at 0.30c, and flat underneath
// from about 0.30c aft, which gives a printed blade a clean build surface.
const CLARKY_THICKNESS = [0.0000, 0.3267, 0.4631, 0.5626, 0.6385, 0.7518, 0.8311, 0.9009, 0.9414, 0.9810,
                          1.0000, 0.9443, 0.8055, 0.7051, 0.5918, 0.4644, 0.3221, 0.1721, 0.0922, 0.0103];
const CLARKY_CAMBER    = [0.0000, 0.6409, 0.6959, 0.7400, 0.7769, 0.8379, 0.8844, 0.9265, 0.9547, 0.9841,
                          1.0000, 0.9443, 0.8055, 0.7051, 0.5918, 0.4644, 0.3221, 0.1721, 0.0922, 0.0103];

// Eppler-Shen YS-920, resampled from the published coordinates (UIUC airfoil
// database, m-selig.ae.illinois.edu). A marine propeller section designed to
// widen the cavitation-free range of angle of attack: crest at 0.343c and, more
// importantly, camber peaking at about 0.66c. That aft loading is the point -
// it keeps the suction peak away from the nose, which is where cavitation and
// ventilation start. The closest thing in this list to a section actually drawn
// for the job a small propeller near a free surface has to do.
const YS920_THICKNESS  = [0.0000, 0.2494, 0.3733, 0.4704, 0.5511, 0.6826, 0.7847, 0.8643, 0.9234, 0.9836,
                          1.0000, 0.9641, 0.8631, 0.7882, 0.6929, 0.5622, 0.3860, 0.2020, 0.1008, 0.0000];
const YS920_CAMBER     = [0.0000, 0.0198, 0.0377, 0.0546, 0.0736, 0.1120, 0.1548, 0.2046, 0.2646, 0.4291,
                          0.5992, 0.8583, 0.9931, 1.0000, 0.9527, 0.8167, 0.5691, 0.2931, 0.1500, 0.0000];

// Eppler E817, from the published coordinates (UIUC). One of Eppler's hydrofoil
// sections: crest at 0.33c with camber peaking near 0.69c, so aft-loaded in the
// same way as the YS-920 and for the same reason. Thinner nose than the YS-920,
// which trades a little tolerance of off-design angle for less drag.
const E817_THICKNESS   = [0.0000, 0.2487, 0.3747, 0.4718, 0.5535, 0.6806, 0.7797, 0.8567, 0.9145, 0.9781,
                          1.0000, 0.9407, 0.7888, 0.6865, 0.5602, 0.4117, 0.2540, 0.1101, 0.0475, 0.0000];
const E817_CAMBER      = [0.0000, 0.0151, 0.0281, 0.0413, 0.0556, 0.0857, 0.1210, 0.1632, 0.2142, 0.3399,
                          0.4778, 0.7505, 0.9512, 1.0000, 0.9819, 0.8780, 0.6915, 0.4229, 0.2398, 0.0000];

// Half-ellipse thickness, crest at 0.50c. No crest to speak of, so the gentlest
// pressure distribution in the list and the most cavitation-tolerant, at the
// cost of efficiency. Useful as a reference point.
const ELLIPSE_THICKNESS = [0.0000, 0.3122, 0.4359, 0.5268, 0.6000, 0.7141, 0.8000, 0.8660, 0.9165, 0.9798,
                           1.0000, 0.9798, 0.9165, 0.8660, 0.8000, 0.7141, 0.6000, 0.4359, 0.3122, 0.0000];

export enum Rotation
{
    annotation { "Name" : "CW from astern (right-hand)" }
    CW,
    annotation { "Name" : "CCW from astern (left-hand)" }
    CCW
}

export enum TailStyle
{
    annotation { "Name" : "Flat face" }
    FLAT,
    annotation { "Name" : "Cone cap (convex)" }
    CONE,
    annotation { "Name" : "Dish (concave)" }
    DISH
}

export enum BoltSize
{
    annotation { "Name" : "M3" }
    M3,
    annotation { "Name" : "M4" }
    M4,
    annotation { "Name" : "M5" }
    M5,
    annotation { "Name" : "M6" }
    M6,
    annotation { "Name" : "M8" }
    M8,
    annotation { "Name" : "Custom" }
    CUSTOM
}

/**
 * Metric bolt table, mm: clearance hole (ISO 273 medium), head recess (ISO 4762 socket head dk + 1), head recess depth
 * (head height k + 1), captive nut pocket across flats (ISO 4032 s + 0.3) and depth (nut height m + 1).
 */
function boltDims(size is BoltSize) returns map
{
    if (size == BoltSize.M3) return { "hole" : 3.4 * millimeter, "head" : 6.5 * millimeter, "headDepth" : 4 * millimeter, "nutFlats" : 5.8 * millimeter, "nutDepth" : 3.4 * millimeter, "name" : "M3" };
    if (size == BoltSize.M4) return { "hole" : 4.5 * millimeter, "head" : 8 * millimeter, "headDepth" : 5 * millimeter, "nutFlats" : 7.3 * millimeter, "nutDepth" : 4.2 * millimeter, "name" : "M4" };
    if (size == BoltSize.M5) return { "hole" : 5.5 * millimeter, "head" : 9.5 * millimeter, "headDepth" : 6 * millimeter, "nutFlats" : 8.3 * millimeter, "nutDepth" : 5.7 * millimeter, "name" : "M5" };
    if (size == BoltSize.M6) return { "hole" : 6.6 * millimeter, "head" : 11 * millimeter, "headDepth" : 7 * millimeter, "nutFlats" : 10.3 * millimeter, "nutDepth" : 6.2 * millimeter, "name" : "M6" };
    return { "hole" : 9 * millimeter, "head" : 14 * millimeter, "headDepth" : 9 * millimeter, "nutFlats" : 13.3 * millimeter, "nutDepth" : 7.8 * millimeter, "name" : "M8" };
}

export enum HubType
{
    annotation { "Name" : "Fixed blades" }
    FIXED,
    annotation { "Name" : "Folding blades" }
    FOLDING
}

export enum PinType
{
    annotation { "Name" : "Straight pin" }
    PIN,
    annotation { "Name" : "Cap-head bolt and captive nut" }
    BOLT
}

export enum MountType
{
    annotation { "Name" : "None (solid hub)" }
    NONE,
    annotation { "Name" : "Plain bore" }
    BORE,
    annotation { "Name" : "Bore and keyway" }
    KEYED,
    annotation { "Name" : "Bolt circle" }
    BOLTED
}

export enum SectionProfile
{
    annotation { "Name" : "B-series (default)" }
    B_SERIES,
    annotation { "Name" : "NACA 16" }
    NACA_16,
    annotation { "Name" : "NACA 66-style" }
    NACA_66,
    annotation { "Name" : "Ogival (flat face)" }
    OGIVAL,
    annotation { "Name" : "Biconvex" }
    BICONVEX,
    annotation { "Name" : "NACA 4-digit" }
    NACA_4DIGIT,
    annotation { "Name" : "Clark Y" }
    CLARK_Y,
    annotation { "Name" : "Elliptical" }
    ELLIPTICAL,
    annotation { "Name" : "Eppler-Shen YS-920" }
    YS_920,
    annotation { "Name" : "Eppler E817" }
    E817
}

// Pitch is held at the nominal value from the root out to this station (the
// usual reference radius for quoting pitch), then blended to the tip value.
const PITCH_REF_STATION = 0.7;

// Rounded leading edge: a semi-ellipse replaces the square-cut LE. Its length
// along the chord is NOSE_LENGTH_RATIO * edge thickness (1.0 = 2:1 ellipse,
// 0.5 = semicircle). NOSE_ANGLES places the points round it (0 = the LE itself).
const NOSE_LENGTH_RATIO = 1.0;
const NOSE_ANGLES = [0, 10, 25, 45, 67.5, 90];   // degrees

// Linear interpolation along a table row at any P in [-1, 1]
function rowAt(row, p)
{
    for (var j = 0; j < size(P_STATIONS) - 1; j += 1)
    {
        if (p <= P_STATIONS[j] && p >= P_STATIONS[j + 1])
        {
            const f = (P_STATIONS[j] - p) / (P_STATIONS[j] - P_STATIONS[j + 1]);
            return row[j] + f * (row[j + 1] - row[j]);
        }
    }
    return row[size(row) - 1];
}

function v1Row(stationIndex, p)
{
    if (stationIndex < size(V1_TABLE))
        return rowAt(V1_TABLE[stationIndex], p);
    return 0;      // V1 is zero from r/R = 0.7 outward
}

function v2Row(stationIndex, p)
{
    if (stationIndex < size(V2_TABLE))
        return rowAt(V2_TABLE[stationIndex], p);
    return 1 - p * p;      // from r/R = 0.9 outward the section is an ellipse-like form
}

// Index of the published station at or below x, and the fraction to the next one
function tableBracket(x)
{
    for (var i = size(TABLE_STATIONS) - 2; i >= 0; i -= 1)
    {
        if (x >= TABLE_STATIONS[i])
            return [i, (x - TABLE_STATIONS[i]) / (TABLE_STATIONS[i + 1] - TABLE_STATIONS[i])];
    }
    return [0, 0];
}

// Slope of a table at a published station (Fritsch-Carlson limited).
// A plain central difference overshoots wherever the data flattens out - the
// chord table peaks at 0.6R and is nearly flat to 0.8R, and the overshoot made
// the chord rise again around 0.72R. That bulge is a ripple in the leading edge.
// Limiting the slope to three times the smaller neighbouring secant keeps the
// curve monotonic between stations while staying smooth.
function tableSlope(table, i)
{
    const last = size(table) - 1;
    if (i == 0)
        return (table[1] - table[0]) / (TABLE_STATIONS[1] - TABLE_STATIONS[0]);
    if (i == last)
        return (table[last] - table[last - 1]) / (TABLE_STATIONS[last] - TABLE_STATIONS[last - 1]);

    const d0 = (table[i] - table[i - 1]) / (TABLE_STATIONS[i] - TABLE_STATIONS[i - 1]);
    const d1 = (table[i + 1] - table[i]) / (TABLE_STATIONS[i + 1] - TABLE_STATIONS[i]);
    if (d0 * d1 <= 0)
        return 0;                       // a turning point: flat tangent, no overshoot

    const m = (d0 + d1) / 2;
    const limit = 3 * min(abs(d0), abs(d1));
    return (abs(m) < limit) ? m : (m < 0 ? -limit : limit);
}

// K, a/c or b/c at any radius, cubic Hermite through the published stations.
// Straight-line interpolation leaves a slope break at every station, which shows
// up as a crease running across the blade - most visibly on the leading edge.
function blendTable(table, x)
{
    const br = tableBracket(x);
    const i = br[0];
    const t = br[1];
    const h = TABLE_STATIONS[i + 1] - TABLE_STATIONS[i];
    const m0 = tableSlope(table, i);
    const m1 = tableSlope(table, i + 1);
    const t2 = t * t;
    const t3 = t2 * t;
    return (2 * t3 - 3 * t2 + 1) * table[i]
         + (t3 - 2 * t2 + t) * h * m0
         + (-2 * t3 + 3 * t2) * table[i + 1]
         + (t3 - t2) * h * m1;
}

function v1At(x, p)                // face ordinate scalar at any radius
{
    const br = tableBracket(x);
    const lo = v1Row(br[0], p);
    return lo + br[1] * (v1Row(br[0] + 1, p) - lo);
}

function v2At(x, p)                // thickness ordinate scalar at any radius
{
    const br = tableBracket(x);
    const lo = v2Row(br[0], p);
    return lo + br[1] * (v2Row(br[0] + 1, p) - lo);
}

// ---- Diagnostics: everything goes to the FeatureScript notices panel --------
/** Console line that is also kept in the Part Studio variable propLog, so it can be read through the API. */
/** The finished bodies turned 180 deg about X so the part is shown aft up (tail toward +Z): a rotation, so handedness holds. */
function showAftUp(context is Context, id is Id)
{
    const bodies = qCreatedBy(id, EntityType.BODY);
    if (size(evaluateQuery(context, bodies)) > 0)
        opTransform(context, id + "aftUp", { "bodies" : bodies, "transform" : rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 0, 0)), 180 * degree) });
}

function propPrint(context is Context, key is string, line is string)
{
    println(line);
    var s = "";
    try silent
    {
        const prev = getVariable(context, key);
        if (prev is string)
            s = prev;
    }
    setVariable(context, key, s == "" ? line : s ~ "\n" ~ line);
}

function logLine(context is Context, message)
{
    propPrint(context, "propLog", "[blade] " ~ message);
}

function lastIndexOf(arr)
{
    return size(arr) - 1;
}

function toMm(value)     // length -> plain number of millimetres, 3 decimals
{
    return round(value / millimeter * 1000) / 1000;
}

function toDeg(value)    // angle -> plain number of degrees, 2 decimals
{
    return round(value / degree * 100) / 100;
}

// Where maximum thickness sits, as a fraction of chord, for the non-B-series
// profiles. The B-series uses its own b/c table, which varies with radius.
function profileMaxThicknessPosition(profile is SectionProfile)
{
    if (profile == SectionProfile.NACA_66)
        return 0.45;
    if (profile == SectionProfile.NACA_4DIGIT || profile == SectionProfile.CLARK_Y)
        return 0.30;
    if (profile == SectionProfile.YS_920)
        return 0.343;
    if (profile == SectionProfile.E817)
        return 0.329;
    return 0.50;
}

// A section, described as a centreline height and a half-thickness above the
// square edge floor. Face = centre - half, back = centre + half, where the
// caller adds the edge band. Returns [centre, halfBody].
function sectionOrdinates(profile is SectionProfile, x, p, tMax, tEdge, camberHeight)
{
    if (profile == SectionProfile.B_SERIES)
    {
        const v1 = v1At(x, p);
        const v2 = v2At(x, p);
        const body = v2 * (tMax - tEdge);
        return [v1 * (tMax - tEdge) + body / 2 + tEdge / 2, body / 2];
    }

    var thicknessShape = ARC_SHAPE;
    var camberShape = ARC_SHAPE;
    if (profile == SectionProfile.NACA_16)
    {
        thicknessShape = NACA16_THICKNESS;
        camberShape = NACA16_CAMBER;
    }
    else if (profile == SectionProfile.NACA_66)
    {
        thicknessShape = NACA66_THICKNESS;
        camberShape = NACA66_CAMBER;
    }
    else if (profile == SectionProfile.NACA_4DIGIT)
    {
        thicknessShape = NACA4_THICKNESS;
        camberShape = NACA16_CAMBER;         // a = 0.8 mean line
    }
    else if (profile == SectionProfile.CLARK_Y)
    {
        thicknessShape = CLARKY_THICKNESS;
        camberShape = CLARKY_CAMBER;
    }
    else if (profile == SectionProfile.ELLIPTICAL)
    {
        thicknessShape = ELLIPSE_THICKNESS;
        camberShape = NACA16_CAMBER;         // a = 0.8 mean line
    }
    else if (profile == SectionProfile.YS_920)
    {
        thicknessShape = YS920_THICKNESS;
        camberShape = YS920_CAMBER;          // aft-loaded, peak near 0.66c
    }
    else if (profile == SectionProfile.E817)
    {
        thicknessShape = E817_THICKNESS;
        camberShape = E817_CAMBER;           // aft-loaded, peak near 0.69c
    }

    const halfBody = rowAt(thicknessShape, p) * (tMax - tEdge) / 2;

    // Ogival: the face is flat, so the centreline follows the half-thickness
    if (profile == SectionProfile.OGIVAL)
        return [halfBody + tEdge / 2, halfBody];

    return [camberHeight * rowAt(camberShape, p) + tMax / 2, halfBody];
}

// Map a section point (s along the pitch line, +s toward the leading edge;
// yOrd normal to it, + toward the suction side) onto the cylinder of radius r.
function sectionPoint(r, theta0, z0, phi, s, yOrd, hand)
{
    const u = s * cos(phi) - yOrd * sin(phi);   // tangential arc length
    const z = s * sin(phi) + yOrd * cos(phi);   // axial
    const theta = theta0 + (u / r) * radian;
    return vector(-hand * r * sin(theta), r * cos(theta), z0 + z);
}

// ---- Operating check: Wageningen B-series open-water polynomials -----------
// Oosterveld & van Oossanen (1975). KT and KQ as sums of C * J^s * (P/D)^t *
// EAR^u * Z^v. Open-water, full scale, no hull, no free surface: for a small
// efoil prop close to the surface, behind a mast and pod, treat the output as
// a comparison between two of your own designs, not an absolute prediction.
// Each row is [C, s, t, u, v].
const KT_TERMS = [
    [0.00880496, 0, 0, 0, 0], [-0.204554, 1, 0, 0, 0], [0.166351, 0, 1, 0, 0], [0.158114, 0, 2, 0, 0],
    [-0.147581, 2, 0, 1, 0], [-0.481497, 1, 1, 1, 0], [0.415437, 0, 2, 1, 0], [0.0144043, 0, 0, 0, 1],
    [-0.0530054, 2, 0, 0, 1], [0.0143481, 0, 1, 0, 1], [0.0606826, 1, 1, 0, 1], [-0.0125894, 0, 0, 1, 1],
    [0.0109689, 1, 0, 1, 1], [-0.133698, 0, 3, 0, 0], [0.00638407, 0, 6, 0, 0], [-0.00132718, 2, 6, 0, 0],
    [0.168496, 3, 0, 1, 0], [-0.0507214, 0, 0, 2, 0], [0.0854559, 2, 0, 2, 0], [-0.0504475, 3, 0, 2, 0],
    [0.010465, 1, 6, 2, 0], [-0.00648272, 2, 6, 2, 0], [-0.00841728, 0, 3, 0, 1], [0.0168424, 1, 3, 0, 1],
    [-0.00102296, 3, 3, 0, 1], [-0.0317791, 0, 3, 1, 1], [0.018604, 1, 0, 2, 1], [-0.00410798, 0, 2, 2, 1],
    [-0.000606848, 0, 0, 0, 2], [-0.0049819, 1, 0, 0, 2], [0.0025983, 2, 0, 0, 2], [-0.000560528, 3, 0, 0, 2],
    [-0.00163652, 1, 2, 0, 2], [-0.000328787, 1, 6, 0, 2], [0.000116502, 2, 6, 0, 2], [0.000690904, 0, 0, 1, 2],
    [0.00421749, 0, 3, 1, 2], [0.0000565229, 3, 6, 1, 2], [-0.00146564, 0, 3, 2, 2]
];

const KQ_TERMS = [
    [0.00379368, 0, 0, 0, 0], [0.00886523, 2, 0, 0, 0], [-0.032241, 1, 1, 0, 0], [0.00344778, 0, 2, 0, 0],
    [-0.0408811, 0, 1, 1, 0], [-0.108009, 1, 1, 1, 0], [-0.0885381, 2, 1, 1, 0], [0.188561, 0, 2, 1, 0],
    [-0.00370871, 1, 0, 0, 1], [0.00513696, 0, 1, 0, 1], [0.0209449, 1, 1, 0, 1], [0.00474319, 2, 1, 0, 1],
    [-0.00723408, 2, 0, 1, 1], [0.00438388, 1, 1, 1, 1], [-0.0269403, 0, 2, 1, 1], [0.0558082, 3, 0, 1, 0],
    [0.0161886, 0, 3, 1, 0], [0.00318086, 1, 3, 1, 0], [0.015896, 0, 0, 2, 0], [0.0471729, 1, 0, 2, 0],
    [0.0196283, 3, 0, 2, 0], [-0.0502782, 0, 1, 2, 0], [-0.030055, 3, 1, 2, 0], [0.0417122, 2, 2, 2, 0],
    [-0.0397722, 0, 3, 2, 0], [-0.00350024, 0, 6, 2, 0], [-0.0106854, 3, 0, 0, 1], [0.00110903, 3, 3, 0, 1],
    [-0.000313912, 0, 6, 0, 1], [0.0035985, 3, 0, 1, 1], [-0.00142121, 0, 6, 1, 1], [-0.00383637, 1, 0, 2, 1],
    [0.0126803, 0, 2, 2, 1], [-0.00318278, 2, 3, 2, 1], [0.00334268, 0, 6, 2, 1], [-0.00183491, 1, 1, 0, 2],
    [0.000112451, 3, 2, 0, 2], [-0.0000297228, 3, 6, 0, 2], [0.000269551, 1, 0, 1, 2], [0.00083265, 2, 0, 1, 2],
    [0.00155334, 0, 2, 1, 2], [0.000302683, 0, 6, 1, 2], [-0.0001843, 0, 0, 2, 2], [-0.000425399, 0, 3, 2, 2],
    [0.0000869243, 3, 3, 2, 2], [-0.0004659, 0, 6, 2, 2], [0.0000554194, 1, 6, 2, 2]
];

const SEAWATER_DENSITY = 1025;      // kg/m^3
const GRAVITY = 9.80665;            // m/s^2, for quoting thrust in kgf as well as N

// Thrust as "NNN N (NN.N kgf)"
function thrustText(newtons)
{
    return round(newtons) ~ " N (" ~ (round(newtons / GRAVITY * 10) / 10) ~ " kgf)";
}

function openWaterCoefficient(terms, J, pd, ear, z)
{
    var total = 0;
    for (var t in terms)
        total = total + t[0] * J ^ t[1] * pd ^ t[2] * ear ^ t[3] * z ^ t[4];
    return total;
}

// Cap one end of the lofted skin. A fill surface is the clean answer, but it
// gives up on a loop that is strongly wrapped, sharply cornered at the leading
// edge and very thin at the trailing edge - which is exactly the root section of
// a wide blade. The fallback lofts the section down to a point just outside it,
// giving a shallow cone: harmless at the root (buried in the hub) and a slightly
// pointed end at the tip. Returns the method that worked, or "" if neither did.
function capEnd(context is Context, capId is Id, loop is Query, centre is Vector, apexOffset is Vector) returns string
{
    try silent
    {
        opFillSurface(context, capId, { "edgesG0" : loop });
        return "fill";
    }

    try silent
    {
        opPoint(context, capId + "apex", { "point" : centre + apexOffset });
        opLoft(context, capId, {
                    "profileSubqueries" : [loop, qCreatedBy(capId + "apex", EntityType.VERTEX)],
                    "bodyType" : ToolBodyType.SURFACE
                });
        return "cone";
    }

    return "";
}

/**
 * Builds one blade from the definition (the blade feature's own fields) and returns a map:
 *   blade (query for the solid), D, R, xHub (hub surface as r/R), rootCentre, tipCentre, and
 *   sectionsOnly = true when the definition asked for the curves alone (no solid; the caller returns).
 * Shared by the blade feature and the propeller feature, which calls it once and patterns the result.
 */
export function buildBlade(context is Context, id is Id, definition is map) returns map
{
        // ---- 1. Driving maths ------------------------------------------------
        const D = definition.diameter;
        const R = D / 2;
        const xHub = definition.hubDiameter / D;    // r/R at the hub surface
        const pitch = definition.pdRatio * D;       // nominal pitch (root to 0.7R)
        const Z = definition.bladeCount;
        const tRoot = definition.rootThickness;
        const tTip = definition.tipThickness;
        const tEdge = definition.edgeThickness;
        const noseLength = NOSE_LENGTH_RATIO * tEdge;
        const hand = (definition.rotation == Rotation.CW) ? 1 : -1;

        const K = (Z <= 3) ? K_Z3 : K_Z4;
        const AC = (Z <= 3) ? AC_Z3 : AC_Z4;
        const BC = (Z <= 3) ? BC_Z3 : BC_Z4;

        // Sample stations from 0.2 to the end of the plain blade. Half even, half
        // cosine-clustered: pure clustering drives the spacing to zero at the tip,
        // and sections a fraction of a millimetre apart make the loft wrinkle.
        const xEnd = definition.roundTip ? 1 - TIP_ROUND_SPAN : 1;
        var stations = [];
        var tipScales = [];
        for (var i = 0; i < definition.sectionCount; i += 1)
        {
            const u = i / (definition.sectionCount - 1);
            stations = append(stations, 0.2 + (xEnd - 0.2) * (0.5 * u + 0.5 * sin(u * 90 * degree)));
            tipScales = append(tipScales, 1);
        }

        const baseGap = (stations[lastIndexOf(stations)] - stations[lastIndexOf(stations) - 1]) * R;
        if (baseGap < 0.4 * millimeter)
            throw regenError("Sections are crowding at the tip (" ~ toMm(baseGap) ~ " mm apart): lower the section count", ["sectionCount"]);

        // Extra sections across the dome, scaled on a quarter ellipse
        if (definition.roundTip)
        {
            for (var k = 1; k <= TIP_ROUND_SECTIONS; k += 1)
            {
                const u = k / TIP_ROUND_SECTIONS;
                stations = append(stations, xEnd + TIP_ROUND_SPAN * u);
                tipScales = append(tipScales, max(TIP_ROUND_FLOOR, sqrt(max(0, 1 - u * u))));
            }
        }
        const lastStation = size(stations) - 1;
        const lastP = size(P_STATIONS) - 1;

        const verbose = definition.debugPrint;
        if (verbose)
        {
            logLine(context, "---- inputs ----");
            logLine(context, "D " ~ toMm(D) ~ " mm, hub " ~ toMm(definition.hubDiameter) ~ " mm (r/R " ~ (round(xHub * 1000) / 1000) ~ "), P/D " ~ definition.pdRatio ~ " -> pitch " ~ toMm(pitch) ~ " mm, tip pitch change " ~ definition.tipPitchChange ~ " %");
            logLine(context, "EAR " ~ definition.ear ~ ", Z " ~ Z ~ ", " ~ ((hand == 1) ? "CW from astern (right-hand)" : "CCW from astern (left-hand)") ~ ", rake " ~ toDeg(definition.rake) ~ " deg, skew " ~ toDeg(definition.skewAngle) ~ " deg");
            logLine(context, "t root " ~ toMm(tRoot) ~ ", t tip " ~ toMm(tTip) ~ ", edge " ~ toMm(tEdge) ~ ", nose length " ~ toMm(noseLength) ~ ", tip chord " ~ toMm(definition.tipChord) ~ " mm, taper exponent " ~ definition.taperExponent);
            logLine(context, "---- stations: index | r/R | r | chord | t max | pitch | pitch angle | wrap | LE-to-generator | points per curve ----");
        }

        if (xHub < 0.22)
            throw regenError("Hub diameter is under 0.22 x D: the blade starts at r/R = 0.2 and would not bury into the hub", ["hubDiameter", "diameter"]);
        if (xHub > 0.6)
            throw regenError("Hub diameter is over 0.6 x D", ["hubDiameter", "diameter"]);
        if (tTip < tEdge)
            throw regenError("Tip thickness must be at least the edge thickness", ["tipThickness", "edgeThickness"]);
        if (tRoot <= tTip)
            throw regenError("Root thickness must exceed tip thickness", ["rootThickness", "tipThickness"]);
        if (definition.tipChord >= 0.5 * blendTable(K, TIP_BLEND_START) * D * definition.ear / Z)
            throw regenError("Tip chord must be under half the chord where the tip blend starts (r/R = " ~ TIP_BLEND_START ~ ")", ["tipChord"]);
        // Blades must fit round the hub: Z chords, each projected onto the hub
        // circumference, cannot exceed it. This is the practical EAR ceiling and
        // it depends on hub diameter and blade count as much as on EAR.
        const phiHub = atan(pitch / (2 * PI * definition.hubDiameter / 2));
        const chordAtHub = blendTable(K, xHub) * D * definition.ear / Z;
        const hubCircumference = PI * definition.hubDiameter;
        const usedAtHub = Z * chordAtHub * cos(phiHub);
        // why EAR is the cause when a root fails: the expanded area is shared over the blades, so few blades or a small
        // hub give each blade a chord that is a large part of the hub's circumference
        const earNote = " [EAR " ~ definition.ear ~ " over " ~ Z ~ " blades puts " ~ round(100 * definition.ear / Z) ~ " % of the disc area on each blade: a " ~ toMm(chordAtHub) ~ " mm chord at the hub, " ~ round(100 * chordAtHub * cos(phiHub) / hubCircumference) ~ " % of the " ~ toMm(hubCircumference) ~ " mm hub circumference at " ~ toDeg(phiHub) ~ " deg pitch angle]";
        // Overlap only bites when the blade is patterned, and one blade on its own
        // still builds, so this is a warning up to a point and an error past it.
        if (usedAtHub > 1.3 * hubCircumference)
            throw regenError("At EAR " ~ definition.ear ~ " with " ~ Z ~ " blades the blades overlap badly where they leave the hub ("
                ~ toMm(usedAtHub) ~ " mm of chord on a " ~ toMm(hubCircumference) ~ " mm circumference). Lower EAR, raise the hub diameter, or use fewer blades.",
                ["ear", "bladeCount", "hubDiameter"]);
        if (usedAtHub > 0.95 * hubCircumference)
            reportFeatureWarning(context, id, "Blades will touch or overlap at the hub when patterned: "
                ~ toMm(usedAtHub) ~ " mm of chord on a " ~ toMm(hubCircumference) ~ " mm circumference. The single blade still builds.");

        if (noseLength > 0.25 * definition.tipChord)
            throw regenError("Edge thickness is too large for the tip chord: the rounded nose would swallow the tip section", ["edgeThickness", "tipChord"]);

        // ---- 2. Build the wrapped sections -----------------------------------
        const sectionsId = id + "sections";
        var profiles = [];
        var rootCentre = vector(0, 0, 0) * millimeter;
        var tipCentre = vector(0, 0, 0) * millimeter;

        var rootSectionNote = "";
        var meanLines = [];           // per station: the section's mean line, 3D points LE -> TE (build 72, for the mold)
        var stationR = [];
        var stationChord = [];
        var stationPhi = [];
        var stationThick = [];
        var stationCrest = [];        // b / c: the thickest point from the leading edge (build 103, for the cut templates)
        for (var i = 0; i <= lastStation; i += 1)
        {
            const x = stations[i];
            const r = R * x;
            const tipScale = tipScales[i];      // 1 everywhere except across the tip dome

            // n = 0 at the hub surface, 1 at the tip. Stations inside the hub have
            // n < 0, so the same curve simply carries on (thicker) inboard and
            // the loft stays smooth where it breaks out of the hub.
            const n = (x - xHub) / (1 - xHub);
            // exponent 1 = linear (B-series style, stiffer mid-span); 2 = parabolic
            // (thins quickly, lower drag); anything in between blends the two.
            const tMax = (tTip + (tRoot - tTip) * (1 - n) ^ definition.taperExponent) * tipScale;
            const tEdgeLocal = tEdge * tipScale;
            const noseLengthLocal = NOSE_LENGTH_RATIO * tEdgeLocal;

            // B-series chord, generator-line offset and max-thickness position.
            // Inboard of TIP_BLEND_START the published chord applies; outboard it
            // is drawn down to the tip chord on a curve that leaves the published
            // outline tangentially and turns over at the tip, so there is no
            // corner where the blend starts and no point at the tip.
            var c = blendTable(K, x) * D * definition.ear / Z;
            if (x > TIP_BLEND_START)
            {
                // The published chord collapses to zero at the tip, so blending
                // against it makes the outline dip below the tip chord and swell
                // back out - that is the fin. Blend against the chord the outline
                // was heading for instead: a straight continuation of the slope at
                // the blend start. Both terms fall monotonically, so the outline
                // leaves 0.9R tangentially and closes on the tip chord.
                // The trend line must leave 0.9R on the SAME slope the Hermite
                // curve arrives with, or the blend start becomes another crease.
                const chordScale = D * definition.ear / Z;
                const cStart = blendTable(K, TIP_BLEND_START) * chordScale;
                const cTrend = cStart + (x - TIP_BLEND_START) * tableSlope(K, 7) * chordScale;
                const m = (x - TIP_BLEND_START) / (1 - TIP_BLEND_START);
                c = definition.tipChord + (cTrend - definition.tipChord) * (1 - m * m) ^ TIP_BLEND_EXPONENT;
            }
            c = c * tipScale;
            const a = blendTable(AC, x) * c;
            var b = blendTable(BC, x) * c;
            if (definition.sectionProfile != SectionProfile.B_SERIES)
                b = profileMaxThicknessPosition(definition.sectionProfile) * c;
            const camberHeight = definition.camberPercent / 100 * c;

            // The rounded nose must fit inside the forward part of the section
            if (noseLengthLocal > 0.4 * b)
                throw regenError("Edge thickness is too large for the chord at r/R = " ~ (round(x * 100) / 100) ~ ": the nose would swallow the section. Raise tip chord or lower edge thickness.", ["edgeThickness", "tipChord"]);
            const sMax = a - b;                         // max-thickness point, measured from generator line

            // Local pitch: nominal to 0.7R, then a parabolic blend to the tip value
            var pitchBlend = 0;
            if (x > PITCH_REF_STATION)
            {
                const m = (x - PITCH_REF_STATION) / (1 - PITCH_REF_STATION);
                pitchBlend = m * m;
            }
            const localPitch = pitch * (1 + definition.tipPitchChange / 100 * pitchBlend);
            const phi = atan(localPitch / (2 * PI * r));    // geometric pitch angle

            if (c * cos(phi) / r > 4.5)
                throw regenError("Section at r/R = " ~ x ~ " wraps more than ~260 deg round the shaft: reduce EAR or raise the blade count." ~ earNote, ["ear", "bladeCount"]);

            const theta0 = -definition.skewAngle * n;   // progressive skew, zero at the hub surface
            const z0 = -r * tan(definition.rake);       // aft rake

            // Both splines run leading edge -> trailing edge and share the LE point.
            var backPoints = [];
            var facePoints = [];

            // 2a. Rounded nose. The square edge of height tEdge is closed off by a
            //     semi-ellipse; the V1/V2 terms underneath it are left untouched.
            for (var k = 0; k < size(NOSE_ANGLES); k += 1)
            {
                const psi = NOSE_ANGLES[k] * degree;
                const setback = noseLengthLocal * (1 - cos(psi));    // distance aft of the LE
                const halfEdge = (tEdgeLocal / 2) * sin(psi);        // half-height of the edge band here
                const p = 1 - setback / b;
                const s = sMax + p * b;
                const ord = sectionOrdinates(definition.sectionProfile, x, p, tMax, tEdgeLocal, camberHeight);
                const yFace = ord[0] - ord[1] - halfEdge;
                const yBack = ord[0] + ord[1] + halfEdge;
                backPoints = append(backPoints, sectionPoint(r, theta0, z0, phi, s, yBack, hand));
                if (k == 0)
                    facePoints = append(facePoints, backPoints[0]);     // exact shared LE vertex
                else
                    facePoints = append(facePoints, sectionPoint(r, theta0, z0, phi, s, yFace, hand));
            }

            // 2b. Standard B-series ordinates aft of the nose, square-cut trailing edge
            for (var j = 0; j <= lastP; j += 1)
            {
                const p = P_STATIONS[j];
                if (p > 0 && p * b > b - 1.4 * noseLengthLocal)
                    continue;                                   // inside (or crowding) the nose
                const s = sMax + ((p >= 0) ? p * b : p * (c - b));
                const ord = sectionOrdinates(definition.sectionProfile, x, p, tMax, tEdgeLocal, camberHeight);
                const yFace = ord[0] - ord[1] - tEdgeLocal / 2;
                const yBack = ord[0] + ord[1] + tEdgeLocal / 2;
                facePoints = append(facePoints, sectionPoint(r, theta0, z0, phi, s, yFace, hand));
                backPoints = append(backPoints, sectionPoint(r, theta0, z0, phi, s, yBack, hand));
            }

            // Closed profile = back spline + face spline (meeting at the LE vertex)
            // + square-cut TE. The LE vertex keeps the loft aligned along the nose.
            var meanPts = [];
            for (var q = 0; q < size(backPoints); q += 1)
                meanPts = append(meanPts, (backPoints[q] + facePoints[q]) / 2);
            meanLines = append(meanLines, meanPts);
            stationR = append(stationR, r);
            stationChord = append(stationChord, c);
            stationPhi = append(stationPhi, phi);
            stationThick = append(stationThick, tMax);
            stationCrest = append(stationCrest, b / c);
            const secId = sectionsId + ("s" ~ i);
            const lastPoint = size(backPoints) - 1;
            if (verbose)
                logLine(context, "" ~ i ~ " | " ~ x ~ " | " ~ toMm(r) ~ " | " ~ toMm(c) ~ " | " ~ toMm(tMax) ~ " | " ~ toMm(localPitch) ~ " | " ~ toDeg(phi) ~ " deg | " ~ toDeg((c * cos(phi) / r) * radian) ~ " deg | " ~ toMm(a) ~ " | " ~ (lastPoint + 1));
            // a section that cannot close is named here, not left to the loft or the cap to fail on
            const wrapHere = c * cos(phi) / r;
            if (i == 0)
                rootSectionNote = "root section at r/R " ~ (round(x * 100) / 100) ~ ": chord " ~ toMm(c) ~ " mm, thickness " ~ toMm(tMax) ~ " mm, wrap " ~ (round(wrapHere * 180 / PI)) ~ " deg round a " ~ toMm(r) ~ " mm radius";
            if (tMax > 0.85 * c)
                throw regenError("Section " ~ i ~ " at r/R " ~ (round(x * 100) / 100) ~ " is " ~ toMm(tMax) ~ " mm thick on a " ~ toMm(c) ~ " mm chord and cannot close. Inside the hub the taper carries on past Root thick. (thicker still at 0.2R), so a big hub with a thick root or a high Taper exponent runs away: lower Root thick. or Taper exponent, or raise EAR or Diameter." ~ earNote, ["rootThickness", "taperExponent", "hubDiameter", "ear"]);
            if (wrapHere > 160 * PI / 180)
                throw regenError("Section " ~ i ~ " at r/R " ~ (round(x * 100) / 100) ~ " wraps " ~ round(wrapHere * 180 / PI) ~ " deg round its " ~ toMm(r) ~ " mm radius (chord " ~ toMm(c) ~ " mm at " ~ toDeg(phi) ~ " deg) and folds onto itself: lower EAR, raise Diameter or the hub, or use more blades." ~ earNote, ["ear", "diameter", "hubDiameter", "bladeCount"]);

            try
            {
                opFitSpline(context, secId + "back", { "points" : backPoints });
                opFitSpline(context, secId + "face", { "points" : facePoints });
                opFitSpline(context, secId + "te", { "points" : [facePoints[lastPoint], backPoints[lastPoint]] });
            }
            catch (error)
            {
                logLine(context, "FAILED building the section curves at station " ~ i ~ " (r/R " ~ x ~ "). Kernel said:");
                println(error);
                logLine(context, "first back point / last back point:");
                println(backPoints[0]);
                println(backPoints[lastPoint]);
                throw regenError("Section curves failed at r/R = " ~ x ~ ". See FeatureScript notices.");
            }

            profiles = append(profiles, qCreatedBy(secId, EntityType.EDGE));

            // Centre of the section, used if an end has to be capped with a cone
            if (i == 0 || i == lastStation)
            {
                var sum = vector(0, 0, 0) * millimeter;
                for (var q = 0; q < size(backPoints); q += 1)
                    sum = sum + backPoints[q] + facePoints[q];
                const centre = sum / (2 * size(backPoints));
                if (i == 0)
                    rootCentre = centre;
                else
                    tipCentre = centre;
            }
        }

        if (verbose)
            logLine(context, "all " ~ (lastStation + 1) ~ " sections built");

        if (definition.sectionsOnly)
        {
            // Red highlight shows only while this feature's dialog is open.
            debug(context, qCreatedBy(sectionsId, EntityType.EDGE));
            logLine(context, "sections only: loft skipped, curves left in the Part Studio");
            return { "sectionsOnly" : true, "blade" : qNothing(), "D" : D, "R" : R, "xHub" : xHub, "rootCentre" : rootCentre, "tipCentre" : tipCentre, "meanLines" : meanLines, "stationR" : stationR, "stationChord" : stationChord, "stationPhi" : stationPhi, "stationThick" : stationThick, "stationCrest" : stationCrest };
        }

        // ---- 3. Loft the blade solid -----------------------------------------
        // First choice: direct solid loft. If the kernel refuses to cap the
        // (slightly non-planar) wrapped end sections, fall back to a surface
        // loft + two fill surfaces + enclose. Every stage reports to the notices
        // panel when it fails, so the message names the step that broke.
        const loftId = id + "pureBladeLoft";
        var haveSolid = false;
        try
        {
            opLoft(context, loftId, { "profileSubqueries" : profiles });
            haveSolid = size(evaluateQuery(context, qBodyType(qCreatedBy(loftId, EntityType.BODY), BodyType.SOLID))) > 0;
            if (verbose)
                logLine(context, "stage A (direct solid loft): ran, solid produced = " ~ haveSolid);
        }
        catch (error)
        {
            // Expected: each section is three separate wire bodies (back, face and
            // trailing edge), and a solid loft wants one closed profile per
            // station, so the kernel returns LOFT_PROFILE_SOLID. The surface loft
            // below has no such requirement. Only worth reporting when debugging.
            if (verbose)
            {
                logLine(context, "stage A (direct solid loft) declined - using surface loft + caps. Kernel said:");
                println(error);
            }
        }

        var bladeQuery = qBodyType(qCreatedBy(loftId, EntityType.BODY), BodyType.SOLID);

        if (!haveSolid)
        {
            try silent
            {
                opDeleteBodies(context, id + "dropFailedLoft", { "entities" : qCreatedBy(loftId, EntityType.BODY) });
            }

            var stage = "B1 (surface loft through the sections)";
            try
            {
                opLoft(context, id + "skinLoft", {
                            "profileSubqueries" : profiles,
                            "bodyType" : ToolBodyType.SURFACE
                        });
                if (verbose)
                    logLine(context, "stage " ~ stage ~ ": ok");

                stage = "B2 (cap the root section)";
                const rootMethod = capEnd(context, id + "rootCap", profiles[0], rootCentre,
                        vector(0, -1, 0) * (0.02 * R));
                if (rootMethod == "")
                    throw regenError("could not cap the root section (" ~ rootSectionNote ~ "). The curve is closed but the kernel will not fill it: usually a section too thick for its chord or wrapped too far round the hub. Turn on Print to notices and 'Sections only' to see it; lower Root thick. or Taper exponent, or raise EAR, Diameter or the hub." ~ earNote, ["rootThickness", "taperExponent", "ear", "hubDiameter"]);
                if (verbose)
                    logLine(context, "stage " ~ stage ~ ": ok by " ~ rootMethod);

                stage = "B3 (cap the tip section)";
                const tipMethod = capEnd(context, id + "tipCap", profiles[lastStation], tipCentre,
                        vector(0, 1, 0) * (0.01 * R));
                if (tipMethod == "")
                    throw regenError("could not cap the tip section");
                if (verbose)
                    logLine(context, "stage " ~ stage ~ ": ok by " ~ tipMethod);

                stage = "B4 (enclose skin + caps into a solid)";
                opEnclose(context, id + "enclose", { "entities" : qUnion([
                                    qCreatedBy(id + "skinLoft", EntityType.BODY),
                                    qCreatedBy(id + "rootCap", EntityType.BODY),
                                    qCreatedBy(id + "tipCap", EntityType.BODY)
                                ]) });
                if (verbose)
                    logLine(context, "stage " ~ stage ~ ": ok");
            }
            catch (error)
            {
                logLine(context, "FAILED at stage " ~ stage ~ ". Kernel said:");
                println(error);
                throw regenError("Blade failed at stage " ~ stage ~ ". See FeatureScript notices; 'Debug: sections only' shows the curves.");
            }

            try silent
            {
                opDeleteBodies(context, id + "dropSheets", { "entities" : qBodyType(qUnion([
                                    qCreatedBy(id + "skinLoft", EntityType.BODY),
                                    qCreatedBy(id + "rootCap", EntityType.BODY),
                                    qCreatedBy(id + "tipCap", EntityType.BODY)
                                ]), BodyType.SHEET) });
            }
            bladeQuery = qBodyType(qCreatedBy(id + "enclose", EntityType.BODY), BodyType.SOLID);
        }

        if (size(evaluateQuery(context, bladeQuery)) == 0)
        {
            logLine(context, "FAILED: every stage ran but no solid body came out");
            throw regenError("Blade loft did not produce a solid. Tick 'Debug: sections only' and inspect the sections.");
        }

        if (verbose)
        {
            try silent
            {
                logLine(context, "solid ok, volume " ~ (round(evVolume(context, { "entities" : bladeQuery }) / (millimeter ^ 3))) ~ " mm^3");
            }
        }

        if (!definition.keepSections)
            opDeleteBodies(context, id + "dropSections", { "entities" : qCreatedBy(sectionsId, EntityType.BODY) });


        return { "sectionsOnly" : false, "blade" : bladeQuery, "D" : D, "R" : R, "xHub" : xHub, "rootCentre" : rootCentre, "tipCentre" : tipCentre, "meanLines" : meanLines, "stationR" : stationR, "stationChord" : stationChord, "stationPhi" : stationPhi, "stationThick" : stationThick, "stationCrest" : stationCrest };
}

/**
 * The open-water operating check, printed to the notices. Pure logging: reads only the definition.
 * Shared by the blade feature and the propeller feature.
 */
/**
 * Share of the expanded blade area that lies outside the hub, from the B-series chord
 * table: the K distribution integrated from xHub to the tip over its integral from 0.2
 * (where the table starts) to the tip. Used to turn thrust into a blade loading.
 */
function expandedAreaFraction(xHub is number, Z is number) returns number
{
    const K = (Z <= 3) ? K_Z3 : K_Z4;
    var total = 0;
    var outside = 0;
    for (var i = 0; i < size(TABLE_STATIONS) - 1; i += 1)
    {
        const x0 = TABLE_STATIONS[i];
        const x1 = TABLE_STATIONS[i + 1];
        const seg = (K[i] + K[i + 1]) / 2 * (x1 - x0);
        total += seg;
        if (xHub <= x0)
            outside += seg;
        else if (xHub < x1)
        {
            // the part of this segment beyond the hub, K linear across it
            const kHub = K[i] + (K[i + 1] - K[i]) * (xHub - x0) / (x1 - x0);
            outside += (kHub + K[i + 1]) / 2 * (x1 - xHub);
        }
    }
    return (total > 0) ? outside / total : 1;
}

/**
 * The folder's tail relief as a revolved cut about the shaft: in the meridional plane (sketch x = radius, y = z) the root
 * envelope swept about the pin from the deployed ray (+r) to the folded ray is bounded inboard by the arc of radius rho0
 * about the pin circle, then by the folded envelope's aft strip (hAft inboard of the folded ray) and the two end strips.
 * Makes the body under reliefId (sketch, revolve, sketch dropped) and returns its query.
 */
function tailReliefBody(context is Context, reliefId is Id, rPin is ValueWithUnits, zPin is ValueWithUnits, rho0 is ValueWithUnits, hAft is ValueWithUnits, hFwd is ValueWithUnits, reach is ValueWithUnits, foldAngle is ValueWithUnits, zAxis is Line) returns Query
{
    const relSk = newSketchOnPlane(context, reliefId + "sketch", { "sketchPlane" : plane(vector(0, 0, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0)) });
    const P = vector(rPin, zPin);
    const u0 = vector(1, 0);
    const v0 = vector(0, 1);
    const uA = vector(cos(foldAngle), -sin(foldAngle));
    const vA = vector(sin(foldAngle), cos(foldAngle));
    const uM = vector(cos(foldAngle / 2), -sin(foldAngle / 2));
    var pts = [P + rho0 * u0 + hFwd * v0, P + rho0 * u0, P + rho0 * uA, P + rho0 * uA - hAft * vA, P + reach * uA - hAft * vA, P + reach * uA + hFwd * vA, P + reach * u0 + hFwd * v0];
    for (var j = 0; j < size(pts); j += 1)
    {
        if (pts[j][0] < 0.2 * millimeter)
            pts[j] = vector(0.2 * millimeter, pts[j][1]);
    }
    skLineSegment(relSk, "s0", { "start" : pts[0], "end" : pts[1] });
    skArc(relSk, "arc", { "start" : pts[1], "mid" : P + rho0 * uM, "end" : pts[2] });
    skLineSegment(relSk, "s2", { "start" : pts[2], "end" : pts[3] });
    skLineSegment(relSk, "s3", { "start" : pts[3], "end" : pts[4] });
    skLineSegment(relSk, "s4", { "start" : pts[4], "end" : pts[5] });
    skLineSegment(relSk, "s5", { "start" : pts[5], "end" : pts[6] });
    skLineSegment(relSk, "s6", { "start" : pts[6], "end" : pts[0] });
    skSolve(relSk);
    opRevolve(context, reliefId + "revolve", { "entities" : qSketchRegion(reliefId + "sketch"), "axis" : zAxis, "angleForward" : 360 * degree });
    opDeleteBodies(context, reliefId + "dropSketch", { "entities" : qCreatedBy(reliefId + "sketch", EntityType.BODY) });
    return qCreatedBy(reliefId + "revolve", EntityType.BODY);
}

/** B-series chord factor K = c Z / (D EAR) at r/R = x, linear between the table stations. */
function chordFactorAt(x is number, Z is number) returns number
{
    const K = (Z <= 3) ? K_Z3 : K_Z4;
    if (x <= TABLE_STATIONS[0])
        return K[0];
    for (var i = 0; i < size(TABLE_STATIONS) - 1; i += 1)
    {
        if (x <= TABLE_STATIONS[i + 1])
            return K[i] + (K[i + 1] - K[i]) * (x - TABLE_STATIONS[i]) / (TABLE_STATIONS[i + 1] - TABLE_STATIONS[i]);
    }
    return 0;
}

/**
 * Speed, dynamic pressure, cavitation number, blade loading and mean lift coefficient
 * at 0.7R for one operating point (advance speed v m/s, prop speed n rev/s, thrust N),
 * with a verdict. Vapour pressure 2.3 kPa (20 C); cavitation starts where the
 * section's suction peak, -Cp, exceeds the cavitation number.
 */
function bladeLoadingLines(context is Context, v is number, n is number, thrust is number, dMetres is number, bladeArea is number, depth is ValueWithUnits, isStatic is boolean)
{
    const v07 = sqrt(v ^ 2 + (0.7 * PI * dMetres * n) ^ 2);
    const q = 0.5 * SEAWATER_DENSITY * v07 ^ 2;
    const sigma = (101325 + SEAWATER_DENSITY * 9.80665 * (depth / meter) - 2340) / q;
    const loading = (bladeArea > 0) ? thrust / bladeArea : 0;
    const cl = loading / q;
    logLine(context, "  at 0.7R: " ~ (round(v07 * 10) / 10) ~ " m/s, q " ~ round(q / 1000) ~ " kPa, cavitation number "
        ~ (round(sigma * 100) / 100) ~ "; blade loading " ~ round(loading / 1000) ~ " kPa, mean CL " ~ (round(cl * 100) / 100));
    if (isStatic)
    {
        if (sigma < 0.5 || cl > 0.5)
            logLine(context, "  at full throttle the blade cavitates and ventilates at this loading (every efoil prop of this size does through the hump); it lasts the seconds of a burst, not a cruise");
    }
    else
    {
        if (sigma < 0.5)
            logLine(context, "  WARNING: cavitation likely at cruise (number under 0.5): a flatter section (NACA 66-style, YS-920), more EAR or a larger diameter at lower rpm");
        else if (sigma < 0.8)
            logLine(context, "  cruise: marginal for an ogival B-series section (-Cp peaks near this), clear for a NACA 66-style or YS-920 section at a CL under 0.3");
        else
            logLine(context, "  cruise: clear of cavitation");
        if (cl > 0.5)
            logLine(context, "  WARNING: mean CL over 0.5 at cruise - the blade is working hard; more EAR or diameter");
    }
}

export function operatingCheck(context is Context, definition is map)
{
        const D = definition.diameter;
        const Z = definition.bladeCount;
        const tRoot = definition.rootThickness;
        const tTip = definition.tipThickness;
        const tEdge = definition.edgeThickness;
        if (definition.motorKv > 0)
        {
            const pd = definition.pdRatio;
            const ear = definition.ear;
            const dMetres = D / meter;
            const torquePerAmp = definition.torqueConst / definition.motorKv;   // Nm per motor amp
            const shaftTorqueLimit = torquePerAmp * definition.currentLimit * definition.gearRatio;
            const packVolts = definition.cellCount * definition.cellVolts;
            const noLoadPropRpm = definition.motorKv * packVolts / definition.gearRatio;
            // The polynomials were fitted on a 0.167 D hub. A bigger hub blanks the inner
            // blade: thrust falls with the blanked area (r^2 dr -> h^3), torque with the
            // blanked moment (r^3 dr -> h^4). Static thrust then reads within 7 % of the
            // e-Foil Holland's published measurements on 12 props (2023).
            const hubRatio = definition.hubDiameter / D;
            const hubThrust = min(1, 1 - (hubRatio ^ 3 - 0.167 ^ 3));
            const hubTorque = min(1, 1 - (hubRatio ^ 4 - 0.167 ^ 4));
            const xHubCheck = max(0.2, hubRatio);
            const bladeArea = ear * PI * dMetres ^ 2 / 4 * expandedAreaFraction(xHubCheck, Z);   // m^2, outside the hub

            logLine(context, "---- operating check (open-water estimate, +/- 20 %) ----");
            logLine(context, "prop " ~ toMm(D) ~ " mm, P/D " ~ pd ~ ", EAR " ~ ear ~ ", " ~ Z ~ " blades, hub "
                ~ toMm(definition.hubDiameter) ~ " mm, rake " ~ toDeg(definition.rake) ~ " deg, skew "
                ~ toDeg(definition.skewAngle) ~ " deg, tip pitch " ~ definition.tipPitchChange ~ " %");
            logLine(context, "sections: root " ~ toMm(tRoot) ~ " mm, tip " ~ toMm(tTip) ~ " mm, edge "
                ~ toMm(tEdge) ~ " mm, taper exponent " ~ definition.taperExponent ~ ", tip chord "
                ~ toMm(definition.tipChord) ~ " mm");
            logLine(context, "motor " ~ definition.motorKv ~ " kV, " ~ definition.cellCount ~ "S at " ~ definition.cellVolts ~ " V/cell ("
                ~ (round(packVolts * 10) / 10) ~ " V), limit " ~ definition.currentLimit
                ~ " A, gear " ~ definition.gearRatio ~ ":1, torque constant " ~ definition.torqueConst ~ "/kV = "
                ~ (round(torquePerAmp * 10000) / 10) ~ " mNm/A");
            logLine(context, "shaft torque at the current limit: " ~ (round(shaftTorqueLimit * 100) / 100)
                ~ " Nm; no-load prop speed " ~ round(noLoadPropRpm) ~ " rpm");
            logLine(context, "hub at " ~ (round(hubRatio * 1000) / 1000) ~ " D: thrust x " ~ (round(hubThrust * 1000) / 1000)
                ~ ", torque x " ~ (round(hubTorque * 1000) / 1000) ~ " on the polynomials (fitted on 0.167 D); blade area outside the hub "
                ~ (round(bladeArea * 1e6)) ~ " mm^2");

            // Static: find the rpm where absorbed torque equals the torque limit
            const kq0 = openWaterCoefficient(KQ_TERMS, 0, pd, ear, Z);
            const kt0 = openWaterCoefficient(KT_TERMS, 0, pd, ear, Z);
            if (kq0 <= 0)
                logLine(context, "static: the polynomials return no torque for this geometry - outside the fitted range");
            else
            {
                const nStatic = sqrt(shaftTorqueLimit / (kq0 * hubTorque * SEAWATER_DENSITY * dMetres ^ 5));
                const rpmStatic = nStatic * 60;
                const thrustStatic = kt0 * hubThrust * SEAWATER_DENSITY * nStatic ^ 2 * dMetres ^ 4;
                const shaftKw = shaftTorqueLimit * 2 * PI * nStatic / 1000;
                logLine(context, "static, at the current limit: " ~ round(rpmStatic) ~ " prop rpm, "
                    ~ thrustText(thrustStatic) ~ " thrust, " ~ (round(shaftKw * 100) / 100) ~ " kW at the shaft");
                logLine(context, "  tip speed " ~ (round(PI * dMetres * nStatic * 10) / 10) ~ " m/s");

                if (rpmStatic > noLoadPropRpm)
                    logLine(context, "  WARNING: that is above no-load speed - the motor cannot reach the current limit, so the prop is UNDER-pitched or too small");
                else if (rpmStatic < 0.35 * noLoadPropRpm)
                    logLine(context, "  WARNING: under 35 % of no-load speed - the prop is OVER-pitched or too large and the motor will run hot at low rpm");
                else
                    logLine(context, "  operating point sits in a workable band (" ~ round(100 * rpmStatic / noLoadPropRpm) ~ " % of no-load speed)");

                if (PI * dMetres * nStatic > 25)
                    logLine(context, "  NOTE: tip speed over 25 m/s - expect ventilation and noise near the surface");
                bladeLoadingLines(context, 0, nStatic, thrustStatic, dMetres, bladeArea, definition.propDepth, true);
                if (definition.foldPinRadius != undefined)
                {
                    // thrust per blade acting near 0.7R, taken about the pin; the tongue's forward face carries it on the
                    // slot's forward wall with its resultant about two thirds of the way out along the tongue
                    const moment = thrustStatic / Z * (0.35 * D - definition.foldPinRadius) / meter;       // N m
                    const lever = 2 / 3 * definition.foldTongue / meter;
                    const stopForce = (lever > 0) ? moment / lever : 0;
                    const stopArea = (definition.foldBossWidth / meter) * (definition.foldTongue / meter);
                    logLine(context, "  folder: thrust moment about the pin " ~ (round(moment * 100) / 100) ~ " Nm per blade at static; stop face carries about "
                        ~ round(stopForce) ~ " N over " ~ round(stopArea * 1e6) ~ " mm^2 = " ~ (round(stopForce / stopArea / 1e5) / 10) ~ " MPa bearing (printed PET-CF or PPA-CF takes 40-60 MPa; keep under 15 with the layer lines across the tongue)");
                }
            }

            // Cruise: same torque limit, but the advance ratio unloads the blade
            if (definition.cruiseSpeed > 0)
            {
                const vCruise = definition.cruiseSpeed / 3.6;
                // Torque the motor can actually deliver at a given prop speed. With a
                // winding resistance the current limit only holds while there is
                // voltage headroom over the back-EMF; without one, assume the limit
                // is always available (optimistic near no-load speed).
                const rOhm = definition.windingResistance / 1000;

                // Bisection on rev/s: find where absorbed torque equals what the motor can give.
                // A coarse scan overshoots - torque climbs steeply with rpm, so a
                // 600 rpm step was reporting several kgf of thrust that isn't there.
                var nLow = 2;                                         // rev/s
                var nHigh = noLoadPropRpm / 60;
                const torqueScale = SEAWATER_DENSITY * dMetres ^ 5;
                for (var iter = 0; iter < 60; iter += 1)
                {
                    const nMid = (nLow + nHigh) / 2;
                    const motorRpm = nMid * 60 * definition.gearRatio;
                    var ampsHere = definition.currentLimit;
                    if (rOhm > 0)
                    {
                        const headroom = packVolts - motorRpm / definition.motorKv;
                        ampsHere = min(definition.currentLimit, max(0, headroom / rOhm));
                    }
                    const torqueHere = torquePerAmp * ampsHere * definition.gearRatio;
                    const kqMid = openWaterCoefficient(KQ_TERMS, vCruise / (nMid * dMetres), pd, ear, Z);
                    if (kqMid * hubTorque * torqueScale * nMid ^ 2 < torqueHere)
                        nLow = nMid;
                    else
                        nHigh = nMid;
                }
                const nCruise = (nLow + nHigh) / 2;
                {
                    const jC = vCruise / (nCruise * dMetres);
                    const ktC = openWaterCoefficient(KT_TERMS, jC, pd, ear, Z);
                    const kqC = openWaterCoefficient(KQ_TERMS, jC, pd, ear, Z);
                    const thrustC = ktC * hubThrust * SEAWATER_DENSITY * nCruise ^ 2 * dMetres ^ 4;
                    const etaC = (kqC > 0) ? jC * ktC / (2 * PI * kqC) : 0;
                    logLine(context, "full throttle at " ~ definition.cruiseSpeed ~ " km/h: about " ~ round(nCruise * 60)
                        ~ " prop rpm, J " ~ (round(jC * 100) / 100) ~ ", " ~ thrustText(thrustC)
                        ~ " thrust, open-water efficiency " ~ round(100 * etaC) ~ " %");
                    const torqueC = kqC * hubTorque * SEAWATER_DENSITY * nCruise ^ 2 * dMetres ^ 5;
                    const ampsC = torqueC / (torquePerAmp * definition.gearRatio);
                    logLine(context, "  drawing about " ~ round(ampsC) ~ " A of the " ~ definition.currentLimit
                        ~ " A limit, at " ~ round(100 * nCruise * 60 / noLoadPropRpm) ~ " % of no-load speed");
                    if (thrustC <= 0)
                        logLine(context, "  WARNING: no thrust at this speed - the prop is windmilling, raise P/D");
                    if (nCruise * 60 > 0.97 * noLoadPropRpm)
                        logLine(context, "  WARNING: hard against no-load speed - little margin at " ~ definition.cruiseSpeed
                            ~ " km/h. Top speed is set by pitch x no-load rpm less slip, so RAISE P/D (or lower the gear ratio, or add cells); lowering pitch makes it worse.");

                    // Theoretical no-slip speed at no-load rpm - the hard ceiling
                    const pitchSpeed = pd * dMetres * (noLoadPropRpm / 60) * 3.6;
                    logLine(context, "  pitch speed at no-load rpm: " ~ (round(pitchSpeed * 10) / 10)
                        ~ " km/h with no slip, so roughly " ~ (round(pitchSpeed * 0.7 * 10) / 10) ~ " km/h at 30 % slip");
                    if (rOhm <= 0)
                        logLine(context, "  (no winding resistance entered, so the current limit is assumed available at any rpm - optimistic near no-load)");
                    bladeLoadingLines(context, vCruise, nCruise, thrustC, dMetres, bladeArea, definition.propDepth, true);
                }

                // Steady cruise: thrust equals the foil's drag, so find the rev/s that gives it
                if (definition.cruiseDrag > 0)
                {
                    const drag = definition.cruiseDrag;
                    var nLo = 0.1;
                    var nHi = 4 * noLoadPropRpm / 60;
                    for (var iter = 0; iter < 80; iter += 1)
                    {
                        const nMid = (nLo + nHi) / 2;
                        const ktMid = openWaterCoefficient(KT_TERMS, vCruise / (nMid * dMetres), pd, ear, Z);
                        if (ktMid * hubThrust * SEAWATER_DENSITY * nMid ^ 2 * dMetres ^ 4 < drag)
                            nLo = nMid;
                        else
                            nHi = nMid;
                    }
                    const nD = (nLo + nHi) / 2;
                    const jD = vCruise / (nD * dMetres);
                    const ktD = openWaterCoefficient(KT_TERMS, jD, pd, ear, Z) * hubThrust;
                    const kqD = openWaterCoefficient(KQ_TERMS, jD, pd, ear, Z) * hubTorque;
                    const torqueD = kqD * SEAWATER_DENSITY * nD ^ 2 * dMetres ^ 5;
                    const etaD = (kqD > 0) ? jD * ktD / (2 * PI * kqD) : 0;
                    const shaftWD = 2 * PI * nD * torqueD;
                    const ampsD = torqueD / (torquePerAmp * definition.gearRatio);
                    const voltsD = nD * 60 * definition.gearRatio / definition.motorKv + ampsD * rOhm;
                    const inWD = voltsD * ampsD;
                    logLine(context, "steady cruise at " ~ definition.cruiseSpeed ~ " km/h against " ~ drag ~ " N of drag: " ~ round(nD * 60)
                        ~ " prop rpm, J " ~ (round(jD * 100) / 100) ~ ", open-water efficiency " ~ round(100 * etaD) ~ " %, "
                        ~ round(shaftWD) ~ " W at the shaft");
                    logLine(context, "  motor " ~ (round(ampsD * 10) / 10) ~ " A at " ~ (round(voltsD * 10) / 10) ~ " V ("
                        ~ round(100 * voltsD / packVolts) ~ " % throttle), " ~ round(inWD) ~ " W in, "
                        ~ (round(inWD / definition.cruiseSpeed * 10) / 10) ~ " Wh/km at the motor terminals (add the ESC and the motor's iron and no-load losses, and the pod's wake on the prop)");
                    if (nD * 60 > 0.97 * noLoadPropRpm)
                        logLine(context, "  WARNING: cruise needs more than no-load speed - the drive cannot hold " ~ definition.cruiseSpeed ~ " km/h; raise P/D or cells");
                    else if (ampsD > definition.currentLimit)
                        logLine(context, "  WARNING: cruise needs " ~ round(ampsD) ~ " A, over the " ~ definition.currentLimit ~ " A limit");
                    bladeLoadingLines(context, vCruise, nD, drag, dMetres, bladeArea, definition.propDepth, false);
                }
            }
            if (definition.sectionProfile != SectionProfile.B_SERIES)
                logLine(context, "NOTE: section profile is not B-series, so the polynomials no longer describe this blade - the figures below are for the equivalent B-series section");
            logLine(context, "the estimate uses diameter, P/D, EAR, blade count and the hub ratio only - rake, skew, tip pitch, thicknesses, the section and the tip shape do not enter the polynomials");
            logLine(context, "---- end operating check ----");
        }
}

annotation { "Feature Type Name" : "Universal B-Series Blade Engine",
             "Feature Type Description" : "Builds one Wageningen B-series propeller blade, lofted from r/R = 0.2 to the tip. Sections are wrapped onto cylinders, so the finished blade is truly diameter D. Union it with the hub, then pattern it around the shaft axis." }
export const bSeriesBlade = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Diameter (D)",
                     "Description" : "Tip-to-tip diameter of the finished propeller. The blade runs from 0.1 x D to 0.5 x D from the shaft axis.<br><b>Typical efoil:</b> 140-200 mm. <b>Accepts</b> 50-1000 mm." }
        isLength(definition.diameter, { (millimeter) : [50, 140, 1000] } as LengthBoundSpec);

        annotation { "Name" : "Hub diameter",
                     "Description" : "Diameter of the hub this blade joins. Sets where root thickness is measured and where the taper and skew start from. The blade always runs in to r/R = 0.2 so the union with the hub leaves no gap.<br><b>Typical:</b> 0.3-0.4 x D (52 mm on a 150 mm prop). <b>Accepts</b> 10-600 mm, and must be 0.22-0.6 x D." }
        isLength(definition.hubDiameter, { (millimeter) : [10, 52, 600] } as LengthBoundSpec);

        annotation { "Name" : "P/D ratio",
                     "Description" : "Pitch divided by diameter, quoted at 0.7R as in the B-series charts. Sets the twist: each section sits at atan(pitch / 2 x pi x r), steep at the root, flat at the tip.<br><b>Typical efoil:</b> 0.7-0.9. Lower for more thrust at low speed, higher for top speed. <b>Accepts</b> 0.2-2.0." }
        isReal(definition.pdRatio, { (unitless) : [0.2, 0.743, 2.0] } as RealBoundSpec);

        annotation { "Name" : "EAR",
                     "Description" : "Expanded area ratio: blade area of the whole propeller over disk area. Higher gives wider blades - more grip and less cavitation, more drag.<br><b>Typical efoil:</b> 0.45-0.65. <b>Ceiling</b> is set by the hub: the blades touch when Z x chord exceeds the hub circumference, which the feature warns about. <b>Accepts</b> 0.1-1.3." }
        isReal(definition.ear, { (unitless) : [0.1, 0.552, 1.3] } as RealBoundSpec);

        // Only ONE blade is ever generated. EAR is a whole-propeller figure, so the
        // chord of a single blade is K * D * EAR / Z: this input is that Z.
        // Pattern the blade in the Part Studio with Circular pattern.
        annotation { "Name" : "Blades (chord)",
                     "Description" : "Number of blades in the finished propeller. Used ONLY to divide EAR into one blade's chord and to pick the 2-3 or 4-7 blade outline table - this feature always builds one blade, which you pattern yourself.<br><b>Typical efoil:</b> 3. At 2 the chord doubles and a wide blade may fail to loft - lower EAR to suit. <b>Accepts</b> 2-7." }
        isInteger(definition.bladeCount, { (unitless) : [2, 3, 7] } as IntegerBoundSpec);

        annotation { "Name" : "Direction",
                     "Description" : "Which way the propeller turns, seen from astern (behind the propeller, looking forward along the direction of travel, +Z). <b>CW from astern</b> is a right-handed propeller, the marine convention for a single screw; <b>CCW</b> builds its true mirror image. Thrust is toward the direction of travel either way; the finished part is shown aft up (tail toward +Z), as it sits on a bench. Match it to the motor direction set in the ESC (an outrunner runs either way; swap two phases to reverse it). A twin or counter-rotating pair uses one of each.<br><b>Typical:</b> CW (right-hand). <b>Accepts</b> CW or CCW." }
        definition.rotation is Rotation;

        // Optional: leave the motor kV at zero and nothing here is calculated.
        annotation { "Group Name" : "Operating check", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Motor kV",
                         "Description" : "Motor kV (rpm per volt, no load), the start of the operating check: no-load speed is kV x pack voltage / gear ratio, and the torque constant is taken as 9.55 / kV Nm per amp. <b>Leave at 0 and no check is run.</b> With it and the fields below, the check prints the static and cruise operating points to the notices panel.<br><b>Typical efoil:</b> 100-160 kV on 12-14S (140 kV on the 63100 and 6384). <b>Accepts</b> 0-1000." }
            isReal(definition.motorKv, { (unitless) : [0, 0, 1000] } as RealBoundSpec);

            annotation { "Name" : "Cells (series)",
                         "Description" : "Battery cells in series. Pack voltage is cells x Cell V, so 12S at 3.7 V = 44.4 V.<br><b>Typical efoil:</b> 12-14S. <b>Accepts</b> 1-30." }
            isInteger(definition.cellCount, { (unitless) : [1, 12, 30] } as IntegerBoundSpec);

            annotation { "Name" : "Cell V",
                         "Description" : "Volts per cell to take the pack at, under load. Sets the no-load speed (kV x cells x this) and the back-EMF headroom, so it moves the top speed and the cruise current.<br><b>Typical:</b> 3.7 for a mid-discharge nominal figure; 4.0-4.1 for a fresh pack under load (what a VESC log shows on the first run); 3.4 near empty. <b>Accepts</b> 3.0-4.2 V." }
            isReal(definition.cellVolts, { (unitless) : [3.0, 3.7, 4.2] } as RealBoundSpec);

            annotation { "Name" : "Torque const",
                         "Description" : "Motor torque constant x kV, in Nm per amp times rpm per volt: the torque per amp is this divided by the kV. 9.55 is the ideal 60 / (2 pi) that a datasheet's mNm/A x kV comes to (the Flipsky 6384: 67.4 mNm/A x 140 kV = 9.44); 8.27 was the older figure this check used, which read 13 % low against measured static thrust.<br><b>Typical:</b> 9.55, or the datasheet's mNm/A x kV / 1000. <b>Accepts</b> 5-12." }
            isReal(definition.torqueConst, { (unitless) : [5, 9.55, 12] } as RealBoundSpec);

            annotation { "Name" : "ESC limit, A",
                         "Description" : "Motor (phase) current limit set in the ESC. Torque at the limit is the torque constant x this current, and the check reports the rpm at which the propeller absorbs exactly that torque, static and at cruise.<br><b>Typical:</b> 80-150 A (100 A on a Mini FSESC 6.8, 150 A on the Plus). <b>Accepts</b> 1-600 A." }
            isReal(definition.currentLimit, { (unitless) : [1, 100, 600] } as RealBoundSpec);

            annotation { "Name" : "Gear ratio",
                         "Description" : "Motor turns per propeller turn. Divides the no-load speed and multiplies the shaft torque.<br><b>Typical:</b> 1 for a direct-drive outrunner pod; 4 to 5 on a geared inrunner drive. <b>Accepts</b> 1-10." }
            isReal(definition.gearRatio, { (unitless) : [1, 1, 10] } as RealBoundSpec);

            annotation { "Name" : "Phase R, mOhm",
                         "Description" : "Motor phase resistance in milliohms. With it, the cruise point accounts for back-EMF: as rpm rises the voltage left to push current falls, so the motor cannot hold the current limit near no-load speed.<br><b>Typical:</b> 20-50 mOhm on a 63-series outrunner (31 on the 63100). <b>Accepts</b> 0-500; 0 assumes the current limit is available at any rpm, which flatters the cruise figure." }
            isReal(definition.windingResistance, { (unitless) : [0, 0, 500] } as RealBoundSpec);

            annotation { "Name" : "Cruise, km/h",
                         "Description" : "Boat speed through the water to report the cruise operating point at, alongside the static case. Thrust and efficiency depend strongly on it through the advance ratio J = V / (n x D).<br><b>Typical:</b> 20-30 km/h for an efoil, 25-40 km/h for a fast foil. <b>Accepts</b> 0-100 km/h." }
            isReal(definition.cruiseSpeed, { (unitless) : [0, 30, 100] } as RealBoundSpec);

            annotation { "Name" : "Cruise drag, N",
                         "Description" : "The foil's total drag at the cruise speed, in newtons, from the foil tool's performance sweep (drag_total_N at that speed) or a log. With it the check adds the steady cruise point, where thrust equals drag: prop rpm, efficiency, motor amps and volts, throttle, motor input power and Wh/km, and the cavitation number there rather than at full throttle. <b>0 skips it.</b><br><b>Typical:</b> 80-120 N for an efoil at 20-25 km/h. <b>Accepts</b> 0-2000 N." }
            isReal(definition.cruiseDrag, { (unitless) : [0, 0, 2000] } as RealBoundSpec);

            annotation { "Name" : "Depth",
                         "Description" : "Depth of the shaft axis below the surface when riding, for the cavitation number: (atmospheric + head - vapour pressure) over the dynamic pressure at 0.7R. Deeper helps a little; the dynamic pressure dominates.<br><b>Typical:</b> 500-700 mm on an efoil mast. <b>Accepts</b> 100-3000 mm." }
            isLength(definition.propDepth, { (millimeter) : [100, 600, 3000] } as LengthBoundSpec);
        }

        // Everything below has a workable default; the group opens collapsed.
        annotation { "Group Name" : "Advanced", "Collapsed By Default" : true }
        {
            // Pitch at the tip relative to nominal. Negative unloads the tip (less tip
            // vortex, noise and ventilation); 0 = constant pitch. Blends in
            // parabolically from 0.7R, so the quoted P/D still holds at 0.7R.
            annotation { "Name" : "Tip pitch, %",
                         "Description" : "Pitch at the tip relative to nominal. Held at nominal out to 0.7R, then blended parabolically to the tip.<br><b>Typical:</b> -10 %. Go to -15 or -20 % if the prop ventilates near the surface or sounds harsh; 0 gives constant pitch. Positive loads the tip and is rarely wanted. <b>Accepts</b> -30 to 30 %." }
            isReal(definition.tipPitchChange, { (unitless) : [-30, -10, 30] } as RealBoundSpec);

            annotation { "Name" : "Skew (+ back)",
                         "Description" : "Sweeps the blade around the shaft, from zero at the hub surface to the full angle at the tip. Positive sweeps back against rotation, which softens the load as each blade passes the mast.<br><b>Typical:</b> 0-25 deg; use more only to set how folding blades rest. <b>Accepts</b> -60 to 60 deg." }
            isAngle(definition.skewAngle, { (degree) : [-60, 0, 60] } as AngleBoundSpec);

            annotation { "Name" : "Root thick.",
                         "Description" : "Maximum section thickness where the blade leaves the hub - the main strength number. Inside the hub it keeps thickening along the same curve.<br><b>Typical:</b> 6-9 mm on a 150 mm prop, roughly 0.045 x D. <b>Accepts</b> 1-50 mm." }
            isLength(definition.rootThickness, { (millimeter) : [1, 7, 50] } as LengthBoundSpec);

            annotation { "Name" : "Rake (+ aft)",
                         "Description" : "Tilts the blade along the shaft axis. Positive leans the tip aft, away from the direction of travel, which relieves root bending stress.<br><b>Typical:</b> 5-15 deg. <b>Accepts</b> -30 to 45 deg." }
            isAngle(definition.rake, { (degree) : [-30, 15, 45] } as AngleBoundSpec);

            // t = tTip + (tRoot - tTip) * (1 - n)^exponent, n = 0 at the hub surface, 1 at the tip
            annotation { "Name" : "Taper exponent",
                         "Description" : "How thickness falls from root to tip: t = tip + (root - tip) x (1 - n)^exponent.<br><b>1</b> = linear, the B-series form, ~60 % more material at 0.7R. <b>2</b> = parabolic, thinner and lower drag. <b>Typical:</b> 1 for a printed blade; thin it toward 2 once one survives a session. <b>Accepts</b> 1-3." }
            isReal(definition.taperExponent, { (unitless) : [1, 2, 3] } as RealBoundSpec);

            annotation { "Name" : "Section profile",
                         "Description" : "Shape of the blade section, the same at every radius. <b>Typical:</b> B-series, the family the chord and pitch tables belong to. <b>B-series</b> uses the family's own ordinates - cambered inboard, flat-faced from about 0.7R out - and ignores the camber input. <b>NACA 16</b> (crest 0.50c) and <b>NACA 66-style</b> (0.45c) use the a = 0.8 marine mean line. <b>YS-920</b> and <b>E817</b> are aft-loaded marine sections, the best bet against cavitation and ventilation. <b>NACA 4-digit</b> and <b>Clark Y</b> (0.30c) are blunter and print-friendly. <b>Ogival</b>, <b>biconvex</b> and <b>elliptical</b> are simple forms. Only the B-series matches the operating check." }
            definition.sectionProfile is SectionProfile;

            annotation { "Name" : "Camber %",
                         "Description" : "Maximum camber as a percentage of chord, for the NACA and biconvex profiles. Ignored by the B-series, which carries its own camber, and by the ogival profile, whose flat face fixes it.<br><b>Typical:</b> 2-4 % for a marine section. Higher lifts more at low speed but cavitates sooner. <b>Accepts</b> 0-10 %." }
            isReal(definition.camberPercent, { (unitless) : [0, 2, 10] } as RealBoundSpec);

            annotation { "Name" : "Tip thick.",
                         "Description" : "Maximum section thickness at the tip.<br><b>Typical:</b> 1.2-2 mm, and at least 2-3 extrusion widths. Thin tips are quieter but chip on grounding. <b>Accepts</b> 0.2-20 mm; must be at least the edge thickness." }
            isLength(definition.tipThickness, { (millimeter) : [0.2, 1.2, 20] } as LengthBoundSpec);

            annotation { "Name" : "Edge thick.",
                         "Description" : "Height of the square-cut trailing edge, and the thickness the rounded leading-edge nose is built from.<br><b>Typical:</b> 0.8-1.2 mm with a 0.4 mm nozzle - two to three extrusion widths. Below 0.8 mm the edge will not print as modelled and the root section gets hard to cap. <b>Accepts</b> 0.2-10 mm." }
            isLength(definition.edgeThickness, { (millimeter) : [0.2, 1.2, 10] } as LengthBoundSpec);

            annotation { "Name" : "Tip chord",
                         "Description" : "Blade width at the very tip. The published B-series chord goes to zero there, which cannot be lofted or printed, so this squares it off.<br><b>Typical:</b> 0.04-0.07 x D (6-10 mm on a 150 mm prop). Larger looks blunt and adds tip vortex. <b>Accepts</b> 1-200 mm, and must be under half the chord at 0.9R." }
            isLength(definition.tipChord, { (millimeter) : [1, 10, 200] } as LengthBoundSpec);

            // Sections lofted from r/R = 0.2 to the tip, clustered toward the tip
            // where chord and pitch change fastest. 10 matches the published
            // stations; more gives a smoother loft at some regeneration cost.
            annotation { "Name" : "Round tip",
                         "Description" : "Closes the blade with an elliptical dome instead of a flat end face: over the last 2 % of radius chord and thickness scale down together on an ellipse, stopping at 45 % rather than a knife edge. Adds three sections out there.<br><b>Typical:</b> on. <b>Accepts</b> on or off; turn it off only if the tip loft misbehaves.", "Default" : true }
            definition.roundTip is boolean;

            annotation { "Name" : "Sections",
                         "Description" : "Number of section profiles lofted from r/R = 0.2 to the tip, spaced with extra density outboard.<br><b>Typical:</b> 20. Use 10-12 while iterating for fast regeneration, 25-30 for the final shape. Past 30 the gain is invisible and the loft gets harder to solve. <b>Accepts</b> 6-60." }
            isInteger(definition.sectionCount, { (unitless) : [6, 20, 60] } as IntegerBoundSpec);

            annotation { "Name" : "Keep sections",
                         "Description" : "Leaves the section curves in the Part Studio alongside the solid, instead of deleting them. Useful for inspecting the shape or taking sections into a drawing.<br><b>Typical:</b> off. <b>Accepts</b> on or off.", "Default" : false }
            definition.keepSections is boolean;

            // Prints inputs, a per-station table and each loft stage to the
            // FeatureScript notices panel.
            annotation { "Name" : "Print to notices",
                         "Description" : "Prints the inputs, a per-station table (radius, chord, thickness, pitch, wrap angle), each loft stage and the operating check to the FeatureScript notices panel and to the Part Studio variables propLog and propNotice. With it on, a failure is reported as a warning with its message instead of an error, so the log survives.<br><b>Typical:</b> on while designing, off for a finished part. <b>Accepts</b> on or off.", "Default" : false }
            definition.debugPrint is boolean;

            // Builds the ten section outlines and stops: no loft, no solid. Use it
            // when the loft fails, to see whether the sections themselves are sane.
            annotation { "Name" : "Sections only",
                         "Description" : "Builds the section curves and stops - no loft, no solid. Use it when the loft fails, to see whether the curves themselves are sane.<br><b>Typical:</b> off. <b>Accepts</b> on or off.", "Default" : false }
            definition.sectionsOnly is boolean;
        }
    }
    {
        propPrint(context, "propLog", "==== B-Series Blade feature, build " ~ PROP_FILE_BUILD ~ " ====");
        try
        {
            const blade = buildBlade(context, id, definition);
            if (blade.sectionsOnly)
            {
                showAftUp(context, id);
                return;
            }

            operatingCheck(context, definition);

            // ---- Universal anchor: mate connector on the shaft axis ---------------
            opMateConnector(context, id + "bladeRootMateTarget", {
                        "coordSystem" : coordSystem(vector(0, 0, 0) * millimeter, vector(1, 0, 0), vector(0, 0, 1)),
                        "owner" : blade.blade
                    });
            showAftUp(context, id);
        }
        catch (error)
        {
            const msg = "ERROR: " ~ toString(error);
            propPrint(context, "propLog", msg);
            propPrint(context, "propNotice", msg);
            // A failed feature is rolled back, variables included, so in debug mode the failure is
            // reported as a warning carrying the message and the feature is left standing with its log.
            if (definition.debugPrint)
                reportFeatureWarning(context, id, msg ~ " (Print to notices is on: the failure is shown as this warning so the log survives)");
            else
                throw error;
        }
    });

annotation { "Feature Type Name" : "B-Series Propeller",
             "Feature Type Description" : "Builds a complete propeller: one Wageningen B-series blade from the same generator as the blade feature, patterned round the shaft axis and joined to a tapered hub with a bore, keyway and bolt circle, with a root fillet. One solid. The blade fields are the blade feature's; the hub diameter the blade is built to is the hub's diameter at the blade root plane." }
export const bSeriesProp = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Diameter (D)",
                     "Description" : "Tip-to-tip diameter of the finished propeller. The blade runs from 0.1 x D to 0.5 x D from the shaft axis.<br><b>Typical efoil:</b> 140-200 mm. <b>Accepts</b> 50-1000 mm." }
        isLength(definition.diameter, { (millimeter) : [50, 140, 1000] } as LengthBoundSpec);

        annotation { "Name" : "P/D ratio",
                     "Description" : "Pitch divided by diameter, quoted at 0.7R as in the B-series charts. Sets the twist: each section sits at atan(pitch / 2 x pi x r), steep at the root, flat at the tip.<br><b>Typical efoil:</b> 0.7-0.9. Lower for more thrust at low speed, higher for top speed. <b>Accepts</b> 0.2-2.0." }
        isReal(definition.pdRatio, { (unitless) : [0.2, 0.743, 2.0] } as RealBoundSpec);

        annotation { "Name" : "EAR",
                     "Description" : "Expanded area ratio: blade area of the whole propeller over disk area. Higher gives wider blades - more grip and less cavitation, more drag.<br><b>Typical efoil:</b> 0.45-0.65. <b>Ceiling</b> is set by the hub: the blades touch when Z x chord exceeds the hub circumference, which the feature warns about. <b>Accepts</b> 0.1-1.3." }
        isReal(definition.ear, { (unitless) : [0.1, 0.552, 1.3] } as RealBoundSpec);

        // Only ONE blade is ever generated. EAR is a whole-propeller figure, so the
        // chord of a single blade is K * D * EAR / Z: this input is that Z.
        // Pattern the blade in the Part Studio with Circular pattern.
        annotation { "Name" : "Blades",
                     "Description" : "Number of blades. Each is the same B-series blade, patterned round the shaft axis and joined to the hub (or on its own pin in a folder). EAR is the whole propeller's, so one blade's chord is K x D x EAR / Z: a two-blade at EAR 0.2 has the same blade as a three-blade at 0.3. The outline table is the 2-3 blade one up to three blades and the 4-7 one above.<br><b>Typical efoil:</b> 3; 2 for a folder that must stow flat. <b>Accepts</b> 2-7." }
        isInteger(definition.bladeCount, { (unitless) : [2, 3, 7] } as IntegerBoundSpec);

        annotation { "Name" : "Direction",
                     "Description" : "Which way the propeller turns, seen from astern (behind the propeller, looking forward along the direction of travel, +Z). <b>CW from astern</b> is a right-handed propeller, the marine convention for a single screw; <b>CCW</b> builds its true mirror image. Thrust is toward the direction of travel either way; the finished part is shown aft up (tail toward +Z), as it sits on a bench. Match it to the motor direction set in the ESC (an outrunner runs either way; swap two phases to reverse it). A twin or counter-rotating pair uses one of each.<br><b>Typical:</b> CW (right-hand). <b>Accepts</b> CW or CCW." }
        definition.rotation is Rotation;

        annotation { "Group Name" : "Hub", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Hub type",
                         "Description" : "<b>Fixed blades</b>: the blades are joined to the hub with a root fillet - one solid.<br><b>Folding blades</b>: each blade pivots aft on a tangential pin in a slot of the hub (the Folding group), so the set is one hub body plus Z blade bodies (and Z pin bodies for a straight pin); the blades are shown deployed or folded. The hub is never cut for the blade beyond the tongue slot and the pin holes: the blade's root and fairing are trimmed to the hub's external profile deployed and wherever they would meet the hub through the fold, so a long hub behind the pin costs root area on the blade.<br><b>Typical:</b> Fixed for a motor-mounted efoil prop; Folding on a foil that is also sailed, paddled or towed. <b>Accepts</b> one of the two.", "Default" : "FIXED" }
            definition.hubType is HubType;

            annotation { "Name" : "Upstream dia",
                         "Description" : "Hub diameter at the upstream face, the one that meets the water first (the direction of travel; the finished part is shown aft up, so upstream points down, -Z). The hub is a straight taper from this to the downstream diameter.<br><b>Typical:</b> the motor's own diameter, 63 mm on a 63-series outrunner. <b>Accepts</b> 10-300 mm." }
            isLength(definition.hubUpstream, { (millimeter) : [10, 63, 300] } as LengthBoundSpec);

            annotation { "Name" : "Downstream dia",
                         "Description" : "Hub diameter at the downstream face.<br><b>Typical:</b> 0.8 x the upstream diameter, 50 mm on a 63 mm hub; equal for a plain cylinder. <b>Accepts</b> 10-300 mm." }
            isLength(definition.hubDownstream, { (millimeter) : [10, 50, 300] } as LengthBoundSpec);

            annotation { "Name" : "Hub length",
                         "Description" : "Length of the hub along the shaft, end face to end face, before any nose or tail cap. The blade root plane sits at its middle unless Axial shift moves it.<br><b>Typical:</b> 20-50 mm. <b>Accepts</b> 5-300 mm." }
            isLength(definition.hubLength, { (millimeter) : [5, 20, 300] } as LengthBoundSpec);

            annotation { "Name" : "Axial shift",
                         "Description" : "Moves the hub along the shaft relative to the blade root plane: positive upstream (toward the direction of travel, shown pointing down). 0 centres the hub on the blades.<br><b>Typical:</b> 0; a few mm aft when the blades are raked and the tips would overhang the downstream face. <b>Accepts</b> -100 to 100 mm." }
            isLength(definition.hubShift, { (millimeter) : [-100, 0, 100] } as LengthBoundSpec);

            annotation { "Name" : "Nose cap",
                         "Description" : "A cone on the upstream face, from the upstream diameter down to a fifth of it over this length. 0 leaves the face flat, which is right when the hub sits against the motor bell.<br><b>Typical:</b> 0 against a motor; 0.25-0.5 x the hub diameter for a free nose. <b>Accepts</b> 0-200 mm." }
            isLength(definition.noseLength, { (millimeter) : [0, 0, 200] } as LengthBoundSpec);

            annotation { "Name" : "Tail style",
                         "Description" : "What the downstream end of the hub does.<br><b>Flat face</b>: the hub ends square.<br><b>Cone cap</b>: a convex cone out of the face, Tail cap long, closing the wake behind a hub that hangs in the flow.<br><b>Dish</b>: a concave cone cut into the face, from the downstream rim in to the tail pocket at Dish angle from the face - the printed hub loses the mass it does not need, the bolt heads sit inside the dish, and the water leaves the rim cleanly. Tail pocket and Pocket floor apply to every style.<br><b>Typical:</b> Dish behind a motor bell; Cone cap on a hub that ends in the flow. <b>Accepts</b> one of the three." }
            definition.tailStyle is TailStyle;

            if (definition.tailStyle == TailStyle.CONE)
            {
                annotation { "Name" : "Tail cap",
                             "Description" : "Length of the convex cone on the downstream face, from the downstream diameter down to a fifth of it.<br><b>Typical:</b> 0.5-1 x the downstream diameter. <b>Accepts</b> 1-200 mm." }
                isLength(definition.tailLength, { (millimeter) : [1, 25, 200] } as LengthBoundSpec);
            }

            if (definition.tailStyle == TailStyle.DISH)
            {
                annotation { "Name" : "Dish angle",
                             "Description" : "Angle of the dish's cone from the downstream face plane: 0 would be flat, 90 a straight bore. The dish rises from the rim toward the axis by (rim radius - pocket radius) x tan(angle); it must stay clear of the upstream face by the pocket floor, or the feature warns and flattens it.<br><b>Typical:</b> 30 deg (the template). <b>Accepts</b> 5-60 deg." }
                isAngle(definition.dishAngle, { (degree) : [5, 30, 60] } as AngleBoundSpec);
            }

            annotation { "Name" : "Tail pocket",
                         "Description" : "Diameter of a pocket cut into the downstream end along the axis, up to the pocket floor: room for the shaft end, a nut or a circlip, and less printed mass. 0 for none. With a bore or centre bore as well, the bore continues through the floor.<br><b>Typical:</b> 24 mm over a 10 mm shaft end (the template); 0 on a solid hub. <b>Accepts</b> 0-200 mm." }
            isLength(definition.pocketDia, { (millimeter) : [0, 0, 200] } as LengthBoundSpec);

            annotation { "Name" : "Pocket floor",
                         "Description" : "Wall left between the pocket floor and the upstream face - the solid that carries the blades and takes the bolts. Used when Tail pocket is above 0.<br><b>Typical:</b> 10-14 mm (12 in the template); at least the bolt flange plus a few mm. <b>Accepts</b> 2-200 mm." }
            isLength(definition.pocketFloor, { (millimeter) : [2, 12, 200] } as LengthBoundSpec);

            if (definition.hubType == HubType.FIXED)
            {
            annotation { "Name" : "Root fillet",
                         "Description" : "Fillet radius where each blade meets the hub. 0 for none. The fillet is tried last; if the kernel refuses it (grazing angles at high skew or rake), the propeller is built without it and a warning says so.<br><b>Typical:</b> 2-4 mm. <b>Accepts</b> 0-20 mm." }
            isLength(definition.rootFillet, { (millimeter) : [0, 2, 20] } as LengthBoundSpec);

            annotation { "Name" : "Hub core", "Default" : false,
                         "Description" : "Also builds the printed core a one-piece carbon propeller is laid up round: the hub inset by Skin thick on its sides and both faces, with the same bore, keyway or bolt circle and the same tail dish and pocket as the hub, as a separate body named Hub core. The Propeller mold reads it for the laminate volume. Off for a printed propeller." }
            definition.hubCore is boolean;

            if (definition.hubCore)
            {
                annotation { "Name" : "Skin thick",
                             "Description" : "Laminate over the core on the hub's sides and faces. Limits 0.5-4 mm. Typical 1.5: seven plies of 200 gsm woven at 50 % fibre volume (0.22 mm each), or three of 450 gsm; the mold's layup line counts them. The dish and the pocket are the core's own bare faces." }
                isLength(definition.coreSkin, { (millimeter) : [0.5, 1.5, 4] } as LengthBoundSpec);
            }
            }
        }

        if (definition.hubType == HubType.FOLDING)
        {
            annotation { "Group Name" : "Folding", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Pin type",
                             "Description" : "What the blade pivots on.<br><b>Straight pin</b>: a plain pin (dowel, drill rod, stainless pin) pressed into the hub's cheeks and running in the boss; Pin dia, Hub fit and Blade fit size the holes, and a pin body is made per blade for the drawing and the print list.<br><b>Cap-head bolt and captive nut</b>: a socket head cap screw of Bolt size through the cheeks and the boss, its head in a counterbore on one cheek and a hex nut in a pocket on the other, so the pin can be fitted and removed without a press.<br><b>Typical:</b> a 4 mm stainless pin, or an M4 bolt. <b>Accepts</b> one of the two.", "Default" : "PIN" }
                definition.pinType is PinType;

                if (definition.pinType == PinType.BOLT)
                {
                    annotation { "Name" : "Pin bolt",
                                 "Description" : "Metric size of the cap-head bolt that is the pin: ISO 273 clearance through the cheeks and the boss, an ISO 4762 head counterbore on one cheek, an ISO 4032 nut pocket (across flats + 0.3 mm, depth + 1) on the other. Custom uses Pin dia as the hole.<br><b>Typical:</b> M4. <b>Accepts</b> M3-M8 or Custom.", "Default" : "M4" }
                    definition.pinBolt is BoltSize;
                }

                if (definition.pinType == PinType.PIN || definition.pinBolt == BoltSize.CUSTOM)
                {
                    annotation { "Name" : "Pin dia",
                                 "Description" : "Diameter of the straight pin, or the hole for a custom bolt.<br><b>Typical:</b> 4 mm on a 140-160 mm prop, 5 mm on a 200 mm (the geared 52 mm hub uses 5 mm). <b>Accepts</b> 2-12 mm." }
                    isLength(definition.pinDia, { (millimeter) : [2, 4, 12] } as LengthBoundSpec);
                }

                if (definition.pinType == PinType.PIN)
                {
                    annotation { "Name" : "Hub fit",
                                 "Description" : "Added to the pin diameter for the holes in the hub's cheeks: negative for a press fit, positive for a clearance so the pin is retained another way.<br><b>Typical:</b> -0.1 mm for a press fit in a printed cheek (printed holes also come out small; ream if needed). <b>Accepts</b> -0.5 to 0.5 mm." }
                    isLength(definition.hubFit, { (millimeter) : [-0.5, -0.1, 0.5] } as LengthBoundSpec);

                    annotation { "Name" : "Blade fit",
                                 "Description" : "Added to the pin diameter for the bore through the blade's boss: the running clearance the blade pivots on.<br><b>Typical:</b> 0.2 mm printed, 0.1 mm reamed. <b>Accepts</b> 0-1 mm." }
                    isLength(definition.bladeFit, { (millimeter) : [0, 0.2, 1] } as LengthBoundSpec);

                    annotation { "Name" : "Pin bodies",
                                 "Description" : "Makes a pin body per blade, cut flush with the hub surface, so the pins appear in the drawing, the parts list and the assembly.<br><b>Typical:</b> on. <b>Accepts</b> on or off.", "Default" : true }
                    definition.makePins is boolean;
                }

                annotation { "Name" : "Pin radius",
                             "Description" : "Distance of the pin axis from the shaft axis. The pin lies tangential (across the blade's radial line) at this radius; it must sit inside the hub with wall to spare: the slot around the boss reaches Boss dia / 2 + Clearance beyond it. Further out gives a longer hub cheek behind the pin and a shorter tongue; further in leaves more cheek wall.<br><b>Typical:</b> 0.6-0.7 x the hub radius (20 mm on a 63 mm hub, 17 on a 52). <b>Accepts</b> 5-250 mm." }
                isLength(definition.pinRadius, { (millimeter) : [5, 20, 250] } as LengthBoundSpec);

                annotation { "Name" : "Pin axial",
                             "Description" : "Axial offset of the pin from the blade root's generator line, which sits at z = -r x tan(rake) at the root (aft of the root plane on a raked blade): positive upstream. At 0 the tongue is centred in the root section. The blade folds aft about the pin, so the hub must extend behind it by at least the boss radius plus clearance. The part of the root section aft of the pin swings in toward the shaft when the blade folds, so a pin further aft (negative) leaves a bigger core behind the pins for the tail relief: on a wide root this is the lever.<br><b>Typical:</b> 0; -5 to -10 mm on a two-blade or a low-EAR-per-blade root that reports a thin core. <b>Accepts</b> -100 to 100 mm." }
                isLength(definition.pinAxial, { (millimeter) : [-100, 0, 100] } as LengthBoundSpec);

                annotation { "Name" : "Boss dia",
                             "Description" : "Diameter of the cylindrical boss on the blade root around the pin, and the height of the tongue that joins it to the blade. The slot in the hub is this plus twice the clearance. Keep it under the blade's root section so the tongue hides inside the root.<br><b>Typical:</b> 3 x the pin diameter, 12-16 mm. <b>Accepts</b> 5-60 mm." }
                isLength(definition.bossDia, { (millimeter) : [5, 14, 60] } as LengthBoundSpec);

                annotation { "Name" : "Boss width",
                             "Description" : "Width of the boss and tongue along the pin: the bearing length on the pin and the thickness that takes the stop load. The hub slot is this plus twice the clearance, so wider bosses leave narrower cheeks.<br><b>Typical:</b> 8-12 mm on a 63 mm hub. <b>Accepts</b> 3-40 mm." }
                isLength(definition.bossWidth, { (millimeter) : [3, 10, 40] } as LengthBoundSpec);

                annotation { "Name" : "Root fairing",
                             "Description" : "Length outside the hub over which the aerofoil is lofted down to the tongue's section (Boss width by Boss dia) at the hub surface, so the root blends into the boss instead of ending in a bare rectangle. The fairing sits on the hub's outside, shaped to its profile; the hub is never cut for it. 0 leaves the bare tongue.<br><b>Typical:</b> 0.25-0.4 x the root chord, 8-12 mm on a 140-160 mm prop. <b>Accepts</b> 0-60 mm." }
                isLength(definition.rootFairing, { (millimeter) : [0, 8, 60] } as LengthBoundSpec);

                annotation { "Name" : "Tail relief",
                             "Description" : "Turns the hub's trailing end behind the pins down to a round shoulder of the tongue's length about the pin circle, with a core tube behind it (its size set by the pin radius less the root section's reach aft of the pin: Pin axial aft grows it, Tube wall keeps the shaft tube inside it), all the way round, so the blade root - its leading edge above all - swings aft without meeting the cheeks' trailing part; the cheeks keep their pins out to the shoulder. Off leaves the hub whole behind the pins and lets the hub trim the blade root instead.<br><b>Typical:</b> on. <b>Accepts</b> on or off.", "Default" : true }
                definition.tailRelief is boolean;

                annotation { "Name" : "Tube wall",
                             "Description" : "Wall the hub keeps round its bore through the folding root: a tube of the bore plus this wall each side stays solid through the gap and the tail relief, and the blade - boss, tongue, fairing and root - is trimmed to clear it (plus Clearance) in every position through the fold. Use it when the pins sit close to the shaft, so the boss's inside edge is shaped round the shaft tube instead of the gap or the relief breaking into the bore. 0 turns it off. Needs a bore (a keyed, plain or piloted mount).<br><b>Typical:</b> 0; 2-3 mm printed when the core behind the pins is reported thin. <b>Accepts</b> 0-10 mm." }
                isLength(definition.tubeWall, { (millimeter) : [0, 0, 10] } as LengthBoundSpec);

                annotation { "Name" : "Gap width",
                             "Description" : "Width, along the pin, of the opening between the hub's cheeks that the blade's boss and root swing through: a flat-walled gap from the tail up to the boss's top plus clearance, whose forward wall is the stop the root bears on under thrust. The cheeks left between the gaps carry the pins, so the gaps must leave them at least 3-4 mm plus the head or nut pocket. Never less than the boss plus clearance.<br><b>Typical:</b> the boss width plus 2-6 mm for the fairing, 12-16 mm on a 63 mm hub; wider on a printed hub with narrow cheeks like the geared 52 mm one. <b>Accepts</b> 4-100 mm." }
                isLength(definition.gapWidth, { (millimeter) : [4, 12, 100] } as LengthBoundSpec);

                annotation { "Name" : "Clearance",
                             "Description" : "Gap between the boss or root and the cheeks, each side, between the root and the hub surface, and between the trimmed root and the hub through the fold.<br><b>Typical:</b> 0.3 mm printed. <b>Accepts</b> 0.1-2 mm." }
                isLength(definition.foldClear, { (millimeter) : [0.1, 0.3, 2] } as LengthBoundSpec);

                annotation { "Name" : "Fold angle",
                             "Description" : "How far the blade swings aft from deployed: the tongue slot is cut to allow it, the blade root is trimmed wherever it would meet the hub on the way, and Show folded displays it. 90 lays the blade along the shaft axis.<br><b>Typical:</b> 90 deg; 100-110 to tuck the tips in against a tapered hub. <b>Accepts</b> 30-150 deg." }
                isAngle(definition.foldAngle, { (degree) : [30, 90, 150] } as AngleBoundSpec);

                annotation { "Name" : "Show folded",
                             "Description" : "Rotates the blades to Fold angle and checks that they clear the hub and each other; off shows them deployed. Either way the hub and the blades are built the same.<br><b>Typical:</b> off to design the blade, on to check the fold. <b>Accepts</b> on or off.", "Default" : false }
                definition.showFolded is boolean;
            }
        }

        annotation { "Group Name" : "Mount", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Mount type",
                         "Description" : "How the propeller attaches to the drive; only that type's fields are shown.<br><b>None</b>: a solid hub, bored or slotted later (or by a folding-hub feature).<br><b>Plain bore</b>: a through bore for a threaded, press-fit or grub-screwed shaft.<br><b>Bore and keyway</b>: the bore plus a keyway along it, for a keyed motor shaft.<br><b>Bolt circle</b>: holes on a pitch circle in the downstream face plus an optional centre bore, for a prop that bolts to an outrunner bell (the 63-series motors).<br><b>Typical:</b> Bolt circle on a 63-series outrunner, Bore and keyway on a geared inrunner drive. <b>Accepts</b> one of the four." }
            definition.mountType is MountType;

            if (definition.mountType == MountType.BORE || definition.mountType == MountType.KEYED)
            {
                annotation { "Name" : "Bore dia",
                             "Description" : "Through bore along the shaft axis. For a threaded shaft enter the tapping drill size; for a keyed shaft the shaft diameter plus clearance.<br><b>Typical:</b> 10 mm shaft: 10.2; M8 thread: 6.8. <b>Accepts</b> 1-100 mm." }
                isLength(definition.boreDia, { (millimeter) : [1, 10.2, 100] } as LengthBoundSpec);
            }

            if (definition.mountType == MountType.KEYED)
            {
                annotation { "Name" : "Keyway width",
                             "Description" : "Width of the keyway cut along the bore.<br><b>Typical:</b> 3 mm on a 10 mm shaft. <b>Accepts</b> 0.5-20 mm." }
                isLength(definition.keyWidth, { (millimeter) : [0.5, 3, 20] } as LengthBoundSpec);

                annotation { "Name" : "Keyway depth",
                             "Description" : "Depth of the keyway beyond the bore radius.<br><b>Typical:</b> 1.4 mm for a 3 mm key. <b>Accepts</b> 0.1-10 mm." }
                isLength(definition.keyDepth, { (millimeter) : [0.1, 1.4, 10] } as LengthBoundSpec);
            }

            if (definition.mountType == MountType.BOLTED)
            {
                annotation { "Name" : "Centre bore",
                             "Description" : "Through bore at the centre for the shaft end or a locating spigot on the bell, 0 for none.<br><b>Typical:</b> 12 mm over a 10 mm shaft end. <b>Accepts</b> 0-100 mm." }
                isLength(definition.pilotDia, { (millimeter) : [0, 12, 100] } as LengthBoundSpec);

                annotation { "Name" : "Bolt size",
                             "Description" : "Metric bolt size. Sets the through hole to the ISO 273 medium clearance (M3 3.4, M4 4.5, M5 5.5, M6 6.6, M8 9 mm) and the head recess to an ISO 4762 socket head plus 1 mm (M3 6.5, M4 8, M5 9.5, M6 11, M8 14 mm). <b>Custom</b> shows Bolt hole dia and Head dia instead.<br><b>Typical:</b> M4 on a 63-series bell, M3 on a 50-series. <b>Accepts</b> M3-M8 or Custom.", "Default" : "M4" }
                definition.boltSize is BoltSize;

                if (definition.boltSize == BoltSize.CUSTOM)
                {
                    annotation { "Name" : "Bolt hole dia",
                                 "Description" : "Diameter of each through hole.<br><b>Typical:</b> the clearance size, or the tapping drill for a hub that is tapped itself (M4: 3.3). <b>Accepts</b> 1-20 mm." }
                    isLength(definition.boltDia, { (millimeter) : [1, 4.5, 20] } as LengthBoundSpec);

                    annotation { "Name" : "Head dia",
                                 "Description" : "Diameter of the head recess, cut from the downstream end up to the bolt flange.<br><b>Typical:</b> the head diameter plus 1 mm (8 for an M4 socket head). <b>Accepts</b> 1-40 mm." }
                    isLength(definition.headDia, { (millimeter) : [1, 8, 40] } as LengthBoundSpec);
                }

                annotation { "Name" : "Recess heads",
                             "Description" : "Counterbores the head recess from the downstream end up to the bolt flange, so the heads sit inside the hub (or the dish). Off leaves plain through holes for bolts that come in from the motor side or for heads that sit proud.<br><b>Typical:</b> on. <b>Accepts</b> on or off.", "Default" : true }
                definition.recessHeads is boolean;

                annotation { "Name" : "Bolt count",
                             "Description" : "Number of holes, equally spaced on the pitch circle in the downstream face.<br><b>Typical:</b> 4 on a 63-series bell. <b>Accepts</b> 2-12." }
                isInteger(definition.boltCount, { (unitless) : [2, 4, 12] } as IntegerBoundSpec);

                annotation { "Name" : "Bolt PCD",
                             "Description" : "Pitch circle diameter of the bolt holes.<br><b>Typical:</b> a 63-series bell: 25 to 32 mm; read it off the motor. <b>Accepts</b> 5-250 mm." }
                isLength(definition.boltPcd, { (millimeter) : [5, 25, 250] } as LengthBoundSpec);

                annotation { "Name" : "Bolt flange",
                             "Description" : "Wall left under the bolt heads at the upstream face: the bolt passes through this into the motor bell, so it sets the thread engagement (bolt length = flange + washer + engagement). The holes themselves always go right through the hub.<br><b>Typical:</b> 6-8 mm (7 in the template). <b>Accepts</b> 1-100 mm." }
                isLength(definition.boltFlange, { (millimeter) : [1, 7, 100] } as LengthBoundSpec);
            }
        }

        // Optional: leave the motor kV at zero and nothing here is calculated.
        annotation { "Group Name" : "Operating check", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Motor kV",
                         "Description" : "Motor kV (rpm per volt, no load), the start of the operating check: no-load speed is kV x pack voltage / gear ratio, and the torque constant is taken as 9.55 / kV Nm per amp. <b>Leave at 0 and no check is run.</b> With it and the fields below, the check prints the static and cruise operating points to the notices panel.<br><b>Typical efoil:</b> 100-160 kV on 12-14S (140 kV on the 63100 and 6384). <b>Accepts</b> 0-1000." }
            isReal(definition.motorKv, { (unitless) : [0, 0, 1000] } as RealBoundSpec);

            annotation { "Name" : "Cells (series)",
                         "Description" : "Battery cells in series. Pack voltage is cells x Cell V, so 12S at 3.7 V = 44.4 V.<br><b>Typical efoil:</b> 12-14S. <b>Accepts</b> 1-30." }
            isInteger(definition.cellCount, { (unitless) : [1, 12, 30] } as IntegerBoundSpec);

            annotation { "Name" : "Cell V",
                         "Description" : "Volts per cell to take the pack at, under load. Sets the no-load speed (kV x cells x this) and the back-EMF headroom, so it moves the top speed and the cruise current.<br><b>Typical:</b> 3.7 for a mid-discharge nominal figure; 4.0-4.1 for a fresh pack under load (what a VESC log shows on the first run); 3.4 near empty. <b>Accepts</b> 3.0-4.2 V." }
            isReal(definition.cellVolts, { (unitless) : [3.0, 3.7, 4.2] } as RealBoundSpec);

            annotation { "Name" : "Torque const",
                         "Description" : "Motor torque constant x kV, in Nm per amp times rpm per volt: the torque per amp is this divided by the kV. 9.55 is the ideal 60 / (2 pi) that a datasheet's mNm/A x kV comes to (the Flipsky 6384: 67.4 mNm/A x 140 kV = 9.44); 8.27 was the older figure this check used, which read 13 % low against measured static thrust.<br><b>Typical:</b> 9.55, or the datasheet's mNm/A x kV / 1000. <b>Accepts</b> 5-12." }
            isReal(definition.torqueConst, { (unitless) : [5, 9.55, 12] } as RealBoundSpec);

            annotation { "Name" : "ESC limit, A",
                         "Description" : "Motor (phase) current limit set in the ESC. Torque at the limit is the torque constant x this current, and the check reports the rpm at which the propeller absorbs exactly that torque, static and at cruise.<br><b>Typical:</b> 80-150 A (100 A on a Mini FSESC 6.8, 150 A on the Plus). <b>Accepts</b> 1-600 A." }
            isReal(definition.currentLimit, { (unitless) : [1, 100, 600] } as RealBoundSpec);

            annotation { "Name" : "Gear ratio",
                         "Description" : "Motor turns per propeller turn. Divides the no-load speed and multiplies the shaft torque.<br><b>Typical:</b> 1 for a direct-drive outrunner pod; 4 to 5 on a geared inrunner drive. <b>Accepts</b> 1-10." }
            isReal(definition.gearRatio, { (unitless) : [1, 1, 10] } as RealBoundSpec);

            annotation { "Name" : "Phase R, mOhm",
                         "Description" : "Motor phase resistance in milliohms. With it, the cruise point accounts for back-EMF: as rpm rises the voltage left to push current falls, so the motor cannot hold the current limit near no-load speed.<br><b>Typical:</b> 20-50 mOhm on a 63-series outrunner (31 on the 63100). <b>Accepts</b> 0-500; 0 assumes the current limit is available at any rpm, which flatters the cruise figure." }
            isReal(definition.windingResistance, { (unitless) : [0, 0, 500] } as RealBoundSpec);

            annotation { "Name" : "Cruise, km/h",
                         "Description" : "Boat speed through the water to report the cruise operating point at, alongside the static case. Thrust and efficiency depend strongly on it through the advance ratio J = V / (n x D).<br><b>Typical:</b> 20-30 km/h for an efoil, 25-40 km/h for a fast foil. <b>Accepts</b> 0-100 km/h." }
            isReal(definition.cruiseSpeed, { (unitless) : [0, 30, 100] } as RealBoundSpec);

            annotation { "Name" : "Cruise drag, N",
                         "Description" : "The foil's total drag at the cruise speed, in newtons, from the foil tool's performance sweep (drag_total_N at that speed) or a log. With it the check adds the steady cruise point, where thrust equals drag: prop rpm, efficiency, motor amps and volts, throttle, motor input power and Wh/km, and the cavitation number there rather than at full throttle. <b>0 skips it.</b><br><b>Typical:</b> 80-120 N for an efoil at 20-25 km/h. <b>Accepts</b> 0-2000 N." }
            isReal(definition.cruiseDrag, { (unitless) : [0, 0, 2000] } as RealBoundSpec);

            annotation { "Name" : "Depth",
                         "Description" : "Depth of the shaft axis below the surface when riding, for the cavitation number: (atmospheric + head - vapour pressure) over the dynamic pressure at 0.7R. Deeper helps a little; the dynamic pressure dominates.<br><b>Typical:</b> 500-700 mm on an efoil mast. <b>Accepts</b> 100-3000 mm." }
            isLength(definition.propDepth, { (millimeter) : [100, 600, 3000] } as LengthBoundSpec);
        }

        // Everything below has a workable default; the group opens collapsed.
        annotation { "Group Name" : "Advanced", "Collapsed By Default" : true }
        {
            // Pitch at the tip relative to nominal. Negative unloads the tip (less tip
            // vortex, noise and ventilation); 0 = constant pitch. Blends in
            // parabolically from 0.7R, so the quoted P/D still holds at 0.7R.
            annotation { "Name" : "Tip pitch, %",
                         "Description" : "Pitch at the tip relative to nominal. Held at nominal out to 0.7R, then blended parabolically to the tip.<br><b>Typical:</b> -10 %. Go to -15 or -20 % if the prop ventilates near the surface or sounds harsh; 0 gives constant pitch. Positive loads the tip and is rarely wanted. <b>Accepts</b> -30 to 30 %." }
            isReal(definition.tipPitchChange, { (unitless) : [-30, -10, 30] } as RealBoundSpec);

            annotation { "Name" : "Skew (+ back)",
                         "Description" : "Sweeps the blade around the shaft, from zero at the hub surface to the full angle at the tip. Positive sweeps back against rotation, which softens the load as each blade passes the mast.<br><b>Typical:</b> 0-25 deg; use more only to set how folding blades rest. <b>Accepts</b> -60 to 60 deg." }
            isAngle(definition.skewAngle, { (degree) : [-60, 0, 60] } as AngleBoundSpec);

            annotation { "Name" : "Root thick.",
                         "Description" : "Maximum section thickness where the blade leaves the hub - the main strength number. Inside the hub it keeps thickening along the same curve.<br><b>Typical:</b> 6-9 mm on a 150 mm prop, roughly 0.045 x D. <b>Accepts</b> 1-50 mm." }
            isLength(definition.rootThickness, { (millimeter) : [1, 7, 50] } as LengthBoundSpec);

            annotation { "Name" : "Rake (+ aft)",
                         "Description" : "Tilts the blade along the shaft axis. Positive leans the tip aft, away from the direction of travel, which relieves root bending stress.<br><b>Typical:</b> 5-15 deg. <b>Accepts</b> -30 to 45 deg." }
            isAngle(definition.rake, { (degree) : [-30, 15, 45] } as AngleBoundSpec);

            // t = tTip + (tRoot - tTip) * (1 - n)^exponent, n = 0 at the hub surface, 1 at the tip
            annotation { "Name" : "Taper exponent",
                         "Description" : "How thickness falls from root to tip: t = tip + (root - tip) x (1 - n)^exponent.<br><b>1</b> = linear, the B-series form, ~60 % more material at 0.7R. <b>2</b> = parabolic, thinner and lower drag. <b>Typical:</b> 1 for a printed blade; thin it toward 2 once one survives a session. <b>Accepts</b> 1-3." }
            isReal(definition.taperExponent, { (unitless) : [1, 2, 3] } as RealBoundSpec);

            annotation { "Name" : "Section profile",
                         "Description" : "Shape of the blade section, the same at every radius. <b>Typical:</b> B-series, the family the chord and pitch tables belong to. <b>B-series</b> uses the family's own ordinates - cambered inboard, flat-faced from about 0.7R out - and ignores the camber input. <b>NACA 16</b> (crest 0.50c) and <b>NACA 66-style</b> (0.45c) use the a = 0.8 marine mean line. <b>YS-920</b> and <b>E817</b> are aft-loaded marine sections, the best bet against cavitation and ventilation. <b>NACA 4-digit</b> and <b>Clark Y</b> (0.30c) are blunter and print-friendly. <b>Ogival</b>, <b>biconvex</b> and <b>elliptical</b> are simple forms. Only the B-series matches the operating check." }
            definition.sectionProfile is SectionProfile;

            annotation { "Name" : "Camber %",
                         "Description" : "Maximum camber as a percentage of chord, for the NACA and biconvex profiles. Ignored by the B-series, which carries its own camber, and by the ogival profile, whose flat face fixes it.<br><b>Typical:</b> 2-4 % for a marine section. Higher lifts more at low speed but cavitates sooner. <b>Accepts</b> 0-10 %." }
            isReal(definition.camberPercent, { (unitless) : [0, 2, 10] } as RealBoundSpec);

            annotation { "Name" : "Tip thick.",
                         "Description" : "Maximum section thickness at the tip.<br><b>Typical:</b> 1.2-2 mm, and at least 2-3 extrusion widths. Thin tips are quieter but chip on grounding. <b>Accepts</b> 0.2-20 mm; must be at least the edge thickness." }
            isLength(definition.tipThickness, { (millimeter) : [0.2, 1.2, 20] } as LengthBoundSpec);

            annotation { "Name" : "Edge thick.",
                         "Description" : "Height of the square-cut trailing edge, and the thickness the rounded leading-edge nose is built from.<br><b>Typical:</b> 0.8-1.2 mm with a 0.4 mm nozzle - two to three extrusion widths. Below 0.8 mm the edge will not print as modelled and the root section gets hard to cap. <b>Accepts</b> 0.2-10 mm." }
            isLength(definition.edgeThickness, { (millimeter) : [0.2, 1.2, 10] } as LengthBoundSpec);

            annotation { "Name" : "Tip chord",
                         "Description" : "Blade width at the very tip. The published B-series chord goes to zero there, which cannot be lofted or printed, so this squares it off.<br><b>Typical:</b> 0.04-0.07 x D (6-10 mm on a 150 mm prop). Larger looks blunt and adds tip vortex. <b>Accepts</b> 1-200 mm, and must be under half the chord at 0.9R." }
            isLength(definition.tipChord, { (millimeter) : [1, 10, 200] } as LengthBoundSpec);

            // Sections lofted from r/R = 0.2 to the tip, clustered toward the tip
            // where chord and pitch change fastest. 10 matches the published
            // stations; more gives a smoother loft at some regeneration cost.
            annotation { "Name" : "Round tip",
                         "Description" : "Closes the blade with an elliptical dome instead of a flat end face: over the last 2 % of radius chord and thickness scale down together on an ellipse, stopping at 45 % rather than a knife edge. Adds three sections out there.<br><b>Typical:</b> on. <b>Accepts</b> on or off; turn it off only if the tip loft misbehaves.", "Default" : true }
            definition.roundTip is boolean;

            annotation { "Name" : "Sections",
                         "Description" : "Number of section profiles lofted from r/R = 0.2 to the tip, spaced with extra density outboard.<br><b>Typical:</b> 20. Use 10-12 while iterating for fast regeneration, 25-30 for the final shape. Past 30 the gain is invisible and the loft gets harder to solve. <b>Accepts</b> 6-60." }
            isInteger(definition.sectionCount, { (unitless) : [6, 20, 60] } as IntegerBoundSpec);

            annotation { "Name" : "Keep sections",
                         "Description" : "Leaves the section curves in the Part Studio alongside the solid, instead of deleting them. Useful for inspecting the shape or taking sections into a drawing.<br><b>Typical:</b> off. <b>Accepts</b> on or off.", "Default" : false }
            definition.keepSections is boolean;

            // Prints inputs, a per-station table and each loft stage to the
            // FeatureScript notices panel.
            annotation { "Name" : "Print to notices",
                         "Description" : "Prints the inputs, a per-station table (radius, chord, thickness, pitch, wrap angle), each loft stage and the operating check to the FeatureScript notices panel and to the Part Studio variables propLog and propNotice. With it on, a failure is reported as a warning with its message instead of an error, so the log survives.<br><b>Typical:</b> on while designing, off for a finished part. <b>Accepts</b> on or off.", "Default" : false }
            definition.debugPrint is boolean;

            // Builds the ten section outlines and stops: no loft, no solid. Use it
            // when the loft fails, to see whether the sections themselves are sane.
            annotation { "Name" : "Sections only",
                         "Description" : "Builds the section curves and stops - no loft, no solid. Use it when the loft fails, to see whether the curves themselves are sane.<br><b>Typical:</b> off. <b>Accepts</b> on or off.", "Default" : false }
            definition.sectionsOnly is boolean;
        }
    }
    {
        propPrint(context, "propLog", "==== B-Series Propeller feature, build " ~ PROP_FILE_BUILD ~ " ====");
        try
        {
            // ---- 1. The hub's geometry along the shaft (Z is the shaft axis, +Z upstream / travel) ----
            const Ru = definition.hubUpstream / 2;
            const Rd = definition.hubDownstream / 2;
            const zTop = definition.hubShift + definition.hubLength / 2;      // upstream face
            const zBot = definition.hubShift - definition.hubLength / 2;      // downstream face
            // hub radius where the blade root plane (z = 0) crosses the taper: what the blade is built to
            const fz = (zTop - 0 * millimeter) / (zTop - zBot);
            var rBlade = Ru + (Rd - Ru) * fz;
            if (zTop < 0 * millimeter || zBot > 0 * millimeter)
            {
                rBlade = min(Ru, Rd);
                propPrint(context, "propLog", "[prop] WARNING: the blade root plane (z = 0) lies outside the hub");
                reportFeatureWarning(context, id, "The blade root plane (z = 0) lies outside the hub: reduce Axial shift or lengthen the hub. Building to the smaller hub diameter.");
            }
            var bladeDef = definition;
            bladeDef.hubDiameter = 2 * rBlade;
            if (abs(Ru - Rd) > 0.01 * millimeter)
            {
                const why = "the blade is built to the hub's " ~ toMm(2 * rBlade) ~ " mm where its root plane (z = 0) crosses the taper from Hub upstream " ~ toMm(2 * Ru) ~ " to Hub downstream " ~ toMm(2 * Rd) ~ " mm; set both the same for a cylindrical hub";
                propPrint(context, "propLog", "[prop] hub: " ~ why);
                reportFeatureInfo(context, id, "Tapered hub: " ~ why ~ ".");
            }

            // ---- 2. One blade, from the shared builder ----
            const blade = buildBlade(context, id + "blade", bladeDef);
            if (blade.sectionsOnly)
                return;
            const D = blade.D;
            const R = blade.R;
            const Z = definition.bladeCount;
            const zAxis = line(vector(0, 0, 0) * millimeter, vector(0, 0, 1));

            // ---- 3. The hub: revolve a profile in the XZ plane about Z ----
            propPrint(context, "propLog", "[prop] blade built; hub");
            const hubId = id + "hub";
            var pts = [vector(0 * millimeter, zBot), vector(Rd, zBot), vector(Ru, zTop), vector(0 * millimeter, zTop)];
            if (definition.noseLength > 0 * millimeter)
                pts = [vector(0 * millimeter, zBot), vector(Rd, zBot), vector(Ru, zTop), vector(Ru / 5, zTop + definition.noseLength), vector(0 * millimeter, zTop + definition.noseLength)];
            const tailLen = (definition.tailStyle == TailStyle.CONE) ? definition.tailLength : 0 * millimeter;
            if (tailLen > 0 * millimeter)
                pts = concatenateArrays([[vector(0 * millimeter, zBot - tailLen), vector(Rd / 5, zBot - tailLen)], subArray(pts, 1, size(pts))]);
            const sk = newSketchOnPlane(context, hubId + "sketch", { "sketchPlane" : plane(vector(0, 0, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0)) });
            // sketch x -> world X (radius), sketch y -> world Z (axial): plane(origin, normal -Y, xAxis +X) gives y = +Z
            for (var k = 0; k < size(pts); k += 1)
            {
                const p0 = pts[k];
                const p1 = pts[(k + 1) % size(pts)];
                skLineSegment(sk, "seg" ~ k, { "start" : p0, "end" : p1 });
            }
            skSolve(sk);
            opRevolve(context, hubId + "revolve", {
                        "entities" : qSketchRegion(hubId + "sketch"),
                        "axis" : zAxis,
                        "angleForward" : 360 * degree
                    });
            const hubBody = qCreatedBy(hubId + "revolve", EntityType.BODY);
            opDeleteBodies(context, hubId + "dropSketch", { "entities" : qCreatedBy(hubId + "sketch", EntityType.BODY) });

            const folding = (definition.hubType == HubType.FOLDING);
            const mt = definition.mountType;
            // ---- the printed hub core for a laminated propeller (build 86): the hub inset by the skin ----
            var coreOn = false;
            var coreBody = qNothing();
            if (!folding && definition.hubCore == true)
            {
                const t = definition.coreSkin;
                const cid = id + "core";
                const cpts = [vector(0 * millimeter, zBot + t), vector(Rd - t, zBot + t), vector(Ru - t, zTop - t), vector(0 * millimeter, zTop - t)];
                const csk = newSketchOnPlane(context, cid + "sketch", { "sketchPlane" : plane(vector(0, 0, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0)) });
                for (var k = 0; k < size(cpts); k += 1)
                    skLineSegment(csk, "seg" ~ k, { "start" : cpts[k], "end" : cpts[(k + 1) % size(cpts)] });
                skSolve(csk);
                opRevolve(context, cid + "revolve", { "entities" : qSketchRegion(cid + "sketch"), "axis" : zAxis, "angleForward" : 360 * degree });
                opDeleteBodies(context, cid + "dropSketch", { "entities" : qCreatedBy(cid + "sketch", EntityType.BODY) });
                coreBody = qCreatedBy(cid + "revolve", EntityType.BODY);
                coreOn = true;
                propPrint(context, "propLog", "[prop] hub core: the hub inset " ~ toMm(t) ~ " mm");
            }
            var propBody = hubBody;
            var bladeBodies = blade.blade;
            var foldInfo = {};
            if (!folding)
            {
                // ---- 4. Trim the blade root where it would stick out of the hub's end faces ----
                propPrint(context, "propLog", "[prop] hub revolved; trim");
                // The blade over-extends inboard to 0.2R; inside the hub that vanishes in the union, but
                // beyond the end faces it would poke out. Cut the blade with a short cylinder past each face.
                const trimLen = 0.6 * D;
                fCylinder(context, id + "trimUp", { "topCenter" : vector(0, 0, 1) * (zTop + trimLen), "bottomCenter" : vector(0, 0, 1) * zTop + vector(0, 0, 0.001) * millimeter, "radius" : Ru + 0.5 * millimeter });
                fCylinder(context, id + "trimDown", { "topCenter" : vector(0, 0, 1) * zBot - vector(0, 0, 0.001) * millimeter, "bottomCenter" : vector(0, 0, 1) * (zBot - trimLen), "radius" : Rd + 0.5 * millimeter });
                try silent
                {
                    opBoolean(context, id + "trim", {
                                "tools" : qUnion([qCreatedBy(id + "trimUp", EntityType.BODY), qCreatedBy(id + "trimDown", EntityType.BODY)]),
                                "targets" : blade.blade,
                                "operationType" : BooleanOperationType.SUBTRACTION
                            });
                }
                try silent
                {
                    opDeleteBodies(context, id + "dropTrims", { "entities" : qUnion([qCreatedBy(id + "trimUp", EntityType.BODY), qCreatedBy(id + "trimDown", EntityType.BODY)]) });
                }

                // ---- 5. Pattern the blade and join everything ----
                propPrint(context, "propLog", "[prop] trimmed; pattern and union");
                var transforms = [];
                var names = [];
                for (var k = 1; k < Z; k += 1)
                {
                    transforms = append(transforms, rotationAround(zAxis, k * 360 / Z * degree));
                    names = append(names, "blade" ~ (k + 1));
                }
                var bladeSet = blade.blade;
                if (Z > 1)
                {
                    opPattern(context, id + "pattern", { "entities" : blade.blade, "transforms" : transforms, "instanceNames" : names });
                    bladeSet = qUnion([blade.blade, qCreatedBy(id + "pattern", EntityType.BODY)]);
                }
                opBoolean(context, id + "union", {
                            "tools" : qUnion([hubBody, bladeSet]),
                            "operationType" : BooleanOperationType.UNION
                        });
                propBody = qBodyType(qUnion([hubBody, bladeSet]), BodyType.SOLID);

                // ---- 6. The root fillet, tried and reported ----
                propPrint(context, "propLog", "[prop] union ok; fillet");
                if (definition.rootFillet > 0 * millimeter)
                {
                    const rootEdges = qCreatedBy(id + "union", EntityType.EDGE);
                    var filleted = false;
                    try silent
                    {
                        opFillet(context, id + "rootFillet", { "entities" : rootEdges, "radius" : definition.rootFillet });
                        filleted = true;
                    }
                    if (!filleted)
                    {
                        const note = "The root fillet failed at " ~ toMm(definition.rootFillet) ~ " mm (grazing angles at this skew or rake): the propeller is built without it. Try a smaller radius, or add it by hand.";
                        propPrint(context, "propLog", "[prop] WARNING: " ~ note);
                        propPrint(context, "propNotice", note);
                        reportFeatureWarning(context, id, note);
                    }
                    else
                        propPrint(context, "propLog", "[prop] root fillet " ~ toMm(definition.rootFillet) ~ " mm ok");
                }
            }
            else
            {
                // ---- 4f. The folder: boss and tongue on the blade, slot, pin holes and pins in the hub ----
                propPrint(context, "propLog", "[prop] hub revolved; folding root");
                const rPin = definition.pinRadius;
                const c = definition.foldClear;
                const bossR = definition.bossDia / 2;
                const bossW = definition.bossWidth;
                const hR = bossR + c;                                   // slot half-height around the pin
                const w = bossW + 2 * c;                                // slot width along the pin
                const rTrim = rBlade + c;                               // the blade starts just clear of the hub
                // the blade's generator line sits at z = -r tan(rake): put the pin (and the tongue) on it at the root
                const zRoot = -rTrim * tan(definition.rake);
                const zPin = zRoot + definition.pinAxial;
                const reach = Ru + 10 * millimeter;                     // how far the slot and pin tools extend, past the hub
                const rHubPin = Rd + (Ru - Rd) * (zPin - zBot) / (zTop - zBot);   // hub radius at the pin's axial station
                if (zPin < zBot || zPin > zTop)
                    throw regenError("The pin at z " ~ toMm(zPin) ~ " (the root's generator line " ~ toMm(zRoot) ~ " plus Pin axial) lies outside the hub (" ~ toMm(zBot) ~ " to " ~ toMm(zTop) ~ " mm)", ["pinAxial", "hubLength", "hubShift", "rake"]);
                if (rPin + hR >= rHubPin)
                    throw regenError("Pin radius " ~ toMm(rPin) ~ " mm plus the boss and clearance (" ~ toMm(hR) ~ ") reaches past the hub surface (" ~ toMm(rHubPin) ~ " mm at the pin)", ["pinRadius", "bossDia", "hubUpstream"]);
                // the blade's root section at the trim radius, from the B-series tables: what the fold channel must clear
                const rAero = rTrim + definition.rootFairing;            // the aerofoil proper starts here; the fairing runs from here to the hub surface
                const cRoot = chordFactorAt(rAero / R, Z) * D * definition.ear / Z;
                const betaRoot = atan(definition.pdRatio * D / (2 * PI * rAero));
                const tRoot = definition.rootThickness;
                // the root section about its generator line: the trailing edge lies aft (0.4 c), the leading edge forward (0.6 c)
                // the section sits about its generator line; the pin is Pin axial forward of that line, so a pin further
                // forward has more of the section aft of it (inboard when folded) and less forward (outboard when folded)
                const hAft0 = 0.4 * cRoot * sin(betaRoot) + 0.5 * tRoot * cos(betaRoot) + definition.pinAxial + c + 1 * millimeter;
                const hFwd0 = 0.62 * cRoot * sin(betaRoot) + 0.5 * tRoot * cos(betaRoot) - definition.pinAxial + c + 1 * millimeter;
                var hAft = max(c + 1 * millimeter, hAft0);              // aft of the pin ray: inboard when folded
                const earWhy = " [EAR " ~ definition.ear ~ " over " ~ Z ~ " blades puts " ~ round(100 * definition.ear / Z) ~ " % of the disc on each blade: a " ~ toMm(cRoot) ~ " mm root chord at " ~ toDeg(betaRoot) ~ " deg, reaching " ~ toMm(hAft0) ~ " mm aft of the pin, which swings in toward the shaft when the blade folds]";
                const hFwd = max(c + 1 * millimeter, hFwd0);            // forward of the pin ray: outboard when folded
                // the shaft tube: the bore plus Tube wall, kept solid through the gap and the relief; the blade is trimmed round it
                const boreHere = (mt == MountType.BOLTED) ? definition.pilotDia : ((mt == MountType.NONE) ? 0 * millimeter : definition.boreDia);
                const rTube = (definition.tubeWall > 0 * millimeter && boreHere > 0 * millimeter) ? boreHere / 2 + definition.tubeWall : 0 * millimeter;
                if (definition.tubeWall > 0 * millimeter && boreHere <= 0 * millimeter)
                    propPrint(context, "propLog", "[prop] NOTE: Tube wall is set but the mount has no bore, so there is no shaft tube to keep");
                if (rTube > 0 * millimeter && hAft > rPin - 2 * millimeter)
                    hAft = rPin - 2 * millimeter;                           // the relief's core can shrink to nothing: the tube fills it
                const pinC = vector(0 * millimeter, rPin, zPin);
                const pinAxisAft = line(pinC, vector(-1, 0, 0));        // +angle about -X swings +Y (the blade) toward -Z (aft)
                const xExit = sqrt(rHubPin ^ 2 - rPin ^ 2);             // where the pin line leaves the hub surface, each side
                var holeHub = definition.pinDia + definition.hubFit;
                var holeBlade = definition.pinDia + definition.bladeFit;
                var pinName = toMm(definition.pinDia) ~ " mm straight pin";
                var bd = {};
                if (definition.pinType == PinType.BOLT)
                {
                    if (definition.pinBolt == BoltSize.CUSTOM)
                    {
                        holeHub = definition.pinDia;
                        holeBlade = definition.pinDia;
                        pinName = "custom bolt, " ~ toMm(definition.pinDia) ~ " mm holes";
                    }
                    else
                    {
                        bd = boltDims(definition.pinBolt);
                        holeHub = bd.hole;
                        holeBlade = bd.hole;
                        pinName = bd.name ~ " cap-head bolt and captive nut";
                    }
                }
                const gapW = max(definition.gapWidth, w);               // the opening between cheeks, never narrower than the tongue slot
                const zGapTop = zPin + hR;                              // the gap's forward wall: the stop
                const rGapFloor = rPin - hR;                            // the gap's floor, clear of the boss
                const cheek = 2 * PI * rPin / Z - gapW;                 // arc of cheek wall between gaps at the pin radius
                const tongueLen = rTrim - rPin;
                propPrint(context, "propLog", "[prop] folder: pin at r " ~ toMm(rPin) ~ ", z " ~ toMm(zPin) ~ " (root generator line z " ~ toMm(zRoot) ~ "; hub r " ~ toMm(rHubPin) ~ " there, pin " ~ toMm(2 * xExit)
                    ~ " mm long); boss " ~ toMm(definition.bossDia) ~ " x " ~ toMm(bossW) ~ ", gap " ~ toMm(gapW) ~ " wide from r " ~ toMm(rGapFloor) ~ " to the surface and from the tail to z " ~ toMm(zGapTop) ~ " (the stop); cheek " ~ toMm(cheek) ~ " mm between gaps; " ~ pinName ~ "; fold " ~ toDeg(definition.foldAngle) ~ " deg aft");
                if (cheek < 3 * millimeter)
                {
                    const note = "The cheeks between the gaps are only " ~ toMm(cheek) ~ " mm at the pin radius: narrow the gap, widen the hub, or move the pin out.";
                    propPrint(context, "propLog", "[prop] WARNING: " ~ note);
                    propPrint(context, "propNotice", note);
                    reportFeatureWarning(context, id, note);
                }
                if (rTube > 0 * millimeter && rTube + c >= rPin - holeBlade / 2)
                    throw regenError("The shaft tube (bore " ~ toMm(boreHere) ~ " plus " ~ toMm(definition.tubeWall) ~ " wall, " ~ toMm(rTube) ~ " mm radius) reaches the pin holes at r " ~ toMm(rPin - holeBlade / 2) ~ ": a thinner wall, a smaller bore, or pins further out", ["tubeWall", "pinRadius", "boreDia"]);
                if (rGapFloor < ((rTube > 0 * millimeter) ? 0 * millimeter : 2 * millimeter))
                    throw regenError("The gap floor (pin radius less the boss radius and clearance) reaches the shaft axis: move the pin out or shrink the boss" ~ ((rTube > 0 * millimeter) ? "" : ", or set a Tube wall so the shaft tube stays and the boss is shaped round it"), ["pinRadius", "bossDia", "tubeWall"]);
                propPrint(context, "propLog", "[prop] root section where the aerofoil starts (r " ~ toMm(rAero) ~ "): chord " ~ toMm(cRoot) ~ " at " ~ toDeg(betaRoot) ~ " deg, " ~ toMm(tRoot) ~ " thick; " ~ toMm(hAft) ~ " aft / " ~ toMm(hFwd) ~ " forward of the pin (Pin axial " ~ toMm(definition.pinAxial) ~ "); the hub is kept whole and trims the blade" ~ ((rTube > 0 * millimeter) ? "; shaft tube " ~ toMm(2 * rTube) ~ " mm dia kept (bore " ~ toMm(boreHere) ~ " + " ~ toMm(definition.tubeWall) ~ " wall)" : ""));

                // ---- the hub's per-blade tools (blade 1, azimuth 0): the gap between cheeks and the pin bore ----
                fCuboid(context, id + "gap", { "corner1" : vector(-gapW / 2, max(rGapFloor, 0 * millimeter), zBot - tailLen - 1 * millimeter), "corner2" : vector(gapW / 2, Ru + 5 * millimeter, zGapTop) });
                fCylinder(context, id + "hubBore", { "topCenter" : pinC + vector(reach, 0 * millimeter, 0 * millimeter), "bottomCenter" : pinC - vector(reach, 0 * millimeter, 0 * millimeter), "radius" : holeHub / 2 });
                var toolIds = [id + "gap", id + "hubBore"];
                const rho0 = tongueLen - c;                             // the shoulder: the flat-cut root and the fairing swing just clear of it
                const rCore = rPin - hAft;
                if (definition.tailRelief)
                {
                    if (rCore < 2 * millimeter)
                        throw regenError("The tail relief's core behind the pins (pin radius less the root's reach aft of the pin) reaches the shaft axis: move the pin outward or aft (Pin axial), set a Tube wall, lower EAR, or turn Tail relief off." ~ earWhy, ["pinRadius", "pinAxial", "tubeWall", "ear", "tailRelief"]);
                    tailReliefBody(context, id + "relief", rPin, zPin, rho0, hAft, hFwd, reach, definition.foldAngle, zAxis);
                    propPrint(context, "propLog", "[prop] tail relief: shoulder " ~ toMm(rho0) ~ " about the pin circle, core " ~ toMm(2 * rCore) ~ " mm dia behind the pins" ~ ((rTube > 0 * millimeter && rCore < rTube) ? " - inside the shaft tube, which fills it; the folded root is trimmed round the tube" : ""));
                    if (rTube <= 0 * millimeter && rCore < boreHere / 2 + 3 * millimeter)
                    {
                        const note = "Behind the pins the tail relief leaves a " ~ toMm(2 * rCore) ~ " mm core over a " ~ toMm(boreHere) ~ " mm bore: under 3 mm of wall. Move the pin aft (Pin axial) or outward, set a Tube wall so the shaft tube stays and the root is trimmed round it, reduce the root chord (EAR), or turn Tail relief off." ~ earWhy;
                        propPrint(context, "propLog", "[prop] WARNING: " ~ note);
                        propPrint(context, "propNotice", note);
                        reportFeatureWarning(context, id, note);
                    }
                }
                if (rTube > 0 * millimeter)
                {
                    // the shaft tube stays: take it out of the gap and the relief before they cut the hub
                    fCylinder(context, id + "tube", { "topCenter" : vector(0 * millimeter, 0 * millimeter, zTop + 2 * millimeter), "bottomCenter" : vector(0 * millimeter, 0 * millimeter, zBot - tailLen - 3 * millimeter), "radius" : rTube });
                    var tubeTargets = [qCreatedBy(id + "gap", EntityType.BODY)];
                    if (definition.tailRelief)
                        tubeTargets = append(tubeTargets, qCreatedBy(id + "relief" + "revolve", EntityType.BODY));
                    var tubeKept = false;
                    try silent
                    {
                        opBoolean(context, id + "tubeKeep", { "tools" : qCreatedBy(id + "tube", EntityType.BODY), "targets" : qUnion(tubeTargets), "operationType" : BooleanOperationType.SUBTRACTION });
                        tubeKept = true;
                    }
                    if (!tubeKept)
                    {
                        opDeleteBodies(context, id + "tubeDrop", { "entities" : qCreatedBy(id + "tube", EntityType.BODY) });
                        propPrint(context, "propLog", "[prop] the gap and relief clear the shaft tube already (nothing to keep)");
                    }
                    else
                        propPrint(context, "propLog", "[prop] shaft tube " ~ toMm(2 * rTube) ~ " mm dia kept through the gap" ~ (definition.tailRelief ? " and the relief" : ""));
                }
                if (definition.pinType == PinType.BOLT && definition.pinBolt != BoltSize.CUSTOM)
                {
                    // head counterbore on the +X cheek, hex nut pocket on the -X cheek, both from the hub surface inward
                    fCylinder(context, id + "headCb", { "topCenter" : pinC + vector(reach, 0 * millimeter, 0 * millimeter), "bottomCenter" : pinC + vector(xExit - bd.headDepth, 0 * millimeter, 0 * millimeter), "radius" : bd.head / 2 });
                    const nutSk = newSketchOnPlane(context, id + "nutSketch", { "sketchPlane" : plane(pinC - vector(xExit - bd.nutDepth, 0 * millimeter, 0 * millimeter), vector(-1, 0, 0), vector(0, 1, 0)) });
                    skRegularPolygon(nutSk, "hex", { "center" : vector(0, 0) * millimeter, "firstVertex" : vector(bd.nutFlats / 2 / cos(30 * degree), 0 * millimeter), "sides" : 6 });
                    skSolve(nutSk);
                    opExtrude(context, id + "nutPocket", { "entities" : qSketchRegion(id + "nutSketch"), "direction" : vector(-1, 0, 0), "endBound" : BoundingType.BLIND, "endDepth" : reach });
                    opDeleteBodies(context, id + "dropNutSketch", { "entities" : qCreatedBy(id + "nutSketch", EntityType.BODY) });
                    toolIds = concatenateArrays([toolIds, [id + "headCb", id + "nutPocket"]]);
                    propPrint(context, "propLog", "[prop] " ~ bd.name ~ ": " ~ toMm(bd.hole) ~ " mm holes, head counterbore " ~ toMm(bd.head) ~ " x " ~ toMm(bd.headDepth) ~ " on the +X cheek, nut pocket " ~ toMm(bd.nutFlats) ~ " AF x " ~ toMm(bd.nutDepth) ~ " on the -X cheek");
                }
                var toolQs = [];
                for (var t in toolIds)
                {
                    toolQs = append(toolQs, qCreatedBy(t, EntityType.BODY));
                }

                // ---- the hub's envelope: the hub as it is, plus clearance, run straight through any tail cap ----
                const envId = id + "hubEnv";
                const envPts = [vector(0 * millimeter, zBot - tailLen - c), vector(Rd + c, zBot - tailLen - c), vector(Rd + c, zBot), vector(Ru + c, zTop), vector(Ru + c, zTop + c), vector(0 * millimeter, zTop + c)];
                const esk = newSketchOnPlane(context, envId + "sketch", { "sketchPlane" : plane(vector(0, 0, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0)) });
                for (var k = 0; k < size(envPts); k += 1)
                {
                    skLineSegment(esk, "e" ~ k, { "start" : envPts[k], "end" : envPts[(k + 1) % size(envPts)] });
                }
                skSolve(esk);
                opRevolve(context, envId + "revolve", { "entities" : qSketchRegion(envId + "sketch"), "axis" : zAxis, "angleForward" : 360 * degree });
                opDeleteBodies(context, envId + "dropSketch", { "entities" : qCreatedBy(envId + "sketch", EntityType.BODY) });
                // the same gap, less the clearance each side, so the blade keeps its clearance to the cheeks and the stop
                fCuboid(context, envId + "gap", { "corner1" : vector(-gapW / 2 + c, max(rGapFloor, 0 * millimeter) + c, zBot - tailLen - 2 * millimeter), "corner2" : vector(gapW / 2 - c, Ru + 6 * millimeter, zGapTop - c) });
                var envTools = [qCreatedBy(envId + "gap", EntityType.BODY)];
                var envRelief = qNothing();
                if (definition.tailRelief)
                {
                    // the same relief, a clearance SMALLER (the envelope is the hub grown by the clearance, so it loses less to
                    // the relief), so the blade keeps its clearance to the shoulder and the core
                    envRelief = tailReliefBody(context, envId + "relief", rPin, zPin, rho0 + c, hAft - c, hFwd - c, reach, definition.foldAngle, zAxis);
                    envTools = append(envTools, envRelief);
                }
                if (rTube > 0 * millimeter)
                {
                    // the envelope keeps the shaft tube grown by the clearance, as the hub keeps the tube
                    fCylinder(context, envId + "tube", { "topCenter" : vector(0 * millimeter, 0 * millimeter, zTop + 2 * millimeter), "bottomCenter" : vector(0 * millimeter, 0 * millimeter, zBot - tailLen - 3 * millimeter), "radius" : rTube + c });
                    var envTubeKept = false;
                    try silent
                    {
                        opBoolean(context, envId + "tubeKeep", { "tools" : qCreatedBy(envId + "tube", EntityType.BODY), "targets" : qUnion(envTools), "operationType" : BooleanOperationType.SUBTRACTION });
                        envTubeKept = true;
                    }
                    if (!envTubeKept)
                        opDeleteBodies(context, envId + "tubeDrop", { "entities" : qCreatedBy(envId + "tube", EntityType.BODY) });
                }
                opBoolean(context, envId + "cutGap", { "tools" : qCreatedBy(envId + "gap", EntityType.BODY), "targets" : qCreatedBy(envId + "revolve", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
                if (definition.tailRelief)
                    opBoolean(context, envId + "cutRelief", { "tools" : envRelief, "targets" : qUnion([qCreatedBy(envId + "revolve", EntityType.BODY), qCreatedBy(envId + "cutGap", EntityType.BODY)]), "operationType" : BooleanOperationType.SUBTRACTION });
                const envBody = qLargest(qBodyType(qUnion([qCreatedBy(envId + "revolve", EntityType.BODY), qCreatedBy(envId + "cutGap", EntityType.BODY), qCreatedBy(envId + "cutRelief", EntityType.BODY)]), BodyType.SOLID));
                propPrint(context, "propLog", "[prop] hub envelope with its gap made (" ~ size(evaluateQuery(context, envBody)) ~ " body); aerofoil and fairing");

                // ---- the blade: aerofoil cut flat outside the hub, fairing lofted to the tongue's section at the surface ----
                fCuboid(context, id + "rootTrim", { "corner1" : vector(-D, -D, -D), "corner2" : vector(D, rAero, D) });
                opBoolean(context, id + "trimRoot", { "tools" : qCreatedBy(id + "rootTrim", EntityType.BODY), "targets" : blade.blade, "operationType" : BooleanOperationType.SUBTRACTION });
                var aeroParts = [blade.blade];
                if (definition.rootFairing > 0 * millimeter)
                {
                    const rootFace = qCoincidesWithPlane(qOwnedByBody(blade.blade, EntityType.FACE), plane(vector(0 * millimeter, rAero, 0 * millimeter), vector(0, 1, 0)));
                    const fairSk = newSketchOnPlane(context, id + "fairSketch", { "sketchPlane" : plane(vector(0 * millimeter, rPin + bossR, 0 * millimeter), vector(0, 1, 0), vector(0, 0, 1)) });
                    // sketch x = world Z, sketch y = world X
                    skRectangle(fairSk, "rect", { "firstCorner" : vector(zPin - bossR, -bossW / 2), "secondCorner" : vector(zPin + bossR, bossW / 2) });
                    skSolve(fairSk);
                    var faired = false;
                    try silent
                    {
                        opLoft(context, id + "fairing", { "profileSubqueries" : [rootFace, qSketchRegion(id + "fairSketch")] });
                        faired = size(evaluateQuery(context, qBodyType(qCreatedBy(id + "fairing", EntityType.BODY), BodyType.SOLID))) > 0;
                    }
                    opDeleteBodies(context, id + "dropFairSketch", { "entities" : qCreatedBy(id + "fairSketch", EntityType.BODY) });
                    if (faired)
                    {
                        aeroParts = append(aeroParts, qCreatedBy(id + "fairing", EntityType.BODY));
                        propPrint(context, "propLog", "[prop] root fairing: the aerofoil at r " ~ toMm(rAero) ~ " lofted down the gap to the " ~ toMm(bossW) ~ " x " ~ toMm(definition.bossDia) ~ " boss at r " ~ toMm(rPin + bossR));
                    }
                    else
                    {
                        const note = "The root fairing loft failed (aerofoil at r " ~ toMm(rAero) ~ " to the tongue at the hub surface): the blade is built with the bare tongue. Try a longer Root fairing or a wider boss.";
                        propPrint(context, "propLog", "[prop] WARNING: " ~ note);
                        propPrint(context, "propNotice", note);
                        reportFeatureWarning(context, id, note);
                    }
                }
                if (size(aeroParts) > 1)
                    opBoolean(context, id + "aeroUnion", { "tools" : qUnion(aeroParts), "operationType" : BooleanOperationType.UNION });
                const aeroBody = qBodyType(qUnion(aeroParts), BodyType.SOLID);
                propPrint(context, "propLog", "[prop] aerofoil and fairing joined (" ~ size(evaluateQuery(context, aeroBody)) ~ " body); the hub trims the blade");

                // ---- the hub trims the blade: deployed, so the blade's hub part fits the hub's external profile, and at
                // 6 deg steps through the fold (the blade swung about its pin, the envelope subtracted, the blade swung back) ----
                const volBefore = evVolume(context, { "entities" : aeroBody });
                {
                    const envSteps = max(5, ceil(definition.foldAngle / (6 * degree)));
                    const step = definition.foldAngle / envSteps;
                    var trimFails = 0;
                    var failedAt = "";
                    for (var k = 0; k <= envSteps; k += 1)
                    {
                        if (k > 0)
                            opTransform(context, id + ("swing" ~ k), { "bodies" : aeroBody, "transform" : rotationAround(pinAxisAft, step) });
                        var done = false;
                        var why = "";
                        try
                        {
                            opBoolean(context, id + ("trim" ~ k), { "tools" : envBody, "targets" : aeroBody, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
                            done = true;
                        }
                        catch (error)
                        {
                            why = toString(error);
                        }
                        if (!done)
                        {
                            // a face of the root landing exactly on a face of the envelope at this angle fails the boolean:
                            // nudge the blade a third of a degree on, cut, and nudge it back
                            const nudge = 0.37 * degree;
                            opTransform(context, id + ("nudgeOn" ~ k), { "bodies" : aeroBody, "transform" : rotationAround(pinAxisAft, nudge) });
                            try silent
                            {
                                opBoolean(context, id + ("trimNudged" ~ k), { "tools" : envBody, "targets" : aeroBody, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
                                done = true;
                            }
                            opTransform(context, id + ("nudgeBack" ~ k), { "bodies" : aeroBody, "transform" : rotationAround(pinAxisAft, -nudge) });
                        }
                        if (!done)
                        {
                            trimFails += 1;
                            failedAt = failedAt ~ ((failedAt == "") ? "" : ", ") ~ toDeg(step * k) ~ " deg (" ~ why ~ ")";
                        }
                    }
                    opTransform(context, id + "swingBack", { "bodies" : aeroBody, "transform" : rotationAround(pinAxisAft, -definition.foldAngle) });
                    opDeleteBodies(context, id + "envDrop", { "entities" : envBody });
                    propPrint(context, "propLog", "[prop] trimmed against the hub deployed and through " ~ envSteps ~ " fold positions");
                    if (trimFails > 0)
                    {
                        const note = "The fold trim failed at " ~ trimFails ~ " of " ~ (envSteps + 1) ~ " positions (" ~ failedAt ~ "): check the folded blade against the hub by eye.";
                        propPrint(context, "propLog", "[prop] WARNING: " ~ note);
                        propPrint(context, "propNotice", note);
                        reportFeatureWarning(context, id, note);
                    }
                }
                const volAfter = evVolume(context, { "entities" : aeroBody });
                propPrint(context, "propLog", "[prop] the hub took " ~ round((volBefore - volAfter) / millimeter ^ 3) ~ " mm^3 off the blade's root (" ~ round(100 * (volBefore - volAfter) / volBefore) ~ " % of the blade)");
                if (size(evaluateQuery(context, aeroBody)) != 1)
                    throw regenError("Trimming the blade to the hub through the fold split it into " ~ size(evaluateQuery(context, aeroBody)) ~ " pieces: shorten the hub behind the pin, move the pin aft (Pin axial), lower EAR, or reduce the fold angle." ~ earWhy, ["hubLength", "pinAxial", "ear", "foldAngle"]);
                fCylinder(context, id + "boss", { "topCenter" : pinC + vector(bossW / 2, 0 * millimeter, 0 * millimeter), "bottomCenter" : pinC - vector(bossW / 2, 0 * millimeter, 0 * millimeter), "radius" : bossR });
                fCuboid(context, id + "tongue", { "corner1" : vector(-bossW / 2, rPin, zPin - bossR), "corner2" : vector(bossW / 2, (definition.rootFairing > 0 * millimeter) ? rPin + bossR + 2 * millimeter : rTrim + 3 * millimeter, zPin + bossR) });
                const rootParts = [aeroBody, qCreatedBy(id + "boss", EntityType.BODY), qCreatedBy(id + "tongue", EntityType.BODY)];
                propPrint(context, "propLog", "[prop] boss and tongue made; union");
                opBoolean(context, id + "bladeUnion", { "tools" : qUnion(rootParts), "operationType" : BooleanOperationType.UNION });
                const bladeBody = qBodyType(qUnion(rootParts), BodyType.SOLID);
                fCylinder(context, id + "bladeBore", { "topCenter" : pinC + vector(reach, 0 * millimeter, 0 * millimeter), "bottomCenter" : pinC - vector(reach, 0 * millimeter, 0 * millimeter), "radius" : holeBlade / 2 });
                opBoolean(context, id + "cutBladeBore", { "tools" : qCreatedBy(id + "bladeBore", EntityType.BODY), "targets" : bladeBody, "operationType" : BooleanOperationType.SUBTRACTION });
                propPrint(context, "propLog", "[prop] boss and tongue joined, bore " ~ toMm(holeBlade) ~ " mm");
                if (rTube > 0 * millimeter)
                {
                    // the blade - boss, tongue, fairing and root - is shaped round the shaft tube plus clearance in every
                    // position through the fold: the boss's inside edge deployed, the tongue's inner face folded, and the
                    // root's aft corner on the way
                    fCylinder(context, id + "bladeTube", { "topCenter" : vector(0 * millimeter, 0 * millimeter, zTop + 2 * millimeter), "bottomCenter" : vector(0 * millimeter, 0 * millimeter, zBot - tailLen - 3 * millimeter), "radius" : rTube + c });
                    const tubeTool = qCreatedBy(id + "bladeTube", EntityType.BODY);
                    const vTubeBefore = evVolume(context, { "entities" : bladeBody });
                    const tubeSteps = max(5, ceil(definition.foldAngle / (6 * degree)));
                    const tubeStep = definition.foldAngle / tubeSteps;
                    var tubeFails = 0;
                    for (var k = 0; k <= tubeSteps; k += 1)
                    {
                        if (k > 0)
                            opTransform(context, id + ("tubeSwing" ~ k), { "bodies" : bladeBody, "transform" : rotationAround(pinAxisAft, tubeStep) });
                        var done = false;
                        try silent
                        {
                            opBoolean(context, id + ("tubeTrim" ~ k), { "tools" : tubeTool, "targets" : bladeBody, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
                            done = true;
                        }
                        if (!done)
                        {
                            const nudge = 0.37 * degree;
                            opTransform(context, id + ("tubeNudgeOn" ~ k), { "bodies" : bladeBody, "transform" : rotationAround(pinAxisAft, nudge) });
                            try silent
                            {
                                opBoolean(context, id + ("tubeTrimNudged" ~ k), { "tools" : tubeTool, "targets" : bladeBody, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
                                done = true;
                            }
                            opTransform(context, id + ("tubeNudgeBack" ~ k), { "bodies" : bladeBody, "transform" : rotationAround(pinAxisAft, -nudge) });
                        }
                        if (!done)
                            tubeFails += 1;
                    }
                    opTransform(context, id + "tubeSwingBack", { "bodies" : bladeBody, "transform" : rotationAround(pinAxisAft, -definition.foldAngle) });
                    opDeleteBodies(context, id + "bladeTubeDrop", { "entities" : tubeTool });
                    const vTubeAfter = evVolume(context, { "entities" : bladeBody });
                    propPrint(context, "propLog", "[prop] blade shaped round the shaft tube (" ~ toMm(2 * (rTube + c)) ~ " mm dia with clearance) through " ~ tubeSteps ~ " fold positions: " ~ round((vTubeBefore - vTubeAfter) / millimeter ^ 3) ~ " mm^3 off the boss, tongue and root" ~ ((tubeFails > 0) ? " (" ~ tubeFails ~ " positions failed to cut)" : ""));
                    if (size(evaluateQuery(context, bladeBody)) != 1)
                        throw regenError("Shaping the blade round the shaft tube split it into " ~ size(evaluateQuery(context, bladeBody)) ~ " pieces: a thinner Tube wall, a smaller bore, or pins further out", ["tubeWall", "boreDia", "pinRadius"]);
                }

                var pinBodies = qNothing();
                if (definition.pinType == PinType.PIN && definition.makePins)
                {
                    fCylinder(context, id + "pin", { "topCenter" : pinC + vector(xExit, 0 * millimeter, 0 * millimeter), "bottomCenter" : pinC - vector(xExit, 0 * millimeter, 0 * millimeter), "radius" : definition.pinDia / 2 });
                    pinBodies = qCreatedBy(id + "pin", EntityType.BODY);
                }

                // ---- pattern the blade, the pin and the hub tools about the shaft, cut the hub ----
                propPrint(context, "propLog", "[prop] gap and bore tools made; pattern and cut");
                var transforms = [];
                var names = [];
                for (var k = 1; k < Z; k += 1)
                {
                    transforms = append(transforms, rotationAround(zAxis, k * 360 / Z * degree));
                    names = append(names, "b" ~ (k + 1));
                }
                var allTools = qUnion(toolQs);
                bladeBodies = bladeBody;
                if (Z > 1)
                {
                    opPattern(context, id + "bladePattern", { "entities" : bladeBody, "transforms" : transforms, "instanceNames" : names });
                    bladeBodies = qUnion([bladeBody, qCreatedBy(id + "bladePattern", EntityType.BODY)]);
                    opPattern(context, id + "toolPattern", { "entities" : qUnion(toolQs), "transforms" : transforms, "instanceNames" : names });
                    allTools = qUnion([qUnion(toolQs), qCreatedBy(id + "toolPattern", EntityType.BODY)]);
                    if (definition.pinType == PinType.PIN && definition.makePins)
                    {
                        opPattern(context, id + "pinPattern", { "entities" : pinBodies, "transforms" : transforms, "instanceNames" : names });
                        pinBodies = qUnion([pinBodies, qCreatedBy(id + "pinPattern", EntityType.BODY)]);
                    }
                }
                opBoolean(context, id + "cutSlots", { "tools" : allTools, "targets" : hubBody, "operationType" : BooleanOperationType.SUBTRACTION });
                if (definition.tailRelief)
                    opBoolean(context, id + "cutRelief", { "tools" : qCreatedBy(id + "relief" + "revolve", EntityType.BODY), "targets" : hubBody, "operationType" : BooleanOperationType.SUBTRACTION });
                // a swept slot can leave slivers of the hub as separate bodies: keep the hub, drop them
                const hubPieces = qBodyType(qUnion([hubBody, qCreatedBy(id + "cutSlots", EntityType.BODY), qCreatedBy(id + "cutRelief", EntityType.BODY)]), BodyType.SOLID);
                const hubMain = qLargest(hubPieces);
                const nHub = size(evaluateQuery(context, hubPieces));
                if (nHub > 1)
                    opDeleteBodies(context, id + "dropHubSlivers", { "entities" : qSubtraction(hubPieces, hubMain) });
                propBody = hubMain;
                foldInfo = { "rPin" : rPin, "zPin" : zPin, "tongueLen" : tongueLen, "bossW" : bossW, "pinBodies" : pinBodies, "pinAxisAft" : pinAxisAft };
                bladeDef.foldPinRadius = rPin;
                bladeDef.foldTongue = tongueLen;
                bladeDef.foldBossWidth = bossW;
                propPrint(context, "propLog", "[prop] hub gapped and bored; " ~ Z ~ " blades on their pins");
            }

            // ---- 6b. The tail: dish and pocket, one revolved cut ----
            propPrint(context, "propLog", "[prop] fillet stage done; tail");
            const zLo = zBot - tailLen - 1 * millimeter;      // just past the downstream end (cone cap included)
            const zHi = zTop + definition.noseLength + 1 * millimeter;
            {
                const pocketR = definition.pocketDia / 2;
                const zFloorWanted = zTop - ((definition.pocketDia > 0 * millimeter) ? definition.pocketFloor : 0 * millimeter);
                var tailPts = [];
                if (definition.tailStyle == TailStyle.DISH)
                {
                    const rise = (Rd - pocketR) * tan(definition.dishAngle);
                    var zDish = zBot + rise;
                    const zDishMax = zTop - max(definition.pocketFloor, 2 * millimeter);
                    if (zDish > zDishMax)
                    {
                        const note = "The dish at " ~ toDeg(definition.dishAngle) ~ " deg would rise " ~ toMm(rise) ~ " mm and break through the upstream face: it is flattened to " ~ toDeg(atan((zDishMax - zBot) / (Rd - pocketR))) ~ " deg. Lengthen the hub, widen the pocket or lower the angle.";
                        propPrint(context, "propLog", "[prop] WARNING: " ~ note);
                        propPrint(context, "propNotice", note);
                        reportFeatureWarning(context, id, note);
                        zDish = zDishMax;
                    }
                    const zOut = zBot - 2 * millimeter * (zDish - zBot) / (Rd - pocketR);      // the dish line carried 2 mm past the rim
                    tailPts = [vector(0 * millimeter, zBot - 3 * millimeter), vector(Rd + 2 * millimeter, zBot - 3 * millimeter), vector(Rd + 2 * millimeter, zOut), vector(pocketR, zDish)];
                    if (pocketR > 0 * millimeter && zFloorWanted > zDish)
                        tailPts = concatenateArrays([tailPts, [vector(pocketR, zFloorWanted), vector(0 * millimeter, zFloorWanted)]]);
                    else if (pocketR > 0 * millimeter)
                        tailPts = append(tailPts, vector(0 * millimeter, zDish));
                    propPrint(context, "propLog", "[prop] tail: dish " ~ toDeg(definition.dishAngle) ~ " deg from the rim (r " ~ toMm(Rd) ~ ") to r " ~ toMm(pocketR) ~ " at z " ~ toMm(zDish) ~ ((pocketR > 0 * millimeter) ? ", pocket " ~ toMm(definition.pocketDia) ~ " mm to z " ~ toMm(max(zFloorWanted, zDish)) : ""));
                }
                else if (pocketR > 0 * millimeter)
                {
                    tailPts = [vector(0 * millimeter, zLo), vector(pocketR, zLo), vector(pocketR, zFloorWanted), vector(0 * millimeter, zFloorWanted)];
                    propPrint(context, "propLog", "[prop] tail: " ~ ((definition.tailStyle == TailStyle.CONE) ? "cone cap " ~ toMm(tailLen) ~ " mm" : "flat") ~ ", pocket " ~ toMm(definition.pocketDia) ~ " mm to z " ~ toMm(zFloorWanted) ~ " (" ~ toMm(definition.pocketFloor) ~ " mm floor)");
                }
                else
                    propPrint(context, "propLog", "[prop] tail: " ~ ((definition.tailStyle == TailStyle.CONE) ? "cone cap " ~ toMm(tailLen) ~ " mm" : "flat") ~ ", no pocket");
                if (size(tailPts) > 0)
                {
                    const tailId = id + "tail";
                    const tsk = newSketchOnPlane(context, tailId + "sketch", { "sketchPlane" : plane(vector(0, 0, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0)) });
                    for (var k = 0; k < size(tailPts); k += 1)
                        skLineSegment(tsk, "seg" ~ k, { "start" : tailPts[k], "end" : tailPts[(k + 1) % size(tailPts)] });
                    skSolve(tsk);
                    opRevolve(context, tailId + "revolve", { "entities" : qSketchRegion(tailId + "sketch"), "axis" : zAxis, "angleForward" : 360 * degree });
                    opDeleteBodies(context, tailId + "dropSketch", { "entities" : qCreatedBy(tailId + "sketch", EntityType.BODY) });
                    opBoolean(context, tailId + "cut", { "tools" : qCreatedBy(tailId + "revolve", EntityType.BODY), "targets" : (coreOn ? qUnion([propBody, coreBody]) : propBody), "operationType" : BooleanOperationType.SUBTRACTION });
                }
            }

            // ---- 7. The mount: one type, chosen in the dialog ----
            propPrint(context, "propLog", "[prop] tail done; mount");
            if (mt == MountType.BORE || mt == MountType.KEYED)
            {
                fCylinder(context, id + "bore", { "topCenter" : vector(0, 0, 1) * zHi, "bottomCenter" : vector(0, 0, 1) * zLo, "radius" : definition.boreDia / 2 });
                var tools = [qCreatedBy(id + "bore", EntityType.BODY)];
                if (mt == MountType.KEYED)
                {
                    const kw = definition.keyWidth;
                    const kr = definition.boreDia / 2 + definition.keyDepth;
                    const keySk = newSketchOnPlane(context, id + "keySketch", { "sketchPlane" : plane(vector(0, 0, 1) * zLo, vector(0, 0, 1), vector(1, 0, 0)) });
                    skRectangle(keySk, "key", { "firstCorner" : vector(-kw / 2, 0 * millimeter), "secondCorner" : vector(kw / 2, kr) });
                    skSolve(keySk);
                    opExtrude(context, id + "key", { "entities" : qSketchRegion(id + "keySketch"), "direction" : vector(0, 0, 1), "endBound" : BoundingType.BLIND, "endDepth" : zHi - zLo });
                    opDeleteBodies(context, id + "dropKeySketch", { "entities" : qCreatedBy(id + "keySketch", EntityType.BODY) });
                    tools = append(tools, qCreatedBy(id + "key", EntityType.BODY));
                }
                opBoolean(context, id + "cutBore", { "tools" : qUnion(tools), "targets" : (coreOn ? qUnion([propBody, coreBody]) : propBody), "operationType" : BooleanOperationType.SUBTRACTION });
                propPrint(context, "propLog", "[prop] mount: " ~ (mt == MountType.KEYED ? "bore " ~ toMm(definition.boreDia) ~ " mm with a " ~ toMm(definition.keyWidth) ~ " x " ~ toMm(definition.keyDepth) ~ " mm keyway" : "plain bore " ~ toMm(definition.boreDia) ~ " mm"));
            }
            else if (mt == MountType.BOLTED)
            {
                var boltTools = [];
                if (definition.pilotDia > 0 * millimeter)
                {
                    fCylinder(context, id + "pilot", { "topCenter" : vector(0, 0, 1) * zHi, "bottomCenter" : vector(0, 0, 1) * zLo, "radius" : definition.pilotDia / 2 });
                    boltTools = append(boltTools, qCreatedBy(id + "pilot", EntityType.BODY));
                }
                const boltHole = (definition.boltSize == BoltSize.CUSTOM) ? definition.boltDia : boltDims(definition.boltSize).hole;
                const headHole = (definition.boltSize == BoltSize.CUSTOM) ? definition.headDia : boltDims(definition.boltSize).head;
                const recess = definition.recessHeads && headHole > boltHole;
                for (var k = 0; k < definition.boltCount; k += 1)
                {
                    const ang = k * 360 / definition.boltCount * degree;
                    const cx = definition.boltPcd / 2 * cos(ang);
                    const cy = definition.boltPcd / 2 * sin(ang);
                    const bid = id + ("bolt" ~ k);
                    fCylinder(context, bid, { "topCenter" : vector(cx, cy, zHi), "bottomCenter" : vector(cx, cy, zLo), "radius" : boltHole / 2 });
                    boltTools = append(boltTools, qCreatedBy(bid, EntityType.BODY));
                    if (recess)
                    {
                        const zHead = zTop - definition.boltFlange;
                        if (zHead > zLo)
                        {
                            fCylinder(context, bid + "head", { "topCenter" : vector(cx, cy, zHead), "bottomCenter" : vector(cx, cy, zLo), "radius" : headHole / 2 });
                            boltTools = append(boltTools, qCreatedBy(bid + "head", EntityType.BODY));
                        }
                    }
                }
                if (recess && zTop - definition.boltFlange <= zLo)
                {
                    const note = "The bolt flange (" ~ toMm(definition.boltFlange) ~ " mm) is thicker than the hub, so there is no room for the head recess: plain through holes cut instead.";
                    propPrint(context, "propLog", "[prop] WARNING: " ~ note);
                    propPrint(context, "propNotice", note);
                    reportFeatureWarning(context, id, note);
                }
                opBoolean(context, id + "cutBolts", { "tools" : qUnion(boltTools), "targets" : (coreOn ? qUnion([propBody, coreBody]) : propBody), "operationType" : BooleanOperationType.SUBTRACTION });
                propPrint(context, "propLog", "[prop] mount: " ~ definition.boltCount ~ " x " ~ ((definition.boltSize == BoltSize.CUSTOM) ? "custom" : toString(definition.boltSize)) ~ " through holes " ~ toMm(boltHole) ~ " mm on a " ~ toMm(definition.boltPcd) ~ " mm PCD" ~ (recess ? ", " ~ toMm(headHole) ~ " mm head recesses to a " ~ toMm(definition.boltFlange) ~ " mm flange" : ", heads not recessed") ~ (definition.pilotDia > 0 * millimeter ? ", centre bore " ~ toMm(definition.pilotDia) ~ " mm" : ""));
            }
            else
                propPrint(context, "propLog", "[prop] mount: none (solid hub)");

            // ---- 8. Name, anchor, fold, check ----
            propPrint(context, "propLog", "[prop] mount done; name, anchor, check");
            if (!folding)
            {
                setProperty(context, { "entities" : propBody, "propertyType" : PropertyType.NAME, "value" : "Propeller " ~ toMm(D) ~ " mm x " ~ Z });
                // ---- the model the Propeller mold reads (build 72), in the frame the part is shown in: showAftUp turns
                //      everything 180 deg about X, so (x, y, z) -> (x, -y, -z); the upstream (nose) face ends at -zTop ----
                var ml = [];
                for (var s = 0; s < size(blade.meanLines); s += 1)
                {
                    var pts = [];
                    for (var p in blade.meanLines[s])
                        pts = append(pts, [p[0] / millimeter, -p[1] / millimeter, -p[2] / millimeter]);
                    ml = append(ml, pts);
                }
                var rs = [];
                var cs = [];
                var phis = [];
                var ts = [];
                var crs = [];
                for (var s = 0; s < size(blade.stationR); s += 1)
                {
                    rs = append(rs, blade.stationR[s] / millimeter);
                    cs = append(cs, blade.stationChord[s] / millimeter);
                    phis = append(phis, blade.stationPhi[s] / degree);
                    ts = append(ts, blade.stationThick[s] / millimeter);
                    crs = append(crs, blade.stationCrest[s]);
                }
                const bodyBox = evBox3d(context, { "topology" : propBody, "tight" : true });
                const modelMap = {
                            "build" : PROP_FILE_BUILD, "tag" : toString(id), "D" : D / millimeter, "R" : R / millimeter, "Z" : Z, "hand" : (definition.rotation == Rotation.CW ? 1 : -1),
                            "hubRu" : Ru / millimeter, "hubRd" : Rd / millimeter, "hubZa" : -zTop / millimeter, "hubZb" : -zBot / millimeter,
                            "noseLen" : definition.noseLength / millimeter, "tailLen" : tailLen / millimeter,
                            "tailStyle" : (definition.tailStyle == TailStyle.DISH ? "DISH" : (definition.tailStyle == TailStyle.CONE ? "CONE" : "FLAT")),
                            "pocketDia" : definition.pocketDia / millimeter, "pocketFloor" : definition.pocketFloor / millimeter,
                            "bore" : ((mt == MountType.BORE || mt == MountType.KEYED) ? definition.boreDia / millimeter : (mt == MountType.BOLTED && definition.pilotDia > 0 * millimeter ? definition.pilotDia / millimeter : 0)), "fillet" : definition.rootFillet / millimeter,
                            "zLo" : -bodyBox.maxCorner[2] / millimeter, "zHi" : -bodyBox.minCorner[2] / millimeter,
                            "stationR" : rs, "stationChord" : cs, "stationPhi" : phis, "stationThick" : ts, "stationCrest" : crs, "meanLines" : ml,
                            "core" : coreOn ? 1 : 0, "skin" : coreOn ? definition.coreSkin / millimeter : 0 };
                setVariable(context, "propModel", modelMap);
                setAttribute(context, { "entities" : propBody, "name" : "propModel", "attribute" : modelMap });
                setAttribute(context, { "entities" : propBody, "name" : "propModelBody", "attribute" : "Propeller " ~ toMm(D) ~ " mm x " ~ Z });
                if (coreOn)
                {
                    setProperty(context, { "entities" : coreBody, "propertyType" : PropertyType.NAME, "value" : "Hub core, " ~ toMm(D) ~ " mm x " ~ Z ~ ", skin " ~ toMm(definition.coreSkin) });
                    setAttribute(context, { "entities" : coreBody, "name" : "propCoreBody", "attribute" : toString(id) });
                }
                propPrint(context, "propLog", "[prop] propModel published for the Propeller mold: " ~ size(ml) ~ " stations; body stamped propModelBody");
            }
            else
            {
                setProperty(context, { "entities" : propBody, "propertyType" : PropertyType.NAME, "value" : "Hub " ~ toMm(D) ~ " mm folder x " ~ Z });
                var bladeList = evaluateQuery(context, bladeBodies);
                for (var k = 0; k < size(bladeList); k += 1)
                {
                    setProperty(context, { "entities" : bladeList[k], "propertyType" : PropertyType.NAME, "value" : "Blade " ~ (k + 1) ~ " of " ~ Z ~ ", " ~ toMm(D) ~ " mm" });
                }
                const pinList = evaluateQuery(context, foldInfo.pinBodies);
                for (var k = 0; k < size(pinList); k += 1)
                {
                    setProperty(context, { "entities" : pinList[k], "propertyType" : PropertyType.NAME, "value" : "Pin " ~ (k + 1) ~ " of " ~ Z });
                }
                if (definition.showFolded)
                {
                    // each blade about its own pin: the azimuth-0 axis rotated to blade k
                    for (var k = 0; k < size(bladeList); k += 1)
                    {
                        const th = k * 360 / Z * degree;
                        const origin = vector(-foldInfo.rPin * sin(th), foldInfo.rPin * cos(th), foldInfo.zPin);
                        const dir = vector(-cos(th), -sin(th), 0);
                        opTransform(context, id + ("fold" ~ k), { "bodies" : bladeList[k], "transform" : rotationAround(line(origin, dir), definition.foldAngle) });
                    }
                    var hits = 0;
                    var bladeHits = 0;
                    try silent
                    {
                        hits = size(evCollision(context, { "tools" : bladeBodies, "targets" : propBody }));
                        for (var k = 0; k < size(bladeList); k += 1)
                        {
                            bladeHits += size(evCollision(context, { "tools" : bladeList[k], "targets" : qSubtraction(bladeBodies, bladeList[k]) }));
                        }
                    }
                    if (hits > 0 || bladeHits > 0)
                    {
                        const note = "Folded to " ~ toDeg(definition.foldAngle) ~ " deg the blades interfere (" ~ hits ~ " blade-hub and " ~ bladeHits ~ " blade-blade face pairs). Aft rake swings folded tips in toward the axis, so rake forward (negative) or add skew to splay them; or a smaller fold angle, a longer hub, or fewer blades.";
                        propPrint(context, "propLog", "[prop] WARNING: " ~ note);
                        propPrint(context, "propNotice", note);
                        reportFeatureWarning(context, id, note);
                    }
                    else
                        propPrint(context, "propLog", "[prop] folded to " ~ toDeg(definition.foldAngle) ~ " deg: the blades clear the hub and each other");
                }
            }
            opMateConnector(context, id + "shaftMate", {
                        "coordSystem" : coordSystem(vector(0, 0, 1) * zBot, vector(1, 0, 0), vector(0, 0, 1)),
                        "owner" : propBody
                    });
            operatingCheck(context, bladeDef);
            showAftUp(context, id);
        }
        catch (error)
        {
            const msg = "ERROR: " ~ toString(error);
            propPrint(context, "propLog", msg);
            propPrint(context, "propNotice", msg);
            // A failed feature is rolled back, variables included, so in debug mode the failure is
            // reported as a warning carrying the message and the feature is left standing with its log.
            if (definition.debugPrint)
                reportFeatureWarning(context, id, msg ~ " (Print to notices is on: the failure is shown as this warning so the log survives)");
            else
                throw error;
        }
    });

// ===========================================================================
// Propeller mold (build 72): a printed matched mold for a fixed propeller laid up as one carbon part.
// This build: the two halves parted on the blades' mean surfaces,
// the well for the hub sectors, the rod hole, the release check. Next: sectors, pinch-off, keys, core.
// ===========================================================================

/** Axis-aligned box from lo to hi, as a loft of two rectangles. */
function pmBox(context is Context, boxId is Id, lo is Vector, hi is Vector)
{
    var sketchBodies = [];
    var profiles = [];
    for (var k = 0; k < 2; k += 1)
    {
        const z = k == 0 ? lo[2] : hi[2];
        const sid = boxId + ("r" ~ k);
        var sk = newSketchOnPlane(context, sid, { "sketchPlane" : plane(vector(0 * meter, 0 * meter, z), Z_DIRECTION, X_DIRECTION) });
        const c = [vector(lo[0], lo[1]), vector(hi[0], lo[1]), vector(hi[0], hi[1]), vector(lo[0], hi[1])];
        for (var j = 0; j < 4; j += 1)
            skLineSegment(sk, "s" ~ j, { "start" : c[j], "end" : c[(j + 1) % 4] });
        skSolve(sk);
        profiles = append(profiles, qSketchRegion(sid));
        sketchBodies = append(sketchBodies, qCreatedBy(sid, EntityType.BODY));
    }
    opLoft(context, boxId, { "profileSubqueries" : profiles });
    opDeleteBodies(context, boxId + "del", { "entities" : qUnion(sketchBodies) });
}

/** Cylinder along Z from z0 to z1 about the axis, radius r, as a loft of two circles. Returns its body query. */
function pmCylinder(context is Context, cylId is Id, z0 is ValueWithUnits, z1 is ValueWithUnits, r is ValueWithUnits) returns Query
{
    const zs = [z0, z1];
    var regions = [];
    for (var i = 0; i < 2; i += 1)
    {
        const sid = cylId + ("c" ~ i);
        var sk = newSketchOnPlane(context, sid, { "sketchPlane" : plane(vector(0 * meter, 0 * meter, zs[i]), Z_DIRECTION, X_DIRECTION) });
        skCircle(sk, "c", { "center" : vector(0, 0) * meter, "radius" : r });
        skSolve(sk);
        regions = append(regions, qSketchRegion(sid));
    }
    opLoft(context, cylId + "loft", { "profileSubqueries" : regions });
    opDeleteBodies(context, cylId + "del", { "entities" : qUnion([qCreatedBy(cylId + "c0", EntityType.BODY), qCreatedBy(cylId + "c1", EntityType.BODY)]) });
    return qCreatedBy(cylId + "loft", EntityType.BODY);
}

/** Truncated cone along Z from z0 (radius r0) to z1 (radius r1), as a loft of two circles about the point (x, y). Returns its body query. */
function pmFrustum(context is Context, fid is Id, x is ValueWithUnits, y is ValueWithUnits, z0 is ValueWithUnits, r0 is ValueWithUnits, z1 is ValueWithUnits, r1 is ValueWithUnits) returns Query
{
    const zs = [z0, z1];
    const rs = [r0, r1];
    var regions = [];
    for (var i = 0; i < 2; i += 1)
    {
        const sid = fid + ("c" ~ i);
        var sk = newSketchOnPlane(context, sid, { "sketchPlane" : plane(vector(x, y, zs[i]), Z_DIRECTION, X_DIRECTION) });
        skCircle(sk, "c", { "center" : vector(0, 0) * meter, "radius" : rs[i] });
        skSolve(sk);
        regions = append(regions, qSketchRegion(sid));
    }
    opLoft(context, fid + "loft", { "profileSubqueries" : regions });
    opDeleteBodies(context, fid + "del", { "entities" : qUnion([qCreatedBy(fid + "c0", EntityType.BODY), qCreatedBy(fid + "c1", EntityType.BODY)]) });
    return qCreatedBy(fid + "loft", EntityType.BODY);
}

/** A thin radial slab at azimuth th (rad): r0 to r1 along the radial, w wide across it, z0 to z1; a loft of two rectangles. Returns its body query. */
function pmSlab(context is Context, sid is Id, th is number, r0 is ValueWithUnits, r1 is ValueWithUnits, w is ValueWithUnits, z0 is ValueWithUnits, z1 is ValueWithUnits) returns Query
{
    const c = cos(th * radian);
    const sn = sin(th * radian);
    const corners = [vector(r0 * c - w / 2 * (-sn), r0 * sn - w / 2 * c), vector(r1 * c - w / 2 * (-sn), r1 * sn - w / 2 * c),
                     vector(r1 * c + w / 2 * (-sn), r1 * sn + w / 2 * c), vector(r0 * c + w / 2 * (-sn), r0 * sn + w / 2 * c)];
    const zs = [z0, z1];
    var regions = [];
    var sketches = [];
    for (var k = 0; k < 2; k += 1)
    {
        const kid = sid + ("r" ~ k);
        var sk = newSketchOnPlane(context, kid, { "sketchPlane" : plane(vector(0 * meter, 0 * meter, zs[k]), Z_DIRECTION, X_DIRECTION) });
        for (var j = 0; j < 4; j += 1)
            skLineSegment(sk, "s" ~ j, { "start" : corners[j], "end" : corners[(j + 1) % 4] });
        skSolve(sk);
        regions = append(regions, qSketchRegion(kid));
        sketches = append(sketches, qCreatedBy(kid, EntityType.BODY));
    }
    opLoft(context, sid + "loft", { "profileSubqueries" : regions });
    opDeleteBodies(context, sid + "del", { "entities" : qUnion(sketches) });
    return qCreatedBy(sid + "loft", EntityType.BODY);
}

/** Every solid of q with its extents, one log line, under a label. */
function pmLogBodies(context is Context, label is string, q is Query)
{
    const bodies = evaluateQuery(context, q);
    var line = "[mold] " ~ label ~ ": " ~ size(bodies) ~ " bodies";
    for (var b in bodies)
    {
        const bb = evBox3d(context, { "topology" : b, "tight" : true });
        line = line ~ " | x " ~ round(bb.minCorner[0] / millimeter) ~ ".." ~ round(bb.maxCorner[0] / millimeter) ~ " y " ~ round(bb.minCorner[1] / millimeter) ~ ".." ~ round(bb.maxCorner[1] / millimeter) ~
            " z " ~ round(bb.minCorner[2] / millimeter * 10) / 10 ~ ".." ~ round(bb.maxCorner[2] / millimeter * 10) / 10;
    }
    propPrint(context, "moldLog", line);
}

/** A thin vertical slab along a plan polyline (2D points with units), w wide, z0 to z1: a loft of two polygons. Returns its body query. */
function pmSlabPoly(context is Context, sid is Id, pts is array, w is ValueWithUnits, z0 is ValueWithUnits, z1 is ValueWithUnits) returns Query
{
    const n = size(pts);
    var left = [];
    var right = [];
    for (var i = 0; i < n; i += 1)
    {
        const d = normalize((i < n - 1 ? pts[i + 1] : pts[i]) - (i > 0 ? pts[i - 1] : pts[i]));
        const nrm = vector(-d[1], d[0]);
        left = append(left, pts[i] + nrm * (w / 2));
        right = append(right, pts[i] - nrm * (w / 2));
    }
    const loop = concatenateArrays([left, reverse(right)]);
    const zs = [z0, z1];
    var regions = [];
    var sketches = [];
    for (var k = 0; k < 2; k += 1)
    {
        const kid = sid + ("p" ~ k);
        var sk = newSketchOnPlane(context, kid, { "sketchPlane" : plane(vector(0 * meter, 0 * meter, zs[k]), Z_DIRECTION, X_DIRECTION) });
        for (var j = 0; j < size(loop); j += 1)
            skLineSegment(sk, "e" ~ j, { "start" : loop[j], "end" : loop[(j + 1) % size(loop)] });
        skSolve(sk);
        regions = append(regions, qSketchRegion(kid));
        sketches = append(sketches, qCreatedBy(kid, EntityType.BODY));
    }
    opLoft(context, sid + "loft", { "profileSubqueries" : regions });
    opDeleteBodies(context, sid + "del", { "entities" : qUnion(sketches) });
    return qCreatedBy(sid + "loft", EntityType.BODY);
}

/** The solids sorted by which block face they touch: upper touches the top only, lower the bottom only. */
function pmHalves(context is Context, solids is Query, lo is Vector, hi is Vector) returns map
{
    var up = [];
    var dn = [];
    for (var b in evaluateQuery(context, solids))
    {
        if (getAttribute(context, { "entity" : b, "name" : "propEjectPlug" }) == true)
            continue;                                                   // a printed ejector plug is not a half (build 114)
        const bx = evBox3d(context, { "topology" : b, "tight" : false });
        const top = bx.maxCorner[2] > hi[2] - 0.5 * millimeter;
        const bottom = bx.minCorner[2] < lo[2] + 0.5 * millimeter;
        if (top && !bottom)
            up = append(up, b);
        else if (bottom && !top)
            dn = append(dn, b);
    }
    return { "upper" : qUnion(up), "lower" : qUnion(dn), "nUpper" : size(up), "nLower" : size(dn) };
}

/** Z of every crossing of the vertical line (x, y) with the body, top first; [] for a miss. */
function pmHits(context is Context, body is Query, x is ValueWithUnits, y is ValueWithUnits, zTop is ValueWithUnits) returns array
{
    const res = evRaycast(context, { "entities" : body, "ray" : line(vector(x, y, zTop), -Z_DIRECTION), "closest" : false });
    var zs = [];
    for (var r in res)
    {
        const z = r.intersection[2];
        if (size(zs) == 0 || zs[size(zs) - 1] - z > 1e-7 * meter)
            zs = append(zs, z);
    }
    return zs;
}

/** A point of an arc { th : [rad], z : [mm] } at fraction u of its index range, linear between samples: [th, z]. */
function pmArcAt(arc is map, u is number) returns array
{
    const n = size(arc.th);
    const s = u * (n - 1);
    const i = min(floor(s), n - 2);
    const f = s - i;
    return [arc.th[i] + f * (arc.th[i + 1] - arc.th[i]), arc.z[i] + f * (arc.z[i + 1] - arc.z[i])];
}

/** The arc between two station arcs at weight w (0 = a, 1 = b), resampled at n points. */
function pmBlend(a is map, b is map, w is number, n is number) returns map
{
    var th = [];
    var z = [];
    for (var k = 0; k < n; k += 1)
    {
        const pa = pmArcAt(a, k / (n - 1));
        const pb = pmArcAt(b, k / (n - 1));
        th = append(th, pa[0] * (1 - w) + pb[0] * w);
        z = append(z, pa[1] * (1 - w) + pb[1] * w);
    }
    return { "r" : a.r * (1 - w) + b.r * w, "th" : th, "z" : z };
}

/** A flat arc: arc's theta span resampled at 8 points, all at height z0, for the loops beyond the tips. */
function pmFlat(arc is map, r is number, z0 is number) returns map
{
    var th = [];
    var z = [];
    for (var k = 0; k < 8; k += 1)
    {
        th = append(th, arc.th[0] + (arc.th[size(arc.th) - 1] - arc.th[0]) * k / 7);
        z = append(z, z0);
    }
    return { "r" : r, "th" : th, "z" : z };
}

/** The parting tool's radial sections round the axis (build 108: every 2.5 deg, was 5). */
const PM_N_AZ = 144;

/**
 * Parting height of a loop at azimuth th (rad), for Z blades: on blade k the mean line's z by linear interpolation in
 * theta; between the end of blade k and the start of blade k + 1 a cosine ease, as the loop's gap arcs.
 */
function pmLoopZ(lp is map, th is number, Z is number) returns number
{
    const n = size(lp.th);
    const span = 2 * PI / Z;
    var t = th - lp.th[0];
    t = t - 2 * PI * floor(t / (2 * PI));                       // 0 <= t < 2 pi from blade 0's start
    const k = floor(t / span);
    const u = t - k * span;                                      // position inside blade k's sector
    const bladeSpan = lp.th[n - 1] - lp.th[0];
    if (u <= bladeSpan)
    {
        const a = lp.th[0] + u;
        for (var i = 0; i < n - 1; i += 1)
            if (a <= lp.th[i + 1] + 1e-12)
                return lp.z[i] + (lp.z[i + 1] - lp.z[i]) * (lp.th[i + 1] > lp.th[i] ? (a - lp.th[i]) / (lp.th[i + 1] - lp.th[i]) : 0);
        return lp.z[n - 1];
    }
    const gap = span - bladeSpan;
    const g = (u - bladeSpan) / gap;
    var z = lp.z[n - 1] + (lp.z[0] - lp.z[n - 1]) * (1 - cos(PI * g * radian)) / 2;
    // build 108: leave each edge on the mean line's own slope (no crease for the loft to round off), decaying over
    // about three section steps
    const d = max(2, floor(n / 8));
    if (n > d + 1)
    {
        const mTE = (lp.th[n - 1] > lp.th[n - 1 - d]) ? (lp.z[n - 1] - lp.z[n - 1 - d]) / (lp.th[n - 1] - lp.th[n - 1 - d]) : 0;
        const mLE = (lp.th[d] > lp.th[0]) ? (lp.z[d] - lp.z[0]) / (lp.th[d] - lp.th[0]) : 0;
        const p = max(2, gap / (3 * 2 * PI / PM_N_AZ) - 1);
        z += mTE * gap * g * (1 - g) ^ p - mLE * gap * (1 - g) * g ^ p;
    }
    return z;
}

/** Parting height z(r, th) from the loops (radii increasing): linear between loops, flat inside the first and beyond the last. */
function pmPartingZ(loops is array, r is number, th is number, Z is number) returns number
{
    if (r <= loops[0].r)
        return pmLoopZ(loops[0], th, Z);
    for (var L = 0; L < size(loops) - 1; L += 1)
    {
        if (r <= loops[L + 1].r)
        {
            const w = (r - loops[L].r) / (loops[L + 1].r - loops[L].r);
            return pmLoopZ(loops[L], th, Z) * (1 - w) + pmLoopZ(loops[L + 1], th, Z) * w;
        }
    }
    return pmLoopZ(loops[size(loops) - 1], th, Z);
}

/** arc at radius r with its z pulled toward z0 by factor f (1 = unchanged, 0 = flat at z0), for the loops inside the well. */
function pmTowards(arc is map, r is number, z0 is number, f is number) returns map
{
    var z = [];
    for (var v in arc.z)
        z = append(z, z0 + (v - z0) * f);
    return { "r" : r, "th" : arc.th, "z" : z };
}

export enum PropMoldClosing
{
    annotation { "Name" : "Clamps against a board" }
    CLAMPS,
    annotation { "Name" : "Vacuum bag round the mold" }
    BAG
}

/** The Foil mold's moldOffsetLoop: a closed plan loop (2D points with units) offset outward by d, bevels on convex corners. */
function pmOffsetLoop(pts is array, d is ValueWithUnits) returns array
{
    const n = size(pts);
    var area2 = 0 * meter * meter;
    for (var i = 0; i < n; i += 1)
        area2 += pts[i][0] * pts[(i + 1) % n][1] - pts[(i + 1) % n][0] * pts[i][1];
    const sgn = area2 > 0 * meter * meter ? 1 : -1;
    var starts = [];
    var ends = [];
    var dirs = [];
    for (var i = 0; i < n; i += 1)
    {
        const a = pts[i];
        const b = pts[(i + 1) % n];
        const tt = b - a;
        const len = norm(tt);
        if (len < 1e-6 * meter)
            continue;
        const u = tt / len;
        const nrm = vector(u[1], -u[0]) * sgn;
        starts = append(starts, a + nrm * d);
        ends = append(ends, b + nrm * d);
        dirs = append(dirs, u);
    }
    const ne = size(starts);
    var out = [];
    for (var i = 0; i < ne; i += 1)
    {
        const j = (i + 1) % ne;
        const cross = (dirs[i][0] * dirs[j][1] - dirs[i][1] * dirs[j][0]) * sgn;
        if (cross >= -1e-9)
        {
            out = append(out, ends[i]);
        }
        else
        {
            const w = starts[j] - ends[i];
            const den = dirs[i][0] * dirs[j][1] - dirs[i][1] * dirs[j][0];
            const sPar = (w[0] * dirs[j][1] - w[1] * dirs[j][0]) / den;
            out = append(out, ends[i] + dirs[i] * sPar);
            starts[j] = out[size(out) - 1];
        }
    }
    var loop = [];
    for (var i = 0; i < ne; i += 1)
    {
        const p0 = starts[i];
        if (size(loop) == 0 || norm(p0 - loop[size(loop) - 1]) > 1e-6 * meter)
            loop = append(loop, p0);
        const q0 = out[i];
        if (norm(q0 - loop[size(loop) - 1]) > 1e-6 * meter)
            loop = append(loop, q0);
    }
    return loop;
}

/** A prism on a closed plan loop (2D points with units) from z0 to z1, a loft of two polygons. Returns its body query. */
function pmPrism(context is Context, prismId is Id, loop is array, z0 is ValueWithUnits, z1 is ValueWithUnits) returns Query
{
    var sketchBodies = [];
    var profiles = [];
    const zs = [z0, z1];
    for (var k = 0; k < 2; k += 1)
    {
        const sid = prismId + ("p" ~ k);
        var sk = newSketchOnPlane(context, sid, { "sketchPlane" : plane(vector(0 * meter, 0 * meter, zs[k]), Z_DIRECTION, X_DIRECTION) });
        for (var i = 0; i < size(loop); i += 1)
            skLineSegment(sk, "e" ~ i, { "start" : loop[i], "end" : loop[(i + 1) % size(loop)] });
        skSolve(sk);
        profiles = append(profiles, qSketchRegion(sid));
        sketchBodies = append(sketchBodies, qCreatedBy(sid, EntityType.BODY));
    }
    opLoft(context, prismId + "loft", { "profileSubqueries" : profiles });
    opDeleteBodies(context, prismId + "del", { "entities" : qUnion(sketchBodies) });
    return qCreatedBy(prismId + "loft", EntityType.BODY);
}

/**
 * The layup schedule of a one-piece propeller, the Foil mold's arithmetic: cured ply thickness gsm / (1780 x Vf); the
 * blade as woven skins each face with UD between, the stack full at the section's thickest line and narrowing toward
 * the faces (two thirds of the chord on average); root tabs onto the hub; the hub skin as the woven plies that fill the
 * core's skin gap. lay = { skinPlies, skinGsm, udGsm, tabPlies, tab (mm), vf, over, wasteC, wasteR } as fractions. Returns the
 * note and the flash volume (cm3).
 */
function pmLayup(model is map, vPart is number, vCore is number, lay is map) returns map
{
    const rhoF = 1.78;                                        // g/cm3 carbon
    const rhoR = 1.15;                                        // g/cm3 cured epoxy
    const vf = lay.vf;
    const over = 1 + lay.over;
    const vLam = vPart - vCore;
    const tSkin = lay.skinGsm / (rhoF * 1e6 * vf) * 1000;      // mm cured ply
    const tUd = lay.udGsm / (rhoF * 1e6 * vf) * 1000;
    const tSkins = 2 * lay.skinPlies * tSkin;                  // mm taken by the skins, both faces
    const head = " | layup at " ~ round(vf * 100) ~ " % fibre, " ~ (round(lay.over * 1000) / 10) ~ " % overcharge: laminate " ~ (round(vLam * 10) / 10) ~ " cm3 (part " ~ (round(vPart * 10) / 10) ~
        (vCore > 0 ? ", core " ~ (round(vCore * 10) / 10) : ", no hub core") ~ "), " ~ round(vLam * (vf * rhoF + (1 - vf) * rhoR)) ~ " g cured, " ~ round(vLam * vf * rhoF) ~ " g of it carbon";
    if (model.stationThick == undefined || model.stationChord == undefined)
        return { "note" : head ~ " | no ply schedule: the model carries no station thicknesses (a Propeller feature older than build 86)", "flash" : vLam * lay.over };
    const rHub = max(model.hubRu, model.hubRd);
    var rr = [];
    var cc = [];
    var tt = [];
    const n = size(model.stationR);
    for (var i = 0; i < n; i += 1)
    {
        if (model.stationR[i] < rHub)
        {
            if (i + 1 < n && model.stationR[i + 1] > rHub)
            {
                const w = (rHub - model.stationR[i]) / (model.stationR[i + 1] - model.stationR[i]);
                rr = append(rr, rHub);
                cc = append(cc, model.stationChord[i] + w * (model.stationChord[i + 1] - model.stationChord[i]));
                tt = append(tt, model.stationThick[i] + w * (model.stationThick[i + 1] - model.stationThick[i]));
            }
            continue;
        }
        rr = append(rr, model.stationR[i]);
        cc = append(cc, model.stationChord[i]);
        tt = append(tt, model.stationThick[i]);
    }
    if (size(rr) < 2)
        return { "note" : head ~ " | no ply schedule: no blade station outside the hub", "flash" : vLam * lay.over };
    var aExp = 0;                                             // mm2, one blade's expanded area outside the hub
    var aUd = 0;                                              // mm2 of UD in one blade
    var vBlade = 0;                                           // mm3, one blade
    for (var i = 0; i < size(rr) - 1; i += 1)
    {
        const dr = rr[i + 1] - rr[i];
        const cM = (cc[i] + cc[i + 1]) / 2;
        const tM = (tt[i] + tt[i + 1]) / 2;
        aExp += cM * dr;
        aUd += max(0, round((tM - tSkins) * over / tUd)) * cM * dr * 2 / 3;
        vBlade += 0.68 * cM * tM * dr;
    }
    const nRoot = max(0, round((tt[0] - tSkins) * over / tUd));
    const nTip = max(0, round((tt[size(tt) - 1] - tSkins) * over / tUd));
    const nTab = min(nRoot, lay.tabPlies);
    aUd += nTab * cc[0] * 2 / 3 * lay.tab;                      // the tab plies run on over the hub; the other root plies stop at its surface
    var plies = "";
    for (var q = 0; q < 6; q += 1)
    {
        const rq = rHub + (model.R - rHub) * (q + 0.5) / 6;
        var best = 0;
        for (var i = 1; i < size(rr); i += 1)
            if (abs(rr[i] - rq) < abs(rr[best] - rq))
                best = i;
        plies = plies ~ (plies == "" ? "" : ", ") ~ "r " ~ round(rr[best]) ~ ": " ~ (round(tt[best] * 10) / 10) ~ " mm = " ~ (2 * lay.skinPlies) ~ " woven + " ~ max(0, round((tt[best] - tSkins) * over / tUd)) ~ " UD";
    }
    const Z = model.Z;
    var hubPlies = 0;
    var aHub = 0;                                             // mm2: the hub's sides and both faces
    var hubNote = "";
    if (vCore > 0 && model.skin != undefined && model.skin > 0)
    {
        hubPlies = max(1, round(model.skin * over / tSkin));
        const hLen = abs(model.hubZb - model.hubZa);
        const slant = sqrt(hLen * hLen + (model.hubRu - model.hubRd) * (model.hubRu - model.hubRd));
        const rb = model.bore / 2;
        aHub = PI * (model.hubRu + model.hubRd) * slant + PI * max(0, model.hubRu * model.hubRu - rb * rb) + PI * max(0, model.hubRd * model.hubRd - rb * rb);
        hubNote = " | hub skin: " ~ hubPlies ~ " woven plies over the core to fill its " ~ model.skin ~ " mm (" ~ (round(hubPlies * aHub / 100) / 100) ~ " dm2 of cloth, sides and faces)";
    }
    else
        hubNote = " | NO HUB CORE: about " ~ round(max(0, vLam - Z * vBlade / 1000)) ~ " cm3 of the hub is solid laminate, which plies cannot fill - turn Hub core on in the Propeller feature";
    const aWoven = (Z * 2 * lay.skinPlies * aExp + hubPlies * aHub) / 1e6;       // m2
    const aUdAll = Z * aUd / 1e6;                                               // m2
    const gWoven = aWoven * lay.skinGsm;
    const gUd = aUdAll * lay.udGsm;
    const vLaid = (gWoven + gUd) / rhoF / vf;                                    // cm3 of laminate that cloth makes at this fibre volume
    const fill = vLam > 0 ? vLaid / vLam * 100 : 0;
    const gMix = vLam * (1 - vf) * rhoR * over * (1 + lay.wasteR);
    const note = head ~
        " | blade skins " ~ lay.skinPlies ~ " x " ~ round(lay.skinGsm) ~ " gsm each face (" ~ (round(tSkin * 100) / 100) ~ " mm/ply), " ~ Z ~ " blades of " ~ (round(aExp / 10) / 10) ~ " cm2 expanded" ~
        " | UD " ~ round(lay.udGsm) ~ " gsm (" ~ (round(tUd * 100) / 100) ~ " mm/ply) between the skins, the full stack along the thickest line and narrower toward the faces: " ~ nRoot ~ " plies at the root to " ~ nTip ~
        " at the tip (" ~ plies ~ ")" ~
        (nTab > 0 && lay.tab > 0 ? "; " ~ nTab ~ " of each root's plies run " ~ lay.tab ~ " mm onto the hub as tabs under the hub skin, alternate blades interleaved, the rest stop at the hub (" ~ (round(nTab * tUd * 100) / 100) ~
            " mm of tab" ~ (vCore > 0 && model.skin != undefined && nTab * tUd > 0.5 * model.skin ? " - MORE THAN HALF THE " ~ model.skin ~ " mm SKIN GAP: fewer Tab plies or a thicker Skin thick" : "") ~ ")"
            : "; no root tabs: the blades join the hub by the hub skin alone") ~
        hubNote ~
        " | cloth: " ~ (round(aWoven * 1000) / 1000) ~ " m2 woven (" ~ round(gWoven) ~ " g), " ~ (round(aUdAll * 1000) / 1000) ~ " m2 UD (" ~ round(gUd) ~ " g); it makes " ~ round(fill) ~ " % of the cavity at this fibre volume" ~
        (fill < 90 ? " - UNDER-FILLED: more plies, heavier cloth or a lower Fibre vol %" : (fill > 120 ? " - OVER-FILLED: the halves will not close; fewer plies or lighter cloth" : "")) ~
        " | buy " ~ (round(aWoven * (1 + lay.wasteC) * 1000) / 1000) ~ " m2 woven and " ~ (round(aUdAll * (1 + lay.wasteC) * 1000) / 1000) ~ " m2 UD (+" ~ round(lay.wasteC * 100) ~ " %) | mix about " ~ round(gMix) ~
        " g of resin (+" ~ round(lay.wasteR * 100) ~ " %)";
    return { "note" : note, "flash" : vLam * lay.over };
}

/** Two template stations mixed at weight w (0 = a, 1 = b). */
function pmMixStation(a is map, b is map, w is number) returns map
{
    return { "r" : a.r + w * (b.r - a.r), "t" : a.t + w * (b.t - a.t), "bc" : a.bc + w * (b.bc - a.bc), "uLE" : a.uLE + w * (b.uLE - a.uLE), "uTE" : a.uTE + w * (b.uTE - a.uTE) };
}

/**
 * The cut templates of a one-piece propeller as flat outlines, points [u, v] in mm: u across the chord in the expanded
 * outline (r x theta / cos(pitch)), v out along the blade from the hub. The blade skin; one outline per UD ply (the
 * region where the section less its skins, taken as tmax (1 - P^2) about its crest, is thick enough for that ply; the
 * first tabPlies with the root tab below fold notches); the hub side panel between two roots, developed at the mean
 * radius from the root's own line on the hub (rootArc: th in rad, z in mm); the hub faces. edges: every station's
 * { s, le, te } azimuths. Returns { shapes, names, cuts }.
 */
function pmTemplateShapes(model is map, edges is array, rootArc is map, lay is map, mg is number, zLo is number, zHi is number) returns map
{
    const rhoF = 1.78;
    const vf = lay.vf;
    const over = 1 + lay.over;
    const tSkin = lay.skinGsm / (rhoF * 1e6 * vf) * 1000;
    const tUd = lay.udGsm / (rhoF * 1e6 * vf) * 1000;
    const tSkins = 2 * lay.skinPlies * tSkin;
    const rHub = max(model.hubRu, model.hubRd);
    const Z = model.Z;
    var shapes = [];
    var names = [];
    var cuts = [];
    var kinds = [];                                           // "W" woven or "U" unidirectional, for the cloth totals
    // every station with its edges in expanded coordinates, the azimuths carried continuously from station to station
    var st = [];
    var havePrev = false;
    var prevLe = 0;
    var th0 = 0;
    for (var e in edges)
    {
        var le = e.le;
        var te = e.te;
        if (havePrev)
        {
            const k = round((le - prevLe) / (2 * PI));
            le -= 2 * PI * k;
            te -= 2 * PI * k;
        }
        else
        {
            th0 = le;
            havePrev = true;
        }
        prevLe = le;
        const r = model.stationR[e.s];
        const cphi = cos(model.stationPhi[e.s] * degree);
        st = append(st, { "r" : r, "t" : max(model.stationThick[e.s], 0.001), "bc" : (model.stationCrest == undefined ? 0.4 : model.stationCrest[e.s]), "uLE" : r * (le - th0) / cphi, "uTE" : r * (te - th0) / cphi });
    }
    var allSt = [];
    for (var i = 0; i < size(st); i += 1)
    {
        if (st[i].r < rHub)
        {
            if (i + 1 < size(st) && st[i + 1].r > rHub)
                allSt = append(allSt, pmMixStation(st[i], st[i + 1], (rHub - st[i].r) / (st[i + 1].r - st[i].r)));
            continue;
        }
        allSt = append(allSt, st[i]);
    }
    if (size(allSt) < 3)
        return { "shapes" : shapes, "names" : names, "cuts" : cuts, "kinds" : kinds };
    var out = [];                                             // every second station, the tip kept: a few dozen lines a shape
    for (var i = 0; i < size(allSt); i += 2)
        out = append(out, allSt[i]);
    if ((size(allSt) - 1) % 2 == 1)
        out = append(out, allSt[size(allSt) - 1]);
    const nO = size(out);
    const sg = out[0].uLE > out[0].uTE ? 1 : -1;
    // 1. the woven skin: the expanded outline, grown by the margin
    if (lay.skinPlies > 0)
    {
        var sk = [];
        var dv = [];                                          // the margin along the blade, eased in over the last two margins of span: a round cap
        for (var i = 0; i < nO; i += 1)
            dv = append(dv, i == 0 ? -mg : (mg > 0 ? mg * max(0, 1 - (out[nO - 1].r - out[i].r) / (2 * mg)) : 0));
        for (var i = 0; i < nO; i += 1)
            sk = append(sk, [out[i].uLE + sg * mg, out[i].r - rHub + dv[i]]);
        for (var i = nO - 1; i >= 0; i -= 1)
            sk = append(sk, [out[i].uTE - sg * mg, out[i].r - rHub + dv[i]]);
        shapes = append(shapes, sk);
        names = append(names, "blade skin, the expanded outline, hub end at v = 0 (cut " ~ (2 * lay.skinPlies * Z) ~ ": " ~ lay.skinPlies ~ " per face per blade)");
        cuts = append(cuts, 2 * lay.skinPlies * Z);
        kinds = append(kinds, "W");
    }
    // 2. the UD plies, ply 1 against the skin and the largest
    const nRoot = max(0, round((out[0].t - tSkins) * over / tUd));
    const nk = 2;
    for (var j = 1; j <= nRoot; j += 1)
    {
        const need = (j - 0.5) * tUd / over + tSkins;         // the section thickness this ply needs under it
        var k = -1;
        for (var i = 0; i < nO; i += 1)
        {
            if (need / out[i].t < 1)
                k = i;
            else
                break;
        }
        if (k < 0)
            continue;
        var eL = [];
        var eT = [];
        for (var i = 0; i <= k; i += 1)
        {
            const s = out[i];
            const w = sqrt(1 - need / s.t);
            eL = append(eL, [s.uLE + (s.bc - w * s.bc) * (s.uTE - s.uLE) + sg * mg, s.r - rHub]);
            eT = append(eT, [s.uLE + (s.bc + w * (1 - s.bc)) * (s.uTE - s.uLE) - sg * mg, s.r - rHub]);
        }
        var tipP = [(eL[k][0] + eT[k][0]) / 2, out[k].r - rHub + mg];
        var rEnd = out[k].r;
        if (k + 1 < nO)
        {
            const q0 = need / out[k].t;
            const q1 = need / out[k + 1].t;
            const f = (1 - q0) / max(q1 - q0, 1e-9);
            const uc0 = out[k].uLE + out[k].bc * (out[k].uTE - out[k].uLE);
            const uc1 = out[k + 1].uLE + out[k + 1].bc * (out[k + 1].uTE - out[k + 1].uLE);
            rEnd = out[k].r + f * (out[k + 1].r - out[k].r);
            tipP = [uc0 + f * (uc1 - uc0), rEnd - rHub + mg];
        }
        const tab = j <= lay.tabPlies ? lay.tab : 0;
        var sh = [];
        if (tab > 0)
        {
            // the tab below v = 0, a notch in each edge at the fold line
            sh = [[eL[0][0], -tab - mg], [eL[0][0], -nk], [eL[0][0] - sg * nk, 0], [eL[0][0], nk]];
            for (var i = 1; i <= k; i += 1)
                sh = append(sh, eL[i]);
            sh = append(sh, tipP);
            for (var i = k; i >= 1; i -= 1)
                sh = append(sh, eT[i]);
            sh = concatenateArrays([sh, [[eT[0][0], nk], [eT[0][0] + sg * nk, 0], [eT[0][0], -nk], [eT[0][0], -tab - mg]]]);
        }
        else
        {
            sh = [[eL[0][0], -mg]];
            for (var i = 0; i <= k; i += 1)
                sh = append(sh, eL[i]);
            sh = append(sh, tipP);
            for (var i = k; i >= 0; i -= 1)
                sh = append(sh, eT[i]);
            sh = append(sh, [eT[0][0], -mg]);
        }
        shapes = append(shapes, sh);
        names = append(names, "UD ply " ~ j ~ " (to r " ~ round(rEnd) ~ " mm" ~ (tab > 0 ? ", + " ~ tab ~ " mm tab onto the hub below the fold notches" : "") ~ ", cut " ~ Z ~ ": one per blade)");
        cuts = append(cuts, Z);
        kinds = append(kinds, "U");
    }
    // 3. the hub skin: the side as the panel between two roots, the faces as discs
    if (model.core == 1 && model.skin != undefined && model.skin > 0)
    {
        const hubPlies = max(1, round(model.skin * over / tSkin));
        const rM = (model.hubRu + model.hubRd) / 2;
        const wP = 2 * PI * rM / Z;
        var line0 = [];
        const nA = size(rootArc.th);
        for (var i = 0; i < nA; i += 1)
        {
            const ii = rootArc.z[0] <= rootArc.z[nA - 1] ? i : nA - 1 - i;      // by rising z
            if (rootArc.z[ii] >= zLo && rootArc.z[ii] <= zHi)
                line0 = append(line0, [rM * (rootArc.th[ii] - rootArc.th[0]), rootArc.z[ii]]);
        }
        const nL = size(line0);
        if (nL >= 2)
        {
            var pan = [[line0[0][0] - mg, zLo - mg]];
            for (var i = 0; i < nL; i += 1)
                pan = append(pan, [line0[i][0] - mg, line0[i][1]]);
            pan = concatenateArrays([pan, [[line0[nL - 1][0] - mg, zHi + mg], [line0[nL - 1][0] + wP + mg, zHi + mg]]]);
            for (var i = nL - 1; i >= 0; i -= 1)
                pan = append(pan, [line0[i][0] + wP + mg, line0[i][1]]);
            pan = append(pan, [line0[0][0] + wP + mg, zLo - mg]);
            shapes = append(shapes, pan);
            names = append(names, "hub side, the panel between two roots, developed at the mean radius " ~ (round(rM * 10) / 10) ~ " mm; the slanted edges lie along the roots (cut " ~ (hubPlies * Z) ~ ": " ~ hubPlies ~ " plies x " ~ Z ~ " gaps)");
            cuts = append(cuts, hubPlies * Z);
            kinds = append(kinds, "W");
        }
        const same = abs(model.hubRu - model.hubRd) < 0.5;
        for (var f = 0; f < (same ? 1 : 2); f += 1)
        {
            const rF = f == 0 ? model.hubRu : model.hubRd;
            var disc = [];
            for (var i = 0; i < 36; i += 1)
                disc = append(disc, [(rF + mg) * cos(i * 10 * degree), (rF + mg) * sin(i * 10 * degree)]);
            shapes = append(shapes, disc);
            names = append(names, "hub face" ~ (same ? "" : (f == 0 ? ", upstream" : ", downstream")) ~ ", a disc of r " ~ (round(rF * 10) / 10) ~ " mm plus the margin; punch the " ~ model.bore ~ " mm bore (cut " ~ (same ? 2 * hubPlies : hubPlies) ~ ")");
            cuts = append(cuts, same ? 2 * hubPlies : hubPlies);
            kinds = append(kinds, "W");
        }
    }
    return { "shapes" : shapes, "names" : names, "cuts" : cuts, "kinds" : kinds };
}

/** The solids that touch neither the block's top nor its bottom: the sectors (and anything left loose). */
/** Height where a vertical ray from (x, y, z0) first meets the body, going up (up true) or down (build 114). */
function pmRayZ(context is Context, body is Query, x is ValueWithUnits, y is ValueWithUnits, z0 is ValueWithUnits, up is boolean) returns map
{
    var res = { "hit" : false, "z" : 0 * meter };
    try silent
    {
        const hits = evRaycast(context, { "entities" : body, "ray" : line(vector(x, y, z0), vector(0, 0, up ? 1 : -1)), "closest" : true });
        if (size(hits) > 0)
            res = { "hit" : true, "z" : hits[0].intersection[2] };
    }
    return res;
}

/** Ejector probe (build 114): the cavity's top and bottom at (x, y) from the two halves, and for each face whether it
    is free of steps across a rim of radius rr: each pair of opposite rim points must average to within 0.5 mm of the
    centre, which lets a sloping face (a tail dish) through and stops a pocket's edge, a pin or a plug. */
function pmEjectProbe(context is Context, upper is Query, lower is Query, x is ValueWithUnits, y is ValueWithUnits, zBelow is ValueWithUnits, zAbove is ValueWithUnits, rr is ValueWithUnits) returns map
{
    const cu = pmRayZ(context, upper, x, y, zBelow, true);
    const cl = pmRayZ(context, lower, x, y, zAbove, false);
    if (!cu.hit || !cl.hit || cu.z - cl.z < 1 * millimeter)
        return { "okU" : false, "okL" : false };
    var okU = true;
    var okL = true;
    for (var d in [[1, 0], [0, 1], [0.7071, 0.7071], [0.7071, -0.7071]])
    {
        const u1 = pmRayZ(context, upper, x + d[0] * rr, y + d[1] * rr, zBelow, true);
        const u2 = pmRayZ(context, upper, x - d[0] * rr, y - d[1] * rr, zBelow, true);
        const l1 = pmRayZ(context, lower, x + d[0] * rr, y + d[1] * rr, zAbove, false);
        const l2 = pmRayZ(context, lower, x - d[0] * rr, y - d[1] * rr, zAbove, false);
        if (!u1.hit || !u2.hit || abs(u1.z + u2.z - 2 * cu.z) > 1 * millimeter || abs(u1.z - cu.z) > rr)
            okU = false;
        if (!l1.hit || !l2.hit || abs(l1.z + l2.z - 2 * cl.z) > 1 * millimeter || abs(l1.z - cl.z) > rr)
            okL = false;
    }
    return { "okU" : okU, "okL" : okL, "zU" : cu.z, "zL" : cl.z, "t" : cu.z - cl.z };
}

function pmFloating(context is Context, solids is Query, lo is Vector, hi is Vector) returns array
{
    var fl = [];
    for (var b in evaluateQuery(context, solids))
    {
        if (getAttribute(context, { "entity" : b, "name" : "propEjectPlug" }) == true)
            continue;
        const bx = evBox3d(context, { "topology" : b, "tight" : false });
        if (bx.maxCorner[2] < hi[2] - 0.5 * millimeter && bx.minCorner[2] > lo[2] + 0.5 * millimeter)
            fl = append(fl, b);
    }
    return fl;
}

/** Deletes whatever q still resolves to, quietly: the clean-up after a piece that failed part way. */
function pmDrop(context is Context, did is Id, q is Query)
{
    try silent
    {
        if (size(evaluateQuery(context, q)) > 0)
            opDeleteBodies(context, did, { "entities" : q });
    }
}

/**
 * The release check for a piece pulled along v: the volume (mm^3) a copy of the body moved by v loses when the part
 * is subtracted from it. 0 when the piece clears the part there; -1 when the kernel fails.
 */
function pmPullOverlap(context is Context, pid is Id, body is Query, part is Query, v is Vector) returns number
{
    var out = -1;
    try silent
    {
        opPattern(context, pid + "c", { "entities" : body, "transforms" : [transform(v)], "instanceNames" : ["m"] });
        const cq = qCreatedBy(pid + "c", EntityType.BODY);
        const v0 = evVolume(context, { "entities" : cq });
        opBoolean(context, pid + "s", { "targets" : cq, "tools" : part, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
        const left = qBodyType(qUnion([cq, qCreatedBy(pid + "s", EntityType.BODY)]), BodyType.SOLID);
        var v1 = 0 * millimeter * millimeter * millimeter;
        if (size(evaluateQuery(context, left)) > 0)
            v1 = evVolume(context, { "entities" : left });
        out = (v0 - v1) / (millimeter * millimeter * millimeter);
    }
    pmDrop(context, pid + "d", qUnion([qCreatedBy(pid + "c", EntityType.BODY), qCreatedBy(pid + "s", EntityType.BODY)]));
    return out;
}

annotation { "Feature Type Name" : "Propeller mold",
             "Feature Type Description" : "Printable matched mold for laying up a fixed B-Series Propeller as one carbon part: a nose half and a tail half pulled along the shaft axis, parted on each blade's own mean surface so the seams lie on the leading and trailing edges, a well round the hub for the radial sector pieces, and a rod hole for the bore. Reads the model the B-Series Propeller feature publishes. Halves, hub sectors with keys, marks and pull holes, the pinch-off groove, half keys, the rod, the release checks and the layup line; closed by clamps or a vacuum bag." }
export enum PropEjector
{
    annotation { "Name" : "Off" }
    OFF,
    annotation { "Name" : "Dowel holes (wax-filled)" }
    DOWEL,
    annotation { "Name" : "Printed flush plugs" }
    PLUG
}

export const propMold = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Part", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1,
                     "Description" : "The propeller solid to mold, as it stands after every edit. Leave empty to take the body the last B-Series Propeller feature above named, 'Propeller <D> mm x <Z>' (a fixed hub)." }
        definition.part is Query;

        annotation { "Group Name" : "Block", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Flange width",
                         "Description" : "Flange round the tip circle, for clamps and keys. Limits 15-60 mm. Typical 25." }
            isLength(definition.flangeWidth, { (millimeter) : [15, 25, 60] } as LengthBoundSpec);

            annotation { "Name" : "Base thick",
                         "Description" : "Mold material under the deepest point of each half. Limits 10-40 mm. Typical 18." }
            isLength(definition.baseThick, { (millimeter) : [10, 18, 40] } as LengthBoundSpec);

            annotation { "Name" : "Sector band",
                         "Description" : "How far beyond the hub radius plus the root fillet the hub sectors reach, which is the radius of the well left in the halves for them and the sectors' wall thickness. Limits 3-20 mm. Typical 8: thick enough to print standing and to carry a key." }
            isLength(definition.sectorBand, { (millimeter) : [3, 8, 20] } as LengthBoundSpec);

            annotation { "Name" : "Sector fit",
                         "Description" : "Clearance between the sectors' outer face and the well's wall. Limits 0-0.5 mm. Typical 0.1." }
            isLength(definition.sectorFit, { (millimeter) : [0, 0.1, 0.5] } as LengthBoundSpec);

            annotation { "Name" : "Key dia",
                         "Description" : "Base diameter of the printed frustum key under each sector, 12 deg draft; the socket in the nose half's well floor is 0.2 mm larger. Limits 3-12 mm. Typical 6; must be under the Sector band less 2 mm." }
            isLength(definition.keyDia, { (millimeter) : [3, 6, 12] } as LengthBoundSpec);

            annotation { "Name" : "Key height",
                         "Description" : "Height of the sector keys and the half keys. Limits 2-10 mm. Typical 5; must be under Base thick less 4 mm." }
            isLength(definition.keyHeight, { (millimeter) : [2, 5, 10] } as LengthBoundSpec);

            annotation { "Name" : "Half key dia",
                         "Description" : "Base diameter of the four frustum keys between the halves at the flange corners, 12 deg draft, boss on the tail half, socket 0.2 mm larger in the nose half. Limits 8-20 mm. Typical 14." }
            isLength(definition.halfKeyDia, { (millimeter) : [8, 14, 20] } as LengthBoundSpec);

            annotation { "Name" : "Rod fit",
                         "Description" : "Clearance added to the bore diameter for the rod hole through both halves; the waxed steel rod of the bore diameter forms the bore and registers the hub core. Limits 0-0.5 mm. Typical 0.1. The bore must run through the hub." }
            isLength(definition.rodFit, { (millimeter) : [0, 0.1, 0.5] } as LengthBoundSpec);

            annotation { "Name" : "Plug engage",
                         "Description" : "With a Hub core: how far each plug that forms a recess open to the hub's end face on the well's side (a groove, a counterbore, a pin slot) reaches past the skin into the core's own printed recess, to locate it; the core forms the rest. Without a core the plug fills the whole recess. Limits 0.5-10 mm. Typical 2." }
            isLength(definition.plugEngage, { (millimeter) : [0.5, 2, 10] } as LengthBoundSpec);

            annotation { "Name" : "Gap points",
                         "Description" : "Points along each parting loop in the gap between two blades, where the parting eases from one blade's trailing edge to the next blade's leading edge. Limits 3-16. Typical 6." }
            isInteger(definition.gapPoints, { (unitless) : [3, 6, 16] } as IntegerBoundSpec);
        }

        annotation { "Group Name" : "Pinch-off", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Pinch-off groove", "Default" : true,
                         "Description" : "A land and a groove round every blade in the parting face of the half the plies are laid on (the sectors' side): a narrow land at the cavity edge where the closing mold pinches the laminate and squeezes the excess out, then a groove for that flash, from the hub well along the leading edge, round the tip and back along the trailing edge. Inside the well the sectors' joints take the flash. Typical on." }
            definition.grooveOn is boolean;

            if (definition.grooveOn)
            {
                annotation { "Name" : "Land width",
                             "Description" : "Flat land between the cavity edge and the groove. Limits 1-10 mm. Typical 2-4: narrow enough to build pinch pressure, wide enough to print cleanly." }
                isLength(definition.landWidth, { (millimeter) : [1, 3, 10] } as LengthBoundSpec);

                annotation { "Name" : "Groove width",
                             "Description" : "Width of the flash groove. Limits 1-10 mm. Typical 3-5. Where two blades' grooves meet between the roots they merge." }
                isLength(definition.grooveWidth, { (millimeter) : [1, 4, 10] } as LengthBoundSpec);

                annotation { "Name" : "Groove depth",
                             "Description" : "Depth of the flash groove below the parting face, measured along the shaft axis (less on the steep parting between the roots). Limits 0.5-5 mm. Typical 1.5 clamped; 3 with a 6 mm groove under a bag, whose groove is also the vent path. The notice gives the groove's volume against the flash." }
                isLength(definition.grooveDepth, { (millimeter) : [0.5, 1.5, 5] } as LengthBoundSpec);
            }
        }

        annotation { "Group Name" : "Closing", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Closing", "Default" : "CLAMPS",
                         "Description" : "How the closed mold is pressed shut while the resin cures, as on the Foil mold: no bolts in the mold. Clamps: G or F clamps on the flanges against a rigid board each side. Vacuum bag: the whole closed mold goes in a bag with breather over it and the atmosphere clamps it evenly; the notice gives the closing force and the pressure on the laminate. Typical clamps." }
            definition.closing is PropMoldClosing;

            if (definition.closing == PropMoldClosing.BAG)
            {
                annotation { "Name" : "Bag vacuum kPa",
                             "Description" : "Vacuum the pump holds in the bag, below atmospheric. Limits 50-100 kPa. Typical 80-90 for a shop pump." }
                isReal(definition.bagVac, { (unitless) : [50, 85, 100] } as RealBoundSpec);

                annotation { "Name" : "Groove vents", "Default" : true,
                             "Description" : "A 1 mm slot from each blade's groove at its tip straight out to the nearest block face, as deep as the groove. Inside a bag the groove is otherwise a sealed pocket and the flash cannot flow into it. Needs the Pinch-off groove on. Typical on." }
                definition.ventOn is boolean;

                annotation { "Name" : "Edge round",
                             "Description" : "Radius on the block's outer edges so the bag wraps them without being cut. Limits 0-10 mm. Typical 4-6; 0 leaves them sharp." }
                isLength(definition.edgeRound, { (millimeter) : [0, 5, 10] } as LengthBoundSpec);
            }

            annotation { "Name" : "Pry sections", "Default" : true,
                         "Description" : "Short 45 deg V pockets across the parting on the block's four faces, just big enough to start a wedge between the halves (build 114, as the Foil mold): in sections along each face, clear of the half keys at the corners. Work the wedges in alternately so the halves part square. Off: plain faces." }
            definition.pryOn is boolean;

            if (definition.pryOn)
            {
                annotation { "Name" : "Pry length",
                             "Description" : "Length of each pry section along the face. Limits 10-100 mm. Typical 25-40, room for a wedge and no more." }
                isLength(definition.pryLength, { (millimeter) : [10, 30, 100] } as LengthBoundSpec);

                annotation { "Name" : "Pry depth",
                             "Description" : "How far each 45 deg pocket runs into each half from the face; the V opens 2 x this. Limits 1-8 mm. Typical 3." }
                isLength(definition.pryDepth, { (millimeter) : [1, 3, 8] } as LengthBoundSpec);

                annotation { "Name" : "Pry spacing",
                             "Description" : "Largest gap between pry sections along a face. Limits 40-400 mm. Typical 120; a face shorter than this gets one, in its middle." }
                isLength(definition.prySpacing, { (millimeter) : [40, 120, 400] } as LengthBoundSpec);
            }

            annotation { "Name" : "Ejectors", "Default" : PropEjector.DOWEL,
                         "Description" : "Holes through each half onto the hub's end faces so a part that stays in a half can be pushed out (build 114, as the Foil mold): two per half, between the rod hole and the hub's edge, where both end faces are smooth (no recess, plug or pin under the rim, checked by rays). Dowel holes: fill flush with filleting wax, tap a dowel in to push. Printed flush plugs: a plug with its head on the cavity side in a counterbore, its face cut from the mold; drop it in before the layup, the laminate's pressure seats it, punch it in from outside. Off: none." }
            definition.ejector is PropEjector;

            if (definition.ejector != PropEjector.OFF)
            {
                annotation { "Name" : "Ejector dia",
                             "Description" : "Diameter of each ejector hole: a stock dowel's, or a plug's shank (its head is 4 mm wider, 2.5 mm deep). Limits 2-10 mm. Typical 4 on a prop hub." }
                isLength(definition.ejectorDia, { (millimeter) : [2, 4, 10] } as LengthBoundSpec);

                if (definition.ejector == PropEjector.PLUG)
                {
                    annotation { "Name" : "Plug fit",
                                 "Description" : "Diametral clearance between a printed plug and its hole. Limits 0.05-0.5 mm. Typical 0.15." }
                    isLength(definition.ejectorFit, { (millimeter) : [0.05, 0.15, 0.5] } as LengthBoundSpec);
                }
            }
        }

        annotation { "Group Name" : "Sectors", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Sector marks", "Default" : true,
                         "Description" : "A row of small dots beside the key on each sector's base and the same row in its seat in the well floor: one dot on the first blade's root and gap sectors, two on the second, and so on. The sectors of a kind are identical, so the dots are for keeping each fitted piece with its seat. Left off a sector too narrow to carry them. Typical on." }
            definition.sectorMarks is boolean;

            annotation { "Name" : "Pull hole",
                         "Description" : "Diameter of a blind radial hole in each sector's outer face, to drive a screw into and draw the sector off the cured part. Limits 0-8 mm. Typical 3.3 for an M4 screw cutting its own thread in the print; 0 for none. Left off a sector too small to carry it." }
            isLength(definition.pullHole, { (millimeter) : [0, 3.3, 8] } as LengthBoundSpec);
        }

        annotation { "Group Name" : "Layup", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Skin plies",
                         "Description" : "Woven plies on each face of every blade, full planform, laid first against the mold. Limits 0-6. Typical 1: the surface and the torsion; the UD between them carries the bending." }
            isInteger(definition.skinPlies, { (unitless) : [0, 1, 6] } as IntegerBoundSpec);

            annotation { "Name" : "Skin gsm",
                         "Description" : "Areal weight of the woven cloth, g/m2, for the blade skins and the hub skin. Limits 50-800. Typical 160-245 (2x2 twill). Cured ply thickness is gsm / (1780 x Fibre vol): 0.22 mm for 200 gsm at 50 %." }
            isReal(definition.skinGsm, { (unitless) : [50, 200, 800] } as RealBoundSpec);

            annotation { "Name" : "UD gsm",
                         "Description" : "Areal weight of the unidirectional cloth between the skins, laid spanwise, g/m2. Limits 50-1200. Typical 200-300 wet layup. Sets the UD ply thickness and so the ply count from root to tip: 0.34 mm for 300 gsm at 50 %." }
            isReal(definition.udGsm, { (unitless) : [50, 300, 1200] } as RealBoundSpec);

            annotation { "Name" : "Tab plies",
                         "Description" : "How many of each blade's root UD plies run on over the hub core as tabs, under the hub skin and interleaved with the neighbouring blades' tabs: the joint between blade and hub. The other root plies stop at the hub surface. Limits 0-12. Typical 2-3: each takes a UD ply's thickness out of the core's Skin thick where it lies, and the layup line warns when the tabs take more than half of it." }
            isInteger(definition.tabPlies, { (unitless) : [0, 2, 12] } as IntegerBoundSpec);

            annotation { "Name" : "Root tab",
                         "Description" : "How far each tab ply runs onto the hub. Limits 0-40 mm. Typical 10-15, about the height of the shadow beside the root." }
            isLength(definition.rootTab, { (millimeter) : [0, 12, 40] } as LengthBoundSpec);

            annotation { "Name" : "Fibre vol %",
                         "Description" : "Fibre volume fraction of the cured laminate. Limits 30-65 %. Typical 45-50 wet layup clamped in a matched mold, 55-60 under a vacuum bag. Sets the cured ply thicknesses and the split between carbon (1.78 g/cm3) and epoxy (1.15)." }
            isReal(definition.fibreVolPct, { (unitless) : [30, 50, 65] } as RealBoundSpec);

            annotation { "Name" : "Overcharge %",
                         "Description" : "Extra material laid over the exact cavity fill. Limits 0-30 %. Typical 4-8 clamped: closing squeezes the excess out as flash at the pinch-off, which packs the laminate; 3-5 under a bag. The notice compares the flash with what the groove holds." }
            isReal(definition.overchargePct, { (unitless) : [0, 4, 30] } as RealBoundSpec);

            annotation { "Name" : "Cloth waste %",
                         "Description" : "Cutting waste added to the woven and UD areas to buy. Limits 0-100 %. Typical 25-40 on a propeller: small tapered plies and the tabs." }
            isReal(definition.clothWastePct, { (unitless) : [0, 25, 100] } as RealBoundSpec);

            annotation { "Name" : "Resin waste %",
                         "Description" : "Resin mixed beyond what stays in the part: the pot, the brush and the flash. Limits 0-150 %. Typical 40-60 on a part this small." }
            isReal(definition.resinWastePct, { (unitless) : [0, 40, 150] } as RealBoundSpec);
        }

        annotation { "Group Name" : "Cut templates", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Cut templates", "Default" : false,
                         "Description" : "Makes a flat 1 mm plate under the mold with every fabric shape cut through it, as the Foil mold does: the blade skin's expanded outline, one outline per UD ply from the Layup fields (the first Tab plies with their tab below fold notches), the hub side as the panel between two roots, and the hub faces. The holes under each shape are its cut count. Right-click the plate's top face and Export as DXF for a cutter or a paper template; the log lists the shapes in order. Typical off until the design is settled." }
            definition.templatesOn is boolean;

            annotation { "Name" : "Trim margin",
                         "Description" : "Allowance added round every template outline, for trimming and the cloth's own slack. Limits 0-20 mm. Typical 3-5 on a propeller." }
            isLength(definition.tplMargin, { (millimeter) : [0, 4, 20] } as LengthBoundSpec);

            annotation { "Name" : "Template gap",
                         "Description" : "Space between neighbouring outlines on the plate. Limits 5-40 mm. Typical 10. The rows wrap at 280 mm, the width of an A3 sheet's short side." }
            isLength(definition.tplGap, { (millimeter) : [5, 10, 40] } as LengthBoundSpec);
        }

        annotation { "Name" : "Release check", "Default" : true,
                     "Description" : "Casts rays along the shaft axis on a polar grid over the propeller and reports where the part is seen more than twice, an undercut for the halves. Outside the well that should be nothing; inside it, the hub's sides beside each blade root, which the sectors form." }
        definition.releaseCheck is boolean;

        annotation { "Name" : "Log inputs", "Default" : true,
                     "Description" : "Prints every field of this feature as one MOLD_INPUTS line in the log (moldLog), so the whole set can be pasted elsewhere." }
        definition.logInputs is boolean;

        annotation { "Name" : "Keep log on error", "Default" : true,
                     "Description" : "When the mold fails, report the failure as a warning carrying the kernel's message and leave the log and notice variables standing, instead of the error that rolls them back. Typical on while the feature is being brought up." }
        definition.keepLogOnError is boolean;
    }
    {
        setVariable(context, "moldLog", "");
        setVariable(context, "moldNotice", "");
        propPrint(context, "moldLog", "==== Propeller mold feature, build " ~ PROP_FILE_BUILD ~ " ====");
        try
        {
        // build 99's fields, with their defaults for a feature made before them
        const grooveOn = definition.grooveOn != false;
        const landW = definition.landWidth == undefined ? 3 * millimeter : definition.landWidth;
        const grooveW = definition.grooveWidth == undefined ? 4 * millimeter : definition.grooveWidth;
        const grooveD = definition.grooveDepth == undefined ? 1.5 * millimeter : definition.grooveDepth;
        const bagOn = definition.closing == PropMoldClosing.BAG;
        const bagVac = definition.bagVac == undefined ? 85 : definition.bagVac;
        const ventOn = bagOn && definition.ventOn != false;
        const edgeRound = (bagOn && definition.edgeRound != undefined) ? definition.edgeRound : 0 * millimeter;
        const marksOn = definition.sectorMarks != false;
        const pullDia = definition.pullHole == undefined ? 3.3 * millimeter : definition.pullHole;
        // build 102's Layup fields
        const lay = { "skinPlies" : definition.skinPlies == undefined ? 1 : definition.skinPlies,
                      "skinGsm" : definition.skinGsm == undefined ? 200 : definition.skinGsm,
                      "udGsm" : definition.udGsm == undefined ? 300 : definition.udGsm,
                      "tabPlies" : definition.tabPlies == undefined ? 2 : definition.tabPlies,
                      "tab" : (definition.rootTab == undefined ? 12 * millimeter : definition.rootTab) / millimeter,
                      "vf" : (definition.fibreVolPct == undefined ? 50 : definition.fibreVolPct) / 100,
                      "over" : (definition.overchargePct == undefined ? 4 : definition.overchargePct) / 100,
                      "wasteC" : (definition.clothWastePct == undefined ? 25 : definition.clothWastePct) / 100,
                      "wasteR" : (definition.resinWastePct == undefined ? 40 : definition.resinWastePct) / 100 };
        if (definition.logInputs)
            propPrint(context, "moldLog", "MOLD_INPUTS: build=" ~ PROP_FILE_BUILD ~ "; flangeWidth_mm=" ~ toMm(definition.flangeWidth) ~ "; baseThick_mm=" ~ toMm(definition.baseThick) ~
                "; sectorBand_mm=" ~ toMm(definition.sectorBand) ~ "; rodFit_mm=" ~ toMm(definition.rodFit) ~ "; gapPoints=" ~ definition.gapPoints ~ "; releaseCheck=" ~ definition.releaseCheck ~
                "; grooveOn=" ~ grooveOn ~ "; landWidth_mm=" ~ toMm(landW) ~ "; grooveWidth_mm=" ~ toMm(grooveW) ~ "; grooveDepth_mm=" ~ toMm(grooveD) ~
                "; closing=" ~ (bagOn ? "BAG" : "CLAMPS") ~ "; bagVac_kPa=" ~ bagVac ~ "; ventOn=" ~ ventOn ~ "; edgeRound_mm=" ~ toMm(edgeRound) ~
                "; plugEngage_mm=" ~ (definition.plugEngage == undefined ? 2 : toMm(definition.plugEngage)) ~ "; sectorMarks=" ~ marksOn ~ "; pullHole_mm=" ~ toMm(pullDia) ~
                "; skinPlies=" ~ lay.skinPlies ~ "; skinGsm=" ~ lay.skinGsm ~ "; udGsm=" ~ lay.udGsm ~ "; tabPlies=" ~ lay.tabPlies ~ "; rootTab_mm=" ~ lay.tab ~ "; fibreVolPct=" ~ (lay.vf * 100) ~
                "; overchargePct=" ~ (lay.over * 100) ~ "; clothWastePct=" ~ (lay.wasteC * 100) ~ "; resinWastePct=" ~ (lay.wasteR * 100) ~
                "; templatesOn=" ~ (definition.templatesOn == true) ~ "; tplMargin_mm=" ~ (definition.tplMargin == undefined ? 4 : toMm(definition.tplMargin)) ~ "; tplGap_mm=" ~ (definition.tplGap == undefined ? 10 : toMm(definition.tplGap)));
        var model;
        try silent
        {
            model = getVariable(context, "propModel");
        }
        if (model == undefined || model.meanLines == undefined)
            throw regenError("Add a B-Series Propeller feature with Fixed blades above this one; build 72 or later publishes the model this mold reads");

        // ---- the part: selected, or the propeller the feature above named ----
        var part = qNothing();
        var nSel = 0;
        if (definition.part != undefined)
        {
            part = definition.part;
            nSel = size(evaluateQuery(context, part));
        }
        propPrint(context, "moldLog", "[mold] part selected: " ~ nSel);
        if (nSel != 1)
        {
            // the body the last B-Series Propeller feature stamped (fixed hub)
            const stamped = evaluateQuery(context, qHasAttribute("propModelBody"));
            propPrint(context, "moldLog", "[mold] bodies stamped by a Propeller feature: " ~ size(stamped));
            if (size(stamped) == 0)
                throw regenError("Select the propeller solid as Part: no body stamped by a B-Series Propeller feature (fixed hub, build 75 or later) was found", ["part"]);
            if (size(stamped) > 1)
                throw regenError("This Part Studio holds " ~ size(stamped) ~ " propellers: select the one to mold as Part", ["part"]);
            part = stamped[0];
        }
        propPrint(context, "moldLog", "[mold] part resolved");
        // the model of THIS part, carried on its body (build 87); the variable only as a fallback
        var frameNote = "";
        const own = getAttribute(context, { "entity" : part, "name" : "propModel" });
        if (own != undefined && own.meanLines != undefined)
            model = own;
        else
            frameNote = frameNote ~ " | WARNING: the part carries no model (a Propeller feature older than build 87?): the last published propModel is used and may not be this propeller";
        var partName = "";
        try silent
        {
            partName = toString(getProperty(context, { "entity" : part, "propertyType" : PropertyType.NAME }));
        }
        propPrint(context, "moldLog", "[mold] part: " ~ partName ~ "; its model: D " ~ model.D ~ ", Z " ~ model.Z ~ ", " ~ size(model.meanLines) ~ " stations, built by Propeller build " ~ model.build ~ (own != undefined ? " (carried on the part)" : " (the last published variable - a fallback)"));
        const partBoxD = evBox3d(context, { "topology" : part, "tight" : true });
        const partWidth = max(partBoxD.maxCorner[0] - partBoxD.minCorner[0], partBoxD.maxCorner[1] - partBoxD.minCorner[1]);
        if (partWidth > model.D * millimeter * 1.08 || partWidth < model.D * millimeter * 0.6)
            throw regenError("The model on the part (D " ~ model.D ~ " mm) does not match the part's size (" ~ toMm(partWidth) ~ " mm across): select the propeller body the B-Series Propeller feature made, not a copy or another body", ["part"]);
        // the part's own frame: its axis from the bore, pilot or hub cylinder (or the hub's cone), its height from the model
        var off = vector(0, 0, 0) * millimeter;
        var axisFound = false;
        const wantR = [model.bore / 2, model.hubRu, model.hubRd];
        for (var f in evaluateQuery(context, qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.CYLINDER)))
        {
            if (axisFound)
                break;
            const cyl = evSurfaceDefinition(context, { "face" : f });
            for (var wr in wantR)
            {
                if (wr > 0 && abs(cyl.radius - wr * millimeter) < 0.05 * millimeter && abs(abs(cyl.coordSystem.zAxis[2]) - 1) < 1e-4)
                {
                    off = vector(cyl.coordSystem.origin[0], cyl.coordSystem.origin[1], 0 * millimeter);
                    axisFound = true;
                    break;
                }
            }
        }
        if (!axisFound)
        {
            for (var f in evaluateQuery(context, qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.CONE)))
            {
                const cone = evSurfaceDefinition(context, { "face" : f });
                if (abs(abs(cone.coordSystem.zAxis[2]) - 1) < 1e-4)
                {
                    off = vector(cone.coordSystem.origin[0], cone.coordSystem.origin[1], 0 * millimeter);
                    axisFound = true;
                    break;
                }
            }
        }
        const partBox0 = evBox3d(context, { "topology" : part, "tight" : true });
        if (!axisFound)
        {
            off = vector((partBox0.minCorner[0] + partBox0.maxCorner[0]) / 2, (partBox0.minCorner[1] + partBox0.maxCorner[1]) / 2, 0 * millimeter);
            frameNote = frameNote ~ " | WARNING: no bore, pilot or hub cylinder on the part to take its axis from: the centre of its box is used";
        }
        if (model.zLo != undefined)
            off = vector(off[0], off[1], partBox0.minCorner[2] - model.zLo * millimeter);
        propPrint(context, "moldLog", "[mold] part's axis at x " ~ toMm(off[0]) ~ ", y " ~ toMm(off[1]) ~ " mm, z offset " ~ toMm(off[2]) ~ (axisFound ? " (from a cylinder or cone)" : " (box centre)"));
        // the mold is built on a copy of the part at the origin and placed back at the end
        opPattern(context, id + "partCopy", { "entities" : part, "transforms" : [transform(-off)], "instanceNames" : ["c"] });
        const partC = qCreatedBy(id + "partCopy", EntityType.BODY);

        // ---- the frame: the part as shown, shaft along Z, tail toward +Z ----
        const mm = millimeter;
        const Z = model.Z;
        const R = model.R * mm;
        const rHub = max(model.hubRu, model.hubRd) * mm;
        const rSect = rHub + model.fillet * mm + definition.sectorBand;
        const zWell0 = min(model.hubZa, model.hubZb) * mm;
        const zWell1 = max(model.hubZa, model.hubZb) * mm;
        const zMid = (zWell0 + zWell1) / 2;
        const pbox = evBox3d(context, { "topology" : partC, "tight" : true });
        const half = R + definition.flangeWidth;
        const lo = vector(-half, -half, pbox.minCorner[2] - definition.baseThick);
        const hi = vector(half, half, pbox.maxCorner[2] + definition.baseThick);
        const rOut = half * sqrt(2) + 5 * mm;
        propPrint(context, "moldLog", "[mold] part box " ~ toMm(pbox.maxCorner[0] - pbox.minCorner[0]) ~ " x " ~ toMm(pbox.maxCorner[1] - pbox.minCorner[1]) ~ " x " ~ toMm(pbox.maxCorner[2] - pbox.minCorner[2]) ~
            " mm; R " ~ toMm(R) ~ ", hub r " ~ toMm(rHub) ~ ", well r " ~ toMm(rSect) ~ " from z " ~ toMm(zWell0) ~ " to " ~ toMm(zWell1) ~ "; block " ~ toMm(2 * half) ~ " square, z " ~ toMm(lo[2]) ~ " to " ~ toMm(hi[2]));

        // ---- the blades' mean lines as (theta, z) arcs per station, unwrapped, running in +theta ----
        var arcs = [];
        var edges = [];                                           // every station's leading and trailing edge azimuth, the tip dome included
        for (var s = 0; s < size(model.meanLines); s += 1)
        {
            const pts = model.meanLines[s];
            var th = [];
            var zz = [];
            var prev = 0;
            for (var j = 0; j < size(pts); j += 1)
            {
                var t = atan2(pts[j][1], pts[j][0]) / radian;
                if (j > 0)
                {
                    while (t - prev > PI)
                        t -= 2 * PI;
                    while (t - prev < -PI)
                        t += 2 * PI;
                }
                th = append(th, t);
                zz = append(zz, pts[j][2]);
                prev = t;
            }
            const leFirst = th[size(th) - 1] >= th[0];               // the mean line runs leading edge to trailing edge
            if (th[size(th) - 1] < th[0])
            {
                th = reverse(th);
                zz = reverse(zz);
            }
            edges = append(edges, { "r" : model.stationR[s], "a" : th[0], "b" : th[size(th) - 1], "s" : s, "le" : (leFirst ? th[0] : th[size(th) - 1]), "te" : (leFirst ? th[size(th) - 1] : th[0]) });
            if (model.stationChord[s] < 4)
                continue;                                            // the tip dome: a chord too short for an arc
            arcs = append(arcs, { "r" : model.stationR[s], "th" : th, "z" : zz, "c" : model.stationChord[s] });
        }
        propPrint(context, "moldLog", "[mold] " ~ size(arcs) ~ " station arcs from the model (of " ~ size(model.meanLines) ~ "; chords under 4 mm on the tip dome left out)");
        const rIn = rHub - 2 * mm;                                // the parting follows the root's mean line to inside the hub
        var s1 = -1;
        for (var s = 0; s < size(arcs); s += 1)
        {
            if (arcs[s].r * mm >= rIn)
            {
                s1 = s;
                break;
            }
        }
        if (s1 < 0)
            throw regenError("No blade station reaches the hub: check the propeller", ["part"]);
        var inner = s1 == 0 ? arcs[0] : pmBlend(arcs[s1 - 1], arcs[s1], (rIn / mm - arcs[s1 - 1].r) / (arcs[s1].r - arcs[s1 - 1].r), 16);
        inner.r = rIn / mm;
        const tip = arcs[size(arcs) - 1];
        var zTipMean = 0;
        for (var v in tip.z)
            zTipMean += v / size(tip.z);
        // the loops, inside out: well centre, well edge, the sector radius, the stations (at most 14), the tip, two flats beyond
        var loops = [pmTowards(inner, rIn / mm - 4, zMid / mm, 0), pmTowards(inner, rIn / mm - 2.5, zMid / mm, 0.15), pmTowards(inner, rIn / mm - 1, zMid / mm, 0.5), inner];
        const nOuter = size(arcs) - 1 - s1;
        const step = max(1, floor(nOuter / 15));
        for (var s = s1; s < size(arcs) - 1; s += step)
            if (arcs[s].r > rIn / mm + 0.5)
                loops = append(loops, arcs[s]);
        loops = append(loops, tip);
        loops = append(loops, pmFlat(tip, R / mm + 10, zTipMean));
        loops = append(loops, pmFlat(tip, rOut / mm, zTipMean));
        propPrint(context, "moldLog", "[mold] loops chosen: " ~ size(loops) ~ " (three inside the hub, the loop 2 mm inside the hub surface, " ~ (size(loops) - 7) ~ " stations, the tip, two flats); tip mean z " ~ (round(zTipMean * 10) / 10) ~ " mm");

        // ---- the parting as a solid lower tool: radial planar sections every 5 deg under z(r, theta), two half lofts ----
        for (var L = 0; L < size(loops); L += 1)
        {
            const lp = loops[L];
            const nA = size(lp.th);
            propPrint(context, "moldLog", "[mold] loop " ~ L ~ " at r " ~ (round(lp.r * 10) / 10) ~ " mm: blade span " ~ round((lp.th[nA - 1] - lp.th[0]) * 180 / PI) ~ " deg, z " ~ (round(lp.z[0] * 10) / 10) ~ " to " ~ (round(lp.z[nA - 1] * 10) / 10));
        }
        const nAz = PM_N_AZ;
        const zFloor = lo[2] - 5 * mm;
        var rs = [0.3];                                           // the radial samples: every loop radius and the midpoints
        for (var L = 0; L < size(loops); L += 1)
        {
            if (L > 0)
                rs = append(rs, (loops[L - 1].r + loops[L].r) / 2);
            rs = append(rs, loops[L].r);
        }
        var sketchBodies = [];
        var toolParts = [];
        var regionAt = [];                                        // one sketch region per azimuth, 0 .. nAz (the last repeats 0)
        for (var j = 0; j <= nAz; j += 1)
        {
            const th = j * 2 * PI / nAz;
            const radial = vector(cos(th * radian), sin(th * radian), 0);
            const normal = vector(sin(th * radian), -cos(th * radian), 0);          // -tangential: with x = radial the sketch's y is +Z
            const sid = id + ("rad" ~ j);
            var sk = newSketchOnPlane(context, sid, { "sketchPlane" : plane(vector(0, 0, 0) * mm, normal, radial) });   // u = r, v = +z
            var pts = [];
            for (var rr in rs)
                pts = append(pts, vector(rr * mm, pmPartingZ(loops, rr, th, Z) * mm));
            skFitSpline(sk, "top", { "points" : pts });
            const pA = pts[size(pts) - 1];
            const pB = vector(pA[0], zFloor);
            const pC = vector(pts[0][0], zFloor);
            skLineSegment(sk, "outer", { "start" : pA, "end" : pB });
            skLineSegment(sk, "floor", { "start" : pB, "end" : pC });
            skLineSegment(sk, "axis", { "start" : pC, "end" : pts[0] });
            skSolve(sk);
            regionAt = append(regionAt, qSketchRegion(sid));
            sketchBodies = append(sketchBodies, qCreatedBy(sid, EntityType.BODY));
        }
        const nQ = 4;
        const perQ = nAz / nQ;
        var pairwiseNote = "";
        for (var q = 0; q < nQ; q += 1)
        {
            var regions = [];
            for (var j = q * perQ; j <= (q + 1) * perQ; j += 1)
                regions = append(regions, regionAt[j]);
            var ok = false;
            try silent
            {
                opLoft(context, id + ("toolQ" ~ q), { "profileSubqueries" : regions });
                ok = size(evaluateQuery(context, qBodyType(qCreatedBy(id + ("toolQ" ~ q), EntityType.BODY), BodyType.SOLID))) > 0;
            }
            if (ok)
                toolParts = append(toolParts, qCreatedBy(id + ("toolQ" ~ q), EntityType.BODY));
            else
            {
                try silent
                {
                    opDeleteBodies(context, id + ("dropQ" ~ q), { "entities" : qCreatedBy(id + ("toolQ" ~ q), EntityType.BODY) });
                }
                for (var j = q * perQ; j < (q + 1) * perQ; j += 1)
                {
                    const pid = id + ("toolP" ~ j);
                    opLoft(context, pid, { "profileSubqueries" : [regionAt[j], regionAt[j + 1]] });
                    toolParts = append(toolParts, qCreatedBy(pid, EntityType.BODY));
                }
                pairwiseNote = pairwiseNote ~ " quarter " ~ q ~ " pairwise;";
            }
            propPrint(context, "moldLog", "[mold] tool quarter " ~ q ~ ": " ~ (ok ? "lofted from " ~ size(regions) ~ " radial sections" : "pairwise ruled lofts"));
        }
        opBoolean(context, id + "toolJoin", { "tools" : qUnion(toolParts), "operationType" : BooleanOperationType.UNION });
        opDeleteBodies(context, id + "dropRadial", { "entities" : qUnion(sketchBodies) });
        if (size(evaluateQuery(context, qBodyType(qUnion(toolParts), BodyType.SOLID))) != 1)
            throw regenError("The parting tool did not join into one solid (" ~ size(evaluateQuery(context, qBodyType(qUnion(toolParts), BodyType.SOLID))) ~ " bodies)" ~ pairwiseNote);
        const toolBody = qUnion(evaluateQuery(context, qBodyType(qUnion(toolParts), BodyType.SOLID)));   // the solid below the parting, kept until the end
        const moldSolids = qSubtraction(qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID), qUnion([partC, toolBody]));
        // the sectors' side: the half that pulls toward the hub's bigger end cannot form the hub's side
        const sectorsLow = model.hubRu >= model.hubRd;          // the lower half when the hub's lower-Z face is the bigger
        // build 105: an imported part may have its nose (upstream) end at +Z; the halves and the notice are named by it
        const noseUp = model.noseLow == false;
        const secSide = (sectorsLow != noseUp) ? "nose" : "tail";
        const freeSide = secSide == "nose" ? "tail" : "nose";
        propPrint(context, "moldLog", "[mold] lower tool joined; block, halves");

        // ---- the halves: tail half = block minus the tool, nose half = a second block intersected with it ----
        pmBox(context, id + "blockA", lo, hi);
        pmBox(context, id + "blockB", lo, hi);
        // bag closing: round the blocks' outer edges before anything is cut from them (the Foil mold's way)
        var roundNote = "";
        if (edgeRound > 0.1 * mm)
        {
            var okRound = false;
            try silent
            {
                opFillet(context, id + "roundA", { "entities" : qCreatedBy(id + "blockA", EntityType.EDGE), "radius" : edgeRound });
                opFillet(context, id + "roundB", { "entities" : qCreatedBy(id + "blockB", EntityType.EDGE), "radius" : edgeRound });
                okRound = true;
            }
            roundNote = okRound ? "; outer edges rounded " ~ toMm(edgeRound) ~ " mm" : "; EDGE ROUND FAILED (the blocks are sharp-edged: pad the corners under the bag)";
            propPrint(context, "moldLog", "[mold] edge round " ~ toMm(edgeRound) ~ " mm: " ~ (okRound ? "done" : "failed"));
        }
        opBoolean(context, id + "tailHalf", { "targets" : qCreatedBy(id + "blockA", EntityType.BODY), "tools" : toolBody, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
        opPattern(context, id + "toolCopyN", { "entities" : toolBody, "transforms" : [identityTransform()], "instanceNames" : ["n"] });
        opBoolean(context, id + "noseHalf", { "tools" : qUnion([qCreatedBy(id + "blockB", EntityType.BODY), qCreatedBy(id + "toolCopyN", EntityType.BODY)]), "operationType" : BooleanOperationType.INTERSECTION });
        // the tool's sections start 0.3 mm off the axis: clear the axis through both halves so no hair of block survives there
        const axisQ = pmCylinder(context, id + "axisClear", lo[2] - 1 * mm, hi[2] + 1 * mm, 0.6 * mm);
        opBoolean(context, id + "cutAxis", { "targets" : qSubtraction(moldSolids, axisQ), "tools" : axisQ, "operationType" : BooleanOperationType.SUBTRACTION });
        const solids = moldSolids;
        if (size(evaluateQuery(context, solids)) != 2)
            throw regenError("The parting tool did not leave two halves (" ~ size(evaluateQuery(context, solids)) ~ " solids)");
        propPrint(context, "moldLog", "[mold] block split into two halves; well, cavity, rod");
        pmLogBodies(context, "after the halves (mold solids, tool excluded)", moldSolids);
        pmLogBodies(context, "the parting tool", toolBody);
        const hv0 = pmHalves(context, moldSolids, lo, hi);
        if (hv0.nUpper != 1 || hv0.nLower != 1)
            throw regenError("The halves did not come out as one touching the block's top and one its bottom (" ~ hv0.nUpper ~ " / " ~ hv0.nLower ~ ")");
        const wellQ = pmCylinder(context, id + "well", zWell0, zWell1, rSect);
        opBoolean(context, id + "cutWell", { "targets" : (sectorsLow ? hv0.lower : hv0.upper), "tools" : wellQ, "operationType" : BooleanOperationType.SUBTRACTION });
        propPrint(context, "moldLog", "[mold] well cut (r " ~ toMm(rSect) ~ " mm, z " ~ toMm(zWell0) ~ " to " ~ toMm(zWell1) ~ "); cavity");
        opBoolean(context, id + "cavity", { "targets" : moldSolids, "tools" : partC, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
        propPrint(context, "moldLog", "[mold] well and cavity cut; naming the halves");
        pmLogBodies(context, "after the well, cavity and axis cuts", moldSolids);
        var hv = pmHalves(context, moldSolids, lo, hi);
        for (var b in evaluateQuery(context, moldSolids))
        {
            const bb = evBox3d(context, { "topology" : b, "tight" : true });
            propPrint(context, "moldLog", "[mold] half: z " ~ toMm(bb.minCorner[2]) ~ " to " ~ toMm(bb.maxCorner[2]) ~ " mm");
        }
        if (hv.nUpper != 1 || hv.nLower != 1)
            throw regenError("The halves did not come out as one touching the block's top and one its bottom (" ~ hv.nUpper ~ " / " ~ hv.nLower ~ ")");
        if (hv.nUpper == 1)
            setProperty(context, { "entities" : hv.upper, "propertyType" : PropertyType.NAME, "value" : noseUp ? "Prop mold, nose half (+Z)" : "Prop mold, tail half (+Z)" });
        if (hv.nLower == 1)
            setProperty(context, { "entities" : hv.lower, "propertyType" : PropertyType.NAME, "value" : noseUp ? "Prop mold, tail half (-Z)" : "Prop mold, nose half (-Z)" });

        // ---- the hub sectors: the ring round the hub between its faces, the part cut from it, split at the blade mid-azimuths ----
        if (definition.keyDia > definition.sectorBand - 2 * mm)
            throw regenError("Key dia must be under Sector band less 2 mm", ["keyDia", "sectorBand"]);
        if (definition.keyHeight > definition.baseThick - 4 * mm)
            throw regenError("Key height must be under Base thick less 4 mm", ["keyHeight", "baseThick"]);
        var halvesQ = qUnion(evaluateQuery(context, moldSolids));   // the two halves as concrete bodies, before the ring exists
        const ringQ = pmCylinder(context, id + "ring", zWell0, zWell1, rSect - definition.sectorFit);
        // build 105: the ring starts at the rod, so a recess open to the sectors' end face (a groove, a counterbore, a pin
        // slot) is formed by a plug on that half rather than left unformed or cut up into sectors
        const rRingIn = model.bore > 0 ? max((model.bore * mm + definition.rodFit) / 2, 0.5 * mm) : 0.5 * mm;
        const ringInQ = pmCylinder(context, id + "ringIn", zWell0 - 1 * mm, zWell1 + 1 * mm, rRingIn);
        opBoolean(context, id + "ringBore", { "targets" : ringQ, "tools" : ringInQ, "operationType" : BooleanOperationType.SUBTRACTION });
        opBoolean(context, id + "ringCavity", { "targets" : ringQ, "tools" : partC, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
        // the ring's pieces: the slivers the dish or the taper leaves inside it go first, then each piece is trimmed to the sectors' side
        var ringPieces = [];
        var ringSlivers = [];
        for (var b in evaluateQuery(context, qBodyType(ringQ, BodyType.SOLID)))
        {
            const sb = evBox3d(context, { "topology" : b, "tight" : true });
            if (sb.maxCorner[2] - sb.minCorner[2] < 0.2 * (zWell1 - zWell0))
                ringSlivers = append(ringSlivers, b);
            else
                ringPieces = append(ringPieces, b);
        }
        if (size(ringSlivers) > 0)
            opDeleteBodies(context, id + "dropRingSlivers", { "entities" : qUnion(ringSlivers) });
        propPrint(context, "moldLog", "[mold] ring: " ~ size(ringPieces) ~ " piece(s), " ~ size(ringSlivers) ~ " sliver(s) from the hub's dish or taper deleted");
        for (var k = 0; k < size(ringPieces); k += 1)
        {
            opPattern(context, id + ("toolCopyR" ~ k), { "entities" : toolBody, "transforms" : [identityTransform()], "instanceNames" : ["r"] });
            if (sectorsLow)
                opBoolean(context, id + ("ringSide" ~ k), { "tools" : qUnion([ringPieces[k], qCreatedBy(id + ("toolCopyR" ~ k), EntityType.BODY)]), "operationType" : BooleanOperationType.INTERSECTION });
            else
                opBoolean(context, id + ("ringSide" ~ k), { "targets" : ringPieces[k], "tools" : qCreatedBy(id + ("toolCopyR" ~ k), EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
        }
        // build 105: a ring piece that reaches the sectors' end face but not the well's wall is a plug in a recess of the
        // hub's end: checked along the axis and joined to the half on that side, which pulls it out
        var plugs = [];
        var plugNote = "";
        var plugNote2 = "";
        for (var b in evaluateQuery(context, qBodyType(qSubtraction(moldSolids, halvesQ), BodyType.SOLID)))
        {
            const pb = evBox3d(context, { "topology" : b, "tight" : true });
            const reach = max(max(abs(pb.minCorner[0]), abs(pb.maxCorner[0])), max(abs(pb.minCorner[1]), abs(pb.maxCorner[1])));
            const atEnd = sectorsLow ? (pb.minCorner[2] <= zWell0 + 0.5 * mm) : (pb.maxCorner[2] >= zWell1 - 0.5 * mm);
            if (atEnd && reach < rSect - definition.sectorFit - 1 * mm)
                plugs = append(plugs, b);
        }
        // build 107: with a hub core the plug only passes the skin and Plug engage into the core's printed recess
        const plugEngage = definition.plugEngage == undefined ? 2 * mm : definition.plugEngage;
        var plugDepthNote = "";
        if (size(plugs) > 0 && model.core == 1 && model.skin != undefined && model.skin > 0)
        {
            const depth = model.skin * mm + plugEngage;
            // everything deeper than the skin and the engagement is cut away
            const deepQ = sectorsLow ? pmCylinder(context, id + "plugDeep", zWell0 + depth, zWell1 + 1 * mm, rSect)
                                     : pmCylinder(context, id + "plugDeep", zWell0 - 1 * mm, zWell1 - depth, rSect);
            opBoolean(context, id + "plugTrim", { "targets" : qUnion(plugs), "tools" : deepQ, "operationType" : BooleanOperationType.SUBTRACTION });
            plugs = [];
            for (var b in evaluateQuery(context, qBodyType(qSubtraction(moldSolids, halvesQ), BodyType.SOLID)))
            {
                const pb = evBox3d(context, { "topology" : b, "tight" : true });
                const reach = max(max(abs(pb.minCorner[0]), abs(pb.maxCorner[0])), max(abs(pb.minCorner[1]), abs(pb.maxCorner[1])));
                const atEnd = sectorsLow ? (pb.minCorner[2] <= zWell0 + 0.5 * mm) : (pb.maxCorner[2] >= zWell1 - 0.5 * mm);
                if (atEnd && reach < rSect - definition.sectorFit - 1 * mm)
                    plugs = append(plugs, b);
            }
            plugDepthNote = ", cut to " ~ toMm(depth) ~ " mm deep (the skin and " ~ toMm(plugEngage) ~ " mm into the core's printed recess)";
        }
        if (size(plugs) > 0)
        {
            const sideHalf = sectorsLow ? hv.lower : hv.upper;
            const away = vector(0, 0, sectorsLow ? -1 : 1);
            var worstPlug = 0;
            var nChecked = 0;
            for (var k = 0; k < size(plugs); k += 1)
            {
                for (var d in [1, 8])
                {
                    const ov = pmPullOverlap(context, id + ("plugRel" ~ k ~ "_" ~ d), plugs[k], partC, away * d * mm);
                    if (ov >= 0)
                    {
                        nChecked += 1;
                        worstPlug = max(worstPlug, ov);
                    }
                }
            }
            var vPlugs = 0;
            for (var pq in plugs)
                vPlugs += evVolume(context, { "entities" : pq }) / (mm * mm * mm);
            opBoolean(context, id + "plugsJoin", { "tools" : qUnion(concatenateArrays([evaluateQuery(context, sideHalf), plugs])), "operationType" : BooleanOperationType.UNION });
            // the union may hand the half a new identity: find both halves again by the block's faces
            hv = pmHalves(context, moldSolids, lo, hi);
            halvesQ = qUnion(concatenateArrays([evaluateQuery(context, hv.upper), evaluateQuery(context, hv.lower)]));
            if (hv.nUpper == 1)
                setProperty(context, { "entities" : hv.upper, "propertyType" : PropertyType.NAME, "value" : noseUp ? "Prop mold, nose half (+Z)" : "Prop mold, tail half (+Z)" });
            if (hv.nLower == 1)
                setProperty(context, { "entities" : hv.lower, "propertyType" : PropertyType.NAME, "value" : noseUp ? "Prop mold, tail half (-Z)" : "Prop mold, nose half (-Z)" });
            plugNote = " | " ~ size(plugs) ~ " plug(s) in recesses of the hub's " ~ secSide ~ " end (" ~ (round(vPlugs * 10) / 10) ~ " mm3" ~ plugDepthNote ~ ") joined to the " ~ secSide ~ " half, pulled along the axis: " ~
                (nChecked == 0 ? "not checked" : (worstPlug < 0.3 ? "clear" : (worstPlug < 5 ? "a graze of " ~ (round(worstPlug * 10) / 10) ~ " mm3" : "HOOKED " ~ (round(worstPlug * 10) / 10) ~ " mm3: the recess has an undercut along the axis")));
            plugNote2 = " and the plugs";
            propPrint(context, "moldLog", "[mold]" ~ plugNote);
        }
        // the cuts run along each blade's two edge lines (the station arcs' ends) from inside the hub out past the well,
        // so each blade gets a root sector under it and the gaps their own: 2 Z sectors
        const rCut0 = min(model.hubRu, model.hubRd) * mm - 3 * mm;
        const rCut1 = rSect + 2 * mm;
        // room between neighbouring roots at the well radius: narrower than a key and its margin, and the gap sectors
        // would be fragments, so the ring is cut at one edge per blade and each sector carries a root and its gap
        var sNear = 0;
        for (var sI = 0; sI < size(arcs); sI += 1)
            if (abs(arcs[sI].r * mm - rSect) < abs(arcs[sNear].r * mm - rSect))
                sNear = sI;
        const gapAngle = 2 * PI / Z - (arcs[sNear].th[size(arcs[sNear].th) - 1] - arcs[sNear].th[0]);
        const gapWidth = rSect * gapAngle;
        const bothEdges = true;
        const narrowGaps = gapWidth < definition.keyDia + 2 * mm;
        propPrint(context, "moldLog", "[mold] gap between neighbouring roots at the well radius " ~ toMm(gapWidth) ~ " mm: cuts at both edges of every blade, a root sector and a gap sector each" ~ (narrowGaps ? "; the gap sectors are too narrow for a key and are held by their neighbours" : ""));
        var cutTools = [];
        for (var k = 0; k < Z; k += 1)
        {
            const rot = k * 2 * PI / Z;
            for (var e = 0; e < (bothEdges ? 2 : 1); e += 1)
            {
                var pts = [];
                var sBelow = -1;
                var sAbove = -1;
                for (var sI = 0; sI < size(arcs); sI += 1)
                {
                    const rr = arcs[sI].r * mm;
                    if (rr < rCut0)
                        sBelow = sI;
                    else if (rr > rCut1 && sAbove < 0)
                        sAbove = sI;
                }
                const sStart = max(sBelow, 0);
                const sEnd = sAbove < 0 ? size(arcs) - 1 : sAbove;
                for (var sI = sStart; sI <= sEnd; sI += 1)
                {
                    const a = arcs[sI];
                    const th = (e == 0 ? a.th[0] : a.th[size(a.th) - 1]) + rot;
                    pts = append(pts, vector(a.r * mm * cos(th * radian), a.r * mm * sin(th * radian)));
                }
                // extend the line straight past both ends so the slab clears the ring
                const dIn = normalize(pts[0] - pts[1]);
                const dOut = normalize(pts[size(pts) - 1] - pts[size(pts) - 2]);
                pts = concatenateArrays([[pts[0] + dIn * 4 * mm], pts, [pts[size(pts) - 1] + dOut * 4 * mm]]);
                cutTools = append(cutTools, pmSlabPoly(context, id + ("sectorCut" ~ k ~ "_" ~ e), pts, 0.1 * mm, zWell0 - 1 * mm, zWell1 + 1 * mm));
            }
        }
        propPrint(context, "moldLog", "[mold] ring round the hub cut by the part; " ~ size(cutTools) ~ " sector cuts along the blades' edges");
        pmLogBodies(context, "the ring on the sectors' side, before the cuts", qSubtraction(moldSolids, qUnion(concatenateArrays([[halvesQ], cutTools]))));
        opBoolean(context, id + "sectorCuts", { "targets" : qSubtraction(qSubtraction(moldSolids, halvesQ), qUnion(cutTools)), "tools" : qUnion(cutTools), "operationType" : BooleanOperationType.SUBTRACTION });
        pmLogBodies(context, "after the sector cuts", qSubtraction(moldSolids, halvesQ));
        // the rod hole, through the halves and the ring: the bore's plug leaves the ring with it
        var rodNote = "; no bore on the hub, no rod";
        if (model.bore > 0)
        {
            const rodQ = pmCylinder(context, id + "rod", lo[2] - 1 * mm, hi[2] + 1 * mm, (model.bore * mm + definition.rodFit) / 2);
            opBoolean(context, id + "cutRod", { "targets" : qSubtraction(moldSolids, rodQ), "tools" : rodQ, "operationType" : BooleanOperationType.SUBTRACTION });
            rodNote = "; rod hole " ~ toMm(model.bore * mm + definition.rodFit) ~ " mm through both halves and the sectors for a " ~ toMm(model.bore * mm) ~ " mm rod";
        }
        var sectorsAll = evaluateQuery(context, qSubtraction(moldSolids, halvesQ));
        var sectors = [];
        var slivers = [];
        for (var b in sectorsAll)
        {
            const sb = evBox3d(context, { "topology" : b, "tight" : true });
            const reachesFace = sectorsLow ? (sb.minCorner[2] <= zWell0 + 0.5 * mm) : (sb.maxCorner[2] >= zWell1 - 0.5 * mm);
            if (sb.maxCorner[2] - sb.minCorner[2] < 0.2 * (zWell1 - zWell0) || !reachesFace)
                slivers = append(slivers, b);                 // the dish's void inside the ring, or a fragment that never reaches the sectors' face
            else
                sectors = append(sectors, b);
        }
        if (size(slivers) > 0)
            opDeleteBodies(context, id + "dropSlivers", { "entities" : qUnion(slivers) });
        propPrint(context, "moldLog", "[mold] " ~ size(sectors) ~ " sector bodies (expected " ~ size(cutTools) ~ ")" ~ (size(slivers) > 0 ? "; " ~ size(slivers) ~ " deleted: slivers at the dish rim, or closed pockets under the blade roots beside the hub's taper that no piece can form (the laminate bridges them)" : ""));
        // keys: a frustum under each sector at its mid-azimuth, the socket in the nose half's floor
        const rKey = (rHub + rSect) / 2;
        const keyR = definition.keyDia / 2;
        const keyTipR = keyR - definition.keyHeight * tan(12 * degree);
        var nKeys = 0;
        for (var k = 0; k < size(sectors); k += 1)
        {
            const bx = evBox3d(context, { "topology" : sectors[k], "tight" : true });
            const cx = (bx.minCorner[0] + bx.maxCorner[0]) / 2;
            const cy = (bx.minCorner[1] + bx.maxCorner[1]) / 2;
            const thS = atan2(cy, cx);
            const kx = rKey * cos(thS);
            const ky = rKey * sin(thS);
            setProperty(context, { "entities" : sectors[k], "propertyType" : PropertyType.NAME, "value" : "Prop mold, hub sector " ~ (k + 1) ~ " of " ~ size(sectors) });
            // a sector narrower across than the key and its margin gets no key: it is held by its neighbours
            const wide = max(bx.maxCorner[0] - bx.minCorner[0], bx.maxCorner[1] - bx.minCorner[1]);
            if (wide < definition.keyDia + 2 * mm)
                continue;
            // the socket first (0.2 mm larger, 0.3 mm deeper), then the key joined to the sector: down into the nose half's
            // well floor, or up into the tail half's ceiling when the sectors sit on the tail side
            const sgnK = sectorsLow ? -1 : 1;
            const zF = sectorsLow ? zWell0 : zWell1;
            const sockQ = pmFrustum(context, id + ("sock" ~ k), kx, ky, zF + sgnK * (definition.keyHeight + 0.3 * mm), keyTipR + 0.1 * mm, zF - sgnK * 0.01 * mm, keyR + 0.1 * mm);
            opBoolean(context, id + ("sockCut" ~ k), { "targets" : (sectorsLow ? hv.lower : hv.upper), "tools" : sockQ, "operationType" : BooleanOperationType.SUBTRACTION });
            const keyQ = pmFrustum(context, id + ("key" ~ k), kx, ky, zF + sgnK * definition.keyHeight, keyTipR, zF - sgnK * 0.5 * mm, keyR);
            opBoolean(context, id + ("keyJoin" ~ k), { "tools" : qUnion([sectors[k], keyQ]), "operationType" : BooleanOperationType.UNION });
            nKeys += 1;
        }
        propPrint(context, "moldLog", "[mold] " ~ nKeys ~ " sector keys " ~ toMm(definition.keyDia) ~ " mm into the " ~ (secSide ~ " half's well " ~ (sectorsLow ? "floor" : "ceiling")));

        // ---- half keys at the flange corners, where the parting is flat at the tip level ----
        const z0 = zTipMean * mm;
        const hkR = definition.halfKeyDia / 2;
        const hkTip = hkR - definition.keyHeight * tan(12 * degree);
        var nHalfKeys = 0;
        for (var c = 0; c < 4; c += 1)
        {
            const sx = (c == 0 || c == 3) ? 1 : -1;
            const sy = (c < 2) ? 1 : -1;
            const kx = sx * (half - 12 * mm);
            const ky = sy * (half - 12 * mm);
            const sockQ = pmFrustum(context, id + ("hsock" ~ c), kx, ky, z0 - definition.keyHeight - 0.3 * mm, hkTip + 0.1 * mm, z0 + 0.01 * mm, hkR + 0.1 * mm);
            opBoolean(context, id + ("hsockCut" ~ c), { "targets" : hv.lower, "tools" : sockQ, "operationType" : BooleanOperationType.SUBTRACTION });
            const bossQ = pmFrustum(context, id + ("hkey" ~ c), kx, ky, z0 - definition.keyHeight, hkTip, z0 + 0.5 * mm, hkR);
            opBoolean(context, id + ("hkeyJoin" ~ c), { "tools" : qUnion([hv.upper, bossQ]), "operationType" : BooleanOperationType.UNION });
            nHalfKeys += 1;
        }
        propPrint(context, "moldLog", "[mold] " ~ nHalfKeys ~ " half keys " ~ toMm(definition.halfKeyDia) ~ " mm at the flange corners");
        pmLogBodies(context, "after the keys", moldSolids);

        // ---- the layup: the laminate is the part less its core; plies from the section thickness ----
        var layNote = "";
        const vPart = evVolume(context, { "entities" : partC }) / (mm * mm * mm) / 1000;
        var vCore = 0;
        if (model.core == 1 && model.tag != undefined)
        {
            const cores = evaluateQuery(context, qHasAttributeWithValue("propCoreBody", model.tag));
            if (size(cores) > 0)
                vCore = evVolume(context, { "entities" : cores[size(cores) - 1] }) / (mm * mm * mm) / 1000;
        }
        const vLam = vPart - vCore;
        const laid = pmLayup(model, vPart, vCore, lay);
        layNote = laid.note;
        propPrint(context, "moldLog", "[mold]" ~ layNote);

        // ---- build 99: the sectors by kind, their marks and their pull holes ----
        const bedHalf = sectorsLow ? hv.lower : hv.upper;         // the half the plies are laid on: the sectors' side
        const zFace = sectorsLow ? zWell0 : zWell1;               // the well floor the sectors sit on
        const into = sectorsLow ? 1 : -1;                         // from that face into the sector
        const spanZ = 2 * PI / Z;
        const nearArc = arcs[sNear];
        const bladeSpan = nearArc.th[size(nearArc.th) - 1] - nearArc.th[0];
        const sectorsNow = pmFloating(context, moldSolids, lo, hi);
        var sectorInfo = [];
        var dotXY = [];
        var pullT = [];
        var nMarked = 0;
        var nMarkedRoot = 0;
        const pullDepth = min(definition.sectorBand - 2 * mm, 8 * mm);
        for (var k = 0; k < size(sectorsNow); k += 1)
        {
            const bx = evBox3d(context, { "topology" : sectorsNow[k], "tight" : true });
            const cx = (bx.minCorner[0] + bx.maxCorner[0]) / 2;
            const cy = (bx.minCorner[1] + bx.maxCorner[1]) / 2;
            const thS = atan2(cy, cx) / radian;
            var rel = thS - nearArc.th[0];
            rel = rel - 2 * PI * floor(rel / (2 * PI));
            const kB = min(floor(rel / spanZ), Z - 1);
            const uS = rel - kB * spanZ;
            const isRoot = uS <= bladeSpan;
            const halfW = (isRoot ? bladeSpan : spanZ - bladeSpan) / 2;
            const thC = nearArc.th[0] + kB * spanZ + (isRoot ? bladeSpan / 2 : bladeSpan + (spanZ - bladeSpan) / 2);
            const wide = max(bx.maxCorner[0] - bx.minCorner[0], bx.maxCorner[1] - bx.minCorner[1]);
            setProperty(context, { "entities" : sectorsNow[k], "propertyType" : PropertyType.NAME, "value" : "Prop mold, " ~ (isRoot ? "root" : "gap") ~ " sector " ~ (kB + 1) ~ " of " ~ Z });
            sectorInfo = append(sectorInfo, { "th" : thS, "thC" : thC, "root" : isRoot, "n" : kB + 1, "wide" : wide });
            if (marksOn && wide >= definition.keyDia + 2 * mm)
            {
                // dots alternate either side of the key along the band's mid radius; they must fit inside the sector's own arc
                const nD = kB + 1;
                var dC = thS - thC;
                dC = dC - 2 * PI * round(dC / (2 * PI));
                // two deep across the band: dots 0, 1 on the outer row either side of the key, 2, 3 on the inner row, then the next column
                const first = definition.keyDia / 2 + 2.2 * mm;
                const need = first + floor((nD - 1) / 4) * 2.6 * mm + 0.8 * mm;
                const room = min((halfW - dC) * rKey, (halfW + dC) * rKey) - 1.5 * mm;
                if (need <= room && definition.sectorBand >= 7 * mm)
                {
                    for (var i = 0; i < nD; i += 1)
                    {
                        const side = (i % 2 == 0) ? 1 : -1;
                        const rD = rKey + ((floor(i / 2) % 2 == 0) ? 1.6 : -1.6) * mm;
                        const thD = thS + side * (first + floor(i / 4) * 2.6 * mm) / rKey;
                        dotXY = append(dotXY, vector(rD * cos(thD * radian), rD * sin(thD * radian), 0 * mm));
                    }
                    nMarked += 1;
                    if (isRoot)
                        nMarkedRoot += 1;
                }
            }
        }

        // ---- the sectors' own release: each along its pull, a copy moved outward must lose nothing to the part ----
        // one root and one gap sector are scanned over the directions; the best of each kind is the pull for all of that kind
        var bestDegRoot = 0;
        var bestDegGap = 0;
        var sectorRelNote = "";
        if (definition.releaseCheck && size(sectorsNow) > 0)
        {
            var nClear = 0;
            var nGraze = 0;
            var nUnknown = 0;
            var hooked = "";
            var worst = 0;
            for (var pass = 0; pass < 2; pass += 1)
            {
                // pass 0: the first sector of each kind, every direction; pass 1: the others along their kind's best
                var seenRoot = false;
                var seenGap = false;
                for (var k = 0; k < size(sectorsNow); k += 1)
                {
                    const isR = sectorInfo[k].root;
                    const firstOfKind = isR ? !seenRoot : !seenGap;
                    if (isR)
                        seenRoot = true;
                    else
                        seenGap = true;
                    if ((pass == 0) != firstOfKind)
                        continue;
                    const dirs = pass == 0 ? [0, 10, -10, 20, -20, 30, -30] : [isR ? bestDegRoot : bestDegGap];
                    var best = -1;
                    var bestDeg = 0;
                    var tried = "";
                    for (var aDeg in dirs)
                    {
                        var tot = 0;
                        var failed = false;
                        for (var stp = 0; stp < 2; stp += 1)
                        {
                            const dist = stp == 0 ? 1 * mm : definition.sectorBand;
                            var ov = -1;
                            for (var tryN = 0; tryN < 5 && ov < 0; tryN += 1)
                            {
                                const dirT = sectorInfo[k].thC + (aDeg + [0, 1, -1, 2, -2][tryN]) * PI / 180;
                                ov = pmPullOverlap(context, id + ("rel" ~ pass ~ "k" ~ k ~ "a" ~ (aDeg + 30) ~ "s" ~ stp ~ "t" ~ tryN), sectorsNow[k], partC, vector(cos(dirT * radian), sin(dirT * radian), 0) * dist);
                            }
                            if (ov < 0)
                                failed = true;
                            else
                                tot += ov;
                        }
                        tried = tried ~ (tried == "" ? "" : ", ") ~ aDeg ~ " deg: " ~ (failed ? "not checked" : (round(tot * 10) / 10) ~ " mm3");
                        if (failed)
                            continue;
                        if (best < 0 || tot < best - 1e-6)
                        {
                            best = tot;
                            bestDeg = aDeg;
                        }
                    }
                    if (pass == 0)
                    {
                        if (isR)
                            bestDegRoot = bestDeg;
                        else
                            bestDegGap = bestDeg;
                    }
                    const sName = (isR ? "root " : "gap ") ~ sectorInfo[k].n;
                    if (best < 0)
                        nUnknown += 1;
                    else if (best < 0.3)
                        nClear += 1;
                    else if (best < 5)
                        nGraze += 1;
                    else
                        hooked = hooked ~ (hooked == "" ? "" : ", ") ~ sName ~ " " ~ (round(best * 10) / 10) ~ " mm3";
                    if (best > worst)
                        worst = best;
                    propPrint(context, "moldLog", "[mold] sector " ~ sName ~ " pulled 1 mm and " ~ toMm(definition.sectorBand) ~ " mm, overlap with the part by direction from radial (+ toward +theta): " ~ tried ~
                        (best < 0 ? "" : " -> " ~ bestDeg ~ " deg, " ~ (best < 0.3 ? "clear" : (best < 5 ? "a graze" : "HOOKS"))));
                }
            }
            sectorRelNote = "; sectors along their own pull (1 mm and " ~ toMm(definition.sectorBand) ~ " mm out; root sectors " ~ bestDegRoot ~ " deg from radial, gap sectors " ~ bestDegGap ~ " deg, + toward +theta): " ~
                nClear ~ " of " ~ size(sectorsNow) ~ " clear" ~ (nGraze > 0 ? ", " ~ nGraze ~ " graze the part (up to " ~ (round(worst * 10) / 10) ~ " mm3: inside a printed fit, ease the flank if it binds)" : "") ~
                (hooked != "" ? "; HOOKED: " ~ hooked ~ " - widen the Sector band or split that sector" : "") ~ (nUnknown > 0 ? "; " ~ nUnknown ~ " not checked" : "");
            propPrint(context, "moldLog", "[mold]" ~ sectorRelNote);
        }
        // pull holes along each kind's pull direction, at mid height of the sector's outer face
        if (pullDia > 0.1 * mm && pullDepth >= 2 * mm)
        {
            for (var k = 0; k < size(sectorsNow); k += 1)
            {
                const thP = sectorInfo[k].thC;
                const zP = pmPartingZ(loops, rSect / mm, thP, Z) * mm;
                if (abs(zP - zFace) >= pullDia + 3 * mm && sectorInfo[k].wide >= pullDia + 4 * mm)
                {
                    // the hole enters the outer face at the sector's mid azimuth and runs inward along the pull direction
                    const tilt = (sectorInfo[k].root ? bestDegRoot : bestDegGap) * PI / 180;
                    const pOut = vector((rSect + 1 * mm) * cos(thP * radian), (rSect + 1 * mm) * sin(thP * radian), (zP + zFace) / 2);
                    const dIn = vector(-cos((thP + tilt) * radian), -sin((thP + tilt) * radian), 0);
                    pullT = append(pullT, { "top" : pOut, "bottom" : pOut + dIn * (pullDepth + 1 * mm + definition.sectorFit) });
                }
            }
        }
        var okDots = size(dotXY) == 0;
        if (size(dotXY) > 0)
        {
            try silent
            {
                const dS = pmFrustum(context, id + "dotS", 0 * mm, 0 * mm, zFace - into * 0.05 * mm, 0.8 * mm, zFace + into * 0.6 * mm, 0.8 * mm);
                const dH = pmFrustum(context, id + "dotH", 0 * mm, 0 * mm, zFace + into * 0.05 * mm, 0.8 * mm, zFace - into * 0.6 * mm, 0.8 * mm);
                var trD = [];
                var nmD = [];
                for (var i = 0; i < size(dotXY); i += 1)
                {
                    trD = append(trD, transform(dotXY[i]));
                    nmD = append(nmD, "d" ~ i);
                }
                opPattern(context, id + "dotsS", { "entities" : dS, "transforms" : trD, "instanceNames" : nmD });
                opPattern(context, id + "dotsH", { "entities" : dH, "transforms" : trD, "instanceNames" : nmD });
                opDeleteBodies(context, id + "dotProto", { "entities" : qUnion([dS, dH]) });
                opBoolean(context, id + "dotCutS", { "targets" : qUnion(sectorsNow), "tools" : qCreatedBy(id + "dotsS", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
                opBoolean(context, id + "dotCutH", { "targets" : bedHalf, "tools" : qCreatedBy(id + "dotsH", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
                okDots = true;
            }
            pmDrop(context, id + "dotDrop", qUnion([qCreatedBy(id + "dotS", EntityType.BODY), qCreatedBy(id + "dotH", EntityType.BODY), qCreatedBy(id + "dotsS", EntityType.BODY), qCreatedBy(id + "dotsH", EntityType.BODY)]));
        }
        var okPull = size(pullT) == 0;
        if (size(pullT) > 0)
        {
            try silent
            {
                var pullQ = [];
                for (var i = 0; i < size(pullT); i += 1)
                {
                    fCylinder(context, id + ("pull" ~ i), { "topCenter" : pullT[i].top, "bottomCenter" : pullT[i].bottom, "radius" : pullDia / 2 });
                    pullQ = append(pullQ, qCreatedBy(id + ("pull" ~ i), EntityType.BODY));
                }
                opBoolean(context, id + "pullCut", { "targets" : qUnion(sectorsNow), "tools" : qUnion(pullQ), "operationType" : BooleanOperationType.SUBTRACTION });
                okPull = true;
            }
            var pullLeft = [];
            for (var i = 0; i < size(pullT); i += 1)
                pullLeft = append(pullLeft, qCreatedBy(id + ("pull" ~ i), EntityType.BODY));
            pmDrop(context, id + "pullDrop", qBodyType(qUnion(pullLeft), BodyType.SOLID));
        }
        const sectorNote = " | sectors: " ~ size(sectorsNow) ~ " named root / gap sector k of " ~ Z ~
            (marksOn ? "; dots on " ~ (okDots ? nMarkedRoot : 0) ~ " root and " ~ (okDots ? nMarked - nMarkedRoot : 0) ~ " gap sectors and their seats (k dots beside the key, two deep" ~ (okDots ? "" : "; THE DOT CUT FAILED") ~
                (nMarked < size(sectorsNow) ? "; the rest are too narrow to carry them" : "") ~ ")" : "; no marks") ~
            (pullDia > 0.1 * mm ? "; a " ~ toMm(pullDia) ~ " mm pull hole " ~ toMm(pullDepth) ~ " deep in " ~ (okPull ? size(pullT) : 0) ~ " outer faces, along each sector's pull" ~ (okPull ? "" : " (THE PULL HOLE CUT FAILED)") : "; no pull holes");
        propPrint(context, "moldLog", "[mold]" ~ sectorNote);
        pmLogBodies(context, "after the sector marks and pull holes", moldSolids);

        // ---- build 99: the pinch-off, a land and a groove round every blade in the bed half's parting face ----
        var grooveNote = " | no pinch-off groove";
        var nVents = 0;
        var bladePlan = 0;                                        // one blade's plan area, cm2
        var grooveLen = 0 * mm;
        var outlineOk = false;
        var loops2 = [];
        var tipPts = [];
        {
            var eA = [];
            for (var e in edges)
                if (e.r * mm >= rHub - 2 * mm)
                    eA = append(eA, e);
            if (size(eA) >= 3)
            {
                for (var k = 0; k < Z; k += 1)
                {
                    const rot = k * spanZ;
                    var lp = [];
                    for (var i = 0; i < size(eA); i += 1)
                        lp = append(lp, vector(eA[i].r * mm * cos((eA[i].a + rot) * radian), eA[i].r * mm * sin((eA[i].a + rot) * radian)));
                    for (var i = size(eA) - 1; i >= 0; i -= 1)
                        lp = append(lp, vector(eA[i].r * mm * cos((eA[i].b + rot) * radian), eA[i].r * mm * sin((eA[i].b + rot) * radian)));
                    loops2 = append(loops2, lp);
                    const eT = eA[size(eA) - 1];
                    tipPts = append(tipPts, vector(eT.r * mm * cos(((eT.a + eT.b) / 2 + rot) * radian), eT.r * mm * sin(((eT.a + eT.b) / 2 + rot) * radian)));
                }
                var a2 = 0 * mm * mm;
                const l0 = loops2[0];
                for (var i = 0; i < size(l0); i += 1)
                {
                    const pA = l0[i];
                    const pB = l0[(i + 1) % size(l0)];
                    a2 += pA[0] * pB[1] - pB[0] * pA[1];
                    if (norm(pA) >= rSect && norm(pB) >= rSect)
                        grooveLen += norm(pB - pA);
                }
                bladePlan = abs(a2) / 2 / (mm * mm) / 100;
                outlineOk = true;
            }
        }
        if (grooveOn && outlineOk)
        {
            var okGroove = false;
            var tools = [];
            var lands = [];
            try silent
            {
                for (var k = 0; k < Z; k += 1)
                {
                    tools = append(tools, pmPrism(context, id + ("gOut" ~ k), pmOffsetLoop(loops2[k], landW + grooveW), lo[2] + 1 * mm, hi[2] - 1 * mm));
                    lands = append(lands, pmPrism(context, id + ("gIn" ~ k), pmOffsetLoop(loops2[k], landW), lo[2] + 0.5 * mm, hi[2] - 0.5 * mm));
                }
                if (ventOn)
                {
                    // from each tip straight out to the nearest block face, 1 mm wide; the lands below trim its start
                    for (var k = 0; k < Z; k += 1)
                    {
                        const pT = tipPts[k];
                        const alongX = abs(pT[0]) >= abs(pT[1]);
                        const sg = (alongX ? pT[0] : pT[1]) >= 0 * mm ? 1 : -1;
                        const s0 = (alongX ? pT[0] : pT[1]) + sg * (landW + 0.5 * mm);
                        const s1 = sg * (half + 1 * mm);
                        const c0 = (alongX ? pT[1] : pT[0]) - 0.5 * mm;
                        const c1 = c0 + 1 * mm;
                        const vLo = alongX ? vector(min(s0, s1), c0, lo[2] + 1 * mm) : vector(c0, min(s0, s1), lo[2] + 1 * mm);
                        const vHi = alongX ? vector(max(s0, s1), c1, hi[2] - 1 * mm) : vector(c1, max(s0, s1), hi[2] - 1 * mm);
                        pmBox(context, id + ("vent" ~ k), vLo, vHi);
                        tools = append(tools, qCreatedBy(id + ("vent" ~ k), EntityType.BODY));
                        nVents += 1;
                    }
                }
                lands = append(lands, pmCylinder(context, id + "gWell", lo[2], hi[2], rSect + landW));
                opBoolean(context, id + "gRing", { "targets" : qUnion(tools), "tools" : qUnion(lands), "operationType" : BooleanOperationType.SUBTRACTION });
                const gQ1 = qBodyType(qUnion([qUnion(tools), qCreatedBy(id + "gRing", EntityType.BODY)]), BodyType.SOLID);
                // the curtain: the parting tool moved by the groove depth leaves only the slab next to the parting face
                if (sectorsLow)
                {
                    opPattern(context, id + "gCurtain", { "entities" : toolBody, "transforms" : [transform(vector(0 * mm, 0 * mm, -grooveD))], "instanceNames" : ["g"] });
                    opBoolean(context, id + "gFloor", { "targets" : gQ1, "tools" : qCreatedBy(id + "gCurtain", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
                }
                else
                {
                    opPattern(context, id + "gUnder", { "entities" : toolBody, "transforms" : [identityTransform()], "instanceNames" : ["g"] });
                    opBoolean(context, id + "gFloor", { "targets" : gQ1, "tools" : qCreatedBy(id + "gUnder", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
                    pmBox(context, id + "gCeil", lo - vector(10, 10, 10) * mm, hi + vector(10, 10, 10) * mm);
                    opPattern(context, id + "gCurtain", { "entities" : toolBody, "transforms" : [transform(vector(0 * mm, 0 * mm, grooveD))], "instanceNames" : ["g"] });
                    opBoolean(context, id + "gCeilCut", { "targets" : qCreatedBy(id + "gCeil", EntityType.BODY), "tools" : qCreatedBy(id + "gCurtain", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
                    const gQ1b = qBodyType(qUnion([qUnion(tools), qCreatedBy(id + "gRing", EntityType.BODY), qCreatedBy(id + "gFloor", EntityType.BODY)]), BodyType.SOLID);
                    opBoolean(context, id + "gTop", { "targets" : gQ1b, "tools" : qBodyType(qUnion([qCreatedBy(id + "gCeil", EntityType.BODY), qCreatedBy(id + "gCeilCut", EntityType.BODY)]), BodyType.SOLID), "operationType" : BooleanOperationType.SUBTRACTION });
                }
                const gQ2 = qBodyType(qUnion([qUnion(tools), qCreatedBy(id + "gRing", EntityType.BODY), qCreatedBy(id + "gFloor", EntityType.BODY), qCreatedBy(id + "gTop", EntityType.BODY)]), BodyType.SOLID);
                const nPieces = size(evaluateQuery(context, gQ2));
                opBoolean(context, id + "gCut", { "targets" : bedHalf, "tools" : gQ2, "operationType" : BooleanOperationType.SUBTRACTION });
                okGroove = true;
                const grooveVol = Z * grooveLen * grooveW * grooveD / (mm * mm * mm) / 1000;
                const flash = laid.flash;
                grooveNote = " | pinch-off: land " ~ toMm(landW) ~ " mm, groove " ~ toMm(grooveW) ~ " x " ~ toMm(grooveD) ~ " mm round every blade from the well out, in the " ~ secSide ~
                    " half (" ~ nPieces ~ " groove pieces; about " ~ (round(grooveVol * 10) / 10) ~ " cm3 against " ~ (round(flash * 10) / 10) ~ " cm3 of flash at " ~ (round(lay.over * 1000) / 10) ~ " % overcharge" ~ (grooveVol < flash ? ": TOO SMALL, deepen or widen it" : "") ~ ")" ~
                    (nVents > 0 ? "; " ~ nVents ~ " vents from the tips to the block's faces" : (bagOn ? "; no vents: the groove is a sealed pocket in the bag" : ""));
            }
            // whatever is left of the tools when a step failed
            var leftQ = [qUnion(tools), qUnion(lands)];
            for (var nm in ["gRing", "gCurtain", "gUnder", "gFloor", "gCeil", "gCeilCut", "gTop"])
                leftQ = append(leftQ, qCreatedBy(id + nm, EntityType.BODY));
            pmDrop(context, id + "gDrop", qBodyType(qUnion(leftQ), BodyType.SOLID));
            if (!okGroove)
            {
                nVents = 0;
                grooveNote = " | PINCH-OFF GROOVE FAILED: the mold is finished without it (close on a film of flash, or cut the groove by hand 3 mm outside the blade edges)";
            }
        }
        propPrint(context, "moldLog", "[mold]" ~ grooveNote);
        pmLogBodies(context, "after the pinch-off", moldSolids);

        // ---- build 99: closing, as the Foil mold: clamps on the flanges or a vacuum bag; no bolts ----
        const blockArea = (2 * half) * (2 * half) / (mm * mm) / 100;                              // cm2, plan
        const partPlan = max(1, Z * bladePlan + PI * (rHub / mm) * (rHub / mm) / 100);          // cm2: the blades' outlines and the hub
        var closingNote = "";
        if (bagOn)
            closingNote = " | closing: vacuum bag at " ~ bagVac ~ " kPa gives " ~ (round(bagVac * 1000 * blockArea * 1e-4 / 100) / 10) ~ " kN on the " ~ round(blockArea) ~ " cm2 block, " ~
                (round(bagVac / 100 * blockArea / partPlan * 10) / 10) ~ " bar on the " ~ round(partPlan) ~ " cm2 of blades and hub" ~ roundNote ~ "; breather over the whole mold, check the flange gap with feelers before the resin gels";
        else
            closingNote = " | closing: clamps on the flanges against a rigid board each side (" ~ round(blockArea) ~ " cm2 block; a vacuum bag would give " ~ (round(85 * 1000 * blockArea * 1e-4 / 100) / 10) ~ " kN at 85 kPa); no bolts in the mold";
        propPrint(context, "moldLog", "[mold]" ~ closingNote);

        // ---- release check: rays along the axis; more than two crossings = a shadow ----
        var relNote = "";
        if (definition.releaseCheck)
        {
            const nR = 16;
            const nT = 72;
            const zTop = hi[2] + 1 * mm;
            var nOut = 0;
            var aOut = 0;
            var nIn = 0;
            var aIn = 0;
            var rOutMax = 0 * mm;
            for (var i = 0; i < nR; i += 1)
            {
                const rr = rSect + (R - rSect) * (i + 0.5) / nR;
                const dr = (R - rSect) / nR;
                for (var j = 0; j < nT; j += 1)
                {
                    const t = 2 * PI * (j + 0.5) / nT;
                    if (size(pmHits(context, partC, rr * cos(t * radian), rr * sin(t * radian), zTop)) > 2)
                    {
                        nOut += 1;
                        aOut += rr * dr * (2 * PI / nT) / (mm * mm);
                        rOutMax = max(rOutMax, rr);
                    }
                }
            }
            const nRi = 6;
            for (var i = 0; i < nRi; i += 1)
            {
                const rr = rHub * 0.9 + (rSect - rHub * 0.9) * (i + 0.5) / nRi;
                const dr = (rSect - rHub * 0.9) / nRi;
                for (var j = 0; j < nT; j += 1)
                {
                    const t = 2 * PI * (j + 0.5) / nT;
                    if (size(pmHits(context, partC, rr * cos(t * radian), rr * sin(t * radian), zTop)) > 2)
                    {
                        nIn += 1;
                        aIn += rr * dr * (2 * PI / nT) / (mm * mm);
                    }
                }
            }
            relNote = " | release along the axis outside the well: " ~ nOut ~ " of " ~ (nR * nT) ~ " rays see the part more than twice (" ~ (round(aOut) / 100) ~ " cm2" ~
                (nOut > 0 ? ", out to r " ~ toMm(rOutMax) ~ " mm - an undercut the halves cannot form" : ", nothing hooks") ~ ")" ~ (nIn > 0 ? "; in the well band " ~ nIn ~ " rays, the sectors' job" : "");
            propPrint(context, "moldLog", "[mold]" ~ relNote);
        }

        // ---- build 103: cut templates, one flat plate under the block with every ply outline cut through it (built last) ----
        var tplNote = "";
        if (definition.templatesOn == true)
        {
            var okTpl = false;
            var nTpl = 0;
            const tplMg = (definition.tplMargin == undefined ? 4 * mm : definition.tplMargin) / mm;
            const tplGap = (definition.tplGap == undefined ? 10 * mm : definition.tplGap) / mm;
            try silent
            {
                const tp = pmTemplateShapes(model, edges, inner, lay, tplMg, zWell0 / mm, zWell1 / mm);
                nTpl = size(tp.shapes);
                if (nTpl > 0)
                {
                    // rows left to right, wrapping at 280 mm; under each shape a band for its tally holes
                    var pos = [];
                    var xC = 0;
                    var yC = 0;
                    var rowH = 0;
                    var wMax = 0;
                    for (var nS = 0; nS < nTpl; nS += 1)
                    {
                        var u0 = 1e9;
                        var u1 = -1e9;
                        var v0 = 1e9;
                        var v1 = -1e9;
                        for (var q in tp.shapes[nS])
                        {
                            u0 = min(u0, q[0]);
                            u1 = max(u1, q[0]);
                            v0 = min(v0, q[1]);
                            v1 = max(v1, q[1]);
                        }
                        const tally = 6 * ceil(tp.cuts[nS] / 10) + 6;
                        const wS = max(u1 - u0, 4 + min(tp.cuts[nS], 10) * 6);
                        if (xC > 0 && xC + wS > 280)
                        {
                            xC = 0;
                            yC += rowH + tplGap;
                            rowH = 0;
                        }
                        pos = append(pos, { "dx" : xC - u0, "dy" : yC + tally - v0, "x" : xC, "y" : yC });
                        xC += wS + tplGap;
                        rowH = max(rowH, (v1 - v0) + tally);
                        wMax = max(wMax, xC - tplGap);
                    }
                    const plateW = (wMax + 2 * tplGap) * mm;
                    const plateH = (yC + rowH + 2 * tplGap) * mm;
                    const zPl = lo[2] - 60 * mm;
                    const x0P = lo[0] - plateW - 50 * mm;
                    const y0P = lo[1];
                    pmBox(context, id + "tplPlate", vector(x0P, y0P, zPl), vector(x0P + plateW, y0P + plateH, zPl + 1 * mm));
                    const tsid = id + "tplSketch";
                    var tsk = newSketchOnPlane(context, tsid, { "sketchPlane" : plane(vector(x0P + tplGap * mm, y0P + tplGap * mm, zPl + 1 * mm), Z_DIRECTION, X_DIRECTION) });
                    for (var nS = 0; nS < nTpl; nS += 1)
                    {
                        const shp = tp.shapes[nS];
                        for (var q = 0; q < size(shp); q += 1)
                        {
                            const pa = vector(shp[q][0] + pos[nS].dx, shp[q][1] + pos[nS].dy) * mm;
                            const pb = vector(shp[(q + 1) % size(shp)][0] + pos[nS].dx, shp[(q + 1) % size(shp)][1] + pos[nS].dy) * mm;
                            if (norm(pb - pa) > 0.01 * mm)
                                skLineSegment(tsk, "s" ~ nS ~ "_" ~ q, { "start" : pa, "end" : pb });
                        }
                        for (var tq = 0; tq < tp.cuts[nS]; tq += 1)
                            skCircle(tsk, "t" ~ nS ~ "_" ~ tq, { "center" : vector(pos[nS].x + 3.5 + (tq % 10) * 6, pos[nS].y + 3 + floor(tq / 10) * 6) * mm, "radius" : 1.5 * mm });
                    }
                    skSolve(tsk);
                    opExtrude(context, id + "tplCut", { "entities" : qSketchRegion(tsid), "direction" : -Z_DIRECTION, "endBound" : BoundingType.BLIND, "endDepth" : 3 * mm });
                    opBoolean(context, id + "tplCutDo", { "tools" : qCreatedBy(id + "tplCut", EntityType.BODY), "targets" : qCreatedBy(id + "tplPlate", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
                    opDeleteBodies(context, id + "tplSketchDel", { "entities" : qCreatedBy(tsid, EntityType.BODY) });
                    setProperty(context, { "entities" : qBodyType(qCreatedBy(id + "tplPlate", EntityType.BODY), BodyType.SOLID), "propertyType" : PropertyType.NAME, "value" : "Prop mold, cut templates" });
                    var aW = 0;                                   // cloth through the templates, margin included, mm2
                    var aU = 0;
                    for (var nS = 0; nS < nTpl; nS += 1)
                    {
                        const shp = tp.shapes[nS];
                        var a2 = 0;
                        for (var q = 0; q < size(shp); q += 1)
                            a2 += shp[q][0] * shp[(q + 1) % size(shp)][1] - shp[(q + 1) % size(shp)][0] * shp[q][1];
                        if (tp.kinds[nS] == "U")
                            aU += abs(a2) / 2 * tp.cuts[nS];
                        else
                            aW += abs(a2) / 2 * tp.cuts[nS];
                    }
                    var key = "TEMPLATES (numbered along each row in +X, the rows stacked in +Y from the plate's edge; +" ~ tplMg ~ " mm margin; the 3 mm holes under each shape, rows of 10, are its cut count; export the plate's top face as DXF):";
                    for (var nS = 0; nS < nTpl; nS += 1)
                        key = key ~ "\n" ~ (nS + 1) ~ ": " ~ tp.names[nS];
                    propPrint(context, "moldLog", key);
                    tplNote = " | cut templates: " ~ nTpl ~ " outlines on a " ~ round(plateW / mm) ~ " x " ~ round(plateH / mm) ~ " mm plate under the block (the key is in the log; export its top face as DXF); cut to them the cloth is " ~
                        (round(aW / 1000) / 1000) ~ " m2 woven and " ~ (round(aU / 1000) / 1000) ~ " m2 UD with the " ~ tplMg ~ " mm margin, before nesting waste: buy from these figures, the layup line's are net of the margin";
                    okTpl = true;
                }
            }
            if (!okTpl)
            {
                pmDrop(context, id + "tplDrop", qUnion([qCreatedBy(id + "tplPlate", EntityType.BODY), qCreatedBy(id + "tplCut", EntityType.BODY), qCreatedBy(id + "tplSketch", EntityType.BODY)]));
                tplNote = nTpl == 0 ? " | CUT TEMPLATES: no shapes (the model carries no station data: rebuild the Propeller feature)" : " | CUT TEMPLATES FAILED (" ~ nTpl ~ " shapes): the mold is finished without the plate";
            }
            propPrint(context, "moldLog", "[mold]" ~ tplNote);
        }

        // ---- build 114: pry sections and ejectors, as the Foil mold ----
        // Pry: a square bar turned 45 deg about the face's run, centred on the parting at the face (flat at the tip level
        // z0 out here), cut from both halves; sections along each face clear of the corner half keys
        var pryNote = "";
        var nPry = 0;
        if (definition.pryOn == true)
        {
            const pL = definition.pryLength;
            const sq = definition.pryDepth * sqrt(2);
            const clear = 12 * mm + definition.halfKeyDia / 2 + 6 * mm;   // the corner keys' centres sit 12 mm in
            const a = -half + clear;
            const b = half - clear;
            if (b - a >= pL)
            {
                const n = max(1, round((b - a) / definition.prySpacing));
                for (var fi = 0; fi < 4; fi += 1)
                {
                    const alongY = fi < 2;                               // the X faces run along Y
                    const face = fi % 2 == 0 ? -half : half;
                    for (var i = 0; i < n; i += 1)
                    {
                        const t = a + (b - a) * (i + 0.5) / n;
                        const c = alongY ? vector(face, t, z0) : vector(t, face, z0);
                        const hh = alongY ? vector(sq / 2, pL / 2, sq / 2) : vector(pL / 2, sq / 2, sq / 2);
                        const pid = id + ("pry" ~ fi ~ "_" ~ i);
                        pmBox(context, pid, c - hh, c + hh);
                        opTransform(context, pid + "rot", { "bodies" : qCreatedBy(pid, EntityType.BODY), "transform" : rotationAround(line(c, alongY ? Y_DIRECTION : X_DIRECTION), 45 * degree) });
                        hv = pmHalves(context, moldSolids, lo, hi);
                        var ok = false;
                        try silent
                        {
                            opBoolean(context, pid + "cut", { "tools" : qCreatedBy(pid, EntityType.BODY), "targets" : qUnion([hv.upper, hv.lower]), "operationType" : BooleanOperationType.SUBTRACTION });
                            ok = true;
                        }
                        if (ok)
                            nPry += 1;
                        else
                            pmDrop(context, pid + "drop", qCreatedBy(pid, EntityType.BODY));
                    }
                }
            }
            pryNote = nPry > 0 ? " | " ~ nPry ~ " pry sections, " ~ toMm(pL) ~ " mm V pockets across the parting on the block's faces: open with wedges in them, worked alternately" : "";
            propPrint(context, "moldLog", "[mold] PRY: " ~ nPry ~ " sections " ~ toMm(pL) ~ " mm, " ~ toMm(definition.pryDepth) ~ " mm into each half");
        }
        // Ejectors onto the hub's end faces: two per half, between the rod hole and the hub's edge, nearly opposite
        var ejectNote = "";
        var nEject = 0;
        if (definition.ejector != undefined && definition.ejector != PropEjector.OFF)
        {
            const rE = definition.ejectorDia / 2;
            const plugs = definition.ejector == PropEjector.PLUG;
            const fitE = plugs ? (definition.ejectorFit == undefined ? 0.15 * mm : definition.ejectorFit) / 2 : 0.1 * mm;
            const headE = 2.5 * mm;
            const rim = rE + (plugs ? 2 * mm : 0 * mm) + 1 * mm;
            const rRod = model.bore > 0 ? (model.bore * mm + definition.rodFit) / 2 : 0 * mm;
            const rIn = rRod + rim + 1 * mm;
            const rOut = rHub - rim - 1 * mm;
            hv = pmHalves(context, moldSolids, lo, hi);
            // each half its own spots: two on its own end face, nearly opposite (the faces differ: a tail dish, a pin slot)
            var spotsBy = [[], []];
            var rUsed = [0 * mm, 0 * mm];
            if (rOut >= rIn)
            {
                for (var side = 0; side < 2; side += 1)
                {
                    const key = side == 0 ? "okU" : "okL";
                    for (var rc in [(rIn + rOut) / 2, rIn, rOut])
                    {
                        for (var k = 0; k < 24 && size(spotsBy[side]) == 0; k += 1)
                        {
                            const th = k * 15 * degree;
                            const pr = pmEjectProbe(context, hv.upper, hv.lower, rc * cos(th), rc * sin(th), lo[2] - 20 * mm, hi[2] + 20 * mm, rim);
                            if (pr[key] == true)
                            {
                                spotsBy[side] = [[rc * cos(th), rc * sin(th), pr.zU, pr.zL, pr.t]];
                                rUsed[side] = rc;
                                for (var dk in [0, 1, -1, 2, -2, 3, -3])
                                {
                                    const th2 = th + (180 + dk * 15) * degree;
                                    const pr2 = pmEjectProbe(context, hv.upper, hv.lower, rc * cos(th2), rc * sin(th2), lo[2] - 20 * mm, hi[2] + 20 * mm, rim);
                                    if (pr2[key] == true)
                                    {
                                        spotsBy[side] = append(spotsBy[side], [rc * cos(th2), rc * sin(th2), pr2.zU, pr2.zL, pr2.t]);
                                        break;
                                    }
                                }
                            }
                        }
                        if (size(spotsBy[side]) > 0)
                            break;
                    }
                }
            }
            for (var side = 0; side < 2; side += 1)
            {
                for (var sp in spotsBy[side])
                {
                    const up = side == 0;
                    const zOut = up ? hi[2] : lo[2];
                    const zCav = up ? sp[2] : sp[3];
                    const sg = up ? 1 : -1;
                    const dIn = min(0.5 * mm, sp[4] / 3);
                    const eid = id + ("ej" ~ nEject);
                    hv = pmHalves(context, moldSolids, lo, hi);
                    const half1 = up ? hv.upper : hv.lower;
                    if (plugs)
                    {
                        // head on the cavity side, in a counterbore in the hub's end face; face cut from a copy of the half
                        opPattern(context, eid + "copy", { "entities" : half1, "transforms" : [identityTransform()], "instanceNames" : ["c"] });
                        const shank = pmFrustum(context, eid + "shank", sp[0], sp[1], min(zCav - sg * dIn, zOut + sg * 1 * mm), rE - fitE, max(zCav - sg * dIn, zOut + sg * 1 * mm), rE - fitE);
                        const head = pmFrustum(context, eid + "head", sp[0], sp[1], min(zCav - sg * dIn, zCav + sg * headE), rE + 2 * mm - fitE, max(zCav - sg * dIn, zCav + sg * headE), rE + 2 * mm - fitE);
                        opBoolean(context, eid + "plugJoin", { "tools" : qUnion([shank, head]), "operationType" : BooleanOperationType.UNION });
                        opBoolean(context, eid + "plugTrim", { "tools" : qUnion([qCreatedBy(eid + "copy", EntityType.BODY), qCreatedBy(eid + "shank", EntityType.BODY), qCreatedBy(eid + "head", EntityType.BODY)]),
                                    "operationType" : BooleanOperationType.INTERSECTION });
                    }
                    var cutters = [pmFrustum(context, eid + "hole", sp[0], sp[1], min(zCav - sg * dIn, zOut + sg * 1 * mm), rE + (plugs ? 0 * mm : 0.1 * mm), max(zCav - sg * dIn, zOut + sg * 1 * mm), rE + (plugs ? 0 * mm : 0.1 * mm))];
                    if (plugs)
                        cutters = append(cutters, pmFrustum(context, eid + "cbore", sp[0], sp[1], min(zCav - sg * dIn, zCav + sg * headE), rE + 2 * mm, max(zCav - sg * dIn, zCav + sg * headE), rE + 2 * mm));
                    opBoolean(context, eid + "cut", { "tools" : qUnion(cutters), "targets" : half1, "operationType" : BooleanOperationType.SUBTRACTION });
                    if (plugs)
                    {
                        const plugQ = qContainsPoint(qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID), vector(sp[0], sp[1], (zOut + zCav) / 2));
                        setAttribute(context, { "entities" : plugQ, "name" : "propEjectPlug", "attribute" : true });
                        setProperty(context, { "entities" : plugQ, "propertyType" : PropertyType.NAME, "value" : "Prop mold, ejector plug " ~ (nEject + 1) ~ (up ? " (upper)" : " (lower)") });
                    }
                    nEject += 1;
                }
            }
            const spots = concatenateArrays(spotsBy);
            ejectNote = nEject > 0 ? " | " ~ nEject ~ " ejector " ~ (plugs ? "plugs" : "holes") ~ " " ~ toMm(definition.ejectorDia) ~ " mm onto the hub's end faces: " ~ size(spotsBy[0]) ~ " through the upper half" ~ (size(spotsBy[0]) > 0 ? " at r " ~ toMm(rUsed[0]) ~ " mm" : "") ~ ", " ~ size(spotsBy[1]) ~ " through the lower" ~ (size(spotsBy[1]) > 0 ? " at r " ~ toMm(rUsed[1]) ~ " mm" : "") ~
                (plugs ? ": drop each plug in from the cavity before the layup (its head seats in the counterbore), punch it in from outside to push the part out" : ": fill flush with filleting wax, tap a dowel in to push the part out") :
                " | ejectors: no smooth spot on the hub's end faces between the rod hole and the hub's edge (a small hub, or recesses there): none made";
            propPrint(context, "moldLog", "[mold] EJECT: " ~ size(spotsBy[0]) ~ " upper, " ~ size(spotsBy[1]) ~ " lower" ~ (size(spots) > 0 ? ", hub " ~ toMm(spots[0][4]) ~ " mm long at the first" : ""));
        }

        // ---- place the mold round the real part, drop the copy and the parting tool ----
        opDeleteBodies(context, id + "dropTool", { "entities" : toolBody });
        if (norm(off) > 0.001 * mm)
            opTransform(context, id + "place", { "bodies" : moldSolids, "transform" : transform(off) });
        opDeleteBodies(context, id + "dropCopy", { "entities" : partC });
        const note = "Propeller mold, build " ~ PROP_FILE_BUILD ~ ": nose and tail halves parted on the blades' mean surfaces (" ~ size(loops) ~ " loops of " ~ (2 * Z) ~ " arcs), block " ~
            toMm(2 * half) ~ " x " ~ toMm(2 * half) ~ " x " ~ toMm(hi[2] - lo[2]) ~ " mm; hub sectors on the " ~ secSide ~ " side of the parting (the hub's bigger end), parted at the blades' edges" ~ (bothEdges ? ": a root sector under each blade and a gap sector between" : ": one per blade") ~ ", well r " ~ toMm(rSect) ~ " mm in that half only, the other half forms the hub above the parting; " ~
            size(sectors) ~ " sectors built with " ~ nKeys ~ " keys, each pulled outward" ~ (size(slivers) > 0 ? " (" ~ size(slivers) ~ " closed pockets or slivers inside the ring deleted: no piece can form them)" : "") ~ rodNote ~ relNote ~ sectorRelNote ~
            "; " ~ nHalfKeys ~ " half keys " ~ toMm(definition.halfKeyDia) ~ " mm at the flange corners" ~ layNote ~
            plugNote ~ sectorNote ~ grooveNote ~ closingNote ~ pryNote ~ ejectNote ~ tplNote ~ " | release: " ~ secSide ~ " half off (it holds the well" ~ plugNote2 ~ "), lift the part with the sectors out of the " ~ freeSide ~ " half, rod out, the gap sectors outward first, then the root sectors (a screw in the pull hole)" ~ frameNote;
        propPrint(context, "moldNotice", note);
        reportFeatureInfo(context, id, note);
        }
        catch (error)
        {
            const msg = "ERROR in Propeller mold: " ~ toString(error);
            propPrint(context, "moldLog", msg);
            try silent
            {
                propPrint(context, "moldLog", "  kernel: " ~ toString(error.message));
            }
            propPrint(context, "moldNotice", msg);
            // build 112: leave no half-built bodies behind (they showed as unnamed parts)
            try silent
            {
                opDeleteBodies(context, id + "dropOnError", { "entities" : qCreatedBy(id, EntityType.BODY) });
            }
            if (definition.keepLogOnError != false)
                reportFeatureWarning(context, id, msg ~ " (Keep log on error is on: the failure is shown as this warning so moldLog survives; its half-built bodies are deleted)");
            else
                throw error;
        }
    });


// ============================================================================
// Propeller model from part (build 104): the model the Propeller mold reads, measured off an imported propeller
// ============================================================================

export enum PropNoseEnd
{
    annotation { "Name" : "Bigger hub end" }
    BIGGER,
    annotation { "Name" : "Lower Z end" }
    LOW_Z,
    annotation { "Name" : "Higher Z end" }
    HIGH_Z
}

/** A number rounded to d decimals, for the log. */
function pfR(x is number, d is number) returns number
{
    const f = 10 ^ d;
    return round(x * f) / f;
}

/** Every hit of a ray on the body, as distances along it in mm (sorted; a hit counted twice on a shared edge dropped). */
function pfHits(context is Context, body is Query, origin is Vector, dir is Vector) returns array
{
    var res = [];
    try silent
    {
        res = evRaycast(context, { "entities" : body, "ray" : line(origin, dir), "closest" : false });
    }
    var ds = [];
    for (var h in res)
    {
        const d = dot(h.intersection - origin, dir) / millimeter;
        if (d > 0)
            ds = append(ds, d);
    }
    ds = sort(ds, function(a, b) { return a - b; });
    var out = [];
    for (var d in ds)
    {
        if (size(out) == 0 || d - out[size(out) - 1] > 1e-4)
            out = append(out, d);
    }
    return out;
}

/** A ray along +Z at radius r, azimuth th (radians) about the axis ax ([x, y] mm), from z0 (mm): the first and last hit. */
function pfAxial(context is Context, body is Query, ax is array, r is number, th is number, z0 is number) returns map
{
    const o = vector(ax[0] + r * cos(th * radian), ax[1] + r * sin(th * radian), z0) * millimeter;
    const h = pfHits(context, body, o, vector(0, 0, 1));
    if (size(h) < 2)
        return { "ok" : false };
    return { "ok" : true, "lo" : z0 + h[0], "hi" : z0 + h[size(h) - 1] };
}

/** The blade's edge at radius r between an azimuth that hits (thIn) and one that misses (thOut), to 1e-4 deg. */
function pfEdge(context is Context, body is Query, ax is array, r is number, thIn is number, thOut is number, z0 is number) returns number
{
    var a = thIn;
    var b = thOut;
    for (var k = 0; k < 14; k += 1)
    {
        const m = (a + b) / 2;
        if (pfAxial(context, body, ax, r, m, z0).ok)
            a = m;
        else
            b = m;
    }
    return a;
}

/** Linear interpolation of ys over increasing xs, clamped at the ends. */
function pfInterp(xs is array, ys is array, x is number) returns number
{
    const n = size(xs);
    if (x <= xs[0])
        return ys[0];
    if (x >= xs[n - 1])
        return ys[n - 1];
    for (var k = 1; k < n; k += 1)
    {
        if (xs[k] >= x)
        {
            const f = (xs[k] - xs[k - 1]) > 1e-12 ? (x - xs[k - 1]) / (xs[k] - xs[k - 1]) : 0;
            return ys[k - 1] + f * (ys[k] - ys[k - 1]);
        }
    }
    return ys[n - 1];
}

/** How far the part reaches from the axis along azimuth a (degrees): the box in a frame turned to a. */
function pfReach(context is Context, part is Query, ax is array, a is number) returns number
{
    const cs = coordSystem(vector(ax[0], ax[1], 0) * millimeter, vector(cos(a * degree), sin(a * degree), 0), vector(0, 0, 1));
    const b = evBox3d(context, { "topology" : part, "tight" : true, "cSys" : cs });
    return b.maxCorner[0] / millimeter;
}

/** One blade's section at radius r, swept across the window +- PI / Z about azimuth thc: azimuths and first and last hits. */
function pfStation(context is Context, part is Query, ax is array, r is number, thc is number, Z is number, z0 is number, nMean is number) returns map
{
    const w = PI / Z;
    const nS = 121;
    var hit = [];
    for (var k = 0; k < nS; k += 1)
        hit = append(hit, pfAxial(context, part, ax, r, thc - w + 2 * w * k / (nS - 1), z0).ok);
    var best0 = -1;
    var best1 = -1;
    var kk = 0;
    while (kk < nS)
    {
        if (hit[kk])
        {
            var e = kk;
            while (e + 1 < nS && hit[e + 1])
                e += 1;
            if (best0 < 0 || e - kk > best1 - best0)
            {
                best0 = kk;
                best1 = e;
            }
            kk = e + 1;
        }
        else
            kk += 1;
    }
    if (best0 < 0 || best1 - best0 < 2)
        return { "ok" : false };
    var ta = thc - w + 2 * w * best0 / (nS - 1);
    var tb = thc - w + 2 * w * best1 / (nS - 1);
    if (best0 > 0)
        ta = pfEdge(context, part, ax, r, ta, thc - w + 2 * w * (best0 - 1) / (nS - 1), z0);
    if (best1 < nS - 1)
        tb = pfEdge(context, part, ax, r, tb, thc - w + 2 * w * (best1 + 1) / (nS - 1), z0);
    var th = [];
    var lo = [];
    var hi = [];
    for (var j = 0; j < nMean; j += 1)
    {
        const u = 0.5 * (1 - cos((PI * j / (nMean - 1)) * radian));
        const t = ta + (tb - ta) * u;
        const a = pfAxial(context, part, ax, r, t, z0);
        if (a.ok)
        {
            th = append(th, t);
            lo = append(lo, a.lo);
            hi = append(hi, a.hi);
        }
    }
    if (size(th) < 5)
        return { "ok" : false };
    return { "ok" : true, "r" : r, "th" : th, "lo" : lo, "hi" : hi };
}

/** A swept station turned into the model's terms: LE -> TE toward the nose end first, chord, pitch angle, thickness, crest. */
function pfSection(st is map, noseLow is boolean) returns map
{
    var th = st.th;
    var lo = st.lo;
    var hi = st.hi;
    const n = size(th);
    var zm = [];
    for (var j = 0; j < n; j += 1)
        zm = append(zm, (lo[j] + hi[j]) / 2);
    const leFirst = noseLow ? (zm[0] < zm[n - 1]) : (zm[0] > zm[n - 1]);
    if (!leFirst)
    {
        th = reverse(th);
        lo = reverse(lo);
        hi = reverse(hi);
        zm = reverse(zm);
    }
    const r = st.r;
    var s = [];
    for (var j = 0; j < n; j += 1)
        s = append(s, r * th[j]);
    const dx = s[n - 1] - s[0];
    const dz = zm[n - 1] - zm[0];
    const c = sqrt(dx * dx + dz * dz);
    var al = [];
    for (var j = 0; j < n; j += 1)
        al = append(al, ((s[j] - s[0]) * dx + (zm[j] - zm[0]) * dz) / c);
    // the pitch angle from the mean line 10 % in from each edge (a ray grazes an edge at a corner, not at its middle)
    const s1 = pfInterp(al, s, 0.1 * c);
    const s2 = pfInterp(al, s, 0.9 * c);
    const z1 = pfInterp(al, zm, 0.1 * c);
    const z2 = pfInterp(al, zm, 0.9 * c);
    const phi = atan2(abs(z2 - z1), abs(s2 - s1)) / radian;
    var tmax = 0;
    var kmax = 0;
    for (var j = 0; j < n; j += 1)
    {
        const t = (hi[j] - lo[j]) * cos(phi * radian);
        if (t > tmax)
        {
            tmax = t;
            kmax = j;
        }
    }
    var ml = [];
    for (var j = 0; j < n; j += 1)
        ml = append(ml, [r * cos(th[j] * radian), r * sin(th[j] * radian), zm[j]]);
    return { "r" : r, "c" : c, "phi" : phi, "t" : tmax, "crest" : al[kmax] / c, "th" : th, "zm" : zm, "ml" : ml };
}

/** A hub end along z between a z where the inward ray meets the hub (zIn) and one where it does not (zOut). */
function pfHubEnd(context is Context, part is Query, ax is array, uG is Vector, oR is number, zIn is number, zOut is number) returns number
{
    var a = zIn;
    var b = zOut;
    for (var j = 0; j < 12; j += 1)
    {
        const m = (a + b) / 2;
        const h = pfHits(context, part, vector(ax[0], ax[1], m) * millimeter + uG * oR * millimeter, -uG);
        if (size(h) > 0 && oR - h[0] > 0.5)
            a = m;
        else
            b = m;
    }
    return a;
}

annotation { "Feature Type Name" : "Propeller model from part",
             "Feature Type Description" : "Measures an imported propeller solid (a STEP from any CAD, shaft along Z) and publishes the model the Propeller mold reads, so a builder's own propeller can be molded without rebuilding it here: hub frustum, bore, blade count, and at each station the chord, pitch angle, thickness, crest and mean line, found with rays along the shaft. Optionally builds the printed Hub core from the part itself." }
export const propModelFromPart = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Part", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1,
                     "Description" : "The propeller solid, imported or modelled elsewhere, with its shaft along the Z axis (move or rotate it first if not). Its blades must release along the shaft, as any propeller the Propeller mold can make does. The axis is taken from its coaxial cylinders and cones, the bore from the smallest of them." }
        definition.part is Query;

        annotation { "Name" : "Nose end", "Default" : PropNoseEnd.BIGGER,
                     "Description" : "Which end of the hub faces upstream (toward the motor on a pusher eFoil): the leading edges are the blade edges toward it. Bigger hub end is right for a hub that tapers aft, as most do. Only the layup line and the cut templates read it; the mold's halves and sectors do not." }
        definition.noseEnd is PropNoseEnd;

        annotation { "Name" : "Stations",
                     "Description" : "Radial stations measured from just outside the hub to the tip, spaced as the B-Series Propeller spaces them (closer toward the tip). Limits 10-60. Typical 30: about 180 rays each, a few seconds in all." }
        isInteger(definition.stations, { (unitless) : [10, 30, 60] } as IntegerBoundSpec);

        annotation { "Name" : "Root fillet",
                     "Description" : "Radius of the fillet between the blade roots and the hub, as modelled on the part. The Propeller mold sizes its hub sectors from the hub radius plus this plus the Sector band. Limits 0-8 mm. Typical 2." }
        isLength(definition.rootFillet, { (millimeter) : [0, 2, 8] } as LengthBoundSpec);

        annotation { "Name" : "Hub core", "Default" : false,
                     "Description" : "Also make the printed core a laminated propeller is laid up round: the part inside its hub frustum inset by Skin thick on the sides and both end faces, so the bore and every recess and slot of the part carry over. Named 'Hub core ...' and stamped for the mold's layup line." }
        definition.hubCore is boolean;

        if (definition.hubCore)
        {
            annotation { "Name" : "Skin thick",
                         "Description" : "Laminate over the core on the hub's sides and faces. Limits 0.5-4 mm. Typical 1.5: about seven plies of 200 gsm woven at 50 % fibre." }
            isLength(definition.coreSkin, { (millimeter) : [0.5, 1.5, 4] } as LengthBoundSpec);
        }
    }
    {
        setVariable(context, "partModelLog", "");
        setVariable(context, "partModelNotice", "");
        propPrint(context, "partModelLog", "==== Propeller model from part, build " ~ PROP_FILE_BUILD ~ " ====");
        const parts = evaluateQuery(context, definition.part);
        if (size(parts) != 1)
            throw regenError("Select the propeller solid as Part", ["part"]);
        const part = parts[0];
        const pBox = evBox3d(context, { "topology" : part, "tight" : true });
        const zLo = pBox.minCorner[2] / millimeter;
        const zHi = pBox.maxCorner[2] / millimeter;
        const z0 = zLo - 5;

        // ---- the axis: the (x, y) most of the part's cylinders and cones parallel to Z share; the bore the smallest cylinder on it ----
        var cands = [];
        for (var f in evaluateQuery(context, qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.CYLINDER)))
        {
            const g = evSurfaceDefinition(context, { "face" : f });
            if (abs(abs(g.coordSystem.zAxis[2]) - 1) < 1e-4)
                cands = append(cands, { "x" : g.coordSystem.origin[0] / millimeter, "y" : g.coordSystem.origin[1] / millimeter, "r" : g.radius / millimeter, "cyl" : true });
        }
        for (var f in evaluateQuery(context, qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.CONE)))
        {
            const g = evSurfaceDefinition(context, { "face" : f });
            if (abs(abs(g.coordSystem.zAxis[2]) - 1) < 1e-4)
                cands = append(cands, { "x" : g.coordSystem.origin[0] / millimeter, "y" : g.coordSystem.origin[1] / millimeter, "r" : 0, "cyl" : false });
        }
        var ax = [(pBox.minCorner[0] + pBox.maxCorner[0]) / 2 / millimeter, (pBox.minCorner[1] + pBox.maxCorner[1]) / 2 / millimeter];
        var axisNote = "";
        var bestN = 0;
        for (var a in cands)
        {
            var nOn = 0;
            for (var b in cands)
            {
                if (abs(a.x - b.x) < 0.05 && abs(a.y - b.y) < 0.05)
                    nOn += 1;
            }
            if (nOn > bestN)
            {
                bestN = nOn;
                ax = [a.x, a.y];
            }
        }
        if (bestN == 0)
            axisNote = " | WARNING: no cylinder or cone parallel to Z on the part: the axis is the centre of its box, which is wrong for an odd blade count";
        var bore = 0;
        for (var a in cands)
        {
            if (a.cyl && abs(a.x - ax[0]) < 0.05 && abs(a.y - ax[1]) < 0.05 && (bore == 0 || 2 * a.r < bore))
                bore = 2 * a.r;
        }
        propPrint(context, "partModelLog", "[part] axis at x " ~ pfR(ax[0], 3) ~ ", y " ~ pfR(ax[1], 3) ~ " mm (" ~ bestN ~ " coaxial cylinders and cones); bore " ~ pfR(bore, 3) ~ " mm; z " ~ pfR(zLo, 2) ~ " to " ~ pfR(zHi, 2));

        // ---- the tip radius: the part's reach from the axis, every 5 deg then every 0.5 deg about the best ----
        var R = 0;
        var aBest = 0;
        for (var k = 0; k < 72; k += 1)
        {
            const rr = pfReach(context, part, ax, k * 5);
            if (rr > R)
            {
                R = rr;
                aBest = k * 5;
            }
        }
        const aMid = aBest;
        for (var k = -5; k <= 5; k += 1)
        {
            const rr = pfReach(context, part, ax, aMid + k * 0.5);
            if (rr > R)
                R = rr;
        }

        // ---- the blades: runs of hits round the circle at 0.7R ----
        const rB7 = 0.7 * R;
        var hit = [];
        for (var k = 0; k < 360; k += 1)
            hit = append(hit, pfAxial(context, part, ax, rB7, k * PI / 180, z0).ok);
        var k0 = -1;
        for (var k = 0; k < 360; k += 1)
        {
            if (!hit[k])
            {
                k0 = k;
                break;
            }
        }
        if (k0 < 0)
            throw regenError("Every ray along the shaft at 0.7R meets the part: the blades overlap in the axial view, which no two-half mold can release", ["part"]);
        var centres = [];
        var kk = 0;
        while (kk < 360)
        {
            if (hit[(k0 + kk) % 360])
            {
                var e = kk;
                while (e + 1 < 360 && hit[(k0 + e + 1) % 360])
                    e += 1;
                centres = append(centres, (k0 + (kk + e) / 2) * PI / 180);
                kk = e + 1;
            }
            else
                kk += 1;
        }
        const Z = size(centres);
        if (Z < 2)
            throw regenError("Found " ~ Z ~ " blade at 0.7R: is the shaft along Z and is this a propeller?", ["part"]);
        propPrint(context, "partModelLog", "[part] D " ~ pfR(2 * R, 2) ~ " mm, " ~ Z ~ " blades, blade 1 at " ~ pfR(centres[0] * 180 / PI, 1) ~ " deg at 0.7R");

        // ---- the hub: rays inward at the middle of the gap after blade 1, its ends by bisection, a frustum fitted to the middle ----
        const thG = centres[0] + PI / Z;
        const uG = vector(cos(thG * radian), sin(thG * radian), 0);
        const oR = R + 5;
        const nZ = 160;
        var zs = [];
        var rs = [];
        for (var k = 0; k <= nZ; k += 1)
        {
            const z = zLo - 0.5 + (zHi - zLo + 1) * k / nZ;
            const h = pfHits(context, part, vector(ax[0], ax[1], z) * millimeter + uG * oR * millimeter, -uG);
            if (size(h) > 0 && oR - h[0] > 0.5)
            {
                zs = append(zs, z);
                rs = append(rs, oR - h[0]);
            }
        }
        if (size(zs) < 8)
            throw regenError("No hub found between the blades: is the shaft along Z?", ["part"]);
        const zStep = (zHi - zLo + 1) / nZ;
        var zA = pfHubEnd(context, part, ax, uG, oR, zs[0], zs[0] - zStep);
        var zB = pfHubEnd(context, part, ax, uG, oR, zs[size(zs) - 1], zs[size(zs) - 1] + zStep);
        // build 112: snap each end to the part's own planar end face, so the mold's well meets it exactly
        var snapA = false;
        var snapB = false;
        for (var f in evaluateQuery(context, qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.PLANE)))
        {
            const pl = evPlane(context, { "face" : f });
            if (abs(abs(pl.normal[2]) - 1) > 1e-6)
                continue;
            const zf = pl.origin[2] / millimeter;
            if (!snapA && abs(zf - zA) < 0.2)
            {
                zA = zf;
                snapA = true;
            }
            else if (!snapB && abs(zf - zB) < 0.2)
            {
                zB = zf;
                snapB = true;
            }
        }
        // an end with no planar face (the side meeting a dish or a cone in a sharp edge): the exact end of the hub's
        // own side, the coaxial cone or cylinder whose extent reaches it
        for (var f in evaluateQuery(context, qUnion([qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.CONE), qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.CYLINDER)])))
        {
            const g = evSurfaceDefinition(context, { "face" : f });
            if (abs(abs(g.coordSystem.zAxis[2]) - 1) > 1e-6 || abs(g.coordSystem.origin[0] / millimeter - ax[0]) > 0.05 || abs(g.coordSystem.origin[1] / millimeter - ax[1]) > 0.05)
                continue;
            const fb = evBox3d(context, { "topology" : f, "tight" : true });
            if (!snapA && abs(fb.minCorner[2] / millimeter - zA) < 0.2)
            {
                zA = fb.minCorner[2] / millimeter;
                snapA = true;
            }
            if (!snapB && abs(fb.maxCorner[2] / millimeter - zB) < 0.2)
            {
                zB = fb.maxCorner[2] / millimeter;
                snapB = true;
            }
        }
        var sx = 0;
        var sy = 0;
        var sxx = 0;
        var sxy = 0;
        var nn = 0;
        for (var j = 0; j < size(zs); j += 1)
        {
            if (zs[j] > zA + 0.1 * (zB - zA) && zs[j] < zB - 0.1 * (zB - zA))
            {
                sx += zs[j];
                sy += rs[j];
                sxx += zs[j] * zs[j];
                sxy += zs[j] * rs[j];
                nn += 1;
            }
        }
        const den = nn * sxx - sx * sx;
        const ks = (nn > 2 && abs(den) > 1e-9) ? (nn * sxy - sx * sy) / den : 0;
        const kb = nn > 0 ? (sy - ks * sx) / nn : rs[0];
        const rA = ks * zA + kb;
        const rB = ks * zB + kb;
        const rHub = max(rA, rB);
        const noseLow = definition.noseEnd == PropNoseEnd.LOW_Z ? true : (definition.noseEnd == PropNoseEnd.HIGH_Z ? false : rA >= rB);
        propPrint(context, "partModelLog", "[part] hub r " ~ pfR(rA, 2) ~ " at z " ~ pfR(zA, 3) ~ " to r " ~ pfR(rB, 2) ~ " at z " ~ pfR(zB, 3) ~ " (" ~ pfR(zB - zA, 2) ~ " mm long); ends " ~ (snapA ? "snapped" : "measured") ~ " / " ~ (snapB ? "snapped" : "measured") ~ " to the part's end faces; nose at the " ~ (noseLow ? "lower" : "higher") ~ "-Z end");

        // ---- the stations: swept across blade 1, from just outside the hub to the tip ----
        const nSt = definition.stations;
        // build 112: start outside the root fillet, where the section is the blade's own; the stations carried inward
        // cover the root (measured through the fillet, the edges land on its surface and the parting touches the part)
        const r0 = rHub + definition.rootFillet / millimeter + 0.6;
        const r1 = R - 0.25;
        var secs = [];
        var thc = centres[0];
        for (var i = 0; i < nSt; i += 1)
        {
            const u = i / (nSt - 1);
            const r = r0 + (r1 - r0) * (0.5 * u + 0.5 * sin((u * PI / 2) * radian));
            const st = pfStation(context, part, ax, r, thc, Z, z0, 31);
            if (!st.ok)
            {
                propPrint(context, "partModelLog", "[part] station r " ~ pfR(r, 2) ~ ": no closed section, left out");
                continue;
            }
            const sec = pfSection(st, noseLow);
            secs = append(secs, sec);
            thc = (st.th[0] + st.th[size(st.th) - 1]) / 2;
        }
        if (size(secs) < 4)
            throw regenError("Only " ~ size(secs) ~ " stations closed: check that the blades release along the shaft", ["part"]);
        // two stations carried on inward, to 3 mm inside the hub's smaller end, as the generator's stations below the hub surface
        const sa = secs[0];
        const sb = secs[1];
        var inner = [];
        const rIn0 = min(rA, rB) - 3;
        for (var r in [rIn0, (rIn0 + sa.r) / 2])
        {
            const f = (r - sa.r) / (sb.r - sa.r);
            var ml = [];
            for (var j = 0; j < size(sa.th); j += 1)
            {
                const t = sa.th[j] + f * (sb.th[min(j, size(sb.th) - 1)] - sa.th[j]);
                const z = sa.zm[j] + f * (sb.zm[min(j, size(sb.zm) - 1)] - sa.zm[j]);
                ml = append(ml, [r * cos(t * radian), r * sin(t * radian), z]);
            }
            inner = append(inner, { "r" : r, "c" : sa.c + f * (sb.c - sa.c), "phi" : sa.phi + f * (sb.phi - sa.phi), "t" : sa.t + f * (sb.t - sa.t), "crest" : sa.crest, "ml" : ml });
        }
        const allSt = concatenateArrays([inner, secs]);

        // ---- the model, in the part's own frame about its axis (the mold finds the axis again from the bore) ----
        var stR = [];
        var stC = [];
        var stPhi = [];
        var stT = [];
        var stCr = [];
        var mls = [];
        for (var sx2 in allSt)
        {
            stR = append(stR, sx2.r);
            stC = append(stC, sx2.c);
            stPhi = append(stPhi, sx2.phi * 180 / PI);
            stT = append(stT, sx2.t);
            stCr = append(stCr, sx2.crest);
            mls = append(mls, sx2.ml);
        }
        const skin = definition.hubCore ? definition.coreSkin / millimeter : 0;
        var coreOn = false;
        if (definition.hubCore)
        {
            const cosA = cos(atan2(abs(rB - rA), zB - zA));
            const dr = skin / cosA;
            fCone(context, id + "coreCone", { "bottomCenter" : vector(ax[0], ax[1], zA + skin) * millimeter, "topCenter" : vector(ax[0], ax[1], zB - skin) * millimeter,
                        "bottomRadius" : (rA - dr) * millimeter, "topRadius" : (rB - dr) * millimeter });
            opPattern(context, id + "coreCopy", { "entities" : part, "transforms" : [identityTransform()], "instanceNames" : ["core"] });
            opBoolean(context, id + "coreCut", { "tools" : qUnion([qCreatedBy(id + "coreCopy", EntityType.BODY), qCreatedBy(id + "coreCone", EntityType.BODY)]), "operationType" : BooleanOperationType.INTERSECTION });
            const cores = evaluateQuery(context, qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID));
            if (size(cores) == 1)
            {
                setProperty(context, { "entities" : cores[0], "propertyType" : PropertyType.NAME, "value" : "Hub core, " ~ pfR(2 * R, 1) ~ " mm x " ~ Z ~ ", skin " ~ pfR(skin, 2) });
                setAttribute(context, { "entities" : cores[0], "name" : "propCoreBody", "attribute" : toString(id) });
                coreOn = true;
                propPrint(context, "partModelLog", "[part] hub core: the part inside the hub inset " ~ pfR(skin, 2) ~ " mm, " ~ pfR(evVolume(context, { "entities" : cores[0] }) / (millimeter * millimeter * millimeter) / 1000, 2) ~ " cm3");
            }
            else
                propPrint(context, "partModelLog", "[part] WARNING: the hub core came out as " ~ size(cores) ~ " bodies; none kept as the core");
        }
        const modelMap = {
                    "build" : PROP_FILE_BUILD, "tag" : toString(id), "source" : "part", "D" : 2 * R, "R" : R, "Z" : Z, "hand" : 0,
                    "hubRu" : rA, "hubRd" : rB, "hubZa" : zA, "hubZb" : zB, "noseLow" : noseLow,
                    "noseLen" : 0, "tailLen" : 0, "tailStyle" : "FLAT", "pocketDia" : 0, "pocketFloor" : 0,
                    "bore" : bore, "fillet" : definition.rootFillet / millimeter, "zLo" : zLo, "zHi" : zHi,
                    "stationR" : stR, "stationChord" : stC, "stationPhi" : stPhi, "stationThick" : stT, "stationCrest" : stCr, "meanLines" : mls,
                    "core" : coreOn ? 1 : 0, "skin" : coreOn ? skin : 0 };
        setVariable(context, "propModel", modelMap);
        setAttribute(context, { "entities" : part, "name" : "propModel", "attribute" : modelMap });
        setAttribute(context, { "entities" : part, "name" : "propModelBody", "attribute" : "Propeller " ~ pfR(2 * R, 1) ~ " mm x " ~ Z ~ " (from part)" });

        // ---- the log's station table and the notice ----
        propPrint(context, "partModelLog", "[part] r mm | r/R | chord | pitch deg | P/D | t mm | t/c | crest");
        var area = 0;
        for (var j = 0; j < size(secs); j += 1)
        {
            const q = secs[j];
            const P = 2 * PI * q.r * tan(q.phi * radian);
            propPrint(context, "partModelLog", "[part] " ~ pfR(q.r, 2) ~ " | " ~ pfR(q.r / R, 3) ~ " | " ~ pfR(q.c, 2) ~ " | " ~ pfR(q.phi * 180 / PI, 2) ~ " | " ~ pfR(P / (2 * R), 3) ~ " | " ~ pfR(q.t, 2) ~ " | " ~ pfR(q.t / q.c, 3) ~ " | " ~ pfR(q.crest, 2));
            if (j > 0)
                area += 0.5 * (q.c + secs[j - 1].c) * (q.r - secs[j - 1].r);
        }
        area += 0.5 * (secs[0].c) * (secs[0].r - rHub) + 0.5 * secs[size(secs) - 1].c * (R - secs[size(secs) - 1].r);
        var r7 = [];
        var c7 = [];
        var p7 = [];
        var t7 = [];
        for (var q in secs)
        {
            r7 = append(r7, q.r);
            c7 = append(c7, q.c);
            p7 = append(p7, 2 * PI * q.r * tan(q.phi * radian));
            t7 = append(t7, q.t);
        }
        const ear = Z * area / (PI * R * R);
        const note = "Propeller model from part, build " ~ PROP_FILE_BUILD ~ ": D " ~ pfR(2 * R, 1) ~ " mm, " ~ Z ~ " blades, EAR " ~ pfR(ear, 3) ~ "; P/D " ~ pfR(pfInterp(r7, p7, 0.7 * R) / (2 * R), 3) ~ " at 0.7R (mean line), chord " ~ pfR(pfInterp(r7, c7, 0.7 * R), 1) ~ " mm, t " ~ pfR(pfInterp(r7, t7, 0.7 * R), 2) ~ " mm; " ~
            "hub " ~ pfR(2 * rA, 1) ~ " to " ~ pfR(2 * rB, 1) ~ " mm over " ~ pfR(zB - zA, 1) ~ " mm, bore " ~ pfR(bore, 2) ~ " mm; " ~ size(secs) ~ " of " ~ nSt ~ " stations measured, 2 carried inside the hub" ~
            (coreOn ? "; hub core made, skin " ~ pfR(skin, 2) ~ " mm" : "") ~ " | the model is on the part: add a Propeller mold below and select this part" ~ axisNote;
        propPrint(context, "partModelNotice", note);
        reportFeatureInfo(context, id, note);
    });


// ============================================================================
// Core forging mold (build 109): a printed piston mold for a hub core in forged carbon
// ============================================================================

export enum ForgePistonEnd
{
    annotation { "Name" : "Bigger end" }
    BIGGER,
    annotation { "Name" : "Lower Z end" }
    LOW_Z,
    annotation { "Name" : "Higher Z end" }
    HIGH_Z
}

/** A cylinder on the axis at (ax[0], ax[1]) from z0 to z1 (mm), radius r (mm). */
function fgCyl(context is Context, id is Id, ax is array, z0 is number, z1 is number, r is number) returns Query
{
    fCylinder(context, id, { "bottomCenter" : vector(ax[0], ax[1], min(z0, z1)) * millimeter, "topCenter" : vector(ax[0], ax[1], max(z0, z1)) * millimeter, "radius" : r * millimeter });
    return qCreatedBy(id, EntityType.BODY);
}

/** A frustum on the axis: radius ra at za, rb at zb (mm). */
function fgCone(context is Context, id is Id, ax is array, za is number, ra is number, zb is number, rb is number) returns Query
{
    const lowA = za < zb;
    fCone(context, id, { "bottomCenter" : vector(ax[0], ax[1], lowA ? za : zb) * millimeter, "topCenter" : vector(ax[0], ax[1], lowA ? zb : za) * millimeter,
                "bottomRadius" : (lowA ? ra : rb) * millimeter, "topRadius" : (lowA ? rb : ra) * millimeter });
    return qCreatedBy(id, EntityType.BODY);
}

/** A frustum key at (x, y), its base at zb, pointing along dir (+1 / -1): base dia d, height h, 12 deg draft. */
function fgKey(context is Context, id is Id, x is number, y is number, zb is number, dir is number, d is number, h is number) returns Query
{
    const rTip = d / 2 - h * tan(12 * degree);
    return fgCone(context, id, [x, y], zb, d / 2, zb + dir * h, rTip);
}

/** The core's outer radius at height z: rays inward from outside at 12 azimuths, the largest first hit (mm). */
function fgOuterR(context is Context, part is Query, ax is array, z is number, rFar is number) returns number
{
    var r = 0;
    for (var k = 0; k < 12; k += 1)
    {
        const a = k * 30 * degree;
        const u = vector(cos(a), sin(a), 0);
        const h = pfHits(context, part, vector(ax[0], ax[1], z) * millimeter + u * rFar * millimeter, -u);
        if (size(h) > 0)
            r = max(r, rFar - h[0]);
    }
    return r;
}

/** The axis (the (x, y) most coaxial cylinders and cones share) and the bore (the smallest cylinder on it), mm. */
function fgAxisBore(context is Context, part is Query, pBox is Box3d) returns map
{
    var cands = [];
    for (var f in evaluateQuery(context, qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.CYLINDER)))
    {
        const g = evSurfaceDefinition(context, { "face" : f });
        if (abs(abs(g.coordSystem.zAxis[2]) - 1) < 1e-4)
            cands = append(cands, { "x" : g.coordSystem.origin[0] / millimeter, "y" : g.coordSystem.origin[1] / millimeter, "r" : g.radius / millimeter, "cyl" : true });
    }
    for (var f in evaluateQuery(context, qGeometry(qOwnedByBody(part, EntityType.FACE), GeometryType.CONE)))
    {
        const g = evSurfaceDefinition(context, { "face" : f });
        if (abs(abs(g.coordSystem.zAxis[2]) - 1) < 1e-4)
            cands = append(cands, { "x" : g.coordSystem.origin[0] / millimeter, "y" : g.coordSystem.origin[1] / millimeter, "r" : 0, "cyl" : false });
    }
    var ax = [(pBox.minCorner[0] + pBox.maxCorner[0]) / 2 / millimeter, (pBox.minCorner[1] + pBox.maxCorner[1]) / 2 / millimeter];
    var bestN = 0;
    for (var a in cands)
    {
        var nOn = 0;
        for (var b in cands)
        {
            if (abs(a.x - b.x) < 0.05 && abs(a.y - b.y) < 0.05)
                nOn += 1;
        }
        if (nOn > bestN)
        {
            bestN = nOn;
            ax = [a.x, a.y];
        }
    }
    var bore = 0;
    for (var a in cands)
    {
        if (a.cyl && abs(a.x - ax[0]) < 0.05 && abs(a.y - ax[1]) < 0.05 && (bore == 0 || 2 * a.r < bore))
            bore = 2 * a.r;
    }
    return { "ax" : ax, "bore" : bore, "n" : bestN };
}

/** The three pieces this feature has made, by their ends along dir: the floor reaches furthest back, the piston
    furthest forward, the sleeve is the largest of the rest. Keys and tool bodies left over are smaller than all three. */
function fgPieces(context is Context, id is Id, dir is number) returns map
{
    var bodies = [];
    for (var b in evaluateQuery(context, qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID)))
    {
        const bb = evBox3d(context, { "topology" : b, "tight" : true });
        const lo = (dir > 0 ? bb.minCorner[2] : -bb.maxCorner[2]) / millimeter;
        const hi = (dir > 0 ? bb.maxCorner[2] : -bb.minCorner[2]) / millimeter;
        bodies = append(bodies, { "b" : b, "lo" : lo, "hi" : hi, "v" : evVolume(context, { "entities" : b }) / (millimeter * millimeter * millimeter) });
    }
    // the three largest bodies are the pieces
    bodies = sort(bodies, function(a, c) { return c.v - a.v; });
    var big = [];
    for (var i = 0; i < min(3, size(bodies)); i += 1)
        big = append(big, bodies[i]);
    var floorB = big[0];
    var pistonB = big[0];
    for (var x in big)
    {
        if (x.lo < floorB.lo)
            floorB = x;
        if (x.hi > pistonB.hi)
            pistonB = x;
    }
    var sleeveB = big[0];
    for (var x in big)
    {
        if (x.b != floorB.b && x.b != pistonB.b)
            sleeveB = x;
    }
    return { "floor" : floorB.b, "sleeve" : sleeveB.b, "piston" : pistonB.b };
}

annotation { "Feature Type Name" : "Core forging mold",
             "Feature Type Description" : "Printed compression (piston) mold for making a hub core in forged carbon, chopped tow and epoxy pressed to shape: a sleeve with the core's frustum as its cavity and a parallel bore above for the piston, a floor plug with the small end's recesses as bosses, and a piston with the big end's recesses as bosses and a shoulder that stops at the finished height. Closed by a bolt through a steel tube that forms the bore. Reports the charge by weight, the clamp force and nut torque schedule, the mold's stresses, the core's strength at the shear pin and the whole technique." }
export const coreForgingMold = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Core", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1,
                     "Description" : "The hub core to forge, shaft along Z: the Hub core a B-Series Propeller or Propeller model from part made, or any core with a bore. Leave empty to take the body stamped as the hub core. Its outside must be a frustum (or a cylinder) about the axis, as hub cores are." }
        definition.part is Query;

        annotation { "Name" : "Piston end", "Default" : ForgePistonEnd.BIGGER,
                     "Description" : "Which end of the core the piston presses from. Bigger end lets the core's taper act as draft, so the finished core is pushed out toward that end. Recesses in this end (a pin slot, a counterbore) are formed by bosses on the piston, those in the other end (a nut pocket) by bosses on the floor plug." }
        definition.pistonEnd is ForgePistonEnd;

        annotation { "Group Name" : "Mold", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Wall",
                         "Description" : "Sleeve wall outside the core's largest radius. Limits 8-40 mm. Typical 15-20: the charge presses outward like a fluid, and the wall's hoop stress (in the notice) must stay well under printed PETG's strength along its layers." }
            isLength(definition.wall, { (millimeter) : [8, 15, 40] } as LengthBoundSpec);

            annotation { "Name" : "Floor",
                         "Description" : "Thickness of the floor plug's flange under the sleeve. Limits 5-30 mm. Typical 10." }
            isLength(definition.floorT, { (millimeter) : [5, 10, 30] } as LengthBoundSpec);

            annotation { "Name" : "Flange",
                         "Description" : "Thickness of the piston's shoulder flange, which lands on the sleeve's rim at the finished height and stops the piston. Limits 4-20 mm. Typical 8." }
            isLength(definition.flangeT, { (millimeter) : [4, 8, 20] } as LengthBoundSpec);

            annotation { "Name" : "Stroke x height",
                         "Description" : "Piston travel as a multiple of the core's height: the loose charge of chopped tow takes about twice the finished volume. Limits 1.2-3. Typical 2 (Easy Composites: too little travel is one of the most common mistakes)." }
            isReal(definition.strokeFactor, { (unitless) : [1.2, 2, 3] } as RealBoundSpec);

            annotation { "Name" : "Cone top", "Default" : true,
                         "Description" : "A cone on the piston's flange rising to a seat for a fender washer round the bolt: it carries the nut's load down to the flange and the stem at about 45 deg and stiffens the flange where it lands on the sleeve, so no steel plate is needed on the piston side. Off: a flat flange under a steel plate. Typical on." }
            definition.coneTop is boolean;

            annotation { "Name" : "Land",
                         "Description" : "Length of the piston's sealing land at its face, which slides at Clearance and keeps the fibres in; above it the stem is relieved. Limits 3-20 mm. Typical 8." }
            isLength(definition.land, { (millimeter) : [3, 8, 20] } as LengthBoundSpec);

            annotation { "Name" : "Relief",
                         "Description" : "How much smaller the stem is than the sleeve's bore above the land, per face, so a long printed stem cannot bind and the bleeding resin has somewhere to go. Limits 0.2-2 mm. Typical 0.5." }
            isLength(definition.relief, { (millimeter) : [0.2, 0.5, 2] } as LengthBoundSpec);

            annotation { "Name" : "Lead-in",
                         "Description" : "45 deg chamfer on the sleeve's mouth, so the piston finds the bore over a loaded charge without catching tow on the edge. Limits 0-3 mm. Typical 1; 0 for a sharp edge." }
            isLength(definition.leadIn, { (millimeter) : [0, 1, 3] } as LengthBoundSpec);

            annotation { "Name" : "Clearance",
                         "Description" : "Sliding clearance per face between the piston and the sleeve, the spigot and the sleeve, and the piston and the tube. Limits 0-0.4 mm. Typical 0.1: printed size on size will not slide, and the excess resin bleeds out through it." }
            isLength(definition.clearance, { (millimeter) : [0, 0.1, 0.4] } as LengthBoundSpec);
        }

        annotation { "Group Name" : "Closing", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Tube wall",
                         "Description" : "Wall of the steel tube that forms the bore; its outside is the bore and the bolt runs inside it. Limits 0.5-3 mm. Typical 1 (a 10 x 1 tube takes an M8)." }
            isLength(definition.tubeWall, { (millimeter) : [0.5, 1, 3] } as LengthBoundSpec);

            annotation { "Name" : "Bolt dia",
                         "Description" : "The bolt through the tube that closes the mold between two steel plates. Limits 3-16 mm. Typical 8: an 8.8 M8 is good for about 20 kN, far more than the printed sleeve." }
            isLength(definition.boltDia, { (millimeter) : [3, 8, 16] } as LengthBoundSpec);

            annotation { "Name" : "Target MPa",
                         "Description" : "Pressure on the charge at the end of the closing, for the force and torque in the notice. Limits 1-20. Typical 5: enough to consolidate a hand-loaded charge; the sleeve's hoop stress sets the ceiling." }
            isReal(definition.pressure, { (unitless) : [1, 5, 20] } as RealBoundSpec);

            annotation { "Name" : "Pry sections", "Default" : true,
                         "Description" : "Two short 45 deg V pockets across each joint on the outside, floor to sleeve and sleeve to piston flange, opposite each other and clear of the clocking keys, to start a wedge or a screwdriver when the pieces have glued themselves together with bled resin (build 114). Off: plain." }
            definition.pryOn is boolean;

            if (definition.pryOn)
            {
                annotation { "Name" : "Pry length",
                             "Description" : "Length of each pocket round the outside. Limits 5-40 mm. Typical 15; held short enough on a small mold that its ends keep half the depth." }
                isLength(definition.pryLength, { (millimeter) : [5, 15, 40] } as LengthBoundSpec);

                annotation { "Name" : "Pry depth",
                             "Description" : "How far each pocket runs into each piece; the V opens 2 x this. Limits 1-5 mm. Typical 2." }
                isLength(definition.pryDepth, { (millimeter) : [1, 2, 5] } as LengthBoundSpec);
            }
        }

        annotation { "Group Name" : "Charge", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Fibre wt %",
                         "Description" : "Carbon as a share of the charge's weight. Limits 40-70. Typical 60 (Easy Composites), about 47 % by volume." }
            isReal(definition.fibrePct, { (unitless) : [40, 60, 70] } as RealBoundSpec);

            annotation { "Name" : "Long tow %",
                         "Description" : "Share of the carbon laid as continuous tow, looped round the bore and the pin slot where the load is; the rest chopped. Limits 0-60. Typical 30: more will not flow round the cavity." }
            isReal(definition.longTowPct, { (unitless) : [0, 30, 60] } as RealBoundSpec);
        }

        annotation { "Group Name" : "Strength", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Drive torque",
                         "Description" : "The most torque the shaft puts through the core, for the strength check. Limits 0-100 Nm. Typical 10-14 for an eFoil prop at full current; 0 skips the check." }
            isReal(definition.torque, { (unitless) : [0, 12, 100] } as RealBoundSpec);

            annotation { "Name" : "Pin dia",
                         "Description" : "Diameter of the cross pin through the shaft that drives the core through a slot in one end. Limits 0-12 mm. Typical 4; 0 for no pin." }
            isLength(definition.pinDia, { (millimeter) : [0, 4, 12] } as LengthBoundSpec);

            annotation { "Name" : "Pin length",
                         "Description" : "Overall length of the cross pin; the slot bears on the part of it outside the shaft (the bore). Limits 0-60 mm. Typical 20." }
            isLength(definition.pinLen, { (millimeter) : [0, 20, 60] } as LengthBoundSpec);
        }
    }
    {
        setVariable(context, "forgeLog", "");
        setVariable(context, "forgeNotice", "");
        propPrint(context, "forgeLog", "==== Core forging mold, build " ~ PROP_FILE_BUILD ~ " ====");
        // ---- the core ----
        var part = qNothing();
        var picks = evaluateQuery(context, definition.part);
        if (size(picks) == 1)
            part = picks[0];
        else
        {
            const stamped = evaluateQuery(context, qHasAttribute("propCoreBody"));
            if (size(stamped) != 1)
                throw regenError("Select the hub core as Core (" ~ size(stamped) ~ " bodies are stamped as hub cores)", ["part"]);
            part = stamped[0];
        }
        const pBox = evBox3d(context, { "topology" : part, "tight" : true });
        const zLo = pBox.minCorner[2] / millimeter;
        const zHi = pBox.maxCorner[2] / millimeter;
        const H = zHi - zLo;
        const axb = fgAxisBore(context, part, pBox);
        const ax = axb.ax;
        const bore = axb.bore;
        if (bore <= 0)
            throw regenError("The core has no bore on its axis: the mold closes with a bolt through the tube that forms the bore", ["part"]);
        const rFar = 2 * max(pBox.maxCorner[0] - pBox.minCorner[0], pBox.maxCorner[1] - pBox.minCorner[1]) / millimeter + 10;
        // the outside as a frustum through the radii just inside each end
        const e = min(0.3, H / 20);
        const rA = fgOuterR(context, part, ax, zLo + e, rFar);
        const rB = fgOuterR(context, part, ax, zHi - e, rFar);
        const slope = (rB - rA) / (H - 2 * e);
        const rLo = rA - slope * e;
        const rHi = rB + slope * e;
        const pistonHigh = definition.pistonEnd == ForgePistonEnd.LOW_Z ? false : (definition.pistonEnd == ForgePistonEnd.HIGH_Z ? true : rHi >= rLo);
        const dir = pistonHigh ? 1 : -1;                          // from the floor toward the piston
        const zF = pistonHigh ? zLo : zHi;                        // the floor end of the core
        const zP = pistonHigh ? zHi : zLo;                        // the piston end (the piston face when closed)
        const rF = pistonHigh ? rLo : rHi;
        const rP = pistonHigh ? rHi : rLo;
        const rMax = max(rLo, rHi);
        const wall = definition.wall / millimeter;
        const rOut = rMax + wall;
        const clr = definition.clearance / millimeter;
        const stroke = definition.strokeFactor * H;
        const floorT = definition.floorT / millimeter;
        const flangeT = definition.flangeT / millimeter;
        const spig = 3;
        const tubeR = bore / 2;
        propPrint(context, "forgeLog", "[forge] core: axis x " ~ pfR(ax[0], 3) ~ ", y " ~ pfR(ax[1], 3) ~ "; bore " ~ pfR(bore, 2) ~ " mm; r " ~ pfR(rLo, 2) ~ " at z " ~ pfR(zLo, 2) ~ " to r " ~ pfR(rHi, 2) ~ " at z " ~ pfR(zHi, 2) ~
            " (" ~ pfR(H, 2) ~ " mm); piston at the " ~ (pistonHigh ? "upper" : "lower") ~ " end (r " ~ pfR(rP, 2) ~ "); stroke " ~ pfR(stroke, 1) ~ " mm");
        if ((rP - rF) * 1 < -0.05)
            propPrint(context, "forgeLog", "[forge] note: the piston end is the smaller end, so the core is pushed out through the floor end");

        // ---- the recesses in each end: the frustum less the core and the tube, by the end they open to ----
        const env = fgCone(context, id + "env", ax, zF, rF, zP, rP);
        opBoolean(context, id + "voids", { "targets" : env, "tools" : part, "operationType" : BooleanOperationType.SUBTRACTION, "keepTools" : true });
        const tubeCut = fgCyl(context, id + "tubeCutV", ax, zLo - 1, zHi + 1, tubeR + 0.02);
        opBoolean(context, id + "voidsTube", { "targets" : qCreatedBy(id + "env", EntityType.BODY), "tools" : tubeCut, "operationType" : BooleanOperationType.SUBTRACTION });
        var floorBosses = [];
        var pistonBosses = [];
        var through = 0;
        var closed = 0;
        var dropV = [];
        for (var b in evaluateQuery(context, qBodyType(qCreatedBy(id + "env", EntityType.BODY), BodyType.SOLID)))
        {
            const vb = evBox3d(context, { "topology" : b, "tight" : true });
            const reach = max(max(abs(vb.minCorner[0] / millimeter - ax[0]), abs(vb.maxCorner[0] / millimeter - ax[0])), max(abs(vb.minCorner[1] / millimeter - ax[1]), abs(vb.maxCorner[1] / millimeter - ax[1])));
            const atF = abs((pistonHigh ? vb.minCorner[2] : vb.maxCorner[2]) / millimeter - zF) < 0.05;
            const atP = abs((pistonHigh ? vb.maxCorner[2] : vb.minCorner[2]) / millimeter - zP) < 0.05;
            const vol = evVolume(context, { "entities" : b }) / (millimeter * millimeter * millimeter);
            if (vol < 1 || reach > min(rLo, rHi) - 0.4 && !(atF || atP))
                dropV = append(dropV, b);                                     // a sliver between the frustum and the core's outside
            else if (atF && atP)
            {
                through += 1;
                pistonBosses = append(pistonBosses, b);
            }
            else if (atP)
                pistonBosses = append(pistonBosses, b);
            else if (atF)
                floorBosses = append(floorBosses, b);
            else
            {
                closed += 1;
                dropV = append(dropV, b);
            }
        }
        if (size(dropV) > 0)
            opDeleteBodies(context, id + "dropV", { "entities" : qUnion(dropV) });
        propPrint(context, "forgeLog", "[forge] recesses: " ~ size(pistonBosses) ~ " in the piston end, " ~ size(floorBosses) ~ " in the floor end" ~ (through > 0 ? "; " ~ through ~ " run through (formed from the piston: drill them after)" : "") ~ (closed > 0 ? "; " ~ closed ~ " closed, filled by the charge" : ""));

        // ---- the floor plug: a flange under the sleeve, a spigot into it, the floor-end bosses ----
        const uS = zF - dir * spig;                                   // the sleeve's bottom, the flange's top
        const floorFl = fgCyl(context, id + "floorFl", ax, uS, uS - dir * floorT, rOut);
        const floorSp = fgCyl(context, id + "floorSp", ax, uS - dir * 0.01, zF, rF - clr);
        opBoolean(context, id + "floorJoin", { "tools" : qUnion(concatenateArrays([[floorFl, floorSp], floorBosses])), "operationType" : BooleanOperationType.UNION });
        // ---- the sleeve: a counterbore for the spigot, the core's frustum, the piston's bore ----
        const zTop = zP + dir * stroke;                               // the sleeve's rim
        const sleeve = fgCyl(context, id + "sleeve", ax, uS, zTop, rOut);
        const cb = fgCyl(context, id + "sleeveCb", ax, uS - dir * 0.01, zF, rF);
        const cav = fgCone(context, id + "sleeveCav", ax, zF, rF, zP, rP);
        const bo = fgCyl(context, id + "sleeveBore", ax, zP, zTop + dir * 0.01, rP);
        var sleeveTools = [cb, cav, bo];
        const leadIn = definition.leadIn == undefined ? 1 : definition.leadIn / millimeter;
        if (leadIn > 0.05)
            sleeveTools = append(sleeveTools, fgCone(context, id + "leadIn", ax, zTop + dir * 0.01, rP + leadIn + 0.01, zTop - dir * leadIn, rP - 0.01));
        opBoolean(context, id + "sleeveCut", { "targets" : sleeve, "tools" : qUnion(sleeveTools), "operationType" : BooleanOperationType.SUBTRACTION });
        // ---- the piston: the stem, the shoulder flange, the piston-end bosses ----
        const landL = min(definition.land == undefined ? 8 : definition.land / millimeter, stroke);
        const relief = definition.relief == undefined ? 0.5 : definition.relief / millimeter;
        const stemLand = fgCyl(context, id + "stemLand", ax, zP, zP + dir * landL, rP - clr);
        const stemUp = fgCyl(context, id + "stem", ax, zP + dir * (landL - 0.01), zTop + dir * 0.01, rP - max(relief, clr));
        const stem = qUnion([stemLand, stemUp]);
        const pfl = fgCyl(context, id + "pistonFl", ax, zTop, zTop + dir * flangeT, rOut);
        // the cone top: from the flange's full radius to a fender-washer seat (outside dia 3 x the bolt), 45 deg
        const coneOn = definition.coneTop != false;
        const rW = 1.5 * definition.boltDia / millimeter;
        const coneH = coneOn ? max(0, rOut - rW) : 0;
        var pTools = concatenateArrays([[stem, pfl], pistonBosses]);
        if (coneOn && coneH > 0.5)
            pTools = append(pTools, fgCone(context, id + "pistonCone", ax, zTop + dir * (flangeT - 0.01), rOut, zTop + dir * (flangeT + coneH), rW));
        opBoolean(context, id + "pistonJoin", { "tools" : qUnion(pTools), "operationType" : BooleanOperationType.UNION });

        // ---- the tube: held snug in the floor, sliding in the piston ----
        const zMin = min(uS - dir * floorT, zTop + dir * (flangeT + coneH)) - 1;
        const zMax = max(uS - dir * floorT, zTop + dir * (flangeT + coneH)) + 1;
        const holeF = fgCyl(context, id + "holeF", ax, zMin, zMax, tubeR + 0.05);
        const holeP = fgCyl(context, id + "holeP", ax, zMin, zMax, tubeR + clr + 0.05);

        // ---- clocking keys: floor to sleeve, sleeve to piston ----
        const rK = (rMax + rOut) / 2;
        const kD = min(6, wall - 3);
        const kH = 4;
        const kx = ax[0] + rK;
        const ky = ax[1];
        const k1 = fgKey(context, id + "key1", kx, ky, uS - dir * 0.5, dir, kD, kH + 0.5);
        const s1 = fgKey(context, id + "sock1", kx, ky, uS - dir * 0.01, dir, kD + 0.4, kH + 0.3);
        const k2 = fgKey(context, id + "key2", kx, ky, zTop - dir * 0.5, dir, kD, kH + 0.5);
        const s2 = fgKey(context, id + "sock2", kx, ky, zTop - dir * 0.01, dir, kD + 0.4, kH + 0.3);

        // the pieces, found by where they sit: the floor lowest along dir, the piston highest
        var pcs = fgPieces(context, id, dir);
        var floorQ = pcs.floor;
        var sleeveQ = pcs.sleeve;
        var pistonQ = pcs.piston;
        // sockets in the sleeve's bottom and the piston's flange, keys onto the floor and the sleeve's rim
        opBoolean(context, id + "sock1Cut", { "targets" : sleeveQ, "tools" : s1, "operationType" : BooleanOperationType.SUBTRACTION });
        opBoolean(context, id + "sock2Cut", { "targets" : pistonQ, "tools" : s2, "operationType" : BooleanOperationType.SUBTRACTION });
        opBoolean(context, id + "key1Join", { "tools" : qUnion([floorQ, k1]), "operationType" : BooleanOperationType.UNION });
        opBoolean(context, id + "key2Join", { "tools" : qUnion([sleeveQ, k2]), "operationType" : BooleanOperationType.UNION });
        pcs = fgPieces(context, id, dir);
        floorQ = pcs.floor;
        pistonQ = pcs.piston;
        opBoolean(context, id + "holeFCut", { "targets" : floorQ, "tools" : holeF, "operationType" : BooleanOperationType.SUBTRACTION });
        opBoolean(context, id + "holePCut", { "targets" : pistonQ, "tools" : holeP, "operationType" : BooleanOperationType.SUBTRACTION });
        pcs = fgPieces(context, id, dir);
        floorQ = pcs.floor;
        sleeveQ = pcs.sleeve;
        pistonQ = pcs.piston;
        setProperty(context, { "entities" : floorQ, "propertyType" : PropertyType.NAME, "value" : "Forge mold, floor" });
        setProperty(context, { "entities" : sleeveQ, "propertyType" : PropertyType.NAME, "value" : "Forge mold, sleeve" });
        setProperty(context, { "entities" : pistonQ, "propertyType" : PropertyType.NAME, "value" : "Forge mold, piston" });
        // ---- build 114: pry pockets across the two joints, at 90 and 270 deg from the clocking keys ----
        var pryNoteF = "";
        if (definition.pryOn == true)
        {
            const dP = definition.pryDepth / millimeter;
            const lP = min(definition.pryLength / millimeter, 2 * sqrt(rOut * dP));    // a straight pocket's ends keep half its depth on the round
            const sqP = dP * sqrt(2);
            var nP = 0;
            const joints = [[uS, qUnion([floorQ, sleeveQ])], [zTop, qUnion([sleeveQ, pistonQ])]];
            for (var j = 0; j < 2; j += 1)
            {
                for (var sgy in [1, -1])
                {
                    const c = vector(ax[0], ax[1] + sgy * rOut, joints[j][0]) * millimeter;
                    const hh = vector(lP / 2, sqP / 2, sqP / 2) * millimeter;
                    const pid = id + ("pry" ~ j ~ (sgy > 0 ? "a" : "b"));
                    fCuboid(context, pid, { "corner1" : c - hh, "corner2" : c + hh });
                    opTransform(context, pid + "rot", { "bodies" : qCreatedBy(pid, EntityType.BODY), "transform" : rotationAround(line(c, X_DIRECTION), 45 * degree) });
                    var ok = false;
                    try silent
                    {
                        opBoolean(context, pid + "cut", { "tools" : qCreatedBy(pid, EntityType.BODY), "targets" : joints[j][1], "operationType" : BooleanOperationType.SUBTRACTION });
                        ok = true;
                    }
                    if (ok)
                        nP += 1;
                    else
                        try silent { opDeleteBodies(context, pid + "del", { "entities" : qCreatedBy(pid, EntityType.BODY) }); }
                }
            }
            pryNoteF = nP > 0 ? "; " ~ nP ~ " pry pockets " ~ pfR(lP, 0) ~ " x " ~ pfR(dP, 1) ~ " mm across the floor and flange joints, opposite each other: a wedge or a screwdriver there if bled resin holds a joint" : "";
            propPrint(context, "forgeLog", "[forge] PRY: " ~ nP ~ " pockets " ~ pfR(lP, 1) ~ " mm long, " ~ pfR(dP, 1) ~ " mm into each piece");
        }
        const nBodies = size(evaluateQuery(context, qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID)));
        if (nBodies != 3)
            propPrint(context, "forgeLog", "[forge] WARNING: " ~ nBodies ~ " solid bodies where 3 pieces are expected");

        // ---- the charge ----
        const vCore = evVolume(context, { "entities" : part }) / (millimeter * millimeter * millimeter) / 1000;   // cm3
        const wf = definition.fibrePct / 100;
        const rho = 1 / (wf / 1.78 + (1 - wf) / 1.15);
        const mass = vCore * rho;
        const fib = wf * mass;
        const lt = definition.longTowPct / 100 * fib;
        const resinMix = 1.25 * fib + 5;
        const vf = (wf / 1.78) / (wf / 1.78 + (1 - wf) / 1.15);
        // ---- the closing ----
        const aP = PI * (rP * rP - tubeR * tubeR);                     // mm2
        const fN = definition.pressure * aP;                           // N
        const d = definition.boltDia / millimeter;
        const tLo = 0.12 * fN * d / 1000;
        const tHi = 0.25 * fN * d / 1000;
        const tMid = 0.18 * fN * d / 1000;
        const sBolt = fN / (0.58 * d * d);
        const tubeId = bore - 2 * definition.tubeWall / millimeter;
        const hoopK = (rOut * rOut + rMax * rMax) / (rOut * rOut - rMax * rMax);
        const sHoop = definition.pressure * hoopK;
        const tubeLen = floorT + spig + H + stroke + flangeT + coneH + (coneOn ? 6 : 2 * 6) + 4;
        const sWasher = fN / (PI * (rW * rW - d * d / 4));
        // a flat flange landing on the rim, as an annular plate loaded at the stem and held at the rim (Roark, roughly)
        const sFlange = 3 * fN / (2 * PI * flangeT * flangeT) * log(rOut / rP);
        // ---- strength: the pin slot and the core's torsion ----
        var strengthNote = "";
        if (definition.torque > 0)
        {
            const T = definition.torque * 1000;                       // N mm
            const rMin = min(rLo, rHi);
            const J = PI / 2 * (rMin ^ 4 - tubeR ^ 4);
            const tau = T * rMin / J;
            strengthNote = "; torsion through the core's smallest section " ~ pfR(tau, 1) ~ " MPa (forged carbon ~80 MPa in shear)";
            const pd = definition.pinDia / millimeter;
            const pl = definition.pinLen / millimeter;
            if (pd > 0 && pl / 2 > tubeR)
            {
                const rm = (tubeR + pl / 2) / 2;
                const fPin = T / (2 * rm);
                const aBear = pd * (pl / 2 - tubeR);
                const sB = fPin / aBear;
                strengthNote = "; pin slot " ~ pfR(fPin, 0) ~ " N each side on " ~ pfR(aBear, 1) ~ " mm2, " ~ pfR(sB, 0) ~ " MPa bearing: factor about " ~ pfR(200 / sB, 1) ~ " to " ~ pfR(300 / sB, 1) ~
                    " in forged carbon (200-300 MPa, an estimate), against " ~ pfR(90 / sB, 1) ~ " in printed PPA-CF (90) and " ~ pfR(400 / sB, 1) ~ " in aluminium (400)" ~ strengthNote;
            }
        }
        if (strengthNote != "")
            propPrint(context, "forgeLog", "[forge] strength at " ~ definition.torque ~ " Nm" ~ strengthNote);
        propPrint(context, "forgeLog", "[forge] charge for " ~ pfR(vCore, 2) ~ " cm3: " ~ pfR(mass, 1) ~ " g at " ~ pfR(rho, 2) ~ " g/cm3 (" ~ definition.fibrePct ~ " % fibre by weight, " ~ pfR(vf * 100, 0) ~ " % by volume): carbon " ~ pfR(fib, 1) ~ " g (" ~ pfR(lt, 1) ~ " g continuous tow, " ~ pfR(fib - lt, 1) ~ " g chopped), resin to mix " ~ pfR(resinMix, 0) ~ " g");
        propPrint(context, "forgeLog", "[forge] closing: piston face " ~ pfR(aP, 0) ~ " mm2; " ~ definition.pressure ~ " MPa is " ~ pfR(fN / 1000, 2) ~ " kN; M" ~ pfR(d, 0) ~ " nut torque " ~ pfR(tLo, 1) ~ " to " ~ pfR(tHi, 1) ~ " Nm (K 0.12 waxed to 0.25 dry), bolt " ~ pfR(sBolt, 0) ~ " MPa (8.8 proof 580)" ~ (tubeId < d + 0.2 ? "; WARNING: the tube's bore " ~ pfR(tubeId, 1) ~ " mm is too small for the bolt" : ""));
        propPrint(context, "forgeLog", "[forge] piston: a " ~ pfR(landL, 1) ~ " mm land at " ~ pfR(clr, 2) ~ " mm clearance, the stem above relieved " ~ pfR(max(relief, clr), 2) ~ " mm; sleeve mouth chamfered " ~ pfR(leadIn, 1) ~ " mm");
        propPrint(context, "forgeLog", "[forge] piston: " ~ (coneOn ? "cone top " ~ pfR(coneH, 1) ~ " mm to a " ~ pfR(2 * rW, 0) ~ " mm fender-washer seat, " ~ pfR(sWasher, 1) ~ " MPa under the washer (printed PETG ~50 in compression: factor " ~ pfR(50 / sWasher, 1) ~ ")" :
            "flat flange " ~ pfR(flangeT, 1) ~ " mm: if the nut is turned on after it lands, about " ~ pfR(sFlange, 0) ~ " MPa bending at the stem (PETG ~30-45 along its layers); put a steel plate over the whole flange"));
        propPrint(context, "forgeLog", "[forge] sleeve: hoop stress " ~ pfR(sHoop, 1) ~ " MPa at " ~ definition.pressure ~ " MPa (thick wall, factor " ~ pfR(hoopK, 2) ~ "), printed PETG ~30 MPa along its layers: factor " ~ pfR(30 / sHoop, 1) ~ (30 / sHoop < 2.5 ? " - THIN: more Wall, a lower Target MPa or a steel band round the sleeve" : ""));
        // ---- the technique ----
        const steps = [
            "Print in PETG at 0.1 mm layers, 5 walls, 75 % infill (the pressure is far above a matched mold's); sleeve on its floor end so its layers carry the hoop stress, piston on its flange, floor on its flange",
            "Buy: a polished steel tube " ~ pfR(bore, 1) ~ " x " ~ pfR(definition.tubeWall / millimeter, 1) ~ " mm, " ~ pfR(ceil(tubeLen / 5) * 5, 0) ~ " mm long (it forms the bore); an M" ~ pfR(d, 0) ~ " 8.8 bolt " ~ pfR(ceil((tubeLen + 12) / 10) * 10, 0) ~ " mm with a nut, " ~ (coneOn ? "a " ~ pfR(2 * rW, 0) ~ " mm fender washer for the cone's seat and a steel plate about " ~ pfR(ceil(2 * rOut / 10) * 10, 0) ~ " mm square and 6 mm thick under the floor" : "two washers and two steel plates about " ~ pfR(ceil(2 * rOut / 10) * 10, 0) ~ " mm square and 6 mm thick") ~ ", drilled for the bolt; a 2-20 Nm torque wrench",
            "Calibrate once: the empty stack on a bathroom scale at about 1 Nm gives K for your bolt and wax (force = torque / (K x " ~ pfR(d, 0) ~ " mm))",
            "Release: two coats of spray wax, 15 min apart, on every face of every piece kept apart; soft filleting wax on the tube and the bolt so the resin cannot bond to them",
            "Weigh the charge, never judge it: " ~ pfR(fib, 1) ~ " g of carbon (" ~ pfR(lt, 1) ~ " g continuous tow, " ~ pfR(fib - lt, 1) ~ " g chopped 10-25 mm and split into two equal piles) and " ~ pfR(resinMix, 0) ~ " g of slow laminating epoxy, mixed twice",
            "Assemble the floor, the sleeve and the tube on the bottom plate. Load the first pile of chopped tow in batches, wetting each; lay the continuous tow wetted in loops round the tube and, at the piston end, where the pin slot will be; the second pile on top",
            "Piston in over the tube (keys lined up), then " ~ (coneOn ? "the fender washer on the cone's seat" : "the top plate and a washer") ~ " and the nut. Close in stages a few minutes apart: " ~ pfR(tMid * 0.25, 1) ~ ", " ~ pfR(tMid * 0.5, 1) ~ ", " ~ pfR(tMid * 0.75, 1) ~ ", " ~ pfR(tMid, 1) ~ " Nm (K 0.18), so the resin bleeds out past the clearance; stop when the torque jumps as the flange lands on the sleeve, or at the last step",
            "Leave it clamped 24 hours",
            (rP >= rF ? "Demold: nut off, piston out; stand the sleeve on blocks and press the floor plug in, pushing the core out through the piston end (its bigger end); drive the tube out; scrape the flash with a blade"
                      : "Demold: nut off, floor plug off; stand the sleeve on blocks and press the piston in, pushing the core out through the floor end (its bigger end); drive the tube out; scrape the flash with a blade"),
            "Post-cure the core out of the mold to the resin's schedule; ream the bore to the shaft; key its outside with 120 grit before it goes in the prop mold (no wax on it)"];
        propPrint(context, "forgeLog", "[forge] technique:");
        for (var i = 0; i < size(steps); i += 1)
            propPrint(context, "forgeLog", "[forge]   " ~ (i + 1) ~ ". " ~ steps[i]);
        const note = "Core forging mold, build " ~ PROP_FILE_BUILD ~ ": sleeve, floor and piston for a " ~ pfR(vCore, 1) ~ " cm3 core (r " ~ pfR(rF, 1) ~ " to " ~ pfR(rP, 1) ~ " mm over " ~ pfR(H, 1) ~ " mm, bore " ~ pfR(bore, 1) ~ "), stroke " ~ pfR(stroke, 0) ~ " mm to a shoulder stop, " ~ size(pistonBosses) ~ " piston and " ~ size(floorBosses) ~ " floor bosses for the end recesses" ~ pryNoteF ~
            " | charge " ~ pfR(mass, 1) ~ " g: " ~ pfR(fib, 1) ~ " g carbon (" ~ pfR(lt, 1) ~ " continuous), resin to mix " ~ pfR(resinMix, 0) ~ " g" ~
            " | closing: M" ~ pfR(d, 0) ~ " bolt through a " ~ pfR(bore, 1) ~ " x " ~ pfR(definition.tubeWall / millimeter, 1) ~ " tube, " ~ pfR(fN / 1000, 1) ~ " kN for " ~ definition.pressure ~ " MPa, " ~ pfR(tLo, 1) ~ "-" ~ pfR(tHi, 1) ~ " Nm; sleeve hoop " ~ pfR(sHoop, 1) ~ " MPa (PETG factor " ~ pfR(30 / sHoop, 1) ~ ")" ~ (coneOn ? "; cone top, " ~ pfR(sWasher, 0) ~ " MPa under a " ~ pfR(2 * rW, 0) ~ " mm fender washer" : "; flat flange under a steel plate") ~
            (strengthNote == "" ? "" : " | strength at " ~ definition.torque ~ " Nm" ~ strengthNote) ~ " | the technique, step by step, is in forgeLog";
        propPrint(context, "forgeNotice", note);
        reportFeatureInfo(context, id, note);
    });
