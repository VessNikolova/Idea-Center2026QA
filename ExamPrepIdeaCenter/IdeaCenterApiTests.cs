using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework.Constraints;
using RestSharp;
using RestSharp.Authenticators;
using ExamPrepIdeaCenter.Models;


namespace ExamPrepIdeaCenter
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://144.91.123.158:82";
        private const string Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI2NmY1MWJhNi01NjQyLTQ3ZWUtYjY3OS03OWJiMmE3MWQ5ODAiLCJpYXQiOiIwNC8xMy8yMDI2IDE0OjA3OjM5IiwiVXNlcklkIjoiNTg4OGZhOTQtMjlkMy00YWVjLTUzNjUtMDhkZTc2YTJkM2VjIiwiRW1haWwiOiJ2ZXMxMjNAYWJ2LmJnIiwiVXNlck5hbWUiOiJWZXNzTmlrb2xvdmEiLCJleHAiOjE3NzYxMTA4NTksImlzcyI6IklkZWFDZW50ZXJfQXBwX1NvZnRVbmkiLCJhdWQiOiJJZGVhQ2VudGVyX1dlYkFQSV9Tb2Z0VW5pIn0.aSH9xTAbOsVzg3SIgAYsKeg5pSKHQ089bob25mPNFw4";
        private const string LoginEmail = "ves123@abv.bg";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(Token))
            {
                jwtToken = Token;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string loginEmail, string loginPassword)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { loginEmail,loginPassword });

            var response = tempClient.Execute(request);

            if(response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }

                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Respose: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateNewIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaData = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test description.",
                Url = "",
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);

            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));

        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnSuccess()
        {
           
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            //Get all items in array
            var responseItem = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItem, Is.Not.Null);
            Assert.That(responseItem, Is.Not.Empty);

            lastCreatedIdeaId = responseItem.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]
        public void EditExistingIdea_ShouldReturnSuccess()
        {
            var editRequestData = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is a edited description.",
                Url = "",
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));

        }

        [Order(4)]
        [Test]
        public void DeleteExistingIdea_ShouldReturnSuccess()
        {  
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
          
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(response.Content, Is.EqualTo("\"The idea is deleted!\""));
        }

        [Order(5)]
        [Test]
        public void CreateNewIdea_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var ideaData = new IdeaDTO
            {
                Title = "",
                Description = "",
                Url = "",
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);

            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

        }

        [Order(6)]
        [Test]
        public void EditNonExistingIdea__ShouldReturnBadRequest()
        {
            var nonExitingIdeaId = "99999999";
            var editRequestData = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "this is an edited description.",
                Url = "",
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExitingIdeaId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingIdea_ShouldReturnBadRequest()
        {
            var nonExitingIdeaId = "99999999";

            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExitingIdeaId);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
