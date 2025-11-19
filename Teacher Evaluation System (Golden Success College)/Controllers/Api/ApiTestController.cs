using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    public class ApiTestController : Controller
    {
        private readonly HttpClient _httpClient;

        public ApiTestController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> TestRole(int id)
        {
            // Call your API endpoint
            var response = await _httpClient.GetAsync($"http://localhost:5050/Home/Index/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return Content($"API returned {response.StatusCode}");
            }

            var role = await response.Content.ReadFromJsonAsync<Role>();
            return View(role); // Or use Content(role.Name) to just display the name
        }
    }
}
