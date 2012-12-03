// sampleprog.cs

using CbRuntime;

class P {
	public static void Main( ) {
		int x, i;
        string hello;

        hello = "howdy there";

		cbio.write("\\\"");

		//---------- Read values ----------
		cbio.read(out x);
        if (x != 3)
            i = 4;
        else
            i = 275;

		//--------- Output the results -----
		cbio.write("Testing output!");
        cbio.write(i);
        cbio.write(hello);

        break;
	}
}