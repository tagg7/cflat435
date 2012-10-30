/*  CbPrVisitor.cs

    Defines a Print Visitor class for the CFlat AST
    
    Author: Nigel Horspool
    
    Date: Oct 2012
*/

using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {


// Traverses the AST to output a textual representation
// If the Type field of a node is not null, that datatype is included in the output.
public class PrVisitor: Visitor {
	private TextWriter f;    // where to output the tree
	private int indent = 0;  // current indentation level

	// constructor where the output destination can be specified
	public PrVisitor( TextWriter outputStream ) {
		f = outputStream;
		indent = 0;
	}

	// constructor where the tree gets written to standard output
	public PrVisitor() {
		f = Console.Out;
		indent = 0;
	}

    private string indentString( int indent ) {
        return " ".PadRight(2*indent);
    }

    private void printTag( AST node ) {
        f.Write("{0}{1}  [line {2}]", indentString(indent), node.Tag, node.LineNumber);
        if (node.Type != null)
            f.Write(", type {0}", node.Type);
    }

	public override void Visit(AST_kary node) {
        printTag(node);
        f.WriteLine();
        int arity = node.NumChildren;
        indent++;
        for( int i = 0; i < arity; i++ ) {
        	AST ch = node[i];
            if (ch != null)
                ch.Accept(this);
            else
                f.WriteLine("{0}-- missing child --", indentString(indent));
        }
        if (arity == 0)
            f.WriteLine("{0}-- no children --", indentString(indent));
        indent--;
    }

	public override void Visit(AST_leaf node) {
        printTag(node);
        switch(node.Tag) {
        case NodeType.Ident:
        case NodeType.StringConst:
            f.WriteLine(" \"{0}\"", node.Sval);  break;
        case NodeType.IntConst:
            f.WriteLine(" {0}", node.Ival);  break;
        default:
            f.WriteLine();  break;
        }
    }

	public override void Visit( AST_nonleaf node ) {
        printTag(node);
        f.WriteLine();
        int arity = node.NumChildren;
        indent++;
        for( int i = 0; i < arity; i++ ) {
        	AST ch = node[i];
            if (ch != null)
                ch.Accept(this);
            else
                f.WriteLine("{0}-- missing child --", indentString(indent));
        }
        if (arity == 0)
            f.WriteLine("{0}-- no children --", indentString(indent));
        indent--;
    }

}

}
