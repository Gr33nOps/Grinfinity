from pathlib import Path
out=Path('art/cosmic');ink='#321e3d';cream='#fff0ce';orange='#f5a451';berry='#ab4564'
def asset(name,body,view='0 0 256 256',size=256):
 (out/(name+'.svg')).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="{view}">{body}</svg>',encoding='utf-8')
asset('shot',f'<circle cx="32" cy="32" r="27" fill="{orange}" stroke="{ink}" stroke-width="5"/><circle cx="32" cy="32" r="20" fill="{cream}"/><path d="M17 30Q19 18 31 17" fill="none" stroke="white" stroke-width="5" stroke-linecap="round"/>', '0 0 64 64',64)
for tier in range(1,4):
 for half,sweep in [('back',1),('front',0)]:
  color=[orange,'#f2c576','#d98798'][tier-1]
  body=f'<g transform="rotate(-14)"><path d="M-186 0A186 65 0 0 {sweep} 186 0" fill="none" stroke="{ink}" stroke-width="30"/><path d="M-186 0A186 65 0 0 {sweep} 186 0" fill="none" stroke="{color}" stroke-width="20"/><path d="M-186 0A186 65 0 0 {sweep} 186 0" fill="none" stroke="{cream}" stroke-width="5"/>'
  if half=='front':body+='<path d="M-136 42L-126 50M-72 59L-63 64M60 64L70 60M125 50L135 44" stroke="#321e3d" stroke-width="5" stroke-linecap="round"/>'
  asset(f'ring_{tier}_{half}',body+'</g>','-256 -256 512 512',512)
