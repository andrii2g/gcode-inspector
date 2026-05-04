; fixture: bridge-warning-unsupported.gcode
; expected: one BR001 warning because unsupported span is 12 mm with sample spacing 12
G92 X15 Y-20
;TYPE:Perimeter
G1 X15 Y20 Z0.2 E1.0
G92 X0 Y15
;TYPE:Perimeter
G1 X30 Y-15 Z0.2 E2.0
;LAYER_CHANGE
G92 X0 Y0
;TYPE:Bridge infill
G1 X30 Y0 Z0.4 E3.0
