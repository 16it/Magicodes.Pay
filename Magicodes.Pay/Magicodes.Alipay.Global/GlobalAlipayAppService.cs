﻿// ======================================================================
//   
//           Copyright (C) 2018-2020 湖南心莱信息科技有限公司    
//           All rights reserved
//   
//           filename : GlobalAlipayAppService.cs
//           description :
//   
//           created by 雪雁 at  2018-11-23 10:00
//           Mail: wenqiang.li@xin-lai.com
//           QQ群：85318032（技术交流）
//           Blog：http://www.cnblogs.com/codelove/
//           GitHub：https://github.com/xin-lai
//           Home：http://xin-lai.com
//   
// ======================================================================

using Magicodes.Alipay.Global.Dto;
using Magicodes.Alipay.Global.Extension;
using Magicodes.Alipay.Global.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Magicodes.Alipay.Global
{
    /// <inheritdoc />
    public class GlobalAlipayAppService : IGlobalAlipayAppService
    {
        private readonly IGlobalAlipaySettings _alipaySettings;

        public GlobalAlipayAppService() => _alipaySettings = GetPayConfigFunc();

        public static Action<string, string> LoggerAction { get; set; }
        public static Func<IGlobalAlipaySettings> GetPayConfigFunc { get; set; }

        /// <inheritdoc />
        /// <summary>
        ///     网站支付
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task<PayOutput> Pay(PayInput input)
        {
            //把请求参数打包成数组
            var sParaTemp = new SortedDictionary<string, string>
            {
                {"service", "create_forex_trade"},
                {"partner", _alipaySettings.Partner},
                {"_input_charset", _alipaySettings.CharSet.ToLower()},
                {"sign_type", _alipaySettings.SignType},
                {"notify_url", input.NotifyUrl ?? _alipaySettings.Notify},
                {"return_url", input.ReturnUrl ?? _alipaySettings.ReturnUrl},
                {"currency", input.Currency ?? _alipaySettings.Currency},
                {"out_trade_no", input.TradeNo ?? Guid.NewGuid().ToString("N")},
                {"subject", input.Subject},
                {"body", input.Body}
            };
            if (input.RmbFee > 0)
            {
                sParaTemp.Add("rmb_fee", input.RmbFee.ToString());
            }
            else
            {
                sParaTemp.Add("total_fee", input.TotalFee.ToString());
            }

            if (!string.IsNullOrWhiteSpace(input.TimeoutRule))
            {
                sParaTemp.Add("timeout_rule", input.TimeoutRule);
            }

            if (!string.IsNullOrWhiteSpace(input.AuthToken))
            {
                sParaTemp.Add("auth_token", input.AuthToken);
            }

            if (!string.IsNullOrWhiteSpace(input.Supplier))
            {
                sParaTemp.Add("supplier", input.Supplier);
            }

            if (!string.IsNullOrWhiteSpace(input.SecondaryMerchantId))
            {
                sParaTemp.Add("secondary_merchant_id", input.SecondaryMerchantId);
            }

            if (!string.IsNullOrWhiteSpace(input.SecondaryMerchantName))
            {
                sParaTemp.Add("secondary_merchant_name", input.SecondaryMerchantName);
            }

            if (!string.IsNullOrWhiteSpace(input.SecondaryMerchantIndustry))
            {
                sParaTemp.Add("secondary_merchant_industry", input.SecondaryMerchantIndustry);
            }

            if (input.OrderGmtCreate.HasValue)
            {
                sParaTemp.Add("order_gmt_create", input.OrderGmtCreate.Value.ToString("yyyy-MM-dd hh:mm:ss"));
            }

            if (input.OrderValidTime.HasValue && input.OrderValidTime > 0)
            {
                sParaTemp.Add("order_valid_time", input.OrderValidTime.Value.ToString());
            }

            if (input.SplitFundInfo != null && input.SplitFundInfo.Count > 0)
            {
                foreach (var splitFundInfoDto in input.SplitFundInfo)
                {
                    if (input.RmbFee > 0)
                    {
                        splitFundInfoDto.Currency = "CNY";
                    }
                    else
                    {
                        splitFundInfoDto.Currency = input.Currency ?? _alipaySettings.Currency;
                    }
                }
                //分账信息
                sParaTemp.Add("split_fund_info", Newtonsoft.Json.JsonConvert.SerializeObject(input.SplitFundInfo));
            }

            //过滤签名参数数组
            sParaTemp.FilterPara();
            var dic = sParaTemp.BuildRequestPara(_alipaySettings);
            var html = dic.GetHtmlSubmitForm(_alipaySettings.Gatewayurl, _alipaySettings.CharSet);
            return Task.FromResult(new PayOutput
            {
                FormHtml = html,
                Parameters = dic
            });
        }

        /// <summary>
        ///     支付回调
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public bool PayNotifyHandler(Dictionary<string, string> dic)
        {
            try
            {
                var sArray = new SortedDictionary<string, string>();
                foreach (var item in dic)
                {
                    sArray.Add(item.Key, item.Value);
                }

                var aliNotify = new NotifyHelper();
                var verifyResult = aliNotify.Verify(sArray, dic["notify_id"], dic["sign"], _alipaySettings);

                return verifyResult;
            }
            catch (Exception e)
            {
                LoggerAction?.Invoke("Error", e.Message);
                return false;
            }
        }
    }
}