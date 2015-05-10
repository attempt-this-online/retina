# Retina

## What is Retina?

Retina is a regex-based programming language. It's main feature is taking some text via standard input and repeatedly applying regex operations to it (e.g. matching, splitting, and most of all replacing). Under the hood, it uses .NET's regex engine, which means that both the .NET flavour and the ECMAScript flavour are available.

## Running Retina

There is an up-to-date Windows binary of Retina in the root directory of the repository. Alternatively, you can build it yourself from the C# sources. I have not yet tested Retina with Mono, and would be very interested in feedback on whether that works.

## How does it work?

Retina either takes one or an even number of filenames as command-line arguments and has several different modes of operation. If you supply a single file, this is the *pattern*. If you supply an even number of files, these are treated as pairs of *pattern* and *replacement*. Instead of filenames, you can also supply the pattern and/or replacement directly on the command line, by using the common `-e` flag. So all of the following are valid invocations:

    Retina ./pattern.rgx
    Retina -e "foo.*"
    Retina ./pattern.rgx ./replacement.rpl
    Retina ./pattern.rgx -e "bar"
    Retina -e "foo.*" ./replacement.rpl
    Retina -e "foo.*" -e "bar"
    Retina ./pattern1.rgx ./replacement1.rpl ./pattern2.rgx ./replacement2.rpl
    Retina ./pattern1.rgx -e "bar" -e "foo*" ./replacement2.rpl

Alternatively, you can use the `-s` flag and read all patterns and replacements from a single newline-separated file.

In any case, the input to the program will be read from the standard input stream.

### The Pattern File

