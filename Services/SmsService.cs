using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BusTicketing.Services
{
    public class SmsService
    {
        private readonly string _authUrl = "https://sms.ekwacha.com/apis/auth";
        private readonly string _sendUrl = "https://sms.ekwacha.com/apis/sms/mt/v2/send";

        private readonly string _username = "ekalenga@fecomastechsolutions.com";
        private readonly string _password = "Ekb@com11";

        public async Task<string> GetAccessTokenAsync()
        {
            using (HttpClient client = GetUnsafeHttpClient())
            {
                var requestBody = new
                {
                    type = "access_token",
                    username = _username,
                    password = _password
                };

                string json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(_authUrl, content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic jsonResponse = JsonConvert.DeserializeObject(result);
                    return jsonResponse?.payload?.access_token ?? "";
                }

                return null;
            }
        }

        public async Task<string> SendSmsAsync(string accessToken, string[] phoneNumbers, string message)
        {
            if (phoneNumbers == null || phoneNumbers.Length == 0 || string.IsNullOrWhiteSpace(message))
                return "Phone numbers and message cannot be empty.";

            using (HttpClient client = GetUnsafeHttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var smsPayload = new[]
                {
                    new
                    {
                        to = phoneNumbers,
                        from = "FECOMAS",
                        message = message
                    }
                };

                string json = JsonConvert.SerializeObject(smsPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(_sendUrl, content);
                return await response.Content.ReadAsStringAsync();
            }
        }

        private HttpClient GetUnsafeHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            return new HttpClient(handler);
        }
    }
}