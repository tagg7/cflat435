%namespace LexScanner
%tokentype Tokens

%{
  public int lineNum = 1;
  public bool errors = false;
  public string filename = null;

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
"+"									{return (int)'+';}
"-"									{return (int)'-';}
"*"									{return (int)'*';}
"/"									{return (int)'/';}
"%"									{return (int)'%';}
">"									{return (int)'>';}
"<"									{return (int)'<';}
"="									{return (int)'=';}
";"									{return (int)';';}
","									{return (int)',';}
"."									{return (int)'.';}
"("									{return (int)'(';}
")"									{return (int)')';}
"["									{return (int)'[';}
"]"									{return (int)']';}
"{"									{return (int)'{';}
"}"									{return (int)'}';}
"++"								{return (int)Tokens.PLUSPLUS;}
"--"								{return (int)Tokens.MINUSMINUS;}
"=="								{return (int)Tokens.EQEQ;}
"!="								{return (int)Tokens.NOTEQ;}
">="								{return (int)Tokens.GTEQ;}
"<="								{return (int)Tokens.LTEQ;}
"&&"								{return (int)Tokens.ANDAND;}
"||"								{return (int)Tokens.OROR;}
break								{return (int)Tokens.Kwd_break;}
class								{return (int)Tokens.Kwd_class;}
const								{return (int)Tokens.Kwd_const;}
else								{return (int)Tokens.Kwd_else;}
if									{return (int)Tokens.Kwd_if;}
new									{return (int)Tokens.Kwd_new;}
out									{return (int)Tokens.Kwd_out;}
public								{return (int)Tokens.Kwd_public;}
return								{return (int)Tokens.Kwd_return;}
static								{return (int)Tokens.Kwd_static;}
struct								{return (int)Tokens.Kwd_struct;}
using								{return (int)Tokens.Kwd_using;}
void								{return (int)Tokens.Kwd_void;}
while								{return (int)Tokens.Kwd_while;}
{number}							{return (int)Tokens.Number;}
{stringConst}						{return (int)Tokens.StringConst;}
{ident}								{return (int)Tokens.Ident;}

<<EOF>>								{if (!errors) Console.WriteLine("{0} lines from file {1} were parsed successfully", lineNum, filename);}
.									{yyerror("illegal character ('{0}')", yytext); errors = true;} 


%%

public string last_token_text = "";

