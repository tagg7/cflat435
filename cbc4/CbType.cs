/*  CbType.cs

    Classes and types to describe the datatypes used in a CFlat programs
    
    Author: Nigel Horspool
    
    Date: Oct 2012
*/

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {


public enum CbBasicType {
    Void, Int, Bool, String, Null, Error
}

public abstract class CbType {
    static CbType vt = new CbBasic(CbBasicType.Void);
    static CbType it = new CbBasic(CbBasicType.Int);
    static CbType bt = new CbBasic(CbBasicType.Bool);
    static CbType st = new CbBasic(CbBasicType.String);
    static CbType nt = new CbBasic(CbBasicType.Null);
    static CbType et = new CbBasic(CbBasicType.Error);
    static IDictionary<CbType,CbType> arrayTypes = new Dictionary<CbType,CbType>();

    // Properties which return unique descriptions of the basic types
    public static CbType Void{ get{ return vt; } }
    public static CbType Int{ get{ return it; } }
    public static CbType Bool{ get{ return bt; } }
    public static CbType String{ get{ return st; } }
    public static CbType Null{ get{ return nt; } }
    public static CbType Error{ get{ return et; } }

    // Static method which returns a unique descriptor for an array type
    public static CbType Array( CbType elt ) {
        if (arrayTypes.ContainsKey(elt))
            return arrayTypes[elt];
        return (arrayTypes[elt] = new CbArray(elt));
    }
}

public class CbArray: CbType {
    public CbType ElementType{ get; set; }

    // Do not call directly -- use CbType.Array(elt) instead
    public CbArray( CbType elt ) {
        ElementType = elt;
    }

    public override string ToString() {
        return System.String.Format("{0}[]", ElementType);
    }
}

// a CFlat struct has members which are fields
public class CbStruct: CbType {
    public IDictionary<string, CbField> Fields { get; private set; }
    public string Name{ get; set; }

    public CbStruct( string name ) {
        Name = name;
        Fields = new Dictionary<string,CbField>();
    }
    
    public override string ToString() {
        return string.Format("struct {0}", Name);
    }

    public bool AddField( string name, CbType type ) {
        if (Fields.ContainsKey(name))
            return false;  // error -- duplicate field name
        Fields[name] = new CbField(name, type, this);
        return true;  // success
    }

    // output a complete listing of all the fields in the struct
    public void Print(TextWriter p) {
        p.Write("struct {0}", Name);
        p.WriteLine(" {");
        
        // output the fields
        foreach( CbField cf in Fields.Values ) {
            p.WriteLine("    {0}", cf);
        }

        p.WriteLine("}\n");
    }
}


public class CbBasic: CbType {
    public CbBasicType Type{ get; private set; }

    public CbBasic( CbBasicType t ) {
        Type = t;
    }

    public override string ToString() {
        return Type.ToString().ToLower();
    }
}


public class CbField {
    public CbType   Type{ get; private set; }  // the type of this field
    public CbStruct Owner{ get; private set;}  // the struct which owns this field
    public string Name{ get; private set; }

    // No need to invoke this constructor ... use the AddField method
    // of CbStruct instead
    public CbField( string nm, CbType t, CbStruct owner ) {
        Name = nm;  Type = t;  Owner = owner;
    }
    
    public override string ToString() {
        return string.Format("{0}:{1}", Name, Type);
    }
}

// Because CFlat does not have delegates or anonymous functions, we can safely
// keep the name of the method inside the CbMethod instance. (In an ideal
// world, the method name is separate from the method's datatype.)
public class CbMethod: CbType {
    public CbType ResultType{ get; private set; }
    public IList<CbType> ArgType{ get; private set; }
    public string Name { get; private set; }
    
    public CbMethod( string nm, CbType rt ) {
        Name = nm;
        ResultType = rt;
        ArgType = new List<CbType>();
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("{0} {1}(", ResultType, Name);
        string s = "";
        foreach( CbType argt in ArgType ) {
            sb.AppendFormat("{0}{1}", s, argt.ToString());
            s = ",";
        }
        sb.Append(")");
        return sb.ToString();
    }
}

} // end of namespace FrontEnd

