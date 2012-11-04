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
    
    private AST repNull( AST tree, AST replacement ) {
        if (tree == null) return replacement;
        AST_nonleaf np = tree as AST_nonleaf;
        for( ; ; ) {
            if (np == null)
                throw new Exception("internal error restructuring Qualifiers");
            if (np[0] == null)
                break;
            np = np[0] as AST_nonleaf;
        }
        np[0] = replacement;
        return tree;
    }

%}

%%

/* *************************************************************************
   *                                                                       *
   *         PRODUCTION RULES AND ASSOCIATED SEMANTIC ACTIONS              *
   *                                                                       *
   ************************************************************************* */

Program:      UsingList Kwd_class Identifier '{'  DeclList  '}'
            { tree = AST.NonLeaf(NodeType.Program, LineNumber, $1, $3, $5); }
        ;

UsingList:    /* empty */
            { $$ = AST.Kary(NodeType.UsingList, LineNumber); }
        | UsingList Kwd_using Identifier ';'
            { $1.AddChild($3);  $$ = $1; }
        ;

DeclList:  DeclList ConstDecl    { $1.AddChild($2); $$ = $1; }
        |  DeclList StructDecl   { $1.AddChild($2); $$ = $1; }
        |  DeclList MethodDecl   { $1.AddChild($2); $$ = $1; }
        |  /* empty */           { $$ = AST.Kary(NodeType.DeclList, LineNumber); }
        ;

ConstDecl:  Kwd_public Kwd_const Type Identifier '=' InitVal ';'
                                 { $$ = AST.NonLeaf(NodeType.Const, LineNumber, $3, $4, $6); }
        ;

InitVal:   IntConst    { $$ = $1; }
        |  SConst      { $$ = $1; }
        ;

StructDecl:   Kwd_public Kwd_struct Identifier '{' FieldDeclList '}'
                                 { $$ = AST.NonLeaf(NodeType.Struct, LineNumber, $3, $5); }
        ;

FieldDeclList:  FieldDeclList FieldDecl
                                 { $1.AddChild($2); $$ = $1; }
        | /* empty */            { $$ = AST.Kary(NodeType.FieldList, LineNumber); }
        ;

FieldDecl:  Kwd_public Type IdentList ';'
                                 { $$ = AST.NonLeaf(NodeType.FieldDecl, LineNumber, $2, $3); }
        ;

IdentList:  IdentList ',' Identifier
                                 { $1.AddChild($3); $$ = $1; }
        | Identifier             { $$ = AST.Kary(NodeType.IdList, LineNumber, $1); }
        ;

MethodDecl:  Kwd_public Kwd_static Type Identifier '(' OptFormals ')' Block
                        { $$ = AST.NonLeaf(NodeType.Method, LineNumber, null, $4, $6, $8); }
        |   Kwd_public Kwd_static Identifier Identifier '(' OptFormals ')' Block  // NEW!
                        { $$ = AST.NonLeaf(NodeType.Method, LineNumber, $3, $4, $6, $8); }
        ;

OptFormals:   /* empty */       { $$ = AST.Kary(NodeType.Formals, LineNumber); }  
        | FormalPars            { $$ = $1; }
        ;

FormalPars:  FormalDecl         { $$ = AST.Kary(NodeType.Formals, LineNumber, $1); }
        | FormalPars ',' FormalDecl
                                { $1.AddChild($3); $$ = $1; }
        ;

FormalDecl: Type Identifier     { $$ = AST.NonLeaf(NodeType.Formal, LineNumber, $1, $2); }
        ;

Type:     Identifier            { $$ = $1; }
        | Identifier '[' ']'    { $$ = AST.NonLeaf(NodeType.Array, LineNumber, $1); }
        ;

Statement: Designator '=' Expr ';'
                                { $$ = AST.NonLeaf(NodeType.Assign, LineNumber, $1, $3); }
        |  Designator '(' OptActuals ')' ';'  /* includes cbio.write(a); */
                                { $$ = AST.NonLeaf(NodeType.Call, LineNumber, $1, $3); }
        |  Designator PLUSPLUS ';'
                                { $$ = AST.NonLeaf(NodeType.PlusPlus, LineNumber, $1); }
        |  Designator MINUSMINUS ';'
                                { $$ = AST.NonLeaf(NodeType.MinusMinus, LineNumber, $1); }
        |  Kwd_if '(' Expr ')' Statement OptElsePart
                                { $$ = AST.NonLeaf(NodeType.If, LineNumber, $3, $5, $6); }
        |  Kwd_while '(' Expr ')' Statement
                                { $$ = AST.NonLeaf(NodeType.While, LineNumber, $3, $5); }
        |  Kwd_break ';'        { $$ = AST.Leaf(NodeType.Break, LineNumber); }
        |  Kwd_return ';'       { $$ = AST.Kary(NodeType.Return, LineNumber); }
        |  Kwd_return Expr ';'  { $$ = AST.Kary(NodeType.Return, LineNumber, $2); }
        |  Designator '(' Kwd_out Designator ')' ';'  /* this is cbio.read(out v); */
                                { $$ = AST.NonLeaf(NodeType.Read, LineNumber, $1, $4); }
        |  Block                { $$ = $1; }
        |  ';'                  { $$ = AST.Leaf(NodeType.Empty, LineNumber); }
        ;

