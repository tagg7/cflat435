// CbRuntime.cs

using System;
using System.IO;

namespace CbRuntime {

public class cbio {
	static char lastch = ' ';  // the last character read
	static bool lastOutWasDigit = false;
	static TextReader inp = System.Console.In;
	static TextWriter outp = System.Console.Out;

	private static void read() {
		int k = inp.Read();
		if (k < 0) // EOF
			throw new IOException("attempt to read past EOF");
		lastch = (char)k;
	}

	public static void read( out int val ) {
		while(Char.IsWhiteSpace(lastch))  // skip intial white space
			read();
		bool neg = false;
		if (lastch == '-' || lastch == '+') {
			if (lastch == '-') neg = true;
			read();
		}
		int result = 0;
		int numdigits = 0;
		while(Char.IsDigit(lastch)) {
			result = result*10 + ((int)lastch - (int)'0');
			if (result < 0)
				throw new IOException("overflow while inputting decimal integer");
			read();
			numdigits++;
		}
		if (numdigits == 0)
			throw new IOException("malformed decimal integer on input");
		val = neg? -result : result;
	}

	public static void write( int val ) {
		if (lastOutWasDigit)
			outp.Write(' ');  // provide a separating blank between numbers
		outp.Write(val);
		lastOutWasDigit = true;
	}

	public static void write( string val ) {
		outp.Write(val);
		lastOutWasDigit = false;
	}
}

} // end of namespace CbRuntime