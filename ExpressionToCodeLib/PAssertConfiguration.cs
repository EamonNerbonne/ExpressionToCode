using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib
{
    public class PAssertConfiguration
    {
        public static readonly PAssertConfiguration DefaultConfiguration = new PAssertConfiguration(new ValuesOnStalksCodeAnnotator());
        public static PAssertConfiguration CurrentConfiguration = DefaultConfiguration;

        public readonly ICodeAnnotator CodeAnnotator;
        public PAssertConfiguration(ICodeAnnotator codeAnnotator) { CodeAnnotator = codeAnnotator; }

    }

    

}
