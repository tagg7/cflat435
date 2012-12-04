// GenCode.cs

using System;
using System.Collections.Generic;
using System.IO;

using AST = FrontEnd.AST;
using AST_leaf = FrontEnd.AST_leaf;
using NodeType = FrontEnd.NodeType;


namespace BackEnd
{
    public class GenCode
    {
	    const int fp = 12;  // frame pointer register
	    const int sp = 13;  // stack pointer register
	    public ARMAssemblerCode Asm { get; private set; }
	    private int nextLabelNum;
	    private int nextStringNum;
	    private string returnLabel;
	    private IList<Tuple<string,string>> stringConsts;

	    // Constructor
	    public GenCode()
        {
		    Asm = new ARMAssemblerCode();
		    regAvailable = new bool[12];
		    stringConsts = new List<Tuple<string,string>>();
		    nextLabelNum = 1;
		    nextStringNum = 1;
		    resetRegs();
	    }

        /********************** AST Traversal Methods ************************/
	
	    int GenExpression(AST n, string[] LocalVars)
        {
		    int result = 0;
		    int lhs, rhs;
		    Loc mem;

		    switch (n.Tag)
            {
		        case NodeType.Ident:
                    // DONE ; NEEDS CHECKING
                    mem = GenVariable(n, LocalVars);
                    result = getReg();			
                    Asm.Append("ldr", Loc.RegisterName(result), mem);
			        break;
		        case NodeType.Dot:
                    // DONE ; NEEDS CHECKING
                    mem = GenVariable(n, LocalVars);
                    result = getReg();			
                    Asm.Append("ldr", Loc.RegisterName(result), mem);
			        break;
		        case NodeType.Index:
                    // DONE
			        mem = GenVariable(n, LocalVars);
			        result = getReg();
			        Asm.Append("ldr", Loc.RegisterName(result), mem);
			        break;
		        case NodeType.Add:
                    // DONE
			        result = lhs = GenExpression(n[0], LocalVars);
			        rhs = GenExpression(n[1], LocalVars);
			        Asm.Append("add", Loc.RegisterName(lhs), Loc.RegisterName(lhs),
						        Loc.RegisterName(rhs));
			        freeReg(rhs);
			        break;
		        case NodeType.StringConst:
                    // DONE
			        result = getReg();
			        string slab = createStringConstant(((AST_leaf)n).Sval);
			        Asm.Append("ldr", Loc.RegisterName(result), "=" + slab);
			        break;
		        case NodeType.UnaryMinus:
                    // DONE
			        result = lhs = GenExpression(n[0], LocalVars);
			        Asm.Append("rsb", Loc.RegisterName(lhs), Loc.RegisterName(lhs), "#0");
			        break;
		        case NodeType.Sub:
                    // DONE
			        result = lhs = GenExpression(n[0], LocalVars);
			        rhs = GenExpression(n[1], LocalVars);
			        Asm.Append("sub", Loc.RegisterName(lhs), Loc.RegisterName(lhs),
						        Loc.RegisterName(rhs));
			        freeReg(rhs);
			        break;
		        case NodeType.Mul:
                    // DONE
			        lhs = GenExpression(n[0], LocalVars);
			        rhs = GenExpression(n[1], LocalVars);
			        result = getReg();
			        Asm.Append("mulsl", Loc.RegisterName(result), Loc.RegisterName(lhs),
						        Loc.RegisterName(rhs));
			        freeReg(lhs);
			        freeReg(rhs);
			        break;		
		        case NodeType.Div:
                    // DONE ; NEEDS CHECKING
			        result = lhs = GenExpression(n[0], LocalVars);
			        rhs = GenExpression(n[1], LocalVars);
			        Asm.Append("mov", "r0", Loc.RegisterName(lhs));
			        Asm.Append("mov", "r1", Loc.RegisterName(rhs));
			        Asm.Append("bl", "cb.DivMod");
			        Asm.Append("mov", Loc.RegisterName(lhs), "r0");
			        freeReg(rhs);
			        break;
		        case NodeType.Mod:
                    // DONE ; NEEDS CHECKING
			        result = lhs = GenExpression(n[0], LocalVars);
			        rhs = GenExpression(n[1], LocalVars);
			        Asm.Append("mov", "r0", Loc.RegisterName(lhs));
			        Asm.Append("mov", "r1", Loc.RegisterName(rhs));
			        Asm.Append("bl", "cb.DivMod");
			        Asm.Append("mov", Loc.RegisterName(lhs), "r1");
			        freeReg(rhs);
			        break;
		        case NodeType.IntConst:
                    // DONE
			        result = getReg();
			        int val = ((AST_leaf)n).Ival;
			        if (255 >= val && val >= 0)
				        Asm.Append("mov", Loc.RegisterName(result), "#" + val.ToString());
			        else if (-255 <= val && val < 0)
				        Asm.Append("mvn", Loc.RegisterName(result), "#" + (-val).ToString());
			        else
				        Asm.Append("ldr", Loc.RegisterName(result), "=" + val.ToString());
			        break;
		        case NodeType.NewStruct:
                    notImplemented(n, "GenExpression");
                    break;
		        case NodeType.NewArray:
                    // DONE ; NEEDS CHECKING
                    // calculate heap space needed (4 bytes for length of array, and 'size' bytes for each element)
                    int length = ((AST_leaf)n[1]).Ival;
                    int size = getTypeSize(n[0].Type);
                    int space = (length * size) + 4;
                    int reg = getReg();
                    Asm.Append("mov", "r0", "#" + space.ToString());
                    // request space from the malloc routine
                    Asm.Append("bl", "cb.Malloc");
                    Asm.Append("mov", Loc.RegisterName(reg), "#" + length.ToString());
                    // store array size in first word, and advance pointer to first element
                    mem = new LocRegOffset(0, size, MemType.Byte);
                    Asm.Append("str", Loc.RegisterName(reg), mem);
                    freeReg(reg);
                    result = 0;
			        break;
		        default:
			        throw new Exception("Unexpected tag: " + n.Tag.ToString());
		    }
		    return result;
	    }

