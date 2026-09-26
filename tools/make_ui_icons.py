"""Icons for the menus and screens. Original vector artwork, no external assets.

Each glyph is drawn in plain white on a 64 x 64 grid, chunky and rounded to sit
beside Lilita One, and is tinted in the game: cream on buttons, ink on the
orange primary button, orange beside a setting's name. A few pieces that carry
their own colour (the slider knob, the on/off switch) use the game's palette.

Run from the project root: python tools/make_ui_icons.py
"""
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / 'art' / 'ui'
INK = '#321E3D'; CREAM = '#FFF0CE'; ORANGE = '#F5A451'; BERRY = '#AB4564'; MUTED = '#C5A9BF'
W = 'white'


def svg(name, body, size=64, view=None, px=None):
    """`px` draws the 64-grid glyph at a smaller native size, for places Godot
    shows an icon at its own size (a field's icon, a dropdown's arrow)."""
    view = view or f'0 0 {size} {size}'
    OUT.mkdir(parents=True, exist_ok=True)
    w, h = (px, px) if px else view.split()[2:]
    (OUT / f'{name}.svg').write_text(
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="{view}">'
        f'<g fill="none" stroke-linecap="round" stroke-linejoin="round">{body}</g></svg>', encoding='utf-8')


def line(d, width=7, colour=W):
    return f'<path d="{d}" stroke="{colour}" stroke-width="{width}"/>'


def shape(d, colour=W, round_=4, extra=''):
    """A filled shape; the matching stroke rounds its corners."""
    stroke = f' stroke="{colour}" stroke-width="{round_}"' if round_ else ''
    return f'<path d="{d}" fill="{colour}"{stroke} {extra}/>'


def dot(x, y, r, colour=W):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{colour}"/>'


def ring(x, y, r, width=6, colour=W):
    return f'<circle cx="{x}" cy="{y}" r="{r}" stroke="{colour}" stroke-width="{width}"/>'


def star_points(cx, cy, outer, inner, points):
    pts = []
    for i in range(points * 2):
        r = outer if i % 2 == 0 else inner
        a = -math.pi / 2 + i * math.pi / points
        pts.append(f'{cx + r * math.cos(a):.1f} {cy + r * math.sin(a):.1f}')
    return 'M ' + ' L '.join(pts) + ' Z'


def arc_arrow(cx, cy, r, start, end, width=7, head=9):
    """An arc from `start` to `end` degrees (clockwise on screen) with an arrowhead at the end."""
    a0, a1 = math.radians(start), math.radians(end)
    x0, y0 = cx + r * math.cos(a0), cy + r * math.sin(a0)
    x1, y1 = cx + r * math.cos(a1), cy + r * math.sin(a1)
    large = 1 if (end - start) % 360 > 180 else 0
    s = line(f'M {x0:.1f} {y0:.1f} A {r} {r} 0 {large} 1 {x1:.1f} {y1:.1f}', width)
    tx, ty = -math.sin(a1), math.cos(a1)          # direction of travel
    px, py = -ty, tx
    tip = (x1 + tx * head, y1 + ty * head)
    b1 = (x1 - tx * 2 + px * head, y1 - ty * 2 + py * head)
    b2 = (x1 - tx * 2 - px * head, y1 - ty * 2 - py * head)
    s += shape(f'M {tip[0]:.1f} {tip[1]:.1f} L {b1[0]:.1f} {b1[1]:.1f} L {b2[0]:.1f} {b2[1]:.1f} Z', W, 3)
    return s


def masked(name, body, holes):
    """`body` with `holes` (black shapes) cut out of it."""
    return (f'<mask id="{name}" maskUnits="userSpaceOnUse" x="0" y="0" width="64" height="64">'
            f'<rect width="64" height="64" fill="white"/>{holes}</mask><g mask="url(#{name})">{body}</g>')


icons = {}

# --- Main menu, pause and game over ------------------------------------------
icons['play'] = shape('M 20 12 L 52 32 L 20 52 Z', W, 7)
icons['trophy'] = (shape('M 18 10 H 46 V 24 Q 46 42 32 42 Q 18 42 18 24 Z', W, 4)
                   + line('M 18 15 H 9 Q 9 30 20 31 M 46 15 H 55 Q 55 30 44 31', 5)
                   + shape('M 28 40 H 36 V 49 H 28 Z', W, 2) + shape('M 18 50 H 46 V 56 H 18 Z', W, 4))
teeth = ''.join(f'<rect x="27" y="4" width="10" height="14" rx="3" fill="white" transform="rotate({i * 45} 32 32)"/>' for i in range(8))
icons['gear'] = masked('gear', teeth + dot(32, 32, 18), dot(32, 32, 7.5, 'black'))
front = 'M 5 38 Q 32 50 59 26'
icons['planet'] = (masked('planet', dot(32, 32, 18.5), line(front, 12, 'black'))
                   + line('M 5 38 Q 12 24 32 22 Q 52 20 59 26', 5) + line(front, 5))
