// ARMAssemblerCode.cs


using System;
using System.Text;
using System.Collections.Generic;
using System.IO;


namespace BackEnd {

public enum CodeType { Op, Label, Comment, Directive }

public class AsmLine {
	public CodeType Kind { get; private set; }
	private string text;
	
	public AsmLine( CodeType kind, string text, params object[] args ) {
		Kind = kind;
		this.text = text;
		Operands = args;
	}
	
	public string OpCode {
		get{
			if (Kind != CodeType.Op || Kind != CodeType.Directive)
				return null;
			return text;
		}
	}

	public string Label {
		get{ return Kind==CodeType.Label? text : null; }
	}
	
	public object[] Operands { get; private set; }
	
	public override string ToString() {
		StringBuilder sb;
		switch(Kind) {
		case CodeType.Op:
		case CodeType.Directive:
			if (Operands == null)
				return "\t"+text;
			sb = new StringBuilder();
			sb.Append("\t");
			sb.Append(text);
			char ch = '\t';
			foreach( object arg in Operands ) {
				sb.Append(ch);
				sb.Append(arg.ToString());
				ch = ',';
			}
			return sb.ToString();
		case CodeType.Label:
			return text+":";
		case CodeType.Comment:
			return "@ "+text;
		}
		return "????";  // should not get here!
	}
}
	
public class ARMAssemblerCode {
	IList<AsmLine> code;

	public ARMAssemblerCode() {
		code = new List<AsmLine>();
	}
	
	public void StartMethod( string name ) {
		AppendComment("");
		AppendComment(string.Format("Method {0}", name));
		AppendDirective(".align", "2");
		AppendLabel(name);
	}
	
	public void EndMethod() {
		AppendDirective(".ltorg");
	}
	
	public void Append( string op, params object[] args ) {
		code.Add( new AsmLine(CodeType.Op, op, args) );
	}

	public void AppendLabel( string lab ) {
		code.Add( new AsmLine(CodeType.Label, lab) );
	}

	public void AppendComment( string text ) {
		code.Add( new AsmLine(CodeType.Comment, text) );
	}

	public void AppendDirective( string dir, params object[] args ) {
		code.Add( new AsmLine(CodeType.Directive, dir, args) );
	}

	public void WriteCode( string path ) {
		using (StreamWriter fs = File.CreateText(path)) {
			fs.WriteLine("@ Created by cbc at {0}", DateTime.Now);
			foreach( AsmLine ln in code )
				fs.WriteLine(ln.ToString());
			fs.WriteLine("\t.end");
		}
	}

}

} // end of namespace BackEnd