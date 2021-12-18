// To generate run:
// java -jar antlr-4.9.3-complete.jar Nip.g4  -o Generated -Dlanguage=CSharp -no-listener -visitor -package NipSharp
grammar Nip;

/* * Parser Rules */

// Left hand side
flagProperty: FLAG;
affixProperty: AFFIX;
stat: IDENTIFIER | INTEGER;
property: IDENTIFIER;
maxQuantity: MAXQUANTITY;
tier: TIER;
mercTier: MERCTIER;

// Right hand side
number: INTEGER | FLOAT;
numberOrAlias: IDENTIFIER | INTEGER;

statExpr
    : statExpr op=(MUL | DIV) statExpr #statMulDivRule
    | statExpr op=(ADD | SUB) statExpr #statAddSubRule
    | '['stat']' #statNameRule
    | number #statNumberRule
    | '('statExpr')' #statExprParenRule
    ;

statRule
    : statExpr op=(EQ | NEQ | GT | GTE | LT | LTE) statExpr #statRelationalRule
    | statRule op=(AND | OR) statRule #statLogicalRule
    | '('statRule')' #statParenRule
    ;

// Because we peek at the value
propertyRule
    : '['flagProperty']' op=(EQ | NEQ | GT | GTE | LT | LTE) numberOrAlias #propFlagRule
    | '['affixProperty']' op=(EQ | NEQ | GT | GTE | LT | LTE) numberOrAlias #propAffixRule
    | '['property']' op=(EQ | NEQ | GT | GTE | LT | LTE) numberOrAlias #propRelationalRule
    | property op=(EQ | NEQ | GT | GTE | LT | LTE) numberOrAlias #propRelationalRule
    | propertyRule op=(AND | OR) propertyRule #propLogicalRule
    | '('propertyRule')' #propParenRule
    ;

additionalRule
    : '['maxQuantity']' op=EQ statExpr #additionalMaxQuantityRule
    | '['tier']' op=EQ statExpr #additionalTierRule
    | '['mercTier']' op=EQ statExpr #additionalMercTierRule
    | additionalRule op=(AND | OR) additionalRule #additionalLogicalRule
    | '('additionalRule')' #additionalParenRule
    ;

nipRule: propertyRule? ('#' statRule? ('#' additionalRule?)?)?;

line: nipRule | <EOF>;

/* * Lexer Rules */
fragment DIGIT: [0-9];
fragment LETTER: [a-z];
fragment QUOTE: '\'';
fragment COMMA: ',';
FLAG: 'flag';
AFFIX: ('prefix' | 'suffix');
MAXQUANTITY: 'maxquantity';
TIER: 'tier';
MERCTIER: 'merctier';
FLOAT: DIGIT+ '.' DIGIT+;
INTEGER: DIGIT+;
IDENTIFIER: LETTER(LETTER | DIGIT | QUOTE | COMMA)*;
EQ: '==';
NEQ: '!=';
GT: '>';
GTE: '>=';
LT: '<';
LTE: '<=';
AND: '&&';
OR: '||';
MUL: '*';
DIV: '/';
ADD: '+';
SUB: '-';
WS: [\t\r\n ]+ -> skip;
LINE_COMMENT: '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip; // The wildcard catch in the middle might be wrong? But seems to work?