        Loc GenVariable(AST n, string[] LocalVars)
        {
		    Loc result = null;
		    int lhs, offset = 0;
		    MemType mtyp;
		    switch (n.Tag)
            {
		        case NodeType.Ident:
			        // The ident must be a local variable or a formal parameter.
			        // In either case, the memory location is at an offset from
			        // the frame pointer register.

			        // search for existing strings?
			        // string label = searchStringConstants(((AST_leaf)n).Sval);
			
			        // search for local variable
                    int pos = Array.IndexOf(LocalVars, ((AST_leaf)n).Sval);
                    // determine offset
			        offset = -4 * (pos + 1);
			        mtyp = MemType.Byte;  // FIX ME!
			        result = new LocRegOffset(fp, offset, mtyp);
			        break;
		        case NodeType.Dot:
                    // FIX ME ; FAR FROM DONE ; MAYBE UBER WRONG

                    // case where expression is String.Length
                    if (n[0].Type == FrontEnd.CbType.String)
                    {
                        lhs = getReg();
                        // load string into r0
                        // find string
                        // FIX ME ; only works for string variables and string constants, not string fields in structures or arrays of strings or combinations thereof
                        string label = null;
                        if (n[0].NumChildren == 0)
                        {
                            string val = ((AST_leaf)n[0]).Sval;
                            if (n[0].Tag == NodeType.Ident)
                            {
                                label = searchStringConstants(val);
                                if (label == null)
                                {
                                    throw new Exception("String variable memory not allocated: " + val);
                                }
                            } else if (n[0].Tag == NodeType.StringConst)
                            {
                                label = searchStringConstants(val);
                                if (label == null)
                                    label = createStringConstant(val);
                            }
                        }
                        Asm.Append("ldr", "r0", label);
                        Asm.Append("bl", "cb.StrLen");
                        offset = 0;
                        mtyp = MemType.Byte;
                    }
                    // case where expression is Array.Length
                    else if (n[0].Type is FrontEnd.CbArray)
                    {
                        lhs = GenExpression(n[0], LocalVars);
                        offset = -4;
                        mtyp = MemType.Byte;
                    }
                    // case where expression is struct.field
                    else if (n[0].Type is FrontEnd.CbStruct)
                    {
                        // FIX ME
                        // The left operand must be an expression with a struct type.
                        // The right operand must be the name of a field in that struct.
                        // The code should set result to a LocRegOffset instance where
                        // the register comes from n[0] and the offset from n[1].
                        lhs = GenExpression(n[0], LocalVars);
                        string rhs = ((AST_leaf)n[1]).Sval;
                        offset = -40; // FIX ME!
                        mtyp = MemType.Word;  // FIX ME!
                    }
                    else
                    {
                        throw new Exception("Unknown Dot operation with left node type " + n[0].Type.ToString());
                    }

			        result = new LocRegOffset(lhs, offset, mtyp);			
			        break;
		        case NodeType.Index:
			        // The left operand must be an expression with an array or string type
			        // The right operand must be an int expression to use as an index.
			        // The code should set result to a LocRegIndex instance where
			        // the register comes from n[0] and the offset from n[1].
			        lhs = GenExpression(n[0], LocalVars);	// is the lhs an expression?
                    // check for struct reference
                    if (n[0].Tag == NodeType.Dot)
                    {
                        result = GenVariable(n[0], LocalVars);     // FIX ME: does not take array position into account!
                        mtyp = MemType.Byte;			// FIX ME: not always true
                    }
                    else
                    {
                        offset = ((AST_leaf)n[1]).Ival * 4;
                        mtyp = MemType.Byte;			// FIX ME: not always true
                        result = new LocRegIndex(lhs, offset, mtyp);
                    }
			        break;
		    }
		    return result;
	    }
	
