@ test the CbRuntime.s module

	.text
Main:
	ldr	r0, =HelloMsg
	bl	cb.WriteString

	ldr	r0, =999
	bl	cb.WriteInt
	ldr	r0, =WI1
	bl	cb.WriteString

	ldr	r0, =-999
	bl	cb.WriteInt
	ldr	r0, =WI2
	bl	cb.WriteString

	ldr	r0, =1000
	ldr	r1, =7
	bl	cb.DivMod
	mov	r4, r1
	bl	cb.WriteInt
	ldr	r0, =DM1
	bl	cb.WriteString
	mov	r0, r4
	bl	cb.WriteInt
	ldr	r0, =DM2
	bl	cb.WriteString

	ldr	r0, =SL1
	bl	cb.WriteString
	ldr	r0, =ABC
	bl	cb.StrLen
	bl	cb.WriteInt
	ldr	r0, =SL2
	bl	cb.WriteString

	mov	r0, #100
	bl	cb.Malloc
	bl	cb.WriteInt
	ldr	r0, =MA1
	bl	cb.WriteString
	mov	r0, #100
	bl	cb.Malloc
	bl	cb.WriteInt
	ldr	r0, =MA2
	bl	cb.WriteString

	ldr	r0, =RD1
	bl	cb.WriteString
readloop:
	bl	cb.ReadInt
	mov	r4, r0
	bl	cb.WriteInt
	ldr	r0, =RD2
	bl	cb.WriteString
	cmp	r4, #0
	bne	readloop
	
	mov	r1, #0
	mov	r0, #0x18
	swi	0x123456	@ program exit
	

	.data
HelloMsg:
	.asciz	"Hello, this is a test run\n\n"
WI1:	.asciz	"  <-- was that 999 output?\n\n"
WI2:	.asciz	"  <-- was that -999 output?\n\n"
DM1:	.asciz	"    "
DM2:	.asciz	"  <-- should have been 1000/7 and 1000%7 displayed\n\n"
SL1:	.asciz	"Length of string \"abc\" = "
SL2:	.asciz  "\n\n"
ABC:	.asciz	"abc"
MA1:	.asciz	"  <-- malloc(100)\n\n"
MA2:	.asciz	"  <-- malloc(100) again\n\n"
RD1:	.asciz	"Enter a sequence of integers, last integer must be 0\n\n"
RD2:	.asciz	"  <-- your integer was this\n"
	.end

	