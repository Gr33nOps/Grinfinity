"""Faces for everything in the arena. Original vector artwork, no external assets.

Only the planet is happy: each of the twelve planets wears one positive emotion,
from relief on the first to love on the last (see below). Every enemy kind wears one negative emotion, and the more dangerous the
enemy, the more intense its emotion, so the arena grows darker in feeling as a
run brings in tougher kinds. Each emotion comes in a few expressions, picked at
random per enemy, so a crowd never looks cloned:

  Drifter    sadness        rose rocks: forlorn, teary, sniffly, moping
             loneliness     stone rocks: longing, wistful, weary, forsaken
             anxiety        clay rocks: jittery, fretting, uneasy, tense
                            (built from parts: three rock shapes, the colour
                            giving the emotion, plus scared and shocked
                            reactions)
  Splinter   self-doubt     unsure, timid, shrinking
  Shard      irritability   huffy, twitchy, snappy, scowling (three
                            four-cornered crystal shapes)
  Fracture   frustration    fed up, exasperated, strained
  Bulwark    shame          hiding, cringing, ashamed
  Planetoid  resentment     grudging, bitter, brooding
  Flare      anger          furious, seething, roaring

  Coil        disgust         disgusted, then revolted below half health
  Brood       grief           mourning, then wailing
  Black Hole  hopelessness    empty, then despairing

No face may read as happy, and none uses floating symbols.

Run from the project root: python tools/make_moods.py
"""
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / 'art' / 'cosmic'
INK = '#321E3D'; CREAM = '#FFF0CE'; BERRY = '#AB4564'; ORANGE = '#F5A451'
TEAR = '#9AD7E8'; BLUSH = '#F08A8A'


def svg(name, body):
    (OUT / (name + '.svg')).write_text(
        f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">'
        f'<g stroke="{INK}" stroke-width="8" stroke-linecap="round" stroke-linejoin="round">{body}</g></svg>',
        encoding='utf-8')


def circle(x, y, r, fill, stroke=None, extra=''):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"' + (f' stroke="{stroke}"' if stroke else '') + f' {extra}/>'


def path(d, fill='none', extra=''):
    return f'<path d="{d}" fill="{fill}" {extra}/>'


# --- Eyes --------------------------------------------------------------------

def eye(x, y, r=13, look=(3, 4), pupil=0.55, shine=True):
    """A cartoon eye: cream white, ink pupil pushed toward `look`, one glint."""
    px, py = x + look[0], y + look[1]
    s = f'<ellipse cx="{x}" cy="{y}" rx="{r}" ry="{r * 1.3:.1f}" fill="{CREAM}" stroke-width="5"/>'
    s += circle(px, py, round(r * pupil, 1), INK, 'none')
    if shine:
        s += circle(round(px + r * 0.22, 1), round(py - r * 0.28, 1), round(r * 0.19, 1), CREAM, 'none')
    return s


def lid(x, y, r, left, right, skin, line=6):
    """Covers the top of an eye down to a slanted edge. `left`/`right` are how far
    below the eye's centre the edge sits at each side (negative is higher).
    Inner corners low reads angry; outer corners low reads sad."""
    top = y - r * 1.3 - 7
    x0, x1 = x - r - 5, x + r + 5
    s = path(f'M {x0} {top:.1f} L {x1} {top:.1f} L {x1} {y + right} L {x0} {y + left} Z', skin, 'stroke="none"')
    s += path(f'M {x - r - 3} {y + left - 1} L {x + r + 3} {y + right - 1}', 'none', f'stroke-width="{line}"')
    return s


def brow(x0, y0, x1, y1, bend=0, width=6):
    mx, my = (x0 + x1) / 2, (y0 + y1) / 2 - bend
    return path(f'M {x0} {y0} Q {mx} {my} {x1} {y1}', 'none', f'stroke-width="{width}"')


def squeezed(x, y, r, facing):
    """An eye screwed shut: a sideways chevron pointing at the nose (> <)."""
    d = r * 0.9
    tip = x + d * facing
    back = x - d * facing
    return path(f'M {back} {y - d * 0.8} L {tip} {y} L {back} {y + d * 0.8}', 'none', 'stroke-width="7"')


def tear(x, y, size=1.0):
    h, w = 20 * size, 9 * size
    return path(f'M {x} {y} C {x + w} {y + h * 0.6} {x + w} {y + h} {x} {y + h} C {x - w} {y + h} {x - w} {y + h * 0.6} {x} {y} Z',
                TEAR, 'stroke-width="4"')


def sweat(x, y):
    return tear(x, y, 0.9) + path(f'M {x - 2} {y + 9} L {x - 2} {y + 13}', 'none', f'stroke="{CREAM}" stroke-width="3"')


def vein(x, y, s=1.0):
    """The cartoon cross-shaped anger mark."""
    k = 9 * s
    arcs = ''
    for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
        cx, cy = x + dx * k, y + dy * k
        arcs += path(f'M {cx - dx * k * 0.2} {cy + dy * k * 0.9} Q {cx - dx * k * 0.1} {cy - dy * 0.1} {cx + dx * k * 0.9} {cy - dy * k * 0.2}',
                     'none', f'stroke="{BERRY}" stroke-width="5"')
    return arcs


# --- Mouths ------------------------------------------------------------------

def frown(mx, my, w, depth=12, width=7):
    return path(f'M {mx - w / 2} {my + depth / 2} Q {mx} {my - depth} {mx + w / 2} {my + depth / 2}', 'none', f'stroke-width="{width}"')


def wobble(mx, my, w, amp=5):
    step = w / 4
    x = mx - w / 2
    d = f'M {x} {my + amp}'
    for i in range(4):
        d += f' Q {x + step * (i + 0.5)} {my + (-amp if i % 2 == 0 else amp) * 1.6} {x + step * (i + 1)} {my + (amp if i % 2 == 0 else -amp) * 0.2}'
    return path(d, 'none', 'stroke-width="6"')


def gritted(mx, my, w, h=20):
    """Clenched teeth: an ink slot with a row of cream teeth."""
    x0, y0 = mx - w / 2, my - h / 2
    s = f'<rect x="{x0}" y="{y0}" width="{w}" height="{h}" rx="{h / 2}" fill="{CREAM}" stroke-width="6"/>'
    s += path(f'M {x0 + 3} {my} L {x0 + w - 3} {my}', 'none', 'stroke-width="4"')
    for i in range(1, 4):
        tx = x0 + w * i / 4
        s += path(f'M {tx} {y0 + 2} L {tx} {y0 + h - 2}', 'none', 'stroke-width="4"')
    return s


def snarl(mx, my, w):
    """Lip curled up on one side, one fang showing."""
    s = path(f'M {mx - w / 2} {my + 6} Q {mx - w * 0.1} {my - 4} {mx + w / 2} {my - 10}', 'none', 'stroke-width="7"')
    s += path(f'M {mx + w * 0.2} {my - 5} L {mx + w * 0.28} {my + 7} L {mx + w * 0.36} {my - 7} Z', CREAM, 'stroke-width="4"')
    return s


def shout(mx, my, w, h):
    """Mouth wide open, yelling: wider at the bottom, tongue showing."""
    s = path(f'M {mx - w * 0.38} {my - h / 2} L {mx + w * 0.38} {my - h / 2} Q {mx + w / 2} {my + h / 2} {mx} {my + h / 2} Q {mx - w / 2} {my + h / 2} {mx - w * 0.38} {my - h / 2} Z', INK)
    s += path(f'M {mx - w * 0.3} {my - h / 2 + 3} L {mx + w * 0.3} {my - h / 2 + 3} L {mx + w * 0.26} {my - h / 2 + 10} L {mx - w * 0.26} {my - h / 2 + 10} Z', CREAM, 'stroke="none"')
    s += f'<ellipse cx="{mx}" cy="{my + h * 0.3}" rx="{w * 0.2}" ry="{h * 0.15}" fill="{BERRY}" stroke="none"/>'
    return s


