using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IT15_DairyFlow.Services
{
    public class PayMongoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _secretKey;
        private readonly string _publicKey;
        private readonly ILogger<PayMongoService> _logger;

        public PayMongoService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PayMongoService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _secretKey = configuration["PayMongo:SecretKey"] ?? throw new InvalidOperationException("PayMongo SecretKey not configured.");
            _publicKey = configuration["PayMongo:PublicKey"] ?? throw new InvalidOperationException("PayMongo PublicKey not configured.");
            _logger = logger;
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("PayMongo");
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_secretKey}:"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        /// <summary>
        /// Creates a PayMongo Checkout Session for the given payment method type.
        /// </summary>
        /// <param name="email">Customer email</param>
        /// <param name="description">Plan description</param>
        /// <param name="amountInCentavos">Amount in centavos (PHP smallest unit)</param>
        /// <param name="paymentMethodType">card, gcash, or paymaya</param>
        /// <param name="successUrl">URL to redirect on success</param>
        /// <param name="cancelUrl">URL to redirect on cancel</param>
        /// <returns>Tuple of checkout URL and session ID</returns>
        public async Task<(string checkoutUrl, string sessionId)?> CreateCheckoutSession(
            string email,
            string description,
            int amountInCentavos,
            string paymentMethodType,
            string successUrl,
            string cancelUrl)
        {
            try
            {
                using var client = CreateClient();

                var payload = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            billing = new
                            {
                                email,
                                name = email.Split('@')[0]
                            },
                            send_email_receipt = false,
                            show_description = true,
                            show_line_items = true,
                            description,
                            line_items = new[]
                            {
                                new
                                {
                                    currency = "PHP",
                                    amount = amountInCentavos,
                                    name = description,
                                    quantity = 1
                                }
                            },
                            payment_method_types = new[] { paymentMethodType },
                            success_url = successUrl,
                            cancel_url = cancelUrl
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.paymongo.com/v1/checkout_sessions", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMongo API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseBody);
                var data = doc.RootElement.GetProperty("data");
                var sessionId = data.GetProperty("id").GetString()!;
                var checkoutUrl = data.GetProperty("attributes").GetProperty("checkout_url").GetString()!;

                _logger.LogInformation("PayMongo Checkout Session created: {SessionId}", sessionId);

                return (checkoutUrl, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create PayMongo checkout session");
                return null;
            }
        }

        /// <summary>
        /// Verifies that a checkout session has been paid.
        /// </summary>
        public async Task<bool> VerifyPayment(string sessionId)
        {
            try
            {
                using var client = CreateClient();

                var response = await client.GetAsync($"https://api.paymongo.com/v1/checkout_sessions/{sessionId}");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMongo verify error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return false;
                }

                using var doc = JsonDocument.Parse(responseBody);
                var data = doc.RootElement.GetProperty("data");
                var attributes = data.GetProperty("attributes");

                // Check if payments array has entries
                if (attributes.TryGetProperty("payments", out var payments) && payments.GetArrayLength() > 0)
                {
                    return true;
                }

                // Check payment_intent status
                if (attributes.TryGetProperty("payment_intent", out var paymentIntent))
                {
                    var piAttributes = paymentIntent.GetProperty("attributes");
                    var status = piAttributes.GetProperty("status").GetString();
                    if (status == "succeeded")
                        return true;
                }

                // Fallback: check checkout session status
                var sessionStatus = attributes.GetProperty("status").GetString();
                return sessionStatus == "paid" || sessionStatus == "completed" || sessionStatus == "active";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify PayMongo payment for session {SessionId}", sessionId);
                return false;
            }
        }
    }
}
