using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;

namespace FreeCert.Core
{
    public class DnsResolver
    {
        private readonly ILogger<DnsResolver> _logger;

        public DnsResolver(ILogger<DnsResolver> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Query TXT records for specified domain names
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public async Task<List<string>> QueryTxtRecord(string domain)
        {
            var dns = new DnsClient();
            try
            {

                var response = await dns.QueryAsync(domain, DnsType.TXT);
                var strings = response.Answers
                    .OfType<TXTRecord>()
                    .SelectMany(txt => txt.Strings);
                return strings.ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Dns resolve error.");
            }
            return new List<string>();
        }
    }
}