using System.IO;
using System.Collections.Generic;

namespace LexScanner
{
    public class CbParser : Parser
    {
        public static string filename;
        public static bool outputTokens;
        public static bool debugMode;
        Stack<string> id_stack = new Stack<string>();

        void push_id()
        {
            string t = ((LexScanner.Scanner)Scanner).last_token_text;
            id_stack.Push(t);
        }
        string pop_id()
        {
            return id_stack.Pop();
        }

        string token_text()
        {
            return ((LexScanner.Scanner)Scanner).last_token_text;
        }

        void writeln()
        {
            writeln(null, null);
        }
        void writeln(string opcode)
        {
            writeln(opcode, null);
        }

        void writeln(string opcode, string value)
        {
            if (opcode != null)
            {
                System.Console.Write(opcode);
                if (value != null)
                {
                    System.Console.Write(' ' + value);
                }
            }
            System.Console.Write('\n');
        }

        static void Main(string[] args)
        {
            foreach (string arg in args) {
                switch (arg)
                {
                    case "-tokens":
                        outputTokens = true;
                        break;
                    case "-debug":
                        debugMode = true;
                        break;
                    default:
                        filename = arg;
                        break;
                }
            }
            if (filename != null)
            {
                CbParser parser = new CbParser();

                try
                {
                    FileStream file = new FileStream(filename, FileMode.Open);
                    LexScanner.Scanner scanner = new LexScanner.Scanner(file);
                    scanner.filename = filename;
                    scanner.tokens = outputTokens;
                    if (outputTokens)
                        scanner.openFile();
                    parser.Scanner = scanner;

                    parser.Parse();
                }
                catch (System.Exception e)
                {
                    System.Console.WriteLine("Error encountered while attempting to read " + filename);
                    if (debugMode)
                    {
                        System.Console.WriteLine(e);
                    }
                }
            }
            else
            {
                System.Console.WriteLine("Usage: cbc [OPTION]... [FILE]");
                System.Console.WriteLine("Compiles Cb file FILE.");
                System.Console.WriteLine("  -tokens             output tokens to tokens.txt");
                System.Console.WriteLine("  -debug              display debug messages");
            }
        }
    }
}