asset('armour_plate','<path d="M160 28L216 53L231 133L208 195L162 224L176 151L177 98Z" fill="#698bad" stroke="#321e3d" stroke-width="10" stroke-linejoin="round"/><path d="M192 60L204 101L201 158L186 194" fill="none" stroke="#d4e5da" stroke-width="9" stroke-linecap="round"/><circle cx="199" cy="125" r="10" fill="#f5a451" stroke="#321e3d" stroke-width="5"/>')
icons={
'rapid':'<path d="M139 37L78 133H117L100 218L182 107H141L164 37Z" fill="#f5cf79"/>',
'nova':'<path d="M128 26L147 78L203 53L179 109L231 128L179 148L203 204L147 179L128 231L108 179L53 203L78 148L26 128L78 109L53 53L108 78Z" fill="#d77a88"/><circle cx="128" cy="128" r="39" fill="#fff0ce"/>',
'dash':'<path d="M68 69L128 128L68 188M128 69L188 128L128 188" fill="none" stroke="#f5a451" stroke-width="21"/>',
'pierce':'<path d="M70 55V202M130 55V202" stroke="#85c6bd" stroke-width="17"/><path d="M35 128H218L181 91M218 128L181 165" fill="none" stroke="#f5a451" stroke-width="18"/>',
'spread':'<circle cx="161" cy="128" r="29" fill="#fff0ce"/><circle cx="129" cy="62" r="25" fill="#f5a451"/><circle cx="129" cy="194" r="25" fill="#f5a451"/><path d="M51 86L77 104M42 128H77M51 170L77 152" stroke="#fff0ce" stroke-width="12"/>',
'firerate':'<path d="M36 92H96M28 128H58M36 164H96" fill="none" stroke="#fff0ce" stroke-width="11"/><circle cx="84" cy="128" r="17" fill="#f5a451"/><circle cx="131" cy="128" r="23" fill="#f5cf79"/><circle cx="188" cy="128" r="31" fill="#fff0ce"/><path d="M178 113A18 18 0 0 1 196 108" fill="none" stroke="#321e3d" stroke-width="6"/>',
'core':'<circle cx="128" cy="130" r="74" fill="#f5a451" fill-opacity=".22" stroke="none"/><path d="M128 52L178 104L128 208L78 104Z" fill="#f5a451"/><path d="M78 104H178M128 52L108 104L128 208M128 52L148 104L128 208" fill="none" stroke="#321e3d" stroke-width="5"/><path d="M96 96L112 72" fill="none" stroke="#fff0ce" stroke-width="9"/><path d="M200 70L206 82L218 88L206 94L200 106L194 94L182 88L194 82Z" fill="#fff0ce" stroke-width="4"/><path d="M52 150L56 158L64 162L56 166L52 174L48 166L40 162L48 158Z" fill="#fff0ce" stroke-width="4"/>',
'moon':'<circle cx="128" cy="128" r="66" fill="#e8e0f0"/><circle cx="104" cy="108" r="15" fill="#c3b6d4" stroke-width="5"/><circle cx="152" cy="150" r="11" fill="#c3b6d4" stroke-width="5"/><circle cx="142" cy="98" r="7" fill="#c3b6d4" stroke-width="4"/><path d="M82 116A48 48 0 0 1 112 76" fill="none" stroke="#fff" stroke-width="9"/><path d="M40 170Q128 230 216 170" fill="none" stroke="#91d7d9" stroke-width="8" stroke-dasharray="2 18"/>',
'powercell':'<rect x="66" y="58" width="124" height="158" rx="22" fill="#b58cff"/><rect x="102" y="38" width="52" height="24" rx="8" fill="#fff0ce"/><path d="M138 84L100 142H126L114 190L158 124H132L144 84Z" fill="#fff0ce"/><path d="M84 80V112" fill="none" stroke="#e3d2ff" stroke-width="9"/>'}
for name,body in icons.items():asset('icon_'+name,f'<circle cx="128" cy="128" r="116" fill="#4d2c49" stroke="{ink}" stroke-width="9"/><g stroke="{ink}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round">{body}</g>')
asset('moon',f'<circle cx="128" cy="132" r="100" fill="{ink}"/><circle cx="126" cy="124" r="98" fill="#e8e0f0" stroke="{ink}" stroke-width="8"/><path d="M40 160C100 214 190 196 222 128C224 184 184 220 128 222C84 222 52 196 40 160Z" fill="#c9bddb"/><circle cx="92" cy="104" r="22" fill="#c9bddb" stroke="{ink}" stroke-width="6"/><circle cx="160" cy="150" r="16" fill="#c9bddb" stroke="{ink}" stroke-width="6"/><circle cx="156" cy="86" r="10" fill="#c9bddb" stroke="{ink}" stroke-width="5"/><path d="M52 110C58 76 82 52 112 44" fill="none" stroke="#fff" stroke-width="11" stroke-linecap="round" opacity=".7"/>')
asset('muzzle',f'<path d="M128 40L149 90L208 76L176 124L220 168L155 162L132 216L112 164L52 183L81 133L42 91L103 98Z" fill="{cream}" stroke="{orange}" stroke-width="12"/>')
print('Rebuilt rings, the moon, armour, round shot and the power icons')

# The planet's faces live in tools/make_moods.py. Each planet wears one positive
# emotion, from relief on the first to love on the last, and its body is painted
# to match: its own colours and a motif across its surface, clipped inside the
# disc so the face and the blaster always sit in the same place.
import math


def sparkle(x, y, r, fill=cream):
    return (f'<path d="M{x} {y-r}Q{x+r*.18} {y-r*.18} {x+r} {y}Q{x+r*.18} {y+r*.18} {x} {y+r}'
            f'Q{x-r*.18} {y+r*.18} {x-r} {y}Q{x-r*.18} {y-r*.18} {x} {y-r}Z" fill="{fill}" stroke="{ink}" stroke-width="3"/>')


def heart(x, y, s, fill):
    return (f'<path d="M{x} {y+s*.9}C{x-s*1.3} {y+s*.05} {x-s*.8} {y-s*.95} {x} {y-s*.3}'
            f'C{x+s*.8} {y-s*.95} {x+s*1.3} {y+s*.05} {x} {y+s*.9}Z" fill="{fill}" stroke="{ink}" stroke-width="3"/>')


def flower(x, y, r, petal, middle):
    f = ''.join(f'<circle cx="{x+math.cos(a)*r:.1f}" cy="{y+math.sin(a)*r:.1f}" r="{r*.72:.1f}" fill="{petal}" stroke="{ink}" stroke-width="3"/>'
                for a in [i * math.tau / 5 - math.pi / 2 for i in range(5)])
    return f + f'<circle cx="{x}" cy="{y}" r="{r*.6:.1f}" fill="{middle}" stroke="{ink}" stroke-width="3"/>'


