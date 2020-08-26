using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Pkcs;
using DynamicDns.Core;
using FreeCert.Core.Models;

namespace FreeCert.Core
{
    public class FreeCertContext
    {
        public IAcmeContext AcmeContext { get; }
        public IOrderContext OrderContext { get; }
        public IAccountContext AccountContext { get; }

        /// <summary>
        /// All new
        /// </summary>
        /// <param name="acmeContext"></param>
        /// <param name="accountContext"></param>
        /// <param name="orderContext"></param>
        public FreeCertContext(IAcmeContext acmeContext,IAccountContext accountContext, IOrderContext orderContext)
        {
            AcmeContext = acmeContext;
            AccountContext = accountContext;
            OrderContext = orderContext;
        }

        public async Task<AcmeOrderInfo> GetOrderInfoAsync()
        {
            var order = await OrderContext.Resource();
            var orderInfo = new AcmeOrderInfo
            {
                Domains = order.Identifiers.Select(a => a.Value).ToList(),
                Expires = order.Expires?.ToLocalTime().DateTime,
                Status = order.Status.ToString()
            };
            return orderInfo;
        }

        public async Task<AcmeAccountInfo> GetAccountAsync()
        {
            var account = await AccountContext.Resource();
            
            var accountInfo = new AcmeAccountInfo
            {
                Status = account.Status.ToString(),
                Contacts = account.Contact.Select(a=>a).ToList(),
                AcceptTos = account.TermsOfServiceAgreed,
            };
            return accountInfo;
        }

        public async Task<List<AcmeDnsAuthorizationInfo>> GetAuthorizationsAsync()
        {
            var result=new List<AcmeDnsAuthorizationInfo>();
            foreach (var authorization in await OrderContext.Authorizations())
            {
                var challengeContext = await authorization.Dns();
                var dnsTxt = AcmeContext.AccountKey.DnsTxt(challengeContext.Token);

                var challenge = await challengeContext.Resource();
                var domain = await GetTopDomainAsync();
                var authInfo=new AcmeDnsAuthorizationInfo()
                {
                    Record = $"{_challengeSubDomain}.{domain}",
                    RecordType = "TXT",
                    Value = dnsTxt,
                    Status = challenge.Status.ToString()
                };
                result.Add(authInfo);
            }
            return result;
        }

        public async Task<AutoCreateDnsRecordResult> AutoCreateDnsRecord(IDynamicDns ddns)
        {
            if (ddns == null)
            {
                throw new ArgumentNullException(nameof(ddns));
            }
            var authInfo = await GetAuthorizationsAsync();
            var domain = await GetTopDomainAsync();

            await ddns.DeleteAsync(domain, _challengeSubDomain);
            foreach (var item in authInfo)
            {
                var opRes = await ddns.AddAsync(domain, _challengeSubDomain, "TXT", item.Value);
                if (opRes.Error)
                {
                    return new AutoCreateDnsRecordResult(false,opRes.Message);
                }
            }

            return new AutoCreateDnsRecordResult(true);
        }

        public async Task<List<string>> GetDnsTxtRecordAsync()
        {
            var domain = await GetTopDomainAsync();
            var records = await new DnsResolver().QueryTxtRecord($"{_challengeSubDomain}.{domain}");
            return records;
        }

        public async Task<bool> CheckDnsTxtRecordAsync()
        {
            var records = await GetDnsTxtRecordAsync();
            var authInfo = await GetAuthorizationsAsync();

            foreach (var item in authInfo)
            {
                if (!records.Contains(item.Value))
                {
                    return false;
                }
            }
            return true;
        }

        public async Task AuthorizationAsync()
        {
            foreach (var authorization in await OrderContext.Authorizations())
            {
                var challengeContext = await authorization.Dns();
                await challengeContext.Validate();
            }
        }

        public async Task OrderFinalizeAsync()
        {
            var order = await GetOrderInfoAsync();
            var topDomain = await GetTopDomainAsync();

            var csrBuilder = new CertificationRequestBuilder();

            csrBuilder.AddName($"C=Country, ST=State, L=City, O=Org, CN={topDomain}");

            //setup the san if necessary
            csrBuilder.SubjectAlternativeNames = order.Domains.Where(a => a != topDomain).ToList();

            byte[] csrByte = csrBuilder.Generate();

            await OrderContext.Finalize(csrByte);

            File.WriteAllText($"{FreeCertConsts.WorkDir}/{topDomain}-{FreeCertConsts.CertPemPrivateKeyName}", csrBuilder.Key.ToPem(),
                Encoding.UTF8);
        }

        public async Task ExportCertAsync(string password)
        {
            var topDomain = await GetTopDomainAsync();

            var cert = await OrderContext.Download();
            //pem证书
            File.WriteAllText($"{FreeCertConsts.WorkDir}/{topDomain}-{FreeCertConsts.CertPemName}", cert.ToPem(),
                Encoding.UTF8);

            var privateKey= File.ReadAllText($"{FreeCertConsts.WorkDir}/{topDomain}-{FreeCertConsts.CertPemPrivateKeyName}",
                Encoding.UTF8);

            //pfx证书
            var pfxBuilder = cert.ToPfx(KeyFactory.FromPem(privateKey));
            var pfx = pfxBuilder.Build(topDomain, password);
            File.WriteAllBytes($"{FreeCertConsts.WorkDir}/{topDomain}{FreeCertConsts.CertPfxName}",pfx);
            File.WriteAllText($"{FreeCertConsts.WorkDir}/{topDomain}{FreeCertConsts.CertPfxPasswordName}", password);
        }


        private string _topDomain;
        private string _challengeSubDomain = "_acme-challenge";
        private async Task<string> GetTopDomainAsync()
        {
            if (string.IsNullOrEmpty(_topDomain))
            {
                var orderInfo = await GetOrderInfoAsync();

                var tempArray = orderInfo.Domains.First().Split(new[] { '.' });
                var topDomain = $"{tempArray[tempArray.Length - 2]}.{tempArray[tempArray.Length - 1]}";
                _topDomain = topDomain;
            }

            return _topDomain;
        }
    }
}