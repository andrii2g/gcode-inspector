; fixture: bridge-critical-unsupported.gcode
; expected: one BR001 critical finding when max-bridge-span-critical is 20 mm
G92 X100 Y0
;TYPE:Perimeter
G1 X130 Y0 Z0.2 E1.0
;LAYER_CHANGE
G92 X0 Y0
;TYPE:Bridge infill
G1 X30 Y0 Z0.4 E2.0