def cloud(x, y, s, fill):
    return (f'<path d="M{x-s*1.4} {y+s*.5}Q{x-s*1.5} {y-s*.2} {x-s*.8} {y-s*.2}Q{x-s*.6} {y-s} {x+s*.1} {y-s*.7}'
            f'Q{x+s*.8} {y-s*1.1} {x+s} {y-s*.2}Q{x+s*1.6} {y-s*.1} {x+s*1.4} {y+s*.5}Z" fill="{fill}" stroke="none"/>')


def laurel(fill, vein):
    """Gold laurel leaves wrapped round the lower rim, from both sides up."""
    leaves = ''
    for side in (-1, 1):
        for k in range(6):
            a = math.pi / 2 + side * (0.25 + k * 0.23)
            for off, lean in ((6, 0.55), (-6, -0.55)):
                cx, cy = 125 + math.cos(a) * (100 + off), 123 + math.sin(a) * (100 + off)
                angle = math.degrees(a + math.pi / 2 * side * -1 + lean * side)
                leaves += (f'<ellipse cx="{cx:.1f}" cy="{cy:.1f}" rx="11" ry="5" transform="rotate({angle:.1f} {cx:.1f} {cy:.1f})"'
                           f' fill="{fill}" stroke="{ink}" stroke-width="3"/>')
    leaves += f'<circle cx="125" cy="224" r="7" fill="{vein}" stroke="{ink}" stroke-width="3"/>'
    return leaves