def wail(mx, my, w, h):
    """A big crying mouth: open, turned down at the corners."""
    s = path(f'M {mx - w / 2} {my + h * 0.35} Q {mx} {my - h * 0.75} {mx + w / 2} {my + h * 0.35} Q {mx} {my + h * 0.55} {mx - w / 2} {my + h * 0.35} Z', INK)
    s += f'<ellipse cx="{mx}" cy="{my + h * 0.2}" rx="{w * 0.18}" ry="{h * 0.14}" fill="{BERRY}" stroke="none"/>'
    return s


def grimace(mx, my, w, h):
    """Teeth bared in a frown: a band of teeth that turns down at both corners,
    so it can never be mistaken for a grin."""
    def quad(t, a, c):
        return (1 - t) ** 2 * a + 2 * t * (1 - t) * c + t ** 2 * a
    corner = my + h * 0.45
    top, bottom, middle = my - h * 0.95, my + h * 0.35, my - h * 0.3
    x0, x1 = mx - w / 2, mx + w / 2
    s = path(f'M {x0} {corner} Q {mx} {top} {x1} {corner} Q {mx} {bottom} {x0} {corner} Z', CREAM, 'stroke-width="6"')
    s += path(f'M {x0 + 4} {corner - 1} Q {mx} {middle} {x1 - 4} {corner - 1}', 'none', 'stroke-width="3.5"')
    for dx in (-0.2, 0, 0.2):
        t = 0.5 + dx
        tx = mx + dx * w
        s += path(f'M {tx:.1f} {quad(t, corner, top) + 2:.1f} L {tx:.1f} {quad(t, corner, bottom) - 2:.1f}', 'none', 'stroke-width="3.5"')
    return s


def jolt(lines):
    """Short strokes flying off the head: the flinch of a sudden fright."""
    return ''.join(path(d, 'none', 'stroke-width="5"') for d in lines)


def o_mouth(mx, my, r):
    return f'<ellipse cx="{mx}" cy="{my}" rx="{r * 0.8}" ry="{r}" fill="{INK}" stroke-width="5"/>' + \
        f'<ellipse cx="{mx}" cy="{my + r * 0.35}" rx="{r * 0.45}" ry="{r * 0.35}" fill="{BERRY}" stroke="none"/>'


def pout(mx, my, w):
    s = frown(mx, my, w, 14, 8)
    s += path(f'M {mx - w * 0.24} {my + 11} Q {mx} {my + 21} {mx + w * 0.24} {my + 11}', 'none', 'stroke-width="6"')
    return s


# --- Enemies -----------------------------------------------------------------

DRIFT = '#BC7F83'
drifter = path('M 37 88 L 72 42 L 145 28 L 207 59 L 229 126 L 203 192 L 139 222 L 64 204 L 26 147 Z', DRIFT)
drifter += path('M 47 158 Q 111 220 213 150 L 202 191 L 139 215 L 64 196 Z', '#945768', 'stroke="none"')


def craters(spots, colour):
    return ''.join(circle(x, y, r, colour) for x, y, r in spots)


# Glum: eyes drooping at the outer corners, looking down, a plain frown.
svg('body_drifter', drifter + craters([(69, 99, 18), (190, 76, 12)], '#945768')
    + eye(113, 118, 13, (0, 6)) + lid(113, 118, 13, 2, -8, DRIFT)
    + eye(159, 114, 13, (0, 6)) + lid(159, 114, 13, -8, 2, DRIFT)
    + brow(98, 90, 122, 84, -2) + brow(150, 80, 174, 86, -2)
    + frown(136, 160, 34, 12))
# Teary: big wet eyes, a tear rolling, wobbly mouth.
svg('body_drifter_2', drifter + craters([(194, 84, 14), (62, 146, 11)], '#945768')
    + eye(113, 116, 14, (1, 5), 0.6) + eye(159, 112, 14, (1, 5), 0.6)
    + brow(97, 88, 121, 80, -3) + brow(151, 76, 175, 84, -3)
    + tear(101, 136) + wobble(136, 160, 34, 4))
# Sulking: half-shut eyes turned away, mouth pushed to one side.
svg('body_drifter_3', drifter + craters([(92, 60, 11), (200, 132, 16), (58, 124, 9)], '#945768')
    + eye(113, 116, 13, (-5, 5)) + lid(113, 116, 13, -1, -1, DRIFT)
    + eye(159, 112, 13, (-5, 5)) + lid(159, 112, 13, -1, -1, DRIFT)
    + path('M 120 162 Q 138 154 156 164', 'none', 'stroke-width="7"'))

SHARD = '#EAA36F'

# Shards come in packs, so each is one of three shapes wearing one of four
# irritable faces, mixed at random: a pack still reads as orange Shards at a
# glance, but no two look copied.
SHARD_SHADE = '#C46E68'


def shard_shape(outline, band, shine):
    """An orange shard: `outline`, a darker band along its base clipped to it, a cream glint."""
    return (f'<defs><clipPath id="c"><path d="{outline}"/></clipPath></defs>'
            + path(outline, SHARD)
            + path(band, SHARD_SHADE, 'stroke="none" clip-path="url(#c)"')
            + path(outline, 'none')
            + path(shine, 'none', f'stroke="{CREAM}"'))


def rounded(points, r=14):
    """A closed outline through `points` with each corner rounded off."""
    import math
    d = ''
    n = len(points)
    for i in range(n):
        (px, py), (x, y), (nx, ny) = points[i - 1], points[i], points[(i + 1) % n]
        a_len = math.hypot(x - px, y - py)
        b_len = math.hypot(nx - x, ny - y)
        ax, ay = x + (px - x) * r / a_len, y + (py - y) * r / a_len
        bx, by = x + (nx - x) * r / b_len, y + (ny - y) * r / b_len
        d += (f'M {ax:.1f} {ay:.1f}' if i == 0 else f' L {ax:.1f} {ay:.1f}') + f' Q {x} {y} {bx:.1f} {by:.1f}'
    return d + ' Z'


# Every Shard has four corners, never three and never square: long, pointed
# shards of crystal. A tall kite, a leaning blade with one sharp tip, and an
# arrow lying on its side.
shard_shapes = {
    'kite': (shard_shape(rounded([(132, 10), (208, 108), (122, 246), (46, 120)], 12),
                         'M 0 182 L 256 176 L 256 256 L 0 256 Z', 'M 72 100 L 100 64'), (127, 120)),
    'blade': (shard_shape(rounded([(46, 96), (216, 18), (206, 150), (86, 234)], 12),
                          'M 0 186 L 256 164 L 256 256 L 0 256 Z', 'M 80 96 L 118 78'), (136, 128)),
    'arrow': (shard_shape(rounded([(16, 140), (150, 48), (240, 118), (140, 216)], 12),
                          'M 0 184 L 256 170 L 256 256 L 0 256 Z', 'M 60 126 L 96 100'), (148, 128)),
}