	    void GenStatement(AST n, string[] LocalVars)
        {
		    Loc variable;
		    switch (n.Tag)
            {
		        case NodeType.Assign:
                    // DONE ; NEEDS CHECKING
                    int reg;
                    Loc lhs = GenVariable(n[0], LocalVars);    // LHS

                    // check for call
                    if (n[1].Tag == NodeType.Call)
                    {
                        GenStatement(n[1], LocalVars);         // RHS
                        reg = 1;
                    }
                    else
    			        reg = GenExpression(n[1], LocalVars);  // RHS

                    if (lhs.Type == MemType.Word)
                        Asm.Append("str", Loc.RegisterName(reg), lhs);
                    else if (lhs.Type == MemType.Byte)
                        Asm.Append("strb", Loc.RegisterName(reg), lhs);
                    else
                        throw new Exception("unsupported memory type " + lhs.Type.ToString());
                    freeReg(reg);
			        break;
		        case NodeType.LocalDecl:
                    // DONE ; NEEDS CHECKING

                    // get position of first empty slot in array
                    int pos = Array.FindIndex(LocalVars, i => string.IsNullOrEmpty(i));
                    // add newly declared variables to array
                    for (int i = 0; i < n[1].NumChildren; i++)
                    {
                        LocalVars[pos] = ((AST_leaf)n[1][i]).Sval;
                        pos++;
                    }

			        break;
		        case NodeType.Block:
                    // DONE
			        for (int i = 0;  i < n.NumChildren;  i++)
				        GenStatement(n[i], LocalVars);
			        break;
		        case NodeType.Read:
                    // DONE
			        variable = GenVariable(n[1], LocalVars);
			        Asm.Append("bl", "cb.ReadInt");
			        Asm.Append("str", Loc.RegisterName(0), variable);
			        break;
		        case NodeType.Call:
			        // Note the special case: cbio.Write(x) where x is an int or a string expression.
			        // The argument x must be loaded into register r0 and then a bl instruction
			        // with cb.WriteInt or cb.WriteString as the destination must be generated.
			        // Otherwise for a call to a method in the CFlat program, the actual parameters
			        // should be evaluated in reverse order and pushed onto the stack; then a
			        // bl instruction to the method generated; that is followed by an add immediate
			        // instruction to pop the stack of all the parameters.

                    // cbio.Write(x)
                    if (n[0].Tag == NodeType.Dot)
                    {
                        int regw = GenExpression(n[1][0], LocalVars);
                        Asm.Append("mov", "r0", Loc.RegisterName(regw));
                        // print integer
                        if (true)           // FIX ME: Detect when expression is an integer
                            Asm.Append("bl", "cb.WriteInt");
                        // print string
                        else if (false)     // FIX ME: Detect when expression is a string
                            Asm.Append("bl", "cb.WriteString");
                        else
                            throw new Exception("Invalid parameter to cbio.Write: " + n[1][0].Tag.ToString());
                        freeReg(regw);
                    }
                    // regular method call
                    else
                    {
                        int i, ilab;
                        string slab;
                        int numc = n[1].NumChildren;
                        Loc temp;
                        int tmpr = getReg();

                        // push each parameter onto the stack
                        for (i = 0; i < numc; i++)
                        {
                            temp = GenVariable(n[1][i], LocalVars);    // Note: Should this be GenExpression ?!
                            variable = new LocRegOffset(sp, -4, MemType.Byte);
                            if (temp.Type == MemType.Byte)
                            {
                                ilab = ((AST_leaf)n[1][i]).Ival;
                                Asm.Append("ldr", Loc.RegisterName(tmpr), "=" + ilab);
                            }
                            else if (temp.Type == MemType.Word)
                            {
                                slab = createStringConstant(((AST_leaf)n[1][i]).Sval);
                                Asm.Append("ldr", Loc.RegisterName(tmpr), slab);
                                variable = new LocRegOffset(sp, -4, MemType.Word);
                            }
                            else
                            {
                                throw new Exception("Invalid parameter: " + n[1][i].Tag.ToString());
                            }
                            // push onto stack and increase pointer by 4 bytes
                            Asm.Append("str", Loc.RegisterName(tmpr), variable + "!");
                        }
                        // go to method
                        Asm.Append("bl", ((AST_leaf)n[0]).Sval);
                        // pop bytes off stack (equal to 4 * # of parameters)
                        i = numc * 4;
                        Asm.Append("add", "sp", "sp", "#" + i.ToString());
                        // move result out of scratch register
                        Asm.Append("mov", Loc.RegisterName(tmpr), "r0");
                        // store result in local variable
                        variable = new LocRegOffset(fp, -40, MemType.Byte); // stack pointer always decreases by 40??
                        Asm.Append("str", Loc.RegisterName(tmpr), variable);
                        freeReg(tmpr);
                    }

			        break;
		        case NodeType.Return:
                    // DONE ; NEEDS CHECKING ; MAYBE NEEDS TO DO A LOT MORE
                    // load result into r1 if something is returned
                    if (n.NumChildren > 0)   // FIX ME: "n[0].Tag != CbType.Empty" does not work
                    {
                        int ret = GenExpression(n[0], LocalVars);
                        Asm.Append("ldr", Loc.RegisterName(1), Loc.RegisterName(ret));
                    }

                    // return statement is a transfer to the label held in 'returnLabel'
                    Asm.Append("b", returnLabel);            
                    break;
		        case NodeType.PlusPlus:
                    // DONE
			        int pp = GenExpression(n[0], LocalVars);
                    string preg = Loc.RegisterName(pp);
                    Asm.Append("add", preg, preg, "#1");
                    freeReg(pp);
			        break;
		        case NodeType.MinusMinus:
                    // DONE
			        int mm = GenExpression(n[0], LocalVars);
                    string mreg = Loc.RegisterName(mm);
                    Asm.Append("sub", mreg, mreg, "#1");
                    freeReg(mm);
			        break;
		        case NodeType.If:
                    // DONE
                    string tl = getNewLabel();
                    string lend = getNewLabel();

                    // no else statement
                    if (n[2].Tag == NodeType.Empty)
                    {
                        // if
                        GenConditional(n[0], tl, lend, LocalVars);
                        // then
                        Asm.AppendLabel(tl);
                        GenStatement(n[1], LocalVars);
                        Asm.Append("b", lend);
                    }
                    // else statement
                    else
                    {
                        string fl = getNewLabel();
                        // if
                        GenConditional(n[0], tl, fl, LocalVars);
                        // then
                        Asm.AppendLabel(tl);
                        GenStatement(n[1], LocalVars);
                        Asm.Append("b", lend);
                        // else
                        Asm.AppendLabel(fl);
                        GenStatement(n[2], LocalVars);
                    }
                    // end of if statement
                    Asm.AppendLabel(lend);
                    break;
		        case NodeType.While:
                    // DONE ; NEEDS CHECKING
                    string wcond = getNewLabel();
                    string wstart = getNewLabel();
                    string wend = getNewLabel();

                    // while
                    Asm.AppendLabel(wcond);
                    GenConditional(n[0], wstart, wend, LocalVars);
                    // then do
                    Asm.AppendLabel(wstart);
                    GenStatement(n[1], LocalVars);
                    Asm.Append("b", wcond);
                    // end of loop
                    Asm.AppendLabel(wend);
                    break;
		        case NodeType.Break:
			        // DONE ; NEEDS CHECKING ; MIGHT NEED TO DO A LOT MORE
			        Asm.Append("b", returnLabel);
			        break;
		        case NodeType.Empty:
                    // DONE
			        // no code to generate!
			        break;
		        default:
			        throw new Exception("Unexpected tag: " + n.Tag.ToString());
		    }
	    }

