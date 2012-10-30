/* SymTab.cs

   Implements a symbol table, useful for processing the local declarations
   in the body of a method.
*/


/*
NOTE:
The implementation provided here will work ... but it becomes exceedingly
inefficient when the number of symbols grows -- linear search is used.

Also, throwing an exception for a duplicate declaration is too heavy because
that exception will have to be caught by the caller, so that type checking
can continue.
*/

using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {

public class SymTabEntry {
    public int DeclLineNo { get; private set; }  // declared on this line
    public CbType Type{ get; set; }              // declared type

    public string Name{ get; private set; }

    public SymTabEntry( string nm, int ln ) {
        Name = nm;  DeclLineNo = ln;
    }
}

public class SymTabException: Exception {
    public SymTabException( string msg ): base(msg) { }

    public SymTabException( string fmt, params object[] args ):
        base(String.Format(fmt, args)) { } 
}


public class SymTab {
    private IList<SymTabEntry> table;  // a simple list data stucture

    public SymTab() {
        table = new List<SymTabEntry>();
        Empty();
    }

    public void Empty() {
        // resets the symbol table to be empty
        table.Clear();
        table.Add(null);    // scope marker for global scope
    }

    public SymTabEntry Binding( string name, int ln ) {
        // check for duplicate definition -- we search from the
        // end of the list to the scope marker
        int last = table.Count;
        for( ; ; ) {
            last--;
            SymTabEntry syt = table[last];
            if (syt == null) break;  // hit scope marker
            if (syt.Name == name)
                throw new SymTabException(
                    "Duplicate declaration of {0} on line {1}", name, ln);
        }
        // add result to the symbol table
        SymTabEntry result = new SymTabEntry(name,ln);
        table.Add(result);
        return result;
    }

    public SymTabEntry Lookup( string name ) {
        SymTabEntry result = null;
        // Search symbol table for this name -- need the latest occurrence
        foreach( SymTabEntry syt in table ) {
            if (syt != null && syt.Name == name) result = syt;
        }
        return result;
    }

    // Start a new scope
    public void Enter() {
        table.Add(null);  // we use null as the scope marker
    }

    // Exit the most recent scope
    // Also make sure that the number of Exits does not exceed
    // the number of Enters.
    public void Exit() {
        int last = table.Count;
        while(last > 0) {
            last--;
            SymTabEntry syt = table[last];
            table.RemoveAt(last);
            if (syt == null) break; // hit the scope marker
        }
        if (last == 0) // we removed the initial scope marker!
            throw new SymTabException("Mismatched Exit() call");
    }
}


} // end of namespace FrontEnd 
