using System;
using Microsoft.AspNetCore.Mvc;

namespace JZPay.SDKService.Controllers.V1
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class APIV1Attribute : RouteAttribute
    {
        public const string V1 = "api/v1/";

        public APIV1Attribute(string t) 
            : base(V1 + t)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class HttpGetCompatibleAttribute : HttpGetAttribute
    {
        public HttpGetCompatibleAttribute(string t)
            : base(t)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class HttpPostCompatibleAttribute : HttpPostAttribute
    {
        public HttpPostCompatibleAttribute()
        {

        }

        public HttpPostCompatibleAttribute(string t)
            : base(t)
        {

        }
    }
}