def shard_face(mood, fx, fy):
    """An irritable face with its eyes on the line `fy`, centred on `fx`."""
    lx, rx = fx - 24, fx + 25
    glare = (eye(lx, fy + 1, 13, (2, 5)) + lid(lx, fy + 1, 13, -9, 1, SHARD)
             + eye(rx, fy - 2, 13, (-2, 5)) + lid(rx, fy - 2, 13, 1, -9, SHARD))
    my = fy + 41
    if mood == 'huffy':  # "hmph": glaring, lips pressed and shoved to one side
        return (glare + path(f'M {fx - 14} {my + 2} L {fx + 10} {my - 1}', 'none', 'stroke-width="7"')
                + path(f'M {fx + 14} {my - 7} Q {fx + 22} {my - 1} {fx + 14} {my + 5}', 'none', 'stroke-width="5"'))
    if mood == 'twitchy':  # one eye glaring, the other screwed down tighter, a tight wavering mouth
        return (eye(lx, fy + 1, 13, (2, 5)) + lid(lx, fy + 1, 13, -9, 1, SHARD)
                + eye(rx, fy - 2, 11, (-2, 4)) + lid(rx, fy - 2, 11, 6, -2, SHARD)
                + wobble(fx, my, 30, 3))
    if mood == 'snappy':  # lip curled, one fang
        return glare + snarl(fx, my, 40)
    # scowling: heavy brows pressed down, a tight frown
    return (glare + path(f'M {lx - 17} {fy - 24} L {lx + 13} {fy - 15}', 'none', 'stroke-width="8"')
            + path(f'M {rx - 13} {fy - 18} L {rx + 17} {fy - 27}', 'none', 'stroke-width="8"')
            + frown(fx, my, 34, 10, 7))


SHARD_MOODS = ('huffy', 'twitchy', 'snappy', 'scowling')
for shape, (art, (fx, fy)) in shard_shapes.items():
    for mood in SHARD_MOODS:
        svg(f'body_shard_{shape}_{mood}', art + shard_face(mood, fx, fy))

PLAN = '#958BBC'
plan = circle(128, 130, 108, PLAN)
plan += path('M 27 148 Q 135 222 228 138 Q 218 228 127 237 Q 45 220 27 148', '#71668F', 'stroke="none"')
PLAN_SHADE = '#71668F'
# Resentment: a heavy, lasting grudge, for the heavy, lasting enemy.
planetoid_faces = {
    # Grudging: side-eye under level lids, a flat mouth that dips at one end.
    'grudging': ([(64, 84, 20), (188, 184, 22)],
                 eye(104, 121, 16, (8, 6)) + lid(104, 121, 16, 1, 3, PLAN, 7)
                 + eye(161, 121, 16, (8, 6)) + lid(161, 121, 16, 3, 1, PLAN, 7)
                 + brow(92, 96, 122, 100, 0, 7) + brow(143, 100, 173, 96, 0, 7)
                 + path('M 102 172 L 144 170 Q 151 170 154 177', 'none', 'stroke-width="7"')),
    # Bitter: lids slanted hard, brows down, lips pressed into a long frown.
    'bitter': ([(192, 74, 16), (56, 150, 17), (150, 208, 10)],
               eye(104, 123, 15, (1, 5)) + lid(104, 123, 15, -7, 4, PLAN, 7)
               + eye(161, 123, 15, (-1, 5)) + lid(161, 123, 15, 4, -7, PLAN, 7)
               + path('M 84 96 L 120 106', 'none', 'stroke-width="7"') + path('M 145 106 L 181 96', 'none', 'stroke-width="7"')
               + frown(128, 174, 54, 10, 7)),
    # Brooding: staring at the ground under heavy brows, bags under the eyes, a deep frown.
    'brooding': ([(70, 76, 18), (192, 176, 18)],
                 eye(104, 124, 16, (0, 9)) + lid(104, 124, 16, 2, 2, PLAN, 7)
                 + eye(161, 124, 16, (0, 9)) + lid(161, 124, 16, 2, 2, PLAN, 7)
                 + path('M 86 100 L 122 104', 'none', 'stroke-width="9"') + path('M 143 104 L 179 100', 'none', 'stroke-width="9"')
                 + path('M 92 150 Q 104 156 116 150', 'none', 'stroke-width="3"') + path('M 149 150 Q 161 156 173 150', 'none', 'stroke-width="3"')
                 + frown(128, 180, 40, 16, 7)),
}
for mood, (spots, face) in planetoid_faces.items():
    svg(f'body_planetoid_{mood}', plan + craters(spots, PLAN_SHADE) + face)

FRACT = '#88BFB7'
fract = path('M 32 85 L 91 30 L 173 36 L 228 99 L 220 170 L 163 225 L 75 214 L 25 153 Z', FRACT)
fract += path('M 168 36 L 160 74 L 186 96 L 172 126 L 204 146 L 223 156', 'none', 'stroke-width="12"')
glassy = (f'<ellipse cx="88" cy="132" rx="10" ry="3.5" fill="{TEAR}" stroke="none"/>'
          f'<ellipse cx="136" cy="132" rx="10" ry="3.5" fill="{TEAR}" stroke="none"/>')
# Frustration, for the rock that cracks under pressure.
fracture_faces = {
    # Fed up: level half-shut lids, brows jammed down, teeth bared in a frown.
    'fed_up': eye(88, 116, 14, (0, 4), 0.5) + lid(88, 116, 14, -1, -1, FRACT)
              + eye(136, 116, 14, (0, 4), 0.5) + lid(136, 116, 14, -1, -1, FRACT)
              + path('M 70 92 L 100 100', 'none', 'stroke-width="7"') + path('M 124 100 L 154 92', 'none', 'stroke-width="7"')
              + grimace(112, 162, 46, 22),
    # Exasperated: eyes rolled up, one brow cocked, a flat mouth dipping at one end.
    'exasperated': eye(88, 114, 14, (1, -8), 0.5) + eye(136, 114, 14, (1, -8), 0.5)
                   + brow(72, 90, 100, 88, 0) + brow(124, 80, 152, 74, 6)
                   + path('M 96 164 L 126 162 Q 134 162 136 170', 'none', 'stroke-width="7"') + sweat(56, 80),
    # Strained: eyes clenched into angry slants, teeth gritted in a frown, sweating.
    'strained': path('M 74 106 L 100 116', 'none', 'stroke-width="7"') + path('M 124 116 L 150 106', 'none', 'stroke-width="7"')
                + path('M 70 78 L 100 90', 'none', 'stroke-width="7"') + path('M 124 90 L 154 78', 'none', 'stroke-width="7"')
                + grimace(112, 164, 52, 26) + sweat(56, 84) + jolt(['M 38 60 L 50 70', 'M 28 88 L 44 92']),
}
for mood, face in fracture_faces.items():
    svg(f'body_fracture_{mood}', fract + face)

# Self-doubt, for the small broken-off pieces. Smooth: the crack stays with the Fracture.
splinter = path('M 32 85 L 91 30 L 173 36 L 228 99 L 220 170 L 163 225 L 75 214 L 25 153 Z', FRACT)
splinter_faces = {
    # Unsure: glancing sideways, one brow up, a small wavering mouth, sweating.
    'unsure': eye(88, 114, 15, (-6, 2), 0.45) + eye(136, 114, 15, (-6, 2), 0.45)
              + brow(72, 88, 102, 86, 0) + brow(122, 80, 152, 72, 5)
              + wobble(106, 162, 26, 3) + sweat(58, 72),
    # Timid: looking down under sad lids, eyes welling up, a small frown.
    'timid': eye(88, 116, 14, (-1, 6)) + lid(88, 116, 14, 2, -7, FRACT)
             + eye(136, 116, 14, (-1, 6)) + lid(136, 116, 14, -7, 2, FRACT)
             + brow(72, 92, 100, 84, -3) + brow(124, 84, 152, 92, -3)
             + glassy + frown(112, 166, 22, 8, 6),
    # Shrinking: big wet eyes looking up, brows pinched, biting its lip.
    'shrinking': eye(88, 112, 16, (0, -4), 0.62) + eye(136, 112, 16, (0, -4), 0.62)
                 + brow(72, 86, 100, 76, -4) + brow(124, 76, 152, 86, -4)
                 + grimace(112, 164, 32, 16) + sweat(58, 74),
}
for mood, face in splinter_faces.items():
    svg(f'body_splinter_{mood}', splinter + face)

pts = []
import math
for i in range(24):
    a = math.tau * i / 24
    r = 115 if i % 2 == 0 else 88
    pts.append(f'{128 + math.cos(a) * r:.1f},{128 + math.sin(a) * r:.1f}')
