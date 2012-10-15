/*  CbAST.cs

    Defines an Abstract Syntax Tree class for CFlat programs
    
    Author: Nigel Horspool
    
    Date: Oct 2012
*/

using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {

public enum NodeType {
    Program, UsingList,
    Class, DeclList, Const, Struct, Method, FieldList, FieldDecl,
    IdList, Formals, Formal, Array, Block, LocalDecl, Assign, Call,
    Actuals, PlusPlus, MinusMinus, If, While, Break,
    Return, Read, Empty,
    Add, Sub, Mul, Div, Mod, And, Or,
    Equals, NotEquals, LessThan, GreaterThan, LessOrEqual, GreaterOrEqual,
    UnaryMinus, Dot, NewStruct, NewArray, Index,
    IntConst, StringConst, Ident
};

public abstract class AST {

    public AST( NodeType tag, int ln ) {
        this.Tag = tag;
        LineNumber = ln;
    }

    // The datatype of the subtree -- note: statements and other constructs
    // without values will leave the value of Type as null
    public CbType Type { get; set; }

    public int LineNumber {  get; protected set; }

    public NodeType Tag { get; protected set; }

    public virtual int NumChildren { get{ return 0; } }

    public virtual void AddChild( AST ch ) {
        throw new Exception("AddChild only supported for k-ary nodes");
    }
 
    public virtual AST this[ int ix ] {
        get{ throw new Exception("get property unimplemented"); }
        set{ throw new Exception("set property unimplemented"); }
    }

	public virtual void Accept( Visitor v ) { }

    static public AST_kary Kary( NodeType tag, int ln, params AST[] firstChildren ) {
        return new AST_kary(tag, ln, firstChildren);
    }

    static public AST_leaf Leaf( NodeType tag, int ln ) {
        return new AST_leaf(tag, ln);
    }

    static public AST_leaf Leaf( NodeType tag, int ln, string s ) {
        return new AST_leaf(tag, ln, s);
    }

    static public AST_leaf Leaf( NodeType tag, int ln, int i ) {
        return new AST_leaf(tag, ln, i);
    }

    static public AST_nonleaf NonLeaf( NodeType tag, int ln, params AST[] ch ) {
        return new AST_nonleaf(tag, ln, ch);
    }
    
}


public class AST_kary : AST {
    IList<AST> children = new List<AST>();

    public AST_kary( NodeType tag, int ln, params AST[] firstChildren ) :
                base(tag,ln) {
        foreach(AST ch in firstChildren) AddChild(ch);
    }

    public override void AddChild( AST newchild ) {
        children.Add(newchild);
    }

    public override int NumChildren { get{ return children.Count; } }

    public override AST this[ int ix ] {
        get{ return children[ix]; }
        set{ children[ix] = value; }
    }

	public override void Accept( Visitor v ) {
		v.Visit(this);
	}
}

public class AST_leaf : AST {

    public string Sval { get; protected set; }
    public int    Ival { get; protected set; }

    // constructor when no associated lexical value (e.g. null token)
    public AST_leaf( NodeType tag, int ln ) : base(tag,ln) {
    	Sval = null; Ival = 0;
    }
    
    // constructor when lexical value is a string (e.g. identifier)
    public AST_leaf( NodeType tag, int ln, string s ) : base(tag,ln) {
        Sval = s;  Ival = 0;
    }

    // constructor when lexical value is an int (e.g. int constant)
    public AST_leaf( NodeType tag, int ln, int i ) : base(tag,ln) {
        Ival = i;  Sval = null;
    }

	public override void Accept( Visitor v ) {
		v.Visit(this);
	}
}


public class AST_nonleaf : AST {
    AST[] children;

    // constructor for any number of children
    public AST_nonleaf( NodeType tag, int ln,
            params AST[] children ) : base(tag,ln) {
        this.children = children;
    }

    public override int NumChildren { get{ return children.Length; } }

    public override AST this[ int ix ] {
        get{ return children[ix]; }
        set{ children[ix] = value; }
    }

	public override void Accept( Visitor v ) {
		v.Visit(this);
	}
}

} // of namespace


