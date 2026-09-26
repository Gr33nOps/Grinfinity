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
# emotion, from calmness on the first to confidence on the last; its body is plain,
# in colours that suit the emotion, and the face alone does the rest.
planets = [
    ('#B3D8EE', '#7AA3C9'),  # 1 Stillwater, calmness: pale sky blue
    ('#A8DCC4', '#6FA79A'),  # 2 Easewind, relief: soft mint
    ('#F2A97E', '#C77A58'),  # 3 Hearthglow, contentment: warm peach
    ('#FFDA66', '#DDA843'),  # 4 Gracelight, gratitude: warm sunny yellow
    ('#B08BDB', '#7D5EAE'),  # 5 Laurelcrown, pride: royal purple
    ('#F47563', '#C24C47'),  # 6 Sparkrush, excitement: hot coral red
    ('#F28AB0', '#C25884'),  # 7 Heartsong, love: rose pink
    ('#5E97E0', '#3D66A8'),  # 8 Boldcrest, confidence: strong blue
]
for i, (base, shade) in enumerate(planets, 1):
    body = f'<circle cx="128" cy="134" r="99" fill="{ink}"/><circle cx="125" cy="123" r="98" fill="{base}" stroke="{ink}" stroke-width="7"/>'
    body += f'<path d="M48 180C114 218 194 183 218 115C226 171 188 217 133 220C95 223 65 207 48 180Z" fill="{shade}"/>'
    body += f'<path d="M49 95C58 65 79 48 109 43" fill="none" stroke="{cream}" stroke-width="9" stroke-linecap="round" opacity=".65"/>'
    for x, y, r in [(55, 133, 11), (180, 67, 10), (181, 184, 13)]:
        body += (f'<circle cx="{x}" cy="{y}" r="{r}" fill="{shade}"/><path d="M{x-r+3} {y+3}Q{x} {y+r+4} {x+r-2} {y+3}" fill="none"'
                 f' stroke="{cream}" stroke-width="3" opacity=".3" stroke-linecap="round"/>')
    asset(f'planet_{i}', body)
print('Refined all eight planet bodies')
