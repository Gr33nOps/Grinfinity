from pathlib import Path
out=Path('art/cosmic');ink='#321e3d';cream='#fff0ce';orange='#f5a451';berry='#ab4564'
def asset(name,body,view='0 0 256 256',size=256):
 (out/(name+'.svg')).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="{view}">{body}</svg>',encoding='utf-8')
asset('shot',f'<circle cx="32" cy="32" r="27" fill="{orange}" stroke="{ink}" stroke-width="5"/><circle cx="32" cy="32" r="20" fill="{cream}"/><path d="M17 30Q19 18 31 17" fill="none" stroke="white" stroke-width="5" stroke-linecap="round"/>', '0 0 64 64',64)
asset('shield_shell','<circle cx="128" cy="128" r="109" fill="#85c6bd" fill-opacity=".055" stroke="#321e3d" stroke-width="9"/><circle cx="128" cy="128" r="109" fill="none" stroke="#91d7d9" stroke-width="5"/><path d="M40 91A96 96 0 0 1 104 35M173 213A96 96 0 0 0 214 171" fill="none" stroke="#fff0ce" stroke-opacity=".85" stroke-width="4" stroke-linecap="round"/>')
for tier in range(1,4):
 for half,sweep in [('back',1),('front',0)]:
  color=[orange,'#f2c576','#d98798'][tier-1]
  body=f'<g transform="rotate(-14)"><path d="M-186 0A186 65 0 0 {sweep} 186 0" fill="none" stroke="{ink}" stroke-width="30"/><path d="M-186 0A186 65 0 0 {sweep} 186 0" fill="none" stroke="{color}" stroke-width="20"/><path d="M-186 0A186 65 0 0 {sweep} 186 0" fill="none" stroke="{cream}" stroke-width="5"/>'
  if half=='front':body+='<path d="M-136 42L-126 50M-72 59L-63 64M60 64L70 60M125 50L135 44" stroke="#321e3d" stroke-width="5" stroke-linecap="round"/>'
  asset(f'ring_{tier}_{half}',body+'</g>','-256 -256 512 512',512)
asset('boss_black_hole',f'<g transform="rotate(-18 128 128)"><ellipse cx="128" cy="134" rx="119" ry="44" fill="{berry}" stroke="{ink}" stroke-width="7"/><ellipse cx="128" cy="128" rx="108" ry="32" fill="{orange}"/><circle cx="128" cy="117" r="69" fill="#100f21" stroke="{ink}" stroke-width="9"/><path d="M67 107A63 63 0 0 1 188 96" fill="none" stroke="{cream}" stroke-width="9" stroke-linecap="round"/><path d="M16 131Q55 179 128 173Q209 171 240 132Q200 151 127 153Q53 155 16 131Z" fill="{orange}" stroke="{ink}" stroke-width="5"/><path d="M32 139Q129 181 224 139" fill="none" stroke="{cream}" stroke-width="8" stroke-linecap="round"/><path d="M66 193L92 199M184 49L202 57" stroke="{berry}" stroke-width="8" stroke-linecap="round"/></g>')
asset('armour_plate','<path d="M160 28L216 53L231 133L208 195L162 224L176 151L177 98Z" fill="#698bad" stroke="#321e3d" stroke-width="10" stroke-linejoin="round"/><path d="M192 60L204 101L201 158L186 194" fill="none" stroke="#d4e5da" stroke-width="9" stroke-linecap="round"/><circle cx="199" cy="125" r="10" fill="#f5a451" stroke="#321e3d" stroke-width="5"/>')
icons={
 'shield':'<circle cx="128" cy="128" r="79" fill="#85c6bd" fill-opacity=".18" stroke="#91d7d9" stroke-width="12"/><path d="M73 103A62 62 0 0 1 127 66M172 174L181 163" fill="none" stroke="#fff0ce" stroke-width="9"/>',
'rapid':'<path d="M139 37L78 133H117L100 218L182 107H141L164 37Z" fill="#f5cf79"/>',
'nova':'<path d="M128 26L147 78L203 53L179 109L231 128L179 148L203 204L147 179L128 231L108 179L53 203L78 148L26 128L78 109L53 53L108 78Z" fill="#d77a88"/><circle cx="128" cy="128" r="39" fill="#fff0ce"/>',
'dash':'<path d="M68 69L128 128L68 188M128 69L188 128L128 188" fill="none" stroke="#f5a451" stroke-width="21"/>',
'pierce':'<path d="M70 55V202M130 55V202" stroke="#85c6bd" stroke-width="17"/><path d="M35 128H218L181 91M218 128L181 165" fill="none" stroke="#f5a451" stroke-width="18"/>',
'spread':'<circle cx="161" cy="128" r="29" fill="#fff0ce"/><circle cx="129" cy="62" r="25" fill="#f5a451"/><circle cx="129" cy="194" r="25" fill="#f5a451"/><path d="M51 86L77 104M42 128H77M51 170L77 152" stroke="#fff0ce" stroke-width="12"/>'}
for name,body in icons.items():asset('icon_'+name,f'<circle cx="128" cy="128" r="116" fill="#4d2c49" stroke="{ink}" stroke-width="9"/><g stroke="{ink}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round">{body}</g>')
asset('muzzle',f'<path d="M128 40L149 90L208 76L176 124L220 168L155 162L132 216L112 164L52 183L81 133L42 91L103 98Z" fill="{cream}" stroke="{orange}" stroke-width="12"/>')
print('Rebuilt rings, shield shell, armour, eclipse, round shot and six illustrated power icons')

