using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections;
using System.Runtime.CompilerServices;

namespace VertMarkets
{

    internal class Program
    {

        private static HttpClient _httpClient = new HttpClient();

        public static async Task Main(string[] args)
        {
            _httpClient.BaseAddress = new Uri("http://magazinestore.azurewebsites.net/");
            _httpClient.Timeout = new TimeSpan(0, 0, 30);
            _httpClient.DefaultRequestHeaders.Clear();

            var token = await GetToken();
            var subscribers = await GetSubscribers(token.Token);
            var categories = await GetCategories(token.Token);


            Dictionary<string, List<int?>> magazinesInCategories = new Dictionary<string, List<int?>>();
            List<string> listOfCategories = new List<string>();

            foreach (var cat in categories.Data)
            {
                listOfCategories.Add(cat);

                var magazineData = await GetMagazinesPerCategory(token.Token, cat);

                List<int?> categoryList = new List<int?>();

                foreach (var magazine in magazineData.Data)
                {
                    categoryList.Add(magazine.Id);
                }

                magazinesInCategories.Add(cat, categoryList);
            }

            var match = false;

            List<Guid> completeMatchIds = new List<Guid>();

            foreach (var sub in subscribers.Data)
            {
                List<string> matchedCategories = new List<string>();

                foreach (var id in sub.MagazineIds)
                {
                    foreach (var mc in magazinesInCategories)
                    {
                        match = mc.Value.Contains(id);

                        if (match)
                        {
                            matchedCategories.Add(mc.Key);
                        }
                    }
                }

                int catCount = matchedCategories.Distinct().Count();

                if (listOfCategories.Count == catCount)
                {
                    completeMatchIds.Add((Guid)sub.Id);
                }

            }

            await SendAnswers(token.Token, completeMatchIds);

        }

        public static async Task<ApiResponse> GetToken()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/token");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var tokens = JsonSerializer.Deserialize<ApiResponse>(content,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }
                );

            return tokens;
        }

        public static async Task<ApiResponseOfListOfApiSubscriber> GetSubscribers(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/subscribers/" + token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<ApiResponseOfListOfApiSubscriber>(content,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }
                    );

            return subscribers;
        }

        public static async Task<ApiResponseOfListOfString> GetCategories(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/categories/" + token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var categories = JsonSerializer.Deserialize<ApiResponseOfListOfString>(content,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }
                    );

            return categories;
        }

        public static async Task<ApiResponseOfListOfMagazine> GetMagazinesPerCategory(string token, string category)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/magazines/" + token + "/" + category);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var magazines = JsonSerializer.Deserialize<ApiResponseOfListOfMagazine>(content,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }
                    );

            return magazines;
        }

        public static async Task SendAnswers(string token, List<Guid> subscriberIds)
        {

            var idsToSend = new Answer()
            {
                Subscribers = subscriberIds
            };

            var serializeIds = JsonSerializer.Serialize(idsToSend);

            var request = new HttpRequestMessage(HttpMethod.Post, "api/answer/" + token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(serializeIds);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var createdIds = JsonSerializer.Deserialize<ApiResponseOfAnswerResponse>(content,
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            }
                      );

            Console.WriteLine(createdIds.Data.AnswerCorrect);
            Console.WriteLine(createdIds.Data.TotalTime);
            Console.WriteLine(createdIds.Success);
            Console.WriteLine(createdIds.Message);
        }

    }

}