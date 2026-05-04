; fixture: no-type-heuristic-bridge.gcode
; expected: heuristic bridge candidate when support coverage is below 50 percent and ;TYPE is missing
G92 X15 Y-20
G1 X15 Y20 Z0.2 E1.0
;LAYER_CHANGE
G92 X0 Y0
G1 X30 Y0 Z0.4 E2.0
