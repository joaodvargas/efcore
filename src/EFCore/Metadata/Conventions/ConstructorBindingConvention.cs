// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class ConstructorBindingConvention : IModelFinalizedConvention
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public ConstructorBindingConvention([NotNull] ProviderConventionSetBuilderDependencies dependencies)
        {
            Dependencies = dependencies;
        }

        /// <summary>
        ///     Parameter object containing service dependencies.
        /// </summary>
        protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void ProcessModelFinalized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                if (entityType.ClrType?.IsAbstract == false)
                {
                    var maxServiceParams = 0;
                    var minPropertyParams = int.MaxValue;
                    var foundBindings = new List<ConstructorBinding>();
                    var bindingFailures = new List<IEnumerable<ParameterInfo>>();

                    foreach (var constructor in entityType.ClrType.GetTypeInfo()
                        .DeclaredConstructors
                        .Where(c => !c.IsStatic))
                    {
                        // Trying to find the constructor with the most service properties
                        // followed by the least scalar property parameters
                        if (Dependencies.ConstructorBindingFactory.TryBindConstructor(entityType, constructor, out var binding, out var failures))
                        {
                            var serviceParamCount = binding.ParameterBindings.OfType<ServiceParameterBinding>().Count();
                            var propertyParamCount = binding.ParameterBindings.Count - serviceParamCount;

                            if (serviceParamCount == maxServiceParams
                                && propertyParamCount == minPropertyParams)
                            {
                                foundBindings.Add(binding);
                            }
                            else if (serviceParamCount > maxServiceParams)
                            {
                                foundBindings.Clear();
                                foundBindings.Add(binding);

                                maxServiceParams = serviceParamCount;
                                minPropertyParams = propertyParamCount;
                            }
                            else if (propertyParamCount < minPropertyParams)
                            {
                                foundBindings.Clear();
                                foundBindings.Add(binding);

                                maxServiceParams = serviceParamCount;
                                minPropertyParams = propertyParamCount;
                            }
                        }
                        else
                        {
                            bindingFailures.Add(failures);
                        }
                    }

                    if (foundBindings.Count == 0)
                    {
                        var constructorErrors = bindingFailures.SelectMany(f => f)
                            .GroupBy(f => f.Member as ConstructorInfo)
                            .Select(
                                x => CoreStrings.ConstructorBindingFailed(
                                    string.Join("', '", x.Select(f => f.Name)),
                                    entityType.DisplayName() + "(" +
                                    string.Join(
                                        ", ", x.Key.GetParameters().Select(
                                            y => y.ParameterType.ShortDisplayName() + " " + y.Name)
                                    ) +
                                    ")"
                                )
                            );

                        throw new InvalidOperationException(
                            CoreStrings.ConstructorNotFound(
                                entityType.DisplayName(),
                                string.Join("; ", constructorErrors)));
                    }

                    if (foundBindings.Count > 1)
                    {
                        throw new InvalidOperationException(
                            CoreStrings.ConstructorConflict(
                                FormatConstructorString(entityType, foundBindings[0]),
                                FormatConstructorString(entityType, foundBindings[1])));
                    }

                    entityType.Builder.HasAnnotation(
                        CoreAnnotationNames.ConstructorBinding,
                        foundBindings[0]);
                }
            }
        }

        private static string FormatConstructorString(IEntityType entityType, ConstructorBinding binding)
            => entityType.ClrType.ShortDisplayName() +
               "(" + string.Join(", ", binding.ParameterBindings.Select(b => b.ParameterType.ShortDisplayName())) + ")";
    }
}