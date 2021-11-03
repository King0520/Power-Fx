﻿//------------------------------------------------------------------------------
// <copyright file="Average.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Average(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel function: Average
    internal sealed class AverageFunction : StatisticalFunction
    {
        public override bool RequiresErrorContext => true;

        public AverageFunction()
            : base("Average", TexlStrings.AboutAverage, FunctionCategories.MathAndStat)
        { }
    }

    // Average(source:*, projection:n)
    // Corresponding DAX function: Average
    internal sealed class AverageTableFunction : StatisticalTableFunction
    {
        public override bool RequiresErrorContext => true;

        public override DelegationCapability FunctionDelegationCapability { get { return DelegationCapability.Average; } }

        public AverageTableFunction()
            : base("Average", TexlStrings.AboutAverageT, FunctionCategories.Table)
        { }
    }
}