	    // n is a subtree which generates a true-false test
	    // TL and FL are labels to jump to if the test is true/false respectively
        void GenConditional(AST n, string TL, string FL, string[] LocalVars)
        {
            int lhs, rhs;
            switch (n.Tag)
            {
		        case NodeType.And:
                    // DONE
			        string mida = getNewLabel();
                    // check if first condition is true
                    GenConditional(n[0], mida, FL, LocalVars);
                    // if first condition is true, check second condition
                    Asm.AppendLabel(mida);
                    GenConditional(n[1], TL, FL, LocalVars);
			        break;
		        case NodeType.Or:
                    // DONE
			        string mido = getNewLabel();
                    // check if first condition is true; if true, go to end
                    GenConditional(n[0], TL, mido, LocalVars);
                    // if first condition is false, check second condition
                    Asm.AppendLabel(mido);
                    GenConditional(n[1], TL, FL, LocalVars);
			        break;
		        case NodeType.Equals:
                    // DONE
                    // needs to compare strings?
                    lhs = GenExpression(n[0], LocalVars);
                    rhs = GenExpression(n[1], LocalVars);
                    Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
			        Asm.Append("beq", TL);
			        Asm.Append("b", FL);
                    freeReg(lhs);
                    freeReg(rhs);
                    break;
		        case NodeType.NotEquals:
                    // DONE
                    // needs to compare strings?
                    lhs = GenExpression(n[0], LocalVars);
                    rhs = GenExpression(n[1], LocalVars);
                    Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
			        Asm.Append("bne", TL);
			        Asm.Append("b", FL);
                    freeReg(lhs);
                    freeReg(rhs);
                    break;
		        case NodeType.LessThan:
                    // DONENG
                    // needs to compare strings?
                    lhs = GenExpression(n[0], LocalVars);
                    rhs = GenExpression(n[1], LocalVars);
                    Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
                    Asm.Append("blt", TL);
                    Asm.Append("b", FL);
                    freeReg(lhs);
                    freeReg(rhs);
			        break;
		        case NodeType.GreaterThan:
                    // DONE
                    // needs to compare strings?
                    lhs = GenExpression(n[0], LocalVars);
                    rhs = GenExpression(n[1], LocalVars);
                    Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
                    Asm.Append("bgt", TL);
                    Asm.Append("b", FL);
                    freeReg(lhs);
                    freeReg(rhs);
			        break;
		        case NodeType.LessOrEqual:
                    // DONE
                    // needs to compare strings?
                    lhs = GenExpression(n[0], LocalVars);
                    rhs = GenExpression(n[1], LocalVars);
                    Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
                    Asm.Append("ble", TL);
                    Asm.Append("b", FL);
                    freeReg(lhs);
                    freeReg(rhs);
			        break;
		        case NodeType.GreaterOrEqual:
                    // DONE
                    // needs to compare strings?
                    lhs = GenExpression(n[0], LocalVars);
                    rhs = GenExpression(n[1], LocalVars);
                    Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
                    Asm.Append("bge", TL);
                    Asm.Append("b", FL);
                    freeReg(lhs);
                    freeReg(rhs);
			        break;
		        default:
			        throw new Exception("Unexpected tag: " + n.Tag.ToString());
		    }
	    }
	
