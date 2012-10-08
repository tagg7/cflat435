/* CbParser.y */

%namespace  FrontEnd
%tokentype  Tokens
%output=CbParser.cs

%YYSTYPE    AST

// All tokens which can be used as operators in expressions
// they are ordered by precedence level (lowest first)
%right '='
%left OROR
%left ANDAND
%nonassoc EQEQ NOTEQ
%nonassoc '>' GTEQ '<' LTEQ
%left '+' '-'
%left '*' '/' '%'
%left UMINUS

// All other named tokens (i.e. the single character tokens are omitted)
// The order in which they are listed here does not matter.
%token Kwd_break Kwd_class Kwd_const Kwd_else Kwd_if Kwd_new Kwd_out
%token Kwd_public Kwd_return Kwd_static Kwd_struct Kwd_using Kwd_void
%token Kwd_while
%token PLUSPLUS MINUSMINUS Ident Number StringConst

%{
    string filename;
    Scanner lexer;
    AST tree;

    // define our own constructor for the Parser class
    public Parser( string filename, Scanner lexer ): base(lexer) {
        this.filename = filename;
        this.Scanner = this.lexer = lexer;
        tree = null;
    }

    // returns the lexer's current line number
    public int LineNumber {
        get{ return lexer.LineNumber; }
    }

    // returns the AST constructed for the CFlat program
    public AST Tree { get{ return tree; } }

    // Use this function for reporting non-fatal errors discovered
    // while parsing and building the AST.
    // An example usage is:
    //    yyerror( "Identifier {0} has not been declared", idname );
    public void yyerror( string format, params Object[] args ) {
        Console.Write("{0}: ", LineNumber);
        Console.WriteLine(format, args);
    }

%}

%%

/* *************************************************************************
   *                                                                       *
   *         PRODUCTION RULES AND ASSOCIATED SEMANTIC ACTIONS              *
   *                                                                       *
   ************************************************************************* */

Program:	  UsingList Kwd_class Identifier '{'  DeclList  '}'
			{ tree = AST.NonLeaf(NodeType.Class, LineNumber, $1, $3, $5); }
		;

UsingList:	  /* empty */
			{ $$ = AST.Kary(NodeType.UsingList, LineNumber); }
		| UsingList Kwd_using Identifier ';'
			{ $1.AddChild($3);  $$ = $1; }
		;

DeclList:	  DeclList ConstDecl
		| DeclList StructDecl
		| DeclList MethodDecl
		| /* empty */
		;

ConstDecl:	  Kwd_public Kwd_const Type Ident '=' InitVal ';'
		;

InitVal:	  Number
		| StringConst
		;

StructDecl:	  Kwd_public Kwd_struct Ident '{' FieldDeclList '}'
		;

FieldDeclList:	  FieldDeclList FieldDecl
		| /* empty */
		;

FieldDecl:	  Kwd_public Type IdentList ';'
		;

IdentList:	  IdentList ',' Ident
		| Ident
		;

MethodDecl:	  Kwd_public Kwd_static Kwd_void Ident '(' OptFormals ')' Block
		| 	  Kwd_public Kwd_static Ident Ident '(' OptFormals ')' Block  // NEW!
		;

OptFormals:	  /* empty */
		| FormalPars
		;

FormalPars:	  FormalDecl
		| FormalPars ',' FormalDecl
		;

FormalDecl:	  Type Ident
		;

Type:		  Ident
		| Ident '[' ']'
		;

Statement:	  Designator '=' Expr ';'
		| Designator '(' OptActuals ')' ';'  /* includes cbio.write(a); */
		| Designator PLUSPLUS ';'
		| Designator MINUSMINUS ';'
		| Kwd_if '(' Expr ')' Statement OptElsePart
		| Kwd_while '(' Expr ')' Statement
		| Kwd_break ';'
		| Kwd_return ';'
		| Kwd_return Expr ';'
		| Designator '(' Kwd_out Designator ')' ';'  /* this is cbio.read(out v); */
		|  Block
		|  ';'
		;

OptActuals:	  /* empty */
		| ActPars
		;

ActPars:	  ActPars ',' Expr
		| Expr
		;

OptElsePart:	  Kwd_else Statement
		| /* empty */    // an empty statement must be generated here
		;

Block:		  '{' DeclsAndStmts '}'
		;

LocalDecl:	  Ident IdentList ';'    // declare with a simple type
		| Ident '[' ']' IdentList ';'  // declare with an array type
		;

DeclsAndStmts:	  /* empty */
		| DeclsAndStmts Statement
		| DeclsAndStmts LocalDecl
		;

Expr:		  Expr OROR Expr
		| Expr ANDAND Expr
		| Expr EQEQ Expr
		| Expr NOTEQ Expr
		| Expr LTEQ Expr
		| Expr '<' Expr
		| Expr GTEQ Expr
		| Expr '>' Expr
		| Expr '+' Expr
		| Expr '-' Expr
		| Expr '*' Expr
		| Expr '/' Expr
		| Expr '%' Expr
		| '-' Expr %prec UMINUS
		| Designator
		| Designator '(' OptActuals ')'
		| Number
		| StringConst
		| StringConst '.' Ident // Ident must be "Length"
		| Kwd_new Ident
		| Kwd_new Ident '[' Expr ']'
		| '(' Expr ')'
		;

Designator:	  Ident Qualifiers
		;

Qualifiers:	  '.' Ident Qualifiers
		| '[' Expr ']' Qualifiers
		| /* empty */
		;

Identifier:	  Ident  { $$ = AST.Leaf(NodeType.Ident, LineNumber, lexer.yytext); }
		;
%%


