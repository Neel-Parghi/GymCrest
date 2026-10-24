using System;

namespace GymCrest.Api.Middlewares
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipResponseWrapperAttribute : Attribute
    {
    }
}
