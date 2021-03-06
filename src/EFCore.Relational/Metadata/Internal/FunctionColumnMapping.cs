// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

#nullable enable

namespace Microsoft.EntityFrameworkCore.Metadata.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class FunctionColumnMapping : ColumnMappingBase, IFunctionColumnMapping
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public FunctionColumnMapping(
            [NotNull] IProperty property,
            [NotNull] FunctionColumn column,
            [NotNull] FunctionMapping viewMapping)
            : base(property, column, viewMapping)
        {
        }

        /// <inheritdoc />
        public virtual IFunctionMapping FunctionMapping
            => (IFunctionMapping)TableMapping;

        /// <inheritdoc />
        public override RelationalTypeMapping TypeMapping => Property.FindRelationalTypeMapping(
            StoreObjectIdentifier.DbFunction(FunctionMapping.DbFunction.Name))!;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override string ToString()
            => this.ToDebugString(MetadataDebugStringOptions.SingleLineDefault);

        IFunctionColumn IFunctionColumnMapping.Column
        {
            [DebuggerStepThrough]
            get => (IFunctionColumn)Column;
        }
    }
}
