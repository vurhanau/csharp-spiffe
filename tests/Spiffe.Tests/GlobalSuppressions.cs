// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("ErrorHandling", "EPC12:Suspicious exception handling: only the 'Message' property is observed in the catch block", Justification = "Test code", Scope = "member", Target = "~M:Spiffe.Tests.Integration.TestIntegration.RunTest(System.Func{System.String})~System.Threading.Tasks.Task")]
