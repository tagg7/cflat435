/*  CbTcVisitor.cs

    Defines a Type Checking Visitor class for the CFlat AST
    
    Authors: Stephen Bates and Mike Lyttle
    
    Date: Nov 2012
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
    public IDictionary<string,List<CbMethod>> Methods {get; private set;}

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
		Methods = new Dictionary<string,List<CbMethod>>();
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

    // check if child nodes are equal to "tleaf" value, then set the node type equal to "tnode"
    private void basicTypeCheck( AST_nonleaf node, CbType tleaf, CbType tnode) {
        int children = node.NumChildren;
        bool err = false;
        for (int i = 0; i < children; i++)
        {
            node[i].Accept(this);
            if (node[i].Type != CbType.Error && node[i].Type != tleaf)
            {
                err = true;
                node.Type = CbType.Error;
            }
        }
        if (err)
        {
            if (children > 1)
                ReportError(node[0].LineNumber, "Cannot perform {0} operation on types '{1}' and '{2}'", node.Tag, node[0].Type, node[1].Type);
            else
                ReportError(node[0].LineNumber, "Cannot perform {0} operation on type '{1}'", node.Tag, node[0].Type);
            return;
        }

        if(tnode != null)
            node.Type = tnode;

        return;
    }

    // given an Ident or an Array node, look up the type representation
    private CbType lookUpType(AST node)
    {
        CbType result = CbType.Error;
        if (node.Tag == NodeType.Array)
        {
            CbType elemType = lookUpType(node[0]);
            result = CbType.Array(elemType);
        }
        else
        {
            // it has to be an Ident node
            result = lookUpIdenType(node);
            if (result == CbType.Error)
            {
                // check if it's a struct
                string name = ((AST_leaf)node).Sval;
                if (Structs.ContainsKey(name))
                    result = Structs[name];  // it's a struct type
                else
                    ReportError(node.LineNumber, "Unknown type {0}", name);
            }
        }
        node.Type = result; //annotate the node
        return result;
    }

    private CbType lookUpIdenType(AST node)
    {
        string iden = ((AST_leaf)node).Sval;
        if (iden == "int")
            return CbType.Int;
        else if (iden == "string")
            return CbType.String;
        else
            return CbType.Error;
    }

    public override void Visit(AST_kary node)
    {
        int children = node.NumChildren;
        switch(node.Tag) {
            case NodeType.UsingList:
                for (int i = 0; i < children; i++)
                {
                    if (((AST_leaf)node[i]).Sval != "CbRuntime")
                        ReportError(node[0].LineNumber, "Invalid using identifier: '{0}' (only allowed identifier is CbRuntime)", ((AST_leaf)node[i]).Sval);
                }
                break;
            case NodeType.DeclList:
                for (int i = 0; i < children; i++)
                {
                    node[i].Accept(this);
                }
                break;
            case NodeType.FieldList:
                // already handled by prePass
                break;
            case NodeType.IdList:
                // already handled in NodeType.LocalDecl
                break;
            case NodeType.Formals:
                // visit all statements
                for (int i = 0; i < children; i++)
                    node[i].Accept(this);
                break;
            case NodeType.Block:
                // change scope
                localSymbols.Enter();
                
                // accept all statements
                for (int i = 0; i < children; i++)
                {
                    node[i].Accept(this);
                }

                // change scope
                localSymbols.Exit();
                break;
            case NodeType.Actuals:
                node.Type = CbType.Void;
                // visit all parameters
                for (int i = 0; i < children; i++)
                {
                    node[i].Accept(this);
                    if (node[i].Type == CbType.Error)
                        node.Type = CbType.Error;
                }
                break;
            case NodeType.Return:
                node.Type = CbType.Void;
                if (node.NumChildren > 0)
                {
                    node[0].Accept(this);
                    node.Type = node[0].Type;
                }
                break;
            default:
                throw new Exception("{0} is not a tag compatible with an AST_kary node");
        }
    }

    public CbMethod getMethod(String mname, AST args, bool lookup)
    {
        // get method from dictionary
        List<CbMethod> methods;

        if (mname == null || !(Methods.TryGetValue(mname, out methods)))
        {
            if (lookup)
                ReportError(args.LineNumber, "Invalid method: '{0}'", mname);
            return null;
        }

        int desiredArgs = args.NumChildren;
        // compare parameters
        foreach (CbMethod method in methods)
        {
            int numArgs = method.ArgType.Count;
            if (numArgs != desiredArgs)
            {
                continue;
            }
            else
            {
                bool matches = true;
                // compare argument types
                for (int i = 0; i < numArgs; i++)
                {
                    if (method.ArgType[i] != args[i].Type)
                        matches = false;
                }
                if (matches)
                    return method;
            }
        }

        // report error if function is called with the lookup flag
        if (lookup)
            ReportError(args.LineNumber, "Invalid method or method parameter(s): '{0}'", mname);

        return null;
    }

	public override void Visit( AST_nonleaf node ) {
        int children = node.NumChildren;
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
                // get type of value
                node[2].Accept(this);
                CbType typact = node[2].Type;

                // look up declared type (can only be int or string)
                CbType typdec = lookUpIdenType(node[0]);

                if (typdec != CbType.Int && typdec != CbType.String)
                    ReportError(node[0].LineNumber, "Constants can only be of type int or string");
                else if (typact != CbType.Int && typact != CbType.String)
                    ReportError(node[0].LineNumber, "Constants must be set to a number or string constant");
                else if (typdec != typact)
                    ReportError(node[0].LineNumber, "Mismatched constant type declaration ({0} = {1})", typdec, typact);

                break;
            case NodeType.Struct:
                // Nothing to do ... this has been handled by the prePass method below
                break;
            case NodeType.Method:
                // We have to typecheck the method
                localSymbols.Empty();  // clear the symbol table
                node[2].Accept(this);
                string name = ((AST_leaf)(node[1])).Sval;

                CbMethod meth = getMethod(name, node[2], false);
                if (meth == null)
                {
                    ReportError(node[0].LineNumber, "Unknown method encountered: ({0})", name);
                }
                else
                {
                    // 1. initialize the symbol table with the formal parameters
                    CbType rettyp = meth.ResultType;
                    AST formals = node[2];
                    for (int i = 0; i < formals.NumChildren; i++)
                    {
                        SymTabEntry ste = localSymbols.Binding(((AST_leaf)formals[i][1]).Sval, node.LineNumber);
                        ste.Type = lookUpType(formals[i][0]);
                    }
                    // 2. visit the body of the method, type checking it
                    AST block = node[3];
                    block.Accept(this);
                    // check that return statements match return type of method
                    for (int i = 0; i < block.NumChildren; i++)
                    {
                        if (block[i].Tag == NodeType.Return && block[i].Type != rettyp)
                            ReportError(block[i].LineNumber, "Return statement type ({0}) doesn't match method return type ({1})", block[i].Type, rettyp);
                    }
                }
                break;
            case NodeType.FieldDecl:
                // Nothing to do ... this has been handled by the prePass method below
                break;
            case NodeType.Formal:
                node.Type = lookUpType(node[0]);
                // Nothing else to do ... this has been handled by the prePass method below
                break;
            case NodeType.Array:
                node[0].Accept(this);
                break;
            case NodeType.LocalDecl:
                CbType typ = lookUpType(node[0]);
                // add identifiers to symbol table
                AST idList = node[1];
                for (int i = 0; i < idList.NumChildren; i++)
                {
                    // check for duplicates
                    if (localSymbols.Lookup(((AST_leaf)idList[i]).Sval) != null)
                        ReportError(node[0].LineNumber, "Duplicate variable declaration: {0}", ((AST_leaf)idList[i]).Sval);
                    else
                        localSymbols.Binding(((AST_leaf)idList[i]).Sval, node.LineNumber).Type = typ;
                }
                break;
            case NodeType.Assign:
                // check left side
                node[0].Accept(this);
                // check right side
                node[1].Accept(this);

                // check that right and left hand sides have the same type
                if (node[0].Type != node[1].Type && node[0].Type != CbType.Error && node[1].Type != CbType.Error)
                    ReportError(node[0].LineNumber, "Cannot convert from type {1} to {0}", node[0].Type, node[1].Type);

                node.Type = node[0].Type;
                break;
            case NodeType.Call:
                node.Type = CbType.Error;
                // check parameters
                node[1].Accept(this);

                // find calling method name                
                string mname = null;
                // check for cbio.write
                if (node[0].Tag == NodeType.Dot)
                {
                    if (node[0][0].Tag != NodeType.Ident || node[0][1].Tag != NodeType.Ident)
                        ReportError(node[0].LineNumber, "Invalid method call");
                    else
                        mname = ((AST_leaf)node[0][0]).Sval + "." + ((AST_leaf)node[0][1]).Sval;
                }
                else
                    mname = ((AST_leaf)node[0]).Sval;

                if (mname != null && node[1].Type != CbType.Error)
                {
                    // check for valid method call
                    CbMethod method = getMethod(mname, node[1], true);
                    if (method != null)
                        node.Type = method.ResultType;
                }
                break;
            case NodeType.PlusPlus:
                basicTypeCheck(node, CbType.Int, null);
                // no type declaration
                break;
            case NodeType.MinusMinus:
                basicTypeCheck(node, CbType.Int, null);
                // no type declaration
                break;
            case NodeType.If:
                // check boolean parameter
                node[0].Accept(this);

                if(node[0].Type != CbType.Bool)
                    ReportError(node[0].LineNumber, "Invalid boolean expression for if statement");

                // now check the do statement
                node[1].Accept(this);
                // now check for the else statement
                node[2].Accept(this);

                // no type declaration
                break;
            case NodeType.While:
                // check boolean parameter
                node[0].Accept(this);
                // now check the do statement
                node[1].Accept(this);

                if (node[0].Type != CbType.Bool)
                    ReportError(node[0].LineNumber, "Invalid boolean expression for while statement");

                // no type declaration
                break;
            case NodeType.Read:
                // The two children should be the method 'cbio.read' and the variable v
                // don't visit LHS, just check that call is to cbio.read and type of parameter is int
                node[1].Accept(this);
                if (node[0].Tag != NodeType.Dot || node[1].Type != CbType.Int || node[0][0].Tag != NodeType.Ident || node[0][1].Tag != NodeType.Ident
                    || ((AST_leaf)node[0][0]).Sval != "cbio" || ((AST_leaf)node[0][1]).Sval != "read")
                    ReportError(node[0].LineNumber, "Invalid read method call");
                break;
            case NodeType.Add:
                basicTypeCheck(node, CbType.Int, CbType.Int);
                break;
            case NodeType.Sub:
                basicTypeCheck(node, CbType.Int, CbType.Int);
                break;
            case NodeType.Mul:
                basicTypeCheck(node, CbType.Int, CbType.Int);
                break;
            case NodeType.Div:
                basicTypeCheck(node, CbType.Int, CbType.Int);
                break;
            case NodeType.Mod:
                basicTypeCheck(node, CbType.Int, CbType.Int);
                break;
            case NodeType.And:
                basicTypeCheck(node, CbType.Bool, CbType.Bool);
                break;
            case NodeType.Or:
                basicTypeCheck(node, CbType.Bool, CbType.Bool);
                break;
            case NodeType.Equals:
                basicTypeCheck(node, CbType.Int, CbType.Bool);
                break;
            case NodeType.NotEquals:
                basicTypeCheck(node, CbType.Int, CbType.Bool);
                break;
            case NodeType.LessThan:
                basicTypeCheck(node, CbType.Int, CbType.Bool);
                break;
            case NodeType.GreaterThan:
                basicTypeCheck(node, CbType.Int, CbType.Bool);
                break;
            case NodeType.LessOrEqual:
                basicTypeCheck(node, CbType.Int, CbType.Bool);
                break;
            case NodeType.GreaterOrEqual:
                basicTypeCheck(node, CbType.Int, CbType.Bool);
                break;
            case NodeType.UnaryMinus:
                basicTypeCheck(node, CbType.Int, CbType.Int);
                break;
            case NodeType.Dot:
                // visit left hand side
                node[0].Accept(this);

                string rhs = ((AST_leaf)node[1]).Sval;
                node.Type = CbType.Error;
                // semantic check for string.Length or arr.Length
                if (node[0].Type == CbType.String || node[0].Type is CbArray)
                {
                    if (rhs == "Length")
                        node.Type = CbType.Int;
                    else
                        ReportError(node[0].LineNumber, "Invalid usage of .Length");
                }
                else if (node[0].Type != CbType.Error)
                {
                    if (node[0].Type is CbStruct)
                    {
                        // check fields of struct
                        string structname = ((CbStruct)node[0].Type).Name;
                        IDictionary<string, CbField> fields = ((CbStruct)node[0].Type).Fields;
                        CbField field;
                        if (fields.TryGetValue(rhs, out field))
                            node.Type = field.Type;
                        else
                            ReportError(node[0].LineNumber, "Invalid field {0} of struct {1}", rhs, structname);
                    }
                    else
                        ReportError(node[0].LineNumber, "Invalid usage of dot on type {0}", node[0].Type);
                }
                break;
            case NodeType.NewStruct:
                // look up type
                node.Type = lookUpType(node[0]);
                break;
            case NodeType.NewArray:
                // visit array size
                node[1].Accept(this);

                node.Type = CbType.Error;
                // check array type
                if (node[1].Type != CbType.Int && node[1].Type != CbType.Error)
                {
                    ReportError(node[1].LineNumber, "Array size must be of type int");
                }
                else if (node[1].Type != CbType.Error)
                {
                    // declare type
                    node.Type = CbType.Array(lookUpType(node[0]));

                    // check for invalid type
                    if (node.Type == CbType.Error)
                        ReportError(node[0].LineNumber, "Invalid array type {0}", ((AST_leaf)node[0]).Sval);
                 }
                break;
            case NodeType.Index:
                // visit LHS
                node[0].Accept(this);
                // visit RHS
                node[1].Accept(this);

                // perform error checking
                node.Type = CbType.Error;
                if (node[1].Type != CbType.Int && node[1].Type != CbType.Error)
                    ReportError(node[0].LineNumber, "Array index type must be int, not {0}", node[1].Type);
                else if (!(node[0].Type is CbArray) && node[0].Type != CbType.Error)
                    ReportError(node[0].LineNumber, "Array indexing used on non-array type {0}", node[0].Type);
                else if (node[0].Type != CbType.Error && node[1].Type != CbType.Error)
                    node.Type = ((CbArray)node[0].Type).ElementType;
                break;
            default:
                throw new Exception("{0} is not a tag compatible with an AST_nonleaf node");
        }
    }

	public override void Visit(AST_leaf node) {
        switch(node.Tag) {
            case NodeType.Empty:
                // no type
                break;
            case NodeType.Break:
                // no type
                break;
            case NodeType.IntConst:
                node.Type = CbType.Int;
                break;
            case NodeType.StringConst:
                node.Type = CbType.String;
                break;
            case NodeType.Ident:
                CbType typ;
                string str = node.Sval;
                // check for null value
                if (str == "null")
                    typ = CbType.Null;
                else
                {
                    // check symbol table for identifer
                    SymTabEntry result = localSymbols.Lookup(str);
                    if (result != null)
                        typ = result.Type;
                    // if identifier not in symbol table, check in Consts, then if not found report error
                    else if (!(Consts.TryGetValue(str, out typ)))
                    {
                        ReportError(node.LineNumber, "Use of undeclared identifier: '{0}'", str);
                        // add undeclared variable to the symbol table to prevent additional errors
                        localSymbols.Binding(str, node.LineNumber);
                        typ = CbType.Error;
                    }
                }
                node.Type = typ;

                break;
            default:
                throw new Exception("{0} is not a tag compatible with an AST_leaf node");
        }
    }

    // adds all predeclared identifiers and methods
    private void addPredeclared()
    {
        // cbio
        // void read( out int val )
        string name = "cbio.read";
        CbType typm = CbType.Void;
        CbMethod method = new CbMethod(name, typm);
        method.ArgType.Add(CbType.Int);
        List<CbMethod> read = new List<CbMethod>();
        read.Add(method);
        // void write( int val )
        name = "cbio.write";
        method = new CbMethod(name, typm);
        method.ArgType.Add(CbType.Int);
        List<CbMethod> writes = new List<CbMethod>();
        writes.Add(method);
        // void write( string val )
        method = new CbMethod(name, typm);
        method.ArgType.Add(CbType.String);
        writes.Add(method);
        Methods.Add(name, writes);
    }

    // Makes two shallow passes over the declarations inside a class to obtain
    // the names and types of all consts, methods and structs declared at the
    // top level; without this info, typechecking cannot begin.
    private void prePass( AST node ) {
        addPredeclared();

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
            string name;
            CbType typ;
            int argsize;

            switch(ch.Tag) {
                case NodeType.Struct:
                    CbStruct str;
                    name = ((AST_leaf)(ch[0])).Sval;

                    // find existing struct
                    Structs.TryGetValue(name, out str);     // do error checking

                    AST fldlst = ch[1];
                    AST fld;
                    AST idlst;
                
                    argsize = fldlst.NumChildren;
                    int idsize;
                    // iterate through the field list
                    for (int j = 0; j < argsize; j++)
                    {
                        // get field declaration
                        fld = fldlst[j];
                        // find declaration type
                        typ = lookUpType(fld[0]);
                        // get list of variables declared
                        idlst = fld[1];
                        idsize = idlst.NumChildren;
                        // add all declared variables to the struct
                        for(int k = 0; k < idsize; k++)
                            str.AddField(((AST_leaf)(idlst[k])).Sval, typ);
                    }
                    break;
                case NodeType.Const:
                    // Add the name and type of this constant to the Consts table
                    name = ((AST_leaf)(ch[1])).Sval;

                    typ = lookUpType(ch[0]);
                    if (Consts.ContainsKey(name))
                        ReportError(ch[0].LineNumber, "Duplicate declaration of const {0}", name);
                    else
                        Consts.Add(name, typ);
                    break;
                case NodeType.Method:
                    name = ((AST_leaf)(ch[1])).Sval;
                    ch[2].Accept(this);
                    // check for duplicate method
                    CbMethod existing = getMethod(name, ch[2], false);
                    if (existing != null)
                    {
                        ReportError(ch[0].LineNumber, "Duplicate declaration of method {0}", name);
                        break;
                    }

                    CbType typm = CbType.Void;
                    // create new CbMethod
                    if(ch[0] != null)
                        typm = lookUpType(ch[0]);
                    CbMethod method = new CbMethod(name, typm);

                    // add argument list (full signature) to CbMethod
                    AST args = ch[2];
                    for (int j = 0; j < args.NumChildren; j++)
                    {
                        method.ArgType.Add(lookUpType(args[j][0]));
                    }
                    // get list of overloaded methods sharing same name
                    List<CbMethod> methods;
                    if (!Methods.TryGetValue(name, out methods)) // if none exist create new list
                        methods = new List<CbMethod>();
                    // add method to list
                    methods.Add(method);
                    // replace old list of methods with this name with new list
                    Methods[name] = methods;
                    break;
                default:
                    throw new Exception("Unexpected node type " + ch.Tag);
            }
        }
    }
}

}