	    void GenMethod(AST n)
        {
		    // NOT DONE ; NEEDS A LOT OF WORK
		    Asm.StartMethod(((AST_leaf)n[1]).Sval);

		    // 1. generate prolog code
		    returnLabel = getNewLabel();
            // push all registers in r4-r14 onto stack
            Asm.Append("stmfd", "sp!", "{r4-r12,lr}");
            // set up frame pointer
            Asm.Append("mov", "fp", "sp");
		
		    // NOTE: Need to add code in CbTcVistitor.cs to determine number of local variables;
		    // FROM NIGEL... store the sizes as annotations on the tree nodes ... and the leaf 
		    // node class for an identifier used as a variable's name or a struct type name 
		    // does have an unused int field which can be used for that purpose.
            // ^^ THIS HAS BEEN DONE, BUT IS NOT TESTED!!!
		
            // reserve bytes for local variables in the function
            int allocb = ((AST_leaf)n[1]).Ival;
            //int allocb = 20;    // replace this with prior line after testing
            Asm.Append("sub", "sp", "sp", "#" + (allocb*4).ToString());

		    // 2. translate the method body
            string[] LocalVars = new string[allocb];
		    GenStatement(n[3], LocalVars);

		    // 3. generate epilog code
		    Asm.EndMethod();
            Asm.AppendLabel(returnLabel);
            // pop local variables
            Asm.Append("mov", "sp", "fp");
            // reload saved registers and return flow
            Asm.Append("ldmfd", "sp!", "{r4-r12,pc}");
            // go back to calling method
            Asm.Append("b", "lr");
	    }

