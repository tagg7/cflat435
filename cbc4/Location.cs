// Location.cs
//
// These classes define how a memory location should be
// accessed in an ARM load or store instruction.
// The supported assembler formats are:
// LocLabel:  for a static memory location in the *text* area
//                  const1
// LocRegOffset: for a memory operand with the formats
//                  [r4,0x484]  or  [r4]
// LocRegIndex: for a memory operand with an index register
//                  [r4,r1]  or  [r4,r1,lsl #2]
//              The scaling for an index is determined by the
//              memory type (Byte or Word).

using System;
using System.Collections.Generic;
using System.IO;


namespace BackEnd {

// MemType specifies what is in the memory location ... 8, 16, 32, 64 bit
// integers or 32, 64 bit floating point numbers.
// Single and Double are the floating-point types -- they are defined
// here only for completeness.
// The HWord (halfword) and DWord (doubleword) integer types should
// not be needed for CFlat programs either. 
public enum MemType { Byte, HWord, Word, DWord, Single, Double }

public abstract class Loc {
	public MemType Type { get; private set; }
	
	public Loc( MemType t ) { Type = t; }

	public static string RegisterName( int num ) {
		switch(num) {
		case 12: return "fp";
		case 13: return "sp";
		case 14: return "lr";
		case 15: return "pc";
		}
		if (num < 0 || num > 15)
			throw new Exception("invalid register number");
		return "r"+num;
	}
}

public class LocLabel: Loc {
	public string Label { get; private set; }
	
	public LocLabel( string name, MemType t ): base(t) {
		Label = name;
	}
	
	public override string ToString() {
		return Label;
	}
}

public class LocRegOffset: Loc {
	public int Reg { get; private set; }
	public int Offset { get; private set; }
	
	public LocRegOffset( int reg, int off, MemType t ): base(t) {
		Reg = reg;
		Offset = off;
	}
	
	public override string ToString() {
		if (Offset == 0)
            return string.Format("[r{0}]", RegisterName(Reg));
		// should check that the offset is in range!
		return string.Format("[{0},#{1}]", RegisterName(Reg), Offset);
	}
}

public class LocRegIndex: Loc {
	public int Reg { get; private set; }
	public int IndexReg { get; private set; }
	
	public LocRegIndex( int reg, int index, MemType t ): base(t) {
		Reg = reg;
		IndexReg = index;
	}
	
	public override string ToString() {
		string fmt = null;
		switch( Type ) {
		case MemType.Byte:	  fmt = "[{0},{1}]";         break;
		case MemType.HWord:   fmt = "[{0},{1},lsl #1]";  break;
		case MemType.Word:
		case MemType.Single:  fmt = "[{0},{1},lsl #2]";  break;
		case MemType.DWord:
		case MemType.Double:  fmt = "[{0},{1};lsl #3]";  break;
		}
		return string.Format(fmt, RegisterName(Reg), RegisterName(IndexReg));
	}
}


} // end of namespace BackEnd