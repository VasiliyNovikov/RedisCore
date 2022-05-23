#if !NETCOREAPP3_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Style", "IDE0130: Namespace does not match folder structure",
                           Justification = "It is intentional for cross platfrom extension methods")]
#endif