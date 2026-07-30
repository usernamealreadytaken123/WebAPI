using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WebApplication1.Contracts.Results;
using WebApplication1.Contracts.Values;

namespace WebApplication1.Tests.Integration;

public sealed class WebApiTests
{
    [Fact]
    public async Task UploadAndGetMethods_ReturnExpectedData_ForValidCsv()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();

        using var uploadResponse = await UploadAsync(
            client,
            "integration.csv",
            BuildCsv(
                "2024-01-10T10:02:00Z;3;30",
                "2024-01-10T10:00:00Z;1;10",
                "2024-01-10T10:01:00Z;2;20"));

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        using var uploadJson = JsonDocument.Parse(
            await uploadResponse.Content.ReadAsStringAsync());
        Assert.True(uploadJson.RootElement.GetProperty("resultId").GetInt64() > 0);
        Assert.Equal(
            3,
            uploadJson.RootElement.GetProperty("dataRowCount").GetInt32());

        var results = await client.GetFromJsonAsync<List<ResultResponse>>(
            "/api/results?fileName=integration.csv");

        Assert.NotNull(results);
        var result = Assert.Single(results);
        Assert.Equal(120, result.TimeDeltaSeconds);
        Assert.Equal(2, result.AverageExecutionTime);
        Assert.Equal(20, result.AverageValue);
        Assert.Equal(20, result.MedianValue);
        Assert.Equal(30, result.MaximumValue);
        Assert.Equal(10, result.MinimumValue);

        var values = await client.GetFromJsonAsync<List<ValueResponse>>(
            "/api/values/latest?fileName=integration.csv");

        Assert.NotNull(values);
        Assert.Equal(
            new[] { 30.0, 20.0, 10.0 },
            values.Select(item => item.Value));
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_ForInvalidCsv()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();

        using var response = await UploadAsync(
            client,
            "invalid.csv",
            "Wrong;Header;Names\n2024-01-10T10:00:00Z;1;10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_ForNonCsvFile()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();

        using var response = await UploadAsync(
            client,
            "data.txt",
            BuildCsv("2024-01-10T10:00:00Z;1;10"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_ForEmptyFile()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();

        using var response = await UploadAsync(
            client,
            "empty.csv",
            string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileNameIsTooLong()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();
        var fileName = $"{new string('a', 252)}.csv";

        using var response = await UploadAsync(
            client,
            fileName,
            BuildCsv("2024-01-10T10:00:00Z;1;10"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ReplacesPreviousData_WhenFileNameIsTheSame()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();

        using var firstResponse = await UploadAsync(
            client,
            "same.csv",
            BuildCsv(
                "2024-01-10T10:00:00Z;1;10",
                "2024-01-10T10:01:00Z;2;20"));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var secondResponse = await UploadAsync(
            client,
            "same.csv",
            BuildCsv(
                "2024-01-11T10:00:00Z;10;100",
                "2024-01-11T10:01:00Z;20;200",
                "2024-01-11T10:02:00Z;30;300"));
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var results = await client.GetFromJsonAsync<List<ResultResponse>>(
            "/api/results?fileName=same.csv");
        var values = await client.GetFromJsonAsync<List<ValueResponse>>(
            "/api/values/latest?fileName=same.csv");

        Assert.NotNull(results);
        Assert.Equal(200, Assert.Single(results).AverageValue);
        Assert.NotNull(values);
        Assert.Equal(
            new[] { 300.0, 200.0, 100.0 },
            values.Select(item => item.Value));
    }

    [Fact]
    public async Task GetResults_ReturnsBadRequest_ForReversedRange()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();

        using var response = await client.GetAsync(
            "/api/results?averageValueFrom=20&averageValueTo=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetResults_ReturnsEmptyArray_WhenFileDoesNotExist()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();

        using var response = await client.GetAsync(
            "/api/results?fileName=unknown.csv");
        var results = await response.Content
            .ReadFromJsonAsync<List<ResultResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetLatestValues_ReturnsBadRequest_WhenFileNameIsMissing()
    {
        using var factory = new WebApiFactory();
        using var client = await factory.CreateInitializedClientAsync();

        using var response = await client.GetAsync("/api/values/latest");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        string fileName,
        string csv)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        form.Add(fileContent, "file", fileName);

        return await client.PostAsync("/api/files/upload", form);
    }

    private static string BuildCsv(params string[] rows)
    {
        return string.Join(
            '\n',
            new[] { "Date;ExecutionTime;Value" }.Concat(rows));
    }
}
