@ CbRuntime.s  -- the runtime support routines, coded in ARM assembler

	.global	cb.DivMod, cb.Malloc, cb.MemCopy, cb.StrLen
	.global	cb.ReadInt, cb.WriteInt, cb.WriteString


@ int cb.ReadInt() reads a decimal integer from the standard input
	.text
cb.ReadInt:
	stmfd	sp!,{r1-r5,lr}
	mov	r2, #0		@ build up result in r2
	mov	r3, #0		@ 0 ==> positive
	mov	r4, #10		@ multiplier
	@ skip over white space
cbri1:	bl	getc
	cmp	r0, #' '
	beq	cbri1
	cmp	r0, #'\t'
	beq	cbri1
	cmp	r0, #'\r'
	beq	cbri1
	cmp	r0, #'\n'
	beq	cbri1
	@ check for a plus or minus sign
	cmp	r0, #'-'
	moveq	r3, #1
	beq	cbri2
	cmp	r0, #'+'
	bne	cbri3
cbri2:	bl	getc		@ get new char
cbri3:	cmp	r0, #'9'
	bgt	cbri9		@ not a digit
	subs	r0, r0, #'0'
	blt	cbri9
	mla	r5, r2, r4, r0
	mov	r2, r5
	b	cbri2
cbri9:	cmp	r3, #0
	rsbne	r2, r2, #0
	mov	r0, r2
	ldmfd	sp!,{r1-r5,pc}
	.ltorg

@ read one byte from input
getc:	adr	r1, getcp
	mov	r0, #0x06
	swi	0x123456
	adr	r0, getcb
	ldrsb	r0, [r0]
	mov	pc, lr
getcp:	.word	0		@ file handle 0 = standard input
	.word	getcb		@ where to put the input
	.word	1		@ number bytes to read
getcb:	.word	0
	.ltorg


@ void WriteInt(n) prints the integer value n on the standard output
	.text
cb.WriteInt:			@Integer to be printed is in r0	
	stmfd	sp!,{r0-r6,lr}
	mov	r1, r0		@ R1: value to print
	ldr	r2, =space	@ R2: output space
	cmp	r1, #0		@ check sign of value (+/-)
	blt	cbwi1
	mov	r0, #'+'	@ prepend with '+'
	b	cbwi2
cbwi1:		
	mov	r0, #'-'		@ prepend with '-'
cbwi2:	
	strb	r0, [r2], #1
	ldr	r3, =tens	@ R3: multiple pointer
	mov	r6, #0		@ R6: write zero digits? (no, for leading zeros)
cbwi3:		
	ldr	r4, [r3], #4	@ R4: current multiple
	cmp	r4, #1		@ stop when multiple < 1
	blt	cbwi99
	bne	cbwi4
	mov	r6, #1		@ write zero digit for last multiple
cbwi4:	mov	r5, #0		@ R5: multiple count
cbwi5:	mov	r0, r1		@ R0: tmp R1
	cmp	r1, #0
	blt	cbwi6
	subs	r0, r0, r4	@ subtract multiple from pos value
	bmi	cbwi8
	b	cbwi7
cbwi6:	add	r0, r0, r4	@ add multiple to neg value
	cmp	r0, #0
	bgt	cbwi8
cbwi7:	mov	r1, r0		@ update value
	add	r5, r5, #1	@ update count
	b	cbwi5
cbwi8:	cmp	r5, #0		@ if digit is '0' and ...
	bne	cbwi9
	cmp	r6, #1		@ if not last multiple
	bne	cbwi3		@ skip leading '0'
cbwi9:	mov	r6, #1		@ write '0' from now on
	ldr	r0, =digits
	ldrb	r0, [r0,r5]	@ write digit character at count offset
	strb	r0, [r2],#1	@ save digit output
	b	cbwi3
cbwi99:	ldr	r1, =WriteNumber
	ldr	r3, =space
	sub	r2, r2, r3
	str	r2, [r1,#8]     @ store number of characters
	mov	r0, #0x05
	swi	0x123456	
	ldmfd	sp!,{r0-r6,pc}	@restore state and return
	.ltorg

	.data
	.align	2
WriteNumber:
	.word	1		@ file handle = stdout
	.word	space		@ addr of buffer
	.word	0		@ number of characters to write
digits:	.ascii	"0123456789ABCDEF"	@ Note: NOT null-terminated
	.align	2
tens:	.word	1000000000,100000000,10000000,1000000,100000,10000,1000,100,10,1,0
space:
	.skip	11	@ Space for 10 characters and a sign (+/-)


@ void WriteStr(s) prints the string s on the standard output
	.text
cb.WriteString:
	stmfd	sp!,{r0-r2,lr}
        mov     r2, r0
cbws1:	ldrb	r1, [r2], #1	@ search for final null byte
	cmp	r1, #0
	bne	cbws1
	sub	r2, r2, r0
	subs	r2, r2, #1	@ r1 = length
	ble	cbws9		@ return if length == 0
	ldr	r1, =WriteString
	str	r0, [r1,#4]	@ store string address
	str	r2, [r1,#8]	@ store length
	mov	r0, #0x05
	swi	0x123456
cbws9:	ldmfd	sp!,{r0-r2,pc}
	.ltorg

	.data
	.align	2
WriteString:
	.word	1		@ file handle = stdout
	.word	0		@ addr of string
	.word	0		@ number of characters to write


@ DivMod(a,b) returns a/b and a%b where a,b are int values
@ We use the floating-point unit for this calculation
	.text
cb.DivMod:
	stmfd	sp!,{r2-r3,lr}
	fmsr	s0, r0
	fmsr	s1, r1
	fdivs	s2, s0, s1
	ftosizs	s2, s2		@ truncate to integer
	fmrs	r2, s2		@ r2 = a/b
	mul	r3, r2, r1	@ r3 = (a/b)*b
	sub	r1, r0, r3	@ r1 = a%b
	mov	r0, r2
	ldmfd	sp!,{r2-r3,pc}
	.ltorg

@ Malloc(x) returns address of a block of heap memory of size x
@ rounded up to a multiple of 4 bytes
	.text
cb.Malloc:
	stmfd	sp!,{r1-r4,lr}
	mov	r2, r0
	ldr	r3, =HeapStart
	ldr	r4, [r3]
	cmp	r4, #0
	bne	cbm1		@ jump if already initialized
	mov	r1, r3
	mov	r0, #0x16
	swi	0x123456
	ldr	r4, [r3]
cbm1:	add	r2, r2, #3
	mvn	r1, #3
	and	r2, r2, r1	@ force request size to multiple of 4
	mov	r0, r4		@ set the result
	add	r4, r4, r2	@ advance heap pointer
	str	r4, [r3]	@ store back in memory
	ldmfd	sp!,{r1-r4,pc}
	.ltorg

	.data
	.align	2
HeapStart:
	.word	0		@ heap area start
	.word	0		@ heap area end
	.word	0		@ stack area start
	.word	0		@ stack area end


@ MemCopy(dst, src, n) copies n bytes from address src to address dst
	.text
cbmc1:	ldrb	r3, [r1],#1
	strb	r3, [r0],#1
cb.MemCopy:
	subs	r2, r2, #1
	bge	cbmc1
	mov	pc, lr

@ StrLen(s) returns the length of string s
	.text
cb.StrLen:
	mov	r1, #0
sl1:	ldrsb	r2, [r0], #1
	cmp	r2, #0
	addne	r1, r1, #1
	bne	sl1
	mov	r0, r1
	mov	pc, lr
	.end