	    void GenConstDefn(AST n)
        {
		    if (n[2].Tag == NodeType.IntConst)
            {
			    Asm.AppendDirective(".align", "2");   // the .align is almost certainly redundant
			    Asm.AppendLabel(((AST_leaf)n[1]).Sval);
			    Asm.AppendDirective(".word", ((AST_leaf)n[2]).Ival.ToString());
		    } else // n[2] must be a StringConst
			    createStringConstant(((AST_leaf)n[1]).Sval, ((AST_leaf)n[2]).Sval);
	    }

        public void GenProgram(AST n)
        {
            // main method prologue
            Asm.AppendDirective(".global _start");
            Asm.AppendDirective(".text");
            Asm.AppendLabel("_start");
            Asm.Append("mov", "r0", "#0");
            Asm.Append("bl", "Main");
            // main method epilogue
            Asm.Append("mov", "r0", "#0x18");
            Asm.Append("mov", "r1", "#0");
            Asm.Append("swi", "0x123456");

		    AST decls = n[2];
		    for (int i = 0; i < decls.NumChildren;  i++)
            {
			    AST decl = decls[i];
			    switch (decl.Tag)
                {
			        case NodeType.Method:
				        GenMethod(decl);
				        break;
			        case NodeType.Const:
				        GenConstDefn(decl);
				        break;
			        case NodeType.Struct:
				        // no code to generate
				        break;
			        default:
				        throw new Exception("Unexpected tag: " + n.Tag.ToString());
			    }
		    }
		    // start the data section and
		    // declare any string constants used in the program
		    Asm.AppendDirective(".data");
		    defineStringConstants();

		    // if there are any static data items left to declare, they should
		    // be generated at this point ... but there probably aren't any

	    }
    
