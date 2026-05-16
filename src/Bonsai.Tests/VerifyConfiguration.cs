using System.Runtime.CompilerServices;
using VerifyTests;
using VerifyXunit;

namespace Bonsai.Tests
{
    public static class VerifyConfiguration
    {
        [ModuleInitializer]
        public static void Init()
        {
            Verifier.DerivePathInfo(Expectations.Initialize);
        }
    }
}