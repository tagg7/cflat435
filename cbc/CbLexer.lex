%namespace LexScanner
%tokentype Tokens

%{
  public int lineNum = 1;

  public int LineNumber { get{ return lineNum; } }

  public override void yyerror( string msg, params object[] args ) {
    Console.WriteLine("{0}: ", lineNum);
    if (args == null || args.Length == 0) {
      Console.WriteLine("{0}", msg);
    }
    else {
      Console.WriteLine(msg, args);
    }
  }

  public void yyerror( int lineNum, string msg, params object[] args ) {
    Console.WriteLine("{0}: {1}", msg, args);
  }

%}

space [ \t]
opchar [+\-*/%] // must escape '-' as it signifies a range
newline \r?[\n]

%%
{space}          {}
"=" {return (int)'=';}
"("	{return (int)'(';}
")"	{return (int)')';}
(0|[1-9][0-9]*|0x[0-9a-fA-F]+)    {last_token_text=yytext;return (int)Tokens.Number;}
[a-zA-Z][a-zA-Z0-9_]*            {last_token_text=yytext;return (int)Tokens.Ident;}
{opchar}         {return (int)(yytext[0]);}
{newline}        {return (int)'\n';}

.                { yyerror("illegal character ({0})", yytext); }

%%

public string last_token_text = "";

