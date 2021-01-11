using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ametatsu.StatusLightClient.Services
{
    public class HomeAssistantService
    {
        private readonly string _baseUrl;
        private readonly string _authToken;

        private readonly RestClient _restClient;

        public HomeAssistantService(string baseUrl, string authToken)
        {
            _baseUrl = baseUrl;
            _authToken = authToken;

            _restClient = new RestClient(baseUrl);
            _restClient.AddDefaultHeader("Authorization", $"Bearer {_authToken}");
        }

        public bool UpdateInputTextValue(string entityId, string value)
        {
            var request = new RestRequest($"/api/services/input_text/set_value", Method.POST);
            request.AddJsonBody(new { entity_id = entityId, option = value });

            var response = _restClient.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK) return true;

            return false;
        }

        public bool UpdateInputSelectValue(string entityId, string value)
        {
            var request = new RestRequest($"/api/services/input_select/select_option", Method.POST);
            request.AddJsonBody(new { entity_id = entityId, option = value });

            var response = _restClient.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK) return true;

            return false;
        }
    }
}
