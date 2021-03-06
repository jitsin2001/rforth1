needs lib/tty-rs232.fs
needs 2006/ds1804.fs

LATA 1 bit S0
LATC 0 bit S1
LATC 1 bit S2
LATA 0 bit S3
LATA 5 bit /OE
PORTC 2 bit IN

: init-tcs ( -- )
  TRISA 1 bit-clr TRISA 0 bit-clr TRISC 1 bit-clr TRISC 0 bit-clr
  TRISA 5 bit-clr TRISC 2 bit-set ;

cvariable color-mode
color-mode 0 bit DOCLEAR
color-mode 1 bit DORED
color-mode 2 bit DOGREEN
color-mode 3 bit DOBLUE

: select-tcs230 ( -- ) /OE bit-clr ;
: deselect-tcs230 ( -- ) /OE bit-set ;
: clear ( -- ) S2 bit-set S3 bit-clr ;
: blue ( -- ) S2 bit-clr S3 bit-set ;
: green ( -- ) S2 bit-set S3 bit-set ;
: red ( -- ) S2 bit-clr S3 bit-clr ;
: mode-down ( -- ) S0 bit-clr S1 bit-clr ;
: mode-2% ( -- ) S0 bit-clr S1 bit-set ;
: mode-20% ( -- ) S0 bit-set S1 bit-clr ;
: mode-100% ( -- ) S0 bit-set S1 bit-set ;

: wait-for-down ( -- ) begin IN bit-clr? until ; inline
: wait-for-up ( -- ) begin IN bit-set? until ; inline

variable overflows

: ovf ( -- ) TMR1IF bit-set? if 1 overflows +! TMR1IF bit-clr then ; inline
: wait-for-ccp ( -- ) begin ovf CCP1IF bit-set? until CCP1IF bit-clr ; inline
: reset-ccp ( -- ) CCP1IF bit-clr 0 TMR1L ! TMR1IF bit-clr 0 overflows ! ;

: count-pulses ( n -- timel timeh )
  \ Turn the module on and Wait for the first pulse
  select-tcs230 wait-for-up reset-ccp wait-for-ccp CCPR1L @ swap
  \ Wait for the required number of cycles
  cfor wait-for-ccp cnext
  \ Compute the time difference and shut down module
  deselect-tcs230 CCPR1L @ swap - overflows @ ;

: init-vars ( -- ) 15 color-mode c! ;
: init-ccp ( -- ) $05 CCP1CON c! $81 T1CON c! $00 T3CON c! ;

: help ( -- )
  ." 0: off  1: 100% [default] 2: 20%  3: 2%" cr
  ." N: clear  R: red  G: green  B: blue  A: all [default]" cr
  ." W: 1 pulse  X: 10 pulses  C: 100 pulses  L: ratios" cr
  ." >: inc. led  <: dec. led  !: store led" cr
  ." Z: zero led M: max led  H: half led" cr
  ." S: select device  D: deselect device  @: reset" cr
;

variable clear-value

: .ratio ( m n -- ) >r clear-value @ 100 r> */ 100 * swap / .10 [char] % emit cr ;
: 16bits-measure ( -- x ) 1 count-pulses drop ;
: ratios ( n -- )
  clear 16bits-measure clear-value !
  ." Red:   " red 21 16bits-measure .ratio
  ." Green: " green 28 16bits-measure .ratio
  ." Blue:  " blue 33 16bits-measure .ratio
;

: prompt ( -- ) .s cr ." ?>" ;

: .measure ( n -- ) count-pulses . . cr ;

: do-measure ( n -- )
  DOCLEAR bit-set? if ." CLEAR: " dup clear .measure then
  DORED bit-set? if ." RED:   " dup red .measure then
  DOGREEN bit-set? if ." GREEN: " dup green .measure then
  DOBLUE bit-set? if ." BLUE:  " dup blue .measure then
  drop
;

: handle-key ( c -- )
  [char] a >= if 32 - then
  >w switchw
  [char] 0 casew mode-down exit
  [char] 1 casew mode-100% exit
  [char] 2 casew mode-20% exit
  [char] 3 casew mode-2% exit
  [char] N casew 1 color-mode c! exit
  [char] R casew 2 color-mode c! exit
  [char] G casew 4 color-mode c! exit
  [char] B casew 8 color-mode c! exit
  [char] A casew 15 color-mode c! exit
  [char] W casew 1 do-measure exit
  [char] X casew 10 do-measure exit
  [char] C casew 100 do-measure exit
  \ For the leds, direction is inverted
  [char] < casew up exit
  [char] > casew down exit
  [char] ! casew save-to-eeprom exit
  [char] Z casew max exit
  [char] M casew zero exit
  [char] H casew medium exit
  [char] S casew select-tcs230 exit
  [char] D casew deselect-tcs230 exit
  [char] L casew ratios exit
  [char] @ casew reset
  endswitchw
  help
;

: mainloop ( -- ) begin prompt key dup emit cr handle-key again ;

: main ( -- ) init-tcs mode-100% init-vars init-ccp init-ds1804 help mainloop ;
