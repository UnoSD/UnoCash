using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using UnoCash.Core;

namespace UnoCash.Api
{
    static class AspNetExtensions
    {
        internal static IResult<Guid> ExtractSingleGuidValue(this IQueryCollection col, string key) =>
            col.ExtractSingleStringValue(key)
               .Bind(value => Guid.TryParse(value, out var guid)
                                  .Match(() => guid.Success(),
                                         () => $"Could not parse the GUID: {value}".Failure<Guid>()));

        internal static IResult<string> ExtractSingleStringValue(this IQueryCollection col, string key) =>
            col.TryGetValue(key, out var stringValues)
               .Match(() => stringValues.Success(),
                      () => $"Cannot find query parameter {key}".Failure<StringValues>())
               .Bind(values => values.Match(() => $"Missing value for {key}".Failure<string>(),
                                            value => value.Success(),
                                            _ => $"Too many values for {key}".Failure<string>()));

        public static OkObjectResult ToOkObject<T>(this T obj) =>
            new OkObjectResult(obj);
    }
}