/* CbParser.y */

%namespace  FrontEnd
%tokentype  Tokens

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
%token Kwd_while Kwd_Length
%token PLUSPLUS MINUSMINUS Ident Number StringConst

%%

/* *************************************************************************
   *                                                                       *
   *         PRODUCTION RULES AND ASSOCIATED SEMANTIC ACTIONS              *
   *                                                                       *
   ************************************************************************* */

Program:	  UsingList Kwd_class Ident '{'  DeclList  '}'
		;

UsingList:	  /* empty */
		| Kwd_using Ident ';' UsingList
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
		| /* empty */
		;

Block:		  '{' DeclsAndStmts '}'
		;

LocalDecl:	  Ident IdentList ';'
		| Ident '[' ']' IdentList ';'
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

%%