icons['star'] = shape(star_points(32, 34, 26, 11, 5), W, 5)
icons['power'] = line('M 20 18 A 19 19 0 1 0 44 18', 7) + line('M 32 8 V 30', 7)
icons['restart'] = arc_arrow(32, 33, 19, -50, 250)
icons['flag'] = line('M 16 8 V 58', 6) + shape('M 19 10 H 50 L 42 21 L 50 32 H 19 Z', W, 4)
icons['home'] = shape('M 10 30 L 32 11 L 54 30 V 54 H 39 V 40 H 25 V 54 H 10 Z', W, 5)
icons['back'] = line('M 28 14 L 10 32 L 28 50', 8) + line('M 12 32 H 54', 8)

# --- Settings -------------------------------------------------------------------
icons['volume'] = (shape('M 6 24 H 16 L 30 12 V 52 L 16 40 H 6 Z', W, 4)
                   + line('M 39 23 Q 45 32 39 41', 5) + line('M 46 15 Q 58 32 46 49', 5))
icons['music'] = (line('M 23 46 V 15 L 51 9 V 40', 6) + line('M 23 16 L 51 10', 9)
                  + '<ellipse cx="16" cy="47" rx="9" ry="7" fill="white" transform="rotate(-20 16 47)"/>'
                  + '<ellipse cx="44" cy="41" rx="9" ry="7" fill="white" transform="rotate(-20 44 41)"/>')
icons['sfx'] = ''.join(line(f'M {x} {32 - h / 2} V {32 + h / 2}', 6)
                       for x, h in zip(range(8, 60, 8), (8, 22, 36, 18, 42, 24, 10)))
icons['shake'] = (shape('M 17 12 H 47 V 52 H 17 Z', W, 8) + line('M 6 22 V 42 M 58 22 V 42', 5))
icons['fullscreen'] = line('M 8 22 V 8 H 22 M 42 8 H 56 V 22 M 56 42 V 56 H 42 M 22 56 H 8 V 42', 7)
icons['screen'] = ('<rect x="6" y="9" width="52" height="35" rx="5" stroke="white" stroke-width="6"/>'
                   + line('M 22 56 H 42 M 32 45 V 55', 6))
icons['vsync'] = arc_arrow(32, 32, 20, 200, 335, 6, 8) + arc_arrow(32, 32, 20, 20, 155, 6, 8)
icons['speed'] = (line('M 10 46 A 23 23 0 1 1 54 46', 6) + line('M 32 42 L 45 25', 6) + dot(32, 42, 6))
icons['textsize'] = line('M 6 14 H 34 M 20 14 V 52', 8) + line('M 38 30 H 58 M 48 30 V 52', 6)
icons['info'] = ring(32, 32, 25, 5) + dot(32, 20, 4.5) + line('M 32 30 V 46', 7)
icons['gamepad'] = masked('pad', shape('M 17 20 H 47 Q 60 20 60 38 Q 60 52 51 52 Q 45 52 41 44 H 23 Q 19 52 13 52 Q 4 52 4 38 Q 4 20 17 20 Z', W, 2),
                          line('M 18 30 V 42 M 12 36 H 24', 5, 'black') + dot(44, 31, 3.5, 'black') + dot(50, 38, 3.5, 'black'))
icons['access'] = (dot(32, 10, 6.5) + line('M 11 22 Q 32 27 53 22', 6) + line('M 32 24 V 39', 8)
                   + line('M 32 38 L 22 57 M 32 38 L 42 57', 7))

# --- Accessibility --------------------------------------------------------------
icons['eye'] = masked('eye', shape('M 4 32 Q 32 6 60 32 Q 32 58 4 32 Z', W, 4), dot(32, 32, 11, 'black')) + dot(32, 32, 5.5)
icons['contrast'] = ring(32, 32, 23, 6) + shape('M 32 12 A 20 20 0 0 1 32 52 Z', W, 0)
icons['bolt'] = shape('M 37 5 L 13 36 H 30 L 26 59 L 51 26 H 34 Z', W, 5)
icons['snail'] = (shape('M 7 52 Q 7 44 15 44 H 46 Q 55 44 57 52 Z', W, 4) + ring(36, 31, 14, 6)
                  + line('M 36 24 A 7 7 0 1 1 30 34', 5) + line('M 14 44 L 10 24 M 18 44 L 19 25', 4)
                  + dot(10, 22, 3.5) + dot(19, 23, 3.5))
icons['number'] = line('M 6 25 H 22 M 14 17 V 33', 6) + line('M 32 20 L 42 12 V 52 M 32 52 H 52', 7)
icons['crosshair'] = (ring(32, 32, 18, 5) + line('M 32 4 V 17 M 32 47 V 60 M 4 32 H 17 M 47 32 H 60', 5) + dot(32, 32, 4.5))