        /************************ Utility Methods **************************/

        // returns the size of a type
        private int getTypeSize(FrontEnd.CbType t)
        {
            //Console.WriteLine("gTS: Type is '{0}'", t);
            if (t == FrontEnd.CbType.Int || t == FrontEnd.CbType.String || t is FrontEnd.CbArray)
                return 4; // ints are words, and so are pointers to strings and arrays
            else if (t is FrontEnd.CbStruct)
            {
                FrontEnd.CbStruct s = (FrontEnd.CbStruct) t;
                int size = 0;
                foreach (FrontEnd.CbField f in s.Fields.Values)
                    size += getTypeSize(f.Type);
                return size;
            }
            else
                throw new Exception("getTypeSize: Unknown type: " + t);
        }

	    // generates a unique label name which cannot clash with
	    // a method name or const name
	    private string getNewLabel()
        {
		    return string.Format("_L.{0}", nextLabelNum++);
	    }

	    // When Assignment 4 is completed, there should be no calls to
	    // this method any longer
	    private void notImplemented(AST n, string where)
        {
		    string s = string.Format("Node type {0} not implemented in function {1}", n.Tag, where);
		    Console.WriteLine(s);
		    Asm.AppendComment(s);
	    }
	
	    private string immediate(int n)
        {
		    return string.Format("#0x{0:X}", n);
	    }

        /******************** Free Register Tracking **********************/

	    private bool[] regAvailable;

	    // marks registers 4-11 as being available
	    void resetRegs()
        {
		    for (int i=0; i<=11; i++)
			    regAvailable[i] = true;
	    }

	    int getReg()
        {
		    for (int i = 4; i<=10; i++ )
            {
			    if (regAvailable[i])
                {
				    regAvailable[i] = false;
				    return i;
			    }
		    }
		    Console.WriteLine("Ran out of registers!!");
		    return 0;
	    }

	    void freeReg( int regNum )
        {
		    if (regNum >= 4 && regAvailable[regNum])
            {
			    Console.WriteLine("Possible compiler error -- register {0} freed twice", regNum);
		    } else
			    regAvailable[regNum] = true;
	    }

        /******************** String Constant Handling **********************/

	    // use this method to define an unnamed string constant; the result
	    // is a new unique label attached to that string constant
	    private string createStringConstant(string s)
        {
		    string label = string.Format("_S.{0}", nextStringNum++);
		    stringConsts.Add(Tuple.Create(label,s));
		    return label;
	    }
    
	    // use this method to define a named string constant
	    private string createStringConstant(string name, string s)
        {
		    stringConsts.Add(Tuple.Create(name,s));
		    return name;
	    }

	    private void defineStringConstants()
        {
		    foreach (Tuple<string,string> pair in stringConsts)
            {
			    Asm.AppendLabel(pair.Item1);
			    Asm.AppendDirective(".asciz", pair.Item2);
		    }
		    stringConsts.Clear();
	    }
	    
	    private string searchStringConstants(string name)
        {
		    foreach (Tuple<string,string> pair in stringConsts)
            {
			    if (pair.Item2 == name)
				    return pair.Item1;
		    }
		    return null;
	    }
    }

} // end of namespace BackEnd