flare = f'<polygon points="{" ".join(pts)}" fill="#E7876F"/>' + circle(128, 128, 75, ORANGE)
flare += path('M 65 121 Q 71 85 99 76', 'none', f'stroke="{CREAM}" stroke-width="7"')
# Anger, for the one that explodes.
flare_faces = {
    # Furious: glaring down hard, yelling.
    'furious': eye(105, 118, 13, (3, 5)) + lid(105, 118, 13, -10, 3, ORANGE, 7)
               + eye(151, 118, 13, (-3, 5)) + lid(151, 118, 13, 3, -10, ORANGE, 7)
               + shout(128, 163, 42, 26),
    # Seething: lids slammed down, teeth bared in a wide frown.
    'seething': eye(105, 120, 13, (3, 5)) + lid(105, 120, 13, -12, 6, ORANGE, 8)
                + eye(151, 120, 13, (-3, 5)) + lid(151, 120, 13, 6, -12, ORANGE, 8)
                + grimace(128, 166, 56, 26),
    # Roaring: eyes blazing wide, brows down hard, mouth open as far as it goes.
    'roaring': eye(105, 116, 15, (0, 0), 0.26) + eye(151, 116, 15, (0, 0), 0.26)
               + path('M 84 88 L 120 102', 'none', 'stroke-width="9"') + path('M 136 102 L 172 88', 'none', 'stroke-width="9"')
               + shout(128, 168, 54, 40),
}
for mood, face in flare_faces.items():
    svg(f'body_flare_{mood}', flare + face)

BUL = '#769CB4'; BUL_SHADE = '#52758F'
bul = path('M 37 61 Q 127 5 222 63 L 215 165 Q 169 224 125 239 Q 77 226 36 165 Z', BUL)
bul += path('M 132 26 L 218 67 L 210 162 Q 173 211 132 231 Z', BUL_SHADE, 'stroke="none"')
bul += path('M 56 76 Q 130 40 200 78', 'none', f'stroke="{CREAM}" stroke-width="10"')
shame_cheeks = ''
for cx in (78, 178):
    shame_cheeks += f'<ellipse cx="{cx}" cy="152" rx="17" ry="9" fill="{BLUSH}" stroke="none" opacity=".6"/>'
    for dx in (-7, 0, 7):
        shame_cheeks += path(f'M {cx + dx + 3} 147 L {cx + dx - 3} 157', 'none', f'stroke="{BERRY}" stroke-width="2.5" opacity=".7"')
# Shame, for the one that hides behind its armour.
bulwark_faces = {
    # Hiding: looking down and away, brows pinched up, cheeks burning, a wavering mouth.
    'hiding': eye(99, 124, 13, (-6, 6)) + lid(99, 124, 13, 2, -6, BUL)
              + eye(154, 124, 13, (-6, 6)) + lid(154, 124, 13, -6, 2, BUL_SHADE)
              + brow(84, 100, 112, 92, -3) + brow(141, 92, 169, 100, -3)
              + shame_cheeks + wobble(127, 172, 30, 3),
    # Cringing: eyes shut and drooping, teeth bared in a frown, sweating.
    'cringing': path('M 86 128 L 112 120', 'none', 'stroke-width="7"') + path('M 141 120 L 167 128', 'none', 'stroke-width="7"')
                + brow(84, 96, 112, 84, -3) + brow(141, 84, 169, 96, -3)
                + grimace(127, 172, 44, 22) + sweat(198, 96),
    # Ashamed: staring at the floor, a tear, a small tight frown.
    'ashamed': eye(99, 126, 13, (0, 7)) + lid(99, 126, 13, 3, -5, BUL)
               + eye(154, 126, 13, (0, 7)) + lid(154, 126, 13, -5, 3, BUL_SHADE)
               + brow(84, 102, 112, 94, -3) + brow(141, 94, 169, 102, -3)
               + tear(88, 142, 0.8) + frown(127, 174, 24, 10, 6),
}
for mood, face in bulwark_faces.items():
    svg(f'body_bulwark_{mood}', bul + face)

# --- Bosses ------------------------------------------------------------------

coil = ''
for a in (0, 90, 180, 270):
    coil += f'<g transform="rotate({a} 128 128)">' + path('M 107 76 Q 51 15 34 46 Q 17 77 85 111', BERRY) + circle(42, 53, 12, ORANGE) + '</g>'
COIL = '#F0CBE0'
coil += circle(128, 128, 71, '#C69AC9') + circle(128, 128, 51, COIL)
nose_scrunch = path('M 117 134 Q 123 130 129 134', 'none', 'stroke-width="3"') + path('M 123 140 Q 129 136 135 140', 'none', 'stroke-width="3"')
# Disgust. Disgusted: one eye narrowed, one brow cocked, nose scrunched, lip curled.
svg('boss_coil', coil
    + eye(109, 118, 12, (3, 3)) + lid(109, 118, 12, -2, -2, COIL)
    + eye(149, 116, 12, (-2, 3)) + brow(137, 94, 161, 92, 8)
    + nose_scrunch + snarl(129, 156, 34))
# Revolted: both eyes squeezed down to slits, brows twisted, a queasy wavering mouth.
svg('boss_coil_2', coil
    + eye(109, 118, 12, (2, 3)) + lid(109, 118, 12, 2, -2, COIL)
    + eye(149, 116, 12, (-2, 3)) + lid(149, 116, 12, -2, 2, COIL)
    + path('M 96 100 L 120 106', 'none', 'stroke-width="6"') + path('M 138 104 L 162 94', 'none', 'stroke-width="6"')
    + nose_scrunch + wobble(129, 158, 40, 5))

BROOD = '#A8C596'
brood = ''.join(circle(x, y, r, '#84B0A0') for x, y, r in [(49, 77, 30), (207, 67, 29), (211, 191, 28), (43, 185, 33)])
brood += path('M 52 91 Q 50 36 126 29 Q 211 41 212 124 Q 224 207 136 230 Q 45 227 40 149 Z', BROOD)
brood += path('M 48 163 Q 128 218 206 155 Q 193 223 127 226 Q 69 219 48 163', '#769F8C', 'stroke="none"')
brood += circle(63, 132, 11, '#769F8C', 'none') + circle(189, 134, 14, '#769F8C', 'none')
# Grief. Mourning: eyes cast down, tears falling, a trembling frown.
svg('boss_brood', brood
    + eye(101, 112, 17, (0, 7)) + lid(101, 112, 17, 2, -9, BROOD)
    + eye(158, 112, 17, (0, 7)) + lid(158, 112, 17, -9, 2, BROOD)
    + brow(80, 84, 116, 76, -4) + brow(142, 76, 178, 84, -4)
    + tear(92, 138, 1.1) + tear(168, 138, 1.1)
    + wobble(129, 178, 52, 5))
# Wailing: eyes screwed shut, tears pouring, mouth wide open.
svg('boss_brood_2', brood
    + squeezed(101, 110, 18, 1) + squeezed(158, 110, 18, -1)
    + brow(80, 84, 116, 78, 5) + brow(142, 78, 178, 84, 5)
    + tear(86, 128, 1.2) + tear(173, 128, 1.2)
    + wail(129, 172, 64, 40))

