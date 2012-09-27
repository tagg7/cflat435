%namespace LexScanner
%tokentype Tokens

%{
  public int lineNum = 1;
  public bool errors = false;
  public bool tokens = false;
  public string filename = null;
  System.IO.StreamWriter file;

  public int LineNumber { get{ return lineNum; } }

  public override void yyerror( string msg, params object[] args ) {
    Console.Write("{0} {1}: ", filename, lineNum);
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

  public void openFile()  {
    file = new System.IO.StreamWriter("tokens.txt");
  }

%}

number [0-9]+
ident [a-zA-Z][a-zA-Z0-9_]*
stringConst \"(\\[ !\x20-\x7e]|[ !\x23-\x5b\x5d-\x7e])*\"

space [ \t]+
newline (\r\n?|\n)

linecomment "//".*

%x BLOCK_COMMENT

%%

"/*"								{BEGIN(BLOCK_COMMENT);}
<BLOCK_COMMENT>"*/"					{BEGIN(INITIAL);}
<BLOCK_COMMENT, INITIAL>{newline}	{lineNum++;}
{linecomment}						{}
{space}								{}
"+"									{if(tokens) printTokenChar("+"); return (int)'+';}
"-"									{if(tokens) printTokenChar("-"); return (int)'-';}
"*"									{if(tokens) printTokenChar("*"); return (int)'*';}
"/"									{if(tokens) printTokenChar("/"); return (int)'/';}
"%"									{if(tokens) printTokenChar("%"); return (int)'%';}
">"									{if(tokens) printTokenChar(">"); return (int)'>';}
"<"									{if(tokens) printTokenChar("<"); return (int)'<';}
"="									{if(tokens) printTokenChar("="); return (int)'=';}
";"									{if(tokens) printTokenChar(";"); return (int)';';}
","									{if(tokens) printTokenChar(","); return (int)',';}
"."									{if(tokens) printTokenChar("."); return (int)'.';}
"("									{if(tokens) printTokenChar("("); return (int)'(';}
")"									{if(tokens) printTokenChar(")"); return (int)')';}
"["									{if(tokens) printTokenChar("["); return (int)'[';}
"]"									{if(tokens) printTokenChar("]"); return (int)']';}
"{"									{if(tokens) printTokenChar("{"); return (int)'{';}
"}"									{if(tokens) printTokenChar("}"); return (int)'}';}
"++"								{if(tokens) printToken("PLUSPLUS"); return (int)Tokens.PLUSPLUS;}
"--"								{if(tokens) printToken("MINUSMINUS"); return (int)Tokens.MINUSMINUS;}
"=="								{if(tokens) printToken("EQEQ"); return (int)Tokens.EQEQ;}
"!="								{if(tokens) printToken("NOTEQ"); return (int)Tokens.NOTEQ;}
">="								{if(tokens) printToken("GTEQ"); return (int)Tokens.GTEQ;}
"<="								{if(tokens) printToken("LTEQ"); return (int)Tokens.LTEQ;}
"&&"								{if(tokens) printToken("ANDAND"); return (int)Tokens.ANDAND;}
"||"								{if(tokens) printToken("OROR"); return (int)Tokens.OROR;}
break								{if(tokens) printToken("Kwd_break"); return (int)Tokens.Kwd_break;}
class								{if(tokens) printToken("Kwd_class"); return (int)Tokens.Kwd_class;}
const								{if(tokens) printToken("Kwd_const"); return (int)Tokens.Kwd_const;}
else								{if(tokens) printToken("Kwd_else"); return (int)Tokens.Kwd_else;}
if									{if(tokens) printToken("Kwd_if"); return (int)Tokens.Kwd_if;}
new									{if(tokens) printToken("Kwd_new"); return (int)Tokens.Kwd_new;}
out									{if(tokens) printToken("Kwd_out"); return (int)Tokens.Kwd_out;}
public								{if(tokens) printToken("Kwd_public"); return (int)Tokens.Kwd_public;}
return								{if(tokens) printToken("Kwd_return"); return (int)Tokens.Kwd_return;}
static								{if(tokens) printToken("Kwd_static"); return (int)Tokens.Kwd_static;}
struct								{if(tokens) printToken("Kwd_struct"); return (int)Tokens.Kwd_struct;}
using								{if(tokens) printToken("Kwd_using"); return (int)Tokens.Kwd_using;}
void								{if(tokens) printToken("Kwd_void"); return (int)Tokens.Kwd_void;}
while								{if(tokens) printToken("Kwd_while"); return (int)Tokens.Kwd_while;}
{number}							{if(tokens) printTokenText("Number",yytext); return (int)Tokens.Number;}
{stringConst}						{if(tokens) printTokenText("StringConst",yytext); return (int)Tokens.StringConst;}
{ident}								{if(tokens) printTokenText("Ident",yytext); return (int)Tokens.Ident;}

<<EOF>>								{if(tokens) file.Close(); if (!errors) Console.WriteLine("{0} lines from file {1} were parsed successfully", lineNum, filename);}
.									{yyerror("illegal character ('{0}')", yytext); errors = true;} 


%%

public string last_token_text = "";

private void printTokenChar(string token)
{
	file.WriteLine("Token \"{0}\"", token);
	return;
}

private void printToken(string token)
{
	file.WriteLine("Token.{0}", token);
	return;
}

private void printTokenText(string token, string val)
{
	file.WriteLine("Token.{0}, text = \"{1}\"", token, val);
	return;
}
