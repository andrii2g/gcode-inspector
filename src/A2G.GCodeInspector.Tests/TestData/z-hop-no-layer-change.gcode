; fixture: z-hop-no-layer-change.gcode
; expected: travel z-hop does not create a new layer
G1 X0 Y0 Z0.2 E1.0
G0 Z0.4
G0 X10 Y0
G0 Z0.2
G1 X20 Y0 E2.0