# The Black Hole keeps its eclipse look; its face sits on the dark core.
hole = f'<g transform="rotate(-18 128 128)">'
hole += f'<ellipse cx="128" cy="134" rx="119" ry="44" fill="{BERRY}" stroke-width="7"/>'
hole += f'<ellipse cx="128" cy="128" rx="108" ry="32" fill="{ORANGE}" stroke="none"/>'
hole += circle(128, 117, 69, '#100f21', None, 'stroke-width="9"')
hole += path('M 67 107A63 63 0 0 1 188 96', 'none', f'stroke="{CREAM}" stroke-width="9"')
hole += path(f'M 16 131Q55 179 128 173Q209 171 240 132Q200 151 127 153Q53 155 16 131Z', ORANGE, 'stroke-width="5"')
hole += path('M 32 139Q129 181 224 139', 'none', f'stroke="{CREAM}" stroke-width="8"')
hole += path('M 66 193L92 199M184 49L202 57', 'none', f'stroke="{BERRY}" stroke-width="8"')
# Hopelessness. Empty: glowing eyes that droop at the outer corners, dim pupils
# sunk low, and a long thin mouth sagging at both ends.
empty = path('M 84 112 Q 100 96 122 102 Q 106 116 84 112 Z', ORANGE, 'stroke="none"')
empty += path('M 172 112 Q 156 96 134 102 Q 150 116 172 112 Z', ORANGE, 'stroke="none"')
empty += circle(106, 109, 3.5, CREAM, 'none') + circle(150, 109, 3.5, CREAM, 'none')
empty += path('M 94 152 Q 128 136 162 152', 'none', f'stroke="{CREAM}" stroke-width="6"')
svg('boss_black_hole', hole + empty + '</g>')
# Despairing: eyes sagging almost shut, glowing tears, a mouth that has given up.
despair = path('M 84 114 Q 102 104 122 106 Q 104 114 84 114 Z', ORANGE, 'stroke="none"')
despair += path('M 172 114 Q 154 104 134 106 Q 152 114 172 114 Z', ORANGE, 'stroke="none"')
despair += path('M 96 118 Q 100 128 96 134 Q 92 128 96 118 Z', ORANGE, 'stroke="none"')
despair += path('M 160 118 Q 164 128 160 134 Q 156 128 160 118 Z', ORANGE, 'stroke="none"')
despair += path('M 98 156 Q 128 132 158 156 Q 128 146 98 156 Z', CREAM, 'stroke="none"')
svg('boss_black_hole_2', hole + despair + '</g>')

# --- The planets: the only happy faces out here -----------------------------------
# Each planet wears one positive emotion, from the gentlest on the planet you
# start with to the strongest on the hardest to earn:
#
#    1 Easewind     relief         7 Wishfall     hope
#    2 Stillwater   calmness       8 Boldcrest    confidence
#    3 Hearthglow   contentment    9 Laurelcrown  pride
#    4 Hushmere     peacefulness  10 Sunburst     joy
#    5 Anchorlight  trust         11 Sparkrush    excitement
#    6 Gracebloom   gratitude     12 Heartsong    love
#
# Each has a blink (eyes shut, everything else the same) unless its eyes are
# already closed. Planet 1's files are face / face_blink; face_happy is the
# shared burst of joy every planet shows in Overdrive and when it scores.

GOLD = '#FFD76A'; TONGUE = '#E0677F'; HEART = '#F0677F'
EYES = (99, 155)

p_brows = path('M 84 80 Q 98 72 112 79', 'none', 'stroke-width="5"') + path('M 142 79 Q 156 72 170 80', 'none', 'stroke-width="5"')
p_cheeks = (f'<ellipse cx="72" cy="146" rx="15" ry="9" fill="{BLUSH}" stroke="none" opacity=".55"/>'
            f'<ellipse cx="182" cy="146" rx="15" ry="9" fill="{BLUSH}" stroke="none" opacity=".55"/>')
warm_cheeks = (f'<ellipse cx="72" cy="146" rx="18" ry="11" fill="{BLUSH}" stroke="none" opacity=".75"/>'
               f'<ellipse cx="182" cy="146" rx="18" ry="11" fill="{BLUSH}" stroke="none" opacity=".75"/>')
p_smile = path('M 97 146 Q 127 153 157 146 Q 153 178 127 179 Q 101 178 97 146 Z', INK, 'stroke-width="5"')
p_smile += path('M 111 170 Q 127 160 143 170 Q 136 177 127 177 Q 118 177 111 170 Z', BERRY, 'stroke="none"')
p_smile += path('M 104 149 Q 127 154 150 149', 'none', f'stroke="{CREAM}" stroke-width="4"')
shut = path('M 83 114 Q 99 124 115 114', 'none', 'stroke-width="6"') + path('M 139 114 Q 155 124 171 114', 'none', 'stroke-width="6"')
arcs = path('M 83 118 Q 99 96 115 118', 'none', 'stroke-width="7"') + path('M 139 118 Q 155 96 171 118', 'none', 'stroke-width="7"')
p_big = path('M 92 142 Q 127 152 162 142 Q 158 186 127 187 Q 96 186 92 142 Z', INK, 'stroke-width="5"')
p_big += path('M 108 174 Q 127 162 146 174 Q 138 184 127 184 Q 116 184 108 174 Z', BERRY, 'stroke="none"')
p_big += path('M 100 146 Q 127 152 154 146', 'none', f'stroke="{CREAM}" stroke-width="4"')
svg('face_happy', p_cheeks + arcs + p_big)


def p_eye(x, y=112, look=(4, 3), size=1.0, glints=1):
    """The planet's eye: big cream oval, big ink pupil, bright glints."""
    px, py = x + look[0] * size, y + look[1] * size
    e = f'<ellipse cx="{x}" cy="{y}" rx="{18 * size:.1f}" ry="{23 * size:.1f}" fill="{CREAM}" stroke-width="5"/>'
    e += f'<ellipse cx="{px:.1f}" cy="{py:.1f}" rx="{9.5 * size:.1f}" ry="{12.5 * size:.1f}" fill="{INK}" stroke="none"/>'
    e += circle(round(px + 4 * size, 1), round(py - 7 * size, 1), round(4 * size, 1), 'white', 'none')
    if glints > 1:
        e += circle(round(px - 3 * size, 1), round(py + 5 * size, 1), round(2.2 * size, 1), 'white', 'none')
    return e


def half_eye(x, y=114, look=0):
    """A relaxed, half-shut eye: only the lower half shows, under a flat lid line."""
    e = path(f'M {x - 18} {y} A 18 21 0 0 0 {x + 18} {y} Q {x} {y - 5} {x - 18} {y} Z', CREAM, 'stroke-width="5"')
    e += circle(x + look, y + 8, 8, INK, 'none') + circle(x + look + 3, y + 5, 2.6, 'white', 'none')
    e += path(f'M {x - 21} {y} Q {x} {y - 6} {x + 21} {y}', 'none', 'stroke-width="6"')
    return e


def star(cx, cy, outer, inner, points=5, fill=GOLD, extra='stroke-width="5"'):
    import math
    pts = []
    for i in range(points * 2):
        r = outer if i % 2 == 0 else inner
        a = -math.pi / 2 + i * math.pi / points
        pts.append(f'{cx + r * math.cos(a):.1f} {cy + r * math.sin(a):.1f}')
    return path('M ' + ' L '.join(pts) + ' Z', fill, extra)


def heart(x, y, size, fill=HEART, extra='stroke-width="5"'):
    """A cartoon heart centred on (x, y)."""
    s_ = size
    return path(f'M {x} {y + s_ * 0.9} C {x - s_ * 1.3} {y + s_ * 0.05} {x - s_ * 0.8} {y - s_ * 0.95} {x} {y - s_ * 0.3} '
                f'C {x + s_ * 0.8} {y - s_ * 0.95} {x + s_ * 1.3} {y + s_ * 0.05} {x} {y + s_ * 0.9} Z', fill, extra)


