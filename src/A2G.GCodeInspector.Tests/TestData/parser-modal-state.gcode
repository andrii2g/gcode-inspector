; fixture: parser-modal-state.gcode
; expected: omitted coordinates inherit from previous machine state
G90
M82
G1 X10 Y10 Z0.2 E1.0 F1200
G1 X20 E2.0
G1 Y30 E3.0
