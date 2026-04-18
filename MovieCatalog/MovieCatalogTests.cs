using MovieCatalog.DTOs;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;


namespace MovieCatalog
{

    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJkN2JiOGZhNC01MWIwLTRkZTgtYWNlYi1jNGVkOGJkYmQ5NGIiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjUyOjMyIiwiVXNlcklkIjoiYzcxN2YyMzQtZDI0My00MjJiLTYyODUtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJib3pvdmEyMDI2QHRlc3QuY29tIiwiVXNlck5hbWUiOiJib3pvdmEyMDI2IiwiZXhwIjoxNzc2NTE2NzUyLCJpc3MiOiJNb3ZpZUNhdGFsb2dfQXBwX1NvZnRVbmkiLCJhdWQiOiJNb3ZpZUNhdGFsb2dfV2ViQVBJX1NvZnRVbmkifQ.LYBTuJWRGgmv0JXanz8Tah6FlZrPtzakGOBnQu1Pjdo";
        private const string LoginEmail = "bozova2026@test.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
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

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
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
                throw new InvalidOperationException($"Failed to authenticate.Status code: {response.StatusCode}, Response: {response.ResponseStatus}");
            }
        }
        [Order(1)]
        [Test]
        public void CreateNewMovie_WithRequiredFields_ShouldReturnSuccess()
        {
            //създаваме данни, с които да пратим request
            var movieData = new MovieDto
            {
                Id = "1",
                Title = "A few good men",
                Description = "This is a movie with Jack Nicholson."
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = this.client.Execute(request);

            //десериализираме json-a

            var createResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            //Assert.That(a movie object is returned in the response)
            Assert.IsNotNull(createResponse.Movie);
            Assert.That(createResponse.Movie.Id, Is.Not.Null.Or.Empty);
            Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"));

            lastCreatedMovieId = createResponse.Movie.Id;
        }

        [Order(2)]
        [Test]

        public void EditExistingMovie_ShouldReturnSuccess()
        {
            var editRequestData = new MovieDto
            {
                Id = "Edited id",
                Title = "this is an edited title",
                Description = "This is an edited description."
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);

            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));

        }
        [Order(3)]
        [Test]

        public void GetAllMovies_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<MovieDto>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItems, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);
        }

        [Order(4)]
        [Test]

        public void DeleteCreatedMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            var response = this.client.Execute(request);

            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected Statuscode 200 OK.");
            Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]

        public void CreateMovie_WithMissingRequiredFields_ShouldReturnBadRequest()
        {
            var movieData = new MovieDto
            {
                Id = "100",
                Title = "",
                Description = ""
            };
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

        }

        [Order(6)]
        [Test]

        public void EditNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "1001";
            var editRequestData = new MovieDto
            {
                Id = nonExistingMovieId, 
                Title = "Valid Title",
                Description = "Valid Description",
            };
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected Statuscode 400 Bad Request.");
            Assert.That(editResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "1111";

            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            var response = this.client.Execute(request);

            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected Statuscode 400 Bad Request.");
            Assert.That(deleteResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]

        public void TearDown()
        {
            //clean up resources if needed
            this.client.Dispose();
        }

    }
}