brows_up = path('M 82 74 Q 98 64 114 72', 'none', 'stroke-width="5"') + path('M 140 72 Q 156 64 172 74', 'none', 'stroke-width="5"')
soft_brows = path('M 86 84 Q 99 80 112 84', 'none', 'stroke-width="5"') + path('M 142 84 Q 155 80 168 84', 'none', 'stroke-width="5"')
small_smile = path('M 108 152 Q 127 166 146 152', 'none', 'stroke-width="6"')
ooh = path('M 110 150 Q 127 157 144 150 Q 142 174 127 174 Q 112 174 110 150 Z', INK, 'stroke-width="5"')
ooh += path('M 119 168 Q 127 162 135 168 Q 131 172 127 172 Q 123 172 119 168 Z', BERRY, 'stroke="none"')
sparkles = (star(56, 88, 13, 4, 4, CREAM, 'stroke-width="3"') + star(204, 80, 10, 3, 4, CREAM, 'stroke-width="3"')
            + star(208, 180, 11, 3.5, 4, CREAM, 'stroke-width="3"'))

faces = {}

# 1 Easewind, relief: eyes closed, brows lifted, letting out a happy "phew".
phew = path('M 110 150 Q 127 158 144 150 Q 140 170 127 170 Q 114 170 110 150 Z', INK, 'stroke-width="5"')
phew += path('M 118 165 Q 127 159 136 165 Q 132 169 127 169 Q 122 169 118 165 Z', BERRY, 'stroke="none"')
faces[1] = (p_cheeks + shut + brows_up + phew + sweat(186, 74), None)

# 2 Stillwater, calmness: relaxed half-shut eyes, level brows, a small easy smile.
calm_smile = path('M 112 154 Q 127 163 142 154', 'none', 'stroke-width="6"')
faces[2] = (p_cheeks + half_eye(99) + half_eye(155) + soft_brows + calm_smile,
            p_cheeks + shut + soft_brows + calm_smile)

# 3 Hearthglow, contentment: eyes curved shut in a smile, rosy, a snug closed smile.
snug = path('M 104 150 Q 127 170 150 150', 'none', 'stroke-width="6"')
faces[3] = (warm_cheeks + arcs + p_brows + snug, None)

# 4 Hushmere, peacefulness: long closed lashes, soft brows, the smallest smile.
serene = (path('M 81 116 Q 99 126 117 116', 'none', 'stroke-width="6"') + path('M 137 116 Q 155 126 173 116', 'none', 'stroke-width="6"')
          + path('M 83 118 L 77 124 M 90 122 L 86 129', 'none', 'stroke-width="4"')
          + path('M 171 118 L 177 124 M 164 122 L 168 129', 'none', 'stroke-width="4"'))
faces[4] = (p_cheeks + serene + soft_brows + path('M 117 156 Q 127 163 137 156', 'none', 'stroke-width="5"'), None)

# 5 Anchorlight, trust: warm open eyes looking right at you, a gentle smile.
gentle = path('M 102 150 Q 127 168 152 150', 'none', 'stroke-width="6"')
faces[5] = (p_cheeks + p_eye(99, look=(0, 3)) + p_eye(155, look=(0, 3)) + p_brows + gentle,
            p_cheeks + shut + p_brows + gentle)

# 6 Gracebloom, gratitude: shining eyes welling with happy tears, a trembling smile.
welling = (f'<ellipse cx="99" cy="134" rx="13" ry="4" fill="{TEAR}" stroke="none"/>'
           f'<ellipse cx="155" cy="134" rx="13" ry="4" fill="{TEAR}" stroke="none"/>')
touched = path('M 102 150 Q 114 162 127 155 Q 140 162 152 150', 'none', 'stroke-width="6"')
faces[6] = (warm_cheeks + p_eye(99, look=(0, 1), size=1.05, glints=2) + p_eye(155, look=(0, 1), size=1.05, glints=2) + welling + brows_up + touched,
            warm_cheeks + shut + brows_up + touched)

# 7 Wishfall, hope: eyes turned up to the stars and shining, brows lifted, a little "oh".
faces[7] = (p_cheeks + p_eye(99, look=(2, -7), glints=2) + p_eye(155, look=(2, -7), glints=2) + brows_up + ooh,
            p_cheeks + shut + brows_up + ooh)

# 8 Boldcrest, confidence: firm brows, a steady look, a broad closed smile.
firm_brows = path('M 83 80 L 113 80', 'none', 'stroke-width="7"') + path('M 141 80 L 171 80', 'none', 'stroke-width="7"')
broad = path('M 90 148 Q 127 176 164 148', 'none', 'stroke-width="7"')
broad += path('M 85 143 Q 87 149 93 152 M 169 143 Q 167 149 161 152', 'none', 'stroke-width="4"')
faces[8] = (p_cheeks + p_eye(99, look=(0, 3)) + p_eye(155, look=(0, 3)) + firm_brows + broad,
            p_cheeks + shut + firm_brows + broad)

# 9 Laurelcrown, pride: eyes shut in satisfaction, brows arched high, a big toothy grin.
grin = path('M 88 144 Q 127 156 166 144 Q 160 180 127 182 Q 94 180 88 144 Z', INK, 'stroke-width="5"')
grin += path('M 95 149 Q 127 159 159 149 L 156 162 Q 127 170 98 162 Z', CREAM, 'stroke="none"')
grin += path('M 112 155 L 112 166 M 127 157 L 127 169 M 142 155 L 142 166', 'none', 'stroke-width="3"')
proud = path('M 83 114 Q 99 100 115 114', 'none', 'stroke-width="7"') + path('M 139 114 Q 155 100 171 114', 'none', 'stroke-width="7"')
arched = path('M 80 80 Q 98 62 116 74', 'none', 'stroke-width="6"') + path('M 138 74 Q 156 62 174 80', 'none', 'stroke-width="6"')
faces[9] = (p_cheeks + proud + arched + grin, None)

# 10 Sunburst, joy: eyes squeezed up in delight, rosy, laughing wide open.
faces[10] = (warm_cheeks + arcs + brows_up + p_big, None)

# 11 Sparkrush, excitement: star eyes, sparkles all round, a huge beaming grin.
starry = star(99, 113, 24, 11) + star(155, 113, 24, 11)
beam = path('M 86 140 Q 127 154 168 140 Q 164 190 127 191 Q 90 190 86 140 Z', INK, 'stroke-width="5"')
beam += path('M 93 145 Q 127 157 161 145 L 159 155 Q 127 165 95 155 Z', CREAM, 'stroke="none"')
beam += path('M 106 178 Q 127 164 148 178 Q 138 188 127 188 Q 116 188 106 178 Z', BERRY, 'stroke="none"')
faces[11] = (p_cheeks + sparkles + starry + brows_up + beam, p_cheeks + sparkles + shut + brows_up + beam)

# 12 Heartsong, love: heart eyes, glowing cheeks, a big warm smile.
hearts = heart(99, 114, 22) + heart(155, 114, 22)
hearts += path('M 88 106 Q 92 101 98 101', 'none', f'stroke="{CREAM}" stroke-width="4"') + path('M 144 106 Q 148 101 154 101', 'none', f'stroke="{CREAM}" stroke-width="4"')
faces[12] = (warm_cheeks + hearts + brows_up + p_smile, warm_cheeks + shut + brows_up + p_smile)

for n, (look, blink) in faces.items():
    name = 'face' if n == 1 else f'face_{n}'
    svg(name, look)
    svg(f'{name}_blink', blink or look)


# --- Drifters, built from parts ----------------------------------------------------
# The most common enemy by far, so a crowd of them must not look stamped out. In
# play a Drifter is a body (three shapes, three rock colours) with a face laid on
# top (eight unhappy moods, each with a blink), plus two reactions: scared when it
# gets close to the planet, and shocked when a neighbour pops. The parts mix
# freely, so no two Drifters in a crowd need to match. The eyelids are drawn in
# the rock's own colour, so each colour has its own set of faces.

