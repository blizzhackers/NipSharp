# NipSharp

### What is this

ANTLR4 grammar based Nip rule evaluator for C#

Nip files are used by Diablo 2 Kolbot botting framework, for deciding what items to pick up/identify/sell.

### Why

Mostly wanted to learn ANTLR and see if I can write something that parses and evaluates the grammar.

Also, share the grammar with others so that it would be easier to write parsers/evaluators in other languages.

### How does it work

The grammar parsing is done ANTLR, the expressions are then turned into a Linq expression tree and compiled for matching.

### How do I use this with a different language

You can use ANTLR generator to generate code for parsing the expressions for any language.

Writing an evaluator in a different language will require something that allows building expression trees and evaluating them.
Something similar to Linq Expression in C#, or something simillar that can be written by hand.

### Other notes

Example usage can be seen in tests project.

If you want to see what does not work, check out https://github.com/blizzhackers/pickits along side this repository and run TestBlizzhackerPickits test,
which will look for every .nip file and try to parse it.

It will be clear what cases are not supported, but the TL;DR of it is:

* Aliases for prefixes, as I could not be bothered to find a list for them, but they don't seem to be used.
* Stuff that I think is genuinely invalid, i.e:
  * `[flag] == !etheral` which should be `[flag] != etheral`, because this doesn't make much sense on how flags are handled, i.e `[flag]&value == value` makes sense, `[flag]&!etheral == !etheral` probably doesn't as it turns a flag into a boolean.
  * `name == scissorssuwayyah` which is missing the `[]`. Could be fixed, OR NOT.
  * `[type] == ringmail` where `ringmail` is a `[name]` not a `[type]`
  * Genuinely invalid aliases `claws` instead of `claw`, etc.
* `me` syntax. Support could be added by allowing to pass in additional values.
* `[tier]` and `[merctier]`. They are parsed, but always evaluate to true, as I am not sure how they are supposed to be used in the context of a pickit.

### Debugging

ANTLR v4 plugin for IntelliJ was great helping why expressions do not parse properly or where the grammar was wrong.
