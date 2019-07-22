using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DarkV.Extension.Bases;
using DarkV.Extension.Json;
using DarkV.Extension.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DarkV.Extension.Collections;
using JZPay.Core.RPC;
using JZPay.Core.Services;
using JZPay.Core;

namespace JZPay.SDKService.Controllers.V1
{
    [APIV1("[controller]")]
    [ApiController]
    public class DCPayController : Controller
    {
        protected static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        
        [HttpGet]
        public ResultInfo Test() => ResultInfo.OK;


        [HttpPost("createOrder/{platform}/{authcode}")]
        public ResultInfo<OrderResponseInfo> CreateOrder(string platform, string authcode, [FromBody]OrderRequestInfo request)
        {
            try
            {
                return DCPayHelper.CreateOrder(request, platform, authcode);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return new ResultInfo<OrderResponseInfo> { Code = -1000, Message = ex.ToString() };
            }
        }

        [HttpPost("queryOrder/{platform}/{authcode}/{orderid}")]
        [HttpGet("queryOrder/{platform}/{authcode}/{orderid}")]
        public ResultInfo<OrderInfo> QueryOrder(string platform, string authcode, string orderid)
        {
            try
            {
                return DCPayHelper.QueryOrder(orderid, platform, authcode);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return new ResultInfo<OrderInfo> { Code = -1000, Message = ex.ToString() };
            }
        }

        [HttpPost("queryOrderByJOrderId/{platform}/{authcode}/{orderid}")]
        [HttpGet("queryOrderByJOrderId/{platform}/{authcode}/{orderid}")]
        public ResultInfo<OrderInfo> QueryOrderByJOrderId(string platform, string authcode, string orderid)
        {
            try
            {
                return DCPayHelper.QueryOrderByJOrderId(orderid, platform, authcode);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return new ResultInfo<OrderInfo> { Code = -1000, Message = ex.ToString() };
            }
        }
    }
}
