# NipSharp

A Antlr4 grammar based Nip rule matcher for C#

Example usage and be seen in tests project.

If you want to see what does not work, check out https://github.com/blizzhackers/pickits along side this repository and run TestBlizzhackerPickits test,
which will look for every .nip file and try to parse it.

It will be clear what cases are not supported, but the TL;DR of it is:


1. Aliases for prefixes, as I could not be bothered to find a list for them.
2. Stuff that I think is genuinely invalid, i.e:
   1. `[flag] == !etheral` which should be `[flag] != etheral`, because this doesn't make much sense on how flags are handled, i.e `[flag]&value == value` makes sense, `[flag]&!etheral == !etheral` probably doesn't as it turns a flag into a boolean.
   2. `name == scissorssuwayyah` which is missing the `[]`. Could be fixed, OR NOT.
   3. `[type] == ringmail` where `ringmail` is a `[name]` not a `[type]`
   4. Genuinely invalid aliases `claws` instead of `claw`
3. Operations that involve floating point numbers.
   This is something that could be fixed, but currently we store all of the attributes in a single <string, int> dictionary.
   If we changed that dictionary to store <string, float> then things like flag checking breaks, because flags work as an AND masking operator, 
   which does not work for floats. There are two ways around it, either have two dictionaries, one for properties, one for stats, or do a cast to int
   when doing flag checks.
4. Any fancy operators that are interpolated by javascript. (i.e, `me.level`, etc). 
   `item.getStat` and be simply replaced with the aliases for those stats, or even `[42]` etc for the 42nd stat.
   In theory, the parser could be extended to simply replace `item.getStat(<n>)` with just `[<n>]`, and we could 
   easily add support for additional "values", i.e, `me.level` 
5. Block comments
