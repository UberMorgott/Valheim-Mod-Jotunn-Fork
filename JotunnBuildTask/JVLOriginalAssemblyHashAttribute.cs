using System;

namespace JotunnBuildTask
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class JVLOriginalAssemblyHashAttribute : Attribute
    {
        public string hash;

        public JVLOriginalAssemblyHashAttribute(string hash)
        {
            this.hash = hash;
        }
    }
}

