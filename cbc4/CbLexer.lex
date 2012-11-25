/*  CbLexer.lex

    Author: Nigel Horspool

    Date:   October 2012
*/
/*   
%token     
*/

%namespace  FrontEnd
%option     out:CbLexer.cs

%{
    public int lineNum = 1;

    public int LineNumber { get{ return lineNum; } }

    public override void yyerror( string msg, params object[] args ) {
        Console.WriteLine("{0}: ", lineNum);
        if (args == null || args.Length == 0) {
            Console.WriteLine("{0}", msg);
        } else {
            Console.WriteLine(msg, args);
        }
    }

    public void yyerror( int lineNum, string msg, params object[] args ) {
        Console.WriteLine("{0}: {1}", msg, args);
    }
    
    public Tokens kwdLookup( string s ) {
    	// maybe this should be implemented using a Dictionary<string><Tokens>
        switch(s) {
        case "new": return Tokens.Kwd_new;
        case "public": return Tokens.Kwd_public;
        case "static": return Tokens.Kwd_static;
        case "class": return Tokens.Kwd_class;
        case "void": return Tokens.Kwd_void;
        case "if": return Tokens.Kwd_if;
        case "else": return Tokens.Kwd_else;
        case "while": return Tokens.Kwd_while;
        case "break": return Tokens.Kwd_break;
        case "return": return Tokens.Kwd_return;
		case "const": return Tokens.Kwd_const;
		case "out": return Tokens.Kwd_out;
		case "struct": return Tokens.Kwd_struct;
		case "using": return Tokens.Kwd_using;
        }
        return Tokens.Ident;
    }
    
    private int commentNesting = 0;  // nesting level of block comments
%}

%x COMMENT

letter          [a-zA-Z]
digit           [0-9]
idChar          [a-zA-Z0-9_]
schar           ([0-9a-zA-Z ~!@#$%^&*()_\-+={[}\]|:;'<,>\.?/]|\\[rnt\\"])

%%

\r\n                        { lineNum++; }  // Windows end-of-line combination
[\r\n]                      { lineNum++; }  // Mac or Unix end-of-line

[ \t]+                      { }  // white-space characters
"//".*						{ }  // comment

"/*"                        { commentNesting = 1; BEGIN(COMMENT); }

"++"						{ return (int)Tokens.PLUSPLUS; }
"--"						{ return (int)Tokens.MINUSMINUS; }
"<="                        { return (int)Tokens.LTEQ; }
">="                        { return (int)Tokens.GTEQ; }
"=="                        { return (int)Tokens.EQEQ; }
"!="                        { return (int)Tokens.NOTEQ; }
"&&"						{ return (int)Tokens.ANDAND; }
"||"						{ return (int)Tokens.OROR; }

[+\-*/%.,;:\[\]()={}<>!]    { return (int)yytext[0]; }
{letter}{idChar}*           { return (int)kwdLookup(yytext); }
{digit}+                    { return (int)Tokens.Number; }
\"{schar}*\"                { return (int)Tokens.StringConst; }

.                           { yyerror("illegal character ({0})", yytext); }

/* handle multi-line nested block comments */
<COMMENT>"*/"               { if (--commentNesting <= 0) { BEGIN(INITIAL); }  }
<COMMENT>"/*"               { commentNesting++; }
<COMMENT>\r\n|[\r\n]		{ lineNum++; }
<COMMENT>.                  {  }

%%

