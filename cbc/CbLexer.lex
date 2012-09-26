%namespace LexScanner
%tokentype Tokens

%{
  public int lineNum = 1;
  public int commentDepth = 0;

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

letter [a-zA-Z]
digit [0-9]
number {digit}+

ident {letter}[a-zA-Z0-9_]*
printableChar [\x20-\x7e]
stringConst \"({printableChar}|\r|\n)*\"

operator (\+\+|--|\+|-|\*|\/|%|==|!=|\>=|\>|\<=|\<|&&|\|\||=|;|,|\.|\(|\)|\[|\]|\{|\}) 
 /* operator matches any of ++ -- + - * / % == != >= > <= < && || = ; , . ( ) [ ] { } */
 
whitespace [ \t\r\n]+
newline (\r\n?|\n)

linecomment \/\/\.*{newline}?

%x BLOCK_COMMENT

%%

\/\*							{BEGIN(BLOCK_COMMENT);commentDepth++;}
<BLOCK_COMMENT>\/\*				{commentDepth++;}
 /* <BLOCK_COMMENT>\*\/				{commentDepth--; if(commentDepth == 0) BEGIN(INITIAL);} */
<BLOCK_COMMENT>\*\/				{BEGIN(INITIAL);}

{linecomment}					{}
using							{return (int)Tokens.Kwd_using;}
<<EOF>>							{Console.WriteLine("DONE!");}
/* .							{yyerror("illegal character ({0})", yytext);} */


%%

public string last_token_text = "";

