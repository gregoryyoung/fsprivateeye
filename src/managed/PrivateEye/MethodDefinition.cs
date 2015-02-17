using System.Reflection;

namespace PrivateEye
{
    public class MethodDefinition
    {
        public MethodInfo MethodInfo;
        public string Name;
        internal uint ModuleId;
        internal uint Token;
    }
}