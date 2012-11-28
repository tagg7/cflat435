// GenCode.cs

using System;
using System.Collections.Generic;
using System.IO;

using AST = FrontEnd.AST;
using AST_leaf = FrontEnd.AST_leaf;
using NodeType = FrontEnd.NodeType;


namespace BackEnd {


public class GenCode {
	const int fp = 12;  // frame pointer register
	const int sp = 13;  // stack pointer register
	public ARMAssemblerCode Asm { get; private set; }
	private int nextLabelNum;
	private int nextStringNum;
	private string returnLabel;
	private IList<Tuple<string,string>> stringConsts;

	// Constructor
	public GenCode() {
		Asm = new ARMAssemblerCode();
		regAvailable = new bool[12];
		stringConsts = new List<Tuple<string,string>>();
		nextLabelNum = 1;
		nextStringNum = 1;
		resetRegs();
	}

    /********************** AST Traversal Methods ************************/
	
	int GenExpression( AST n ) {
		int result = 0;
		int lhs, rhs;
		Loc mem;

		switch(n.Tag) {
		case NodeType.Ident:
			notImplemented(n, "GenStatement");
			break;
		case NodeType.Dot:
			notImplemented(n, "GenStatement");
			break;
		case NodeType.Index:
            // DONE
			mem = GenVariable(n);
			result = getReg();
			Asm.Append("ldr", Loc.RegisterName(result), mem);
			break;
		case NodeType.Add:
            // DONE
			result = lhs = GenExpression(n[0]);
			rhs = GenExpression(n[1]);
			Asm.Append("add", Loc.RegisterName(lhs), Loc.RegisterName(lhs),
						Loc.RegisterName(rhs));
			freeReg(rhs);
			break;
		case NodeType.StringConst:
            // DONE
			result = getReg();
			string slab = createStringConstant(((AST_leaf)n).Sval);
			Asm.Append("ldr", Loc.RegisterName(result), slab);
			break;
		case NodeType.UnaryMinus:
            // DONE ; NEEDS CHECKING
			result = lhs = GenExpression(n[0]);
			Asm.Append("mvn", Loc.RegisterName(lhs), Loc.RegisterName(lhs));
			break;
		case NodeType.Sub:
            // DONE ; NEEDS CHECKING
			result = lhs = GenExpression(n[0]);
			rhs = GenExpression(n[1]);
			Asm.Append("sub", Loc.RegisterName(lhs), Loc.RegisterName(lhs),
						Loc.RegisterName(rhs));
			freeReg(rhs);
			break;
		case NodeType.Mul:
            // DONE ; NEEDS CHECKING
			lhs = GenExpression(n[0]);
			rhs = GenExpression(n[1]);
			result = getReg();
			Asm.Append("mulsl", Loc.RegisterName(result), Loc.RegisterName(lhs),
						Loc.RegisterName(rhs));
			freeReg(lhs);
			freeReg(rhs);
			break;		
		case NodeType.Div:
            // DONE ; NEEDS CHECKING
			result = lhs = GenExpression(n[0]);
			rhs = GenExpression(n[1]);
			Asm.Append("mov", "r0", Loc.RegisterName(lhs));
			Asm.Append("mov", "r1", Loc.RegisterName(rhs));
			Asm.Append("bl", "cb.DivMod");
			Asm.Append("mov", Loc.RegisterName(lhs), "r0");
			freeReg(rhs);
			break;
		case NodeType.Mod:
            // DONE ; NEEDS CHECKING
			result = lhs = GenExpression(n[0]);
			rhs = GenExpression(n[1]);
			Asm.Append("mov", "r0", Loc.RegisterName(lhs));
			Asm.Append("mov", "r1", Loc.RegisterName(rhs));
			Asm.Append("bl", "cb.DivMod");
			Asm.Append("mov", Loc.RegisterName(lhs), "r1");
			freeReg(rhs);
			break;
		case NodeType.IntConst:
            // DONE ; NEEDS CHECKING
			result = getReg();
			int val = ((AST_leaf)n).Ival;
			if(255 > val && val >= 0)
				Asm.Append("mov", Loc.RegisterName(result), "#" + val.ToString());
			else if(-255 < val && val < 0)
				Asm.Append("mvn", Loc.RegisterName(result), "#" + Math.Abs(val).ToString());
			else
				Asm.Append("ldr", Loc.RegisterName(result), "=" + val.ToString());
			break;
		case NodeType.NewStruct:
            notImplemented(n, "GenExpression");
            break;
		case NodeType.NewArray:
            // DONE ; NEEDS CHECKING
            result = 0;
            int elem = ((AST_leaf)n[1]).Ival;
            // calculate heap space needed (4 bytes for size of array, and 4 bytes for each element)
            int space = (elem * 4) + 4;
            int reg = getReg();
            Asm.Append("mov", "r0", "#" + space.ToString());
            // request space from the malloc routine
            Asm.Append("bl", "cb.Malloc");
            Asm.Append("mov", Loc.RegisterName(reg), "#" + elem.ToString());
            // store array size in first word, and advance pointer to first element
            mem = new LocRegOffset(0, 4, MemType.Byte);
            Asm.Append("str", Loc.RegisterName(reg), mem);
            freeReg(reg);
			break;
		default:
			throw new Exception("Unexpected tag: " + n.Tag.ToString());
		}
		return result;
	}
	
