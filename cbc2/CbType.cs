/*  CbType.cs

    Classes and types to describe the datatypes used in a CFlat programs
    
    Author: Nigel Horspool
    
    Date: Oct 2012
*/

using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {


public enum CbBasicType {
    Void, Int, Bool, String, Error
}

public abstract class CbType {
    static CbType vt = new CbBasic(CbBasicType.Void);
    static CbType it = new CbBasic(CbBasicType.Int);
    static CbType bt = new CbBasic(CbBasicType.Bool);
    static CbType st = new CbBasic(CbBasicType.String);
    static CbType et = new CbBasic(CbBasicType.Error);
    static IDictionary<CbType,CbType> arrayTypes = new Dictionary<CbType,CbType>();

    // Properties which return unique descriptions of the basic types
    public static CbType Void{ get{ return vt; } }
    public static CbType Int{ get{ return it; } }
    public static CbType Bool{ get{ return bt; } }
    public static CbType String{ get{ return st; } }
    public static CbType Error{ get{ return et; } }

	// Set during semantic checking -- contains a list of all classes
	public static IDictionary<string,CbClass> DeclaredClasses { get; set; }

    // Static method which returns a unique descriptor for an array type
    public static CbType Array( CbType elt ) {
        if (arrayTypes.ContainsKey(elt))
            return arrayTypes[elt];
        return (arrayTypes[elt] = new CFArray(elt));
    }

    public abstract void Print(TextWriter p);

    public static void PrintClasses( TextWriter p ) {
        foreach( CbClass cl in DeclaredClasses.Values ) {
            if (cl.Name == "object")
                    continue;
            cl.Print(p);
        }
    }
}

public class CFArray: CbType {
    public CbType ElementType{ get; set; }

    // Do not call directly -- use CbType.Array(elt) instead
    public CFArray( CbType elt ) {
        ElementType = elt;
    }

    public override string ToString() {
        return System.String.Format("{0}[]", ElementType);
    }
    
    public override void Print(TextWriter p) {
        p.Write(this.ToString());
    }
}

// A class has members which can be fields, a constructor and methods.
// Since CFlat does not have overloading, a dictionary can be used to
// look up the unique member with a particular name.
public class CbClass: CbType {
    public IDictionary<string, CbMember> Members{ get; set; }
    public string Name{ get; set; }
    
    public CbClass( string name ) {
        Name = name;
        Members = new Dictionary<string,CbMember>();
    }

    public override string ToString() {
        return System.String.Format("class {0}", Name);
    }

    public override void Print(TextWriter p) {
        p.Write("class {0}", Name);
        p.WriteLine(" {");
        
        // output the fields
        foreach( CbMember cm in Members.Values ) {
            CbField cf = cm as CbField;
            if (cf == null) continue;
            p.Write("    ");
            cf.Print(p);
        }

        // output the constructors (there should be at most one)
        foreach( CbMember cm in Members.Values ) {
            CbConstructor cc = cm as CbConstructor;
            if (cc == null || cc is CFMethod) continue;
            p.Write("    ");
            cc.Print(p);
        }

        // output the methods
        foreach( CbMember cm in Members.Values ) {
            CFMethod ct = cm as CFMethod;
            if (ct == null) continue;
            p.Write("    ");
            ct.Print(p);
        }

        p.WriteLine("}\n");
    }

}

public class CbBasic: CbType {

    public CbBasicType Type{ get; protected set; }

    public CbBasic( CbBasicType t ) {
        Type = t;
    }

    public override string ToString() {
        return Type.ToString().ToLower();
    }

    public override void Print(TextWriter p) {
        p.WriteLine(this.ToString());
    }
}

// Members of a class can be fields, a constructor, or methods
public abstract class CbMember {
    public String Name{ get; set; }
    public CbClass Owner { get; set; }  // class owning this field

    public abstract void Print(TextWriter p);
}

public class CbField: CbMember {
    public CbType Type{ get; set; }

    public CbField( string nm, CbType t, CbClass owner ) {
        Name = nm;  Type = t;  Owner = owner;
    }

    public override void Print(TextWriter p) {
        p.WriteLine("{0}:{1}", Name, Type);
    }
}

public class CbConstructor: CbMember {
    public IList<CbType> ArgType{ get; set; }

    public CbConstructor( CbClass owner ) {
        Name = owner.Name;
        ArgType = new List<CbType>();
        Owner = owner;
    }

    public override void Print(TextWriter p) {
        p.Write("{0}(", Owner.Name);
        string s = "";
        foreach( CbType at in ArgType ) {
            p.Write("{0}{1}", s, at.ToString());
            s = ",";
        }
        p.WriteLine(")");
    }
}

public class CFMethod: CbConstructor {
    public CbType ResultType{ get; set; }
    
    public CFMethod( string nm, CbClass owner, CbType rt ): base(owner) {
        Name = nm;  ResultType = rt;
    }

    public override void Print(TextWriter p) {
        p.Write("{0} {1}(", ResultType, Name);
        string s = "";
        foreach( CbType at in ArgType ) {
            p.Write("{0}{1}", s, at.ToString());
            s = ",";
        }
        p.WriteLine(")");
    }
}

} // end of namespace FrontEnd