ROCKS = {'rose': ('#BC7F83', '#945768'), 'stone': ('#A6979B', '#7A6A70'), 'clay': ('#C9A07E', '#9C7458')}
SHAPES = {
    'jagged': ('M 37 88 L 72 42 L 145 28 L 207 59 L 229 126 L 203 192 L 139 222 L 64 204 L 26 147 Z',
               [(69, 99, 18), (190, 76, 12)]),
    'pebble': ('M 24 128 Q 22 70 84 48 Q 140 30 196 52 Q 238 74 236 130 Q 234 186 178 208 Q 124 226 70 206 Q 26 186 24 128 Z',
               [(58, 112, 14), (204, 150, 11), (182, 66, 8)]),
    'lumpy': ('M 46 82 Q 44 48 80 44 Q 102 22 136 34 Q 170 22 194 48 Q 232 58 226 100 Q 244 136 222 166 Q 216 208 176 212 '
              'Q 144 234 108 220 Q 66 222 50 190 Q 20 164 32 128 Q 24 100 46 82 Z',
              [(72, 178, 12), (198, 86, 13), (56, 112, 8)]),
}
for shape, (outline, spots) in SHAPES.items():
    for rock, (base, shade) in ROCKS.items():
        clip = f'<clipPath id="c"><path d="{outline}"/></clipPath>'
        body = clip + path(outline, base)
        # The lower part of the rock in shadow, clipped to its outline.
        body += f'<g clip-path="url(#c)"><ellipse cx="140" cy="240" rx="150" ry="78" fill="{shade}" stroke="none"/></g>'
        body += path(outline, 'none')
        body += craters(spots, shade)
        svg(f'drifter_body_{shape}_{rock}', body)

worried = brow(98, 90, 122, 81, -2) + brow(150, 78, 174, 87, -2)
shut_sad = path('M 101 121 Q 113 127 125 121', 'none', 'stroke-width="6"') + path('M 147 117 Q 159 123 171 117', 'none', 'stroke-width="6"')


def moods(skin):
    """Each mood as (eyes, everything else), so the blink can swap just the eyes."""
    return {
        # Forlorn: eyes drooping at the outer corners, looking down, a plain frown.
        'forlorn': (eye(113, 118, 13, (0, 6)) + lid(113, 118, 13, 2, -8, skin) + eye(159, 114, 13, (0, 6)) + lid(159, 114, 13, -8, 2, skin),
                 brow(98, 90, 122, 84, -2) + brow(150, 80, 174, 86, -2) + frown(136, 160, 34, 12)),
        'teary': (eye(113, 116, 14, (1, 5), 0.6) + eye(159, 112, 14, (1, 5), 0.6),
                  brow(97, 88, 121, 80, -3) + brow(151, 76, 175, 84, -3) + tear(101, 136) + wobble(136, 160, 34, 4)),
        # Moping: half-shut eyes turned away, mouth pushed to one side.
        'moping': (eye(113, 116, 13, (-5, 5)) + lid(113, 116, 13, -1, -1, skin) + eye(159, 112, 13, (-5, 5)) + lid(159, 112, 13, -1, -1, skin),
                    path('M 120 162 Q 138 154 156 164', 'none', 'stroke-width="7"')),
        # Weary of being alone: heavy lids, bags under the eyes, a flat line of a mouth.
        'weary': (eye(113, 118, 13, (0, 7)) + lid(113, 118, 13, 4, 4, skin) + eye(159, 114, 13, (0, 7)) + lid(159, 114, 13, 4, 4, skin),
                   path('M 101 141 Q 113 147 125 141', 'none', 'stroke-width="3"') + path('M 147 137 Q 159 143 171 137', 'none', 'stroke-width="3"')
                   + path('M 124 165 L 150 163', 'none', 'stroke-width="6"')),
        # Jittery: eyes darting sideways, brows up, a sweat drop, a shaky mouth.
        'jittery': (eye(113, 116, 13, (6, 2), 0.5) + eye(159, 112, 13, (6, 2), 0.5),
                    worried + sweat(188, 86) + wobble(136, 162, 28, 3)),
        # Longing: big pleading eyes looking up, bottom lip pushed out.
        'longing': (eye(113, 116, 14, (0, -3), 0.62) + eye(159, 112, 14, (0, -3), 0.62),
                  worried + pout(136, 158, 30)),
        # Sniffly: droopy eyes, a pink nose and a drip.
        'sniffly': (eye(113, 118, 13, (0, 5)) + lid(113, 118, 13, 0, -6, skin) + eye(159, 114, 13, (0, 5)) + lid(159, 114, 13, -6, 0, skin),
                    f'<ellipse cx="136" cy="140" rx="10" ry="7" fill="{BLUSH}" stroke="none"/>'
                    + tear(143, 145, 0.55) + frown(134, 168, 24, 8, 6)),
        # Forsaken: staring far off to one side under drooping lids, a single tear.
        'forsaken': (eye(113, 118, 13, (-7, 3)) + lid(113, 118, 13, 2, -7, skin) + eye(159, 114, 13, (-7, 3)) + lid(159, 114, 13, -7, 2, skin),
                     brow(98, 90, 122, 84, -2) + brow(150, 80, 174, 86, -2)
                     + tear(172, 132, 0.7) + path('M 124 166 L 148 163', 'none', 'stroke-width="6"')),
        # Fretting: looking down and aside, brows pinched high, biting its lip, sweating.
        'fretting': (eye(113, 116, 13, (-3, 5), 0.5) + eye(159, 112, 13, (-3, 5), 0.5),
                     worried + grimace(136, 162, 30, 16) + sweat(80, 94)),
        # Uneasy: a sideways glance, one brow cocked, a crooked frown.
        'uneasy': (eye(113, 116, 13, (5, 1), 0.5) + eye(159, 112, 13, (5, 1), 0.5),
                   brow(98, 90, 122, 88, 0) + brow(150, 78, 174, 72, 5)
                   + path('M 122 166 Q 136 157 152 163', 'none', 'stroke-width="6"')),
        # Tense: staring wide, brows up, lips pressed tight, a bead of sweat.
        'tense': (eye(113, 116, 14, (0, 0), 0.3) + eye(159, 112, 14, (0, 0), 0.3),
                  brow(97, 86, 122, 78, -3) + brow(150, 76, 175, 84, -3)
                  + path('M 120 164 L 152 164', 'none', 'stroke-width="6"')
                  + path('M 118 159 L 120 164 L 118 169 M 154 159 L 152 164 L 154 169', 'none', 'stroke-width="4"')
                  + sweat(188, 84)),
        # Wistful: gazing up and away, brows gently pinched, eyes welling, a small frown.
        'wistful': (eye(113, 116, 13, (-5, -4), 0.5) + eye(159, 112, 13, (-5, -4), 0.5),
                    brow(98, 88, 122, 82, -2) + brow(150, 78, 174, 84, -2)
                    + f'<ellipse cx="113" cy="132" rx="9" ry="3" fill="{TEAR}" stroke="none"/>'
                    + f'<ellipse cx="159" cy="128" rx="9" ry="3" fill="{TEAR}" stroke="none"/>'
                    + frown(134, 164, 22, 8, 6)),
    }


# The rock's colour gives a Drifter its emotion, each in four expressions.
EMOTIONS = {
    'rose': ('forlorn', 'teary', 'sniffly', 'moping'),      # sadness
    'stone': ('longing', 'wistful', 'weary', 'forsaken'),   # loneliness
    'clay': ('jittery', 'fretting', 'uneasy', 'tense'),     # anxiety
}
for rock, (base, _) in ROCKS.items():
    for mood, (eyes, rest) in moods(base).items():
        if mood not in EMOTIONS[rock]:
            continue
        svg(f'drifter_face_{rock}_{mood}', eyes + rest)
        svg(f'drifter_face_{rock}_{mood}_blink', shut_sad + rest)

