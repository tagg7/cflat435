CSC 435 2012F

Assignment 1
Lexical Analysis for Cb Programming Language

By Mike Lyttle and Stephen Bates



Building:

gplex CbLexer.lex
gppg /gplex CbParser.y > CbParser.cs
csc /r:QUT.ShiftReduceParser.dll CbLexer.cs CbParser.cs cbc.cs



Usage: cbc [OPTION]... [FILE]
Compiles Cb file FILE.
  -tokens             output tokens to tokens.txt
  -debug              display debug messages
