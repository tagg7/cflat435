@ Note: if this is used as a separate file, then the label
@ Main needs to have been declared as global in the file
@ where it is declared.

	.global	_start
	.text
_start:	mov	r0, #0		@ args parameter = null
	bl	Main		@ call the Main method
	mov	r0, #0x18
	mov	r1, #0
	swi	0x123456	@ stop the program
	.end

