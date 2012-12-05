@ Created by cbc at 12/04/2012 10:35:50 PM
	.global _start
	.text
_start:
	mov	r0,#0
	bl	Main
	mov	r0,#0x18
	mov	r1,#0
	swi	0x123456
@ 
@ Method Main
	.align	2
Main:
	stmfd	sp!,{r4-r12,lr}
	mov	fp,sp
	sub	sp,sp,#12
	ldr	r4,=_S.1
	str	r4,[fp,#-12]
	ldr	r4,=_S.2
	mov	r0,r4
	bl	cb.WriteString
	bl	cb.ReadInt
	str	r0,[fp,#-4]
	ldr	r4,[fp,#-4]
	mov	r5,#3
	cmp	r4,r5
	bne	_L.2
	b	_L.4
_L.2:
	mov	r4,#4
	str	r4,[fp,#-8]
	b	_L.3
_L.4:
	ldr	r4,=275
	str	r4,[fp,#-8]
_L.3:
	ldr	r4,=_S.3
	mov	r0,r4
	bl	cb.WriteString
	ldr	r4,[fp,#-8]
	mov	r0,r4
	bl	cb.WriteInt
	ldr	r4,[fp,#-12]
	mov	r0,r4
	bl	cb.WriteString
	b	_L.1
	.ltorg
_L.1:
	mov	sp,fp
	ldmfd	sp!,{r4-r12,pc}
	b	lr
	.data
_S.1:
	.asciz	"howdy there"
_S.2:
	.asciz	"\\\""
_S.3:
	.asciz	"Testing output!"
	.end
