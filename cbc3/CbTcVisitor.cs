/*  CbTcVisitor.cs

    Defines a Type Checking Visitor class for the CFlat AST
    
    Authors: Stephen Bates and Mike Lyttle
    
    Date: Oct 2012
*/

using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {


// Traverses the AST to annotate the tree nodes with their datatypes
// reporting semantic errors in the process
public class TcVisitor: Visitor {
    public int NumErrors   { get; private set; }
    public int NumWarnings { get; private set; }

    // all methods declared in the CFlat program (which is a single class)
    public IDictionary<string,CbMethod> Methods {get; private set;}

    // all structs declared in the CFlat program
    public IDictionary<string,CbStruct> Structs {get; private set;}    

    // all consts declared in the CFlat program
    public IDictionary<string,CbType>  Consts  {get; private set;}
    
    // a symbol table used while typechecking a method body
    private SymTab localSymbols;
    
	// constructor
	public TcVisitor() {
		NumErrors   = 0;
		NumWarnings = 0;
		Methods = new Dictionary<string,CbMethod>();
		Structs = new Dictionary<string,CbStruct>();
		Consts  = new Dictionary<string,CbType>();
		localSymbols = new SymTab();
	}

    // Use this method to output an informational message (neither an error
    // nor a warning). If no line number can be associated with the message,
    // provide 0 for the ln parameter.
    public void ReportInformation( int ln, string msg, params object[] args) {
        if (ln > 0) Console.Write("{0}: ", ln);
        Console.WriteLine(msg, args);
    }

	// generate an error message
	public void ReportError( int ln, string msg, params object[] args) {
	    ReportInformation(ln, msg, args);
	    NumErrors++;
	}

	// generate a warning message
	public void ReportWarning( int ln, string msg, params object[] args) {
	    ReportInformation(ln, msg, args);
	    NumWarnings++;
	}

    // check if the child nodes are integers
    private int intExprCheck( AST_nonleaf node ) {
        int children = node.NumChildren;

        node[0].Accept(this);

        if (children == 1)
        {
            if (node[0].Type != CbType.Int)
            {
                ReportError(node[0].LineNumber, "Cannot perform {0} operation on types {1} and {2}", node.Tag, node[0].Type, node[1].Type);
                return 1;
            }
        }
        else if (children == 2)
        {
            node[1].Accept(this);

            if (node[0].Type != CbType.Int && node[1].Type != CbType.Int)
            {
                ReportError(node[0].LineNumber, "Cannot perform {0} operation on type {1}", node.Tag, node[0].Type);
                return 1;
            }
        }
        
        node.Type = CbType.Int;

        return 0;
    }

    // given an Ident or an Array node, look up the type representation
    private CbType lookUpType( AST node ) {
        CbType result = CbType.Error;
        if (node.Tag == NodeType.Array) {
            CbType elemType = lookUpType(node[0]);
            result = CbType.Array(elemType);
        } else {
            // it has to be an Ident node
            string name = ((AST_leaf)node).Sval;
            if (Structs.ContainsKey(name))
                result = Structs[name];  // it's a struct type
            else if (name == "int")
                result = CbType.Int;
            else if (name == "string")
                result = CbType.String;
            else
                ReportError(node.LineNumber, "Unknown type {0}", name);
        }
        node.Type = result; //annotate the node
        return result;
    }

	public override void Visit(AST_kary node) {
        switch(node.Tag) {
        case NodeType.UsingList:
            break;
        case NodeType.DeclList:
            break;
        case NodeType.FieldList:
            break;
        case NodeType.IdList:
            break;
        case NodeType.Formals:
            break;
        case NodeType.Block:
            node[0].Accept(this);
            break;
        case NodeType.Actuals:
            break;
        case NodeType.Return:
            break;
        default:
            throw new Exception("{0} is not a tag compatible with an AST_kary node");
        }
    }

	public override void Visit( AST_nonleaf node ) {
        switch(node.Tag) {
        case NodeType.Program:
            // do a multiple pre-pass over the declarations of structs, methods and consts
            prePass(node[2]);
            // now check that the using list is OK
            node[0].Accept(this);
            // now typecheck the bodies of method declarations
            node[2].Accept(this);
            break;
        case NodeType.Const:
            // FIX ME!
            // Must type check the expression used to initialize the constant and verify
            // that its type matches the declared type for this constant
            break;
        case NodeType.Struct:
            // Nothing to do ... this has been handled by the prePass method below
            break;
        case NodeType.Method:
            localSymbols.Empty();  // clear the symbol table
            // FIX ME!
            // We have to typecheck the method
            // 1. initialize the symbol table with the formal parameters
            // 2. visit the body of the method, type checking it
            break;
        case NodeType.FieldDecl:
            // Nothing to do ... this has been handled by the prePass method below
            break;
        case NodeType.Formal:
            // Nothing to do ... this has been handled by the prePass method below
            break;
        case NodeType.Array:
            break;
        case NodeType.LocalDecl:
            break;
        case NodeType.Assign:
            break;
        case NodeType.Call:
            break;
        case NodeType.PlusPlus:
            intExprCheck(node);
            break;
        case NodeType.MinusMinus:
            intExprCheck(node);
            break;
        case NodeType.If:
            break;
        case NodeType.While:
            break;
        case NodeType.Read:
            break;
        case NodeType.Add:
            intExprCheck(node);
            break;
        case NodeType.Sub:
            intExprCheck(node);
            break;
        case NodeType.Mul:
            intExprCheck(node);
            break;
        case NodeType.Div:
            intExprCheck(node);
            break;
        case NodeType.Mod:
            intExprCheck(node);
            break;
        case NodeType.And:
            intExprCheck(node);
            break;
        case NodeType.Or:
            intExprCheck(node);
            break;
        case NodeType.Equals:
            intExprCheck(node);
            break;
        case NodeType.NotEquals:
            intExprCheck(node);
            break;
        case NodeType.LessThan:
            intExprCheck(node);
            break;
        case NodeType.GreaterThan:
            intExprCheck(node);
            break;
        case NodeType.LessOrEqual:
            intExprCheck(node);
            break;
        case NodeType.GreaterOrEqual:
            intExprCheck(node);
            break;
        case NodeType.UnaryMinus:
            intExprCheck(node);
            break;
        case NodeType.Dot:
            break;
        case NodeType.NewStruct:
            break;
        case NodeType.NewArray:
            break;
        case NodeType.Index:
            break;
        default:
            throw new Exception("{0} is not a tag compatible with an AST_nonleaf node");
        }
    }

	public override void Visit(AST_leaf node) {
        switch(node.Tag) {
        case NodeType.Empty:
            break;
        case NodeType.Break:
            break;
        case NodeType.IntConst:
            node.Type = CbType.Int;
            break;
        case NodeType.StringConst:
            node.Type = CbType.String;
            break;
        case NodeType.Ident:
            break;
        default:
            throw new Exception("{0} is not a tag compatible with an AST_leaf node");
        }
    }

    // Makes two shallow passes over the declarations inside a class to obtain
    // the names and types of all consts, methods and structs declared at the
    // top level; without this info, typechecking cannot begin.
    private void prePass( AST node ) {
        AST_kary decls = node as AST_kary;
        if (decls == null || decls.Tag != NodeType.DeclList)
            throw new Exception("Bad argument passed to prePass");
        // make one pass over the declarations just to enter the names of structs into
        // the Structs table
        int arity = decls.NumChildren;
        for( int i = 0; i < arity; i++ ) {
            AST ch = decls[i];
            if (ch.Tag != NodeType.Struct)
                continue;  // it was not a struct declaration
            string name = ((AST_leaf)(ch[0])).Sval;
            if (Structs.ContainsKey(name))
                ReportError(ch[0].LineNumber, "Duplicate declaration of struct {0}", name);
            else
                Structs.Add(name, new CbStruct(name));
        }   
        // now make a second pass over the declarations to
        // 1. add const declarations to the Consts table
        // 2. add method declarations to the Methods table
        // 3. fill in the field details for each struct in the Structs table
        for( int i = 0; i < arity; i++ ) {
            AST ch = decls[i];
            switch(ch.Tag) {
            case NodeType.Struct:
                // FIX ME!
                // Add the fields to the CbStruct instance in the Structs table
                break;
            case NodeType.Const:
                // Add the name and type of this constant to the Consts table
                CbType typ = lookUpType(ch[0]);
                string name = ((AST_leaf)(ch[1])).Sval;
                if (Consts.ContainsKey(name))
                    ReportError(ch[0].LineNumber, "Duplicate declaration of const {0}", name);
                else
                    Consts.Add(name, typ);
                break;
            case NodeType.Method:
                // FIX ME!
                // Add the name and full signature of this method to the Methods table
                break;
            default:
                throw new Exception("Unexpected node type " + ch.Tag);
            }
        }
    }
}

}