OptActuals:   /* empty */       { $$ = AST.Kary(NodeType.Actuals, LineNumber); }
        | ActPars               { $$ = $1; }
        ;

ActPars:   ActPars ',' Expr     { $1.AddChild($3); $$ = $1; }
        |  Expr                 { $$ = AST.Kary(NodeType.Actuals, LineNumber, $1); }
        ;

OptElsePart:  Kwd_else Statement
                                { $$ = $2; }
        | /* empty */    // an empty statement must be generated here
                                { $$ = AST.Leaf(NodeType.Empty, LineNumber); }
        ;

Block:    '{' DeclsAndStmts '}' { $$ = $2; }
        ;

LocalDecl: Identifier IdentList ';'    // declare with a simple type
                                 { $$ = AST.NonLeaf(NodeType.LocalDecl, LineNumber, $1, $2); }
        | Identifier '[' ']' IdentList ';'  // declare with an array type
                                 {   AST t = AST.NonLeaf(NodeType.Array, LineNumber, $1);
                                     $$ = AST.NonLeaf(NodeType.LocalDecl, LineNumber, t, $4);
                                 }
        ;

DeclsAndStmts:    /* empty */      { $$ = AST.Kary(NodeType.Block, LineNumber); }
        | DeclsAndStmts Statement  { $1.AddChild($2); $$ = $1; }
        | DeclsAndStmts LocalDecl  { $1.AddChild($2); $$ = $1; }
        ;

Expr:     Expr OROR Expr         { $$ = AST.NonLeaf(NodeType.Or, LineNumber, $1, $3); }
        | Expr ANDAND Expr       { $$ = AST.NonLeaf(NodeType.And, LineNumber, $1, $3); }
        | Expr EQEQ Expr         { $$ = AST.NonLeaf(NodeType.Equals, LineNumber, $1, $3); }
        | Expr NOTEQ Expr        { $$ = AST.NonLeaf(NodeType.NotEquals, LineNumber, $1, $3); }
        | Expr LTEQ Expr         { $$ = AST.NonLeaf(NodeType.LessOrEqual, LineNumber, $1, $3); }
        | Expr '<' Expr          { $$ = AST.NonLeaf(NodeType.LessThan, LineNumber, $1, $3); }
        | Expr GTEQ Expr         { $$ = AST.NonLeaf(NodeType.GreaterOrEqual, LineNumber, $1, $3); }
        | Expr '>' Expr          { $$ = AST.NonLeaf(NodeType.GreaterThan, LineNumber, $1, $3); }
        | Expr '+' Expr          { $$ = AST.NonLeaf(NodeType.Add, LineNumber, $1, $3); }
        | Expr '-' Expr          { $$ = AST.NonLeaf(NodeType.Sub, LineNumber, $1, $3); }
        | Expr '*' Expr          { $$ = AST.NonLeaf(NodeType.Mul, LineNumber, $1, $3); }
        | Expr '/' Expr          { $$ = AST.NonLeaf(NodeType.Div, LineNumber, $1, $3); }
        | Expr '%' Expr          { $$ = AST.NonLeaf(NodeType.Mod, LineNumber, $1, $3); }
        | '-' Expr %prec UMINUS  { $$ = AST.NonLeaf(NodeType.UnaryMinus, LineNumber, $2); }
        | Designator             { $$ = $1; }
        | Designator '(' OptActuals ')'
                                 { $$ = AST.NonLeaf(NodeType.Call, LineNumber, $1, $3); }
        | IntConst               { $$ = $1; }
        | SConst                 { $$ = $1; }
        | SConst '.' Identifier // Ident must be "Length"
                                 { $$ = AST.NonLeaf(NodeType.Dot, LineNumber, $1, $3); } 
        | Kwd_new Identifier     { $$ = AST.NonLeaf(NodeType.NewStruct, LineNumber, $2); } 
        | Kwd_new Identifier '[' Expr ']'
                                 { $$ = AST.NonLeaf(NodeType.NewArray, LineNumber, $2, $4); }
        | '(' Expr ')'           { $$ = $2; }
        ;

Designator:   Identifier Qualifiers
                                  { $$ = repNull($2,$1); }
        ;

Qualifiers:   '.' Identifier Qualifiers
                                  { AST t = AST.NonLeaf(NodeType.Dot, LineNumber, null, $2);
                                    $$ = repNull($3,t);
                                  }
        | '[' Expr ']' Qualifiers
                                  { AST t = AST.NonLeaf(NodeType.Index, LineNumber, null, $2);
                                    $$ = repNull($4,t);
                                  }
        | /* empty */
                                  { $$ = null; }
        ;

Identifier:   Ident  { $$ = AST.Leaf(NodeType.Ident, LineNumber, lexer.yytext); }
        ;

SConst:       StringConst { $$ = AST.Leaf(NodeType.StringConst, LineNumber, lexer.yytext); }
        ;

IntConst:     Number  { $$ = AST.Leaf(NodeType.IntConst, LineNumber,
                            int.Parse(lexer.yytext)); }
        ;

%%


