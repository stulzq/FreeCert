using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using FreeCert.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace FreeCert.Core
{
    public class FreeCertBuilder
    {
        private readonly string _workDir;
        private readonly ILogger _logger;
        private readonly bool _debug;
        private readonly List<string> _accounts=new List<string>();
        private readonly List<string> _domains=new List<string>();
        private string _accountKey;
        private Uri _orderUri;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="acceptTos">Let's Encrypt Terms of Service. https://letsencrypt.org/documents/LE-SA-v1.2-November-15-2017.pdf </param>
        /// <param name="workDir"></param>
        /// <param name="logger"></param>
        /// <param name="debug">When true will use Let's Encrypt staging environment, which may result in rate constraints if used directly in a production environment</param>
        public FreeCertBuilder(bool acceptTos,string workDir,ILogger logger, bool debug = false)
        {
            if (!acceptTos)
            {
                throw new FreeCertException("As you do not accept the Let's Encrypt terms of service, will not be able to continue working.");
            }

            _workDir = workDir;
            _logger = logger;
            _debug = debug;

            if (!Directory.Exists(_workDir))
            {
                Directory.CreateDirectory(_workDir);
            }
        }

        public async Task<string> GetTosUriAsync()
        {
            return (await new AcmeContext(WellKnownServers.LetsEncryptV2).TermsOfService()).ToString();
        }

        /// <summary>
        /// Create a new account
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public FreeCertBuilder AddNewAccount(string email)
        {
            _accounts.Add(email);
            return this;
        }

        /// <summary>
        /// Create a new account with multiple email address.
        /// </summary>
        /// <param name="emails"></param>
        /// <returns></returns>
        public FreeCertBuilder AddNewAccount(string[] emails)
        {
            _accounts.AddRange(emails);
            return this;
        }

        /// <summary>
        /// Load exists account.
        /// </summary>
        /// <returns></returns>
        public FreeCertBuilder LoadAccount(string accountKey)
        {
            _accountKey = accountKey;
            return this;
        }

        /// <summary>
        /// Load exists order.
        /// </summary>
        /// <returns></returns>
        public FreeCertBuilder LoadOrder(Uri orderUri)
        {
            _orderUri = orderUri;
            return this;
        }

        /// <summary>
        /// Add domain
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public FreeCertBuilder SetDomain(string domain)
        {
            _domains.Add(domain);
            return this;
        }

        /// <summary>
        /// Add domain
        /// </summary>
        /// <param name="domains"></param>
        /// <returns></returns>
        public FreeCertBuilder SetDomains(string[] domains)
        {
            _domains.AddRange(domains);
            return this;
        }

        public string GetAccountKey()
        {
            return _accountKey;
        }

        public Uri GetOrderUri()
        {
            return _orderUri;
        }

        public string GetWorkDir()
        {
            return _workDir;
        }

        public async Task<FreeCertContext> BuildAsync()
        {
            IAcmeContext acmeContext;
            IAccountContext account;
            IOrderContext order;

            //select server
            var acmeServer = _debug ? WellKnownServers.LetsEncryptStagingV2 : WellKnownServers.LetsEncryptV2;

            //Create account
            if (string.IsNullOrEmpty(_accountKey))
            {

                if (_domains.Count == 0)
                {
                    throw new FreeCertException("No domain is available.");
                }

                acmeContext = new AcmeContext(acmeServer);
                
                if (_accounts.Count == 0)
                {
                    throw new FreeCertException("No email address is available and no account can be created.");
                }
                else if(_accounts.Count==1)
                {
                    account = await acmeContext.NewAccount(_accounts[0], true);
                }
                else
                {
                    var accountList = _accounts.Select(a => $"mailto:{a}").ToList();
                    account = await acmeContext.NewAccount(accountList, true);
                }
                _logger.LogInformation("Account created.");

                //存储新创建用户AccountKey
                _accountKey = acmeContext.AccountKey.ToPem();

                //创建新订单
                order = await acmeContext.NewOrder(_domains);
                _logger.LogInformation("Order created.");
            }
            else
            {
                acmeContext = new AcmeContext(acmeServer, KeyFactory.FromPem(_accountKey));
                //从服务器拉取账号信息
                account = await acmeContext.Account();
                _logger.LogInformation("Account loaded.");

                //Load order
                if (_orderUri != null)
                {
                    order = acmeContext.Order(_orderUri);
                    _logger.LogInformation("Order loaded.");
                }
                else
                {
                    order = await acmeContext.NewOrder(_domains);
                    _logger.LogInformation("Created loaded.");
                }
            }

            _orderUri = order.Location;

            return new FreeCertContext(acmeContext,account,order, _workDir);
        }
    }
}