# Closed, smooth shapes keep the expression legible at the smallest player size.
mouth=f'<path d="M96 148C114 158 142 158 160 148C156 174 106 181 96 148Z" fill="{ink}"/><path d="M102 152Q128 163 154 152L150 159Q128 169 107 159Z" fill="{cream}"/>'
eyes=''
for x in (99,155):
 eyes+=f'<ellipse cx="{x}" cy="112" rx="18" ry="24" fill="{cream}" stroke="{ink}" stroke-width="5"/><ellipse cx="{x+5}" cy="116" rx="9" ry="13" fill="{ink}"/><ellipse cx="{x+7}" cy="109" rx="3" ry="4" fill="white"/>'
brows=f'<path d="M85 79Q98 71 110 78M144 78Q156 71 169 80" fill="none" stroke="{ink}" stroke-width="5" stroke-linecap="round"/>'
asset('face',eyes+brows+mouth)
closed=f'<path d="M83 113Q99 126 115 113M139 113Q155 126 171 113" fill="none" stroke="{ink}" stroke-width="6" stroke-linecap="round"/>'
asset('face_blink',closed+brows+mouth)
asset('face_happy',f'<path d="M83 116Q99 94 115 116M139 116Q155 94 171 116" fill="none" stroke="{ink}" stroke-width="6" stroke-linecap="round"/>'+mouth)
palettes=[('#F5A451','#CB704B'),('#A8D7BF','#558F8F'),('#D9AEDE','#A173A8'),('#F3C76B','#C58B4D'),('#93C6DF','#5E8EB6'),('#E78992','#B95A72'),('#B6C982','#879755'),('#B7ACE1','#8376B7'),('#E5AF87','#BC7E68'),('#AED4D7','#699FA6'),('#EAC8D7','#B287AD'),('#F4DB9B','#CFB76D')]
for i,(base,shade) in enumerate(palettes,1):
 body=f'<circle cx="128" cy="134" r="99" fill="{ink}"/><circle cx="125" cy="123" r="98" fill="{base}" stroke="{ink}" stroke-width="7"/>'
 body+=f'<path d="M48 180C114 218 194 183 218 115C226 171 188 217 133 220C95 223 65 207 48 180Z" fill="{shade}"/>'
 body+=f'<path d="M49 95C58 65 79 48 109 43" fill="none" stroke="{cream}" stroke-width="9" stroke-linecap="round" opacity=".65"/>'
 for x,y,r in [(55,133,11),(180,67,10),(181,184,13)]:
  body+=f'<circle cx="{x}" cy="{y}" r="{r}" fill="{shade}"/><path d="M{x-r+3} {y+3}Q{x} {y+r+4} {x+r-2} {y+3}" fill="none" stroke="{cream}" stroke-width="3" opacity=".3" stroke-linecap="round"/>'
 asset(f'planet_{i}',body)
print('Refined all twelve planet bodies and three clean facial expressions')
