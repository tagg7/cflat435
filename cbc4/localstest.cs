// sampleprog.cs

using CbRuntime;

class P
{
    public const int size = 10;
    public const string message = "greetings";

    public struct Point
    {
        public int x;
        public int y;
        public string[] labels;
    }

    public struct Rectangle
    {
        public int width;
        public int height;
        public Point topleft, bottomright;
    }

    public static int add(int a1, int a2)
    {
        int sum;
        sum = a1 + a2;
        return sum;
    }

    public static Point add2(Point p1, Point p2)
    {
        Point p3;
        p3.x = p1.x + p2.x;
        p3.y = p1.y + p2.y;
        return p3;
    }
    
	public static void Main()
    {
        int i;
        i = 0;

        string[] ss;
        ss = new string[size];
        while (i < size)
        {
            ss[i] = "Z";
            i = i + 1;
        }

        int x, y, z;
        x = 2;
        y = 13;
        z = add(x, y);

        Point a;
        a.x = 1;
        a.y = 2;

        Point b;
        b.x = 5;
        b.y = a.y;

        Point c;
        c = add2(a, b);
	}
}