	Loc GenVariable( AST n ) {
		Loc result = null;
		switch(n.Tag) {
		case NodeType.Ident:
			// The ident must be a local variable or a formal parameter.
			// In either case, the memory location is at an offset from
			// the frame pointer register.
			int offset = -40;  // FIX ME!
			MemType mtyp = MemType.Word;  // FIX ME!
			result = new LocRegOffset( fp, offset, mtyp );
			break;
		case NodeType.Dot:
			// The left operand must be an expression with a struct type.
			// The right operand must be the name of a field in that struct.
			// The code should set result to a LocRegOffset instance where
			// the register comes from n[0] and the offset from n[1].
			notImplemented(n, "GenStatement");
			break;
		case NodeType.Index:
			// The left operand must be an expression with an array or string type
			// The right operand must be an int expression to use as an index.
			// The code should set result to a LocRegIndex instance where
			// the register comes from n[0] and the offset from n[1].
			notImplemented(n, "GenVariable");
			result = new LocLabel("ERROR", MemType.Word);  // FIX ME!
			break;
		}
		return result;
	}
	
	void GenStatement( AST n ) {
		Loc variable;
		switch(n.Tag) {
		case NodeType.Assign:
            // DONE
			Loc lhs = GenVariable(n[0]);  // LHS
			int reg = GenExpression(n[1]);  // RHS
			if (lhs.Type == MemType.Byte)
				Asm.Append("strb", Loc.RegisterName(reg), lhs);
			else if (lhs.Type == MemType.Word)
				Asm.Append("str", Loc.RegisterName(reg), lhs);
			else
				throw new Exception("unsupported memory type " + lhs.Type.ToString());
			break;
		case NodeType.LocalDecl:
            // DONE
            // no instructions are generated for local declarations
			break;
		case NodeType.Block:
            // DONE
			for( int i = 0;  i < n.NumChildren;  i++ )
				GenStatement(n[i]);
			break;
		case NodeType.Read:
            // DONE
			variable = GenVariable(n[1]);
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
                Loc output = GenVariable(n[1]);
                // print integer
                if (output.Type == MemType.Byte)
                {
                    int ilab = ((AST_leaf)n[1][0]).Ival;
                    Asm.Append("ldr", "r0", "=" + ilab);  // does this work?
                    Asm.Append("bl", "cb.WriteInt");
                }
                // print string
                else if (output.Type == MemType.Word)
                {
                    string slab = createStringConstant(((AST_leaf)n[0][0]).Sval);
                    Asm.Append("ldr", "r0", slab);
                    Asm.Append("bl", "cb.WriteString");
                }
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
                    temp = GenVariable(n[1][i]);
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
                        // FIX ME
                        Console.Write("Error");
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
                variable = new LocRegOffset(fp, -40, MemType.Byte); // stack pointer always decrease by 40??
                Asm.Append("str", Loc.RegisterName(tmpr), variable); 
            }

			break;
		case NodeType.Return:
            // DONE ; NEEDS CHECKING
            // load result into r1 if something is returned
            if (true)   // FIX ME: "n[0].Tag != CbType.Empty" does not work
            {
                int ret = GenExpression(n[0]);
                Asm.Append("ldr", Loc.RegisterName(1), Loc.RegisterName(ret));
            }

            // return statement is a transfer to the label held in 'returnLabel'
            Asm.Append("b", returnLabel);            
            break;
		case NodeType.PlusPlus:
            // DONE ; NEEDS CHECKING
			int pp = GenExpression(n[0]);
			Asm.Append("add", Loc.RegisterName(pp), "#1");
			break;
		case NodeType.MinusMinus:
            // DONE ; NEEDS CHECKING
			int mm = GenExpression(n[0]);
			Asm.Append("sub", Loc.RegisterName(mm), "#1");
			break;		
		case NodeType.If:
            // DONE ; NEEDS CHECKING
            string tl = getNewLabel();
            string fl = getNewLabel();
            string lend = getNewLabel();

            // if
            GenConditional(n[0], tl, fl);
            // then
            Asm.AppendLabel(tl);
            GenExpression(n[1]);
            Asm.Append("bl", lend);
            // else
            Asm.AppendLabel(fl);
            GenExpression(n[2]);
            // end of loop
            Asm.AppendLabel(lend);
            break;
		case NodeType.While:
            // DONE ; NEEDS CHECKING
            string wcond = getNewLabel();
            string wstart = getNewLabel();
            string wend = getNewLabel();

            // while
            Asm.AppendLabel(wcond);
            GenConditional(n[0], wstart, wend);
            // then do
            Asm.AppendLabel(wstart);
            GenExpression(n[1]);
            Asm.Append("b", wcond);
            // end of loop
            Asm.AppendLabel(wend);
            break;
		case NodeType.Break:
			notImplemented(n, "GenStatement");
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
	// TL and FL are labels to jump to if the test is true/false resprectively
	void GenConditional( AST n, string TL, string FL ) {
        int lhs, rhs;
        switch(n.Tag) {
		case NodeType.And:
            // DONE ; NEEDS CHECKING
			string mida = getNewLabel();
            // check if first condition is true
            GenConditional(n[0], mida, FL);
            // if first condition is true, check second condition
            Asm.AppendLabel(mida);
            GenConditional(n[1], TL, FL);
			break;
		case NodeType.Or:
            // DONE ; NEEDS CHECKING
			string mido = getNewLabel();
            // check if first condition is true; if true, go to end
            GenConditional(n[0], TL, mido);
            // if first condition is false, check second condition
            Asm.AppendLabel(mido);
            GenConditional(n[1], TL, FL);
			break;
		case NodeType.Equals:
            // DONE ; NEEDS CHECKING
            // needs to compare strings?
			lhs = GenExpression(n[0]);
            rhs = GenExpression(n[1]);
            Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
			Asm.Append("beq", TL);
			Asm.Append("b", FL);
            break;
		case NodeType.NotEquals:
            // DONE ; NEEDS CHECKING
            // needs to compare strings?
			lhs = GenExpression(n[0]);
            rhs = GenExpression(n[1]);
            Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
			Asm.Append("bne", TL);
			Asm.Append("b", FL);
            break;
		case NodeType.LessThan:
            // DONE ; NEEDS CHECKING
            lhs = GenExpression(n[0]);
            rhs = GenExpression(n[1]);
            Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
            Asm.Append("blt", TL);
            Asm.Append("b", FL);
			break;
		case NodeType.GreaterThan:
            // DONE ; NEEDS CHECKING
            lhs = GenExpression(n[0]);
            rhs = GenExpression(n[1]);
            Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
            Asm.Append("bgt", TL);
            Asm.Append("b", FL);
			break;
		case NodeType.LessOrEqual:
            // DONE ; NEEDS CHECKING
            lhs = GenExpression(n[0]);
            rhs = GenExpression(n[1]);
            Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
            Asm.Append("ble", TL);
            Asm.Append("b", FL);
			break;
		case NodeType.GreaterOrEqual:
            // DONE ; NEEDS CHECKING
            lhs = GenExpression(n[0]);
            rhs = GenExpression(n[1]);
            Asm.Append("cmp", Loc.RegisterName(lhs), Loc.RegisterName(rhs));
            Asm.Append("bge", TL);
            Asm.Append("b", FL);
			break;
		default:
			throw new Exception("Unexpected tag: " + n.Tag.ToString());
		}
	}
	
	void GenMethod( AST n ) {
		Asm.StartMethod(((AST_leaf)n[1]).Sval);

		// 1. generate prolog code
		// FIX ME!
		returnLabel = getNewLabel();

		// 2. translate the method body
		GenStatement(n[3]);

		Asm.AppendLabel(returnLabel);
		// 3. generate epilog code
		// FIX ME!
		Asm.EndMethod();

	}

	void GenConstDefn( AST n ) {
		if (n[2].Tag == NodeType.IntConst) {
			Asm.AppendDirective(".align", "2");   // the .align is almost certainly redundant
			Asm.AppendLabel( ((AST_leaf)n[1]).Sval );
			Asm.AppendDirective(".word", ((AST_leaf)n[2]).Ival.ToString());
		} else // n[2] must be a StringConst
			createStringConstant(((AST_leaf)n[1]).Sval, ((AST_leaf)n[2]).Sval);
	}

	public void GenProgram( AST n ) {
		AST decls = n[2];
		for( int i=0;  i<decls.NumChildren;  i++ ) {
			AST decl = decls[i];
			switch(decl.Tag) {
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

	// generates a unique label name which cannot clash with
	// a method name or const name
	private string getNewLabel() {
		return string.Format("_L.{0}", nextLabelNum++);
	}

	// When Assignment 4 is completed, there should be no calls to
	// this method any longer
	private void notImplemented( AST n, string where ) {
		string s = string.Format(
				"Node type {0} not implemented in function {1}", n.Tag, where);
		Console.WriteLine(s);
		Asm.AppendComment(s);
	}
	
	private string immediate( int n ) {
		return string.Format("#0x{0:X}", n);
	}

    /******************** Free Register Tracking **********************/

	private bool[] regAvailable;

	// marks registers 4-11 as being available
	void resetRegs() {
		for(int i=0; i<=11; i++ )
			regAvailable[i] = true;
	}

	int getReg() {
		for(int i = 4; i<=11; i++ ) {
			if (regAvailable[i]) {
				regAvailable[i] = false;
				return i;
			}
		}
		Console.WriteLine("Ran out of registers!!");
		return 0;
	}

	void freeReg( int regNum ) {
		if (regNum >= 4 && regAvailable[regNum]) {
			Console.WriteLine("Possible compiler error -- register {0} freed twice", regNum);
		} else
			regAvailable[regNum] = true;
	}

    /******************** String Constant Handling **********************/

	// use this method to define an unnamed string constant; the result
	// is a new unique label attached to that string constant
	private string createStringConstant( string s ) {
		string label = string.Format("_S.{0}", nextStringNum++);
		stringConsts.Add( Tuple.Create(label,s) );
		return label;
	}
    
	// use this method to define a named string constant
	private string createStringConstant( string name, string s ) {
		stringConsts.Add( Tuple.Create(name,s) );
		return name;
	}

	private void defineStringConstants() {
		foreach( Tuple<string,string> pair in stringConsts ) {
			Asm.AppendLabel(pair.Item1);
			Asm.AppendDirective(".asciz", pair.Item2);
		}
		stringConsts.Clear();
	}
}

} // end of namespace BackEnd