# Reactions. Each Drifter has its own fear face (its mood's) and one shocked face of
# four, so a crowd reacting at once still reacts in different ways.
high_worry = brow(96, 84, 122, 76, -3) + brow(150, 74, 176, 82, -3)


def spiral(x, y, r):
    """A dizzy spiral eye."""
    import math
    points = []
    for k in range(40):
        t = k / 39 * 3 * math.pi
        rr = r * (0.12 + 0.88 * k / 39)
        points.append(f'{x + rr * math.cos(t):.1f} {y + rr * math.sin(t):.1f}')
    return (f'<circle cx="{x}" cy="{y}" r="{r + 2}" fill="{CREAM}" stroke-width="5"/>'
            + path('M ' + ' L '.join(points), 'none', 'stroke-width="4"'))


def shiny(x, y, r):
    """A huge pleading eye: big pupil, two glints."""
    return (eye(x, y, r, (0, -2), 0.7)
            + circle(round(x - r * 0.25, 1), round(y + r * 0.3, 1), round(r * 0.14, 1), CREAM, 'none'))


# Fear, close to the planet: each mood panics in its own way, so a crowd bunched
# around you never wears one face. Drawn to differ in outline at play size, not
# just in small details.
scared_faces = {
    # Forlorn dreads it: wide wet eyes, a big wavering frown.
    'fear_forlorn': eye(113, 114, 15, (0, 3), 0.3) + eye(159, 110, 15, (0, 3), 0.3) + high_worry
                 + f'<ellipse cx="113" cy="131" rx="11" ry="3.5" fill="{TEAR}" stroke="none"/>'
                 + f'<ellipse cx="159" cy="127" rx="11" ry="3.5" fill="{TEAR}" stroke="none"/>'
                 + path('M 108 174 Q 136 142 164 174 Q 150 164 136 166 Q 122 164 108 174 Z', INK, 'stroke-width="5"'),
    # Teary bawls: eyes screwed shut, a huge wail, tears everywhere.
    'fear_teary': squeezed(113, 116, 14, 1) + squeezed(159, 112, 14, -1) + high_worry
                  + wail(136, 168, 50, 34) + tear(92, 128, 1.2) + tear(182, 124, 1.2) + tear(86, 156, 0.8),
    # Moping grimaces: eyes narrowed under low brows, teeth bared in a frown, sweating.
    'fear_moping': eye(113, 118, 12, (0, 1), 0.42) + eye(159, 114, 12, (0, 1), 0.42)
                    + brow(97, 100, 126, 93, -2) + brow(146, 91, 175, 97, -2)
                    + grimace(140, 164, 50, 22) + sweat(190, 90),
    # Weary is jolted wide awake: enormous eyes, a tiny o, shock lines.
    'fear_weary': eye(113, 112, 18, (0, 0), 0.2) + eye(159, 108, 18, (0, 0), 0.2)
                   + brow(96, 78, 124, 70, 6) + brow(146, 68, 174, 74, 6) + o_mouth(136, 170, 7)
                   + jolt(['M 118 36 L 122 52', 'M 142 32 L 142 48', 'M 166 36 L 162 52']),
    # Jittery goes to pieces: spiral eyes, a wobbling mouth, sweat both sides.
    'fear_jittery': spiral(113, 114, 15) + spiral(159, 110, 15)
                    + wobble(136, 164, 46, 6) + sweat(192, 84) + sweat(78, 96),
    # Longing begs: huge shiny eyes, a trembling lip, one tear.
    'fear_longing': shiny(113, 114, 17) + shiny(159, 110, 17) + high_worry + pout(136, 162, 30)
                  + tear(98, 136, 0.8) + jolt(['M 112 186 L 116 182', 'M 160 186 L 156 182']),
    # Sniffly screams: wide eyes with pin-prick pupils, a howling mouth, a runny tear.
    'fear_sniffly': eye(113, 114, 15, (0, 0), 0.22) + eye(159, 110, 15, (0, 0), 0.22) + high_worry
                    + wail(136, 170, 46, 40) + tear(94, 132, 0.9)
                    + jolt(['M 118 38 L 122 54', 'M 154 34 L 152 50']),
    # Forsaken breaks down: eyes shut and drooping, tears streaming, a shaking mouth.
    'fear_forsaken': path('M 100 122 L 126 113', 'none', 'stroke-width="7"') + path('M 146 109 L 172 118', 'none', 'stroke-width="7"')
                     + high_worry + tear(98, 126, 1.1) + tear(170, 122, 1.1) + wobble(136, 166, 36, 5),
    # Fretting panics: pin-prick eyes, teeth bared in a frown, sweat flying.
    'fear_fretting': eye(113, 114, 15, (0, 0), 0.22) + eye(159, 110, 15, (0, 0), 0.22) + high_worry
                     + grimace(136, 166, 46, 22) + sweat(80, 92) + sweat(192, 84),
    # Uneasy bolts: eyes swung hard to the side, a wobbling mouth, jolt lines.
    'fear_uneasy': eye(113, 114, 15, (9, 0), 0.25) + eye(159, 110, 15, (9, 0), 0.25) + high_worry
                   + wobble(136, 166, 34, 5) + jolt(['M 118 38 L 122 54', 'M 154 34 L 152 50']),
    # Tense freezes: huge eyes, brows flying up, a tiny trembling mouth, sweating.
    'fear_tense': eye(113, 112, 18, (0, 0), 0.2) + eye(159, 108, 18, (0, 0), 0.2)
                  + brow(94, 76, 124, 66, 6) + brow(146, 64, 176, 74, 6)
                  + wobble(136, 170, 20, 3) + sweat(190, 80),
    # Wistful trembles: eyes wide and staring up, tears spilling, a shaking mouth.
    'fear_wistful': eye(113, 112, 15, (0, -2), 0.25) + eye(159, 108, 15, (0, -2), 0.25) + high_worry
                    + wobble(136, 166, 36, 5) + tear(92, 128) + tear(180, 124)
                    + jolt(['M 118 38 L 122 54', 'M 154 34 L 152 50']),
}
shocked_faces = {
    # Gasp: brows flying up, round eyes, mouth a big O.
    'shocked': eye(113, 116, 15, (0, 0), 0.34) + eye(159, 112, 15, (0, 0), 0.34)
               + brow(98, 80, 124, 74, 6) + brow(148, 72, 174, 78, 6) + o_mouth(136, 164, 13),
    # Jaw drop: pin-prick pupils and a long open mouth.
    'shocked_2': eye(113, 112, 14, (0, 0), 0.2) + eye(159, 108, 14, (0, 0), 0.2)
                 + brow(98, 76, 124, 70, 8) + brow(148, 68, 174, 74, 8)
                 + f'<ellipse cx="136" cy="168" rx="11" ry="20" fill="{INK}" stroke-width="5"/>'
                 + f'<ellipse cx="136" cy="178" rx="6" ry="6" fill="{BERRY}" stroke="none"/>',
    # Double take: one eye popping, a small gasp, a bead of sweat.
    'shocked_3': eye(113, 116, 17, (0, -2), 0.3) + eye(159, 113, 11, (0, -2), 0.35)
                 + brow(94, 82, 124, 70, 6) + brow(150, 88, 172, 86, 0)
                 + o_mouth(130, 164, 9) + sweat(190, 84),
    # Flinch: eyes clamped shut and drooping, brows pinched up, a wobbling frown, jolt lines.
    'shocked_4': path('M 100 122 L 126 113', 'none', 'stroke-width="7"') + path('M 146 109 L 172 118', 'none', 'stroke-width="7"')
                 + high_worry + wobble(136, 166, 38, 5)
                 + jolt(['M 62 58 L 72 72', 'M 46 84 L 62 90', 'M 86 38 L 90 54']),
}
for name, face in {**scared_faces, **shocked_faces}.items():
    svg(f'drifter_face_{name}', face)

print('Wrote the planet, enemy, boss and Drifter faces')