# --- Controls -------------------------------------------------------------------
up = line('M 32 55 V 11 M 15 28 L 32 11 L 49 28', 8)
for name, turn in (('up', 0), ('right', 90), ('down', 180), ('left', 270)):
    icons[f'arrow_{name}'] = f'<g transform="rotate({turn} 32 32)">{up}</g>'
icons['dash'] = line('M 31 14 L 49 32 L 31 50', 8) + line('M 8 20 H 22 M 4 32 H 24 M 8 44 H 22', 6)
icons['nova'] = dot(32, 32, 9) + ring(32, 32, 20, 5) + ''.join(
    line(f'M {32 + 26 * math.cos(math.radians(a)):.1f} {32 + 26 * math.sin(math.radians(a)):.1f} '
         f'L {32 + 30 * math.cos(math.radians(a)):.1f} {32 + 30 * math.sin(math.radians(a)):.1f}', 5)
    for a in range(0, 360, 45))
icons['upgrade'] = line('M 14 34 L 32 16 L 50 34', 8) + line('M 14 52 L 32 34 L 50 52', 8)
icons['pause'] = shape('M 16 10 H 26 V 54 H 16 Z', W, 5) + shape('M 38 10 H 48 V 54 H 38 Z', W, 5)

# --- My Planet and the leaderboard ------------------------------------------------
icons['person'] = dot(32, 19, 12) + shape('M 9 57 Q 9 36 32 36 Q 55 36 55 57 Z', W, 4)
icons['rocket'] = masked('rocket', shape('M 32 4 Q 47 16 47 34 L 43 45 H 21 L 17 34 Q 17 16 32 4 Z', W, 3)
                         + shape('M 17 33 L 7 47 L 20 44 Z M 47 33 L 57 47 L 44 44 Z', W, 3), dot(32, 24, 6, 'black')) \
                  + shape('M 26 49 L 32 60 L 38 49 Z', W, 3)
icons['pop'] = shape(star_points(32, 32, 28, 14, 8), W, 4)
icons['clock'] = ring(32, 32, 24, 6) + line('M 32 18 V 32 L 42 39', 6)
icons['bars'] = shape('M 9 38 H 19 V 54 H 9 Z', W, 4) + shape('M 27 26 H 37 V 54 H 27 Z', W, 4) + shape('M 45 12 H 55 V 54 H 45 Z', W, 4)
icons['pencil'] = shape('M 12 52 L 15 39 L 41 13 L 51 23 L 25 49 Z', W, 4) + line('M 36 18 L 46 28', 3, 'black')
icons['medal'] = (shape('M 15 4 H 27 L 34 24 H 22 Z M 49 4 H 37 L 30 24 H 42 Z', W, 3)
                  + masked('medal', dot(32, 41, 18), ring(32, 41, 11, 3, 'black')))
icons['chevron_left'] = line('M 40 12 L 20 32 L 40 52', 9)
icons['chevron_right'] = line('M 24 12 L 44 32 L 24 52', 9)
icons['chevron_down'] = line('M 12 24 L 32 44 L 52 24', 9)

for name, body in icons.items():
    svg(name, body)

# Small fixed-size versions: the dropdown arrow (tinted by the button) and the
# name field's pencil, which a text field cannot tint, so it is orange already.
svg('dropdown', icons['chevron_down'], px=24)
svg('pencil_field', icons['pencil'].replace('"white"', f'"{ORANGE}"'), px=28)

# --- Pieces in colour ---------------------------------------------------------------
# The slider knob is a little planet, the same round orange world the game is about.
svg('knob', f'<circle cx="20" cy="20" r="15" fill="{ORANGE}" stroke="{INK}" stroke-width="4"/>'
            f'<circle cx="14" cy="23" r="3.2" fill="#D9803A"/><circle cx="24" cy="14" r="2.4" fill="#D9803A"/>'
            f'<circle cx="25" cy="25" r="1.8" fill="#D9803A"/>'
            f'<path d="M 11 13 Q 14 9 19 8.5" stroke="{CREAM}" stroke-width="3"/>', 40)
svg('switch_on', f'<rect x="3" y="3" width="66" height="34" rx="17" fill="{ORANGE}" stroke="{CREAM}" stroke-width="3"/>'
                 f'<circle cx="52" cy="20" r="12" fill="{CREAM}"/>', view='0 0 72 40')
svg('switch_off', f'<rect x="3" y="3" width="66" height="34" rx="17" fill="#281B36" stroke="{MUTED}" stroke-width="3"/>'
                  f'<circle cx="20" cy="20" r="11" fill="{MUTED}"/>', view='0 0 72 40')

print(f'Wrote {len(icons) + 5} interface icons')
