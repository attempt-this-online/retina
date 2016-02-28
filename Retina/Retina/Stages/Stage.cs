﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Retina.Stages
{
    public abstract class Stage
    {
        public Options Options { get; set; }

        public Stage(Options options)
        {
            Options = options;
        }

        public virtual string Execute(string input)
        {
            string result;
            if (Options.Loop)
            {
                result = input;
                string lastResult;
                do
                {
                    lastResult = result;
                    result = Process(lastResult).ToString();
                    if (!Options.IterationSilent)
                        if (Options.IterationTrailingLinefeed)
                            Console.WriteLine(result);
                        else
                            Console.Write(result);
                } while (lastResult != result);
            }
            else
                result = Process(input).ToString();

            if (!(Options.Silent ?? true))
                if (Options.TrailingLinefeed)
                    Console.WriteLine(result);
                else
                    Console.Write(result);

            return result;
        }

        abstract protected StringBuilder Process(string input);
    }
}
