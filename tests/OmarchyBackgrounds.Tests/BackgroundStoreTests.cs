using System.Net;
using System.Text;
using OmarchyBackgrounds.BackgroundStore;
using OmarchyBackgrounds.Catalog;

namespace OmarchyBackgrounds.Tests;

public class BackgroundStoreTests
{
    [Fact]
    public async Task GetLocalPathAsync_downloads_once_and_reuses_file()
    {
        var dir = Path.Combine(Path.GetTempPath(), "OmarchyBackgroundsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var downloads = 0;

        try
        {
            var handler = new StubHandler((_, _) =>
            {
                downloads++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-image-bytes")),
                });
            });

            using var client = new HttpClient(handler);
            var store = new FileBackgroundStore(client, dir);
            var background = new BackgroundImage
            {
                Id = "aura:1.png",
                FileName = "1.png",
                ImageUrl = "https://example.com/backgrounds/1.png",
            };

            var first = await store.GetLocalPathAsync(background);
            var second = await store.GetLocalPathAsync(background);

            Assert.Equal(first, second);
            Assert.True(File.Exists(first));
            Assert.Equal(1, downloads);
            Assert.Equal("fake-image-bytes", await File.ReadAllTextAsync(first));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
