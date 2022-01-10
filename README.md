# Upload/Download Files Using HttpClient in C#

In this new post, I show you how to upload/download files using HttpClient in [C#](https://www.puresourcecode.com/tag/csharp/) and [.NET Core](https://www.puresourcecode.com/category/dotnet/net-core/). Creating a new version of the [Markdown Editor component](https://www.puresourcecode.com/dotnet/blazor/markdown-editor-with-blazor/) for [Blazor](https://www.puresourcecode.com/category/dotnet/blazor/), I face some issues with the file upload. So, I was working to find a solution and now I can tell you how to do it.

First, I will take a look at how to send multipart MIME data to a [Web API](https://www.puresourcecode.com/category/dotnet/webapi/) using HttpClient. We will create two applications to demonstrate the data transfer between the _client side_ and the _server side_. The server-side app is an ASP.NET Core web project, which includes a `Web API controller` for uploading and downloading files. The client-side app is a `Console project`, which contains a [Typed HttpClient](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1#typed-clients) to send HTTP requests for file uploading and/or downloading.

The full explanetion of the code is in this [post]() on [PureSourceCode](https://www.puresourcecode.com/).
