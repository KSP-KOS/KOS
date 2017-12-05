using System;
using System.Reflection;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public abstract class SuffixBase : ISuffix
    {
        protected SuffixBase(string description)
        {
            Description = description;
        }

        public virtual ISuffixResult Get()
        {
            return new DelegateSuffixResult(DelegateInfo(), Call);
        }

        private DelegateInfo delegateInfo;

        private DelegateInfo DelegateInfo()
        {
            if (delegateInfo != null)
            {
                return delegateInfo;
            }

            Delegate del = Delegate;

            MethodInfo methInfo = del.Method;
            ParameterInfo[] paramInfos = methInfo.GetParameters();

            DelegateParameter[] delParams = new DelegateParameter[paramInfos.Length];
            for (int i = 0; i < paramInfos.Length; i++)
            {
                delParams[i] = new DelegateParameter()
                {
                    ParameterType = paramInfos[i].ParameterType,
                    IsParams = (i == paramInfos.Length - 1) ? Attribute.IsDefined(paramInfos[i], typeof(ParamArrayAttribute)) : false
                };
            }

            delegateInfo = new DelegateInfo() { Name = methInfo.Name, ReturnType = methInfo.ReturnType, Parameters = delParams };

            return delegateInfo;
        }

        protected abstract Delegate Delegate { get; }

        protected abstract object Call(object[] args);

        public string Description { get; private set; }
    }

    public class DelegateInfo
    {
        public string Name { get; set; }
        public Type ReturnType { get; set; }
        public DelegateParameter[] Parameters { get; set; }
    }

    public class DelegateParameter
    {
        public Type ParameterType { get; set; }
        public bool IsParams { get; set; }
    }
}