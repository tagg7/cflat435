// sampleprog.cs

/* nested block comment test /*   */

using CbRuntime;

class P {
	public const int size = 10;

	public struct Table {
		public int[] pos;
		public int[] neg;
	}

	public static void Main( ) {
		Table val;
		int x, i;

		//---------- Initialize val ----------
		val.pos = new int[size];
		val.neg = new int[size];
		i = 0;
		while (i < size) {
			val.pos[i] = 0;
			val.neg[i] = 0;
			i++;
		}

		//---------- Read values ----------
		cbio.read(out x);
		while(-size < x && x < size) {
			if (0 <= x)
				val.pos[x]++;
			else
				val.neg[-x]++;
			cbio.read(out x);
		}

		//--------- Output the results -----
		i = 0;
		while (i < size) {
			cbio.write("Integers "); cbio.write(i); cbio.write(" and ");
			cbio.write(-i); cbio.write(" occurred "); cbio.write(val.pos[i]);
			cbio.write(" and "); cbio.write(val.neg[i]); cbio.write(" times\n");
			i++;
		}
		
		//--------- test the length feature
		cbio.write("Length of val.pos[] = ");
		cbio.write(val.pos.Length);
		cbio.write("\n");
		cbio.write("Length of string 'abc' = ");
		cbio.write("abc".Length);
		cbio.write("\n");
	}
}