Regardless of how many source files are supplied, the first one (and then every other file) will be the *pattern file*. First and foremost, this will contain the regex to be used. However, if the file contains at least one backtick (`` ` ``), the code before the first backtick will be used to configure the exact operation mode of Retina - let's call this the *configuration string*. As an example, the pattern file

    _Ss`a.

configures Retina with `_Ss` (more on that later) and defines the regex as `a.`. Any further backticks are simply part of the regex. If you want to use backticks in your regex, but do not want to configure Retina, just start your pattern file with a single backtick (which translates to an empty configuration).

If *only* the pattern file is supplied, there are several different operation modes available, the default being a matching mode.

### The Replacement File

If an even number of files is supplied, Retina always operates in Replace mode (which can also be configured). In this case the files are first grouped into pairs, where each pair constitutes one replacement stage, which modifies the input and passes it on to the next stage. The first source file in each pair is then still a *pattern*, but the second file in each pair is used to define the replacement string. You can use all the usual references to capturing groups like `$n`.

### The Configuration String

Currently, the configuration string simply is an unordered bunch of characters, which switches between different options, modes and flags. This may get more complicated in the future. If multiple conflicting options are used, the latter option will override the former.

Some characters are available in all modes, some only in specific ones. Mode-specific options are denoted by non-alphanumeric characters and are mentioned below when the individual modes are discussed.

#### Regex Modifiers

[All regex modifiers available in .NET](https://msdn.microsoft.com/en-us/library/system.text.regularexpressions.regexoptions%28v=vs.110%29.aspx) (through `RegexOptions`), except `Compiled` are available through the configuration string. This means you don't have use inline modifiers like `(?m)` in the regex (although you can). All regex modifiers are available through lower case letters:

- `c`: Is for `CultureInvariant`. Quoting MSDN: "Specifies that cultural differences in language is ignored. For more information, see the "Comparison Using the Invariant Culture" section in the Regular Expression Options topic."
- `e`: Activates `ECMAScript` mode and essentially changes the regex flavour. Some of the other modifiers don't work in combination with this.
- `i`: Makes the pattern case-insensitive.
- `m`: Activates `Multiline` mode, which makes `^` and `$` match the beginning and end of lines, respectively, in addition to the beginning and end of the entire input.
- `n`: Activates `ExplicitCapture` mode. Quoting MSDN: "Specifies that the only valid captures are explicitly named or numbered groups of the form (?<name>…). This allows unnamed parentheses to act as noncapturing groups without the syntactic clumsiness of the expression (?:…). For more information, see the "Explicit Captures Only" section in the Regular Expression Options topic."
- `r`: Activates `RightToLeft` mode. For more information [see MSDN](https://msdn.microsoft.com/en-us/library/yd1hzczs(v=vs.110).aspx#RightToLeft).
- `s`: Activates `Singleline` mode, which makes `.` match newline characters.
- `x`: Activates free-spacing mode, in which all whitespace in the pattern is ignored (unless escaped), and single-line comments can be written with `#`.

#### Mode Selection

When only the pattern file is supplied, Retina will usually operate in Match mode, but the following upper-case letters in the configuration string can select other modes:

- `S`: Split mode.
- `G`: Grep mode.
- `A`: AntiGrep mode.

#### General Options

The following options apply to all modes (but may be a bit useless in some of them):

- `;`: Silent mode, suppresses all output. This is mostly available for legacy reasons. All but the last stage are silent by default.
- `:`: Turns off silent mode. Use this if you want to output the results of intermediate stages.
- `(` and `)`: `(` opens a loop and `)` closes a loop. All stages between `(` and `)` (inclusive) will be repeated in a loop until an iteration doesn't change the result. Note that `(` and `)` may appear in the same stage, looping only that stage. Also, the order of `(` and `)` within a single stage is irrelevant - they are always treated as if all `(` appear before all `)`. Furthermore, an unmatched `)` assumes a `(` in the first stage, and an unmatched `(` assumes a `)` in the last stage. Loops can be nested.

## Operation Modes

As outlined above, Retina currently supports 5 different operation modes: Match, Split, Grep, AntiGrep, Replace.

### Match Mode

This is the default mode. It takes the regex with its modifiers and applies it to the input. By default the number of matches will be printed.

Match mode currently supports the following options:

- `!`: Print each match, separated by newlines. (Instead of the total match count.)
- `&`: Consider overlapping matches. Normally, the regex engine will start looking for the next match after the *end* of the previous match. With this mode, Retina will instead look for the next match after the *beginning* of the previous match. Note that this will still not take into account overlapping matches which start at the same position (but end at different positions.)

Ultimately, this mode will probably receive the most elaborate configuration scheme, in order print capturing groups or other information about the match.

### Split Mode

This passes the regex on `Regex.Split`, and prints each string in the result on its own line. This means that you can use capturing groups to include parts of the matches in the output.

Split mode comes with one additional option:

- `_`: Filter all empty strings out of the result before printing.

I might add more configuration options in order to control the output format or to limit the number of splits in the future.

### Grep and AntiGrep Mode

Grep mode makes Retina assume [grep's](http://en.wikipedia.org/wiki/Grep) basic mode of operation: the input is split into individual lines, the regex is matched against each line, and Retina prints all lines that yielded a match.

AntiGrep mode is almost the same, except that it prints all lines which *didn't* yield a match.

### Replace Mode

Replace mode does what it says on the tin: it replaces all matches of the regex in the input with the replacement string, and prints the result. However, Replace mode comes with very important options:

- `+`: Repeatedly apply the regex replacement until the regex doesn't match any more. This option makes Retina [Turing-complete](http://en.wikipedia.org/wiki/Turing_completeness) (see below for details).
- `?`: Only in conjunction with `+`, print all the intermediate results of the loop, each on its own line.

Replace mode will probably get at least one more option in the future: limiting the number of replacements done per iteration (e.g. replace only the first match).

## Retina is Turing-complete

While the .NET-flavour itself is *just* short of being Turing-complete (as far as I know), Retina is, thanks to repeated Replace mode via the `+` option. As an example, I've implemented a [2-Tag System](http://en.wikipedia.org/wiki/Tag_system) "interpreter" in Retina.

Pattern file:

    +`^(.).(\w*)(?=\|.*\1>(\w*))|^(?<2>\w+).*

Replacement file:

    $2$3

With this Retina code in place, an arbitrary tag system can be supplied via STDIN, as long as its alphabet consists only of alphanumerical characters and underscores (this limitation could easily be removed). The input must consist of the initial word, as well as the set of production rules available for the tag system. For instance, [the first example system given on Wikipedia](http://en.wikipedia.org/wiki/Tag_system#Example:_A_simple_2-tag_illustration), would be written as

    baa|a>ccbaH,b>cca,c>cc

Note that the pattern is hardcoded to 2-tag systems, although it would also be possible to generalise this to `n`-tag systems, where `n` could be encoded in unary in the input string.
