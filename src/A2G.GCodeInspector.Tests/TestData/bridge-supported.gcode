; fixture: bridge-supported.gcode
; expected: no BR001 finding because the bridge is fully supported
G92 X0 Y0
;TYPE:Perimeter
G1 X30 Y0 Z0.2 E1.0
;LAYER_CHANGE
G92 X0 Y0
;TYPE:Bridge infill
G1 X30 Y0 Z0.4 E2.0
