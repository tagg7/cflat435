// cbtest.cs

using CbRuntime;

class CbTest
{
    public const int max_size = 10;

    public struct Prime
    {
        public int[] yes;
        public int[] no;
    }

    public static void Main()
    {
        //--------- Prime Number test ---------
        // initialize values and struct
        Prime list;
        int a, b, i, j, t, u;

        list.yes = new int[max_size];
        list.no = new int[max_size];
        i = 0;
        j = 0;

        cbio.write("Enter positive integers. Enter a negative number to terminate input.\n");

        // read and sort prime values
        cbio.read(out a);
        while (a > 0)
        {
            b = a;
            u = a - 1;
            t = 0;
            while (b != 0 && u > 1)
            {
                t = b % u;
                if (t == 0)
                    break;
                u = u - 1;
            }
            if (t == 0)
                list.no[i++] = a;
            else
                list.yes[j++] = a;

            cbio.read(out a);
        }

        // output sorted prime numbers
        t = 0;
        cbio.write("Prime numbers:");
        while (t < j)
        {
            cbio.write(" ");
            cbio.write(list.yes[t]);
            t++;
        }

        // output sorted nonprime numbers
        t = 0;
        cbio.write("\nNonprime numbers: ");
        while (t < i)
        {
            cbio.write(" ");
            cbio.write(list.no[t]);
            t++;
        }

        //--------- Greatest Common Denominator test ---------
        /*
        int a, b, t, a_temp, b_temp, gcd;

        cbio.write("Find the gcd of two numbers. Enter numbers: ");
        cbio.read(out a);
        cbio.read(out b);

        a_temp = a;
        b_temp = b;

        // compute the gcd
        while (b != 0)
        {
            t = b;
            b = a % b;
            a = t;
        }
        gcd = a;

        cbio.write("The gcd of "); cbio.write(a_temp); cbio.write(" and "); cbio.write(b_temp); 
        cbio.write(" is: "); cbio.write(gcd);
        */
    }
}