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

  public void openFile( )  {
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
"+"									{if(tokens) printToken(1,"+"); return (int)'+';}
"-"									{if(tokens) printToken(1,"-"); return (int)'-';}
"*"									{if(tokens) printToken(1,"*"); return (int)'*';}
"/"									{if(tokens) printToken(1,"/"); return (int)'/';}
"%"									{if(tokens) printToken(1,"%"); return (int)'%';}
">"									{if(tokens) printToken(1,">"); return (int)'>';}
"<"									{if(tokens) printToken(1,"<"); return (int)'<';}
"="									{if(tokens) printToken(1,"="); return (int)'=';}
";"									{if(tokens) printToken(1,";"); return (int)';';}
","									{if(tokens) printToken(1,","); return (int)',';}
"."									{if(tokens) printToken(1,"."); return (int)'.';}
"("									{if(tokens) printToken(1,"("); return (int)'(';}
")"									{if(tokens) printToken(1,")"); return (int)')';}
"["									{if(tokens) printToken(1,"["); return (int)'[';}
"]"									{if(tokens) printToken(1,"]"); return (int)']';}
"{"									{if(tokens) printToken(1,"{"); return (int)'{';}
"}"									{if(tokens) printToken(1,"}"); return (int)'}';}
"++"								{if(tokens) printToken(2,"PLUSPLUS"); return (int)Tokens.PLUSPLUS;}
"--"								{if(tokens) printToken(2,"MINUSMINUS"); return (int)Tokens.MINUSMINUS;}
"=="								{if(tokens) printToken(2,"EQEQ"); return (int)Tokens.EQEQ;}
"!="								{if(tokens) printToken(2,"NOTEQ"); return (int)Tokens.NOTEQ;}
">="								{if(tokens) printToken(2,"GTEQ"); return (int)Tokens.GTEQ;}
"<="								{if(tokens) printToken(2,"LTEQ"); return (int)Tokens.LTEQ;}
"&&"								{if(tokens) printToken(2,"&&"); return (int)Tokens.ANDAND;}
"||"								{if(tokens) printToken(2,"OROR"); return (int)Tokens.OROR;}
break								{if(tokens) printToken(2,"Kwd_break"); return (int)Tokens.Kwd_break;}
class								{if(tokens) printToken(2,"Kwd_class"); return (int)Tokens.Kwd_class;}
const								{if(tokens) printToken(2,"Kwd_const"); return (int)Tokens.Kwd_const;}
else								{if(tokens) printToken(2,"Kwd_else"); return (int)Tokens.Kwd_else;}
if									{if(tokens) printToken(2,"Kwd_if"); return (int)Tokens.Kwd_if;}
new									{if(tokens) printToken(2,"Kwd_new"); return (int)Tokens.Kwd_new;}
out									{if(tokens) printToken(2,"Kwd_out"); return (int)Tokens.Kwd_out;}
public								{if(tokens) printToken(2,"Kwd_public"); return (int)Tokens.Kwd_public;}
return								{if(tokens) printToken(2,"Kwd_return"); return (int)Tokens.Kwd_return;}
static								{if(tokens) printToken(2,"Kwd_static"); return (int)Tokens.Kwd_static;}
struct								{if(tokens) printToken(2,"Kwd_struct"); return (int)Tokens.Kwd_struct;}
using								{if(tokens) printToken(2,"Kwd_using"); return (int)Tokens.Kwd_using;}
void								{if(tokens) printToken(2,"Kwd_void"); return (int)Tokens.Kwd_void;}
while								{if(tokens) printToken(2,"Kwd_while"); return (int)Tokens.Kwd_while;}
{number}							{if(tokens) printToken(3, yytext); return (int)Tokens.Number;}
{stringConst}						{if(tokens) printToken(4, yytext); return (int)Tokens.StringConst;}
{ident}								{if(tokens) printToken(5, yytext); return (int)Tokens.Ident;}

<<EOF>>								{if(tokens) file.Close(); if (!errors) Console.WriteLine("{0} lines from file {1} were parsed successfully", lineNum, filename);}
.									{yyerror("illegal character ('{0}')", yytext); errors = true;} 


%%

public string last_token_text = "";

private void printToken(int type, string text)
{
	switch(type)
	{
		case 1:
			file.WriteLine("Token \"{0}\"", text);
			break;
		case 2:
			file.WriteLine("Token.{0}", text);
			break;
		case 3:
			file.WriteLine("Token.Number, value = \"{0}\"", text);
			break;
		case 4:
			file.WriteLine("Token.StringConst, text = \"{0}\"", text);
			break;
		case 5:
			file.WriteLine("Token.Ident, text = \"{0}\"", text);
			break;
	}

	return;
}