# (base, shade, motif inside the disc, extra drawn over the rim)
planets = {
    # 1 Easewind, relief: soft mint, a breeze curling across it.
    1: ('#A8DCC4', '#6FA79A',
        f'<path d="M38 150Q78 132 104 150Q124 164 150 150" fill="none" stroke="{cream}" stroke-width="7" stroke-linecap="round" opacity=".55"/>'
        f'<path d="M150 150Q164 140 158 130Q150 124 144 132" fill="none" stroke="{cream}" stroke-width="7" stroke-linecap="round" opacity=".55"/>'
        f'<path d="M150 58Q186 50 210 70" fill="none" stroke="{cream}" stroke-width="6" stroke-linecap="round" opacity=".45"/>', ''),
    # 2 Stillwater, calmness: pale sky blue, still ripples spreading slowly.
    2: ('#A9CFE8', '#6F97BF',
        ''.join(f'<ellipse cx="150" cy="190" rx="{r}" ry="{r*.35:.1f}" fill="none" stroke="{cream}" stroke-width="4" opacity="{o}"/>'
                for r, o in ((20, .6), (40, .45), (62, .3))), ''),
    # 3 Hearthglow, contentment: warm peach with snug, soft bands.
    3: ('#F4B58A', '#C9805F',
        '<path d="M20 96Q125 118 232 96L232 114Q125 136 20 114Z" fill="#E59A73" opacity=".7"/>'
        '<path d="M20 160Q125 182 232 160L232 174Q125 196 20 174Z" fill="#E59A73" opacity=".7"/>', ''),
    # 4 Hushmere, peacefulness: lavender, with soft clouds drifting by.
    4: ('#C8B6E6', '#9280BD',
        cloud(60, 176, 16, '#EDE3FA') + cloud(190, 70, 13, '#EDE3FA') + cloud(186, 196, 10, '#EDE3FA'), ''),
    # 5 Anchorlight, trust: deep teal with a sturdy band round its middle.
    5: ('#82C4BD', '#4E8E93',
        '<path d="M20 140Q125 162 232 140L232 162Q125 184 20 162Z" fill="#5FA5A2"/>'
        + ''.join(f'<circle cx="{x}" cy="{151 + 11 * (1 - ((x - 125) / 107) ** 2):.1f}" r="4" fill="{cream}"/>' for x in (46, 86, 166, 206)), ''),
    # 6 Gracebloom, gratitude: rose pink, dotted with little blossoms.
    6: ('#F2A7B8', '#C06F87',
        flower(58, 150, 9, cream, '#F5C451') + flower(190, 72, 8, cream, '#F5C451') + flower(184, 182, 7, cream, '#F5C451')
        + flower(62, 88, 6, '#FFE0E8', '#F5C451'), ''),
    # 7 Wishfall, hope: dawn gold fading to pink, a shooting star and a few wishes.
    7: ('#F7D08A', '#DE9A7C',
        '<path d="M26 150Q125 196 226 150L232 256L20 256Z" fill="#EDAE92" opacity=".8"/>'
        f'<path d="M150 64L204 44" fill="none" stroke="{cream}" stroke-width="5" stroke-linecap="round" opacity=".6"/>'
        + sparkle(148, 66, 9) + sparkle(56, 92, 7) + sparkle(196, 168, 8), ''),
    # 8 Boldcrest, confidence: strong blue with a bold sash across it.
    8: ('#6FA8E0', '#4471AE',
        f'<path d="M36 196L196 36L226 62L66 222Z" fill="#9CC6EE"/><path d="M50 186L186 50" fill="none" stroke="{cream}" stroke-width="4" opacity=".6"/>', ''),
    # 9 Laurelcrown, pride: royal purple, crowned with a golden laurel wreath.
    9: ('#B08BDB', '#7D5EAE',
        '<circle cx="182" cy="74" r="8" fill="#9B77C9"/><circle cx="60" cy="98" r="6" fill="#9B77C9"/>',
        laurel('#F5C451', '#E0677F')),
    # 10 Sunburst, joy: sunny yellow, sprinkled with bright confetti.
    10: ('#FFD86B', '#E3A443',
         ''.join(f'<rect x="{x}" y="{y}" width="11" height="6" rx="3" fill="{c}" stroke="{ink}" stroke-width="2.5" transform="rotate({r} {x+5} {y+3})"/>'
                 for x, y, c, r in ((52, 90, '#F28AA0', 30), (186, 70, '#8FD3C4', -25), (60, 166, '#9CC6EE', -40),
                                    (190, 178, '#F28AA0', 20), (150, 56, '#B08BDB', 60), (40, 128, '#8FD3C4', 70))), ''),
    # 11 Sparkrush, excitement: bright coral crackling with lightning.
    11: ('#F7967A', '#CF5F52',
         f'<path d="M34 110L60 96L54 124L82 110" fill="none" stroke="#FFE39A" stroke-width="6" stroke-linejoin="round" stroke-linecap="round"/>'
         f'<path d="M176 176L200 160L196 186L220 170" fill="none" stroke="#FFE39A" stroke-width="6" stroke-linejoin="round" stroke-linecap="round"/>'
         + sparkle(190, 70, 9, '#FFE39A') + sparkle(56, 176, 7, '#FFE39A'), ''),
    # 12 Heartsong, love: warm rose, scattered with little hearts.
    12: ('#F28599', '#C24F6C',
         heart(58, 150, 10, '#FFD3DC') + heart(190, 74, 9, '#FFD3DC') + heart(186, 182, 8, '#FFD3DC') + heart(62, 90, 6, cream), ''),
}
for i, (base, shade, motif, over) in planets.items():
    clip = '<defs><clipPath id="d"><circle cx="125" cy="123" r="94"/></clipPath></defs>'
    body = clip + f'<circle cx="128" cy="134" r="99" fill="{ink}"/><circle cx="125" cy="123" r="98" fill="{base}"/>'
    body += f'<g clip-path="url(#d)">{motif}'
    body += f'<path d="M48 180C114 218 194 183 218 115C226 171 188 217 133 220C95 223 65 207 48 180Z" fill="{shade}" opacity=".85"/></g>'
    body += f'<path d="M49 95C58 65 79 48 109 43" fill="none" stroke="{cream}" stroke-width="9" stroke-linecap="round" opacity=".65"/>'
    body += f'<circle cx="125" cy="123" r="98" fill="none" stroke="{ink}" stroke-width="7"/>' + over
    asset(f'planet_{i}', body)
print('Refined all twelve planet bodies')
