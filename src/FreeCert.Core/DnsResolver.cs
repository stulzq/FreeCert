using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Makaretu.Dns;

namespace FreeCert.Core
{
    public class DnsResolver
    {
        /// <summary>
        /// Query TXT records for specified domain names
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public async Task<List<string>> QueryTxtRecord(string domain)
        {
            var dns = new DnsClient();
            var response = await dns.QueryAsync(domain, DnsType.TXT);
            var strings = response.Answers
                .OfType<TXTRecord>()
                .SelectMany(txt => txt.Strings);
            return strings.ToList();
        }
    }
}